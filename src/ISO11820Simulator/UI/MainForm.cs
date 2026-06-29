using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Guna.UI2.WinForms;
using ISO11820Simulator.App;
using ISO11820Simulator.Models;
using ISO11820Simulator.UI.Controls;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.WindowsForms;

namespace ISO11820Simulator.UI;

public sealed class MainForm : Form
{
    private readonly Label _status = Theme.Label("空闲", 12, true, Theme.Muted);
    // Realtime dashboard controls: 低频仿真数据 + 高频自绘动画
    private readonly TemperatureRail _temperatureRail = new();
    private readonly FurnaceThermalViewPro _furnaceView = new();
    private readonly DecisionStatusPanel _decisionPanel = new();
    private readonly ExperimentTimeline _timeline = new();
    private readonly PlotView _plot = new() { Dock = DockStyle.Fill, BackColor = Theme.Surface };
    private readonly LineSeries _tf1 = new() { Title = "炉温1", StrokeThickness = 2 };
    private readonly LineSeries _tf2 = new() { Title = "炉温2", StrokeThickness = 2 };
    private readonly LineSeries _ts = new() { Title = "表面温", StrokeThickness = 2 };
    private readonly LineSeries _tc = new() { Title = "中心温", StrokeThickness = 2 };
    private readonly Guna2Button _btnNew = Theme.GunaButton("＋ 新建试验", Theme.Accent);
    private readonly Guna2Button _btnStartHeat = Theme.GunaButton("▶ 开始升温");
    private readonly Guna2Button _btnStopHeat = Theme.GunaButton("■ 停止升温");
    private readonly Guna2Button _btnStartRecord = Theme.GunaButton("● 开始记录", Theme.Success);
    private readonly Guna2Button _btnStopRecord = Theme.GunaButton("■ 停止记录", Theme.Warning);
    private readonly Guna2Button _btnPhenomenon = Theme.GunaButton("📝 试验记录", Theme.Success);
    private readonly Guna2Button _btnSettings = Theme.GunaButton("⚙ 参数设置");
    private readonly DataGridView _recordGrid = new();
    private readonly DateTimePicker _from = new() { Format = DateTimePickerFormat.Short };
    private readonly DateTimePicker _to = new() { Format = DateTimePickerFormat.Short };
    private readonly Guna2TextBox _productFilter = Theme.GunaTextBox(placeholder: "样品编号");
    private readonly Guna2ComboBox _operatorFilter = Theme.GunaCombo();
    private readonly DataGridView _calibrationGrid = new();
    private readonly DataGridView _calibrationHistory = new();
    private readonly List<double> _calibrationPoints = new();
    private readonly Label _calibrationNow = Theme.Label("-- °C", 24, true, Theme.Accent);
    private readonly CalibrationHeatmap _calibrationHeatmap = new();
    private readonly AnalyticsDashboard _analyticsDashboard = new();
    private TemperatureSample _latest = new();
    private List<TestRecordSummary> _lastQueryRows = new();
    private int _chartIndex;

    public MainForm()
    {
        Text = "ISO 11820 建筑材料不燃性试验仿真系统";
        WindowState = FormWindowState.Maximized;
        MinimumSize = new Size(1000, 650);
        BackColor = Theme.Background;
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
        DoubleBuffered = true;
        Font = Theme.Body;
        BuildUi();
        ConfigurePlot();
        WireEvents();
        LoadOperators();
        QueryRecords();
        RefreshCalibrationHistory();
        GlobalApp.Controller.DataBroadcast += OnDataBroadcast;
        AppendLog("系统初始化，操作员：" + GlobalApp.Session.Username, MessageLevel.Info);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        GlobalApp.Controller.DataBroadcast -= OnDataBroadcast;
        base.OnFormClosed(e);
    }

    private void BuildUi()
    {
        var main = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, BackColor = Theme.Background };
        main.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
        main.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(main);

        // Header
        var header = new Guna2Panel { Dock = DockStyle.Fill, FillColor = Color.FromArgb(10, 16, 30), Padding = new Padding(24, 10, 24, 8) };
        main.Controls.Add(header, 0, 0);
        var headerLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, BackColor = header.FillColor };
        headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
        headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
        header.Controls.Add(headerLayout);
        var titleBox = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, BackColor = header.FillColor, WrapContents = false };
        var titleLabel = Theme.Label("ISO 11820 建筑材料不燃性试验仿真系统", 16, true, Theme.Text);
        titleLabel.AutoSize = false;
        titleLabel.MaximumSize = new Size(500, 0);
        titleLabel.AutoEllipsis = true;
        titleBox.Controls.Add(titleLabel);
        titleBox.Controls.Add(Theme.Label("实时采集 · 状态机控制 · SQLite 归档 · Excel/PDF 报告", 9, false, Theme.Muted));
        headerLayout.Controls.Add(titleBox, 0, 0);
        headerLayout.Controls.Add(Theme.Label($"{GlobalApp.Session.RoleDisplay}：{GlobalApp.Session.Username}", 11, true, Theme.Muted), 1, 0);
        var statusPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, BackColor = header.FillColor, WrapContents = false };
        statusPanel.Controls.Add(new Label { Text = "●", ForeColor = Theme.Muted, Font = new Font("Microsoft YaHei UI", 14), AutoSize = true, Margin = new Padding(0, 4, 6, 0) });
        statusPanel.Controls.Add(_status);
        headerLayout.Controls.Add(statusPanel, 2, 0);

        // Body
        var body = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, BackColor = Theme.Background, Padding = new Padding(10) };
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200));
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        main.Controls.Add(body, 0, 1);

        // Sidebar
        var sidebar = new Guna2Panel { Dock = DockStyle.Fill, FillColor = Theme.Surface, BorderRadius = 8, BorderColor = Theme.Border, BorderThickness = 1, Padding = new Padding(8) };
        var sideLayout = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, RowCount = 10, ColumnCount = 1, BackColor = Theme.Surface };
        sidebar.Controls.Add(sideLayout);
        foreach (var b in new Guna2Button[] { _btnNew, _btnStartHeat, _btnStopHeat, _btnStartRecord, _btnStopRecord, _btnPhenomenon, _btnSettings })
        {
            b.Dock = DockStyle.Top;
            b.Margin = new Padding(4, 3, 4, 3);
            sideLayout.Controls.Add(b);
        }
        var hint = Theme.Label("完成后必须先保存试验记录，才能新建下一次试验。", 8, false, Theme.Muted);
        hint.MaximumSize = new Size(170, 0);
        sideLayout.Controls.Add(hint);
        body.Controls.Add(sidebar, 0, 0);

        // Tabs
        var tabs = new Guna2TabControl { Dock = DockStyle.Fill, BackColor = Theme.Background };
        tabs.TabPages.Add(BuildDashboardTab());
        tabs.TabPages.Add(BuildRecordsTab());
        tabs.TabPages.Add(BuildCalibrationTab());
        tabs.TabPages.Add(BuildAnalyticsTab());
        body.Controls.Add(tabs, 1, 0);
    }

    private Guna2Panel MakeCard()
    {
        return new Guna2Panel { FillColor = Theme.Surface, BorderRadius = 8, BorderColor = Theme.Border, BorderThickness = 1, Margin = new Padding(4), Padding = new Padding(10, 8, 10, 8) };
    }

    private TabPage BuildDashboardTab()
    {
        var page = new TabPage("实时试验") { BackColor = Theme.Background };

        // 新首页布局：顶部温度总览横跨全宽，右侧炉膛热场变成主模块。
        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 3,
            ColumnCount = 2,
            Padding = new Padding(8),
            BackColor = Theme.Background
        };
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 118));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 260));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 63));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 37));
        page.Controls.Add(grid);

        _temperatureRail.Dock = DockStyle.Fill;
        grid.Controls.Add(_temperatureRail, 0, 0);
        grid.SetColumnSpan(_temperatureRail, 2);

        var plotSection = MakeSection("温度曲线", "绿色背景带表示 745–755°C 稳定区间，黄色虚线表示 750°C 目标炉温", _plot);
        grid.Controls.Add(plotSection, 0, 1);

        _furnaceView.Dock = DockStyle.Fill;
        var furnaceSection = MakeSection("炉膛热场", "动态热场 / 加热线圈 / 传感器位置 / 样品温度梯度", _furnaceView);
        grid.Controls.Add(furnaceSection, 1, 1);

        _timeline.Dock = DockStyle.Fill;
        grid.Controls.Add(MakeSection("事件时间线", "系统初始化、升温、稳定、记录、结束等关键节点", _timeline), 0, 2);

        _decisionPanel.Dock = DockStyle.Fill;
        grid.Controls.Add(MakeSection("稳定性判定", "炉温范围 + 温漂 + 稳定计数", _decisionPanel), 1, 2);

        return page;
    }

    private Guna2Panel MakeSection(string title, string subtitle, Control content)
    {
        var card = MakeCard();
        card.Dock = DockStyle.Fill;
        card.Padding = new Padding(12, 10, 12, 12);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1,
            BackColor = Theme.Surface
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            BackColor = Theme.Surface
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var titleLabel = Theme.Label(title, 10, true, Theme.Text);
        titleLabel.Margin = new Padding(0, 2, 10, 0);
        header.Controls.Add(titleLabel, 0, 0);

        var subLabel = Theme.Label(subtitle, 8, false, Theme.Muted);
        subLabel.AutoSize = false;
        subLabel.Dock = DockStyle.Fill;
        subLabel.TextAlign = ContentAlignment.MiddleLeft;
        subLabel.AutoEllipsis = true;
        header.Controls.Add(subLabel, 1, 0);

        content.Dock = DockStyle.Fill;
        layout.Controls.Add(header, 0, 0);
        layout.Controls.Add(content, 0, 1);
        card.Controls.Add(layout);
        return card;
    }

    private TabPage BuildRecordsTab()
    {
        var page = new TabPage("记录查询") { BackColor = Theme.Background };
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, BackColor = Theme.Background };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 110));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        page.Controls.Add(root);
        var filters = MakeCard(); filters.Dock = DockStyle.Fill;
        var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, BackColor = Theme.Surface, FlowDirection = FlowDirection.LeftToRight, WrapContents = true, Padding = new Padding(10, 10, 10, 10) };
        filters.Controls.Add(flow);
        _from.Value = DateTime.Today.AddDays(-30);
        _to.Value = DateTime.Today.AddDays(1);
        flow.Controls.Add(Theme.Label("开始", 9, false, Theme.Muted)); flow.Controls.Add(_from);
        flow.Controls.Add(Theme.Label("结束", 9, false, Theme.Muted)); flow.Controls.Add(_to);
        flow.Controls.Add(Theme.Label("样品编号", 9, false, Theme.Muted)); _productFilter.Width = 140; flow.Controls.Add(_productFilter);
        flow.Controls.Add(Theme.Label("操作员", 9, false, Theme.Muted)); _operatorFilter.Width = 120; flow.Controls.Add(_operatorFilter);
        var search = Theme.GunaButton("查询", Theme.Accent); search.Width = 80; search.Height = 34; search.Click += (_, _) => QueryRecords(); flow.Controls.Add(search);
        var export = Theme.GunaButton("导出", Theme.Success); export.Width = 80; export.Height = 34; export.Click += (_, _) => ExportQuery(); flow.Controls.Add(export);
        root.Controls.Add(filters);
        Theme.StyleGrid(_recordGrid);
        _recordGrid.Dock = DockStyle.Fill;
        _recordGrid.CellDoubleClick += (_, _) => ShowSelectedRecordDetail();
        root.Controls.Add(_recordGrid);
        return page;
    }

    private TabPage BuildCalibrationTab()
    {
        var page = new TabPage("设备校准") { BackColor = Theme.Background };
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, BackColor = Theme.Background };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
        page.Controls.Add(root);
        var left = MakeCard(); left.Dock = DockStyle.Fill;
        var l = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 7, ColumnCount = 1, BackColor = Theme.Surface };
        l.RowStyles.Add(new RowStyle(SizeType.Absolute, 28)); l.Controls.Add(Theme.Label("当前校准温度", 10, true, Theme.Muted));
        l.RowStyles.Add(new RowStyle(SizeType.Absolute, 50)); _calibrationNow.Dock = DockStyle.Fill; _calibrationNow.TextAlign = ContentAlignment.MiddleCenter; l.Controls.Add(_calibrationNow);
        l.RowStyles.Add(new RowStyle(SizeType.Absolute, 40)); var record = Theme.GunaButton("记录当前点", Theme.Accent); record.Dock = DockStyle.Fill; record.Height = 36; record.Click += (_, _) => RecordCalibrationPoint(); l.Controls.Add(record);
        l.RowStyles.Add(new RowStyle(SizeType.Absolute, 40)); var save = Theme.GunaButton("保存校准记录", Theme.Success); save.Dock = DockStyle.Fill; save.Height = 36; save.Click += (_, _) => SaveCalibration(); l.Controls.Add(save);
        l.RowStyles.Add(new RowStyle(SizeType.Absolute, 40)); var clear = Theme.GunaButton("清空采样点"); clear.Dock = DockStyle.Fill; clear.Height = 36; clear.Click += (_, _) => { _calibrationPoints.Clear(); RefreshCalibrationPoints(); }; l.Controls.Add(clear);
        l.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); Theme.StyleGrid(_calibrationGrid); _calibrationGrid.Dock = DockStyle.Fill; l.Controls.Add(_calibrationGrid);
        left.Controls.Add(l); root.Controls.Add(left, 0, 0);
        var center = MakeCard(); center.Dock = DockStyle.Fill; _calibrationHeatmap.Dock = DockStyle.Fill; center.Controls.Add(_calibrationHeatmap); root.Controls.Add(center, 1, 0);
        var right = MakeCard(); right.Dock = DockStyle.Fill;
        var rl = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, BackColor = Theme.Surface };
        rl.RowStyles.Add(new RowStyle(SizeType.Absolute, 28)); rl.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        rl.Controls.Add(Theme.Label("历史校准记录", 10, true, Theme.Muted)); Theme.StyleGrid(_calibrationHistory); _calibrationHistory.Dock = DockStyle.Fill; rl.Controls.Add(_calibrationHistory);
        right.Controls.Add(rl); root.Controls.Add(right, 2, 0);
        return page;
    }

    private TabPage BuildAnalyticsTab()
    {
        var page = new TabPage("数据分析") { BackColor = Theme.Background };
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 1, ColumnCount = 1, Padding = new Padding(8), BackColor = Theme.Background };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        page.Controls.Add(root); _analyticsDashboard.Dock = DockStyle.Fill; root.Controls.Add(_analyticsDashboard);
        return page;
    }

    private void ConfigurePlot()
    {
        var model = new PlotModel();
        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Bottom,
            Title = "时间(s)",
            MinimumPadding = 0,
            MaximumPadding = 0
        });
        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Left,
            Title = "温度(℃)",
            Minimum = 0,
            Maximum = 800
        });

        _tf1.Color = OxyColor.FromRgb(6, 182, 212);
        _tf2.Color = OxyColor.FromRgb(59, 130, 246);
        _ts.Color = OxyColor.FromRgb(16, 185, 129);
        _tc.Color = OxyColor.FromRgb(245, 158, 11);
        model.Series.Add(_tf1);
        model.Series.Add(_tf2);
        model.Series.Add(_ts);
        model.Series.Add(_tc);

        model.Legends.Add(new OxyPlot.Legends.Legend
        {
            LegendPosition = OxyPlot.Legends.LegendPosition.TopRight,
            LegendPlacement = OxyPlot.Legends.LegendPlacement.Outside,
            LegendOrientation = OxyPlot.Legends.LegendOrientation.Horizontal,
            TextColor = OxyColors.LightGray,
            LegendBackground = OxyColor.FromArgb(200, 30, 41, 59),
            LegendBorder = OxyColor.FromRgb(71, 85, 105)
        });

        PlotIsoAnnotations.ApplyTemperaturePlotTheme(model);
        _plot.Model = model;
    }

    private void WireEvents()
    {
        _btnNew.Click += (_, _) => OpenNewTest();
        _btnStartHeat.Click += (_, _) => SafeRun(() => GlobalApp.Controller.StartHeating());
        _btnStopHeat.Click += (_, _) => SafeRun(() => GlobalApp.Controller.StopHeating());
        _btnStartRecord.Click += (_, _) => SafeRun(() => GlobalApp.Controller.StartRecording());
        _btnStopRecord.Click += (_, _) => SafeRun(() => GlobalApp.Controller.StopRecording());
        _btnPhenomenon.Click += (_, _) => OpenPhenomenon();
        _btnSettings.Click += (_, _) => new SettingsForm(GlobalApp.Settings).ShowDialog(this);
    }

    private void OnDataBroadcast(object? sender, DataBroadcastEventArgs e)
    {
        if (IsDisposed) return;
        BeginInvoke(new Action(() =>
        {
            _latest = e.Current;
            _status.Text = e.State.ToChinese();
            _status.ForeColor = e.State switch
            {
                TestState.Ready => Theme.Success,
                TestState.Recording => Theme.Accent,
                TestState.Complete => Theme.Warning,
                TestState.Preparing => Theme.Warning,
                _ => Theme.Muted
            };

            var snapshot = ToThermalSnapshot(e);
            _temperatureRail.UpdateSnapshot(snapshot);
            _furnaceView.PushSnapshot(snapshot);
            _decisionPanel.UpdateSnapshot(snapshot);

            _calibrationNow.Text = $"{e.Current.Calibration:F1} °C";
            foreach (var msg in e.Messages) AppendLog(msg.Message, msg.Level, msg.Time);
            AddPoint(e.Current);
            UpdateButtons(e.State, e.HasUnsavedCompletedTest);
        }));
    }

    private static ThermalSnapshot ToThermalSnapshot(DataBroadcastEventArgs e)
    {
        return new ThermalSnapshot(
            Tf1: e.Current.Tf1,
            Tf2: e.Current.Tf2,
            Surface: e.Current.Surface,
            Center: e.Current.Center,
            Calibration: e.Current.Calibration,
            DriftTf1Per10Min: e.DriftTf1Per10Min,
            DriftTf2Per10Min: e.DriftTf2Per10Min,
            StableTicks: e.StableTicks,
            RecordSeconds: e.RecordSeconds,
            SampleNo: e.CurrentProductId ?? "未选择样品",
            StateText: e.State.ToChinese(),
            State: ToVisualState(e.State));
    }

    private static ThermalVisualState ToVisualState(TestState state)
    {
        return state switch
        {
            TestState.Preparing => ThermalVisualState.Preparing,
            TestState.Ready => ThermalVisualState.Ready,
            TestState.Recording => ThermalVisualState.Recording,
            TestState.Complete => ThermalVisualState.Complete,
            _ => ThermalVisualState.Idle
        };
    }

    private void AddPoint(TemperatureSample sample)
    {
        var x = _chartIndex++;
        _tf1.Points.Add(new DataPoint(x, sample.Tf1));
        _tf2.Points.Add(new DataPoint(x, sample.Tf2));
        _ts.Points.Add(new DataPoint(x, sample.Surface));
        _tc.Points.Add(new DataPoint(x, sample.Center));
        var maxPoints = GlobalApp.Settings.Experiment.ChartWindowSeconds;
        foreach (var s in new[] { _tf1, _tf2, _ts, _tc }) while (s.Points.Count > maxPoints) s.Points.RemoveAt(0);
        _plot.Model?.Axes[0].Reset();
        _plot.Model?.InvalidatePlot(true);
    }

    private void AppendLog(string message, MessageLevel level, string? time = null)
    {
        var color = level switch { MessageLevel.Success => Theme.Success, MessageLevel.Warning => Theme.Warning, MessageLevel.Error => Theme.Danger, _ => Theme.Muted };
        var ts = time ?? DateTime.Now.ToString("HH:mm:ss");
        _timeline.AddEntry(ts, message, color);
    }

    private void UpdateButtons(TestState state, bool unsaved)
    {
        var hasActive = GlobalApp.Controller.ActiveTest is not null;
        _btnNew.Enabled = state != TestState.Recording && !unsaved;
        _btnStartHeat.Enabled = state == TestState.Idle;
        _btnStopHeat.Enabled = state is TestState.Preparing or TestState.Ready or TestState.Complete;
        _btnStartRecord.Enabled = state == TestState.Ready && hasActive && !unsaved;
        _btnStopRecord.Enabled = state == TestState.Recording;
        _btnPhenomenon.Enabled = unsaved;
        _btnSettings.Enabled = state != TestState.Recording;
    }

    private void OpenNewTest() { using var form = new NewTestForm(); if (form.ShowDialog(this) == DialogResult.OK && form.Test is not null) SafeRun(() => { GlobalApp.Controller.CreateTest(form.Test); }); }

    private void OpenPhenomenon()
    {
        var active = GlobalApp.Controller.ActiveTest; if (active is null) return;
        using var form = new PhenomenonForm(active.PreWeight);
        if (form.ShowDialog(this) == DialogResult.OK && form.Request is not null)
            SafeRun(() => { var result = GlobalApp.Controller.SavePhenomenon(form.Request); MessageBox.Show($"保存成功！\nCSV: {result.Csv}\nExcel: {result.Excel}\nPDF: {result.Pdf}", "报告生成", MessageBoxButtons.OK, MessageBoxIcon.Information); QueryRecords(); });
    }

    private void LoadOperators() { _operatorFilter.Items.Clear(); _operatorFilter.Items.Add("全部"); foreach (var op in GlobalApp.Db.GetOperators()) _operatorFilter.Items.Add(op); _operatorFilter.SelectedIndex = 0; }

    private void QueryRecords()
    {
        var op = _operatorFilter.SelectedItem?.ToString() == "全部" ? "" : _operatorFilter.SelectedItem?.ToString() ?? "";
        _lastQueryRows = GlobalApp.Db.QueryTests(_from.Value.Date, _to.Value.Date, _productFilter.Text.Trim(), op);
        _recordGrid.DataSource = _lastQueryRows.Select(x => new { 样品编号 = x.ProductId, 试验ID = x.TestId, 试验日期 = x.TestDate.ToString("yyyy-MM-dd"), 操作员 = x.Operator, 样品名称 = x.ProductName, 失重率 = x.LostWeightPercent.ToString("F2") + "%", 温升 = x.DeltaTf.ToString("F2") + "℃", 试验时长 = x.TotalTestTime + "s", 判定结果 = x.ResultDisplay }).ToList();
        _analyticsDashboard.UpdateData(_lastQueryRows);
    }

    private void ExportQuery() { if (_lastQueryRows.Count == 0) { MessageBox.Show("没有可导出的查询结果"); return; } SafeRun(() => MessageBox.Show("已导出：" + GlobalApp.Exporter.ExportQueryResult(_lastQueryRows), "导出成功")); }

    private void ShowSelectedRecordDetail() { if (_recordGrid.CurrentRow?.Cells[0].Value is null) return; var pid = _recordGrid.CurrentRow.Cells[0].Value.ToString()!; var tid = _recordGrid.CurrentRow.Cells[1].Value.ToString()!; var detail = GlobalApp.Db.GetTestDetail(pid, tid); using var form = new TestDetailForm(detail); form.ShowDialog(this); }

    private void RecordCalibrationPoint() { _calibrationPoints.Add(_latest.Calibration); RefreshCalibrationPoints(); if (_calibrationPoints.Count >= 9) _calibrationHeatmap.UpdateData(_calibrationPoints.Take(9).ToArray()); }

    private void RefreshCalibrationPoints() { _calibrationGrid.DataSource = _calibrationPoints.Select((v, i) => new { Index = i + 1, Temperature = v.ToString("F2") + "℃", Deviation = (v - _calibrationPoints.Average()).ToString("F2") }).ToList(); }

    private void SaveCalibration() { SafeRun(() => { GlobalApp.Db.SaveCalibration(_calibrationPoints, "Surface", GlobalApp.Session.Username, "界面手动校准"); _calibrationPoints.Clear(); RefreshCalibrationPoints(); RefreshCalibrationHistory(); MessageBox.Show("校准记录已保存"); }); }

    private void RefreshCalibrationHistory() { _calibrationHistory.DataSource = GlobalApp.Db.QueryCalibrations().Select(x => new { Date = x.CalibrationDate.ToString("yyyy-MM-dd HH:mm"), Type = x.CalibrationType, x.Operator, Avg = x.AverageTemperature?.ToString("F2"), MaxDeviation = x.MaxDeviation?.ToString("F2"), Passed = x.PassedCriteria ? "通过" : "未通过" }).ToList(); }

    private void SafeRun(Action action) { try { action(); } catch (Exception ex) { AppendLog(ex.Message, MessageLevel.Error); MessageBox.Show(ex.Message, "操作失败", MessageBoxButtons.OK, MessageBoxIcon.Warning); } }
}
