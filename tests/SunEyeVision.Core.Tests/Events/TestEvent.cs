using SunEyeVision.Core.Events;

namespace SunEyeVision.Core.Tests.Events;

/// <summary>
/// 测试用事件
/// </summary>
public class TestEvent : EventBase
{
    public string Message { get; set; } = string.Empty;

    public TestEvent(string source) : base(source)
    {
    }
}
