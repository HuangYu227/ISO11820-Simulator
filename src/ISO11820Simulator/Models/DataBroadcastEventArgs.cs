namespace ISO11820Simulator.Models;

public sealed class DataBroadcastEventArgs : EventArgs
{
    public required TemperatureSample Current { get; init; }
    public required TestState State { get; init; }
    public required IReadOnlyList<MasterMessage> Messages { get; init; }
    public required int RecordSeconds { get; init; }
    public required double DriftTf1Per10Min { get; init; }
    public required double DriftTf2Per10Min { get; init; }
    public required string? CurrentProductId { get; init; }
    public required bool HasUnsavedCompletedTest { get; init; }
    public required int StableTicks { get; init; }
}
