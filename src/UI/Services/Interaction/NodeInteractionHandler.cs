using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;
using SunEyeVision.UI.Models;
using SunEyeVision.UI.ViewModels;
using SunEyeVision.UI.Views.Controls.Canvas;
using SunEyeVision.UI.Services.Rendering;

namespace SunEyeVision.UI.Services.Interaction
{
    /// <summary>
    /// 工作流节点交互处理器
    /// 负责节点的鼠标事件处理、拖拽、选择等交�?
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
        private const int ConnectionUpdateIntervalMs = 50; // 连接线更新间隔（毫秒�?

        // 多选节点拖拽相�?
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
        /// 节点鼠标进入事件（显示连接点�?
        /// </summary>
        public void Node_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Border border && border.Tag is WorkflowNode node)
            {
                SetPortsVisibility(border, true);
            }
        }

        /// <summary>
        /// 节点鼠标离开事件（隐藏连接点�?
        /// </summary>
        public void Node_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Border border && border.Tag is WorkflowNode node)
            {
                SetPortsVisibility(border, false);
            }
        }

        /// <summary>
        /// 连接点鼠标进入事�?
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
        /// 设置单个节点的连接点可见�?
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
        /// 节点鼠标左键按下 - 开始拖�?
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

                // 检查是否按�?Shift �?Ctrl 键（多选模式）
                bool isMultiSelect = (Keyboard.Modifiers & ModifierKeys.Shift) != 0 ||
                                   (Keyboard.Modifiers & ModifierKeys.Control) != 0;

                // 如果节点未被选中，且不是多选模式，则只选中当前节点
                if (!node.IsSelected && !isMultiSelect)
                {
                    ClearAllSelections();
                    node.IsSelected = true;
                }
                // 如果是多选模式，切换选中状�?
                else if (isMultiSelect)
                {
                    node.IsSelected = !node.IsSelected;
                }

                _viewModel.SelectedNode = node;

                // 记录所有选中节点的初始位�?
                RecordSelectedNodesPositions();

                // 单击事件：拖拽准�?
                _isDragging = true;
                _draggedNode = node;
                _initialNodePosition = node.Position;
                _startDragPosition = e.GetPosition(_canvasControl.WorkflowCanvas);

                border.CaptureMouse();

                // 阻�止事件冒泡到 Canvas，避免触发框�?
                e.Handled = true;
            }
            catch (Exception ex)
            {
    
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

                // 计算偏移�?
                double offsetX = currentPosition.X - _startDragPosition.X;
                double offsetY = currentPosition.Y - _startDragPosition.Y;

                // 更新所有选中节点的位�?
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
                // 检查是否是同一个节�?
                if (_connectionSourceNode == targetNode)
                {
                    _viewModel!.StatusText = "无法连接到同一个节�?;
    
                    _connectionSourceNode = null;
                    return;
                }

                // 检查连接是否已存在
                var existingConnection = selectedTab.WorkflowConnections.FirstOrDefault(c =>
                    c.SourceNodeId == _connectionSourceNode!.Id && c.TargetNodeId == targetNode.Id);

                if (existingConnection != null)
                {
                    _viewModel!.StatusText = "连接已存�?;
                    _connectionSourceNode = null;
                    return;
                }

                // 创建新连�?

                _connectionManager.CreateConnection(_connectionSourceNode, targetNode, null);

                // 退出连接模�?
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
    
                throw;
            }
        }

        /// <summary>
        /// 清除所有节点的选中状�?
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
        /// 记录选中节点的初始位�?
        /// </summary>
        private void RecordSelectedNodesPositions()
        {
            try
            {
    

                if (_canvasControl.CurrentWorkflowTab == null)
                {
    
                    return;
                }

                var selectedNodes = _canvasControl.CurrentWorkflowTab.WorkflowNodes
                    .Where(n => n.IsSelected)
                    .ToList();

    

                _selectedNodesInitialPositions = selectedNodes
                    .Select(n => n.Position)
                    .ToArray();

    
                for (int i = 0; i < _selectedNodesInitialPositions.Length; i++)
                {
    
                }
    
            }
            catch (Exception ex)
            {
    
    
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
                _viewModel!.StatusText = "不能连接到自�?;
                return;
            }

            // 检查连接是否已存在
            var exists = selectedTab.WorkflowConnections.Any(c =>
                c.SourceNodeId == _connectionStartNode.Id &&
                c.TargetNodeId == targetNode.Id);

            if (exists)
            {
                _viewModel!.StatusText = "连接已存�?;
                return;
            }

            // 创建新连�?
            _connectionManager.CreateConnection(_connectionStartNode, targetNode, "BottomPort");
            _viewModel!.StatusText = $"成功连接: {_connectionStartNode.Name} -> {targetNode.Name}";
        }
    }
}
