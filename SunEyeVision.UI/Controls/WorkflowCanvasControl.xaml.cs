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
        private string? _dragConnectionSourcePort = null; // 记录拖拽开始时的源端口
        private Border? _highlightedTargetBorder = null; // 高亮的目标节点Border（用于恢复原始样式）
        private Ellipse? _highlightedTargetPort = null; // 高亮的目标端口（Ellipse）
        private int _dragMoveCounter = 0; // 拖拽移动计数器，用于减少日志输出频率
        private int _highlightCounter = 0; // 端口高亮计数器，用于减少日志输出频率
        private string? _lastHighlightedPort = null; // 上次高亮的端口名称
        private string? _directHitTargetPort = null; // 用户直接命中的目标端口名称

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
        public ViewModels.WorkflowTabViewModel? CurrentWorkflowTab
        {
            get
            {
                // 确保获取到最新的SelectedTab
                if (_viewModel != null && _viewModel.WorkflowTabViewModel != null)
                {
                    return _viewModel.WorkflowTabViewModel.SelectedTab;
                }
                return null;
            }
        }

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
                            _viewModel?.AddLog($"[WorkflowCanvas] ✓ SelectedTab: ID={selectedTab.Id}, Name={selectedTab.Name}");
                            _viewModel?.AddLog($"[WorkflowCanvas] 当前节点数: {selectedTab.WorkflowNodes.Count}, 连接数: {selectedTab.WorkflowConnections.Count}");
                        }
                    }

                    _viewModel.AddLog($"[WorkflowCanvas] ✓ WorkflowCanvasControl 已加载，可以开始拖拽创建连接");
                }
            }

            // 初始化智能路径转换器的节点集合
            if (CurrentWorkflowTab != null)
            {
                Converters.SmartPathConverter.Nodes = CurrentWorkflowTab.WorkflowNodes;
                _viewModel?.AddLog($"[WorkflowCanvas] ✓ 已初始化SmartPathConverter的节点集合，共{CurrentWorkflowTab.WorkflowNodes.Count}个节点");
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
                        var offsets = new List<System.Windows.Point>();
                        for (int i = 0; i < selectedNodes.Count; i++)
                        {
                            if (_selectedNodesInitialPositions != null && i < _selectedNodesInitialPositions.Length)
                            {
                                offsets.Add(new System.Windows.Point(
                                    selectedNodes[i].Position.X - _selectedNodesInitialPositions[i].X,
                                    selectedNodes[i].Position.Y - _selectedNodesInitialPositions[i].Y
                                ));
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
            // 获取节点对象（支持 Border 或 Ellipse 作为 sender）
            WorkflowNode? targetNode = null;

            if (sender is Border border && border.Tag is WorkflowNode clickedNodeFromBorder)
            {
                targetNode = clickedNodeFromBorder;
            }
            else if (sender is Ellipse ellipse && ellipse.Tag is WorkflowNode clickedNodeFromEllipse)
            {
                targetNode = clickedNodeFromEllipse;

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
                return;
            }

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
            _viewModel?.AddLog("[拖拽连接] ========== 开始拖拽连接 ==========");

            if (sender is not Ellipse port || port.Tag is not WorkflowNode node)
            {
                _viewModel?.AddLog("[拖拽连接] ❌ 条件检查失败");
                return;
            }

            e.Handled = true;

            _viewModel?.AddLog($"[拖拽连接] 源节点: {node.Name} (ID={node.Id})");

            // 选中当前节点
            if (_viewModel?.WorkflowTabViewModel.SelectedTab != null)
            {
                foreach (var n in _viewModel.WorkflowTabViewModel.SelectedTab.WorkflowNodes)
                {
                    n.IsSelected = (n == node);
                }
                _viewModel.SelectedNode = node;
            }


            // 开始拖拽连接
            IsDraggingConnection = true;
            _dragConnectionSourceNode = node;
            _dragMoveCounter = 0; // 重置移动计数器
            _highlightCounter = 0; // 重置高亮计数器
            _lastHighlightedPort = null; // 重置上次高亮的端口
            _directHitTargetPort = null; // 重置直接命中的目标端口


            // 获取连接点在画布上的位置
            _dragConnectionStartPoint = e.GetPosition(WorkflowCanvas);

            // 判断点击的是哪个端口并设置起始点
            string? clickedPort = DetermineClickedPort(node, _dragConnectionStartPoint);
            _viewModel?.AddLog($"[拖拽连接] 源端口: {clickedPort}, 起始点: {_dragConnectionStartPoint}");

            // 显示临时连接线
            if (TempConnectionGeometry != null)
            {
                TempConnectionGeometry.Figures.Clear();
                var pathFigure = new PathFigure { StartPoint = _dragConnectionStartPoint, IsClosed = false };
                pathFigure.Segments.Add(new LineSegment(_dragConnectionStartPoint, true));
                TempConnectionGeometry.Figures.Add(pathFigure);
            }

            if (TempConnectionLine != null)
            {
                TempConnectionLine.Visibility = Visibility.Visible;
            }

            // 捕获鼠标
            WorkflowCanvas.CaptureMouse();

            _viewModel?.AddLog("[拖拽连接] ✓ 拖拽连接初始化完成");
        }

        /// <summary>
        /// 连接点鼠标释放 - 结束拖拽并创建连接
        /// </summary>
        private void Port_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _viewModel?.AddLog($"[拖拽连接] ========== 鼠标释放（Port） ==========");

            if (!_isDraggingConnection || _dragConnectionSourceNode == null)
            {
                return;
            }

            // 在处理任何操作之前立即标记事件为已处理，阻止传播
            e.Handled = true;

            if (sender is Ellipse port)
            {
                port.ReleaseMouseCapture();
            }

            // 隐藏临时连接线
            TempConnectionLine.Visibility = Visibility.Collapsed;

            // 查找鼠标位置下的目标节点
            var mousePosition = e.GetPosition(WorkflowCanvas);
            _viewModel?.AddLog($"[拖拽连接] 鼠标位置: {mousePosition}");

            WorkflowNode? targetNode = null;
            Border? targetBorder = null;
            int hitTestCount = 0;

            // 使用 HitTest 查找鼠标位置下的所有元素
            VisualTreeHelper.HitTest(WorkflowCanvas, null,
                result =>
                {
                    hitTestCount++;

                    // 如果找到 Border 且带有 WorkflowNode Tag，记录下来
                    if (result.VisualHit is Border hitBorder && hitBorder.Tag is WorkflowNode hitNode)
                    {
                        targetNode = hitNode;
                        targetBorder = hitBorder;
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
                            return HitTestResultBehavior.Stop;
                        }
                        current = VisualTreeHelper.GetParent(current);
                    }

                    return HitTestResultBehavior.Continue;
                },
                new PointHitTestParameters(mousePosition));

            _viewModel?.AddLog($"[拖拽连接] HitTest检测到{hitTestCount}个元素");
            _viewModel?.AddLog($"[拖拽连接] 源节点: {_dragConnectionSourceNode.Name}, 目标节点: {targetNode?.Name ?? "null"}");

            // 检查是否找到目标节点
            if (targetNode != null && targetNode != _dragConnectionSourceNode)
            {
                // 检查连接是否已存在
                var selectedTab = _viewModel?.WorkflowTabViewModel.SelectedTab;
                if (selectedTab != null)
                {
                    _viewModel?.AddLog($"[拖拽连接] 当前连接数: {selectedTab.WorkflowConnections.Count}");

                    var existingConnection = selectedTab.WorkflowConnections.FirstOrDefault(c =>
                        c.SourceNodeId == _dragConnectionSourceNode.Id && c.TargetNodeId == targetNode.Id);

                    if (existingConnection == null)
                    {
                        _viewModel?.AddLog($"[拖拽连接] 创建连接: {_dragConnectionSourceNode.Name} -> {targetNode.Name}");
                        // 创建新连接
                        CreateConnection(_dragConnectionSourceNode, targetNode);
                        _viewModel?.AddLog($"[拖拽连接] ✓ 连接创建完成，新连接数: {selectedTab.WorkflowConnections.Count}");
                    }
                    else
                    {
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
                }
                else
                {
                    _viewModel?.AddLog($"[拖拽连接] ❌ 目标节点与源节点相同");
                }
            }

            // 重置拖拽状态（使用属性设置，会自动隐藏所有连接点）
            IsDraggingConnection = false;
            _dragConnectionSourceNode = null;
            ClearTargetPortHighlight(); // 清除端口高亮
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
                _dragMoveCounter++;

                if (TempConnectionGeometry != null && _dragConnectionSourceNode != null)
                {
                    var currentPoint = e.GetPosition(WorkflowCanvas);

                    // 每20次移动事件才输出一次日志
                    if (_dragMoveCounter % 20 == 0 || _dragMoveCounter == 1)
                    {
                        _viewModel?.AddLog($"[拖拽连接-Move] 移动次数: {_dragMoveCounter}, 鼠标位置: ({currentPoint.X:F1}, {currentPoint.Y:F1})");
                    }

                    // 获取源节点的连接点位置
                    var sourcePort = GetPortPosition(_dragConnectionSourceNode, _dragConnectionStartPoint);

                    // 计算智能直角折线路径
                    var pathPoints = CalculateSmartPath(sourcePort, currentPoint);

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
                    }

                    // 动态高亮目标端口
                    var hitNodes = new List<(WorkflowNode node, Border border, double distance)>();
                    var hitPorts = new List<(Ellipse port, string portName)>();

                    VisualTreeHelper.HitTest(WorkflowCanvas, null,
                        result =>
                        {
                            // 检查是否命中端口
                            if (result.VisualHit is Ellipse hitEllipse)
                            {
                                var ellipseName = hitEllipse.Name;
                                if (!string.IsNullOrEmpty(ellipseName) && (ellipseName == "LeftPortEllipse" ||
                                    ellipseName == "RightPortEllipse" ||
                                    ellipseName == "TopPortEllipse" ||
                                    ellipseName == "BottomPortEllipse"))
                                {
                                    string portName = ellipseName.Replace("Ellipse", "");
                                    hitPorts.Add((hitEllipse, portName));
                                }
                            }

                            if (result.VisualHit is Border hitBorder && hitBorder.Tag is WorkflowNode hitNode)
                            {
                                var nodeCenterX = hitNode.Position.X + 70;
                                var nodeCenterY = hitNode.Position.Y + 45;
                                double distance = Math.Sqrt(Math.Pow(currentPoint.X - nodeCenterX, 2) + Math.Pow(currentPoint.Y - nodeCenterY, 2));
                                hitNodes.Add((hitNode, hitBorder, distance));
                            }
                            DependencyObject? current = result.VisualHit as DependencyObject;
                            for (int depth = 0; current != null && depth < 10; depth++)
                            {
                                if (current is Border currentBorder && currentBorder.Tag is WorkflowNode currentBorderNode)
                                {
                                    var nodeCenterX = currentBorderNode.Position.X + 70;
                                    var nodeCenterY = currentBorderNode.Position.Y + 45;
                                    double distance = Math.Sqrt(Math.Pow(currentPoint.X - nodeCenterX, 2) + Math.Pow(currentPoint.Y - nodeCenterY, 2));
                                    hitNodes.Add((currentBorderNode, currentBorder, distance));
                                    break;
                                }
                                current = VisualTreeHelper.GetParent(current);
                            }
                            return HitTestResultBehavior.Continue;
                        },
                        new PointHitTestParameters(currentPoint));

                    // 优先处理命中的端口（需要排除源节点的端口）
                    if (hitPorts.Count > 0)
                    {
                        var targetPortName = hitPorts[0].portName;

                        // 找到端口所属的节点
                        Border? portBorder = null;
                        foreach (var hitPort in hitPorts)
                        {
                            DependencyObject? parent = hitPort.port;
                            while (parent != null)
                            {
                                if (parent is Border border && border.Tag is WorkflowNode node)
                                {
                                    if (node != _dragConnectionSourceNode)
                                    {
                                        portBorder = border;
                                        // 只在端口变化时才高亮和记录
                                        if (_lastHighlightedPort != targetPortName)
                                        {
                                            if (_highlightCounter % 10 == 0)
                                            {
                                                _viewModel?.AddLog($"[Move] ✓ 命中端口: {targetPortName}, 节点: {node.Name}");
                                            }
                                            HighlightSpecificPort(border, targetPortName);
                                            _directHitTargetPort = targetPortName;
                                            _lastHighlightedPort = targetPortName;
                                        }
                                        break;
                                    }
                                }
                                parent = VisualTreeHelper.GetParent(parent);
                            }
                            if (portBorder != null) break;
                        }

                        if (portBorder == null)
                        {
                            // 命中的都是源节点的端口，清除高亮
                            if (_lastHighlightedPort != null)
                            {
                                ClearTargetPortHighlight();
                                _directHitTargetPort = null;
                                _lastHighlightedPort = null;
                            }
                        }
                    }
                    else if (hitNodes.Count > 0)
                    {
                        var nearest = hitNodes.OrderBy(n => n.distance).First();
                        if (nearest.node != _dragConnectionSourceNode)
                        {
                            HighlightTargetPort(nearest.border, _dragConnectionSourceNode);
                        }
                        else
                        {
                            ClearTargetPortHighlight();
                        }
                    }
                    else
                    {
                        ClearTargetPortHighlight();
                    }
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
            _viewModel?.AddLog($"[Canvas] ========== 鼠标释放（Canvas） ==========");
            _viewModel?.AddLog($"[Canvas] 拖拽连接: {_isDraggingConnection}, 框选: {_isBoxSelecting}");

            // 如果正在拖拽连接，尝试创建连接
            if (_isDraggingConnection)
            {
                e.Handled = true; // 阻止事件继续传播

                // 隐藏临时连接线
                TempConnectionLine.Visibility = Visibility.Collapsed;

                // 查找鼠标位置下的目标节点
                var mousePosition = e.GetPosition(WorkflowCanvas);

                // 清除之前的高亮
                if (_highlightedTargetBorder != null)
                {
                    _highlightedTargetBorder.Background = new SolidColorBrush(Colors.White);
                    _highlightedTargetBorder.BorderBrush = new SolidColorBrush(Colors.Transparent);
                    _highlightedTargetBorder.BorderThickness = new Thickness(0);
                    _highlightedTargetBorder = null;
                }

                // 收集所有命中的节点并选择最近的一个
                var hitNodes = new List<(WorkflowNode node, Border border, double distance)>();
                var hitPorts = new List<(Ellipse port, string portName, double distance)>(); // 新增：命中的端口列表
                int hitTestCount = 0;

                _viewModel?.AddLog($"[Canvas] 源节点: {_dragConnectionSourceNode?.Name}, 源端口: {_dragConnectionSourcePort}");
                _viewModel?.AddLog($"[Canvas] 鼠标位置: {mousePosition.X:F1},{mousePosition.Y:F1}");

                // 使用 HitTest 查找鼠标位置下的所有元素
                VisualTreeHelper.HitTest(WorkflowCanvas, null,
                    result =>
                    {
                        hitTestCount++;

                        // 新增：检查是否命中了端口
                        if (result.VisualHit is Ellipse hitEllipse)
                        {
                            var ellipseName = hitEllipse.Name;
                            _viewModel?.AddLog($"[Canvas HitTest] 命中Ellipse: {ellipseName}");

                            // 检查是否是端口
                            if (!string.IsNullOrEmpty(ellipseName) && (ellipseName == "LeftPortEllipse" ||
                                ellipseName == "RightPortEllipse" ||
                                ellipseName == "TopPortEllipse" ||
                                ellipseName == "BottomPortEllipse"))
                            {
                                // 提取端口名称
                                string portName = ellipseName.Replace("Ellipse", "");
                                var portCenterX = hitEllipse.RenderSize.Width / 2;
                                var portCenterY = hitEllipse.RenderSize.Height / 2;
                                var portPos = hitEllipse.PointToScreen(new Point(portCenterX, portCenterY));
                                var canvasPos = WorkflowCanvas.PointFromScreen(portPos);
                                double portDistance = Math.Sqrt(Math.Pow(mousePosition.X - canvasPos.X, 2) +
                                                                    Math.Pow(mousePosition.Y - canvasPos.Y, 2));

                                hitPorts.Add((hitEllipse, portName, portDistance));
                                _viewModel?.AddLog($"[Canvas HitTest] ✓ 命中端口: {portName}, 距离={portDistance:F1}");
                            }
                        }

                        // 如果找到 Border 且带有 WorkflowNode Tag，计算距离并记录
                        if (result.VisualHit is Border hitBorder && hitBorder.Tag is WorkflowNode hitNode)
                        {
                            var nodeCenterX = hitNode.Position.X + 70; // 节点宽度140，中心在70
                            var nodeCenterY = hitNode.Position.Y + 45; // 节点高度90，中心在45
                            double distance = Math.Sqrt(Math.Pow(mousePosition.X - nodeCenterX, 2) + Math.Pow(mousePosition.Y - nodeCenterY, 2));
                            hitNodes.Add((hitNode, hitBorder, distance));
                            _viewModel?.AddLog($"[Canvas HitTest] 命中节点Border: {hitNode.Name}, 距离中心={distance:F1}");
                        }

                        // 对于任何命中的元素，都向上查找带有WorkflowNode Tag的Border
                        DependencyObject? current = result.VisualHit as DependencyObject;
                        int depth = 0;
                        while (current != null && depth < 30)
                        {
                            depth++;
                            if (current is Border currentBorder && currentBorder.Tag is WorkflowNode currentBorderNode)
                            {
                                var nodeCenterX = currentBorderNode.Position.X + 70;
                                var nodeCenterY = currentBorderNode.Position.Y + 45;
                                double distance = Math.Sqrt(Math.Pow(mousePosition.X - nodeCenterX, 2) + Math.Pow(mousePosition.Y - nodeCenterY, 2));
                                hitNodes.Add((currentBorderNode, currentBorder, distance));
                                break;
                            }
                            current = VisualTreeHelper.GetParent(current);
                        }

                        return HitTestResultBehavior.Continue;
                    },
                    new PointHitTestParameters(mousePosition));

                if (hitPorts.Count > 0)
                {
                    _viewModel?.AddLog($"[Canvas] 检测到{hitPorts.Count}个端口");
                }

                // 选择距离鼠标最近的节点
                WorkflowNode? targetNode = null;
                Border? targetBorder = null;

                // 优先选择命中的端口
                if (hitPorts.Count > 0)
                {
                    var nearestPort = hitPorts.OrderBy(p => p.distance).First();
                    var targetPortEllipse = nearestPort.port;

                    // 找到端口所属的节点Border
                    DependencyObject? parent = targetPortEllipse;
                    while (parent != null)
                    {
                        if (parent is Border border && border.Tag is WorkflowNode node)
                        {
                            // 排除源节点
                            if (node != _dragConnectionSourceNode)
                            {
                                targetNode = node;
                                targetBorder = border;
                                _viewModel?.AddLog($"[Canvas] ✓ 直接命中端口: {nearestPort.portName}, 节点: {node.Name}");
                                _directHitTargetPort = nearestPort.portName;
                                break;
                            }
                        }
                        parent = VisualTreeHelper.GetParent(parent);
                    }

                    if (targetBorder != null)
                    {
                        HighlightSpecificPort(targetBorder, nearestPort.portName);
                    }
                }
                // 如果没有命中端口，则使用节点选择逻辑
                else if (hitNodes.Count > 0)
                {
                    var nearest = hitNodes.OrderBy(n => n.distance).First();
                    // 排除源节点
                    if (nearest.node != _dragConnectionSourceNode)
                    {
                        targetNode = nearest.node;
                        targetBorder = nearest.border;
                        _viewModel?.AddLog($"[Canvas] 找到{hitNodes.Count}个节点，最近: {targetNode.Name}");

                        // 高亮显示目标节点的端口（使用智能选择）
                        HighlightTargetPort(targetBorder, _dragConnectionSourceNode);
                    }
                }

                // 检查是否找到目标节点
                if (targetNode != null && targetNode != _dragConnectionSourceNode)
                {
                    // 确定源端口和目标端口
                    string sourcePort = _dragConnectionSourcePort ?? "RightPort";
                    string targetPort = _directHitTargetPort ?? (hitPorts.Count > 0 ? hitPorts.OrderBy(p => p.distance).First().portName : null);

                    // 检查相同连接点是否已存在连接
                    var selectedTab = _viewModel?.WorkflowTabViewModel.SelectedTab;
                    if (selectedTab != null)
                    {
                        var existingConnection = selectedTab.WorkflowConnections.FirstOrDefault(c =>
                            c.SourceNodeId == _dragConnectionSourceNode.Id &&
                            c.TargetNodeId == targetNode.Id &&
                            c.SourcePort == sourcePort &&
                            c.TargetPort == targetPort);

                        if (existingConnection == null)
                        {
                            _viewModel?.AddLog($"[Canvas] 创建连接: {_dragConnectionSourceNode.Name}({sourcePort}) -> {targetNode.Name}({targetPort})");

                            if (!string.IsNullOrEmpty(targetPort))
                            {
                                CreateConnectionWithSpecificPort(_dragConnectionSourceNode, targetNode, targetPort);
                            }
                            else
                            {
                                CreateConnection(_dragConnectionSourceNode, targetNode);
                            }
                        }
                        else
                        {
                            _viewModel?.AddLog($"[Canvas] ❌ 相同连接点已存在: {_dragConnectionSourceNode.Name}({sourcePort}) -> {targetNode.Name}({targetPort})");
                        }
                    }
                }
                else if (targetNode != null && targetNode == _dragConnectionSourceNode)
                {
                    _viewModel?.AddLog($"[Canvas] ❌ 不允许自连接: {_dragConnectionSourceNode.Name}");
                }

                // 重置拖拽状态
                WorkflowCanvas.ReleaseMouseCapture();
                IsDraggingConnection = false;
                _dragConnectionSourceNode = null;
                ClearTargetPortHighlight(); // 清除端口高亮
                _viewModel?.AddLog($"[Canvas] ========== Canvas_PreviewMouseLeftButtonUp 完成（创建连接） ==========");
                return;
            }

            if (!_isBoxSelecting)
            {
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
            _viewModel?.AddLog($"[Canvas] ========== Canvas_PreviewMouseLeftButtonUp 完成（框选） ==========");
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
            var points = new List<Point>();
            double deltaX = Math.Abs(end.X - start.X);
            double deltaY = Math.Abs(end.Y - start.Y);

            bool isHorizontal = deltaX > deltaY;

            if (isHorizontal)
            {
                // 水平方向：先水平移动到中间点，再垂直移动到目标Y，最后水平移动到目标X
                double midX = (start.X + end.X) / 2;
                points.Add(new Point(midX, start.Y));
                points.Add(new Point(midX, end.Y));
            }
            else
            {
                // 垂直方向：先垂直移动到中间点，再水平移动到目标X，最后垂直移动到目标Y
                double midY = (start.Y + end.Y) / 2;
                points.Add(new Point(start.X, midY));
                points.Add(new Point(end.X, midY));
            }

            points.Add(end);
            return points;
        }

        /// <summary>
        /// 判断点击的端口并设置起始点
        /// </summary>
        private string? DetermineClickedPort(WorkflowNode node, Point clickPoint)
        {
            double nodeCenterX = node.Position.X + 70;
            double nodeCenterY = node.Position.Y + 45;
            double offsetX = clickPoint.X - nodeCenterX;
            double offsetY = clickPoint.Y - nodeCenterY;

            string? clickedPort = null;
            if (Math.Abs(offsetX) > Math.Abs(offsetY))
            {
                if (offsetX > 0)
                {
                    clickedPort = "RightPort";
                    _dragConnectionStartPoint = node.RightPortPosition;
                }
                else
                {
                    clickedPort = "LeftPort";
                    _dragConnectionStartPoint = node.LeftPortPosition;
                }
            }
            else
            {
                if (offsetY > 0)
                {
                    clickedPort = "BottomPort";
                    _dragConnectionStartPoint = node.BottomPortPosition;
                }
                else
                {
                    clickedPort = "TopPort";
                    _dragConnectionStartPoint = node.TopPortPosition;
                }
            }
            
            _dragConnectionSourcePort = clickedPort;
            return clickedPort;
        }

        /// <summary>
        /// 获取节点的连接点位置
        /// </summary>
        private Point GetPortPosition(WorkflowNode node, Point clickPoint)
        {
            // 计算点击点相对于节点中心的偏移
            double nodeCenterX = node.Position.X + 70;  // 节点宽度的一半
            double nodeCenterY = node.Position.Y + 45;  // 节点高度的一半
            double offsetX = clickPoint.X - nodeCenterX;
            double offsetY = clickPoint.Y - nodeCenterY;

            // 判断点击的是哪个连接点
            Point portPosition;
            if (Math.Abs(offsetX) > Math.Abs(offsetY))
            {
                // 水平方向（左右）
                if (offsetX > 0)
                {
                    portPosition = node.RightPortPosition;
                }
                else
                {
                    portPosition = node.LeftPortPosition;
                }
            }
            else
            {
                // 垂直方向（上下）
                if (offsetY > 0)
                {
                    portPosition = node.BottomPortPosition;
                }
                else
                {
                    portPosition = node.TopPortPosition;
                }
            }

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
        /// 创建节点连接（使用指定的目标端口）
        /// </summary>
        private void CreateConnectionWithSpecificPort(WorkflowNode sourceNode, WorkflowNode targetNode, string targetPortName)
        {
            _viewModel?.AddLog($"[CreateConnectionWithSpecificPort] ========== 开始创建连接（指定端口） ==========");

            var selectedTab = _viewModel?.WorkflowTabViewModel.SelectedTab;
            if (selectedTab == null || selectedTab.WorkflowConnections == null)
            {
                _viewModel?.AddLog("[CreateConnectionWithSpecificPort] ❌ SelectedTab或WorkflowConnections为null");
                return;
            }

            var connectionId = $"conn_{Guid.NewGuid().ToString("N")[..8]}";
            var newConnection = new WorkflowConnection(connectionId, sourceNode.Id, targetNode.Id);
            _viewModel?.AddLog($"[CreateConnectionWithSpecificPort] 新连接ID: {connectionId}");

            // 设置源端口名称
            newConnection.SourcePort = _dragConnectionSourcePort ?? "RightPort";
            newConnection.TargetPort = targetPortName;

            // 获取源端口位置
            Point sourcePos;
            switch (_dragConnectionSourcePort)
            {
                case "TopPort":
                    sourcePos = sourceNode.TopPortPosition;
                    break;
                case "BottomPort":
                    sourcePos = sourceNode.BottomPortPosition;
                    break;
                case "LeftPort":
                    sourcePos = sourceNode.LeftPortPosition;
                    break;
                case "RightPort":
                    sourcePos = sourceNode.RightPortPosition;
                    break;
                default:
                    sourcePos = sourceNode.RightPortPosition;
                    break;
            }

            // 获取目标端口位置（使用用户指定的端口）
            Point targetPos;
            switch (targetPortName)
            {
                case "TopPort":
                    targetPos = targetNode.TopPortPosition;
                    break;
                case "BottomPort":
                    targetPos = targetNode.BottomPortPosition;
                    break;
                case "LeftPort":
                    targetPos = targetNode.LeftPortPosition;
                    break;
                case "RightPort":
                    targetPos = targetNode.RightPortPosition;
                    break;
                default:
                    targetPos = targetNode.LeftPortPosition;
                    break;
            }

            newConnection.SourcePosition = sourcePos;
            newConnection.TargetPosition = targetPos;

            _viewModel?.AddLog($"[CreateConnectionWithSpecificPort] 源端口:{_dragConnectionSourcePort} 位置:{sourcePos}");
            _viewModel?.AddLog($"[CreateConnectionWithSpecificPort] 目标端口:{targetPortName} 位置:{targetPos}");

            CurrentWorkflowTab?.WorkflowConnections.Add(newConnection);
            _viewModel?.AddLog($"[CreateConnectionWithSpecificPort] ✓ 连接创建完成");
        }

        /// <summary>
        /// 创建节点连接
        /// </summary>
        private void CreateConnection(WorkflowNode sourceNode, WorkflowNode targetNode)
        {
            _viewModel?.AddLog($"[CreateConnection] ========== 开始创建连接 ==========");

            var selectedTab = _viewModel?.WorkflowTabViewModel.SelectedTab;
            if (selectedTab == null)
            {
                _viewModel?.AddLog("[CreateConnection] ❌ SelectedTab为null");
                return;
            }

            if (selectedTab.WorkflowConnections == null)
            {
                _viewModel?.AddLog("[CreateConnection] ❌ WorkflowConnections为null");
                return;
            }

            _viewModel?.AddLog($"[CreateConnection] 源节点: {sourceNode.Name} (ID={sourceNode.Id}), 位置: {sourceNode.Position}");
            _viewModel?.AddLog($"[CreateConnection] 目标节点: {targetNode.Name} (ID={targetNode.Id}), 位置: {targetNode.Position}");

            var connectionId = $"conn_{Guid.NewGuid().ToString("N")[..8]}";
            var newConnection = new WorkflowConnection(connectionId, sourceNode.Id, targetNode.Id);
            _viewModel?.AddLog($"[CreateConnection] 新连接ID: {connectionId}");


            // 智能选择连接点位置
            Point sourcePos, targetPos;
            string finalSourcePort, finalTargetPort;

            // 使用记录的源端口
            string initialSourcePort = _dragConnectionSourcePort ?? "RightPort";
            switch (initialSourcePort)
            {
                case "TopPort":
                    sourcePos = sourceNode.TopPortPosition;
                    break;
                case "BottomPort":
                    sourcePos = sourceNode.BottomPortPosition;
                    break;
                case "LeftPort":
                    sourcePos = sourceNode.LeftPortPosition;
                    break;
                case "RightPort":
                    sourcePos = sourceNode.RightPortPosition;
                    break;
                default:
                    sourcePos = sourceNode.RightPortPosition;
                    break;
            }

            // 选择目标端口（根据源端口方向和目标节点位置选择最近的端口）
            var deltaX = targetNode.Position.X - sourcePos.X;
            var deltaY = targetNode.Position.Y - sourcePos.Y;

            _viewModel?.AddLog($"[CreateConnection] 源端口:{initialSourcePort} 源位置:({sourcePos.X:F1},{sourcePos.Y:F1})");
            _viewModel?.AddLog($"[CreateConnection] 目标节点:({targetNode.Position.X:F1},{targetNode.Position.Y:F1}) 偏移:({deltaX:F1},{deltaY:F1})");

            string direction = "";
            bool isVerticalDominant = initialSourcePort == "TopPort" || initialSourcePort == "BottomPort";

            if (isVerticalDominant)
            {
                // 源端口是垂直方向（Top/Bottom），优先选择垂直方向的目标端口
                if (Math.Abs(deltaX) > 2 * Math.Abs(deltaY))
                {
                    direction = "水平（源垂直但水平偏移过大）";
                    if (deltaX > 0)
                    {
                        _viewModel?.AddLog($"[CreateConnection] {direction}:源在左，目标在右 -> 源右->目标左");
                        finalSourcePort = "RightPort";
                        finalTargetPort = "LeftPort";
                        sourcePos = sourceNode.RightPortPosition;
                        targetPos = targetNode.LeftPortPosition;
                    }
                    else
                    {
                        _viewModel?.AddLog($"[CreateConnection] {direction}:源在右，目标在左 -> 源左->目标右");
                        finalSourcePort = "LeftPort";
                        finalTargetPort = "RightPort";
                        sourcePos = sourceNode.LeftPortPosition;
                        targetPos = targetNode.RightPortPosition;
                    }
                }
                else
                {
                    direction = "垂直（源端口主导）";
                    if (deltaY > 0)
                    {
                        _viewModel?.AddLog($"[CreateConnection] {direction}:源在上，目标在下 -> 源底->目标顶");
                        finalSourcePort = "BottomPort";
                        finalTargetPort = "TopPort";
                        sourcePos = sourceNode.BottomPortPosition;
                        targetPos = targetNode.TopPortPosition;
                    }
                    else
                    {
                        _viewModel?.AddLog($"[CreateConnection] {direction}:源在下，目标在上 -> 源顶->目标底");
                        finalSourcePort = "TopPort";
                        finalTargetPort = "BottomPort";
                        sourcePos = sourceNode.TopPortPosition;
                        targetPos = targetNode.BottomPortPosition;
                    }
                }
            }
            else
            {
                // 源端口是水平方向（Left/Right），优先选择水平方向的目标端口
                if (Math.Abs(deltaY) > 2 * Math.Abs(deltaX))
                {
                    direction = "垂直（源水平但垂直偏移过大）";
                    if (deltaY > 0)
                    {
                        _viewModel?.AddLog($"[CreateConnection] {direction}:源在上，目标在下 -> 源底->目标顶");
                        finalSourcePort = "BottomPort";
                        finalTargetPort = "TopPort";
                        sourcePos = sourceNode.BottomPortPosition;
                        targetPos = targetNode.TopPortPosition;
                    }
                    else
                    {
                        _viewModel?.AddLog($"[CreateConnection] {direction}:源在下，目标在上 -> 源顶->目标底");
                        finalSourcePort = "TopPort";
                        finalTargetPort = "BottomPort";
                        sourcePos = sourceNode.TopPortPosition;
                        targetPos = targetNode.BottomPortPosition;
                    }
                }
                else
                {
                    direction = "水平（源端口主导）";
                    if (deltaX > 0)
                    {
                        _viewModel?.AddLog($"[CreateConnection] {direction}:源在左，目标在右 -> 源右->目标左");
                        finalSourcePort = "RightPort";
                        finalTargetPort = "LeftPort";
                        sourcePos = sourceNode.RightPortPosition;
                        targetPos = targetNode.LeftPortPosition;
                    }
                    else
                    {
                        _viewModel?.AddLog($"[CreateConnection] {direction}:源在右，目标在左 -> 源左->目标右");
                        finalSourcePort = "LeftPort";
                        finalTargetPort = "RightPort";
                        sourcePos = sourceNode.LeftPortPosition;
                        targetPos = targetNode.RightPortPosition;
                    }
                }
            }

            _viewModel?.AddLog($"[CreateConnection] |deltaX|:{Math.Abs(deltaX):F1} |deltaY|:{Math.Abs(deltaY):F1} 最终端口:({sourcePos.X:F1},{sourcePos.Y:F1})->({targetPos.X:F1},{targetPos.Y:F1})");

            newConnection.SourcePort = finalSourcePort;
            newConnection.TargetPort = finalTargetPort;
            newConnection.SourcePosition = sourcePos;
            newConnection.TargetPosition = targetPos;

            _viewModel?.AddLog($"[CreateConnection] ✓ 源端口位置: {sourcePos}");
            _viewModel?.AddLog($"[CreateConnection] ✓ 目标端口位置: {targetPos}");

            // 关键信息：添加前后的连接数
            int beforeCount = selectedTab.WorkflowConnections.Count;
            _viewModel?.AddLog($"[CreateConnection] 添加前连接数: {beforeCount}");

            CurrentWorkflowTab?.WorkflowConnections.Add(newConnection);

            int afterCount = selectedTab.WorkflowConnections.Count;
            _viewModel?.AddLog($"[CreateConnection] 添加后连接数: {afterCount}");

            // 关键信息：验证连接是否真的在集合中
            var addedConnection = selectedTab.WorkflowConnections.FirstOrDefault(c => c.Id == connectionId);
            if (addedConnection != null)
            {
                _viewModel?.AddLog($"[CreateConnection] ✓ 连接验证成功，ID: {addedConnection.Id}");
            }
            else
            {
                _viewModel?.AddLog($"[CreateConnection] ❌ 连接验证失败，ID: {connectionId}");
            }

            // 关键信息：检查 WorkflowConnections 集合的引用
            if (CurrentWorkflowTab != null)
            {
                _viewModel?.AddLog($"[CreateConnection] CurrentWorkflowTab.WorkflowConnections 引用相同: {ReferenceEquals(selectedTab.WorkflowConnections, CurrentWorkflowTab.WorkflowConnections)}");
            }

            _viewModel!.StatusText = $"成功连接: {sourceNode.Name} -> {targetNode.Name}";
            _viewModel.AddLog($"[CreateConnection] ========== 连接创建完成 ==========");
        }

        /// <summary>
        /// 获取节点指定端口的Ellipse元素
        /// </summary>
        private Ellipse? GetPortElement(Border nodeBorder, string portName)
        {
            if (nodeBorder == null) return null;

            // 根据端口名称构造Ellipse名称（例如："LeftPort" -> "LeftPortEllipse"）
            string ellipseName = portName + "Ellipse";

            // 在节点Border的视觉树中查找指定名称的端口
            var visualChildren = FindAllVisualChildren<DependencyObject>(nodeBorder);

            // 只在第一次查找失败时输出日志
            bool found = false;
            // 查找包含端口名称的元素（通过Name属性或Tag）
            foreach (var child in visualChildren)
            {
                if (child is FrameworkElement element && element.Name == ellipseName)
                {
                    if (!found && _highlightCounter % 20 == 0) // 每20次高亮才输出一次
                    {
                        _viewModel?.AddLog($"[GetPortElement] ✓ 找到端口: {element.Name}");
                    }
                    return element as Ellipse;
                }
            }

            if (_highlightCounter % 20 == 0) // 每20次高亮才输出一次
            {
                _viewModel?.AddLog($"[GetPortElement] ❌ 未找到端口: {ellipseName}");
            }
            return null;
        }

        /// <summary>
        /// 高亮显示目标端口
        /// </summary>
        private void HighlightTargetPort(Border? nodeBorder, WorkflowNode? sourceNode)
        {
            // 先取消之前的高亮
            ClearTargetPortHighlight();

            if (nodeBorder == null || sourceNode == null) return;

            // 获取源端口的实际位置（而不是节点中心）
            Point sourcePos;
            switch (_dragConnectionSourcePort)
            {
                case "TopPort":
                    sourcePos = sourceNode.TopPortPosition;
                    break;
                case "BottomPort":
                    sourcePos = sourceNode.BottomPortPosition;
                    break;
                case "LeftPort":
                    sourcePos = sourceNode.LeftPortPosition;
                    break;
                case "RightPort":
                    sourcePos = sourceNode.RightPortPosition;
                    break;
                default:
                    sourcePos = sourceNode.RightPortPosition;
                    break;
            }

            var targetNode = nodeBorder.Tag as WorkflowNode;
            if (targetNode == null) return;

            var targetPos = targetNode.Position;
            var deltaX = targetPos.X - sourcePos.X;
            var deltaY = targetPos.Y - sourcePos.Y;

            string targetPortName = "LeftPort"; // 默认

            // 根据源端口方向和相对位置选择目标端口
            // 策略：优先选择与源端口方向对应的目标端口，但允许根据实际位置调整
            string direction = "";
            bool isVerticalDominant = _dragConnectionSourcePort == "TopPort" || _dragConnectionSourcePort == "BottomPort";

            if (isVerticalDominant)
            {
                // 源端口是垂直方向（Top/Bottom），优先选择垂直方向的目标端口
                // 但如果水平偏移远大于垂直偏移（2倍以上），则选择水平方向
                if (Math.Abs(deltaX) > 2 * Math.Abs(deltaY))
                {
                    direction = "水平（源垂直但水平偏移过大）";
                    if (deltaX > 0)
                        targetPortName = "LeftPort";
                    else
                        targetPortName = "RightPort";
                }
                else
                {
                    direction = "垂直（源端口主导）";
                    if (deltaY > 0)
                        targetPortName = "TopPort";
                    else
                        targetPortName = "BottomPort";
                }
            }
            else
            {
                // 源端口是水平方向（Left/Right），优先选择水平方向的目标端口
                // 但如果垂直偏移远大于水平偏移（2倍以上），则选择垂直方向
                if (Math.Abs(deltaY) > 2 * Math.Abs(deltaX))
                {
                    direction = "垂直（源水平但垂直偏移过大）";
                    if (deltaY > 0)
                        targetPortName = "TopPort";
                    else
                        targetPortName = "BottomPort";
                }
                else
                {
                    direction = "水平（源端口主导）";
                    if (deltaX > 0)
                        targetPortName = "LeftPort";
                    else
                        targetPortName = "RightPort";
                }
            }

            // 只在端口变化或每10次高亮时输出日志
            bool shouldLog = _lastHighlightedPort != targetPortName || _highlightCounter % 10 == 0;
            if (shouldLog)
            {
                _viewModel?.AddLog($"[HighlightTargetPort] 源端口:{_dragConnectionSourcePort} 源位置:({sourcePos.X:F1},{sourcePos.Y:F1})");
                _viewModel?.AddLog($"[HighlightTargetPort] 目标节点:({targetPos.X:F1},{targetPos.Y:F1}) 偏移:({deltaX:F1},{deltaY:F1})");
                _viewModel?.AddLog($"[HighlightTargetPort] 选择逻辑:{direction} |deltaX|:{Math.Abs(deltaX):F1} |deltaY|:{Math.Abs(deltaY):F1}");
                _viewModel?.AddLog($"[HighlightTargetPort] ✓ 选择目标端口: {targetPortName}");
                _lastHighlightedPort = targetPortName;
            }
            _highlightCounter++;

            // 获取端口元素
            var portElement = GetPortElement(nodeBorder, targetPortName);
            if (portElement != null)
            {
                // 确保端口可见
                portElement.Visibility = Visibility.Visible;

                _highlightedTargetPort = portElement;
                _highlightedTargetBorder = nodeBorder;

                // 保存原始样式
                _originalPortFill = portElement.Fill;
                _originalPortStroke = portElement.Stroke;
                _originalPortStrokeThickness = portElement.StrokeThickness;

                // 设置高亮样式
                portElement.Fill = new SolidColorBrush(Color.FromRgb(255, 200, 0)); // 金色填充
                portElement.Stroke = new SolidColorBrush(Color.FromRgb(255, 100, 0)); // 深橙色边框
                portElement.StrokeThickness = 3;
            }
            else
            {
                _viewModel?.AddLog($"[HighlightTargetPort] ❌ 未找到端口元素: {targetPortName}");
            }
        }

        /// <summary>
        /// 高亮指定的端口（用于直接命中端口的情况）
        /// </summary>
        private void HighlightSpecificPort(Border nodeBorder, string portName)
        {
            ClearTargetPortHighlight();

            if (nodeBorder == null) return;

            var portElement = GetPortElement(nodeBorder, portName);
            if (portElement != null)
            {
                portElement.Visibility = Visibility.Visible;

                _highlightedTargetPort = portElement;
                _highlightedTargetBorder = nodeBorder;

                // 保存原始样式
                _originalPortFill = portElement.Fill;
                _originalPortStroke = portElement.Stroke;
                _originalPortStrokeThickness = portElement.StrokeThickness;

                // 设置高亮样式
                portElement.Fill = new SolidColorBrush(Color.FromRgb(255, 200, 0)); // 金色填充
                portElement.Stroke = new SolidColorBrush(Color.FromRgb(255, 100, 0)); // 深橙色边框
                portElement.StrokeThickness = 3;

                // 只在端口变化时记录日志
                if (_lastHighlightedPort != portName && _highlightCounter % 5 == 0)
                {
                    _viewModel?.AddLog($"[HighlightSpecificPort] ✓ 高亮成功: {portName}");
                }
            }
        }

        /// <summary>
        /// 清除目标端口的高亮
        /// </summary>
        private void ClearTargetPortHighlight()
        {
            if (_highlightedTargetPort != null && _originalPortFill != null)
            {
                // 恢复原始样式
                _highlightedTargetPort.Fill = _originalPortFill;
                _highlightedTargetPort.Stroke = _originalPortStroke ?? new SolidColorBrush(Colors.Transparent);
                _highlightedTargetPort.StrokeThickness = _originalPortStrokeThickness;

                // 注意：不要隐藏端口，因为拖拽时所有端口应该保持可见
                // 端口的可见性由 IsDraggingConnection 属性控制
            }

            _highlightedTargetPort = null;
            _originalPortFill = null;
            _originalPortStroke = null;
            _originalPortStrokeThickness = 0;
        }

        // 保存端口原始样式
        private Brush? _originalPortFill;
        private Brush? _originalPortStroke;
        private double _originalPortStrokeThickness;

        /// <summary>
        /// Path元素加载事件 - 监控连接线路径创建
        /// </summary>
        private void Path_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is Path path && path.DataContext is WorkflowConnection connection)
            {
                _viewModel?.AddLog($"[Path_Loaded] ✓ Path加载，连接ID: {connection.Id}");

                if (path.Data is PathGeometry geom && geom.Figures.Count > 0)
                {
                    _viewModel?.AddLog($"[Path_Loaded] ✓ 路径数据: {geom.Figures.Count}个Figure, {geom.Figures[0].Segments.Count}个Segment");
                }
                else
                {
                    _viewModel?.AddLog($"[Path_Loaded] ⚠ 路径数据未正确创建");
                }
            }
        }

        /// <summary>
        /// Path的DataContext变化事件 - 监控连接数据更新
        /// </summary>
        private void Path_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is Path path)
            {
                if (e.NewValue is WorkflowConnection newConn)
                {
                    _viewModel?.AddLog($"[Path_DataContextChanged] 连接ID: {newConn.Id}, 源: {newConn.SourcePosition}, 目标: {newConn.TargetPosition}");
                }
            }
        }

        #endregion
    }
}
