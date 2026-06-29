namespace ISO11820Simulator.Models;

public sealed class TemperatureSample
{
    public int TimeSeconds { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public double Tf1 { get; set; }
    public double Tf2 { get; set; }
    public double Surface { get; set; }
    public double Center { get; set; }
    public double Calibration { get; set; }

    public TemperatureSample CloneWithTime(int seconds) => new()
    {
        TimeSeconds = seconds,
        Timestamp = DateTime.Now,
        Tf1 = Tf1,
        Tf2 = Tf2,
        Surface = Surface,
        Center = Center,
        Calibration = Calibration
    };
}
