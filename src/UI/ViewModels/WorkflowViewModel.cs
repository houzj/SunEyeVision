using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using SunEyeVision.UI.Models;

namespace SunEyeVision.UI.ViewModels
{
    /// <summary>
    /// 工作流画布视图模�?
    /// </summary>
    public class WorkflowViewModel : ViewModelBase
    {
        private WorkflowNode? _selectedNode;
        private WorkflowConnection? _selectedConnection;

        public ObservableCollection<WorkflowNode> Nodes { get; }

        public WorkflowNode? SelectedNode
        {
            get => _selectedNode;
            set
            {
                if (SetProperty(ref _selectedNode, value))
                {
                    UpdateSelection();
                }
            }
        }

        public WorkflowConnection? SelectedConnection
        {
            get => _selectedConnection;
            set => SetProperty(ref _selectedConnection, value);
        }

        public ICommand AddNodeCommand { get; }
        public ICommand RemoveNodeCommand { get; }
        public ICommand SelectNodeCommand { get; }
        public ICommand ClearSelectionCommand { get; }
        public ICommand DeleteSelectedCommand { get; }

        public event Action<WorkflowNode?>? SelectedNodeChanged;

        // 连接模式相关
        public bool IsInConnectionMode { get; private set; }
        public WorkflowNode? ConnectionSourceNode { get; private set; }

        public ObservableCollection<WorkflowConnection> Connections { get; }

        public WorkflowViewModel()
        {
            Nodes = new ObservableCollection<WorkflowNode>();
            Connections = new ObservableCollection<WorkflowConnection>();

            AddNodeCommand = new RelayCommand<string>(ExecuteAddNode);
            RemoveNodeCommand = new RelayCommand<WorkflowNode>(ExecuteRemoveNode);
            SelectNodeCommand = new RelayCommand<WorkflowNode>(node => SelectedNode = node);
            ClearSelectionCommand = new RelayCommand(ExecuteClearSelection);
            DeleteSelectedCommand = new RelayCommand(ExecuteDeleteSelected, () => SelectedNode != null || SelectedConnection != null);

            InitializeSampleWorkflow();
        }

        private void InitializeSampleWorkflow()
        {
            var node1 = new WorkflowNode("node1", "图像输入", "Input");
            node1.Position = new Point(50, 50);

            var node2 = new WorkflowNode("node2", "高斯模糊", "Preprocess");
            node2.Position = new Point(250, 50);

            var node3 = new WorkflowNode("node3", "边缘检�?, "Detection");
            node3.Position = new Point(450, 50);

            var node4 = new WorkflowNode("node4", "结果输出", "Output");
            node4.Position = new Point(650, 50);

            Nodes.Add(node1);
            Nodes.Add(node2);
            Nodes.Add(node3);
            Nodes.Add(node4);

            var conn1 = new WorkflowConnection("conn1", "node1", "node2");
            var conn2 = new WorkflowConnection("conn2", "node2", "node3");
            var conn3 = new WorkflowConnection("conn3", "node3", "node4");

            // 为初始连接设置位�?
            conn1.SourcePosition = new Point(190, 95);
            conn1.TargetPosition = new Point(250, 95);

            conn2.SourcePosition = new Point(390, 95);
            conn2.TargetPosition = new Point(450, 95);

            conn3.SourcePosition = new Point(590, 95);
            conn3.TargetPosition = new Point(650, 95);

            Connections.Add(conn1);
            Connections.Add(conn2);
            Connections.Add(conn3);
        }

        private void ExecuteAddNode(string? type)
        {
            if (string.IsNullOrEmpty(type)) return;

            var id = $"node{Nodes.Count + 1}";
            var name = type switch
            {
                "Input" => "图像输入",
                "Preprocess" => "高斯模糊",
                "Detection" => "边缘检�?,
                "Output" => "结果输出",
                _ => "新节�?
            };

            var newNode = new WorkflowNode(id, name, type);
            newNode.Position = new Point(100 + Nodes.Count * 200, 100);
            Nodes.Add(newNode);
            SelectedNode = newNode;
        }

        private void ExecuteRemoveNode(WorkflowNode? node)
        {
            if (node == null) return;

            var connectionsToRemove = Connections
                .Where(c => c.SourceNodeId == node.Id || c.TargetNodeId == node.Id)
                .ToList();

            foreach (var conn in connectionsToRemove)
            {
                Connections.Remove(conn);
            }

            Nodes.Remove(node);

            if (SelectedNode == node)
            {
                SelectedNode = null;
            }
        }

        private void ExecuteClearSelection()
        {
            SelectedNode = null;
            SelectedConnection = null;

            foreach (var node in Nodes)
            {
                node.IsSelected = false;
            }
        }

        private void ExecuteDeleteSelected()
        {
            if (SelectedNode != null)
            {
                ExecuteRemoveNode(SelectedNode);
            }
            else if (SelectedConnection != null)
            {
                Connections.Remove(SelectedConnection);
                SelectedConnection = null;
            }
        }

        private void UpdateSelection()
        {
            foreach (var node in Nodes)
            {
                node.IsSelected = (node == SelectedNode);
            }
            SelectedNodeChanged?.Invoke(SelectedNode);
        }

        #region 连接模式

        /// <summary>
        /// 执行连接节点命令
        /// </summary>
        public void ExecuteConnectNodes()
        {
            if (SelectedNode != null)
            {
                IsInConnectionMode = true;
                ConnectionSourceNode = SelectedNode;
            }
        }

        /// <summary>
        /// 尝试连接到目标节�?
        /// </summary>
        public bool TryConnectNode(WorkflowNode targetNode)
        {
            if (ConnectionSourceNode == null || targetNode == null)
                return false;

            // 检查是否是同一个节�?
            if (ConnectionSourceNode == targetNode)
                return false;

            // 检查连接是否已存在
            var existingConnection = Connections.FirstOrDefault(c =>
                c.SourceNodeId == ConnectionSourceNode.Id && c.TargetNodeId == targetNode.Id);

            if (existingConnection != null)
                return false;

            // 创建新连�?
            var connectionId = $"conn_{Guid.NewGuid().ToString("N")[..8]}";
            var newConnection = new WorkflowConnection(connectionId, ConnectionSourceNode.Id, targetNode.Id);

            // 计算连接点位置（节点右中心到左中心）
            var sourcePos = new Point(
                ConnectionSourceNode.Position.X + 140,
                ConnectionSourceNode.Position.Y + 35
            );
            var targetPos = new Point(
                targetNode.Position.X,
                targetNode.Position.Y + 35
            );

            newConnection.SourcePosition = sourcePos;
            newConnection.TargetPosition = targetPos;

            Connections.Add(newConnection);

            // 退出连接模�?
            IsInConnectionMode = false;
            ConnectionSourceNode = null;

            return true;
        }

        #endregion
    }
}
