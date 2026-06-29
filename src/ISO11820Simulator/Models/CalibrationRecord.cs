namespace ISO11820Simulator.Models;

public sealed class CalibrationRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public DateTime CalibrationDate { get; set; } = DateTime.Now;
    public string CalibrationType { get; set; } = "Surface";
    public int ApparatusId { get; set; }
    public string Operator { get; set; } = string.Empty;
    public string TemperatureData { get; set; } = "[]";
    public double? UniformityResult { get; set; }
    public double? MaxDeviation { get; set; }
    public double? AverageTemperature { get; set; }
    public bool PassedCriteria { get; set; }
    public string Remarks { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
