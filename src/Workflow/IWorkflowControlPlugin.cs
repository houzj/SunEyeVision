using System.Collections.Generic;
using System.Threading.Tasks;
using SunEyeVision.Plugin.SDK.Metadata;

namespace SunEyeVision.Workflow
{
    /// <summary>
    /// 工作流控制插件接口
    /// </summary>
    public interface IWorkflowControlPlugin
    {
        /// <summary>
        /// 获取插件元数据
        /// </summary>
        ToolMetadata GetMetadata();

        /// <summary>
        /// 获取工作流控制节点列表
        /// </summary>
        /// <returns>工作流控制节点列表</returns>
        List<WorkflowControlNode> GetWorkflowControlNodes();

        /// <summary>
        /// 创建子程序节点
        /// </summary>
        /// <param name="name">节点名称</param>
        /// <param name="workflowId">子程序工作流ID</param>
        /// <returns>子程序节点实例</returns>
        SubroutineNode CreateSubroutineNode(string name, string workflowId);

        /// <summary>
        /// 创建条件判断节点
        /// </summary>
        /// <param name="name">节点名称</param>
        /// <param name="conditionExpression">条件表达式</param>
        /// <returns>条件判断节点实例</returns>
        ConditionNode CreateConditionNode(string name, string conditionExpression);

        /// <summary>
        /// 执行子程序
        /// </summary>
        /// <param name="node">子程序节点</param>
        /// <param name="context">工作流执行上下文</param>
        /// <returns>执行结果</returns>
        Task<ExecutionResult> ExecuteSubroutine(
            SubroutineNode node,
            WorkflowContext context);

        /// <summary>
        /// 评估条件表达式
        /// </summary>
        /// <param name="node">条件判断节点</param>
        /// <param name="context">工作流执行上下文</param>
        /// <returns>条件是否成立</returns>
        bool EvaluateCondition(
            ConditionNode node,
            WorkflowContext context);
    }
}
