namespace ISO11820Simulator.Models;

public enum MessageLevel
{
    Info,
    Success,
    Warning,
    Error
}

public sealed class MasterMessage
{
    public string Time { get; set; } = DateTime.Now.ToString("HH:mm:ss");
    public string Message { get; set; } = string.Empty;
    public MessageLevel Level { get; set; } = MessageLevel.Info;
}
