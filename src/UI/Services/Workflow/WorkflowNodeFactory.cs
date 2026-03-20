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

            // 获取序号
            int localIndex = _sequenceManager.GetNextLocalIndex(workflowIdSafe, algorithmType);
            int globalIndex = _sequenceManager.GetNextGlobalIndex();

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

            // 使用显示适配器设置图标
            if (_displayAdapter != null)
            {
                node.NodeTypeIcon = _displayAdapter.GetIcon(node);
            }

            return node;
        }

        /// <summary>
        /// 初始化节点参数（基于 AlgorithmType 创建默认参数）
        /// </summary>
        private void InitializeNodeParameters(WorkflowModel.WorkflowNode node, string algorithmType)
        {
            try
            {
                // 从 ToolRegistry 创建默认参数
                var defaultParams = ToolRegistry.CreateParameters(algorithmType);

                // 将 ToolParameters 转换为 Dictionary<string, object>
                if (defaultParams != null && defaultParams.GetType() != typeof(GenericToolParameters))
                {
                    // 使用 ToSerializableDictionary 方法
                    var parametersDict = defaultParams.ToSerializableDictionary();

                    // 如果返回的字典包含实际的参数，则使用它
                    if (parametersDict != null && parametersDict.Count > 1) // 至少包含 "$type"
                    {
                        node.Parameters = parametersDict;
                    }
                    else
                    {
                        // 如果只有 "$type"，则使用 GenericToolParameters
                        node.Parameters = new Dictionary<string, object>
                        {
                            ["$type"] = "Generic",
                            ["Version"] = 1
                        };
                    }
                }
                else
                {
                    // GenericToolParameters 或参数类型未知，使用默认值
                    node.Parameters = new Dictionary<string, object>
                    {
                        ["$type"] = "Generic",
                        ["Version"] = 1
                    };
                }
            }
            catch (Exception ex)
            {
                // 发生异常时，使用空字典（降级处理）
                node.Parameters = new Dictionary<string, object>();
            }
        }
    }
}
