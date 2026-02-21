using Xunit;
using FluentAssertions;
using SunEyeVision.Workflow;

namespace SunEyeVision.Core.Tests.Workflow;

// 使用别名解决LogLevel命名冲突
using WorkflowLogLevel = SunEyeVision.Workflow.LogLevel;

/// <summary>
/// WorkflowContext单元测试
/// </summary>
public class WorkflowContextTests
{
    [Fact]
    public void Constructor_ShouldInitializeProperties()
    {
        // Act
        var context = new WorkflowContext();

        // Assert
        context.ExecutionId.Should().NotBeNullOrEmpty();
        context.Variables.Should().NotBeNull();
        context.CallStack.Should().NotBeNull();
        context.ExecutionPath.Should().NotBeNull();
        context.NodeStates.Should().NotBeNull();
        context.Logs.Should().NotBeNull();
    }

    [Fact]
    public void SetVariable_ShouldStoreValue()
    {
        // Arrange
        var context = new WorkflowContext();

        // Act
        context.SetVariable("testVar", 42);

        // Assert
        var value = context.GetVariable<int>("testVar");
        value.Should().Be(42);
    }

    [Fact]
    public void GetVariable_WithNonExistentKey_ShouldReturnDefault()
    {
        // Arrange
        var context = new WorkflowContext();

        // Act
        var value = context.GetVariable<int>("nonExistent");

        // Assert
        value.Should().Be(0);
    }

    [Fact]
    public void HasVariable_ShouldReturnCorrectValue()
    {
        // Arrange
        var context = new WorkflowContext();
        context.SetVariable("existingVar", "value");

        // Act & Assert
        context.HasVariable("existingVar").Should().BeTrue();
        context.HasVariable("nonExistentVar").Should().BeFalse();
    }

    [Fact]
    public void RemoveVariable_ShouldDeleteVariable()
    {
        // Arrange
        var context = new WorkflowContext();
        context.SetVariable("toBeRemoved", "value");

        // Act
        var removed = context.RemoveVariable("toBeRemoved");

        // Assert
        removed.Should().BeTrue();
        context.HasVariable("toBeRemoved").Should().BeFalse();
    }

    [Fact]
    public void CallStack_ShouldTrackDepth()
    {
        // Arrange
        var context = new WorkflowContext();

        // Act
        context.PushCallInfo(new SubroutineCallInfo { NodeId = "node1", SubroutineId = "sub1" });
        context.PushCallInfo(new SubroutineCallInfo { NodeId = "node2", SubroutineId = "sub2" });

        // Assert
        context.GetCurrentCallDepth().Should().Be(2);
        
        context.PopCallInfo();
        context.GetCurrentCallDepth().Should().Be(1);
    }

    [Fact]
    public void UpdateNodeStatus_ShouldTrackStatus()
    {
        // Arrange
        var context = new WorkflowContext();

        // Act
        context.UpdateNodeStatus("node1", NodeStatus.Running);
        context.UpdateNodeStatus("node1", NodeStatus.Completed);

        // Assert
        context.NodeStates["node1"].Status.Should().Be(NodeStatus.Completed);
    }

    [Fact]
    public void AddLog_ShouldRecordMessages()
    {
        // Arrange
        var context = new WorkflowContext();

        // Act
        context.AddLog("Test message", WorkflowLogLevel.Info);
        context.AddLog("Warning message", WorkflowLogLevel.Warning);

        // Assert
        context.Logs.Should().HaveCount(2);
        context.Logs[0].Message.Should().Be("Test message");
        context.Logs[0].Level.Should().Be(WorkflowLogLevel.Info);
    }

    [Fact]
    public void CreateSubContext_ShouldInheritVariables()
    {
        // Arrange
        var context = new WorkflowContext();
        context.SetVariable("parentVar", "parentValue");

        // Act
        var subContext = context.CreateSubContext("subroutine1");

        // Assert
        subContext.HasVariable("parentVar").Should().BeTrue();
        subContext.GetVariable<string>("parentVar").Should().Be("parentValue");
    }

    [Fact]
    public void GetStatistics_ShouldReturnValidStats()
    {
        // Arrange
        var context = new WorkflowContext();
        context.UpdateNodeStatus("node1", NodeStatus.Completed);
        context.UpdateNodeStatus("node2", NodeStatus.Failed);
        context.AddLog("Test log", WorkflowLogLevel.Info);

        // Act
        var stats = context.GetStatistics();

        // Assert
        stats.Should().NotBeNull();
        stats.TotalNodes.Should().Be(2);
        stats.CompletedNodes.Should().Be(1);
        stats.FailedNodes.Should().Be(1);
        stats.TotalLogs.Should().Be(1);
    }
}
