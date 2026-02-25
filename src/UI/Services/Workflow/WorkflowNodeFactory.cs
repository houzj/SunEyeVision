using SunEyeVision.UI.Adapters;
using SunEyeVision.UI.Services.Canvas;
using SunEyeVision.UI.Services.Interaction;
using SunEyeVision.UI.Services.Workflow;
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
        private static DateTime _lastLogTime = DateTime.Now;

        /// <summary>
        /// 带时间戳的调试日志
        /// </summary>
        private static void LogTimestamp(string tag, string message)
        {
            var now = DateTime.Now;
            var elapsed = (now - _lastLogTime).TotalMilliseconds;
            _lastLogTime = now;
            System.Diagnostics.Debug.WriteLine($"[{now:HH:mm:ss.fff}] [+{elapsed:F0}ms] [{tag}] {message}");
        }

        public WorkflowNodeFactory(INodeSequenceManager sequenceManager, INodeDisplayAdapter displayAdapter)
        {
            _sequenceManager = sequenceManager ?? throw new ArgumentNullException(nameof(sequenceManager));
            _displayAdapter = displayAdapter ?? throw new ArgumentNullException(nameof(displayAdapter));
        }

        public NodeModel CreateNode(string algorithmType, string? name = null, string? workflowId = null)
        {
            _lastLogTime = DateTime.Now; // 重置计时器
            LogTimestamp("NodeFactory", $"▶ CreateNode 开始: algorithmType={algorithmType}");

            if (string.IsNullOrWhiteSpace(algorithmType))
            {
                throw new ArgumentException("Algorithm type cannot be null or empty.", nameof(algorithmType));
            }

            string workflowIdSafe = workflowId ?? Guid.NewGuid().ToString();

            // 获取序号
            var startTime = DateTime.Now;
            int localIndex = _sequenceManager.GetNextLocalIndex(workflowIdSafe, algorithmType);
            int globalIndex = _sequenceManager.GetNextGlobalIndex();
            LogTimestamp("NodeFactory", $"获取序号耗时: {(DateTime.Now - startTime).TotalMilliseconds:F0}ms, local={localIndex}, global={globalIndex}");

            // 创建节点实例
            startTime = DateTime.Now;
            var node = new WorkflowModel.WorkflowNode(
                Guid.NewGuid().ToString(),
                name ?? algorithmType,
                algorithmType,
                localIndex,
                globalIndex
            );
            LogTimestamp("NodeFactory", $"创建 WorkflowNode 耗时: {(DateTime.Now - startTime).TotalMilliseconds:F0}ms");

            // 使用显示适配器设置图标
            startTime = DateTime.Now;
            if (_displayAdapter != null)
            {
                node.NodeTypeIcon = _displayAdapter.GetIcon(node);
            }
            LogTimestamp("NodeFactory", $"设置图标耗时: {(DateTime.Now - startTime).TotalMilliseconds:F0}ms");

            LogTimestamp("NodeFactory", $"✓ CreateNode 完成: Id={node.Id}");
            return node;
        }
    }
}
