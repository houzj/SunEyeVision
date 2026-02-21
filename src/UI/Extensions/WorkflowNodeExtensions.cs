using SunEyeVision.Core.Models;

namespace SunEyeVision.UI.Extensions
{
    /// <summary>
    /// WorkflowNode 扩展方法
    /// </summary>
    public static class WorkflowNodeExtensions
    {
        /// <summary>
        /// 将UI层的WorkflowNode转换为Workflow层的WorkflowNode
        /// </summary>
        public static Workflow.WorkflowNode ToWorkflowNode(this Models.WorkflowNode uiNode)
        {
            // 工具插件创建的节点始终为 AlgorithmNode
            // 控制节点由专门的 UI 操作创建
            var workflowNode = new Workflow.WorkflowNode(uiNode.Id, uiNode.Name, NodeType.Algorithm);
            workflowNode.AlgorithmType = uiNode.AlgorithmType;
            return workflowNode;
        }
    }
}
