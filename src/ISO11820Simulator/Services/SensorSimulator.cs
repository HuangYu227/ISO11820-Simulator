using ISO11820Simulator.Config;
using ISO11820Simulator.Models;

namespace ISO11820Simulator.Services;

public sealed class SensorSimulator
{
    private readonly SimulationSettings _settings;
    private readonly Random _random = new();
    private double _tf1;
    private double _tf2;
    private double _surface;
    private double _center;
    private double _calibration;
    private int _stableTicks;

    public SensorSimulator(SimulationSettings settings)
    {
        _settings = settings;
        Reset();
    }

    public bool IsStable => _stableTicks >= Math.Max(1, _settings.StableTickCount);
    public int StableTicks => _stableTicks;

    public void Reset()
    {
        _tf1 = _settings.InitialFurnaceTemp;
        _tf2 = _settings.InitialFurnaceTemp - 0.3;
        _surface = Math.Max(_settings.InitialAmbientTemp, _tf1 * 0.30);
        _center = Math.Max(_settings.InitialAmbientTemp, _tf1 * 0.25);
        _calibration = _tf1;
        _stableTicks = 0;
    }

    public TemperatureSample Update(TestState state, double dtSeconds)
    {
        switch (state)
        {
            case TestState.Preparing:
            case TestState.Ready:
            case TestState.Complete:
                HeatOrHold(dtSeconds);
                _surface = _tf1 * 0.30 + Noise();
                _center = _tf1 * 0.25 + Noise();
                break;
            case TestState.Recording:
                HoldAtTarget();
                var surfaceTarget = Math.Min(_tf1 * 0.95, 800);
                var centerTarget = Math.Min(_tf1 * 0.85, 750);
                _surface += (surfaceTarget - _surface) * 0.02 + Noise();
                _center += (centerTarget - _center) * 0.01 + Noise();
                break;
            default:
                CoolDown();
                _surface = Math.Max(_settings.InitialAmbientTemp, _surface - 0.2 + Noise() * 0.1);
                _center = Math.Max(_settings.InitialAmbientTemp, _center - 0.15 + Noise() * 0.1);
                _stableTicks = 0;
                break;
        }

        _calibration = _tf1 + Noise() * 2;
        return new TemperatureSample
        {
            Timestamp = DateTime.Now,
            Tf1 = Clamp(_tf1, 0, 850),
            Tf2 = Clamp(_tf2, 0, 850),
            Surface = Clamp(_surface, 0, 850),
            Center = Clamp(_center, 0, 850),
            Calibration = Clamp(_calibration, 0, 850)
        };
    }

    private void HeatOrHold(double dtSeconds)
    {
        if (_tf1 < _settings.TargetFurnaceTemp - _settings.StableThreshold)
        {
            _tf1 += _settings.HeatingRatePerSecond * dtSeconds + Noise();
            _tf2 += _settings.HeatingRatePerSecond * dtSeconds + Noise();
            _stableTicks = 0;
            return;
        }
        HoldAtTarget();
    }

    private void HoldAtTarget()
    {
        _tf1 = _settings.TargetFurnaceTemp + Noise();
        _tf2 = _settings.TargetFurnaceTemp + Noise();
        _stableTicks++;
    }

    private void CoolDown()
    {
        _tf1 = Math.Max(_settings.InitialAmbientTemp, _tf1 - 0.5 + Noise() * 0.1);
        _tf2 = Math.Max(_settings.InitialAmbientTemp, _tf2 - 0.5 + Noise() * 0.1);
    }

    private double Noise() => (_random.NextDouble() * 2 - 1) * _settings.TempFluctuation;
    private static double Clamp(double value, double min, double max) => Math.Max(min, Math.Min(max, value));
}
