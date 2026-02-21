using Xunit;
using FluentAssertions;
using SunEyeVision.Core.Events;
using SunEyeVision.Core.Interfaces;
using Moq;

namespace SunEyeVision.Core.Tests.Events;

// 使用别名解决EventHandler命名冲突
using TestEventHandler = SunEyeVision.Core.Events.EventHandler<SunEyeVision.Core.Tests.Events.TestEvent>;

/// <summary>
/// EventBus单元测试
/// </summary>
public class EventBusTests : IDisposable
{
    private readonly EventBus _eventBus;
    private readonly Mock<ILogger> _loggerMock;

    public EventBusTests()
    {
        _loggerMock = new Mock<ILogger>();
        _eventBus = new EventBus(_loggerMock.Object);
    }

    public void Dispose()
    {
        _eventBus?.Dispose();
    }

    [Fact]
    public void Subscribe_ShouldAddHandler()
    {
        // Arrange
        var handlerInvoked = false;
        TestEventHandler handler = e => handlerInvoked = true;
        
        // Act
        _eventBus.Subscribe(handler);
        _eventBus.Publish(new TestEvent("TestSource"));

        // Assert
        handlerInvoked.Should().BeTrue();
    }

    [Fact]
    public void Unsubscribe_ShouldRemoveHandler()
    {
        // Arrange
        var handlerInvoked = false;
        TestEventHandler handler = e => handlerInvoked = true;
        
        // Act
        _eventBus.Subscribe(handler);
        _eventBus.Unsubscribe(handler);
        _eventBus.Publish(new TestEvent("TestSource"));

        // Assert
        handlerInvoked.Should().BeFalse();
    }

    [Fact]
    public void Publish_WithMultipleHandlers_ShouldInvokeAll()
    {
        // Arrange
        var counter = 0;
        TestEventHandler handler1 = e => counter++;
        TestEventHandler handler2 = e => counter++;
        
        // Act
        _eventBus.Subscribe(handler1);
        _eventBus.Subscribe(handler2);
        _eventBus.Publish(new TestEvent("TestSource"));

        // Assert
        counter.Should().Be(2);
    }

    [Fact]
    public void Clear_ShouldRemoveAllHandlers()
    {
        // Arrange
        var counter = 0;
        TestEventHandler handler = e => counter++;
        _eventBus.Subscribe(handler);

        // Act
        _eventBus.Clear();
        _eventBus.Publish(new TestEvent("TestSource"));

        // Assert
        counter.Should().Be(0);
    }

    [Fact]
    public void Statistics_ShouldTrackSubscriptions()
    {
        // Arrange
        TestEventHandler handler = e => { };

        // Act
        _eventBus.Subscribe(handler);
        _eventBus.Subscribe(handler);

        // Assert
        _eventBus.Statistics.TotalSubscriptions.Should().Be(2);
    }
}
