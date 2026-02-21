using Xunit;
using FluentAssertions;
using SunEyeVision.Workflow;
using SunEyeVision.Core.Models;
using SunEyeVision.Core.Interfaces;
using Moq;

namespace SunEyeVision.Core.Tests.Workflow;

/// <summary>
/// WorkflowEngine单元测试
/// </summary>
public class WorkflowEngineTests
{
    private readonly WorkflowEngine _engine;
    private readonly Mock<ILogger> _loggerMock;

    public WorkflowEngineTests()
    {
        _loggerMock = new Mock<ILogger>();
        _engine = new WorkflowEngine(_loggerMock.Object);
    }

    [Fact]
    public void CreateWorkflow_ShouldReturnValidWorkflow()
    {
        // Act
        var workflow = _engine.CreateWorkflow("test-id", "TestWorkflow");

        // Assert
        workflow.Should().NotBeNull();
        workflow.Name.Should().Be("TestWorkflow");
        workflow.Id.Should().Be("test-id");
    }

    [Fact]
    public void CreateWorkflow_WithDuplicateId_ShouldThrow()
    {
        // Arrange
        _engine.CreateWorkflow("test-id", "Workflow1");

        // Act & Assert
        var action = () => _engine.CreateWorkflow("test-id", "Workflow2");
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GetWorkflow_ShouldReturnCreatedWorkflow()
    {
        // Arrange
        var created = _engine.CreateWorkflow("test-id", "TestWorkflow");

        // Act
        var retrieved = _engine.GetWorkflow("test-id");

        // Assert
        retrieved.Should().NotBeNull();
        retrieved.Should().BeSameAs(created);
    }

    [Fact]
    public void DeleteWorkflow_ShouldRemoveWorkflow()
    {
        // Arrange
        _engine.CreateWorkflow("test-id", "TestWorkflow");

        // Act
        var result = _engine.DeleteWorkflow("test-id");

        // Assert
        result.Should().BeTrue();
        _engine.GetWorkflow("test-id").Should().BeNull();
    }

    [Fact]
    public void SetCurrentWorkflow_ShouldSetCurrent()
    {
        // Arrange
        var workflow = _engine.CreateWorkflow("test-id", "TestWorkflow");

        // Act
        _engine.SetCurrentWorkflow("test-id");

        // Assert
        _engine.CurrentWorkflow.Should().NotBeNull();
        _engine.CurrentWorkflow.Should().BeSameAs(workflow);
    }

    [Fact]
    public void GetAllWorkflows_ShouldReturnAll()
    {
        // Arrange
        _engine.CreateWorkflow("id1", "Workflow1");
        _engine.CreateWorkflow("id2", "Workflow2");

        // Act
        var allWorkflows = _engine.GetAllWorkflows();

        // Assert
        allWorkflows.Should().HaveCount(2);
    }
}
