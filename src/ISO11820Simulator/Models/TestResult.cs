namespace ISO11820Simulator.Models;

public sealed class ResultSaveRequest
{
    public bool HasFlame { get; set; }
    public int FlameTime { get; set; }
    public int FlameDuration { get; set; }
    public double PostWeight { get; set; }
    public string Memo { get; set; } = string.Empty;
}

public sealed class TestResult
{
    public double PostWeight { get; set; }
    public double LostWeight { get; set; }
    public double LostWeightPercent { get; set; }
    public int TotalTestTime { get; set; }
    public int ConstPower { get; set; }
    public string PhenoCode { get; set; } = string.Empty;
    public int FlameTime { get; set; }
    public int FlameDuration { get; set; }
    public double MaxTf1 { get; set; }
    public double MaxTf2 { get; set; }
    public double MaxTs { get; set; }
    public double MaxTc { get; set; }
    public int MaxTf1Time { get; set; }
    public int MaxTf2Time { get; set; }
    public int MaxTsTime { get; set; }
    public int MaxTcTime { get; set; }
    public double FinalTf1 { get; set; }
    public double FinalTf2 { get; set; }
    public double FinalTs { get; set; }
    public double FinalTc { get; set; }
    public int FinalTf1Time { get; set; }
    public int FinalTf2Time { get; set; }
    public int FinalTsTime { get; set; }
    public int FinalTcTime { get; set; }
    public double DeltaTf1 { get; set; }
    public double DeltaTf2 { get; set; }
    public double DeltaTf { get; set; }
    public double DeltaTs { get; set; }
    public double DeltaTc { get; set; }
    public string Memo { get; set; } = string.Empty;
    public bool Passed => DeltaTf <= 50 && LostWeightPercent <= 50 && FlameDuration < 5;
}
