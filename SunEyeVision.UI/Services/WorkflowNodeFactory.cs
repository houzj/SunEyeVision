using SunEyeVision.UI.Adapters;
using SunEyeVision.UI.Interfaces;
using NodeModel = SunEyeVision.UI.Models.WorkflowNode;
using WorkflowModel = SunEyeVision.UI.Models;

namespace SunEyeVision.UI.Services
{
    /// <summary>
    /// 工作流节点工厂实现
    /// </summary>
    public class WorkflowNodeFactory : IWorkflowNodeFactory
    {
        private readonly INodeSequenceManager _sequenceManager;
        private readonly INodeDisplayAdapter _displayAdapter;

        public WorkflowNodeFactory(INodeSequenceManager sequenceManager, INodeDisplayAdapter displayAdapter)
        {
            _sequenceManager = sequenceManager ?? throw new ArgumentNullException(nameof(sequenceManager));
            _displayAdapter = displayAdapter ?? throw new ArgumentNullException(nameof(displayAdapter));
        }

        public NodeModel CreateNode(string algorithmType, string? name = null, string? workflowId = null)
        {
            if (string.IsNullOrWhiteSpace(algorithmType))
            {
                throw new ArgumentException("Algorithm type cannot be null or empty.", nameof(algorithmType));
            }

            string workflowIdSafe = workflowId ?? Guid.NewGuid().ToString();

            int localIndex = _sequenceManager.GetNextLocalIndex(workflowIdSafe, algorithmType);
            int globalIndex = _sequenceManager.GetNextGlobalIndex();

            var node = new WorkflowModel.WorkflowNode(
                Guid.NewGuid().ToString(),
                name ?? algorithmType,
                algorithmType,
                localIndex,
                globalIndex
            );

            // 使用显示适配器设置图标
            node.NodeTypeIcon = _displayAdapter.GetIcon(node);

            return node;
        }
    }
}

