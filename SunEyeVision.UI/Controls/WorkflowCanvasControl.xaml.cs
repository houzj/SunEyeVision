using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using SunEyeVision.UI.Models;
using SunEyeVision.UI.ViewModels;

namespace SunEyeVision.UI.Controls
{
    /// <summary>
    /// WorkflowCanvasControl.xaml 的交互逻辑
    /// 纯画布控件，负责节点和连线的显示、拖拽、连接等交互
    /// </summary>
    public partial class WorkflowCanvasControl : UserControl
    {
        private MainWindowViewModel? _viewModel;
        private bool _isDragging;
        private WorkflowNode? _draggedNode;
        private System.Windows.Point _startDragPosition;
        private System.Windows.Point _initialNodePosition;

        // 框选相关
        private bool _isBoxSelecting;
        private System.Windows.Point _boxSelectStart;
        private System.Windows.Point[]? _selectedNodesInitialPositions;

        // 连接模式相关
        private bool _isCreatingConnection = false;
        private WorkflowNode? _connectionSourceNode = null;
        private System.Windows.Point _connectionStartPoint;

        // 拖拽连接相关
        private bool _isDraggingConnection = false;
        private WorkflowNode? _dragConnectionSourceNode = null;
        private System.Windows.Point _dragConnectionStartPoint;

        /// <summary>
        /// 是否正在拖拽连接（用于绑定，控制连接点是否显示）
        /// </summary>
        public bool IsDraggingConnection
        {
            get => _isDraggingConnection;
            private set
            {
                _isDraggingConnection = value;
                SetPortsVisibility(value);
            }
        }

        /// <summary>
        /// 获取当前工作流Tab（从ViewModel中获取）
        /// </summary>
        public ViewModels.WorkflowTabViewModel? CurrentWorkflowTab => _viewModel?.WorkflowTabViewModel.SelectedTab;

        /// <summary>
        /// 获取当前工作流信息（用于转换器）
        /// </summary>
        public Models.WorkflowInfo? CurrentWorkflow
        {
            get
            {
                var tab = CurrentWorkflowTab;
                if (tab == null) return null;
                return new Models.WorkflowInfo { Id = tab.Id, Name = tab.Name };
            }
        }

        public WorkflowCanvasControl()
        {
            InitializeComponent();

            Loaded += WorkflowCanvasControl_Loaded;

            // 尝试获取 ViewModel（从父窗口）
            try
            {
                if (Window.GetWindow(this) is MainWindow mainWindow)
                {
                    _viewModel = mainWindow.DataContext as MainWindowViewModel;
                    _viewModel?.AddLog("[WorkflowCanvas] 构造函数中获取MainWindowViewModel: {_viewModel != null}");
                }
            }
            catch (Exception ex)
            {
                _viewModel?.AddLog($"[WorkflowCanvas] 构造函数异常: {ex.Message}");
            }
        }

        private void WorkflowCanvasControl_Loaded(object sender, RoutedEventArgs e)
        {
            _viewModel?.AddLog("[WorkflowCanvas] ====== Loaded事件触发 ======");

            // 获取 MainWindowViewModel
            if (Window.GetWindow(this) is MainWindow mainWindow)
            {
                _viewModel = mainWindow.DataContext as MainWindowViewModel;
                if (_viewModel == null)
                {
                    _viewModel?.AddLog("[WorkflowCanvas] ❌ 无法获取MainWindowViewModel");
                }
                else
                {
                    _viewModel?.AddLog("[WorkflowCanvas] ✓ 成功获取MainWindowViewModel");

                    // 检查 WorkflowTabViewModel
                    if (_viewModel.WorkflowTabViewModel == null)
                    {
                        _viewModel?.AddLog("[WorkflowCanvas] ❌ WorkflowTabViewModel为null");
                    }
                    else
                    {
                        _viewModel?.AddLog("[WorkflowCanvas] ✓ WorkflowTabViewModel不为null");

                        // 检查 SelectedTab
                        var selectedTab = _viewModel.WorkflowTabViewModel.SelectedTab;
                        if (selectedTab == null)
                        {
                            _viewModel?.AddLog("[WorkflowCanvas] ❌ SelectedTab为null");
                        }
                        else
                        {
                            _viewModel?.AddLog($"[WorkflowCanvas] ✓ SelectedTab不为null，ID={selectedTab.Id}");
                            _viewModel?.AddLog($"[WorkflowCanvas] 当前节点数: {selectedTab.WorkflowNodes.Count}");
                            _viewModel?.AddLog($"[WorkflowCanvas] 当前连接数: {selectedTab.WorkflowConnections.Count}");
                        }
                    }

                    _viewModel.AddLog($"[WorkflowCanvas] ✓ WorkflowCanvasControl 已加载，可以开始拖拽创建连接");
                }
            }

            // 初始化智能路径转换器的节点集合
            if (_viewModel?.WorkflowTabViewModel.SelectedTab != null)
            {
                Converters.SmartPathConverter.Nodes = _viewModel.WorkflowTabViewModel.SelectedTab.WorkflowNodes;
                _viewModel?.AddLog($"[WorkflowCanvas] ✓ 已初始化SmartPathConverter的节点集合，共{Converters.SmartPathConverter.Nodes.Count()}个节点");
            }
        }

        #region 节点交互事件

        /// <summary>
        /// 节点鼠标进入事件（显示连接点）
        /// </summary>
        private void Node_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Border border && border.Tag is WorkflowNode node)
            {
                SetPortsVisibility(border, true);
            }
        }

        /// <summary>
        /// 节点鼠标离开事件（隐藏连接点）
        /// </summary>
        private void Node_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Border border && border.Tag is WorkflowNode node)
            {
                SetPortsVisibility(border, false);
            }
        }

        /// <summary>
        /// 连接点鼠标进入事件
        /// </summary>
        private void Ellipse_MouseEnter(object sender, MouseEventArgs e)
        {
            // 连接点样式已通过 XAML 处理
        }

        /// <summary>
        /// 连接点鼠标离开事件
        /// </summary>
        private void Ellipse_MouseLeave(object sender, MouseEventArgs e)
        {
            // 连接点样式已通过 XAML 处理
        }

        /// <summary>
        /// 设置所有节点的连接点可见性
        /// </summary>
        private void SetPortsVisibility(bool isVisible)
        {
            if (_viewModel?.WorkflowTabViewModel.SelectedTab == null)
                return;

            // 遍历所有节点并设置连接点可见性
            var selectedTab = _viewModel.WorkflowTabViewModel.SelectedTab;
            var nodeBorders = FindAllVisualChildren<Border>(WorkflowCanvas);

            foreach (var border in nodeBorders)
            {
                if (border.Tag is WorkflowNode node && selectedTab.WorkflowNodes.Contains(node))
                {
                    SetPortsVisibility(border, isVisible);
                }
            }
        }

        /// <summary>
        /// 设置单个节点的连接点可见性
        /// </summary>
        private void SetPortsVisibility(Border border, bool isVisible)
        {
            var ellipses = FindAllVisualChildren<Ellipse>(border);
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
        private void Node_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
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
            _startDragPosition = e.GetPosition(WorkflowCanvas);

            border.CaptureMouse();

            // 阻止事件冒泡到 Canvas，避免触发框选
            e.Handled = true;
        }

        /// <summary>
        /// 节点鼠标左键释放 - 结束拖拽
        /// </summary>
        private void Node_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging && _draggedNode != null)
            {
                // 拖拽结束，执行批量移动命令
                if (_viewModel?.WorkflowTabViewModel.SelectedTab != null)
                {
                    var selectedNodes = _viewModel.WorkflowTabViewModel.SelectedTab.WorkflowNodes
                        .Where(n => n.IsSelected)
                        .ToList();

                    if (selectedNodes.Count > 0)
                    {
                        // 计算每个节点的移动偏移量
                        var offsets = new System.Windows.Point[selectedNodes.Count];
                        for (int i = 0; i < selectedNodes.Count; i++)
                        {
                            if (_selectedNodesInitialPositions != null && i < _selectedNodesInitialPositions.Length)
                            {
                                offsets[i] = new System.Windows.Point(
                                    selectedNodes[i].Position.X - _selectedNodesInitialPositions[i].X,
                                    selectedNodes[i].Position.Y - _selectedNodesInitialPositions[i].Y
                                );
                            }
                        }

                        // 如果有移动，执行批量移动命令
                        if (offsets.Any(o => o.X != 0 || o.Y != 0))
                        {
                            var batchCommand = new Commands.BatchMoveNodesCommand(
                                new ObservableCollection<WorkflowNode>(selectedNodes),
                                offsets
                            );
                            _viewModel.WorkflowTabViewModel.SelectedTab.CommandManager.Execute(batchCommand);
                        }
                    }
                }

                _isDragging = false;
                _draggedNode = null!;
                (sender as Border)?.ReleaseMouseCapture();
            }
        }

        /// <summary>
        /// 节点鼠标移动 - 执行拖拽
        /// </summary>
        private void Node_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging && _draggedNode != null && e.LeftButton == MouseButtonState.Pressed)
            {
                var currentPosition = e.GetPosition(WorkflowCanvas);

                // 批量移动所有选中的节点
                if (_viewModel?.WorkflowTabViewModel.SelectedTab != null &&
                    _selectedNodesInitialPositions != null)
                {
                    var selectedNodes = _viewModel.WorkflowTabViewModel.SelectedTab.WorkflowNodes
                        .Where(n => n.IsSelected)
                        .ToList();

                    // 计算从拖动开始到现在的总偏移量
                    var totalOffset = currentPosition - _startDragPosition;

                    for (int i = 0; i < selectedNodes.Count && i < _selectedNodesInitialPositions.Length; i++)
                    {
                        var newPos = new System.Windows.Point(
                            _selectedNodesInitialPositions[i].X + totalOffset.X,
                            _selectedNodesInitialPositions[i].Y + totalOffset.Y
                        );
                        selectedNodes[i].Position = newPos;
                    }
                }
                else
                {
                    // 单个节点移动（向后兼容）
                    var offset = currentPosition - _startDragPosition;
                    _draggedNode.Position = new System.Windows.Point(
                        _initialNodePosition.X + offset.X,
                        _initialNodePosition.Y + offset.Y
                    );
                }
            }
        }

        /// <summary>
        /// 节点点击事件 - 用于连接或选中
        /// </summary>
        private void Node_ClickForConnection(object sender, RoutedEventArgs e)
        {
            _viewModel?.AddLog($"[Connection] Node_ClickForConnection 触发，Sender类型: {sender?.GetType().Name}");

            // 获取节点对象（支持 Border 或 Ellipse 作为 sender）
            WorkflowNode? targetNode = null;

            if (sender is Border border && border.Tag is WorkflowNode clickedNodeFromBorder)
            {
                targetNode = clickedNodeFromBorder;
                _viewModel?.AddLog($"[Connection] 从Border获取到节点: {targetNode.Name}");
            }
            else if (sender is Ellipse ellipse && ellipse.Tag is WorkflowNode clickedNodeFromEllipse)
            {
                targetNode = clickedNodeFromEllipse;
                _viewModel?.AddLog($"[Connection] 从Ellipse获取到节点: {targetNode.Name}, 节点ID={targetNode.Id}, 节点Hash={targetNode.GetHashCode()}");

                // 选中当前节点（连接点点击时也需要选中节点）
                if (_viewModel?.WorkflowTabViewModel.SelectedTab != null)
                {
                    foreach (var n in _viewModel.WorkflowTabViewModel.SelectedTab.WorkflowNodes)
                    {
                        n.IsSelected = (n == targetNode);
                    }
                    _viewModel.SelectedNode = targetNode;
                }
            }
            else
            {
                _viewModel?.AddLog($"[Connection] ❌ 无法从sender获取节点，sender类型: {sender?.GetType().Name}");
                return;
            }

            if (targetNode == null)
                return;

            // 阻止事件冒泡到节点的点击事件
            e.Handled = true;

            // 使用 SelectedTab 的连接模式状态
            var selectedTab = _viewModel?.WorkflowTabViewModel.SelectedTab;
            if (selectedTab == null)
            {
                _viewModel?.AddLog($"[Connection] ❌ SelectedTab为null");
                return;
            }

            _viewModel?.AddLog($"[Connection] 当前源节点: {_connectionSourceNode?.Name ?? "null"}");

            // 检查是否在连接模式
            if (_connectionSourceNode == null)
            {
                // 进入连接模式
                _connectionSourceNode = targetNode;
                _viewModel?.AddLog($"[连接] 开始创建连接，源节点: {targetNode.Name}, ID={targetNode.Id}, Hash={targetNode.GetHashCode()}");
                _viewModel!.StatusText = $"请选择目标节点进行连接，从: {targetNode.Name}";
                _viewModel?.AddLog($"[Connection] ✓ 进入连接模式，源节点: {targetNode.Name}, ID={targetNode.Id}, Hash={targetNode.GetHashCode()}");
            }
            else
            {
                // 检查是否是同一个节点
                _viewModel?.AddLog($"[Connection] 检查同一节点: 源节点ID={_connectionSourceNode?.Id}, 目标节点ID={targetNode.Id}, 源节点Hash={_connectionSourceNode?.GetHashCode()}, 目标Hash={targetNode.GetHashCode()}");
                if (_connectionSourceNode == targetNode)
                {
                    _viewModel!.StatusText = "无法连接到同一个节点";
                    _viewModel.AddLog("[连接] ❌ 无法连接到同一个节点");
                    _viewModel?.AddLog($"[Connection] ❌ 不能连接到同一个节点");
                    _connectionSourceNode = null;
                    return;
                }

                // 检查连接是否已存在
                var existingConnection = selectedTab.WorkflowConnections.FirstOrDefault(c =>
                    c.SourceNodeId == _connectionSourceNode!.Id && c.TargetNodeId == targetNode.Id);

                if (existingConnection != null)
                {
                    _viewModel!.StatusText = "连接已存在";
                    _viewModel?.AddLog($"[Connection] ❌ 连接已存在");
                    _connectionSourceNode = null;
                    return;
                }

                // 创建新连接
                _viewModel?.AddLog($"[Connection] ✓ 创建连接: {_connectionSourceNode.Name} -> {targetNode.Name}");
                _viewModel?.AddLog($"[连接] 创建连接成功: {_connectionSourceNode.Name} -> {targetNode.Name}");
                CreateConnection(_connectionSourceNode, targetNode);

                // 退出连接模式
                _connectionSourceNode = null;
            }
        }

        /// <summary>
        /// 连接点鼠标按下 - 开始拖拽连接
        /// </summary>
        private void Port_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _viewModel?.AddLog($"[拖拽连接] ========== Port_PreviewMouseLeftButtonDown 开始 ==========");
            _viewModel?.AddLog($"[拖拽连接] Sender类型: {sender?.GetType().Name}");
            _viewModel?.AddLog($"[拖拽连接] OriginalSource类型: {e.OriginalSource?.GetType().Name}");
            _viewModel?.AddLog($"[拖拽连接] 当前_isDraggingConnection: {_isDraggingConnection}");

            if (sender is not Ellipse port || port.Tag is not WorkflowNode node)
            {
                _viewModel?.AddLog($"[拖拽连接] ❌ 条件检查失败 - Sender类型: {sender?.GetType().Name}");
                if (sender is Ellipse ellipse)
                {
                    _viewModel?.AddLog($"[拖拽连接] ❌ Ellipse Tag是否为WorkflowNode: {ellipse.Tag is WorkflowNode}");
                }
                return;
            }

            e.Handled = true;

            _viewModel?.AddLog($"[拖拽连接] ✓ 获取到源节点: {node.Name}, ID={node.Id}, Hash={node.GetHashCode()}");

            // 选中当前节点
            if (_viewModel?.WorkflowTabViewModel.SelectedTab != null)
            {
                int nodeCount = _viewModel.WorkflowTabViewModel.SelectedTab.WorkflowNodes.Count;
                _viewModel?.AddLog($"[拖拽连接] 当前工作流共有{nodeCount}个节点");

                foreach (var n in _viewModel.WorkflowTabViewModel.SelectedTab.WorkflowNodes)
                {
                    n.IsSelected = (n == node);
                }
                _viewModel.SelectedNode = node;
                _viewModel?.AddLog($"[拖拽连接] ✓ 选中节点完成: {node.Name}");
            }
            else
            {
                _viewModel?.AddLog($"[拖拽连接] ⚠ SelectedTab为null，无法选中节点");
            }

            // 开始拖拽连接（使用属性设置，会自动显示所有连接点）
            IsDraggingConnection = true;
            _dragConnectionSourceNode = node;
            _viewModel?.AddLog($"[拖拽连接] ✓ 设置拖拽状态完成，IsDraggingConnection={_isDraggingConnection}");
            _viewModel?.AddLog($"[拖拽连接] ✓ 设置拖拽状态，IsDraggingConnection={_isDraggingConnection}");

            // 输出节点信息
            _viewModel?.AddLog($"[拖拽连接] 源节点详细信息:");
            _viewModel?.AddLog($"[拖拽连接]   节点位置: X={node.Position.X:F2}, Y={node.Position.Y:F2}");
            _viewModel?.AddLog($"[拖拽连接]   节点大小: 140x90");
            _viewModel?.AddLog($"[拖拽连接]   各端口位置:");
            _viewModel?.AddLog($"[拖拽连接]     - TopPort: {node.TopPortPosition}");
            _viewModel?.AddLog($"[拖拽连接]     - BottomPort: {node.BottomPortPosition}");
            _viewModel?.AddLog($"[拖拽连接]     - LeftPort: {node.LeftPortPosition}");
            _viewModel?.AddLog($"[拖拽连接]     - RightPort: {node.RightPortPosition}");

            // 获取连接点在画布上的位置
            _dragConnectionStartPoint = e.GetPosition(WorkflowCanvas);
            _viewModel?.AddLog($"[拖拽连接] ✓ 获取起始点（鼠标位置）: {_dragConnectionStartPoint}");

            // 计算鼠标相对于节点中心的偏移
            double nodeCenterX = node.Position.X + 70;
            double nodeCenterY = node.Position.Y + 45;
            double offsetX = _dragConnectionStartPoint.X - nodeCenterX;
            double offsetY = _dragConnectionStartPoint.Y - nodeCenterY;
            _viewModel?.AddLog($"[拖拽连接] 节点中心: X={nodeCenterX:F2}, Y={nodeCenterY:F2}");
            _viewModel?.AddLog($"[拖拽连接] 鼠标相对于中心偏移: offsetX={offsetX:F2}, offsetY={offsetY:F2}");

            // 判断点击的是哪个端口
            string? clickedPort = null;
            if (Math.Abs(offsetX) > Math.Abs(offsetY))
            {
                // 水平方向
                if (offsetX > 0)
                {
                    clickedPort = "RightPort";
                    _viewModel?.AddLog($"[拖拽连接] 判断: 点击右侧端口");
                }
                else
                {
                    clickedPort = "LeftPort";
                    _viewModel?.AddLog($"[拖拽连接] 判断: 点击左侧端口");
                }
            }
            else
            {
                // 垂直方向
                if (offsetY > 0)
                {
                    clickedPort = "BottomPort";
                    _viewModel?.AddLog($"[拖拽连接] 判断: 点击底部端口");
                }
                else
                {
                    clickedPort = "TopPort";
                    _viewModel?.AddLog($"[拖拽连接] 判断: 点击顶部端口");
                }
            }

            // 根据判断的端口更新起始点为端口的标准位置
            switch (clickedPort)
            {
                case "TopPort":
                    _dragConnectionStartPoint = node.TopPortPosition;
                    break;
                case "BottomPort":
                    _dragConnectionStartPoint = node.BottomPortPosition;
                    break;
                case "LeftPort":
                    _dragConnectionStartPoint = node.LeftPortPosition;
                    break;
                case "RightPort":
                    _dragConnectionStartPoint = node.RightPortPosition;
                    break;
            }

            _viewModel?.AddLog($"[拖拽连接] ✓ 最终使用的起始点（端口位置）: {_dragConnectionStartPoint}");

            // 显示临时连接线
            _viewModel?.AddLog($"[拖拽连接] TempConnectionLine是否为null: {TempConnectionLine == null}");
            _viewModel?.AddLog($"[拖拽连接] TempConnectionGeometry是否为null: {TempConnectionGeometry == null}");

            if (TempConnectionLine != null)
            {
                _viewModel?.AddLog($"[拖拽连接] 临时连接线当前Visibility: {TempConnectionLine.Visibility}");
                TempConnectionLine.Visibility = Visibility.Visible;
                _viewModel?.AddLog($"[拖拽连接] 临时连接线设置后Visibility: {TempConnectionLine.Visibility}");
            }

            if (TempConnectionGeometry != null)
            {
                // 初始化临时连接线为直线（起点和终点相同）
                TempConnectionGeometry.Figures.Clear();
                var pathFigure = new PathFigure
                {
                    StartPoint = _dragConnectionStartPoint,
                    IsClosed = false
                };
                pathFigure.Segments.Add(new LineSegment(_dragConnectionStartPoint, true));
                TempConnectionGeometry.Figures.Add(pathFigure);
                _viewModel?.AddLog($"[拖拽连接] 临时连接线起点: {_dragConnectionStartPoint}");
            }

            _viewModel?.AddLog($"[拖拽连接] ✓ 显示临时连接线");

            // 在Canvas上捕获鼠标（而不是在Ellipse上），这样可以在任意位置检测释放事件
            WorkflowCanvas.CaptureMouse();
            _viewModel?.AddLog($"[拖拽连接] ✓ 在Canvas上捕获鼠标");
            _viewModel?.AddLog($"[拖拽连接] ✓ 鼠标已捕获");

            _viewModel?.AddLog($"[拖拽连接] 开始拖拽连接，源节点: {node.Name}");
            _viewModel?.AddLog($"[拖拽连接] ========== Port_PreviewMouseLeftButtonDown 完成 ==========");
        }

        /// <summary>
        /// 连接点鼠标释放 - 结束拖拽并创建连接
        /// </summary>
        private void Port_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _viewModel?.AddLog($"[拖拽连接] ========== Port_PreviewMouseLeftButtonUp 开始 ==========");
            _viewModel?.AddLog($"[拖拽连接] 当前_isDraggingConnection: {_isDraggingConnection}");
            _viewModel?.AddLog($"[拖拽连接] 拖拽源节点: {_dragConnectionSourceNode?.Name ?? "null"}");
            _viewModel?.AddLog($"[拖拽连接] Sender类型: {sender?.GetType().Name}");
            _viewModel?.AddLog($"[拖拽连接] OriginalSource类型: {e.OriginalSource?.GetType().Name}");
            _viewModel?.AddLog($"[拖拽连接] e.Handled设置前: {e.Handled}");

            if (!_isDraggingConnection || _dragConnectionSourceNode == null)
            {
                _viewModel?.AddLog($"[拖拽连接] ❌ 未在拖拽状态或源节点为null，退出");
                return;
            }

            // 在处理任何操作之前立即标记事件为已处理，阻止传播
            e.Handled = true;
            _viewModel?.AddLog($"[拖拽连接] ✓ e.Handled已设置为true，阻止事件传播到Canvas");

            if (sender is Ellipse port)
            {
                port.ReleaseMouseCapture();
                _viewModel?.AddLog($"[拖拽连接] ✓ 释放鼠标捕获");
            }

            // 隐藏临时连接线
            TempConnectionLine.Visibility = Visibility.Collapsed;
            _viewModel?.AddLog($"[拖拽连接] ✓ 隐藏临时连接线");

            // 查找鼠标位置下的目标节点
            var mousePosition = e.GetPosition(WorkflowCanvas);
            _viewModel?.AddLog($"[拖拽连接] 鼠标释放位置: {mousePosition}");

            WorkflowNode? targetNode = null;
            Border? targetBorder = null;
            int hitTestCount = 0;

            _viewModel?.AddLog($"[拖拽连接] ========== 开始HitTest查找目标节点 ==========");
            _viewModel?.AddLog($"[拖拽连接] 源节点信息: {_dragConnectionSourceNode.Name}, ID={_dragConnectionSourceNode.Id}");
            _viewModel?.AddLog($"[拖拽连接] 源节点位置: X={_dragConnectionSourceNode.Position.X:F2}, Y={_dragConnectionSourceNode.Position.Y:F2}");

            // 使用 HitTest 查找鼠标位置下的所有元素
            VisualTreeHelper.HitTest(WorkflowCanvas, null,
                result =>
                {
                    hitTestCount++;
                    _viewModel?.AddLog($"[拖拽连接] HitTest #{hitTestCount}: 找到元素类型: {result.VisualHit?.GetType().Name}");

                    // 如果找到 Border 且带有 WorkflowNode Tag，记录下来
                    if (result.VisualHit is Border hitBorder && hitBorder.Tag is WorkflowNode hitNode)
                    {
                        targetNode = hitNode;
                        targetBorder = hitBorder;
                        _viewModel?.AddLog($"[拖拽连接] ✓✓ HitTest找到节点Border: {hitNode.Name}, ID={hitNode.Id}, Hash={hitNode.GetHashCode()}");
                        return HitTestResultBehavior.Stop;
                    }

                    // 对于任何命中的元素，都向上查找带有WorkflowNode Tag的Border
                    DependencyObject? current = result.VisualHit as DependencyObject;
                    int depth = 0;
                    while (current != null && depth < 30)
                    {
                        depth++;
                        if (current is Border currentBorder && currentBorder.Tag is WorkflowNode currentBorderNode)
                        {
                            targetNode = currentBorderNode;
                            targetBorder = currentBorder;
                            _viewModel?.AddLog($"[拖拽连接] ✓✓ 向上查找第{depth}层找到节点Border: {currentBorderNode.Name}, ID={currentBorderNode.Id}, Hash={currentBorderNode.GetHashCode()}");
                            return HitTestResultBehavior.Stop;
                        }
                        current = VisualTreeHelper.GetParent(current);
                    }

                    return HitTestResultBehavior.Continue;
                },
                new PointHitTestParameters(mousePosition));

            _viewModel?.AddLog($"[拖拽连接] HitTest共检测到{hitTestCount}个元素");
            _viewModel?.AddLog($"[拖拽连接] 最终目标节点: {targetNode?.Name ?? "null"}");

            // 检查是否找到目标节点
            if (targetNode != null && targetNode != _dragConnectionSourceNode)
            {
                _viewModel?.AddLog($"[拖拽连接] ✓ 找到有效目标节点，检查连接是否已存在");
                _viewModel?.AddLog($"[拖拽连接] 找到目标节点: {targetNode.Name}");

                // 检查连接是否已存在
                var selectedTab = _viewModel?.WorkflowTabViewModel.SelectedTab;
                if (selectedTab != null)
                {
                    _viewModel?.AddLog($"[拖拽连接] SelectedTab不为null，当前连接数: {selectedTab.WorkflowConnections.Count}");

                    var existingConnection = selectedTab.WorkflowConnections.FirstOrDefault(c =>
                        c.SourceNodeId == _dragConnectionSourceNode.Id && c.TargetNodeId == targetNode.Id);

                    if (existingConnection == null)
                    {
                        _viewModel?.AddLog($"[拖拽连接] ✓ 连接不存在，准备创建新连接");
                        // 创建新连接
                        CreateConnection(_dragConnectionSourceNode, targetNode);
                        _viewModel?.AddLog($"[拖拽连接] ✓ 连接创建成功: {_dragConnectionSourceNode.Name} -> {targetNode.Name}");
                        _viewModel?.AddLog($"[拖拽连接] ✓✓ 连接创建完成");
                    }
                    else
                    {
                        _viewModel?.AddLog($"[拖拽连接] ❌ 连接已存在: {_dragConnectionSourceNode.Name} -> {targetNode.Name}");
                        _viewModel?.AddLog($"[拖拽连接] ❌ 连接已存在，ID={existingConnection.Id}");
                    }
                }
                else
                {
                    _viewModel?.AddLog($"[拖拽连接] ❌ SelectedTab为null");
                }
            }
            else
            {
                if (targetNode == null)
                {
                    _viewModel?.AddLog($"[拖拽连接] ❌ 未找到目标节点");
                    _viewModel?.AddLog($"[拖拽连接] ❌ 目标节点为null");
                }
                else
                {
                    _viewModel?.AddLog($"[拖拽连接] ❌ 目标节点与源节点相同");
                    _viewModel?.AddLog($"[拖拽连接] ❌ 目标节点与源节点相同: {_dragConnectionSourceNode?.Name}");
                }
            }

            // 重置拖拽状态（使用属性设置，会自动隐藏所有连接点）
            IsDraggingConnection = false;
            _dragConnectionSourceNode = null;
            _viewModel?.AddLog($"[拖拽连接] ✓ 重置拖拽状态完成");
            _viewModel?.AddLog($"[拖拽连接] ========== Port_PreviewMouseLeftButtonUp 完成 ==========");
        }

        #endregion

        #region 框选功能

        /// <summary>
        /// Canvas 鼠标左键按下 - 开始框选或清除选择
        /// </summary>
        private void WorkflowCanvas_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 检查点击的是否是节点（通过原始源）
            var originalSource = e.OriginalSource as DependencyObject;

            // 手动查找带 WorkflowNode Tag 的 Border
            WorkflowNode? clickedNode = null;
            DependencyObject? current = originalSource;
            while (current != null)
            {
                if (current is Border border && border.Tag is WorkflowNode node)
                {
                    clickedNode = node;
                    break;
                }
                current = VisualTreeHelper.GetParent(current);
            }

            // 如果点击的是有 WorkflowNode Tag 的 Border，则由节点的事件处理，不触发框选
            if (clickedNode != null)
            {
                return;
            }

            // 检查是否按住 Shift 或 Ctrl 键（多选模式）
            bool isMultiSelect = (Keyboard.Modifiers & ModifierKeys.Shift) != 0 ||
                               (Keyboard.Modifiers & ModifierKeys.Control) != 0;

            // 开始框选
            _isBoxSelecting = true;
            _boxSelectStart = e.GetPosition(WorkflowCanvas);

            // 如果不是多选模式，清除所有选择
            if (!isMultiSelect)
            {
                ClearAllSelections();
            }

            // 开始显示框选框
            SelectionBox?.StartSelection(_boxSelectStart);

            WorkflowCanvas.CaptureMouse();
            e.Handled = true;
        }

        /// <summary>
        /// Canvas 鼠标移动 - 更新框选区域
        /// </summary>
        private void WorkflowCanvas_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            // 处理拖拽连接
            if (_isDraggingConnection)
            {
                if (TempConnectionGeometry != null && _dragConnectionSourceNode != null)
                {
                    var currentPoint = e.GetPosition(WorkflowCanvas);
                    _viewModel?.AddLog($"[拖拽连接-Move] ========== 拖拽移动 ==========");
                    _viewModel?.AddLog($"[拖拽连接-Move] 源节点: {_dragConnectionSourceNode.Name}");
                    _viewModel?.AddLog($"[拖拽连接-Move] 鼠标当前位置: X={currentPoint.X:F2}, Y={currentPoint.Y:F2}");
                    _viewModel?.AddLog($"[拖拽连接-Move] 拖拽起始点: {_dragConnectionStartPoint}");

                    // 获取源节点的连接点位置
                    var sourcePort = GetPortPosition(_dragConnectionSourceNode, _dragConnectionStartPoint);
                    _viewModel?.AddLog($"[拖拽连接-Move] 源连接点位置: {sourcePort}");

                    // 计算智能直角折线路径
                    var pathPoints = CalculateSmartPath(sourcePort, currentPoint);
                    _viewModel?.AddLog($"[拖拽连接-Move] 计算路径点数量: {pathPoints.Count}");
                    for (int i = 0; i < pathPoints.Count && i < 8; i++)
                    {
                        _viewModel?.AddLog($"[拖拽连接-Move] 路径点#{i}: X={pathPoints[i].X:F2}, Y={pathPoints[i].Y:F2}");
                    }

                    // 更新临时连接线
                    if (TempConnectionGeometry != null)
                    {
                        TempConnectionGeometry.Figures.Clear();
                        var pathFigure = new PathFigure
                        {
                            StartPoint = sourcePort,
                            IsClosed = false
                        };

                        // 添加路径点
                        foreach (var point in pathPoints)
                        {
                            pathFigure.Segments.Add(new LineSegment(point, true));
                        }

                        TempConnectionGeometry.Figures.Add(pathFigure);
                        _viewModel?.AddLog($"[拖拽连接-Move] ✓ 临时连接线已更新，Segments数量: {pathFigure.Segments.Count}");
                        _viewModel?.AddLog($"[拖拽连接-Move] ========== 拖拽移动完成 ==========");
                    }
                    else
                    {
                        _viewModel?.AddLog($"[拖拽连接-Move] ⚠ TempConnectionGeometry为null，无法更新临时连接线");
                    }
                }
                else
                {
                    _viewModel?.AddLog($"[拖拽连接-Move] ❌ TempConnectionGeometry或源节点为null");
                    _viewModel?.AddLog($"[拖拽连接-Move] TempConnectionGeometry是否为null: {TempConnectionGeometry == null}");
                    _viewModel?.AddLog($"[拖拽连接-Move] 源节点是否为null: {_dragConnectionSourceNode == null}");
                }
                return;
            }

            // 处理框选
            if (!_isBoxSelecting) return;

            // 更新框选框
            var selectionPoint = e.GetPosition(WorkflowCanvas);
            SelectionBox?.UpdateSelection(selectionPoint);

            // 获取框选区域
            var selectionRect = SelectionBox?.GetSelectionRect() ?? new Rect();

            // 更新选中的节点
            if (_viewModel?.WorkflowTabViewModel.SelectedTab != null)
            {
                int selectedCount = 0;

                foreach (var node in _viewModel.WorkflowTabViewModel.SelectedTab.WorkflowNodes)
                {
                    // 获取节点边界（节点大小为 140x90）
                    var nodeRect = new Rect(node.Position.X, node.Position.Y, 140, 90);

                    // 检查节点是否与框选区域相交
                    bool isSelected = selectionRect.IntersectsWith(nodeRect);
                    node.IsSelected = isSelected;

                    if (isSelected) selectedCount++;
                }

                // 更新框选信息显示
                SelectionBox?.SetItemCount(selectedCount);
            }
        }

        /// <summary>
        /// Canvas 鼠标左键释放 - 结束框选或创建连接
        /// </summary>
        private void WorkflowCanvas_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _viewModel?.AddLog($"[Canvas] ========== WorkflowCanvas_PreviewMouseLeftButtonUp 开始 ==========");
            _viewModel?.AddLog($"[Canvas] _isDraggingConnection: {_isDraggingConnection}, _isBoxSelecting: {_isBoxSelecting}");
            _viewModel?.AddLog($"[Canvas] OriginalSource类型: {e.OriginalSource?.GetType().Name}");

            // 如果正在拖拽连接，尝试创建连接
            if (_isDraggingConnection)
            {
                _viewModel?.AddLog($"[Canvas] ✓ 正在拖拽连接，开始处理连接创建");

                e.Handled = true; // 阻止事件继续传播

                // 隐藏临时连接线
                TempConnectionLine.Visibility = Visibility.Collapsed;
                _viewModel?.AddLog($"[Canvas] ✓ 隐藏临时连接线");

                // 查找鼠标位置下的目标节点
                var mousePosition = e.GetPosition(WorkflowCanvas);
                _viewModel?.AddLog($"[Canvas] 鼠标释放位置: X={mousePosition.X:F2}, Y={mousePosition.Y:F2}");

                WorkflowNode? targetNode = null;
                Border? targetBorder = null;
                int hitTestCount = 0;

                _viewModel?.AddLog($"[Canvas] ========== 开始HitTest查找目标节点 ==========");
                _viewModel?.AddLog($"[Canvas] 源节点信息: {_dragConnectionSourceNode.Name}, ID={_dragConnectionSourceNode.Id}");
                _viewModel?.AddLog($"[Canvas] 源节点位置: X={_dragConnectionSourceNode.Position.X:F2}, Y={_dragConnectionSourceNode.Position.Y:F2}");

                // 使用 HitTest 查找鼠标位置下的所有元素
                VisualTreeHelper.HitTest(WorkflowCanvas, null,
                    result =>
                    {
                        hitTestCount++;
                        _viewModel?.AddLog($"[Canvas] HitTest #{hitTestCount}: 找到元素类型: {result.VisualHit?.GetType().Name}");

                        // 如果找到 Border 且带有 WorkflowNode Tag，记录下来
                        if (result.VisualHit is Border hitBorder && hitBorder.Tag is WorkflowNode hitNode)
                        {
                            targetNode = hitNode;
                            targetBorder = hitBorder;
                            _viewModel?.AddLog($"[Canvas] ✓✓ HitTest找到节点Border: {hitNode.Name}, ID={hitNode.Id}, Hash={hitNode.GetHashCode()}");
                            return HitTestResultBehavior.Stop;
                        }

                        // 对于任何命中的元素，都向上查找带有WorkflowNode Tag的Border
                        DependencyObject? current = result.VisualHit as DependencyObject;
                        int depth = 0;
                        while (current != null && depth < 30)
                        {
                            depth++;
                            if (current is Border currentBorder && currentBorder.Tag is WorkflowNode currentBorderNode)
                            {
                                targetNode = currentBorderNode;
                                targetBorder = currentBorder;
                                _viewModel?.AddLog($"[Canvas] ✓✓ 向上查找第{depth}层找到节点Border: {currentBorderNode.Name}, ID={currentBorderNode.Id}, Hash={currentBorderNode.GetHashCode()}");
                                return HitTestResultBehavior.Stop;
                            }
                            current = VisualTreeHelper.GetParent(current);
                        }

                        return HitTestResultBehavior.Continue;
                    },
                    new PointHitTestParameters(mousePosition));

                _viewModel?.AddLog($"[Canvas] HitTest共检测到{hitTestCount}个元素");
                _viewModel?.AddLog($"[Canvas] 最终目标节点: {targetNode?.Name ?? "null"}");

                // 检查是否找到目标节点
                if (targetNode != null && targetNode != _dragConnectionSourceNode)
                {
                    _viewModel?.AddLog($"[Canvas] ✓ 找到有效目标节点，检查连接是否已存在");
                    _viewModel?.AddLog($"[拖拽连接] 找到目标节点: {targetNode.Name}");

                    // 检查连接是否已存在
                    var selectedTab = _viewModel?.WorkflowTabViewModel.SelectedTab;
                    if (selectedTab != null)
                    {
                        _viewModel?.AddLog($"[Canvas] SelectedTab不为null，当前连接数: {selectedTab.WorkflowConnections.Count}");

                        var existingConnection = selectedTab.WorkflowConnections.FirstOrDefault(c =>
                            c.SourceNodeId == _dragConnectionSourceNode.Id && c.TargetNodeId == targetNode.Id);

                        if (existingConnection == null)
                        {
                            _viewModel?.AddLog($"[Canvas] ✓ 连接不存在，准备创建新连接");
                            // 创建新连接
                            CreateConnection(_dragConnectionSourceNode, targetNode);
                            _viewModel?.AddLog($"[拖拽连接] ✓ 连接创建成功: {_dragConnectionSourceNode.Name} -> {targetNode.Name}");
                            _viewModel?.AddLog($"[Canvas] ✓✓ 连接创建完成");
                        }
                        else
                        {
                            _viewModel?.AddLog($"[拖拽连接] ❌ 连接已存在: {_dragConnectionSourceNode.Name} -> {targetNode.Name}");
                            _viewModel?.AddLog($"[Canvas] ❌ 连接已存在，ID={existingConnection.Id}");
                        }
                    }
                    else
                    {
                        _viewModel?.AddLog($"[Canvas] ❌ SelectedTab为null");
                    }
                }
                else
                {
                    if (targetNode == null)
                    {
                        _viewModel?.AddLog($"[拖拽连接] ❌ 未找到目标节点");
                        _viewModel?.AddLog($"[Canvas] ❌ 目标节点为null");
                    }
                    else
                    {
                        _viewModel?.AddLog($"[拖拽连接] ❌ 目标节点与源节点相同");
                        _viewModel?.AddLog($"[Canvas] ❌ 目标节点与源节点相同: {_dragConnectionSourceNode?.Name}");
                    }
                }

                // 重置拖拽状态
                WorkflowCanvas.ReleaseMouseCapture();
                IsDraggingConnection = false;
                _dragConnectionSourceNode = null;
                _viewModel?.AddLog($"[Canvas] ✓ 重置拖拽状态完成");
                _viewModel?.AddLog($"[Canvas] ========== WorkflowCanvas_PreviewMouseLeftButtonUp 完成（创建连接） ==========");
                return;
            }

            if (!_isBoxSelecting)
            {
                _viewModel?.AddLog($"[Canvas] 不在框选状态，直接返回");
                return;
            }

            _isBoxSelecting = false;
            _viewModel?.AddLog($"[Canvas] 结束框选模式");


            // 结束框选
            SelectionBox?.EndSelection();
            WorkflowCanvas.ReleaseMouseCapture();

            // 记录选中节点的初始位置（用于批量移动）
            RecordSelectedNodesPositions();

            e.Handled = true;
            _viewModel?.AddLog($"[Canvas] ========== WorkflowCanvas_PreviewMouseLeftButtonUp 完成（框选） ==========");
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

        #endregion

        #region 拖放事件

        private void WorkflowCanvas_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("ToolItem"))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }

        private void WorkflowCanvas_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("ToolItem"))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }

        private void WorkflowCanvas_DragLeave(object sender, DragEventArgs e)
        {
            // 可选：添加离开画布时的视觉效果
        }

        private void WorkflowCanvas_Drop(object sender, DragEventArgs e)
        {
            try
            {
                if (e.Data.GetData("ToolItem") is Models.ToolItem tool && _viewModel != null)
                {
                    var position = e.GetPosition(WorkflowCanvas);

                    // 创建新节点，使用ToolId作为AlgorithmType
                    var node = new WorkflowNode(
                        Guid.NewGuid().ToString(),
                        tool.Name,
                        tool.ToolId
                    );

                    // 设置拖放位置(居中放置,节点大小140x90)
                    var x = Math.Max(0, position.X - 70);
                    var y = Math.Max(0, position.Y - 45);
                    node.Position = new System.Windows.Point(x, y);

                    // 添加到当前选中的工作流
                    if (_viewModel.WorkflowTabViewModel.SelectedTab != null)
                    {
                        _viewModel.WorkflowTabViewModel.SelectedTab.WorkflowNodes.Add(node);
                        _viewModel.StatusText = $"添加节点: {node.Name}";
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"添加节点时出错: {ex.Message}", "错误",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        #endregion

        #region 辅助方法

        #region 智能路径计算

        /// <summary>
        /// 计算智能直角折线路径（与实际连接使用相同的逻辑）
        /// </summary>
        private List<Point> CalculateSmartPath(Point start, Point end)
        {
            _viewModel?.AddLog($"[CalculateSmartPath] ========== 路径计算开始 ==========");
            _viewModel?.AddLog($"[CalculateSmartPath] 起点: X={start.X:F2}, Y={start.Y:F2}");
            _viewModel?.AddLog($"[CalculateSmartPath] 终点: X={end.X:F2}, Y={end.Y:F2}");

            var points = new List<Point>();
            double deltaX = Math.Abs(end.X - start.X);
            double deltaY = Math.Abs(end.Y - start.Y);

            bool isHorizontal = deltaX > deltaY;
            _viewModel?.AddLog($"[CalculateSmartPath] deltaX={deltaX:F2}, deltaY={deltaY:F2}");
            _viewModel?.AddLog($"[CalculateSmartPath] 判断: {(isHorizontal ? "水平" : "垂直")}折线 (deltaX > deltaY: {isHorizontal})");

            if (isHorizontal)
            {
                // 水平方向：先水平移动到中间点，再垂直移动到目标Y，最后水平移动到目标X
                double midX = (start.X + end.X) / 2;
                points.Add(new Point(midX, start.Y));
                points.Add(new Point(midX, end.Y));
                _viewModel?.AddLog($"[CalculateSmartPath] 水平模式 - 中间点X: {midX:F2}");
                _viewModel?.AddLog($"[CalculateSmartPath] 第1个转折点: X={midX:F2}, Y={start.Y:F2}");
                _viewModel?.AddLog($"[CalculateSmartPath] 第2个转折点: X={midX:F2}, Y={end.Y:F2}");
            }
            else
            {
                // 垂直方向：先垂直移动到中间点，再水平移动到目标X，最后垂直移动到目标Y
                double midY = (start.Y + end.Y) / 2;
                points.Add(new Point(start.X, midY));
                points.Add(new Point(end.X, midY));
                _viewModel?.AddLog($"[CalculateSmartPath] 垂直模式 - 中间点Y: {midY:F2}");
                _viewModel?.AddLog($"[CalculateSmartPath] 第1个转折点: X={start.X:F2}, Y={midY:F2}");
                _viewModel?.AddLog($"[CalculateSmartPath] 第2个转折点: X={end.X:F2}, Y={midY:F2}");
            }

            points.Add(end);
            _viewModel?.AddLog($"[CalculateSmartPath] 最终点: X={end.X:F2}, Y={end.Y:F2}");
            _viewModel?.AddLog($"[CalculateSmartPath] ✓ 路径计算完成，共{points.Count}个点");
            _viewModel?.AddLog($"[CalculateSmartPath] ========== 路径计算完成 ==========");
            return points;
        }

        /// <summary>
        /// 获取节点的连接点位置
        /// </summary>
        private Point GetPortPosition(WorkflowNode node, Point clickPoint)
        {
            _viewModel?.AddLog($"[GetPortPosition] ========== 获取连接点位置 ==========");
            _viewModel?.AddLog($"[GetPortPosition] 节点: {node.Name}, ID={node.Id}");
            _viewModel?.AddLog($"[GetPortPosition] 点击点: X={clickPoint.X:F2}, Y={clickPoint.Y:F2}");
            _viewModel?.AddLog($"[GetPortPosition] 节点位置: X={node.Position.X:F2}, Y={node.Position.Y:F2}");

            // 计算点击点相对于节点中心的偏移
            double nodeCenterX = node.Position.X + 70;  // 节点宽度的一半
            double nodeCenterY = node.Position.Y + 45;  // 节点高度的一半
            double offsetX = clickPoint.X - nodeCenterX;
            double offsetY = clickPoint.Y - nodeCenterY;

            _viewModel?.AddLog($"[GetPortPosition] 节点中心: X={nodeCenterX:F2}, Y={nodeCenterY:F2}");
            _viewModel?.AddLog($"[GetPortPosition] 相对偏移: offsetX={offsetX:F2}, offsetY={offsetY:F2}");

            // 判断点击的是哪个连接点
            Point portPosition;
            if (Math.Abs(offsetX) > Math.Abs(offsetY))
            {
                // 水平方向（左右）
                if (offsetX > 0)
                {
                    portPosition = node.RightPortPosition;
                    _viewModel?.AddLog($"[GetPortPosition] 判断: 右侧端口");
                }
                else
                {
                    portPosition = node.LeftPortPosition;
                    _viewModel?.AddLog($"[GetPortPosition] 判断: 左侧端口");
                }
            }
            else
            {
                // 垂直方向（上下）
                if (offsetY > 0)
                {
                    portPosition = node.BottomPortPosition;
                    _viewModel?.AddLog($"[GetPortPosition] 判断: 底部端口");
                }
                else
                {
                    portPosition = node.TopPortPosition;
                    _viewModel?.AddLog($"[GetPortPosition] 判断: 顶部端口");
                }
            }

            _viewModel?.AddLog($"[GetPortPosition] 返回端口位置: X={portPosition.X:F2}, Y={portPosition.Y:F2}");
            _viewModel?.AddLog($"[GetPortPosition] ========== 获取连接点位置完成 ==========");

            // 根据点击的连接点确定实际连接位置
            return portPosition;
        }

        #endregion

        /// <summary>
        /// 查找指定类型的首个子元素
        /// </summary>
        private T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null)
                return null;

            if (parent is T child)
                return child;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var found = FindVisualChild<T>(VisualTreeHelper.GetChild(parent, i));
                if (found != null)
                    return found;
            }

            return null;
        }

        /// <summary>
        /// 在视觉树中查找指定类型的所有子元素
        /// </summary>
        private List<T> FindAllVisualChildren<T>(DependencyObject parent) where T : DependencyObject
        {
            var results = new List<T>();

            if (parent == null)
            {
                return results;
            }

            int childCount = VisualTreeHelper.GetChildrenCount(parent);

            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T t)
                {
                    results.Add(t);
                }

                results.AddRange(FindAllVisualChildren<T>(child));
            }

            return results;
        }

        /// <summary>
        /// 创建节点连接
        /// </summary>
        private void CreateConnection(WorkflowNode sourceNode, WorkflowNode targetNode)
        {
            _viewModel?.AddLog($"[CreateConnection] ========== CreateConnection 开始 ==========");
            _viewModel?.AddLog($"[CreateConnection] 源节点: {sourceNode.Name}, ID={sourceNode.Id}, Hash={sourceNode.GetHashCode()}");
            _viewModel?.AddLog($"[CreateConnection] 源节点位置: X={sourceNode.Position.X:F2}, Y={sourceNode.Position.Y:F2}");
            _viewModel?.AddLog($"[CreateConnection] 源节点各端口位置:");
            _viewModel?.AddLog($"[CreateConnection]   - TopPort: {sourceNode.TopPortPosition}");
            _viewModel?.AddLog($"[CreateConnection]   - BottomPort: {sourceNode.BottomPortPosition}");
            _viewModel?.AddLog($"[CreateConnection]   - LeftPort: {sourceNode.LeftPortPosition}");
            _viewModel?.AddLog($"[CreateConnection]   - RightPort: {sourceNode.RightPortPosition}");
            _viewModel?.AddLog($"[CreateConnection] 源节点位置: X={sourceNode.Position.X:F2}, Y={sourceNode.Position.Y:F2}");
            _viewModel?.AddLog($"[CreateConnection] 源节点各端口位置:");
            _viewModel?.AddLog($"[CreateConnection]   - TopPort: {sourceNode.TopPortPosition}");
            _viewModel?.AddLog($"[CreateConnection]   - BottomPort: {sourceNode.BottomPortPosition}");
            _viewModel?.AddLog($"[CreateConnection]   - LeftPort: {sourceNode.LeftPortPosition}");
            _viewModel?.AddLog($"[CreateConnection]   - RightPort: {sourceNode.RightPortPosition}");
            _viewModel?.AddLog($"[CreateConnection] 目标节点: {targetNode.Name}, ID={targetNode.Id}, Hash={targetNode.GetHashCode()}");
            _viewModel?.AddLog($"[CreateConnection] 目标节点位置: X={targetNode.Position.X:F2}, Y={targetNode.Position.Y:F2}");
            _viewModel?.AddLog($"[CreateConnection] 目标节点各端口位置:");
            _viewModel?.AddLog($"[CreateConnection]   - TopPort: {targetNode.TopPortPosition}");
            _viewModel?.AddLog($"[CreateConnection]   - BottomPort: {targetNode.BottomPortPosition}");
            _viewModel?.AddLog($"[CreateConnection]   - LeftPort: {targetNode.LeftPortPosition}");
            _viewModel?.AddLog($"[CreateConnection]   - RightPort: {targetNode.RightPortPosition}");

            var selectedTab = _viewModel?.WorkflowTabViewModel.SelectedTab;
            if (selectedTab == null)
            {
                _viewModel?.AddLog($"[CreateConnection] ❌ SelectedTab为null");
                return;
            }

            if (selectedTab.WorkflowConnections == null)
            {
                _viewModel?.AddLog($"[CreateConnection] ❌ WorkflowConnections为null");
                return;
            }

            var connectionId = $"conn_{Guid.NewGuid().ToString("N")[..8]}";
            var newConnection = new WorkflowConnection(connectionId, sourceNode.Id, targetNode.Id);
            _viewModel?.AddLog($"[CreateConnection] 创建连接对象，ID: {connectionId}, Hash={newConnection.GetHashCode()}");
            _viewModel?.AddLog($"[CreateConnection] 添加前当前连接数: {selectedTab.WorkflowConnections.Count}");

            // 智能选择连接点位置
            Point sourcePos, targetPos;
            var deltaX = targetNode.Position.X - sourceNode.Position.X;
            var deltaY = targetNode.Position.Y - sourceNode.Position.Y;
            _viewModel?.AddLog($"[CreateConnection] 节点位置差: deltaX={deltaX:F2}, deltaY={deltaY:F2}");
            _viewModel?.AddLog($"[CreateConnection] deltaX绝对值: {Math.Abs(deltaX):F2}, deltaY绝对值: {Math.Abs(deltaY):F2}");
            _viewModel?.AddLog($"[CreateConnection] 源节点位置: X={sourceNode.Position.X:F2}, Y={sourceNode.Position.Y:F2}");
            _viewModel?.AddLog($"[CreateConnection] 目标节点位置: X={targetNode.Position.X:F2}, Y={targetNode.Position.Y:F2}");

            if (Math.Abs(deltaX) > Math.Abs(deltaY))
            {
                // 水平方向
                _viewModel?.AddLog($"[CreateConnection] 判断: 水平方向连接 (|deltaX| > |deltaY|)");
                if (deltaX > 0)
                {
                    sourcePos = sourceNode.RightPortPosition;
                    targetPos = targetNode.LeftPortPosition;
                    _viewModel?.AddLog($"[CreateConnection] 选择水平连接: 源节点右侧 -> 目标节点左侧");
                }
                else
                {
                    sourcePos = sourceNode.LeftPortPosition;
                    targetPos = targetNode.RightPortPosition;
                    _viewModel?.AddLog($"[CreateConnection] 选择水平连接: 源节点左侧 -> 目标节点右侧");
                }
            }
            else
            {
                // 垂直方向
                _viewModel?.AddLog($"[CreateConnection] 判断: 垂直方向连接 (|deltaY| >= |deltaX|)");
                if (deltaY > 0)
                {
                    sourcePos = sourceNode.BottomPortPosition;
                    targetPos = targetNode.TopPortPosition;
                    _viewModel?.AddLog($"[CreateConnection] 选择垂直连接: 源节点底部 -> 目标节点顶部");
                }
                else
                {
                    sourcePos = sourceNode.TopPortPosition;
                    targetPos = targetNode.BottomPortPosition;
                    _viewModel?.AddLog($"[CreateConnection] 选择垂直连接: 源节点顶部 -> 目标节点底部");
                }
            }

            _viewModel?.AddLog($"[CreateConnection] 最终选择的源端口位置: {sourcePos}");
            _viewModel?.AddLog($"[CreateConnection] 最终选择的目标端口位置: {targetPos}");
            _viewModel?.AddLog($"[CreateConnection] 连接线距离: X={Math.Abs(targetPos.X - sourcePos.X):F2}, Y={Math.Abs(targetPos.Y - sourcePos.Y):F2}");

            newConnection.SourcePosition = sourcePos;
            newConnection.TargetPosition = targetPos;
            _viewModel?.AddLog($"[CreateConnection] 设置连接对象SourcePosition: {newConnection.SourcePosition}");
            _viewModel?.AddLog($"[CreateConnection] 设置连接对象TargetPosition: {newConnection.TargetPosition}");

            selectedTab.WorkflowConnections.Add(newConnection);
            _viewModel?.AddLog($"[CreateConnection] ✓ 连接已添加到集合，当前连接数: {selectedTab.WorkflowConnections.Count}");
            _viewModel?.AddLog($"[CreateConnection] 新连接是否在集合中: {selectedTab.WorkflowConnections.Contains(newConnection)}");
            _viewModel?.AddLog($"[CreateConnection] 集合中最后一个连接的ID: {selectedTab.WorkflowConnections.LastOrDefault()?.Id ?? "null"}");

            _viewModel!.StatusText = $"成功连接: {sourceNode.Name} -> {targetNode.Name}";
            _viewModel.AddLog($"[CreateConnection] ✓ 连接创建完成，当前总连接数: {selectedTab.WorkflowConnections.Count}");
            _viewModel?.AddLog($"[CreateConnection] ========== CreateConnection 完成 ==========");
        }

        /// <summary>
        /// Path元素加载事件 - 监控连接线路径创建
        /// </summary>
        private void Path_Loaded(object sender, RoutedEventArgs e)
        {
            _viewModel?.AddLog($"[Path_Loaded] ========== Path加载开始 ==========");
            if (sender is Path path)
            {
                _viewModel?.AddLog($"[Path_Loaded] Path对象哈希码: {path.GetHashCode()}");
                _viewModel?.AddLog($"[Path_Loaded] Path.DataContext类型: {path.DataContext?.GetType().Name ?? "null"}");
                _viewModel?.AddLog($"[Path_Loaded] Path.Stretch: {path.Stretch}");
                _viewModel?.AddLog($"[Path_Loaded] Path.Stroke: {path.Stroke}");
                _viewModel?.AddLog($"[Path_Loaded] Path.StrokeThickness: {path.StrokeThickness}");
                _viewModel?.AddLog($"[Path_Loaded] Path.Data: {path.Data?.GetType().Name ?? "null"}");

                var connection = path.DataContext as WorkflowConnection;
                if (connection != null)
                {
                    _viewModel?.AddLog($"[Path_Loaded] ✓ Path加载完成，连接ID: {connection.Id}");
                    _viewModel?.AddLog($"[Path_Loaded] 连接对象哈希码: {connection.GetHashCode()}");
                    _viewModel?.AddLog($"[Path_Loaded] 源节点ID: {connection.SourceNodeId}");
                    _viewModel?.AddLog($"[Path_Loaded] 目标节点ID: {connection.TargetNodeId}");
                    _viewModel?.AddLog($"[Path_Loaded] 源位置: X={connection.SourcePosition.X:F2}, Y={connection.SourcePosition.Y:F2}");
                    _viewModel?.AddLog($"[Path_Loaded] 目标位置: X={connection.TargetPosition.X:F2}, Y={connection.TargetPosition.Y:F2}");
                    _viewModel?.AddLog($"[Path_Loaded] Data属性类型: {path.Data?.GetType().Name ?? "null"}");
                    _viewModel?.AddLog($"[Path_Loaded] Data属性是否为PathGeometry: {path.Data is PathGeometry}");

                    if (path.Data is PathGeometry geom)
                    {
                        _viewModel?.AddLog($"[Path_Loaded] PathGeometry.Figures数量: {geom.Figures.Count}");
                        _viewModel?.AddLog($"[Path_Loaded] PathGeometry.Bounds: {geom.Bounds}");
                        if (geom.Figures.Count > 0)
                        {
                            var figure = geom.Figures[0];
                            _viewModel?.AddLog($"[Path_Loaded] PathFigure.Segments数量: {figure.Segments.Count}");
                            _viewModel?.AddLog($"[Path_Loaded] PathFigure.StartPoint: X={figure.StartPoint.X:F2}, Y={figure.StartPoint.Y:F2}");
                            _viewModel?.AddLog($"[Path_Loaded] PathFigure.IsClosed: {figure.IsClosed}");
                            _viewModel?.AddLog($"[Path_Loaded] PathFigure.IsFilled: {figure.IsFilled}");

                            // 详细列出所有段
                            for (int i = 0; i < figure.Segments.Count; i++)
                            {
                                var segment = figure.Segments[i];
                                _viewModel?.AddLog($"[Path_Loaded] Segment #{i}: {segment.GetType().Name}");
                                if (segment is LineSegment line)
                                {
                                    _viewModel?.AddLog($"[Path_Loaded]   LineSegment.Point: X={line.Point.X:F2}, Y={line.Point.Y:F2}");
                                }
                                else if (segment is PolyLineSegment polyLine)
                                {
                                    _viewModel?.AddLog($"[Path_Loaded]   PolyLineSegment.Points数量: {polyLine.Points.Count}");
                                    for (int j = 0; j < polyLine.Points.Count && j < 5; j++)
                                    {
                                        _viewModel?.AddLog($"[Path_Loaded]     Point #{j}: X={polyLine.Points[j].X:F2}, Y={polyLine.Points[j].Y:F2}");
                                    }
                                }
                                else if (segment is BezierSegment bezier)
                                {
                                    _viewModel?.AddLog($"[Path_Loaded]   BezierSegment.Point1: {bezier.Point1}");
                                    _viewModel?.AddLog($"[Path_Loaded]   BezierSegment.Point2: {bezier.Point2}");
                                    _viewModel?.AddLog($"[Path_Loaded]   BezierSegment.Point3: {bezier.Point3}");
                                }
                            }
                        }
                        else
                        {
                            _viewModel?.AddLog($"[Path_Loaded] ⚠ PathGeometry.Figures为空！");
                        }
                    }
                    else
                    {
                        _viewModel?.AddLog($"[Path_Loaded] ⚠ Path.Data不是PathGeometry！");
                    }
                }
                else
                {
                    _viewModel?.AddLog($"[Path_Loaded] ❌ Path的DataContext不是WorkflowConnection，而是: {path.DataContext?.GetType().Name ?? "null"}");
                }
            }
            else
            {
                _viewModel?.AddLog($"[Path_Loaded] ❌ Sender不是Path，而是: {sender?.GetType().Name ?? "null"}");
            }
            _viewModel?.AddLog($"[Path_Loaded] ========== Path加载完成 ==========");
        }

        /// <summary>
        /// Path的DataContext变化事件 - 监控连接数据更新
        /// </summary>
        private void Path_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            _viewModel?.AddLog($"[Path_DataContextChanged] ========== DataContext变化 ==========");
            if (sender is Path path)
            {
                _viewModel?.AddLog($"[Path_DataContextChanged] Path哈希码: {path.GetHashCode()}");
                _viewModel?.AddLog($"[Path_DataContextChanged] OldValue类型: {e.OldValue?.GetType().Name ?? "null"}");
                _viewModel?.AddLog($"[Path_DataContextChanged] NewValue类型: {e.NewValue?.GetType().Name ?? "null"}");
                
                if (e.NewValue is WorkflowConnection newConn)
                {
                    _viewModel?.AddLog($"[Path_DataContextChanged] ✓ 新连接ID: {newConn.Id}");
                    _viewModel?.AddLog($"[Path_DataContextChanged] 源位置: {newConn.SourcePosition}");
                    _viewModel?.AddLog($"[Path_DataContextChanged] 目标位置: {newConn.TargetPosition}");
                }
            }
            _viewModel?.AddLog($"[Path_DataContextChanged] ========== DataContext变化完成 ==========");
        }

        #endregion
    }
}
