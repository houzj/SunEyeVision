using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using SunEyeVision.UI.Models;

namespace SunEyeVision.UI.ViewModels
{
    /// <summary>
    /// 工作流视图模型
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
        }

        private void ExecuteAddNode(string? type)
        {
            if (string.IsNullOrEmpty(type)) return;

            var id = $"node{Nodes.Count + 1}";
            var dispName = type switch
            {
                "Input" => "图像输入",
                "Preprocess" => "高斯模糊",
                "Detection" => "边缘检测",
                "Output" => "输出",
                _ => "新节点"
            };

            var newNode = new WorkflowNode(id, $"{Nodes.Count + 1} {dispName}", dispName, type);
            newNode.Position = new Point(100 + Nodes.Count * 200, 100);
            AddToCollection(Nodes, newNode);
            SelectedNode = newNode;
        }

        private void ExecuteRemoveNode(WorkflowNode? node)
        {
            if (node == null) return;

            var connectionsToRemove = Connections
                .Where(c => c.SourceNodeId == node.Id || c.TargetNodeId == node.Id)
                .ToList();

            // 使用线程安全方法批量移除连接
            RemoveRangeFromCollection(Connections, connectionsToRemove);

            // 移除节点
            RemoveFromCollection(Nodes, node);

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
                var connectionToRemove = SelectedConnection;
                SelectedConnection = null;

                // 使用线程安全方法移除连接
                RemoveFromCollection(Connections, connectionToRemove);
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
        /// 开始连接节点
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
        /// 尝试连接到目标节点
        /// </summary>
        public bool TryConnectNode(WorkflowNode targetNode)
        {
            if (ConnectionSourceNode == null || targetNode == null)
                return false;

            // 不能连接到同一个节点
            if (ConnectionSourceNode == targetNode)
                return false;

            // 连接是否已存在
            var existingConnection = Connections.FirstOrDefault(c =>
                c.SourceNodeId == ConnectionSourceNode.Id && c.TargetNodeId == targetNode.Id);

            if (existingConnection != null)
                return false;

            // 创建新连接
            var connectionId = $"conn_{Guid.NewGuid().ToString("N")[..8]}";
            var newConnection = new WorkflowConnection(connectionId, ConnectionSourceNode.Id, targetNode.Id);

            // 计算连接位置坐标
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

            AddToCollection(Connections, newConnection);

            // 退出连接模式
            IsInConnectionMode = false;
            ConnectionSourceNode = null;

            return true;
        }

        #endregion
    }
}
