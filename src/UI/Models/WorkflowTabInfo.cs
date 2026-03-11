using System.Collections.ObjectModel;
using SunEyeVision.Workflow;

namespace SunEyeVision.UI.Models
{
    /// <summary>
    /// 工作流标签页信息
    /// </summary>
    public class WorkflowTabInfo
    {
        /// <summary>
        /// 工作流 ID
        /// </summary>
        public string WorkflowId { get; set; } = string.Empty;

        /// <summary>
        /// 工作流名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 工作流文件路径
        /// </summary>
        public string? FilePath { get; set; }

        /// <summary>
        /// 是否已修改
        /// </summary>
        public bool IsModified { get; set; }

        /// <summary>
        /// 工作流节点列表（UI 层）
        /// </summary>
        public ObservableCollection<WorkflowNode> WorkflowNodes { get; set; } = new();

        /// <summary>
        /// 工作流连接列表（UI 层）
        /// </summary>
        public ObservableCollection<WorkflowConnection> WorkflowConnections { get; set; } = new();

        /// <summary>
        /// 底层工作流对象
        /// </summary>
        public SunEyeVision.Workflow.Workflow? Workflow { get; set; }
    }
}
