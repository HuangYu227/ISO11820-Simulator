using ISO11820Simulator.Models;

namespace ISO11820Simulator.Services;

public static class TrendService
{
    public static double ComputeDriftPer10Min(IReadOnlyList<TemperatureSample> samples, Func<TemperatureSample, double> selector)
    {
        if (samples.Count < 3) return 0;
        var n = samples.Count;
        var sumX = 0d;
        var sumY = 0d;
        var sumXY = 0d;
        var sumXX = 0d;
        for (var i = 0; i < n; i++)
        {
            var x = samples[i].TimeSeconds;
            var y = selector(samples[i]);
            sumX += x;
            sumY += y;
            sumXY += x * y;
            sumXX += x * x;
        }
        var denom = n * sumXX - sumX * sumX;
        if (Math.Abs(denom) < 1e-9) return 0;
        var slopePerSecond = (n * sumXY - sumX * sumY) / denom;
        return slopePerSecond * 600.0;
    }
}
