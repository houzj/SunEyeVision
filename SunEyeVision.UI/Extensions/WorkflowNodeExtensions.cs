using SunEyeVision.Models;

namespace SunEyeVision.UI
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
            // 从AlgorithmType或Name推断NodeType
            NodeType nodeType = NodeType.Algorithm;
            if (!string.IsNullOrEmpty(uiNode.AlgorithmType))
            {
                if (uiNode.AlgorithmType.Contains("Subroutine") || uiNode.AlgorithmType.Contains("子程序"))
                    nodeType = NodeType.Subroutine;
                else if (uiNode.AlgorithmType.Contains("Condition") || uiNode.AlgorithmType.Contains("条件"))
                    nodeType = NodeType.Condition;
                else if (uiNode.AlgorithmType.Contains("Start") || uiNode.AlgorithmType.Contains("开始"))
                    nodeType = NodeType.Start;
            }

            var workflowNode = new Workflow.WorkflowNode(uiNode.Id, uiNode.Name, nodeType);
            workflowNode.AlgorithmType = uiNode.AlgorithmType;
            return workflowNode;
        }
    }
}
