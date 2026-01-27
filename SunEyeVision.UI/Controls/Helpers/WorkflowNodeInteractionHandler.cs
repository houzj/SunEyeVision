using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;
using SunEyeVision.UI.Models;
using SunEyeVision.UI.ViewModels;

namespace SunEyeVision.UI.Controls.Helpers
{
    /// <summary>
    /// 工作流节点交互处理器
    /// 负责节点的鼠标事件处理、拖拽、选择等交互
    /// </summary>
    public class WorkflowNodeInteractionHandler
    {
        private readonly WorkflowCanvasControl _canvasControl;
        private readonly MainWindowViewModel? _viewModel;
        private readonly WorkflowConnectionManager _connectionManager;

        // 节点拖拽相关
        private bool _isDragging;
        private WorkflowNode? _draggedNode;
        private System.Windows.Point _startDragPosition;
        private System.Windows.Point _initialNodePosition;

        // 节点拖拽性能优化
        private DateTime _lastConnectionUpdateTime = DateTime.MinValue;
        private const int ConnectionUpdateIntervalMs = 50; // 连接线更新间隔（毫秒）

        // 多选节点拖拽相关
        private System.Windows.Point[]? _selectedNodesInitialPositions;

        // 连接模式相关
        private WorkflowNode? _connectionSourceNode = null;

        public WorkflowNodeInteractionHandler(
            WorkflowCanvasControl canvasControl, 
            MainWindowViewModel? viewModel,
            WorkflowConnectionManager connectionManager)
        {
            _canvasControl = canvasControl;
            _viewModel = viewModel;
            _connectionManager = connectionManager;
        }

        /// <summary>
        /// 节点鼠标进入事件（显示连接点）
        /// </summary>
        public void Node_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Border border && border.Tag is WorkflowNode node)
            {
                SetPortsVisibility(border, true);
            }
        }

        /// <summary>
        /// 节点鼠标离开事件（隐藏连接点）
        /// </summary>
        public void Node_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Border border && border.Tag is WorkflowNode node)
            {
                SetPortsVisibility(border, false);
            }
        }

        /// <summary>
        /// 连接点鼠标进入事件
        /// </summary>
        public void Ellipse_MouseEnter(object sender, MouseEventArgs e)
        {
            // 连接点样式已通过 XAML 处理
        }

        /// <summary>
        /// 连接点鼠标离开事件
        /// </summary>
        public void Ellipse_MouseLeave(object sender, MouseEventArgs e)
        {
            // 连接点样式已通过 XAML 处理
        }

        /// <summary>
        /// 设置单个节点的连接点可见性
        /// </summary>
        public void SetPortsVisibility(Border border, bool isVisible)
        {
            var ellipses = WorkflowVisualHelper.FindAllVisualChildren<Ellipse>(border);
            foreach (var ellipse in ellipses)
            {
                var ellipseName = ellipse.Name ?? "";
                if (ellipseName.Contains("Port"))
                {
                    ellipse.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
                }
            }
        }

        /// <summary>
        /// 节点鼠标左键按下 - 开始拖拽
        /// </summary>
        public void Node_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Border border || border.Tag is not WorkflowNode node)
                return;

            // 双击事件：打开调试窗口
            if (e.ClickCount == 2)
            {
                if (_viewModel?.WorkflowTabViewModel.SelectedTab != null)
                {
                    foreach (var n in _viewModel.WorkflowTabViewModel.SelectedTab.WorkflowNodes)
                    {
                        n.IsSelected = (n == node);
                    }
                }
                _viewModel.SelectedNode = node;

                // 打开调试窗口
                _viewModel.OpenDebugWindowCommand.Execute(node);
                e.Handled = true;
                return;
            }

            // 检查是否按住 Shift 或 Ctrl 键（多选模式）
            bool isMultiSelect = (Keyboard.Modifiers & ModifierKeys.Shift) != 0 ||
                               (Keyboard.Modifiers & ModifierKeys.Control) != 0;

            // 如果节点未被选中，且不是多选模式，则只选中当前节点
            if (!node.IsSelected && !isMultiSelect)
            {
                ClearAllSelections();
                node.IsSelected = true;
            }
            // 如果是多选模式，切换选中状态
            else if (isMultiSelect)
            {
                node.IsSelected = !node.IsSelected;
            }

            _viewModel.SelectedNode = node;

            // 记录所有选中节点的初始位置
            RecordSelectedNodesPositions();

            // 单击事件：拖拽准备
            _isDragging = true;
            _draggedNode = node;
            _initialNodePosition = node.Position;
            _startDragPosition = e.GetPosition(_canvasControl.WorkflowCanvas);

            border.CaptureMouse();

            // 阻止事件冒泡到 Canvas，避免触发框选
            e.Handled = true;
        }

        /// <summary>
        /// 节点鼠标左键释放 - 结束拖拽
        /// </summary>
        public void Node_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_isDragging)
                return;

            _isDragging = false;
            _draggedNode = null;

            if (sender is Border border)
            {
                border.ReleaseMouseCapture();
            }

            e.Handled = true;
        }

        /// <summary>
        /// 节点鼠标移动 - 拖拽节点
        /// </summary>
        public void Node_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDragging || _draggedNode == null)
                return;

            var currentPos = e.GetPosition(_canvasControl.WorkflowCanvas);
            var delta = new System.Windows.Point(
                currentPos.X - _startDragPosition.X,
                currentPos.Y - _startDragPosition.Y);

            // 获取当前选中的所有节点
            var selectedNodes = _viewModel?.WorkflowTabViewModel.SelectedTab?.WorkflowNodes
                .Where(n => n.IsSelected)
                .ToList();

            if (selectedNodes == null || selectedNodes.Count == 0)
                return;

            // 移动所有选中的节点
            for (int i = 0; i < selectedNodes.Count; i++)
            {
                var node = selectedNodes[i];
                if (_selectedNodesInitialPositions != null && i < _selectedNodesInitialPositions.Length)
                {
                    node.Position = new System.Windows.Point(
                        _selectedNodesInitialPositions[i].X + delta.X,
                        _selectedNodesInitialPositions[i].Y + delta.Y);
                }
            }

            // 性能优化：限制连接线更新频率
            var now = DateTime.Now;
            if ((now - _lastConnectionUpdateTime).TotalMilliseconds >= ConnectionUpdateIntervalMs)
            {
                _connectionManager.RefreshAllConnectionPaths();
                _lastConnectionUpdateTime = now;
            }

            e.Handled = true;
        }

        /// <summary>
        /// 节点点击事件 - 用于连接模式
        /// </summary>
        public void Node_ClickForConnection(object sender, RoutedEventArgs e)
        {
            if (sender is not Border border || border.Tag is not WorkflowNode targetNode)
                return;

            var selectedTab = _viewModel?.WorkflowTabViewModel.SelectedTab;
            if (selectedTab == null)
                return;

            // 检查是否在连接模式
            if (_connectionSourceNode == null)
            {
                // 进入连接模式
                _connectionSourceNode = targetNode;
                _viewModel!.StatusText = $"请选择目标节点进行连接，从: {targetNode.Name}";
            }
            else
            {
                // 检查是否是同一个节点
                if (_connectionSourceNode == targetNode)
                {
                    _viewModel!.StatusText = "无法连接到同一个节点";
                    _viewModel.AddLog("[Connection] ❌ 无法连接到同一个节点");
                    _connectionSourceNode = null;
                    return;
                }

                // 检查连接是否已存在
                var existingConnection = selectedTab.WorkflowConnections.FirstOrDefault(c =>
                    c.SourceNodeId == _connectionSourceNode!.Id && c.TargetNodeId == targetNode.Id);

                if (existingConnection != null)
                {
                    _viewModel!.StatusText = "连接已存在";
                    _connectionSourceNode = null;
                    return;
                }

                // 创建新连接
                _viewModel?.AddLog($"[Connection] 创建连接: {_connectionSourceNode.Name} -> {targetNode.Name}");
                _connectionManager.CreateConnection(_connectionSourceNode, targetNode, null);

                // 退出连接模式
                _connectionSourceNode = null;
            }
        }

        /// <summary>
        /// 清除所有节点的选中状态
        /// </summary>
        private void ClearAllSelections()
        {
            if (_viewModel?.WorkflowTabViewModel.SelectedTab != null)
            {
                foreach (var node in _viewModel.WorkflowTabViewModel.SelectedTab.WorkflowNodes)
                {
                    node.IsSelected = false;
                }
            }
        }

        /// <summary>
        /// 记录选中节点的初始位置
        /// </summary>
        private void RecordSelectedNodesPositions()
        {
            if (_viewModel?.WorkflowTabViewModel.SelectedTab == null) return;

            var selectedNodes = _viewModel.WorkflowTabViewModel.SelectedTab.WorkflowNodes
                .Where(n => n.IsSelected)
                .ToList();

            _selectedNodesInitialPositions = selectedNodes
                .Select(n => n.Position)
                .ToArray();
        }
    }
}
