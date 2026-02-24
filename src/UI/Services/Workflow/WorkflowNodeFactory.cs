using SunEyeVision.UI.Adapters;
using SunEyeVision.UI.Services.Canvas;
using SunEyeVision.UI.Services.Interaction;
using SunEyeVision.UI.Services.Workflow;
using NodeModel = SunEyeVision.UI.Models.WorkflowNode;
using WorkflowModel = SunEyeVision.UI.Models;

namespace SunEyeVision.UI.Services.Workflow
{
    /// <summary>
    /// 工作流节点工厂实�?
    /// </summary>
    public class WorkflowNodeFactory : IWorkflowNodeFactory
    {
        private readonly INodeSequenceManager _sequenceManager;
        private readonly INodeDisplayAdapter _displayAdapter;

        public WorkflowNodeFactory(INodeSequenceManager sequenceManager, INodeDisplayAdapter displayAdapter)
        {
            System.Diagnostics.Debug.WriteLine($"[NodeFactory] �?构造函数调�?);
            System.Diagnostics.Debug.WriteLine($"[NodeFactory]   - sequenceManager: {(sequenceManager != null ? "存在" : "null")}");
            System.Diagnostics.Debug.WriteLine($"[NodeFactory]   - displayAdapter: {(displayAdapter != null ? "存在" : "null")}");

            _sequenceManager = sequenceManager ?? throw new ArgumentNullException(nameof(sequenceManager));
            _displayAdapter = displayAdapter ?? throw new ArgumentNullException(nameof(displayAdapter));
        }

        public NodeModel CreateNode(string algorithmType, string? name = null, string? workflowId = null)
        {
            System.Diagnostics.Debug.WriteLine($"[NodeFactory.CreateNode] �?开始创建节�?);
            System.Diagnostics.Debug.WriteLine($"[NodeFactory.CreateNode]   - algorithmType: {algorithmType}");
            System.Diagnostics.Debug.WriteLine($"[NodeFactory.CreateNode]   - name: {name ?? "(null)"}");
            System.Diagnostics.Debug.WriteLine($"[NodeFactory.CreateNode]   - workflowId: {workflowId ?? "(null)"}");

            if (string.IsNullOrWhiteSpace(algorithmType))
            {
                System.Diagnostics.Debug.WriteLine($"[NodeFactory.CreateNode] �?algorithmType 为空!");
                throw new ArgumentException("Algorithm type cannot be null or empty.", nameof(algorithmType));
            }

            string workflowIdSafe = workflowId ?? Guid.NewGuid().ToString();
            System.Diagnostics.Debug.WriteLine($"[NodeFactory.CreateNode]   - workflowIdSafe: {workflowIdSafe}");

            System.Diagnostics.Debug.WriteLine($"[NodeFactory.CreateNode] �?获取序号...");
            System.Diagnostics.Debug.WriteLine($"[NodeFactory.CreateNode]   - _sequenceManager: {(_sequenceManager != null ? "存在" : "null")}");

            if (_sequenceManager == null)
            {
                System.Diagnostics.Debug.WriteLine($"[NodeFactory.CreateNode] �?_sequenceManager �?null!");
                throw new InvalidOperationException("SequenceManager is not initialized");
            }

            int localIndex = _sequenceManager.GetNextLocalIndex(workflowIdSafe, algorithmType);
            int globalIndex = _sequenceManager.GetNextGlobalIndex();
            System.Diagnostics.Debug.WriteLine($"[NodeFactory.CreateNode]   - localIndex: {localIndex}");
            System.Diagnostics.Debug.WriteLine($"[NodeFactory.CreateNode]   - globalIndex: {globalIndex}");

            System.Diagnostics.Debug.WriteLine($"[NodeFactory.CreateNode] �?创建 WorkflowNode 实例...");
            var node = new WorkflowModel.WorkflowNode(
                Guid.NewGuid().ToString(),
                name ?? algorithmType,
                algorithmType,
                localIndex,
                globalIndex
            );
            System.Diagnostics.Debug.WriteLine($"[NodeFactory.CreateNode] �?WorkflowNode 实例创建成功: Id={node.Id}");

            // 使用显示适配器设置图�?
            System.Diagnostics.Debug.WriteLine($"[NodeFactory.CreateNode] �?设置节点图标...");
            System.Diagnostics.Debug.WriteLine($"[NodeFactory.CreateNode]   - _displayAdapter: {(_displayAdapter != null ? "存在" : "null")}");

            if (_displayAdapter != null)
            {
                node.NodeTypeIcon = _displayAdapter.GetIcon(node);
                System.Diagnostics.Debug.WriteLine($"[NodeFactory.CreateNode] �?图标设置成功");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[NodeFactory.CreateNode] �?_displayAdapter �?null，跳过图标设�?);
            }

            System.Diagnostics.Debug.WriteLine($"[NodeFactory.CreateNode] ✓✓�?节点创建完成 ✓✓�?);
            return node;
        }
    }
}

