namespace ISO11820Simulator.Models;

public enum TestState
{
    Idle,
    Preparing,
    Ready,
    Recording,
    Complete
}

public static class TestStateExtensions
{
    public static string ToChinese(this TestState state) => state switch
    {
        TestState.Idle => "空闲",
        TestState.Preparing => "升温中",
        TestState.Ready => "就绪",
        TestState.Recording => "记录中",
        TestState.Complete => "完成",
        _ => state.ToString()
    };
}
