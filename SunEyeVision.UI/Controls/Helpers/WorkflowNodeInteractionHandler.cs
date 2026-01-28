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
        private Dictionary<WorkflowNode, System.Windows.Point>? _initialNodePositions;

        // 连接模式相关
        private WorkflowNode? _connectionSourceNode = null;
        private bool _isCreatingConnection = false;
        private WorkflowNode? _connectionStartNode = null;

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
            try
            {
                if (sender is not Border border || border.Tag is not WorkflowNode node)
                {
                    return;
                }

                // 双击事件：打开调试窗口
                if (e.ClickCount == 2)
                {
                    if (_canvasControl.CurrentWorkflowTab != null)
                    {
                        foreach (var n in _canvasControl.CurrentWorkflowTab.WorkflowNodes)
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

                // 阻�止事件冒泡到 Canvas，避免触发框选
                e.Handled = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Node_MouseLeftButtonDown] 异常: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 节点鼠标左键释放 - 结束拖拽
        /// </summary>
        public void Node_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (sender is not Border border || border.Tag is not WorkflowNode node)
                {
                    return;
                }

                // 如果正在创建连接模式，则处理连接创建
                if (_isCreatingConnection)
                {
                    HandleConnectionCreation(node);
                    _isCreatingConnection = false;
                    _connectionStartNode = null;
                    border.ReleaseMouseCapture();
                    e.Handled = true;
                    return;
                }

                // 如果正在拖拽，则结束拖拽
                if (node == _draggedNode && _isDragging)
                {
                    _isDragging = false;
                    _draggedNode = null;
                    border.ReleaseMouseCapture();
                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Node_MouseLeftButtonUp] 异常: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 节点鼠标移动 - 处理拖拽
        /// </summary>
        public void Node_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                if (!_isDragging || _draggedNode == null)
                {
                    return;
                }

                if (sender is not Border border || border.Tag is not WorkflowNode node)
                {
                    return;
                }

                // 获取当前鼠标位置
                Point currentPosition = e.GetPosition(_canvasControl.WorkflowCanvas);

                // 计算偏移量
                double offsetX = currentPosition.X - _startDragPosition.X;
                double offsetY = currentPosition.Y - _startDragPosition.Y;

                // 更新所有选中节点的位置
                if (_canvasControl.CurrentWorkflowTab != null)
                {
                    int index = 0;
                    foreach (var selectedNode in _canvasControl.CurrentWorkflowTab.WorkflowNodes.Where(n => n.IsSelected))
                    {
                        if (_selectedNodesInitialPositions != null && index < _selectedNodesInitialPositions.Length)
                        {
                            selectedNode.Position = new Point(
                                _selectedNodesInitialPositions[index].X + offsetX,
                                _selectedNodesInitialPositions[index].Y + offsetY);
                            index++;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Node_MouseMove] 异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 节点点击事件 - 用于连接模式
        /// </summary>
        public void Node_ClickForConnection(object sender, RoutedEventArgs e)
        {
            if (sender is not Border border || border.Tag is not WorkflowNode targetNode)
                return;

            var selectedTab = _canvasControl.CurrentWorkflowTab;
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
                    System.Diagnostics.Debug.WriteLine("[Connection] ❌ 无法连接到同一个节点");
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
                System.Diagnostics.Debug.WriteLine($"[Connection] 创建连接: {_connectionSourceNode.Name} -> {targetNode.Name}");
                _connectionManager.CreateConnection(_connectionSourceNode, targetNode, null);

                // 退出连接模式
                _connectionSourceNode = null;
            }
        }

        /// <summary>
        /// 节点点击事件 - 用于创建连接
        /// </summary>
        public void Node_ClickForConnection(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (sender is not Border border || border.Tag is not WorkflowNode node)
                {
                    return;
                }

                if (_connectionStartNode == null)
                {
                    _connectionStartNode = node;
                    _isCreatingConnection = true;
                    border.CaptureMouse();
                    e.Handled = true;
                }
                else if (_connectionStartNode != node)
                {
                    HandleConnectionCreation(node);
                    _isCreatingConnection = false;
                    _connectionStartNode = null;
                    border.ReleaseMouseCapture();
                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Node_ClickForConnection] 异常: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 清除所有节点的选中状态
        /// </summary>
        private void ClearAllSelections()
        {
            if (_canvasControl.CurrentWorkflowTab != null)
            {
                foreach (var node in _canvasControl.CurrentWorkflowTab.WorkflowNodes)
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
            try
            {
                System.Diagnostics.Debug.WriteLine("[RecordSelectedNodesPositions] 开始执行");

                if (_canvasControl.CurrentWorkflowTab == null)
                {
                    System.Diagnostics.Debug.WriteLine("[RecordSelectedNodesPositions] SelectedTab为null，返回");
                    return;
                }

                var selectedNodes = _canvasControl.CurrentWorkflowTab.WorkflowNodes
                    .Where(n => n.IsSelected)
                    .ToList();

                System.Diagnostics.Debug.WriteLine($"[RecordSelectedNodesPositions] 选中节点数量: {selectedNodes.Count}");

                _selectedNodesInitialPositions = selectedNodes
                    .Select(n => n.Position)
                    .ToArray();

                System.Diagnostics.Debug.WriteLine($"[RecordSelectedNodesPositions] 记录了 {_selectedNodesInitialPositions.Length} 个初始位置");
                for (int i = 0; i < _selectedNodesInitialPositions.Length; i++)
                {
                    System.Diagnostics.Debug.WriteLine($"[RecordSelectedNodesPositions] 节点 {i} 初始位置: ({_selectedNodesInitialPositions[i].X}, {_selectedNodesInitialPositions[i].Y})");
                }
                System.Diagnostics.Debug.WriteLine("[RecordSelectedNodesPositions] 执行完成");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RecordSelectedNodesPositions] 异常: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[RecordSelectedNodesPositions] 堆栈: {ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// 处理连接创建
        /// </summary>
        private void HandleConnectionCreation(WorkflowNode targetNode)
        {
            if (_connectionStartNode == null || targetNode == null)
                return;

            var selectedTab = _viewModel?.WorkflowTabViewModel.SelectedTab;
            if (selectedTab == null)
                return;

            // 检查是否自连接
            if (_connectionStartNode.Id == targetNode.Id)
            {
                _viewModel!.StatusText = "不能连接到自身";
                return;
            }

            // 检查连接是否已存在
            var exists = selectedTab.WorkflowConnections.Any(c =>
                c.SourceNodeId == _connectionStartNode.Id &&
                c.TargetNodeId == targetNode.Id);

            if (exists)
            {
                _viewModel!.StatusText = "连接已存在";
                return;
            }

            // 创建新连接
            _connectionManager.CreateConnection(_connectionStartNode, targetNode, "BottomPort");
            _viewModel!.StatusText = $"成功连接: {_connectionStartNode.Name} -> {targetNode.Name}";
        }
    }
}