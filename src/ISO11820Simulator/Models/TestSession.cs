namespace ISO11820Simulator.Models;

public sealed class TestSession
{
    public string ProductId { get; set; } = string.Empty;
    public string TestId { get; set; } = DateTime.Now.ToString("yyyyMMdd-HHmmss");
    public string ProductName { get; set; } = string.Empty;
    public string Specific { get; set; } = string.Empty;
    public double Diameter { get; set; }
    public double Height { get; set; }
    public double AmbientTemperature { get; set; } = 25;
    public double AmbientHumidity { get; set; } = 50;
    public string According { get; set; } = "ISO 11820:2022";
    public string Operator { get; set; } = string.Empty;
    public string ApparatusId { get; set; } = "FURNACE-01";
    public string ApparatusName { get; set; } = "一号试验炉";
    public DateTime ApparatusCheckDate { get; set; } = DateTime.Today;
    public string ReportNo { get; set; } = string.Empty;
    public double PreWeight { get; set; }
    public int TargetDurationSeconds { get; set; } = 3600;
    public bool UseStandardDuration { get; set; } = true;
    public int ConstPower { get; set; }
    public bool Saved { get; set; }
}
