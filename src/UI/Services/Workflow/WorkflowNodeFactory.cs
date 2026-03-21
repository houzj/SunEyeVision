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

            // 自动添加序号,格式:"工具名称 局部序号"
            string nodeName = name ?? $"{algorithmType} {localIndex}";

            var node = new WorkflowModel.WorkflowNode(
                Guid.NewGuid().ToString(),
                nodeName,
                algorithmType,
                localIndex,
                globalIndex
            );

            // 初始化节点参数
            InitializeNodeParameters(node, algorithmType);

            // 使用 NodeDisplayAdapterFactory 根据算法类型获取适配器
            var adapter = NodeDisplayAdapterFactory.GetAdapter(algorithmType);
            if (adapter != null)
            {
                node.NodeTypeIcon = adapter.GetIcon(node);
            }

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
                    node.ParametersTypeName = defaultParams.GetType().AssemblyQualifiedName;
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
