using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using SunEyeVision.UI.Controls.Helpers;
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

        // 辅助类
        private WorkflowDragDropHandler? _dragDropHandler;
        private WorkflowPortHighlighter? _portHighlighter;
        private WorkflowConnectionCreator? _connectionCreator;
        private PortPositionService? _portPositionService; // 端口位置查询服务

        private bool _isDragging;
        private WorkflowNode? _draggedNode;
        private System.Windows.Point _startDragPosition;
        private System.Windows.Point _initialNodePosition;

        // 框选相关
        private bool _isBoxSelecting;
        private System.Windows.Point _boxSelectStart;
        private System.Windows.Point[]? _selectedNodesInitialPositions;

        // 连接模式相关
        private WorkflowNode? _connectionSourceNode = null;

        // 拖拽连接相关
        private bool _isDraggingConnection = false;
        private WorkflowNode? _dragConnectionSourceNode = null;
        private Border? _dragConnectionSourceBorder = null; // 拖拽连接时的源节点Border
        private System.Windows.Point _dragConnectionStartPoint;
        private System.Windows.Point _dragConnectionEndPoint;
        private string? _dragConnectionSourcePort = null; // 记录拖拽开始时的源端口
    private Border? _highlightedTargetBorder = null; // 高亮的目标节点Border（用于恢复原始样式）
    private Ellipse? _highlightedTargetPort = null; // 高亮的目标端口（Ellipse）
    private int _dragMoveCounter = 0; // 拖拽移动计数器，用于减少日志输出频率
    private string? _lastHighlightedPort = null; // 上次高亮的端口名称
    private string? _directHitTargetPort = null; // 用户直接命中的目标端口名称
    private Path? _tempConnectionLine = null; // 临时连接线（用于拖拽时显示）
    private PathGeometry? _tempConnectionGeometry = null; // 临时连接线路径几何
    private Border? _highlightedTargetNodeBorder = null; // 拖拽时高亮的目标节点Border（用于显示端口）

        // 连接线路径缓存
        private Services.ConnectionPathCache? _connectionPathCache;

        // 批量延迟更新管理器
        private Services.ConnectionBatchUpdateManager? _batchUpdateManager;

        // 位置节流优化 - 只在移动超过阈值时才触发连接线更新
        private const double PositionUpdateThreshold = 5.0; // 5px 阈值
        private Dictionary<string, Point> _lastReportedNodePositions = new Dictionary<string, Point>();

        /// <summary>
        /// 是否正在拖拽连接（用于绑定，控制连接点是否显示）
        /// </summary>
        public bool IsDraggingConnection
        {
            get => _isDraggingConnection;
            private set
            {
                _isDraggingConnection = value;
                // 不再控制所有端口的可见性，改为只在鼠标悬停节点时显示该节点的端口
            }
        }

        /// <summary>
        /// 是否显示最大外接矩形
        /// </summary>
        public bool ShowBoundingRectangle
        {
            get => (bool)(GetValue(ShowBoundingRectangleProperty) ?? false);
            set => SetValue(ShowBoundingRectangleProperty, value);
        }

        public static readonly DependencyProperty ShowBoundingRectangleProperty =
            DependencyProperty.Register(
                nameof(ShowBoundingRectangle),
                typeof(bool),
                typeof(WorkflowCanvasControl),
                new PropertyMetadata(false, OnShowBoundingRectangleChanged));

        private static void OnShowBoundingRectangleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is WorkflowCanvasControl control)
            {
                control.UpdateBoundingRectangle();
            }
        }

        /// <summary>
        /// 最大外接矩形的源节点ID
        /// </summary>
        public string? BoundingSourceNodeId
        {
            get => (string)GetValue(BoundingSourceNodeIdProperty);
            set => SetValue(BoundingSourceNodeIdProperty, value);
        }

        public static readonly DependencyProperty BoundingSourceNodeIdProperty =
            DependencyProperty.Register(
                nameof(BoundingSourceNodeId),
                typeof(string),
                typeof(WorkflowCanvasControl),
                new PropertyMetadata(null, OnBoundingRectangleChanged));

        /// <summary>
        /// 最大外接矩形的目标节点ID
        /// </summary>
        public string? BoundingTargetNodeId
        {
            get => (string)GetValue(BoundingTargetNodeIdProperty);
            set => SetValue(BoundingTargetNodeIdProperty, value);
        }

        public static readonly DependencyProperty BoundingTargetNodeIdProperty =
            DependencyProperty.Register(
                nameof(BoundingTargetNodeId),
                typeof(string),
                typeof(WorkflowCanvasControl),
                new PropertyMetadata(null, OnBoundingRectangleChanged));

        private static void OnBoundingRectangleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is WorkflowCanvasControl control)
            {
                control.UpdateBoundingRectangle();
            }
        }

        /// <summary>
        /// 获取当前工作流Tab（从 DataContext 获取）
        /// </summary>
        public ViewModels.WorkflowTabViewModel? CurrentWorkflowTab => DataContext as WorkflowTabViewModel;

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

        /// <summary>
        /// DataContext变化时绑定ScaleTransform并初始化ConnectionPathCache
        /// </summary>
        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (DataContext is WorkflowTabViewModel workflowTab)
            {
                // System.Diagnostics.Debug.WriteLine("[WorkflowCanvas DataContextChanged] ════════════════════════════════════");
                // System.Diagnostics.Debug.WriteLine($"[WorkflowCanvas DataContextChanged] DataContext已设置为: {workflowTab.Name}");
                // System.Diagnostics.Debug.WriteLine($"[WorkflowCanvas DataContextChanged]   Id: {workflowTab.Id}");
                // System.Diagnostics.Debug.WriteLine($"[WorkflowCanvas DataContextChanged]   节点数: {workflowTab.WorkflowNodes?.Count ?? 0}, 连接数: {workflowTab.WorkflowConnections?.Count ?? 0}");
                // System.Diagnostics.Debug.WriteLine($"[WorkflowCanvas DataContextChanged]   WorkflowNodes Hash: {workflowTab.WorkflowNodes?.GetHashCode() ?? 0}");
                // System.Diagnostics.Debug.WriteLine($"[WorkflowCanvas DataContextChanged]   WorkflowConnections Hash: {workflowTab.WorkflowConnections?.GetHashCode() ?? 0}");

                // 将WorkflowCanvas的RenderTransform绑定到ViewModel的ScaleTransform
                var binding = new System.Windows.Data.Binding("ScaleTransform")
                {
                    Source = workflowTab,
                    Mode = System.Windows.Data.BindingMode.OneWay
                };
                WorkflowCanvas.SetBinding(Canvas.RenderTransformProperty, binding);
                // System.Diagnostics.Debug.WriteLine("[WorkflowCanvas DataContextChanged] ? 已绑定ScaleTransform到WorkflowCanvas");

                // 设置SmartPathConverter的节点集合和连接集合
                Converters.SmartPathConverter.Nodes = workflowTab.WorkflowNodes;
                Converters.SmartPathConverter.Connections = workflowTab.WorkflowConnections;
                // System.Diagnostics.Debug.WriteLine($"[WorkflowCanvas DataContextChanged] ✅ SmartPathConverter.Nodes/Connections已设置");
                // System.Diagnostics.Debug.WriteLine($"[WorkflowCanvas DataContextChanged]   Nodes Hash: {Converters.SmartPathConverter.Nodes?.GetHashCode() ?? 0}");
                // System.Diagnostics.Debug.WriteLine($"[WorkflowCanvas DataContextChanged]   Connections Hash: {Converters.SmartPathConverter.Connections?.GetHashCode() ?? 0}");

                // 🔥 关键修复：强制刷新所有 ItemsControl 的 ItemsSource 绑定
                // System.Diagnostics.Debug.WriteLine("[WorkflowCanvas DataContextChanged] 🔥 强制刷新 ItemsControl 绑定...");
                ForceRefreshItemsControls();

                // 每次 DataContext 变化时都重新创建 ConnectionPathCache（确保每个工作流独立）
                // System.Diagnostics.Debug.WriteLine("[WorkflowCanvas DataContextChanged] ╔═════════════════════════════════════════════════════╗");
                // System.Diagnostics.Debug.WriteLine("[WorkflowCanvas DataContextChanged] ║      正在创建路径计算器...                        ║");
                // System.Diagnostics.Debug.WriteLine("[WorkflowCanvas DataContextChanged] ╚═════════════════════════════════════════════════════╝");

                try
                {
                    var calculatorType = Services.PathCalculators.PathCalculatorFactory.CurrentCalculatorType;
                    // System.Diagnostics.Debug.WriteLine($"[WorkflowCanvas DataContextChanged] 当前路径计算器类型: {calculatorType}");

                    var calculator = Services.PathCalculators.PathCalculatorFactory.CreateCalculator();
                    // System.Diagnostics.Debug.WriteLine("[WorkflowCanvas DataContextChanged] ✅ 路径计算器实例创建成功");

                    // 每次都创建新的 ConnectionPathCache，确保每个工作流独立
                    _connectionPathCache = new Services.ConnectionPathCache(
                        workflowTab.WorkflowNodes,
                        calculator
                    );
                    // System.Diagnostics.Debug.WriteLine("[WorkflowCanvas DataContextChanged] ✅ ConnectionPathCache 创建成功（工作流独立）");

                    // 设置SmartPathConverter的PathCache引用
                    Converters.SmartPathConverter.PathCache = _connectionPathCache;
                    // System.Diagnostics.Debug.WriteLine("[WorkflowCanvas DataContextChanged] ✅ SmartPathConverter.PathCache已设置");

                    // 初始化批量延迟更新管理器
                    if (_connectionPathCache != null)
                    {
                        _batchUpdateManager = new Services.ConnectionBatchUpdateManager(_connectionPathCache);
                        _batchUpdateManager.SetCurrentTab(workflowTab);
                        // System.Diagnostics.Debug.WriteLine("[WorkflowCanvas DataContextChanged] ✅ BatchUpdateManager已初始化");
                    }

                        // 预热缓存
                        _connectionPathCache.WarmUp(workflowTab.WorkflowConnections);
                        var stats = _connectionPathCache.GetStatistics();
                        // System.Diagnostics.Debug.WriteLine($"[WorkflowCanvas DataContextChanged] 缓存预热完成: {stats.CacheSize}个连接");

                        // 订阅连接集合变化事件
                        if (workflowTab.WorkflowConnections != null)
                        {
                            workflowTab.WorkflowConnections.CollectionChanged += (s, args) =>
                            {
                                // System.Diagnostics.Debug.WriteLine($"[WorkflowCanvas DataContextChanged] 连接集合变化: Added={args.NewItems?.Count ?? 0}, Removed={args.OldItems?.Count ?? 0}");
                                UpdateBoundingRectangle();
                                if (_connectionPathCache != null)
                                {
                                    _connectionPathCache.MarkAllDirty();
                                }
                            };
                        }

                        // 订阅节点集合变化事件
                        if (workflowTab.WorkflowNodes is ObservableCollection<WorkflowNode> nodesCollection)
                        {
                            nodesCollection.CollectionChanged += (s, args) =>
                            {
                                Converters.SmartPathConverter.Nodes = workflowTab.WorkflowNodes;
                                RefreshAllConnectionPaths();
                                if (_connectionPathCache != null)
                                {
                                    if (args.NewItems != null)
                                    {
                                        foreach (WorkflowNode node in args.NewItems)
                                        {
                                            _connectionPathCache.MarkNodeDirty(node.Id);
                                        }
                                    }
                                }
                            };
                        }

                        // System.Diagnostics.Debug.WriteLine("[WorkflowCanvas DataContextChanged] ╔═════════════════════════════════════════════════════╗");
                        // System.Diagnostics.Debug.WriteLine("[WorkflowCanvas DataContextChanged] ║  ✅ 路径计算器初始化成功！                        ║");
                        // System.Diagnostics.Debug.WriteLine("[WorkflowCanvas DataContextChanged] ╚═════════════════════════════════════════════════════╝");
                    }
                    catch (Exception ex)
                    {
                        // System.Diagnostics.Debug.WriteLine($"[WorkflowCanvas DataContextChanged] ❌ 路径计算器创建失败: {ex.GetType().Name}");
                        // System.Diagnostics.Debug.WriteLine($"[WorkflowCanvas DataContextChanged] 消息: {ex.Message}");

                        // 备用方案：使用 PathCalculatorFactory 创建（默认贝塞尔曲线）
                        _connectionPathCache = new Services.ConnectionPathCache(
                            workflowTab.WorkflowNodes,
                            Services.PathCalculators.PathCalculatorFactory.CreateCalculator()
                        );
                        Converters.SmartPathConverter.PathCache = _connectionPathCache;
                        // System.Diagnostics.Debug.WriteLine("[WorkflowCanvas DataContextChanged] ✅ OrthogonalPathCalculator 备用方案已启用");
                    }
                }
            else
            {
                // System.Diagnostics.Debug.WriteLine($"[WorkflowCanvas DataContextChanged] ⚠ DataContext 不是 WorkflowTabViewModel: {DataContext?.GetType().Name ?? "null"}");
            }
        }

        /// <summary>
        /// 强制刷新所有 ItemsControl 的 ItemsSource 绑定
        /// 这是修复工作流 Tab 共享同一画布问题的关键
        /// </summary>
        public void ForceRefreshItemsControls()
        {
            try
            {
                // 查找 WorkflowCanvas 中的所有 ItemsControl
                var itemsControls = FindVisualChildren<ItemsControl>(WorkflowCanvas).ToList();

                int refreshed = 0;
                foreach (var itemsControl in itemsControls)
                {
                    // 获取绑定表达式
                    var bindingExpression = itemsControl.GetBindingExpression(ItemsControl.ItemsSourceProperty);
                    if (bindingExpression != null)
                    {
                        // 刷新绑定表达式，强制重新从DataContext读取数据
                        bindingExpression.UpdateTarget();
                    }
                    else
                    {
                        // 如果没有绑定表达式，可能是因为绑定还没有建立
                        // 我们需要手动触发DataContextChanged事件
                        var oldDataContext = itemsControl.DataContext;
                        itemsControl.DataContext = null;
                        itemsControl.DataContext = oldDataContext;
                    }

                    refreshed++;
                }
            }
            catch (Exception)
            {
                // 忽略异常
            }
        }

        /// <summary>
        /// 查找所有指定类型的子元素
        /// </summary>
        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj == null) yield break;

            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                DependencyObject child = System.Windows.Media.VisualTreeHelper.GetChild(depObj, i);
                if (child != null && child is T)
                {
                    yield return (T)child;
                }

                foreach (T childOfChild in FindVisualChildren<T>(child))
                {
                    yield return childOfChild;
                }
            }
        }

        public WorkflowCanvasControl()
        {
            InitializeComponent();

            // 验证BoundingRectangle元素

            // 禁用设备像素对齐，启用亚像素渲染
            this.SnapsToDevicePixels = false;
            this.UseLayoutRounding = false;

            // 监听DataContext变化，绑定ScaleTransform
            DataContextChanged += OnDataContextChanged;

            Loaded += WorkflowCanvasControl_Loaded;

            // 初始化临时连接线
            _tempConnectionLine = this.FindName("TempConnectionLine") as Path;
            _tempConnectionGeometry = this.FindName("TempConnectionGeometry") as PathGeometry;
            if (_tempConnectionLine != null)
            {
                _tempConnectionLine.Visibility = Visibility.Collapsed;
            }

            // 初始化辅助类
            _dragDropHandler = new WorkflowDragDropHandler(this);
        }

        private void WorkflowCanvasControl_Loaded(object sender, RoutedEventArgs e)
        {
            // System.Diagnostics.Debug.WriteLine("[WorkflowCanvas_Loaded] ════════════════════════════════════");
            // System.Diagnostics.Debug.WriteLine("[WorkflowCanvas_Loaded] ✅ WorkflowCanvasControl Loaded Event Triggered");

            // 检查DataContext
            var dataContext = DataContext;
            // System.Diagnostics.Debug.WriteLine($"[WorkflowCanvas_Loaded] DataContext: {dataContext?.GetType().Name ?? "null"}");
            if (dataContext is WorkflowTabViewModel workflowTab)
            {
                // System.Diagnostics.Debug.WriteLine($"[WorkflowCanvas_Loaded]   Tab Name: {workflowTab.Name}");
                // System.Diagnostics.Debug.WriteLine($"[WorkflowCanvas_Loaded]   Tab Id: {workflowTab.Id}");
                // System.Diagnostics.Debug.WriteLine($"[WorkflowCanvas_Loaded]   Nodes Count: {workflowTab.WorkflowNodes?.Count ?? 0}");
                // System.Diagnostics.Debug.WriteLine($"[WorkflowCanvas_Loaded]   Connections Count: {workflowTab.WorkflowConnections?.Count ?? 0}");
                // System.Diagnostics.Debug.WriteLine($"[WorkflowCanvas_Loaded]   CurrentScale: {workflowTab.CurrentScale:P0}");
                // System.Diagnostics.Debug.WriteLine($"[WorkflowCanvas_Loaded]   ScaleTransform Hash: {workflowTab.ScaleTransform?.GetHashCode() ?? 0}");
                // System.Diagnostics.Debug.WriteLine($"[WorkflowCanvas_Loaded]   WorkflowNodes Hash: {workflowTab.WorkflowNodes?.GetHashCode() ?? 0}");
                // System.Diagnostics.Debug.WriteLine($"[WorkflowCanvas_Loaded]   WorkflowConnections Hash: {workflowTab.WorkflowConnections?.GetHashCode() ?? 0}");
            }
            else
            {
                // System.Diagnostics.Debug.WriteLine($"[WorkflowCanvas_Loaded] ⚠ DataContext is not WorkflowTabViewModel!");
            }

            // 添加调试：检查 ItemsControl 的绑定
            // System.Diagnostics.Debug.WriteLine("[WorkflowCanvas_Loaded] 🔍 检查 UI 元素绑定...");
            var nodesItemsControl = this.FindName("WorkflowCanvas") as Canvas;
            if (nodesItemsControl != null)
            {
                // System.Diagnostics.Debug.WriteLine($"[WorkflowCanvas_Loaded]   WorkflowCanvas 元素存在");
            }

            // 获取 MainWindowViewModel
            if (Window.GetWindow(this) is MainWindow mainWindow)
            {
                _viewModel = mainWindow.DataContext as MainWindowViewModel;
                if (_viewModel != null)
                {
                    // System.Diagnostics.Debug.WriteLine($"[WorkflowCanvas_Loaded] ✅ MainWindowViewModel 获取成功");

                    // 初始化辅助类（需要ViewModel）
                    if (_portHighlighter == null)
                    {
                        _portHighlighter = new WorkflowPortHighlighter(_viewModel);
                        // System.Diagnostics.Debug.WriteLine($"[WorkflowCanvas_Loaded] ✅ PortHighlighter 初始化成功");
                    }
                    if (_connectionCreator == null)
                    {
                        _connectionCreator = new WorkflowConnectionCreator(_viewModel);
                        // System.Diagnostics.Debug.WriteLine($"[WorkflowCanvas_Loaded] ✅ ConnectionCreator 初始化成功");
                    }

                    // 初始化端口位置查询服务（完全解耦方案）
                    if (_portPositionService == null)
                    {
                        _portPositionService = new PortPositionService(WorkflowCanvas, NodeStyles.Standard);
                        // System.Diagnostics.Debug.WriteLine($"[WorkflowCanvas_Loaded] ✅ PortPositionService 初始化成功");
                    }
                }
                else
                {
                    // System.Diagnostics.Debug.WriteLine($"[WorkflowCanvas_Loaded] ⚠ MainWindowViewModel 获取失败");
                }
            }

            // 注意：ConnectionPathCache 的初始化已移到 OnDataContextChanged 方法中
            // 这样可以确保在 DataContext 设置后立即初始化，避免 PathCache 为 null 的问题

            // System.Diagnostics.Debug.WriteLine("[WorkflowCanvas_Loaded] ════════════════════════════════════");
        }

        /// <summary>
        /// 刷新所有连接的路径（触发重新计算）
        /// </summary>
        private void RefreshAllConnectionPaths()
        {
            if (CurrentWorkflowTab == null) return;

            // 标记所有缓存为脏数据
            if (_connectionPathCache != null)
            {
                _connectionPathCache.MarkAllDirty();
            }

            // 使用 WorkflowPathCalculator 刷新所有连接路径
            WorkflowPathCalculator.RefreshAllConnectionPaths(CurrentWorkflowTab.WorkflowConnections);
        }

        /// <summary>
        /// 设置路径计算器（支持运行时切换）
        /// </summary>
        public void SetPathCalculator(string pathCalculatorType)
        {

            try
            {
                // 解析路径计算器类型
                if (System.Enum.TryParse<Services.PathCalculators.PathCalculatorType>(pathCalculatorType, true, out var type))
                {
                    // 创建新的路径计算器实例
                    var newCalculator = Services.PathCalculators.PathCalculatorFactory.CreateCalculator(type);

                    // 替换ConnectionPathCache
                    if (CurrentWorkflowTab != null)
                    {
                        _connectionPathCache = new Services.ConnectionPathCache(
                            CurrentWorkflowTab.WorkflowNodes,
                            newCalculator
                        );

                        // 更新SmartPathConverter的缓存引用
                        Converters.SmartPathConverter.PathCache = _connectionPathCache;

                        // 刷新所有连接路径
                        RefreshAllConnectionPaths();

                    }
                }
                else
                {
                }
            }
            catch (System.Exception ex)
            {
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
                // 如果没有正在拖拽连接，则隐藏当前节点的端口
                if (!_isDraggingConnection)
                {
                    SetPortsVisibility(border, false);
                }
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
        /// 端口鼠标进入事件 - 调试用
        /// </summary>
        private void Port_MouseEnter(object sender, MouseEventArgs e)
        {
            // 移除高频日志
        }

        /// <summary>
        /// 端口鼠标离开事件 - 调试用
        /// </summary>
        private void Port_MouseLeave(object sender, MouseEventArgs e)
        {
            // 移除高频日志
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
            var nodeBorders = WorkflowVisualHelper.FindAllVisualChildren<Border>(WorkflowCanvas);

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
            if (border.Tag is WorkflowNode node)
            {
                var ellipses = WorkflowVisualHelper.FindAllVisualChildren<Ellipse>(border);
                foreach (var ellipse in ellipses)
                {
                    var ellipseName = ellipse.Name ?? "";
                    if (ellipseName.Contains("Port"))
                    {
                        // 使用 Visibility.Collapsed 而不是 Opacity，确保端口不响应鼠标事件
                        // Collapsed 的元素不可见且不响应鼠标事件
                        ellipse.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
                    }
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
                // 不设置 e.Handled，让事件冒泡到 Port_MouseLeftButtonUp
            // 设置 e.Handled = true 会导致 Port_MouseLeftButtonUp 无法被触发，
            // 从而导致临时连接线无法隐藏
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
            // 不设置 e.Handled，让事件冒泡到 Port_MouseLeftButtonUp
            // 设置 e.Handled = true 会导致 Port_MouseLeftButtonUp 无法被触发，
            // 从而导致临时连接线无法隐藏
        }

        /// <summary>
        /// 节点鼠标左键释放 - 结束拖拽
        /// </summary>
        private void Node_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging && _draggedNode != null)
            {
                // 🔥 减少日志输出以提高性能
                // System.Diagnostics.Debug.WriteLine($"[Node_LeftButtonUp] ========== 节点释放 [{DateTime.Now:HH:mm:ss.fff}] ==========");

                // 拖拽结束
                if (_viewModel?.WorkflowTabViewModel.SelectedTab != null)
                {
                    var selectedNodes = _viewModel.WorkflowTabViewModel.SelectedTab.WorkflowNodes
                        .Where(n => n.IsSelected)
                        .ToList();

                    if (selectedNodes.Count > 0 && _selectedNodesInitialPositions != null)
                    {
                        // 计算统一的移动偏移量（从初始位置到当前位置）
                        var delta = new Vector(
                            selectedNodes[0].Position.X - _selectedNodesInitialPositions[0].X,
                            selectedNodes[0].Position.Y - _selectedNodesInitialPositions[0].Y
                        );

                        /*
                        */

                        // 🔥 关键修复：不要再次执行 BatchMoveNodesCommand
                        // 因为节点位置已经在 Node_MouseMove 中被更新了
                        // 如果这里再执行一次，会导致节点被移动两次

                        // 拖拽结束后，强制更新所有相关连接的缓存
                        if (_connectionPathCache != null)
                        {
                            foreach (var node in selectedNodes)
                            {
                                _connectionPathCache.MarkNodeDirty(node.Id);
                                // System.Diagnostics.Debug.WriteLine($"[Node_LeftButtonUp]   已标记节点 {node.Name} 为脏");
                            }
                        }

                        // 🔥 使用批量延迟更新管理器：立即执行所有待处理的更新
                        if (_batchUpdateManager != null)
                        {
                            _batchUpdateManager.ForceUpdateAll();
                            // System.Diagnostics.Debug.WriteLine($"[Node_LeftButtonUp] 已强制执行所有待处理的连接更新");
                        }

                        // 🔥 TODO: 如果需要支持撤销/重做，需要在这里创建并执行命令
                        // 但是要确保命令不会重复移动节点
                    }
                }

                _isDragging = false;
                _draggedNode = null!;
                (sender as Border)?.ReleaseMouseCapture();
                // System.Diagnostics.Debug.WriteLine($"[Node_LeftButtonUp]   拖拽已结束，_isDragging={_isDragging}");

                // 清除位置节流记录，准备下次拖拽
                ClearPositionThrottling();
            }
        }

        /// <summary>
        /// 节点鼠标移动 - 执行拖拽（方案5优化：分层更新策略 - 实时位置+延迟路径）
        /// </summary>
        private void Node_MouseMove(object sender, MouseEventArgs e)
        {
            // 如果正在拖拽连接，则不移动节点（避免冲突）
            if (_isDraggingConnection)
                return;

            if (_isDragging && _draggedNode != null && e.LeftButton == MouseButtonState.Pressed)
            {
                var currentPosition = e.GetPosition(WorkflowCanvas);

                // 计算从拖动开始到现在的总偏移量
                var totalOffset = currentPosition - _startDragPosition;

                // 5A: 获取所有选中节点
                var selectedNodes = _viewModel?.WorkflowTabViewModel.SelectedTab?.WorkflowNodes
                    .Where(n => n.IsSelected)
                    .ToList();

                if (selectedNodes != null && selectedNodes.Count > 0 && _selectedNodesInitialPositions != null)
                {
                    // 🔥 关键优化：立即更新节点位置（实时层），不使用批处理
                    // 位置更新必须实时响应鼠标移动，否则会出现延迟和闪烁
                    for (int i = 0; i < selectedNodes.Count && i < _selectedNodesInitialPositions.Length; i++)
                    {
                        var newPos = new System.Windows.Point(
                            _selectedNodesInitialPositions[i].X + totalOffset.X,
                            _selectedNodesInitialPositions[i].Y + totalOffset.Y
                        );

                        // 直接设置位置，立即触发PropertyChanged
                        // 这会立即更新Canvas绑定，节点位置实时跟随鼠标
                        selectedNodes[i].Position = newPos;
                    }

                    // 5C: 路径更新使用位置节流 + 批量延迟机制（双层优化）
                    // 路径计算成本高，先通过距离节流减少更新次数，再通过批量延迟合并快速更新
                    if (_batchUpdateManager != null)
                    {
                        // 收集需要更新的节点ID（通过位置节流过滤）
                        var nodesToUpdate = new List<string>();
                        foreach (var node in selectedNodes)
                        {
                            if (ShouldScheduleConnectionUpdate(node.Id, node.Position))
                            {
                                nodesToUpdate.Add(node.Id);
                            }
                        }

                        // 只有当有节点需要更新时才调用批量更新管理器
                        if (nodesToUpdate.Count > 0)
                        {
                            _batchUpdateManager.ScheduleUpdateForNodes(nodesToUpdate);
                        }
                    }
                }
                else
                {
                    // 单个节点移动（向后兼容）
                    var newPos = new System.Windows.Point(
                        _initialNodePosition.X + totalOffset.X,
                        _initialNodePosition.Y + totalOffset.Y
                    );

                    // 直接设置位置，立即触发PropertyChanged
                    _draggedNode.Position = newPos;

                    // 5C: 单个节点的路径更新也使用位置节流机制
                    if (_batchUpdateManager != null)
                    {
                        if (ShouldScheduleConnectionUpdate(_draggedNode.Id, _draggedNode.Position))
                        {
                            _batchUpdateManager.ScheduleUpdateForNode(_draggedNode.Id);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 判断是否应该触发连接线更新（位置节流）
        /// 只有当节点移动距离超过阈值时才触发更新
        /// </summary>
        private bool ShouldScheduleConnectionUpdate(string nodeId, Point currentPosition)
        {
            // 如果没有记录过该节点的位置，则记录并返回true（首次更新）
            if (!_lastReportedNodePositions.ContainsKey(nodeId))
            {
                _lastReportedNodePositions[nodeId] = currentPosition;
                return true;
            }

            // 计算距离上次报告位置的偏移
            Point lastPosition = _lastReportedNodePositions[nodeId];
            double deltaX = Math.Abs(currentPosition.X - lastPosition.X);
            double deltaY = Math.Abs(currentPosition.Y - lastPosition.Y);

            // 检查是否超过阈值
            if (deltaX > PositionUpdateThreshold || deltaY > PositionUpdateThreshold)
            {
                _lastReportedNodePositions[nodeId] = currentPosition;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 清除位置节流记录（在拖拽结束时调用）
        /// </summary>
        private void ClearPositionThrottling()
        {
            _lastReportedNodePositions.Clear();
        }

        /// <summary>
        /// 端口鼠标左键按下 - 开始拖拽连接线
        /// </summary>
        private void Port_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("════════════════════════════════════════════════════════════");
            System.Diagnostics.Debug.WriteLine("[Port_MouseLeftButtonDown] ▶ 触发");

            var mousePos = e.GetPosition(WorkflowCanvas);
            System.Diagnostics.Debug.WriteLine($"[Port_MouseLeftButtonDown] 鼠标位置: ({mousePos.X:F1}, {mousePos.Y:F1})");

            if (sender is not Ellipse ellipse || ellipse.Tag is not string portName)
            {
                System.Diagnostics.Debug.WriteLine($"[Port_MouseLeftButtonDown] ✗ 提前返回: sender类型={sender?.GetType().Name ?? "null"}, Tag类型={(sender is Ellipse el ? el.Tag?.GetType().Name ?? "null" : "N/A")}");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"[Port_MouseLeftButtonDown] ✓ Ellipse: Name={ellipse.Name}, Tag={portName}, Visibility={ellipse.Visibility}");

            // 保护：如果已经在拖拽状态，直接返回，不启动新的拖拽
            if (_isDraggingConnection)
            {
                System.Diagnostics.Debug.WriteLine($"[Port_MouseLeftButtonDown] ⚠ 已在拖拽状态，跳过");
                e.Handled = true;
                return;
            }

            // 获取父节点Border（向上遍历查找）
            DependencyObject? current = VisualTreeHelper.GetParent(ellipse);
            Border? border = null;
            WorkflowNode? node = null;

            while (current != null)
            {
                if (current is Border currentBorder && currentBorder.Tag is WorkflowNode workflowNode)
                {
                    border = currentBorder;
                    node = workflowNode;
                    break;
                }
                current = VisualTreeHelper.GetParent(current);
            }

            if (border == null || node == null)
            {
                System.Diagnostics.Debug.WriteLine($"[Port_MouseLeftButtonDown] ✗ 未找到父节点: border={border != null}, node={node?.Name ?? "null"}");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"[Port_MouseLeftButtonDown] ✓ 找到父节点: {node.Name} (Id={node.Id})");

            // 设置连接拖拽状态
            _isDraggingConnection = true;
            _dragConnectionSourceNode = node;
            _dragConnectionSourceBorder = border; // 保存源节点的Border
            _dragConnectionSourcePort = portName;

            System.Diagnostics.Debug.WriteLine($"[Port_MouseLeftButtonDown] ✓✓✓ 拖拽状态已设置: _isDraggingConnection=True, 源节点={node.Name}, 源端口={portName}");

            // 保持源节点的端口可见
            SetPortsVisibility(border, true);

            // 获取端口位置
            Point portPosition = GetPortPositionByName(node, portName);
            _dragConnectionStartPoint = portPosition;
            _dragConnectionEndPoint = portPosition;

            System.Diagnostics.Debug.WriteLine($"[Port_MouseLeftButtonDown] 端口位置: ({portPosition.X:F1}, {portPosition.Y:F1})");

            // 显示临时连接线
            if (_tempConnectionLine != null && _tempConnectionGeometry != null)
            {
                _tempConnectionLine.Visibility = Visibility.Visible;
                UpdateTempConnectionPath(portPosition, portPosition);
                System.Diagnostics.Debug.WriteLine($"[Port_MouseLeftButtonDown] ✓ 临时连接线已显示");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[Port_MouseLeftButtonDown] ⚠ 临时连接线未初始化: _tempConnectionLine={_tempConnectionLine != null}, _tempConnectionGeometry={_tempConnectionGeometry != null}");
            }

            // 捕获鼠标
            ellipse.CaptureMouse();
            System.Diagnostics.Debug.WriteLine($"[Port_MouseLeftButtonDown] ✓ 鼠标已捕获");

            // 阻止事件冒泡到Border的Node_MouseLeftButtonDown
            // 这样可以避免在拖拽连接时错误地触发节点移动
            e.Handled = true;
            System.Diagnostics.Debug.WriteLine("════════════════════════════════════════════════════════════");
        }

        /// <summary>
        /// 更新临时连接线路径
        /// </summary>
        private void UpdateTempConnectionPath(Point startPoint, Point endPoint)
        {
            if (_tempConnectionGeometry == null)
                return;

            // 创建简单的直线路径
            var figure = new PathFigure
            {
                StartPoint = startPoint,
                IsClosed = false
            };
            
            var lineSegment = new LineSegment(endPoint, true);
            figure.Segments.Add(lineSegment);
            
            _tempConnectionGeometry.Figures.Clear();
            _tempConnectionGeometry.Figures.Add(figure);
        }

        /// <summary>
        /// 端口鼠标左键释放 - 结束拖拽连接线
        /// </summary>
        private void Port_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"[Port_MouseLeftButtonUp] ▶ 触发, _isDraggingConnection={_isDraggingConnection}");

            if (!_isDraggingConnection || _dragConnectionSourceNode == null)
            {
                System.Diagnostics.Debug.WriteLine($"[Port_MouseLeftButtonUp] ✗ 提前返回: _isDraggingConnection={_isDraggingConnection}, _dragConnectionSourceNode={(_dragConnectionSourceNode?.Name ?? "null")}");
                return;
            }

            // 释放鼠标捕获
            if (sender is Ellipse ellipse)
            {
                ellipse.ReleaseMouseCapture();
            }

            // 执行命中测试目标端口
            var mousePos = e.GetPosition(WorkflowCanvas);
            System.Diagnostics.Debug.WriteLine($"[Port_MouseLeftButtonUp] 鼠标位置: ({mousePos.X:F1}, {mousePos.Y:F1})");

            var hitTestResult = VisualTreeHelper.HitTest(WorkflowCanvas, mousePos);

            if (hitTestResult?.VisualHit is not null)
            {
                System.Diagnostics.Debug.WriteLine($"[Port_MouseLeftButtonUp] 命中测试: VisualHit类型={hitTestResult.VisualHit.GetType().Name}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[Port_MouseLeftButtonUp] ⚠ 命中测试: 未命中任何视觉元素");
            }

            if (hitTestResult?.VisualHit is Ellipse targetEllipse &&
                targetEllipse.Tag is string targetPortName &&
                targetEllipse.Name.Contains("PortEllipse"))
            {
                System.Diagnostics.Debug.WriteLine($"[Port_MouseLeftButtonUp] ✓ 目标Ellipse: Name={targetEllipse.Name}, Tag={targetPortName}");

                // 获取目标节点 - 向上遍历视觉树找到 Border (Ellipse → Grid → Border)
                Border? targetBorder = null;
                DependencyObject? parent = VisualTreeHelper.GetParent(targetEllipse);
                while (parent != null)
                {
                    if (parent is Border border)
                    {
                        targetBorder = border;
                        break;
                    }
                    parent = VisualTreeHelper.GetParent(parent);
                }

                if (targetBorder?.Tag is WorkflowNode targetNode &&
                    targetNode != _dragConnectionSourceNode)
                {
                    System.Diagnostics.Debug.WriteLine($"[Port_MouseLeftButtonUp] ✓ 目标节点: {targetNode.Name} (Id={targetNode.Id})");
                    System.Diagnostics.Debug.WriteLine($"[Port_MouseLeftButtonUp] 源节点: {_dragConnectionSourceNode.Name}, 源端口: {_dragConnectionSourcePort ?? "RightPort"}, 目标端口: {targetPortName}");

                    // 创建连接
                    var connectionCreated = CreateConnectionBetweenPorts(
                        _dragConnectionSourceNode,
                        _dragConnectionSourcePort ?? "RightPort",
                        targetNode,
                        targetPortName
                    );

                    System.Diagnostics.Debug.WriteLine($"[Port_MouseLeftButtonUp] 连接创建结果: {connectionCreated}");

                    // 连接创建成功后，阻止事件冒泡，避免触发节点选择导致图像预览器被隐藏
                    if (connectionCreated)
                    {
                        e.Handled = true;
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[Port_MouseLeftButtonUp] ✗ 目标Border/Node无效: targetBorder={targetBorder != null}, targetNode={(targetBorder?.Tag as WorkflowNode)?.Name ?? "null"}");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[Port_MouseLeftButtonUp] ✗ 未命中有效的端口Ellipse: hitTestResult={hitTestResult != null}, isEllipse={hitTestResult?.VisualHit is Ellipse}, Name={(hitTestResult?.VisualHit as Ellipse)?.Name ?? "null"}");
            }

            // 重置连接拖拽状态
            _isDraggingConnection = false;
            _dragConnectionSourceNode = null;
            _dragConnectionSourcePort = null;

            // 清理源节点的端口可见性
            if (_dragConnectionSourceBorder != null)
            {
                SetPortsVisibility(_dragConnectionSourceBorder, false);
                _dragConnectionSourceBorder = null;
            }

            // 隐藏临时连接线
            if (_tempConnectionLine != null)
            {
                var oldVisibility = _tempConnectionLine.Visibility;
                _tempConnectionLine.Visibility = Visibility.Collapsed;

                // 清除几何数据，避免旧数据残留
                if (_tempConnectionGeometry != null)
                {
                    _tempConnectionGeometry.Figures.Clear();
                }

                _tempConnectionLine.UpdateLayout();
            }
            else
            {
            }

            // 清理高亮的目标节点
            if (_highlightedTargetNodeBorder != null)
            {
                SetPortsVisibility(_highlightedTargetNodeBorder, false);
                _highlightedTargetNodeBorder = null;
            }

            // 注意：在连接创建成功时已设置 e.Handled = true 阻止事件冒泡
            // 如果没有成功创建连接，事件会继续冒泡，允许其他处理器响应
        }

        /// <summary>
        /// 端口鼠标移动 - 更新临时连接线
        /// </summary>
        private void Port_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDraggingConnection || e.LeftButton != MouseButtonState.Pressed)
                return;

            // 更新临时连接线终点
            var currentPosition = e.GetPosition(WorkflowCanvas);
            _dragConnectionEndPoint = currentPosition;

            if (_tempConnectionLine != null)
            {
                UpdateTempConnectionPath(_dragConnectionStartPoint, currentPosition);
            }

            // HitTest 查找鼠标下的节点
            var hitResult = VisualTreeHelper.HitTest(WorkflowCanvas, currentPosition);
            if (hitResult?.VisualHit is DependencyObject obj)
            {
                // 向上查找 Border（节点容器）
                Border? targetBorder = null;
                while (obj != null)
                {
                    if (obj is Border border && border.Tag is WorkflowNode node)
                    {
                        targetBorder = border;
                        break;
                    }
                    obj = VisualTreeHelper.GetParent(obj);
                }

                // 如果目标节点改变，更新端口可见性
                if (targetBorder != null && targetBorder != _highlightedTargetNodeBorder)
                {
                    // 隐藏之前高亮的节点端口
                    if (_highlightedTargetNodeBorder != null)
                    {
                        SetPortsVisibility(_highlightedTargetNodeBorder, false);
                    }

                    // 显示新的目标节点端口
                    _highlightedTargetNodeBorder = targetBorder;
                    SetPortsVisibility(_highlightedTargetNodeBorder, true);
                }
            }
            else
            {
                // 鼠标不在任何节点上，隐藏之前高亮的节点端口
                if (_highlightedTargetNodeBorder != null)
                {
                    SetPortsVisibility(_highlightedTargetNodeBorder, false);
                    _highlightedTargetNodeBorder = null;
                }
            }

            // 不设置 e.Handled，让事件冒泡到 Port_MouseLeftButtonUp
            // 设置 e.Handled = true 会导致 Port_MouseLeftButtonUp 无法被触发，
            // 从而导致临时连接线无法隐藏
        }

        /// <summary>
        /// 根据端口名称获取端口位置
        /// </summary>
        private Point GetPortPositionByName(WorkflowNode node, string portName)
        {
            return portName switch
            {
                "TopPort" => node.TopPortPosition,
                "BottomPort" => node.BottomPortPosition,
                "LeftPort" => node.LeftPortPosition,
                "RightPort" => node.RightPortPosition,
                _ => node.RightPortPosition
            };
        }

        /// <summary>
        /// 在两个端口之间创建连接
        /// </summary>
        /// <returns>是否成功创建连接</returns>
        private bool CreateConnectionBetweenPorts(WorkflowNode sourceNode, string sourcePort, 
            WorkflowNode targetNode, string targetPort)
        {
            System.Diagnostics.Debug.WriteLine($"[CreateConnectionBetweenPorts] ▶ 开始创建连接");
            System.Diagnostics.Debug.WriteLine($"[CreateConnectionBetweenPorts]   - 源节点: {sourceNode?.Name ?? "null"}, 源端口: {sourcePort}");
            System.Diagnostics.Debug.WriteLine($"[CreateConnectionBetweenPorts]   - 目标节点: {targetNode?.Name ?? "null"}, 目标端口: {targetPort}");
            
            var selectedTab = _viewModel?.WorkflowTabViewModel.SelectedTab;
            if (selectedTab == null)
            {
                System.Diagnostics.Debug.WriteLine($"[CreateConnectionBetweenPorts] ✗ selectedTab 为 null, _viewModel={(_viewModel != null ? "已设置" : "null")}");
                return false;
            }

            // 检查是否已存在相同连接（同时检查节点和端口）
            var existingConnection = selectedTab.WorkflowConnections?.FirstOrDefault(
                c => c.SourceNodeId == sourceNode.Id && 
                     c.TargetNodeId == targetNode.Id &&
                     c.SourcePort == sourcePort && 
                     c.TargetPort == targetPort);
            
            if (existingConnection != null)
            {
                System.Diagnostics.Debug.WriteLine($"[CreateConnectionBetweenPorts] ✗ 连接已存在: {existingConnection.Id} (源端口={sourcePort}, 目标端口={targetPort})");
                return false;
            }

            // 创建新连接
            var connectionId = $"conn_{Guid.NewGuid().ToString("N")[..8]}";
            var newConnection = new WorkflowConnection(connectionId, sourceNode.Id, targetNode.Id);
            newConnection.SourcePort = sourcePort;
            newConnection.TargetPort = targetPort;

            // 添加连接到集合
            selectedTab.WorkflowConnections?.Add(newConnection);

            // 标记相关节点为脏，触发连接线更新
            if (_connectionPathCache != null)
            {
                _connectionPathCache.MarkNodeDirty(sourceNode.Id);
                _connectionPathCache.MarkNodeDirty(targetNode.Id);
            }


            // 连接创建后，选中目标节点，让图像预览器自动显示上游图像采集节点的图像
            // UpdateImagePreviewVisibility 会自动通过 BFS 追溯上游来决定是否显示图像预览器
            System.Diagnostics.Debug.WriteLine($"[CreateConnectionBetweenPorts] 连接创建完成: {sourceNode.Name} → {targetNode.Name}");
            System.Diagnostics.Debug.WriteLine($"[CreateConnectionBetweenPorts] 目标节点信息: IsImageCaptureNode={targetNode.IsImageCaptureNode}");
            System.Diagnostics.Debug.WriteLine($"[CreateConnectionBetweenPorts] _viewModel={(_viewModel == null ? "null" : "已设置")}");
            
            if (_viewModel != null)
            {
                System.Diagnostics.Debug.WriteLine($"[CreateConnectionBetweenPorts] 当前 SelectedNode={_viewModel.SelectedNode?.Name ?? "null"}");
                _viewModel.SelectedNode = targetNode;
                // ★ 强制刷新图像预览器，确保即使 SelectedNode 值相同也会重新计算
                _viewModel.ForceRefreshImagePreview();
                System.Diagnostics.Debug.WriteLine($"[CreateConnectionBetweenPorts] 设置 SelectedNode={targetNode.Name} 后, ShowImagePreview={_viewModel.ShowImagePreview}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[CreateConnectionBetweenPorts] 警告: _viewModel 为 null，无法设置 SelectedNode");
            }

            return true;
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
            // 不设置 e.Handled，让事件冒泡到 Port_MouseLeftButtonUp
            // 设置 e.Handled = true 会导致 Port_MouseLeftButtonUp 无法被触发，
            // 从而导致临时连接线无法隐藏

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
                    _viewModel.AddLog("[Connection] ? 无法连接到同一个节点");
                    _connectionSourceNode = null;
                    return;
                }

                // 检查连接是否已存在（节点点击模式使用硬编码的 RightPort）
                var existingConnection = selectedTab.WorkflowConnections.FirstOrDefault(c =>
                    c.SourceNodeId == _connectionSourceNode!.Id && 
                    c.TargetNodeId == targetNode.Id &&
                    c.SourcePort == "RightPort" && 
                    c.TargetPort == "LeftPort");

                if (existingConnection != null)
                {
                    _viewModel!.StatusText = "连接已存在";
                    _connectionSourceNode = null;
                    return;
                }

                // 创建新连接
                _connectionCreator?.CreateConnection(_connectionSourceNode, targetNode, "RightPort", CurrentWorkflowTab);

                // 退出连接模式
                _connectionSourceNode = null;
            }
        }

        /// <summary>
        /// 连接点鼠标按下 - 开始拖拽连接
        /// </summary>
        private void Port_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 移除此方法，统一使用 Port_MouseLeftButtonDown 和 Canvas 事件处理
            // 避免事件处理器冲突
        }

        /// <summary>
        /// 连接点鼠标释放 - 结束拖拽并创建连接
        /// </summary>
        private void Port_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // 移除此方法，统一使用 Port_MouseLeftButtonUp 和 Canvas 事件处理
            // 避免事件处理器冲突
        }

        #endregion

        #region 框选功能

        /// <summary>
        /// Canvas 鼠标左键按下 - 开始框选或清除选择
        /// </summary>
        private void WorkflowCanvas_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var mousePos = e.GetPosition(WorkflowCanvas);

            // 保护：如果处于拖拽连接状态，先重置状态
            if (_isDraggingConnection)
            {
                _isDraggingConnection = false;
                _dragConnectionSourceNode = null;
                _dragConnectionSourcePort = null;
                if (_tempConnectionLine != null)
                {
                    _tempConnectionLine.Visibility = Visibility.Collapsed;
                    _tempConnectionLine.UpdateLayout();
                }
            }

            // 检查点击的是否是节点或端口（通过原始源）
            var originalSource = e.OriginalSource as DependencyObject;

            // 手动查找带 WorkflowNode Tag 的 Border 或端口 Ellipse
            WorkflowNode? clickedNode = null;
            bool clickedPort = false;
            DependencyObject? current = originalSource;

            while (current != null)
            {
                if (current is Border border && border.Tag is WorkflowNode node)
                {
                    clickedNode = node;
                    break;
                }
                // 检查是否点击了端口 Ellipse
                if (current is Ellipse ellipse)
                {
                    var ellipseName = ellipse.Name ?? "";
                    if (ellipseName.Contains("Port"))
                    {
                        clickedPort = true;
                        break;
                    }
                }
                current = VisualTreeHelper.GetParent(current);
            }

            // 如果点击的是有 WorkflowNode Tag 的 Border 或端口 Ellipse，则由节点的事件处理，不触发框选
            if (clickedNode != null || clickedPort)
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
            // 不设置 e.Handled，让事件冒泡到 Port_MouseLeftButtonUp
            // 设置 e.Handled = true 会导致 Port_MouseLeftButtonUp 无法被触发，
            // 从而导致临时连接线无法隐藏
        }

        /// <summary>
        /// Canvas 鼠标移动 - 更新框选区域
        /// </summary>
        private void WorkflowCanvas_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            var mousePos = e.GetPosition(WorkflowCanvas);

            // 处理拖拽连接
            if (_isDraggingConnection)
            {
                _dragMoveCounter++;

                // 保护：如果状态不一致（_isDraggingConnection=true 但 _dragConnectionSourceNode=null），立即重置状态
                if (_dragConnectionSourceNode == null)
                {
                    _isDraggingConnection = false;
                    if (_tempConnectionLine != null)
                    {
                        _tempConnectionLine.Visibility = Visibility.Collapsed;
                        _tempConnectionLine.UpdateLayout();
                    }
                    return;
                }

                // 保护：确保临时连接线没有意外显示
                if (_tempConnectionLine != null && _tempConnectionLine.Visibility != Visibility.Visible)
                {
                    _tempConnectionLine.Visibility = Visibility.Collapsed;
                    return;
                }

                // 确保源节点不为空才更新临时连接线
                if (_tempConnectionGeometry != null && _dragConnectionSourceNode != null)
                {
                    var currentPoint = e.GetPosition(WorkflowCanvas);

                    // 获取源节点的连接点位置
                    var sourcePort = GetPortPosition(_dragConnectionSourceNode, _dragConnectionStartPoint);

                    // 计算智能直角折线路径
                    var pathPoints = CalculateSmartPath(sourcePort, currentPoint);

                    // 更新临时连接线
                    if (_tempConnectionGeometry != null)
                    {
                        _tempConnectionGeometry.Figures.Clear();
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

                        _tempConnectionGeometry.Figures.Add(pathFigure);
                    }

                    // 动态高亮目标端口
                    var hitNodes = new List<(WorkflowNode node, Border border, double distance)>();
                    var hitPorts = new List<(Ellipse port, string portName)>();
                    int hitTestCount = 0;

                    VisualTreeHelper.HitTest(WorkflowCanvas, null,
                        result =>
                        {
                            hitTestCount++;

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
                            // 动态计算节点中心（完全解耦）
                            var nodeCenter = hitNode.NodeCenter;
                            double distance = Math.Sqrt(Math.Pow(currentPoint.X - nodeCenter.X, 2) + Math.Pow(currentPoint.Y - nodeCenter.Y, 2));
                            hitNodes.Add((hitNode, hitBorder, distance));
                        }
                            DependencyObject? current = result.VisualHit as DependencyObject;
                            for (int depth = 0; current != null && depth < 10; depth++)
                            {
                                if (current is Border currentBorder && currentBorder.Tag is WorkflowNode currentBorderNode)
                                {
                                    // 动态计算节点中心（完全解耦）
                                    var nodeCenter = currentBorderNode.NodeCenter;
                                    double distance = Math.Sqrt(Math.Pow(currentPoint.X - nodeCenter.X, 2) + Math.Pow(currentPoint.Y - nodeCenter.Y, 2));
                                    hitNodes.Add((currentBorderNode, currentBorder, distance));
                                    break;
                                }
                                current = VisualTreeHelper.GetParent(current);
                            }
                            return HitTestResultBehavior.Continue;
                        },
                        new PointHitTestParameters(currentPoint));

                    if (_dragMoveCounter % 100 == 0)
                    {
                    }

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
                                            _portHighlighter?.HighlightSpecificPort(border, targetPortName);
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
                                _portHighlighter?.ClearTargetPortHighlight();
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
                            // 显示目标节点的所有端口（因为鼠标没有真正进入目标节点）
                            SetPortsVisibility(nearest.border, true);

                            // 高亮目标端口
                            _portHighlighter?.HighlightTargetPort(nearest.border, _dragConnectionSourceNode, _dragConnectionSourcePort ?? "RightPort");

                            // 保存高亮的节点Border，用于后续清理（即使鼠标离开也保持显示）
                            if (_highlightedTargetNodeBorder != nearest.border)
                            {
                                if (_highlightedTargetNodeBorder != null)
                                {
                                    SetPortsVisibility(_highlightedTargetNodeBorder, false);
                                }
                                _highlightedTargetNodeBorder = nearest.border;
                            }
                        }
                        else
                        {
                            _portHighlighter?.ClearTargetPortHighlight();
                        }
                    }
                    // 注意：这里不再隐藏端口，保持目标节点的端口可见直到拖拽结束
                    // 这样确保拖拽结束时也能命中目标节点的端口
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
                    // 获取节点边界（动态计算，完全解耦）
                    var nodeRect = node.NodeRect;

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
            var mousePos = e.GetPosition(WorkflowCanvas);

            // 立即隐藏临时连接线（在任何逻辑处理之前）
            // 这样可以确保无论后续逻辑如何，临时连接线都被隐藏
            if (_isDraggingConnection && _tempConnectionLine != null)
            {
                var oldVisibility = _tempConnectionLine.Visibility;
                _tempConnectionLine.Visibility = Visibility.Collapsed;

                // 清除几何数据，避免旧数据残留
                if (_tempConnectionGeometry != null)
                {
                    _tempConnectionGeometry.Figures.Clear();
                }

                // 强制刷新UI，确保临时连接线立即被隐藏
                _tempConnectionLine.UpdateLayout();
            }

            // 如果正在拖拽连接，尝试创建连接
            if (_isDraggingConnection)
            {
                var mousePosition = e.GetPosition(WorkflowCanvas);


            // 不设置 e.Handled，让事件冒泡到 Port_MouseLeftButtonUp
            // 设置 e.Handled = true 会导致 Port_MouseLeftButtonUp 无法被触发，
            // 从而导致临时连接线无法隐藏

            // 隐藏临时连接线
            if (_tempConnectionLine != null)
            {
                _tempConnectionLine.Visibility = Visibility.Collapsed;
            }
            else
            {
            }

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

                // 输出所有节点的位置信息（用于诊断）
                if (CurrentWorkflowTab?.WorkflowNodes != null)
                {
                    foreach (var node in CurrentWorkflowTab.WorkflowNodes)
                    {
                    }
                }

                // 检查节点是否被渲染到Canvas（用于诊断）
                var nodeBorders = WorkflowVisualHelper.FindAllVisualChildren<Border>(WorkflowCanvas);
                foreach (var border in nodeBorders)
                {
                    if (border.Tag is WorkflowNode node)
                    {
                        // 检查Border的父元素ContentPresenter的位置
                        var parent = VisualTreeHelper.GetParent(border);
                        if (parent is System.Windows.Controls.ContentPresenter cp)
                        {
                            double cpLeft = Canvas.GetLeft(cp);
                            double cpTop = Canvas.GetTop(cp);
                        }
                        else
                        {
                        }
                    }
                }

                // 使用 HitTest 查找鼠标位置下的所有元素
                VisualTreeHelper.HitTest(WorkflowCanvas, null,
                    result =>
                    {
                        hitTestCount++;

                        // 检查是否命中了端口
                        if (result.VisualHit is Ellipse hitEllipse)
                        {
                            var ellipseName = hitEllipse.Name;

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

                                // 查找端口所属的节点
                                DependencyObject? parent = hitEllipse;
                                while (parent != null)
                                {
                                    if (parent is Border border && border.Tag is WorkflowNode node)
                                    {
                                        break;
                                    }
                                    parent = VisualTreeHelper.GetParent(parent);
                                }
                            }
                        }

                        // 如果找到 Border 且带有 WorkflowNode Tag，计算距离并记录
                        if (result.VisualHit is Border hitBorder && hitBorder.Tag is WorkflowNode hitNode)
                        {
                            // 动态计算节点中心（完全解耦）
                            var nodeCenter = hitNode.NodeCenter;
                            double distance = Math.Sqrt(Math.Pow(mousePosition.X - nodeCenter.X, 2) + Math.Pow(mousePosition.Y - nodeCenter.Y, 2));
                            hitNodes.Add((hitNode, hitBorder, distance));
                        }

                        // 对于任何命中的元素，都向上查找带有WorkflowNode Tag的Border
                        DependencyObject? current = result.VisualHit as DependencyObject;
                        int depth = 0;
                        while (current != null && depth < 30)
                        {
                            depth++;
                            if (current is Border currentBorder && currentBorder.Tag is WorkflowNode currentBorderNode)
                            {
                                // 动态计算节点中心（完全解耦）
                                var nodeCenter = currentBorderNode.NodeCenter;
                                double distance = Math.Sqrt(Math.Pow(mousePosition.X - nodeCenter.X, 2) + Math.Pow(mousePosition.Y - nodeCenter.Y, 2));
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
                var nearestPort = hitPorts.OrderBy(p => p.distance).First();
            }

            if (hitPorts.Count > 0)
            {
            }
            
            if (hitNodes.Count > 0)
            {
                foreach (var (node, border, distance) in hitNodes)
                {
                }
            }
            else
            {
            }

                // 选择距离鼠标最近的节点
                WorkflowNode? targetNode = null;
                Border? targetBorder = null;

                // 优先选择命中的端口（排除源节点的端口）
                if (hitPorts.Count > 0)
                {
                    // 先过滤掉源节点的端口
                    var validPorts = hitPorts.Where(p =>
                    {
                        DependencyObject? parent = p.port;
                        while (parent != null)
                        {
                            if (parent is Border border && border.Tag is WorkflowNode node)
                            {
                                return node != _dragConnectionSourceNode;
                            }
                            parent = VisualTreeHelper.GetParent(parent);
                        }
                        return false;
                    }).ToList();

                    if (validPorts.Count > 0)
                    {
                        var nearestPort = validPorts.OrderBy(p => p.distance).First();
                        var targetPortEllipse = nearestPort.port;

                        // 找到端口所属的节点Border
                        DependencyObject? parent = targetPortEllipse;
                        while (parent != null)
                        {
                            if (parent is Border border && border.Tag is WorkflowNode node)
                            {
                                if (node != _dragConnectionSourceNode)
                                {
                                    targetNode = node;
                                    targetBorder = border;
                                    _directHitTargetPort = nearestPort.portName;
                                    break;
                                }
                            }
                            parent = VisualTreeHelper.GetParent(parent);
                        }

                        if (targetBorder != null)
                        {
                            _portHighlighter?.HighlightSpecificPort(targetBorder, nearestPort.portName);
                        }
                    }
                    else
                    {
                    }
                }

                // 如果没有命中有效的目标端口，则使用节点选择逻辑
                // 增加容错距离：即使鼠标不在节点上，如果距离足够近（150px以内），也认为命中该节点
                if (targetNode == null && hitNodes.Count > 0)
                {
                    // 先过滤掉源节点，避免把自己当成目标节点
                    var validNodes = hitNodes.Where(n => n.node != _dragConnectionSourceNode).ToList();
                    
                    if (validNodes.Count > 0)
                    {
                        var nearest = validNodes.OrderBy(n => n.distance).First();
                        const double MaxDistance = 150.0; // 最大容错距离150px

                        if (nearest.distance <= MaxDistance)
                        {
                            targetNode = nearest.node;
                            targetBorder = nearest.border;

                            // 高亮显示目标节点的端口（使用智能选择）
                            _portHighlighter?.HighlightTargetPort(targetBorder, _dragConnectionSourceNode, _dragConnectionSourcePort ?? "RightPort");
                        }
                        else
                        {
                        }
                    }
                    else
                    {
                    }
                }

                // 首先检查是否命中了任何端口
                // 如果命中了端口（无论是源端口自己还是其他节点端口），让 Port_MouseLeftButtonUp 处理
                // 这样避免Preview事件提前重置拖拽状态
                if (hitPorts.Count > 0)
                {
                    // 命中了端口，让 Port_MouseLeftButtonUp 处理连接创建或状态清理
                    // 包括：拖到其他端口创建连接，或拖回自己端口取消连接
                    return;
                }

                // 检查是否找到目标节点（拖拽到节点主体，而非端口）
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
                            if (!string.IsNullOrEmpty(targetPort))
                            {
                                _connectionCreator?.CreateConnectionWithSpecificPort(_dragConnectionSourceNode, targetNode, _dragConnectionSourcePort ?? "RightPort", targetPort, CurrentWorkflowTab);
                            }
                            else
                            {
                                _connectionCreator?.CreateConnection(_dragConnectionSourceNode, targetNode, _dragConnectionSourcePort ?? "RightPort", CurrentWorkflowTab);
                            }
                        }
                    }
                }

                // 没有命中有效端口，重置拖拽状态
                WorkflowCanvas.ReleaseMouseCapture();
                IsDraggingConnection = false;
                _dragConnectionSourceNode = null;
                _dragConnectionSourcePort = null;

                // 清理源节点的端口可见性
                if (_dragConnectionSourceBorder != null)
                {
                    SetPortsVisibility(_dragConnectionSourceBorder, false);
                    _dragConnectionSourceBorder = null;
                }

                _portHighlighter?.ClearTargetPortHighlight(); // 清除端口高亮
                SetPortsVisibility(false); // 隐藏所有端口

                return;
            }

            if (!_isBoxSelecting)
            {
                return;
            }

            _isBoxSelecting = false;


            // 结束框选
            SelectionBox?.EndSelection();
            WorkflowCanvas.ReleaseMouseCapture();

            // 记录选中节点的初始位置（用于批量移动）
            RecordSelectedNodesPositions();

            // 不设置 e.Handled，让事件冒泡到 Port_MouseLeftButtonUp
            // 设置 e.Handled = true 会导致 Port_MouseLeftButtonUp 无法被触发，
            // 从而导致临时连接线无法隐藏
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
            _dragDropHandler?.Canvas_DragEnter(sender, e);
        }

        private void WorkflowCanvas_DragOver(object sender, DragEventArgs e)
        {
            _dragDropHandler?.Canvas_DragOver(sender, e);
        }

        private void WorkflowCanvas_DragLeave(object sender, DragEventArgs e)
        {
            _dragDropHandler?.Canvas_DragLeave(sender, e);
        }

        private void WorkflowCanvas_Drop(object sender, DragEventArgs e)
        {
            _dragDropHandler?.Canvas_Drop(sender, e);
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
        /// 获取节点指定端口的Ellipse元素
        /// </summary>
        private Ellipse? GetPortElement(Border nodeBorder, string portName)
        {
            if (nodeBorder == null) return null;

            // 根据端口名称构造Ellipse名称（例如："LeftPort" -> "LeftPortEllipse"）
            string ellipseName = portName + "Ellipse";

            // 在节点Border的视觉树中查找指定名称的端口
            var visualChildren = WorkflowVisualHelper.FindAllVisualChildren<DependencyObject>(nodeBorder);

            // 查找包含端口名称的元素（通过Name属性或Tag）
            foreach (var child in visualChildren)
            {
                if (child is FrameworkElement element && element.Name == ellipseName)
                {
                    return element as Ellipse;
                }
            }

            return null;
        }

        /// <summary>
        /// Path元素加载事件 - 监控连接线路径创建
        /// </summary>
        private void Path_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is Path path && path.DataContext is WorkflowConnection connection)
            {

                if (path.Data is PathGeometry geom && geom.Figures.Count > 0)
                {
                }
                else
                {
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
                }
            }
        }

        /// <summary>
        /// 连接线鼠标左键点击事件 - 切换中间点的显示/隐藏
        /// </summary>
        private void ConnectionLine_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Path path && path.DataContext is WorkflowConnection connection)
            {
                // 切换 ShowPathPoints 属性
                connection.ShowPathPoints = !connection.ShowPathPoints;

                // 标记事件已处理，防止事件继续传播
                // 不设置 e.Handled，让事件冒泡到 Port_MouseLeftButtonUp
            // 设置 e.Handled = true 会导致 Port_MouseLeftButtonUp 无法被触发，
            // 从而导致临时连接线无法隐藏
            }
        }

        /// <summary>
        /// 箭头Path加载事件 - 设置箭头旋转角度
        /// </summary>
        private void ArrowPath_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is Path arrowPath && arrowPath.DataContext is WorkflowConnection connection)
            {
                // 设置箭头旋转角度
                var rotateTransform = new RotateTransform(connection.ArrowAngle);
                arrowPath.RenderTransform = rotateTransform;

                // 关键日志：记录箭头渲染

                // 监听ArrowAngle变化，动态更新旋转角度
                connection.PropertyChanged += (s, args) =>
                {
                    if (args.PropertyName == nameof(WorkflowConnection.ArrowAngle))
                    {
                        if (arrowPath.RenderTransform is RotateTransform rt)
                        {
                            rt.Angle = connection.ArrowAngle;
                        }
                    }
                };
            }
        }

        #endregion

        /// <summary>
        /// 更新最大外接矩形的显示
        /// </summary>
        private void UpdateBoundingRectangle()
        {
            if (!ShowBoundingRectangle)
            {
                BoundingRectangle.Visibility = Visibility.Collapsed;
                return;
            }

            // 查找源节点和目标节点
            WorkflowNode? sourceNode = null;
            WorkflowNode? targetNode = null;

            if (CurrentWorkflowTab?.WorkflowNodes != null)
            {
                if (!string.IsNullOrEmpty(BoundingSourceNodeId))
                {
                    sourceNode = CurrentWorkflowTab.WorkflowNodes.FirstOrDefault(n => n.Id == BoundingSourceNodeId);
                }

                if (!string.IsNullOrEmpty(BoundingTargetNodeId))
                {
                    targetNode = CurrentWorkflowTab.WorkflowNodes.FirstOrDefault(n => n.Id == BoundingTargetNodeId);
                }
            }

            // 如果找到了源节点和目标节点，计算并显示矩形
            if (sourceNode != null && targetNode != null)
            {
                double sourceLeft = sourceNode.Position.X;
                double sourceRight = sourceNode.Position.X + 140; // NodeWidth
                double sourceTop = sourceNode.Position.Y;
                double sourceBottom = sourceNode.Position.Y + 90; // NodeHeight

                double targetLeft = targetNode.Position.X;
                double targetRight = targetNode.Position.X + 140;
                double targetTop = targetNode.Position.Y;
                double targetBottom = targetNode.Position.Y + 90;


                // 计算包围两个节点的原始矩形
                double minX = Math.Min(sourceLeft, targetLeft);
                double maxX = Math.Max(sourceRight, targetRight);
                double minY = Math.Min(sourceTop, targetTop);
                double maxY = Math.Max(sourceBottom, targetBottom);

                // 计算矩形的宽度和高度
                double rectWidth = maxX - minX;
                double rectHeight = maxY - minY;

                // 使用最大边长作为正方形的边长,增加搜索范围
                double maxSide = Math.Max(rectWidth, rectHeight);

                // 以源节点和目标节点的中心点为基准,构建正方形搜索区域
                double centerX = (minX + maxX) / 2;
                double centerY = (minY + maxY) / 2;

                // 设置正方形的位置和大小
                double rectX = centerX - maxSide / 2;
                double rectY = centerY - maxSide / 2;

                Canvas.SetLeft(BoundingRectangle, rectX);
                Canvas.SetTop(BoundingRectangle, rectY);
                BoundingRectangle.Width = maxSide;
                BoundingRectangle.Height = maxSide;

                BoundingRectangle.Visibility = Visibility.Visible;

            }
            else
            {
                BoundingRectangle.Visibility = Visibility.Collapsed;
            }
        }
    }
}