using ISO11820Simulator.Config;
using ISO11820Simulator.Data;
using ISO11820Simulator.Models;
using Serilog;

namespace ISO11820Simulator.Services;

public sealed class TestController : IDisposable
{
    private readonly AppSettings _settings;
    private readonly DbHelper _db;
    private readonly ExportService _exporter;
    private readonly UserSession _session;
    private readonly SensorSimulator _simulator;
    private readonly System.Threading.Timer _timer;
    private readonly object _sync = new();
    private readonly List<TemperatureSample> _recorded = new();
    private readonly List<TemperatureSample> _history = new();
    private readonly Queue<int> _pidPowerQueue = new();
    private double _sampleAccumulator;
    private int _recordSeconds;
    private TemperatureSample _latest = new();
    private bool _disposed;

    public event EventHandler<DataBroadcastEventArgs>? DataBroadcast;

    public TestController(AppSettings settings, DbHelper db, ExportService exporter, UserSession session)
    {
        _settings = settings;
        _db = db;
        _exporter = exporter;
        _session = session;
        _simulator = new SensorSimulator(settings.Simulation);
        State = TestState.Idle;
        _timer = new System.Threading.Timer(Tick, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(800));
    }

    public TestState State { get; private set; }
    public TestSession? ActiveTest { get; private set; }
    public IReadOnlyList<TemperatureSample> RecordedSamples { get { lock (_sync) return _recorded.ToList(); } }
    public bool HasUnsavedCompletedTest => ActiveTest is { Saved: false } && State == TestState.Complete && _recorded.Count > 0;
    public TemperatureSample Latest { get { lock (_sync) return _latest; } }

    public void CreateTest(TestSession test)
    {
        lock (_sync)
        {
            if (HasUnsavedCompletedTest)
            {
                throw new InvalidOperationException("当前存在已完成但未保存的试验，请先保存试验记录。");
            }
            if (State == TestState.Recording)
            {
                throw new InvalidOperationException("记录中不能新建试验。");
            }
            ActiveTest = test;
            ActiveTest.Saved = false;
            _recorded.Clear();
            _recordSeconds = 0;
            _sampleAccumulator = 0;
            _db.CreateTest(test);
            AddBroadcastMessage($"新建试验成功：{test.ProductId} / {test.TestId}", MessageLevel.Success);
        }
    }

    public void StartHeating()
    {
        lock (_sync)
        {
            if (State == TestState.Recording) throw new InvalidOperationException("记录中不能开始升温。");
            State = TestState.Preparing;
            AddBroadcastMessage("开始升温，系统升温中", MessageLevel.Info);
        }
    }

    public void StopHeating()
    {
        lock (_sync)
        {
            if (State == TestState.Recording) throw new InvalidOperationException("记录中不能停止升温，请先停止记录。");
            State = TestState.Idle;
            AddBroadcastMessage("停止升温，炉温进入自然冷却", MessageLevel.Warning);
        }
    }

    public void StartRecording()
    {
        lock (_sync)
        {
            if (ActiveTest is null) throw new InvalidOperationException("请先新建试验。");
            if (State != TestState.Ready) throw new InvalidOperationException("炉温尚未稳定，不能开始记录。");
            if (HasUnsavedCompletedTest) throw new InvalidOperationException("当前试验已完成但未保存，请先保存试验记录。");
            ActiveTest.ConstPower = _pidPowerQueue.Count > 0 ? (int)_pidPowerQueue.Average() : _settings.Hardware.ConstPower;
            _recorded.Clear();
            _recordSeconds = 0;
            _sampleAccumulator = 0;
            State = TestState.Recording;
            AddBroadcastMessage($"开始记录，计时开始；恒功率={ActiveTest.ConstPower}", MessageLevel.Success);
        }
    }

    public void StopRecording(bool manual = true)
    {
        lock (_sync)
        {
            if (State != TestState.Recording) return;
            CompleteRecording(manual ? "用户手动停止记录" : "试验结束");
        }
    }

    public (TestResult Result, string Csv, string? Excel, string? Pdf) SavePhenomenon(ResultSaveRequest request)
    {
        lock (_sync)
        {
            if (ActiveTest is null) throw new InvalidOperationException("当前没有活动试验。");
            if (State != TestState.Complete || _recorded.Count == 0) throw new InvalidOperationException("试验尚未完成，不能保存现象记录。");
            if (request.PostWeight <= 0 || request.PostWeight > ActiveTest.PreWeight * 1.2)
            {
                throw new InvalidOperationException("试验后质量不合理，请检查输入。");
            }

            var result = BuildResult(ActiveTest, request, _recorded);
            _db.UpdateTestResult(ActiveTest, result);
            var csv = _exporter.SaveCsv(ActiveTest, _recorded);
            string? excel = null;
            string? pdf = null;

            // Excel导出 - 独立try-catch
            try
            {
                if (_settings.Report.EnableExcelExport) excel = _exporter.ExportExcelReport(ActiveTest, result, _recorded);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Excel导出失败，跳过");
            }

            // PDF导出 - 独立try-catch
            try
            {
                if (_settings.Report.EnablePdfExport) pdf = _exporter.ExportPdfReport(ActiveTest, result, _recorded);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "PDF导出失败，跳过");
            }

            ActiveTest.Saved = true;
            AddBroadcastMessage($"试验记录已保存，判定：{(result.Passed ? "通过" : "不通过")}", result.Passed ? MessageLevel.Success : MessageLevel.Warning);
            Log.Information("试验保存完成 {ProductId} {TestId} CSV={Csv} Excel={Excel} PDF={Pdf}", ActiveTest.ProductId, ActiveTest.TestId, csv, excel, pdf);
            ActiveTest = null;
            State = TestState.Preparing;
            return (result, csv, excel, pdf);
        }
    }

    private void Tick(object? state)
    {
        if (_disposed) return;
        List<MasterMessage> messages;
        TemperatureSample current;
        double driftTf1;
        double driftTf2;
        int seconds;
        TestState currentState;
        string? productId;
        bool unsaved;
        int stableTicks;

        lock (_sync)
        {
            var dt = 0.8;
            _latest = _simulator.Update(State, dt);
            _latest.TimeSeconds = _recordSeconds;
            _history.Add(_latest);
            TrimHistory();
            current = _latest;

            if (State is TestState.Preparing or TestState.Ready or TestState.Complete)
            {
                EnqueuePowerEstimate();
            }

            if (State == TestState.Preparing && CheckReady(current))
            {
                State = TestState.Ready;
                AddBroadcastMessage("温度已稳定，可以开始记录", MessageLevel.Success);
            }
            else if (State == TestState.Ready && !InReadyRange(current))
            {
                State = TestState.Preparing;
                AddBroadcastMessage("炉温跌出稳定范围，已退回升温中", MessageLevel.Warning);
            }

            if (State == TestState.Recording)
            {
                _sampleAccumulator += dt;
                if (_sampleAccumulator >= _settings.Experiment.CsvSamplingSeconds)
                {
                    _sampleAccumulator = 0;
                    _recordSeconds += _settings.Experiment.CsvSamplingSeconds;
                    _recorded.Add(current.CloneWithTime(_recordSeconds));
                    if (ShouldAutoStop())
                    {
                        CompleteRecording(_recordSeconds >= ActiveTest!.TargetDurationSeconds
                            ? $"记录时间到达 {_recordSeconds} 秒，试验自动结束"
                            : "满足终止条件，试验结束");
                    }
                }
            }

            driftTf1 = TrendService.ComputeDriftPer10Min(_history, x => x.Tf1);
            driftTf2 = TrendService.ComputeDriftPer10Min(_history, x => x.Tf2);
            seconds = _recordSeconds;
            currentState = State;
            productId = ActiveTest?.ProductId;
            unsaved = HasUnsavedCompletedTest;
            stableTicks = _simulator.StableTicks;
            messages = DrainMessages();
        }

        DataBroadcast?.Invoke(this, new DataBroadcastEventArgs
        {
            Current = current,
            State = currentState,
            Messages = messages,
            RecordSeconds = seconds,
            DriftTf1Per10Min = driftTf1,
            DriftTf2Per10Min = driftTf2,
            CurrentProductId = productId,
            HasUnsavedCompletedTest = unsaved,
            StableTicks = stableTicks
        });
    }

    private readonly List<MasterMessage> _pendingMessages = new();

    private void AddBroadcastMessage(string text, MessageLevel level)
    {
        _pendingMessages.Add(new MasterMessage { Message = text, Level = level, Time = DateTime.Now.ToString("HH:mm:ss") });
    }

    private List<MasterMessage> DrainMessages()
    {
        var list = _pendingMessages.ToList();
        _pendingMessages.Clear();
        return list;
    }

    private void TrimHistory()
    {
        var max = Math.Max(60, _settings.Experiment.ChartWindowSeconds + 20);
        if (_history.Count > max) _history.RemoveRange(0, _history.Count - max);
    }

    private void EnqueuePowerEstimate()
    {
        var power = _settings.Hardware.ConstPower + (int)Math.Round((_latest.Tf1 - _settings.Simulation.TargetFurnaceTemp) * 4);
        power = Math.Clamp(power, 0, 25600);
        _pidPowerQueue.Enqueue(power);
        while (_pidPowerQueue.Count > 600) _pidPowerQueue.Dequeue();
    }

    private bool InReadyRange(TemperatureSample sample)
    {
        var target = _settings.Simulation.TargetFurnaceTemp;
        return sample.Tf1 >= target - 5 && sample.Tf1 <= target + 5 && sample.Tf2 >= target - 5 && sample.Tf2 <= target + 5;
    }

    private bool CheckReady(TemperatureSample sample) => InReadyRange(sample) && _simulator.IsStable;

    private bool ShouldAutoStop()
    {
        if (ActiveTest is null) return false;
        if (_recordSeconds >= ActiveTest.TargetDurationSeconds) return true;
        if (!ActiveTest.UseStandardDuration) return false;
        if (_recordSeconds < 1800 || _recordSeconds % 300 != 0) return false;
        var last = _recorded.TakeLast(Math.Min(600, _recorded.Count)).ToList();
        if (last.Count < 60) return false;
        var d1 = Math.Abs(TrendService.ComputeDriftPer10Min(last, s => s.Tf1));
        var d2 = Math.Abs(TrendService.ComputeDriftPer10Min(last, s => s.Tf2));
        return d1 <= _settings.Simulation.MaxTemperatureDriftPerTenMinutes && d2 <= _settings.Simulation.MaxTemperatureDriftPerTenMinutes;
    }

    private void CompleteRecording(string reason)
    {
        State = TestState.Complete;
        AddBroadcastMessage(reason, reason.Contains("终止") ? MessageLevel.Warning : MessageLevel.Info);
    }

    private static TestResult BuildResult(TestSession test, ResultSaveRequest request, IReadOnlyList<TemperatureSample> samples)
    {
        var first = samples.First();
        var last = samples.Last();
        TemperatureSample MaxBy(Func<TemperatureSample, double> selector) => samples.OrderByDescending(selector).First();
        var maxTf1 = MaxBy(s => s.Tf1);
        var maxTf2 = MaxBy(s => s.Tf2);
        var maxTs = MaxBy(s => s.Surface);
        var maxTc = MaxBy(s => s.Center);
        var lost = test.PreWeight - request.PostWeight;
        var lostPer = test.PreWeight <= 0 ? 0 : lost / test.PreWeight * 100.0;
        var deltaTf1 = last.Tf1 - test.AmbientTemperature;
        var deltaTf2 = last.Tf2 - test.AmbientTemperature;
        var deltaTs = last.Surface - test.AmbientTemperature;
        var deltaTc = last.Center - test.AmbientTemperature;
        return new TestResult
        {
            PostWeight = request.PostWeight,
            LostWeight = lost,
            LostWeightPercent = lostPer,
            TotalTestTime = last.TimeSeconds,
            ConstPower = test.ConstPower,
            PhenoCode = request.HasFlame ? "FLAME" : "NONE",
            FlameTime = request.HasFlame ? request.FlameTime : 0,
            FlameDuration = request.HasFlame ? request.FlameDuration : 0,
            MaxTf1 = maxTf1.Tf1,
            MaxTf2 = maxTf2.Tf2,
            MaxTs = maxTs.Surface,
            MaxTc = maxTc.Center,
            MaxTf1Time = maxTf1.TimeSeconds,
            MaxTf2Time = maxTf2.TimeSeconds,
            MaxTsTime = maxTs.TimeSeconds,
            MaxTcTime = maxTc.TimeSeconds,
            FinalTf1 = last.Tf1,
            FinalTf2 = last.Tf2,
            FinalTs = last.Surface,
            FinalTc = last.Center,
            FinalTf1Time = last.TimeSeconds,
            FinalTf2Time = last.TimeSeconds,
            FinalTsTime = last.TimeSeconds,
            FinalTcTime = last.TimeSeconds,
            DeltaTf1 = deltaTf1,
            DeltaTf2 = deltaTf2,
            DeltaTf = deltaTs,
            DeltaTs = deltaTs,
            DeltaTc = deltaTc,
            Memo = request.Memo
        };
    }

    public void Dispose()
    {
        _disposed = true;
        _timer.Dispose();
    }
}
