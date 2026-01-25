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
                    System.Diagnostics.Debug.WriteLine("[WorkflowCanvas] 构造函数中获取MainWindowViewModel: {_viewModel != null}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[WorkflowCanvas] 构造函数异常: {ex.Message}");
            }
        }

        private void WorkflowCanvasControl_Loaded(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("[WorkflowCanvas] ====== Loaded事件触发 ======");

            // 获取 MainWindowViewModel
            if (Window.GetWindow(this) is MainWindow mainWindow)
            {
                _viewModel = mainWindow.DataContext as MainWindowViewModel;
                if (_viewModel == null)
                {
                    System.Diagnostics.Debug.WriteLine("[WorkflowCanvas] ❌ 无法获取MainWindowViewModel");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[WorkflowCanvas] ✓ 成功获取MainWindowViewModel");

                    // 检查 WorkflowTabViewModel
                    if (_viewModel.WorkflowTabViewModel == null)
                    {
                        System.Diagnostics.Debug.WriteLine("[WorkflowCanvas] ❌ WorkflowTabViewModel为null");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("[WorkflowCanvas] ✓ WorkflowTabViewModel不为null");

                        // 检查 SelectedTab
                        var selectedTab = _viewModel.WorkflowTabViewModel.SelectedTab;
                        if (selectedTab == null)
                        {
                            System.Diagnostics.Debug.WriteLine("[WorkflowCanvas] ❌ SelectedTab为null");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"[WorkflowCanvas] ✓ SelectedTab不为null，ID={selectedTab.Id}");
                            System.Diagnostics.Debug.WriteLine($"[WorkflowCanvas] 当前节点数: {selectedTab.WorkflowNodes.Count}");
                            System.Diagnostics.Debug.WriteLine($"[WorkflowCanvas] 当前连接数: {selectedTab.WorkflowConnections.Count}");
                        }
                    }

                    _viewModel.AddLog($"[WorkflowCanvas] ✓ WorkflowCanvasControl 已加载，可以开始拖拽创建连接");
                }
            }

            // 初始化智能路径转换器的节点集合
            if (_viewModel?.WorkflowTabViewModel.SelectedTab != null)
            {
                Converters.SmartPathConverter.Nodes = _viewModel.WorkflowTabViewModel.SelectedTab.WorkflowNodes;
                System.Diagnostics.Debug.WriteLine($"[WorkflowCanvas] ✓ 已初始化SmartPathConverter的节点集合，共{Converters.SmartPathConverter.Nodes.Count()}个节点");
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
            System.Diagnostics.Debug.WriteLine($"[Connection] Node_ClickForConnection 触发，Sender类型: {sender?.GetType().Name}");

            // 获取节点对象（支持 Border 或 Ellipse 作为 sender）
            WorkflowNode? targetNode = null;

            if (sender is Border border && border.Tag is WorkflowNode clickedNodeFromBorder)
            {
                targetNode = clickedNodeFromBorder;
                System.Diagnostics.Debug.WriteLine($"[Connection] 从Border获取到节点: {targetNode.Name}");
            }
            else if (sender is Ellipse ellipse && ellipse.Tag is WorkflowNode clickedNodeFromEllipse)
            {
                targetNode = clickedNodeFromEllipse;
                System.Diagnostics.Debug.WriteLine($"[Connection] 从Ellipse获取到节点: {targetNode.Name}, 节点ID={targetNode.Id}, 节点Hash={targetNode.GetHashCode()}");

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
                System.Diagnostics.Debug.WriteLine($"[Connection] ❌ 无法从sender获取节点，sender类型: {sender?.GetType().Name}");
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
                System.Diagnostics.Debug.WriteLine($"[Connection] ❌ SelectedTab为null");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"[Connection] 当前源节点: {_connectionSourceNode?.Name ?? "null"}");

            // 检查是否在连接模式
            if (_connectionSourceNode == null)
            {
                // 进入连接模式
                _connectionSourceNode = targetNode;
                _viewModel?.AddLog($"[连接] 开始创建连接，源节点: {targetNode.Name}, ID={targetNode.Id}, Hash={targetNode.GetHashCode()}");
                _viewModel!.StatusText = $"请选择目标节点进行连接，从: {targetNode.Name}";
                System.Diagnostics.Debug.WriteLine($"[Connection] ✓ 进入连接模式，源节点: {targetNode.Name}, ID={targetNode.Id}, Hash={targetNode.GetHashCode()}");
            }
            else
            {
                // 检查是否是同一个节点
                System.Diagnostics.Debug.WriteLine($"[Connection] 检查同一节点: 源节点ID={_connectionSourceNode?.Id}, 目标节点ID={targetNode.Id}, 源节点Hash={_connectionSourceNode?.GetHashCode()}, 目标Hash={targetNode.GetHashCode()}");
                if (_connectionSourceNode == targetNode)
                {
                    _viewModel!.StatusText = "无法连接到同一个节点";
                    _viewModel.AddLog("[连接] ❌ 无法连接到同一个节点");
                    System.Diagnostics.Debug.WriteLine($"[Connection] ❌ 不能连接到同一个节点");
                    _connectionSourceNode = null;
                    return;
                }

                // 检查连接是否已存在
                var existingConnection = selectedTab.WorkflowConnections.FirstOrDefault(c =>
                    c.SourceNodeId == _connectionSourceNode!.Id && c.TargetNodeId == targetNode.Id);

                if (existingConnection != null)
                {
                    _viewModel!.StatusText = "连接已存在";
                    System.Diagnostics.Debug.WriteLine($"[Connection] ❌ 连接已存在");
                    _connectionSourceNode = null;
                    return;
                }

                // 创建新连接
                System.Diagnostics.Debug.WriteLine($"[Connection] ✓ 创建连接: {_connectionSourceNode.Name} -> {targetNode.Name}");
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
            System.Diagnostics.Debug.WriteLine($"[拖拽连接] ========== Port_PreviewMouseLeftButtonDown 开始 ==========");
            System.Diagnostics.Debug.WriteLine($"[拖拽连接] Sender类型: {sender?.GetType().Name}");
            System.Diagnostics.Debug.WriteLine($"[拖拽连接] OriginalSource类型: {e.OriginalSource?.GetType().Name}");
            System.Diagnostics.Debug.WriteLine($"[拖拽连接] 当前_isDraggingConnection: {_isDraggingConnection}");

            if (sender is not Ellipse port || port.Tag is not WorkflowNode node)
            {
                System.Diagnostics.Debug.WriteLine($"[拖拽连接] ❌ 条件检查失败 - Sender类型: {sender?.GetType().Name}");
                if (sender is Ellipse ellipse)
                {
                    System.Diagnostics.Debug.WriteLine($"[拖拽连接] ❌ Ellipse Tag是否为WorkflowNode: {ellipse.Tag is WorkflowNode}");
                }
                return;
            }

            e.Handled = true;

            System.Diagnostics.Debug.WriteLine($"[拖拽连接] ✓ 获取到源节点: {node.Name}, ID={node.Id}, Hash={node.GetHashCode()}");

            // 选中当前节点
            if (_viewModel?.WorkflowTabViewModel.SelectedTab != null)
            {
                int nodeCount = _viewModel.WorkflowTabViewModel.SelectedTab.WorkflowNodes.Count;
                System.Diagnostics.Debug.WriteLine($"[拖拽连接] 当前工作流共有{nodeCount}个节点");

                foreach (var n in _viewModel.WorkflowTabViewModel.SelectedTab.WorkflowNodes)
                {
                    n.IsSelected = (n == node);
                }
                _viewModel.SelectedNode = node;
                System.Diagnostics.Debug.WriteLine($"[拖拽连接] ✓ 选中节点完成: {node.Name}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[拖拽连接] ⚠ SelectedTab为null，无法选中节点");
            }

            // 开始拖拽连接（使用属性设置，会自动显示所有连接点）
            IsDraggingConnection = true;
            _dragConnectionSourceNode = node;
            System.Diagnostics.Debug.WriteLine($"[拖拽连接] ✓ 设置拖拽状态完成，IsDraggingConnection={_isDraggingConnection}");
            _viewModel?.AddLog($"[拖拽连接] ✓ 设置拖拽状态，IsDraggingConnection={_isDraggingConnection}");

            // 获取连接点在画布上的位置
            _dragConnectionStartPoint = e.GetPosition(WorkflowCanvas);
            System.Diagnostics.Debug.WriteLine($"[拖拽连接] ✓ 获取起始点: {_dragConnectionStartPoint}");
            _viewModel?.AddLog($"[拖拽连接] ✓ 起始点: {_dragConnectionStartPoint}");

            // 显示临时连接线
            System.Diagnostics.Debug.WriteLine($"[拖拽连接] TempConnectionLine是否为null: {TempConnectionLine == null}");
            System.Diagnostics.Debug.WriteLine($"[拖拽连接] TempConnectionGeometry是否为null: {TempConnectionGeometry == null}");

            if (TempConnectionLine != null)
            {
                System.Diagnostics.Debug.WriteLine($"[拖拽连接] 临时连接线当前Visibility: {TempConnectionLine.Visibility}");
                TempConnectionLine.Visibility = Visibility.Visible;
                System.Diagnostics.Debug.WriteLine($"[拖拽连接] 临时连接线设置后Visibility: {TempConnectionLine.Visibility}");
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
                System.Diagnostics.Debug.WriteLine($"[拖拽连接] 临时连接线起点: {_dragConnectionStartPoint}");
            }

            _viewModel?.AddLog($"[拖拽连接] ✓ 显示临时连接线");

            // 在Canvas上捕获鼠标（而不是在Ellipse上），这样可以在任意位置检测释放事件
            WorkflowCanvas.CaptureMouse();
            System.Diagnostics.Debug.WriteLine($"[拖拽连接] ✓ 在Canvas上捕获鼠标");
            _viewModel?.AddLog($"[拖拽连接] ✓ 鼠标已捕获");

            _viewModel?.AddLog($"[拖拽连接] 开始拖拽连接，源节点: {node.Name}");
            System.Diagnostics.Debug.WriteLine($"[拖拽连接] ========== Port_PreviewMouseLeftButtonDown 完成 ==========");
        }

        /// <summary>
        /// 连接点鼠标释放 - 结束拖拽并创建连接
        /// </summary>
        private void Port_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"[拖拽连接] ========== Port_PreviewMouseLeftButtonUp 开始 ==========");
            System.Diagnostics.Debug.WriteLine($"[拖拽连接] 当前_isDraggingConnection: {_isDraggingConnection}");
            System.Diagnostics.Debug.WriteLine($"[拖拽连接] 拖拽源节点: {_dragConnectionSourceNode?.Name ?? "null"}");
            System.Diagnostics.Debug.WriteLine($"[拖拽连接] Sender类型: {sender?.GetType().Name}");
            System.Diagnostics.Debug.WriteLine($"[拖拽连接] OriginalSource类型: {e.OriginalSource?.GetType().Name}");
            System.Diagnostics.Debug.WriteLine($"[拖拽连接] e.Handled设置前: {e.Handled}");

            if (!_isDraggingConnection || _dragConnectionSourceNode == null)
            {
                System.Diagnostics.Debug.WriteLine($"[拖拽连接] ❌ 未在拖拽状态或源节点为null，退出");
                return;
            }

            // 在处理任何操作之前立即标记事件为已处理，阻止传播
            e.Handled = true;
            System.Diagnostics.Debug.WriteLine($"[拖拽连接] ✓ e.Handled已设置为true，阻止事件传播到Canvas");

            if (sender is Ellipse port)
            {
                port.ReleaseMouseCapture();
                System.Diagnostics.Debug.WriteLine($"[拖拽连接] ✓ 释放鼠标捕获");
            }

            // 隐藏临时连接线
            TempConnectionLine.Visibility = Visibility.Collapsed;
            System.Diagnostics.Debug.WriteLine($"[拖拽连接] ✓ 隐藏临时连接线");

            // 查找鼠标位置下的目标节点
            var mousePosition = e.GetPosition(WorkflowCanvas);
            System.Diagnostics.Debug.WriteLine($"[拖拽连接] 鼠标释放位置: {mousePosition}");

            WorkflowNode? targetNode = null;
            Border? targetBorder = null;
            int hitTestCount = 0;

            // 使用 HitTest 查找鼠标位置下的所有元素
            VisualTreeHelper.HitTest(WorkflowCanvas, null,
                result =>
                {
                    hitTestCount++;
                    System.Diagnostics.Debug.WriteLine($"[拖拽连接] HitTest #{hitTestCount}: 找到元素类型: {result.VisualHit?.GetType().Name}");

                    // 如果找到 Border 且带有 WorkflowNode Tag，记录下来
                    if (result.VisualHit is Border hitBorder && hitBorder.Tag is WorkflowNode hitNode)
                    {
                        targetNode = hitNode;
                        targetBorder = hitBorder;
                        System.Diagnostics.Debug.WriteLine($"[拖拽连接] ✓✓ HitTest找到节点Border: {hitNode.Name}, ID={hitNode.Id}, Hash={hitNode.GetHashCode()}");
                        return HitTestResultBehavior.Stop;
                    }
                    // 如果找到 Ellipse（连接点），尝试找到其父级 Border
                    else if (result.VisualHit is Ellipse ellipse)
                    {
                        System.Diagnostics.Debug.WriteLine($"[拖拽连接] ✓ HitTest找到Ellipse，Tag={ellipse.Tag?.GetType().Name}");
                        if (ellipse.Tag is WorkflowNode ellipseNode)
                        {
                            System.Diagnostics.Debug.WriteLine($"[拖拽连接] ✓ Ellipse有WorkflowNode Tag: {ellipseNode.Name}");

                            // 向上查找父级 Border
                            var parent = VisualTreeHelper.GetParent(ellipse);
                            int depth = 0;
                            while (parent != null && depth < 20)
                            {
                                depth++;
                                System.Diagnostics.Debug.WriteLine($"[拖拽连接] 向上查找第{depth}层: {parent?.GetType().Name}");
                                if (parent is Border parentBorder)
                                {
                                    System.Diagnostics.Debug.WriteLine($"[拖拽连接] 找到Border，Tag={parentBorder.Tag?.GetType().Name}");
                                    if (parentBorder.Tag is WorkflowNode parentNode)
                                    {
                                        targetNode = parentNode;
                                        targetBorder = parentBorder;
                                        System.Diagnostics.Debug.WriteLine($"[拖拽连接] ✓✓ 从Ellipse向上找到节点Border: {parentNode.Name}, ID={parentNode.Id}, Hash={parentNode.GetHashCode()}");
                                        return HitTestResultBehavior.Stop;
                                    }
                                }
                                parent = VisualTreeHelper.GetParent(parent);
                            }
                        }
                    }
                    return HitTestResultBehavior.Continue;
                },
                new PointHitTestParameters(mousePosition));

            System.Diagnostics.Debug.WriteLine($"[拖拽连接] HitTest共检测到{hitTestCount}个元素");
            System.Diagnostics.Debug.WriteLine($"[拖拽连接] 最终目标节点: {targetNode?.Name ?? "null"}");

            // 检查是否找到目标节点
            if (targetNode != null && targetNode != _dragConnectionSourceNode)
            {
                System.Diagnostics.Debug.WriteLine($"[拖拽连接] ✓ 找到有效目标节点，检查连接是否已存在");
                _viewModel?.AddLog($"[拖拽连接] 找到目标节点: {targetNode.Name}");

                // 检查连接是否已存在
                var selectedTab = _viewModel?.WorkflowTabViewModel.SelectedTab;
                if (selectedTab != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[拖拽连接] SelectedTab不为null，当前连接数: {selectedTab.WorkflowConnections.Count}");

                    var existingConnection = selectedTab.WorkflowConnections.FirstOrDefault(c =>
                        c.SourceNodeId == _dragConnectionSourceNode.Id && c.TargetNodeId == targetNode.Id);

                    if (existingConnection == null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[拖拽连接] ✓ 连接不存在，准备创建新连接");
                        // 创建新连接
                        CreateConnection(_dragConnectionSourceNode, targetNode);
                        _viewModel?.AddLog($"[拖拽连接] ✓ 连接创建成功: {_dragConnectionSourceNode.Name} -> {targetNode.Name}");
                        System.Diagnostics.Debug.WriteLine($"[拖拽连接] ✓✓ 连接创建完成");
                    }
                    else
                    {
                        _viewModel?.AddLog($"[拖拽连接] ❌ 连接已存在: {_dragConnectionSourceNode.Name} -> {targetNode.Name}");
                        System.Diagnostics.Debug.WriteLine($"[拖拽连接] ❌ 连接已存在，ID={existingConnection.Id}");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[拖拽连接] ❌ SelectedTab为null");
                }
            }
            else
            {
                if (targetNode == null)
                {
                    _viewModel?.AddLog($"[拖拽连接] ❌ 未找到目标节点");
                    System.Diagnostics.Debug.WriteLine($"[拖拽连接] ❌ 目标节点为null");
                }
                else
                {
                    _viewModel?.AddLog($"[拖拽连接] ❌ 目标节点与源节点相同");
                    System.Diagnostics.Debug.WriteLine($"[拖拽连接] ❌ 目标节点与源节点相同: {_dragConnectionSourceNode?.Name}");
                }
            }

            // 重置拖拽状态（使用属性设置，会自动隐藏所有连接点）
            IsDraggingConnection = false;
            _dragConnectionSourceNode = null;
            System.Diagnostics.Debug.WriteLine($"[拖拽连接] ✓ 重置拖拽状态完成");
            System.Diagnostics.Debug.WriteLine($"[拖拽连接] ========== Port_PreviewMouseLeftButtonUp 完成 ==========");
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

                    // 获取源节点的连接点位置
                    var sourcePort = GetPortPosition(_dragConnectionSourceNode, _dragConnectionStartPoint);

                    // 计算智能直角折线路径
                    var pathPoints = CalculateSmartPath(sourcePort, currentPoint);

                    // 更新临时连接线
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

                    // 每隔一定输出一次日志（避免刷屏）
                    if (DateTime.Now.Millisecond % 200 < 20)
                    {
                        System.Diagnostics.Debug.WriteLine($"[Canvas拖拽连接] 更新临时连接线，位置: {currentPoint}");
                        _viewModel?.AddLog($"[Canvas拖拽连接] 位置: {currentPoint}");
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
            System.Diagnostics.Debug.WriteLine($"[Canvas] ========== WorkflowCanvas_PreviewMouseLeftButtonUp 开始 ==========");
            System.Diagnostics.Debug.WriteLine($"[Canvas] _isDraggingConnection: {_isDraggingConnection}, _isBoxSelecting: {_isBoxSelecting}");
            System.Diagnostics.Debug.WriteLine($"[Canvas] OriginalSource类型: {e.OriginalSource?.GetType().Name}");

            // 如果正在拖拽连接，尝试创建连接
            if (_isDraggingConnection)
            {
                System.Diagnostics.Debug.WriteLine($"[Canvas] ✓ 正在拖拽连接，开始处理连接创建");

                e.Handled = true; // 阻止事件继续传播

                // 隐藏临时连接线
                TempConnectionLine.Visibility = Visibility.Collapsed;
                System.Diagnostics.Debug.WriteLine($"[Canvas] ✓ 隐藏临时连接线");

                // 查找鼠标位置下的目标节点
                var mousePosition = e.GetPosition(WorkflowCanvas);
                System.Diagnostics.Debug.WriteLine($"[Canvas] 鼠标释放位置: {mousePosition}");

                WorkflowNode? targetNode = null;
                Border? targetBorder = null;
                int hitTestCount = 0;

                // 使用 HitTest 查找鼠标位置下的所有元素
                VisualTreeHelper.HitTest(WorkflowCanvas, null,
                    result =>
                    {
                        hitTestCount++;
                        System.Diagnostics.Debug.WriteLine($"[Canvas] HitTest #{hitTestCount}: 找到元素类型: {result.VisualHit?.GetType().Name}");

                        // 如果找到 Border 且带有 WorkflowNode Tag，记录下来
                        if (result.VisualHit is Border hitBorder && hitBorder.Tag is WorkflowNode hitNode)
                        {
                            targetNode = hitNode;
                            targetBorder = hitBorder;
                            System.Diagnostics.Debug.WriteLine($"[Canvas] ✓✓ HitTest找到节点Border: {hitNode.Name}, ID={hitNode.Id}, Hash={hitNode.GetHashCode()}");
                            return HitTestResultBehavior.Stop;
                        }
                        // 如果找到 Ellipse（连接点），尝试找到其父级 Border
                        else if (result.VisualHit is Ellipse ellipse)
                        {
                            System.Diagnostics.Debug.WriteLine($"[Canvas] ✓ HitTest找到Ellipse，Tag={ellipse.Tag?.GetType().Name}");
                            if (ellipse.Tag is WorkflowNode ellipseNode)
                            {
                                System.Diagnostics.Debug.WriteLine($"[Canvas] ✓ Ellipse有WorkflowNode Tag: {ellipseNode.Name}");

                                // 向上查找父级 Border
                                var parent = VisualTreeHelper.GetParent(ellipse);
                                int depth = 0;
                                while (parent != null && depth < 20)
                                {
                                    depth++;
                                    System.Diagnostics.Debug.WriteLine($"[Canvas] 向上查找第{depth}层: {parent?.GetType().Name}");
                                    if (parent is Border parentBorder)
                                    {
                                        System.Diagnostics.Debug.WriteLine($"[Canvas] 找到Border，Tag={parentBorder.Tag?.GetType().Name}");
                                        if (parentBorder.Tag is WorkflowNode parentNode)
                                        {
                                            targetNode = parentNode;
                                            targetBorder = parentBorder;
                                            System.Diagnostics.Debug.WriteLine($"[Canvas] ✓✓ 从Ellipse向上找到节点Border: {parentNode.Name}, ID={parentNode.Id}, Hash={parentNode.GetHashCode()}");
                                            return HitTestResultBehavior.Stop;
                                        }
                                    }
                                    parent = VisualTreeHelper.GetParent(parent);
                                }
                            }
                        }
                        return HitTestResultBehavior.Continue;
                    },
                    new PointHitTestParameters(mousePosition));

                System.Diagnostics.Debug.WriteLine($"[Canvas] HitTest共检测到{hitTestCount}个元素");
                System.Diagnostics.Debug.WriteLine($"[Canvas] 最终目标节点: {targetNode?.Name ?? "null"}");

                // 检查是否找到目标节点
                if (targetNode != null && targetNode != _dragConnectionSourceNode)
                {
                    System.Diagnostics.Debug.WriteLine($"[Canvas] ✓ 找到有效目标节点，检查连接是否已存在");
                    _viewModel?.AddLog($"[拖拽连接] 找到目标节点: {targetNode.Name}");

                    // 检查连接是否已存在
                    var selectedTab = _viewModel?.WorkflowTabViewModel.SelectedTab;
                    if (selectedTab != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[Canvas] SelectedTab不为null，当前连接数: {selectedTab.WorkflowConnections.Count}");

                        var existingConnection = selectedTab.WorkflowConnections.FirstOrDefault(c =>
                            c.SourceNodeId == _dragConnectionSourceNode.Id && c.TargetNodeId == targetNode.Id);

                        if (existingConnection == null)
                        {
                            System.Diagnostics.Debug.WriteLine($"[Canvas] ✓ 连接不存在，准备创建新连接");
                            // 创建新连接
                            CreateConnection(_dragConnectionSourceNode, targetNode);
                            _viewModel?.AddLog($"[拖拽连接] ✓ 连接创建成功: {_dragConnectionSourceNode.Name} -> {targetNode.Name}");
                            System.Diagnostics.Debug.WriteLine($"[Canvas] ✓✓ 连接创建完成");
                        }
                        else
                        {
                            _viewModel?.AddLog($"[拖拽连接] ❌ 连接已存在: {_dragConnectionSourceNode.Name} -> {targetNode.Name}");
                            System.Diagnostics.Debug.WriteLine($"[Canvas] ❌ 连接已存在，ID={existingConnection.Id}");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[Canvas] ❌ SelectedTab为null");
                    }
                }
                else
                {
                    if (targetNode == null)
                    {
                        _viewModel?.AddLog($"[拖拽连接] ❌ 未找到目标节点");
                        System.Diagnostics.Debug.WriteLine($"[Canvas] ❌ 目标节点为null");
                    }
                    else
                    {
                        _viewModel?.AddLog($"[拖拽连接] ❌ 目标节点与源节点相同");
                        System.Diagnostics.Debug.WriteLine($"[Canvas] ❌ 目标节点与源节点相同: {_dragConnectionSourceNode?.Name}");
                    }
                }

                // 重置拖拽状态
                WorkflowCanvas.ReleaseMouseCapture();
                IsDraggingConnection = false;
                _dragConnectionSourceNode = null;
                System.Diagnostics.Debug.WriteLine($"[Canvas] ✓ 重置拖拽状态完成");
                System.Diagnostics.Debug.WriteLine($"[Canvas] ========== WorkflowCanvas_PreviewMouseLeftButtonUp 完成（创建连接） ==========");
                return;
            }

            if (!_isBoxSelecting)
            {
                System.Diagnostics.Debug.WriteLine($"[Canvas] 不在框选状态，直接返回");
                return;
            }

            _isBoxSelecting = false;
            System.Diagnostics.Debug.WriteLine($"[Canvas] 结束框选模式");


            // 结束框选
            SelectionBox?.EndSelection();
            WorkflowCanvas.ReleaseMouseCapture();

            // 记录选中节点的初始位置（用于批量移动）
            RecordSelectedNodesPositions();

            e.Handled = true;
            System.Diagnostics.Debug.WriteLine($"[Canvas] ========== WorkflowCanvas_PreviewMouseLeftButtonUp 完成（框选） ==========");
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
            System.Diagnostics.Debug.WriteLine($"[CalculateSmartPath] 起点: {start}, 终点: {end}");

            var points = new List<Point>();
            double deltaX = Math.Abs(end.X - start.X);
            double deltaY = Math.Abs(end.Y - start.Y);

            bool isHorizontal = deltaX > deltaY;
            System.Diagnostics.Debug.WriteLine($"[CalculateSmartPath] 使用{(isHorizontal ? "水平" : "垂直")}折线，deltaX={deltaX}, deltaY={deltaY}");

            if (isHorizontal)
            {
                // 水平方向：先水平移动到中间点，再垂直移动到目标Y，最后水平移动到目标X
                double midX = (start.X + end.X) / 2;
                points.Add(new Point(midX, start.Y));
                points.Add(new Point(midX, end.Y));
                System.Diagnostics.Debug.WriteLine($"[CalculateSmartPath] 中间点: ({midX}, {start.Y}), ({midX}, {end.Y})");
            }
            else
            {
                // 垂直方向：先垂直移动到中间点，再水平移动到目标X，最后垂直移动到目标Y
                double midY = (start.Y + end.Y) / 2;
                points.Add(new Point(start.X, midY));
                points.Add(new Point(end.X, midY));
                System.Diagnostics.Debug.WriteLine($"[CalculateSmartPath] 中间点: ({start.X}, {midY}), ({end.X}, {midY})");
            }

            points.Add(end);
            System.Diagnostics.Debug.WriteLine($"[CalculateSmartPath] ✓ 路径计算完成，共{points.Count}个点");
            return points;
        }

        /// <summary>
        /// 获取节点的连接点位置
        /// </summary>
        private Point GetPortPosition(WorkflowNode node, Point clickPoint)
        {
            // 根据点击的连接点确定实际连接位置
            // 简化实现：返回起始点的位置（可以根据需要更精确地计算）
            return clickPoint;
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
            System.Diagnostics.Debug.WriteLine($"[CreateConnection] ========== CreateConnection 开始 ==========");
            System.Diagnostics.Debug.WriteLine($"[CreateConnection] 源节点: {sourceNode.Name}, ID={sourceNode.Id}");
            System.Diagnostics.Debug.WriteLine($"[CreateConnection] 目标节点: {targetNode.Name}, ID={targetNode.Id}");
            _viewModel?.AddLog($"[CreateConnection] 开始创建连接: {sourceNode.Name} -> {targetNode.Name}");

            var selectedTab = _viewModel?.WorkflowTabViewModel.SelectedTab;
            if (selectedTab == null)
            {
                System.Diagnostics.Debug.WriteLine($"[CreateConnection] ❌ SelectedTab为null");
                return;
            }

            if (selectedTab.WorkflowConnections == null)
            {
                System.Diagnostics.Debug.WriteLine($"[CreateConnection] ❌ WorkflowConnections为null");
                return;
            }

            var connectionId = $"conn_{Guid.NewGuid().ToString("N")[..8]}";
            var newConnection = new WorkflowConnection(connectionId, sourceNode.Id, targetNode.Id);
            System.Diagnostics.Debug.WriteLine($"[CreateConnection] 创建连接对象，ID: {connectionId}");
            System.Diagnostics.Debug.WriteLine($"[CreateConnection] 添加前当前连接数: {selectedTab.WorkflowConnections.Count}");

            // 智能选择连接点位置
            Point sourcePos, targetPos;
            var deltaX = targetNode.Position.X - sourceNode.Position.X;
            var deltaY = targetNode.Position.Y - sourceNode.Position.Y;
            System.Diagnostics.Debug.WriteLine($"[CreateConnection] 节点位置差: deltaX={deltaX}, deltaY={deltaY}");

            if (Math.Abs(deltaX) > Math.Abs(deltaY))
            {
                // 水平方向
                if (deltaX > 0)
                {
                    sourcePos = sourceNode.RightPortPosition;
                    targetPos = targetNode.LeftPortPosition;
                    System.Diagnostics.Debug.WriteLine($"[CreateConnection] 选择水平连接: 右->左");
                }
                else
                {
                    sourcePos = sourceNode.LeftPortPosition;
                    targetPos = targetNode.RightPortPosition;
                    System.Diagnostics.Debug.WriteLine($"[CreateConnection] 选择水平连接: 左->右");
                }
            }
            else
            {
                // 垂直方向
                if (deltaY > 0)
                {
                    sourcePos = sourceNode.BottomPortPosition;
                    targetPos = targetNode.TopPortPosition;
                    System.Diagnostics.Debug.WriteLine($"[CreateConnection] 选择垂直连接: 下->上");
                }
                else
                {
                    sourcePos = sourceNode.TopPortPosition;
                    targetPos = targetNode.BottomPortPosition;
                    System.Diagnostics.Debug.WriteLine($"[CreateConnection] 选择垂直连接: 上->下");
                }
            }

            newConnection.SourcePosition = sourcePos;
            newConnection.TargetPosition = targetPos;
            System.Diagnostics.Debug.WriteLine($"[CreateConnection] 源位置: {sourcePos}");
            System.Diagnostics.Debug.WriteLine($"[CreateConnection] 目标位置: {targetPos}");

            selectedTab.WorkflowConnections.Add(newConnection);
            System.Diagnostics.Debug.WriteLine($"[CreateConnection] ✓ 连接已添加到集合，当前连接数: {selectedTab.WorkflowConnections.Count}");
            System.Diagnostics.Debug.WriteLine($"[CreateConnection] 新连接是否在集合中: {selectedTab.WorkflowConnections.Contains(newConnection)}");

            _viewModel!.StatusText = $"成功连接: {sourceNode.Name} -> {targetNode.Name}";
            _viewModel.AddLog($"[CreateConnection] ✓ 连接创建完成，当前总连接数: {selectedTab.WorkflowConnections.Count}");
            System.Diagnostics.Debug.WriteLine($"[CreateConnection] ========== CreateConnection 完成 ==========");
        }

        #endregion
    }
}
