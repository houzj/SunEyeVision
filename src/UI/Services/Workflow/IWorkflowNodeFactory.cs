using NodeModel = SunEyeVision.UI.Models.WorkflowNode;

namespace SunEyeVision.UI.Services.Workflow
{
    /// <summary>
    /// 工作流节点工厂接收?
    /// </summary>
    public interface IWorkflowNodeFactory
    {
        /// <summary>
        /// 创建节点
        /// </summary>
        /// <param name="algorithmType">算法类型</param>
        /// <param name="name">节点名称（可选）</param>
        /// <param name="workflowId">工作流ID（可选）</param>
        /// <returns>创建的工作流节点</returns>
        NodeModel CreateNode(string algorithmType, string? name = null, string? workflowId = null);
    }
}

