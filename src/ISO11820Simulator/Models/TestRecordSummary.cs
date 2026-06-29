namespace ISO11820Simulator.Models;

public sealed class TestRecordSummary
{
    public string ProductId { get; set; } = string.Empty;
    public string TestId { get; set; } = string.Empty;
    public DateTime TestDate { get; set; }
    public string Operator { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public double LostWeightPercent { get; set; }
    public double DeltaTf { get; set; }
    public int TotalTestTime { get; set; }
    public string Flag { get; set; } = string.Empty;
    public int FlameDuration { get; set; }
    public string ResultDisplay => Flag == "10000000" ? (DeltaTf <= 50 && LostWeightPercent <= 50 && FlameDuration < 5 ? "通过" : "不通过") : "未保存";
}
