using SunEyeVision.UI.Adapters;
using SunEyeVision.UI.Services.Canvas;
using SunEyeVision.UI.Services.Interaction;
using SunEyeVision.Plugin.Infrastructure.Managers.Tool;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using NodeModel = SunEyeVision.UI.Models.WorkflowNode;
using WorkflowModel = SunEyeVision.UI.Models;

namespace SunEyeVision.UI.Services.Workflow
{
    /// <summary>
    /// 工作流节点工厂实现
    /// </summary>
    public class WorkflowNodeFactory : IWorkflowNodeFactory
    {
        private readonly INodeSequenceManager _sequenceManager;

        public WorkflowNodeFactory(INodeSequenceManager sequenceManager, INodeDisplayAdapter displayAdapter)
        {
            _sequenceManager = sequenceManager ?? throw new ArgumentNullException(nameof(sequenceManager));
        }

        public NodeModel CreateNode(string algorithmType, string? name = null, string? workflowId = null)
        {
            if (string.IsNullOrWhiteSpace(algorithmType))
            {
                throw new ArgumentException("Algorithm type cannot be null or empty.", nameof(algorithmType));
            }

            string workflowIdSafe = workflowId ?? Guid.NewGuid().ToString();

            // 获取序号
            int localIndex = _sequenceManager.GetNextLocalIndex(workflowIdSafe, algorithmType);
            int globalIndex = _sequenceManager.GetNextGlobalIndex();

            return CreateIndexedNodeInternal(algorithmType, localIndex, name, workflowIdSafe, globalIndex);
        }

        public NodeModel CreateIndexedNode(string algorithmType, int localIndex, string? name = null, string? workflowId = null)
        {
            if (string.IsNullOrWhiteSpace(algorithmType))
            {
                throw new ArgumentException("Algorithm type cannot be null or empty.", nameof(algorithmType));
            }

            string workflowIdSafe = workflowId ?? Guid.NewGuid().ToString();
            int globalIndex = _sequenceManager.GetNextGlobalIndex();

            return CreateIndexedNodeInternal(algorithmType, localIndex, name, workflowIdSafe, globalIndex);
        }

        /// <summary>
        /// 创建节点的内部实现
        /// </summary>
        private NodeModel CreateIndexedNodeInternal(string algorithmType, int localIndex, string? name, string workflowId, int globalIndex)
        {
            if (string.IsNullOrWhiteSpace(algorithmType))
            {
                throw new ArgumentException("Algorithm type cannot be null or empty.", nameof(algorithmType));
            }

            // 获取工具元数据
            var metadata = ToolRegistry.GetToolMetadata(algorithmType);
            var displayName = metadata?.DisplayName ?? algorithmType;

            // 生成节点ID和名称
            string nodeId = _sequenceManager.GenerateNodeId(algorithmType, globalIndex, localIndex);
            string nodeName = name ?? _sequenceManager.GenerateNodeName(displayName, localIndex);

            var node = new WorkflowModel.WorkflowNode(
                nodeId,
                nodeName,
                algorithmType
            );

            // 初始化节点参数
            InitializeNodeParameters(node, algorithmType);

            return node;
        }

        /// <summary>
        /// 初始化节点参数（基于 AlgorithmType 创建默认参数）
        /// </summary>
        private void InitializeNodeParameters(NodeModel node, string algorithmType)
        {
            try
            {
                // 从 ToolRegistry 创建默认参数（直接返回 ToolParameters）
                var defaultParams = ToolRegistry.CreateParameters(algorithmType);

                if (defaultParams != null)
                {
                    // 直接使用 ToolParameters，不进行转换
                    node.Parameters = defaultParams;
                    // ParametersTypeName 已过时，不再使用
                }
                else
                {
                    // 创建失败时，使用 GenericToolParameters 作为降级处理
                    node.Parameters = new GenericToolParameters();
                }
            }
            catch (Exception ex)
            {
                // 发生异常时，使用 GenericToolParameters（降级处理）
                node.Parameters = new GenericToolParameters();
            }
        }
    }
}
