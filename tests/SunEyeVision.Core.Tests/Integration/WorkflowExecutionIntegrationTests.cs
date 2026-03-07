using System;
using System.Threading.Tasks;
using FluentAssertions;
using SunEyeVision.Workflow;
using SunEyeVision.Plugin.SDK.Logging;
using Moq;
using Xunit;

namespace SunEyeVision.Core.Tests.Integration
{
    /// <summary>
    /// 工作流执行集成测试
    /// 简化版本，验证基本概念
    /// </summary>
    public class WorkflowExecutionIntegrationTests
    {
        private readonly WorkflowEngine _workflowEngine;
        private readonly Mock<ILogger> _loggerMock;

        public WorkflowExecutionIntegrationTests()
        {
            _loggerMock = new Mock<ILogger>();
            _workflowEngine = new WorkflowEngine(_loggerMock.Object);
        }

        #region 工作流创建测试

        [Fact]
        public void WorkflowEngine_CreateWorkflow_ShouldInitializeCorrectly()
        {
            // Arrange & Act
            var workflow = _workflowEngine.CreateWorkflow("test-workflow-1", "测试工作流");

            // Assert
            workflow.Should().NotBeNull();
            workflow.Id.Should().Be("test-workflow-1");
            workflow.Name.Should().Be("测试工作流");
        }

        [Fact]
        public void WorkflowEngine_CreateMultipleWorkflows_ShouldManageCorrectly()
        {
            // Arrange & Act
            var workflow1 = _workflowEngine.CreateWorkflow("wf-1", "工作流1");
            var workflow2 = _workflowEngine.CreateWorkflow("wf-2", "工作流2");

            // Assert
            _workflowEngine.GetAllWorkflows().Should().HaveCount(2);
            _workflowEngine.GetWorkflow("wf-1").Should().NotBeNull();
            _workflowEngine.GetWorkflow("wf-2").Should().NotBeNull();
        }

        #endregion

        #region 执行上下文测试

        [Fact]
        public void WorkflowContext_CreateSubContext_ShouldInheritParentVariables()
        {
            // Arrange
            var parentContext = new WorkflowContext();
            parentContext.SetVariable("ParentVar", "ParentValue");

            // Act
            var subContext = parentContext.CreateSubContext("subroutine-1");

            // Assert
            subContext.HasVariable("ParentVar").Should().BeTrue();
            subContext.GetVariable<string>("ParentVar").Should().Be("ParentValue");
        }

        [Fact]
        public void WorkflowContext_CallStack_ShouldTrackNesting()
        {
            // Arrange
            var context = new WorkflowContext();

            // Act - 模拟嵌套调用
            context.PushCallInfo(new SubroutineCallInfo { NodeId = "node-1", SubroutineId = "sub-1" });
            context.PushCallInfo(new SubroutineCallInfo { NodeId = "node-2", SubroutineId = "sub-2" });
            context.PushCallInfo(new SubroutineCallInfo { NodeId = "node-3", SubroutineId = "sub-3" });

            // Assert
            context.GetCurrentCallDepth().Should().Be(3);
            context.GetCurrentCallInfo().Should().NotBeNull();
            context.GetCurrentCallInfo()!.SubroutineId.Should().Be("sub-3");
        }

        #endregion

        #region 节点状态追踪测试

        [Fact]
        public void WorkflowContext_UpdateNodeStatus_ShouldTrackProgress()
        {
            // Arrange
            var context = new WorkflowContext();

            // Act
            context.UpdateNodeStatus("node-1", NodeStatus.Pending);
            context.UpdateNodeStatus("node-1", NodeStatus.Running);
            context.UpdateNodeStatus("node-1", NodeStatus.Completed);

            // Assert
            context.NodeStates["node-1"].Status.Should().Be(NodeStatus.Completed);
        }

        [Fact]
        public void WorkflowContext_GetStatistics_ShouldProvideExecutionSummary()
        {
            // Arrange
            var context = new WorkflowContext();
            context.UpdateNodeStatus("node-1", NodeStatus.Completed);
            context.UpdateNodeStatus("node-2", NodeStatus.Completed);
            context.UpdateNodeStatus("node-3", NodeStatus.Failed);
            context.AddLog("测试日志1", LogLevel.Info);
            context.AddLog("测试日志2", LogLevel.Warning);

            // Act
            var stats = context.GetStatistics();

            // Assert
            stats.TotalNodes.Should().Be(3);
            stats.CompletedNodes.Should().Be(2);
            stats.FailedNodes.Should().Be(1);
            stats.TotalLogs.Should().Be(2);
        }

        #endregion
    }
}
