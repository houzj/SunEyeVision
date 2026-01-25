using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using SunEyeVision.UI.Controls;
using SunEyeVision.UI.Models;
using SunEyeVision.UI.ViewModels;

namespace SunEyeVision.UI
{
    /// <summary>
    /// MainWindow - 太阳眼视觉风格的主界面窗口
    /// 实现完整的机器视觉平台主界面，包含工作流画布、工具箱、属性面板等
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainWindowViewModel _viewModel;
        private bool _isTabItemClick = false;  // 标记是否是通过点击TabItem触发的切换

        // 画布配置常量
        private const double CanvasVirtualWidth = 5000;  // 虚拟画布宽度
        private const double CanvasVirtualHeight = 5000; // 虚拟画布高度

        // 缩放相关
        private const double MinScale = 0.25;  // 25%
        private const double MaxScale = 3.0;   // 300%

        // 节点拖拽相关
        private bool _isDragging;
        private WorkflowNode? _draggedNode;
        private System.Windows.Point _startDragPosition;
        private System.Windows.Point _initialNodePosition;

        // 框选相关
        private bool _isBoxSelecting;
        private System.Windows.Point _boxSelectStart;
        private System.Windows.Point[]? _selectedNodesInitialPositions;

        // 连接模式相关
        private WorkflowNode? _connectionSourceNode;

        /// <summary>
        /// 节点鼠标进入事件（用于调试悬停效果）
        /// </summary>
        private void Node_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Border border && border.Tag is WorkflowNode node)
            {
                // 延迟读取,确保样式已应用
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    var debugInfo = $"[节点悬停] 节点: {node.Name}, BorderBrush: {border.BorderBrush}, BorderThickness: {border.BorderThickness}";
                    System.Diagnostics.Debug.WriteLine(debugInfo);
                    AddLogToUI(debugInfo);
                }), System.Windows.Threading.DispatcherPriority.Render);
            }
        }

        /// <summary>
        /// 节点鼠标离开事件（用于调试悬停效果）
        /// </summary>
        private void Node_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Border border && border.Tag is WorkflowNode node)
            {
                // 延迟读取,确保样式已恢复
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    var debugInfo = $"[节点离开] 节点: {node.Name}, BorderBrush: {border.BorderBrush}, BorderThickness: {border.BorderThickness}";
                    System.Diagnostics.Debug.WriteLine(debugInfo);
                    AddLogToUI(debugInfo);
                }), System.Windows.Threading.DispatcherPriority.Render);
            }
        }

        /// <summary>
        /// 连接点鼠标进入事件（用于调试悬停效果）
        /// </summary>
        private void Ellipse_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Ellipse ellipse && ellipse.Tag is WorkflowNode node)
            {
                // 延迟读取,确保样式已应用
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    var debugInfo = $"[连接点悬停] 节点: {node.Name}, Fill: {ellipse.Fill}, Size: {ellipse.ActualWidth:F1}x{ellipse.ActualHeight:F1}";
                    System.Diagnostics.Debug.WriteLine(debugInfo);
                    AddLogToUI(debugInfo);
                    AddLogToUI($"  触发器状态: IsMouseOver={ellipse.IsMouseOver}");
                }), System.Windows.Threading.DispatcherPriority.Render);
            }
        }

        /// <summary>
        /// 连接点鼠标离开事件（用于调试悬停效果）
        /// </summary>
        private void Ellipse_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Ellipse ellipse && ellipse.Tag is WorkflowNode node)
            {
                // 延迟读取,确保样式已恢复
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    var debugInfo = $"[连接点离开] 节点: {node.Name}, Fill: {ellipse.Fill}, Size: {ellipse.ActualWidth:F1}x{ellipse.ActualHeight:F1}";
                    System.Diagnostics.Debug.WriteLine(debugInfo);
                    AddLogToUI(debugInfo);
                }), System.Windows.Threading.DispatcherPriority.Render);
            }
        }

        /// <summary>
        /// 将日志添加到UI界面
        /// </summary>
        private void AddLogToUI(string message)
        {
            if (_viewModel != null)
            {
                // 将日志添加到 LogText（如果存在）
                var currentLog = _viewModel.LogText ?? "";
                var newLog = $"{DateTime.Now:HH:mm:ss.fff} {message}\n";
                // 只保留最后100行
                var lines = (currentLog + newLog).Split('\n');
                if (lines.Length > 100)
                {
                    _viewModel.LogText = string.Join("\n", lines.Skip(lines.Length - 100));
                }
                else
                {
                    _viewModel.LogText = currentLog + newLog;
                }
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainWindowViewModel();
            DataContext = _viewModel;

            RegisterHotkeys();
        }

        /// <summary>
        /// 注册快捷键
        /// </summary>
        private void RegisterHotkeys()
        {
            // 文件操作快捷键
            InputBindings.Add(new KeyBinding(_viewModel.NewWorkflowCommand, Key.N, ModifierKeys.Control));
            InputBindings.Add(new KeyBinding(_viewModel.OpenWorkflowCommand, Key.O, ModifierKeys.Control));
            InputBindings.Add(new KeyBinding(_viewModel.SaveWorkflowCommand, Key.S, ModifierKeys.Control));

            // 运行控制快捷键
            InputBindings.Add(new KeyBinding(_viewModel.RunWorkflowCommand, Key.F5, ModifierKeys.None));
            InputBindings.Add(new KeyBinding(_viewModel.StopWorkflowCommand, Key.F5, ModifierKeys.Shift));

            // 帮助快捷键
            InputBindings.Add(new KeyBinding(_viewModel.ShowHelpCommand, Key.F1, ModifierKeys.None));
            InputBindings.Add(new KeyBinding(new PauseCommandWrapper(_viewModel.PauseCommand), Key.Pause, ModifierKeys.None));

            // 编辑快捷键
            InputBindings.Add(new KeyBinding(new UndoCommandWrapper(_viewModel.UndoCommand), Key.Z, ModifierKeys.Control));
            InputBindings.Add(new KeyBinding(new RedoCommandWrapper(_viewModel.RedoCommand), Key.Y, ModifierKeys.Control));
            InputBindings.Add(new KeyBinding(_viewModel.DeleteSelectedNodesCommand, Key.Delete, ModifierKeys.None));
        }

        #region 窗口事件

        protected override void OnClosed(EventArgs e)
        {
            // TODO: 清理资源
            _viewModel?.StopWorkflowCommand.Execute(null);
            base.OnClosed(e);
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // 自动加载所有工具插件
                PluginSystem.ToolInitializer.RegisterAllTools();

                var toolCount = PluginSystem.ToolRegistry.GetToolCount();
                _viewModel.StatusText = $"已加载 {toolCount} 个工具插件";

                // 初始化画布配置
                InitializeCanvas();

                // TODO: 加载工作流
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"加载工具插件时出错: {ex.Message}",
                    "加载失败",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
            }
        }

        #endregion

        #region 画布初始化

        /// <summary>
        /// 初始化画布配置
        /// </summary>
        private void InitializeCanvas()
        {
            // 在TabControl加载完成后初始化画布
            Dispatcher.BeginInvoke(new Action(() =>
            {
                // 遍历所有Tab的Canvas进行初始化
                foreach (var tabItem in WorkflowTabControl.Items)
                {
                    var container = WorkflowTabControl.ItemContainerGenerator.ContainerFromItem(tabItem);
                    if (container is TabItem tab && tab.DataContext is WorkflowTabViewModel workflow)
                    {
                        var contentPresenter = FindVisualChild<ContentPresenter>(tab);
                        if (contentPresenter != null)
                        {
                            var grid = FindVisualChild<Grid>(contentPresenter);
                            if (grid != null)
                            {
                                var scrollViewer = FindVisualChild<ScrollViewer>(grid);
                                if (scrollViewer != null)
                                {
                                    var canvases = FindAllVisualChildren<Canvas>(scrollViewer);

                                    // 初始化所有找到的 Canvas
                                    foreach (var canvas in canvases)
                                    {
                                        // 设置虚拟画布大小
                                        canvas.Width = CanvasVirtualWidth;
                                        canvas.Height = CanvasVirtualHeight;
                                    }
                                }
                            }
                        }
                    }
                }
            }), System.Windows.Threading.DispatcherPriority.ContextIdle);
        }

        /// <summary>
        /// 主内容区域加载完成
        /// </summary>
        private void MainContentGrid_Loaded(object sender, RoutedEventArgs e)
        {
            // 初始化缩放显示
            UpdateZoomDisplay();
        }

        #endregion

        #region TabControl 多流程管理事件处理

        /// <summary>
        /// TabControl 加载完成后,监测ScrollViewer的ScrollableWidth变化
        /// </summary>
        private void WorkflowTabControl_Loaded(object sender, RoutedEventArgs e)
        {
            // 找到ScrollViewer
            var scrollViewer = FindVisualChild<ScrollViewer>(WorkflowTabControl);
            if (scrollViewer != null)
            {
                // 监听ScrollViewer的SizeChanged事件
                scrollViewer.SizeChanged += ScrollViewer_SizeChanged;
                
                // 初始检查 - 需要传入TabControl的视觉树根元素来查找两个按钮
                UpdateAddButtonPosition(WorkflowTabControl);
            }
        }

        /// <summary>
        /// ScrollViewer大小变化事件 - 更新添加按钮位置
        /// </summary>
        private void ScrollViewer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (sender is ScrollViewer scrollViewer)
            {
                // 从ScrollViewer向上找到TabControl，然后查找两个按钮
                var tabControl = FindVisualParent<TabControl>(scrollViewer);
                if (tabControl != null)
                {
                    UpdateAddButtonPosition(tabControl);
                }
            }
        }

        /// <summary>
        /// 根据ScrollableWidth判断TabItems是否超出,动态调整添加按钮位置
        /// </summary>
        private void UpdateAddButtonPosition(TabControl tabControl)
        {
            // 找到ScrollViewer
            var scrollViewer = FindVisualChild<ScrollViewer>(tabControl);
            if (scrollViewer == null)
                return;

            // 找到两个按钮的Border容器 - 在TabControl的视觉树中查找
            var scrollableButton = FindChildByName<Border>(tabControl, "ScrollableAddButtonBorder");
            var fixedButton = FindChildByName<Border>(tabControl, "FixedAddButtonBorder");
            
            if (scrollableButton == null || fixedButton == null)
                return;
            
            // ScrollableWidth > 0 表示有滚动条,即TabItems超出了可视区域
            bool isOverflow = scrollViewer.ScrollableWidth > 0;
            
            if (isOverflow)
            {
                // 超出时:显示右侧固定按钮,隐藏滚动区域内的按钮
                scrollableButton.Visibility = Visibility.Collapsed;
                fixedButton.Visibility = Visibility.Visible;
            }
            else
            {
                // 未超出时:显示滚动区域内的按钮(跟随TabItems),隐藏右侧固定按钮
                scrollableButton.Visibility = Visibility.Visible;
                fixedButton.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// 在视觉树中通过Name查找指定类型的子元素
        /// </summary>
        private T? FindChildByName<T>(DependencyObject parent, string name) where T : DependencyObject
        {
            if (parent == null)
                return null;

            if (parent is T child && (child as FrameworkElement)?.Name == name)
                return child;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var found = FindChildByName<T>(VisualTreeHelper.GetChild(parent, i), name);
                if (found != null)
                    return found;
            }

            return null;
        }

        /// <summary>
        /// TabControl 选择变化事件 - 根据切换方式决定是否滚动
        /// </summary>
        private void WorkflowTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 使用Dispatcher延迟执行，确保UI已更新
            Dispatcher.BeginInvoke(new Action(() =>
            {
                // 只有通过下拉器切换时才滚动到中间，点击TabItem时不滚动
                if (!_isTabItemClick)
                {
                    ScrollToSelectedTabItem();  // 只滚动到选中的TabItem，使其居中显示
                }
                // 更新添加按钮位置，确保始终在最右边
                UpdateAddButtonPosition(WorkflowTabControl);
                // 重置标志
                _isTabItemClick = false;
            }), System.Windows.Threading.DispatcherPriority.ContextIdle);

            // 使用更高优先级延迟执行 ApplyZoom，确保 Tab 内容已生成
            Dispatcher.BeginInvoke(new Action(() =>
            {
                ApplyZoom();
            }), System.Windows.Threading.DispatcherPriority.Render);
        }

        /// <summary>
        /// TabControl 预览鼠标左键按下事件 - 检测是否点击了TabItem
        /// </summary>
        private void WorkflowTabControl_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 检查点击的是否是TabItem
            var source = e.OriginalSource as DependencyObject;
            if (source != null)
            {
                var tabItem = FindVisualParent<TabItem>(source);
                if (tabItem != null)
                {
                    // 标记为TabItem点击
                    _isTabItemClick = true;
                }
            }
        }

        /// <summary>
        /// 添加工作流点击事件
        /// </summary>
        private void AddWorkflow_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.WorkflowTabViewModel.AddWorkflow();
            _viewModel.StatusText = "已添加新工作流";

            // 自动滚动到新添加的TabItem，使其居中显示
            Dispatcher.BeginInvoke(new Action(() =>
            {
                ScrollToSelectedTabItem();
                // 等待 Canvas 加载完成后应用初始缩放
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    ApplyZoom();
                }), System.Windows.Threading.DispatcherPriority.Render);
            }), System.Windows.Threading.DispatcherPriority.ContextIdle);
        }

        /// <summary>
        /// 滚动到选中的TabItem，使其显示在可见范围的中间
        /// </summary>
        private void ScrollToSelectedTabItem()
        {
            var selectedTabItem = FindTabItem(_viewModel.WorkflowTabViewModel.SelectedTab);
            if (selectedTabItem != null)
            {
                // 找到ScrollViewer
                var scrollViewer = FindVisualParent<ScrollViewer>(selectedTabItem);
                if (scrollViewer != null)
                {
                    // 获取TabPanel（内容容器）
                    var tabPanel = FindVisualChild<TabPanel>(scrollViewer);
                    if (tabPanel == null)
                        return;

                    // 计算TabItem相对于TabPanel的位置（内容区域的绝对位置）
                    var transform = selectedTabItem.TransformToVisual(tabPanel);
                    var position = transform.Transform(new Point(0, 0));

                    // 计算使TabItem居中的滚动位置
                    // TabItem中心位置 = position.X + selectedTabItem.ActualWidth / 2
                    // 视口中心位置 = scrollViewer.ViewportWidth / 2
                    // 目标滚动位置 = TabItem中心位置 - 视口中心位置
                    var targetOffset = position.X + (selectedTabItem.ActualWidth / 2) - (scrollViewer.ViewportWidth / 2);

                    // 确保滚动位置在有效范围内
                    targetOffset = Math.Max(0, Math.Min(targetOffset, scrollViewer.ScrollableWidth));

                    // 滚动到目标位置，使TabItem居中显示
                    scrollViewer.ScrollToHorizontalOffset(targetOffset);
                }
            }
        }

        /// <summary>
        /// 在视觉树中查找指定类型的第一个子元素
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
        /// 在视觉树中查找指定数据对应的TabItem
        /// </summary>
        private TabItem? FindTabItem(WorkflowTabViewModel? workflow)
        {
            if (workflow == null)
                return null;

            // 在TabControl的Items中查找TabItem
            var tabControl = WorkflowTabControl;
            if (tabControl == null)
                return null;

            // 通过遍历TabControl的视觉树找到所有TabItem
            var tabItems = new List<TabItem>();
            FindVisualChildren<TabItem>(tabControl, tabItems);

            return tabItems.FirstOrDefault(item => item.DataContext == workflow);
        }

        /// <summary>
        /// 在视觉树中查找指定类型的所有子元素
        /// </summary>
        private void FindVisualChildren<T>(DependencyObject parent, List<T> results) where T : DependencyObject
        {
            if (parent == null)
                return;

            if (parent is T child)
                results.Add(child);

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                FindVisualChildren<T>(VisualTreeHelper.GetChild(parent, i), results);
            }
        }

        /// <summary>
        /// 在视觉树中查找指定类型的父元素
        /// </summary>
        private T? FindVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);

            if (parentObject == null)
                return null;

            if (parentObject is T parent)
                return parent;

            return FindVisualParent<T>(parentObject);
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
        /// TabItem 单次运行点击事件
        /// </summary>
        private void TabItem_SingleRun_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is WorkflowTabViewModel workflow)
            {
                _viewModel.WorkflowTabViewModel.RunSingle(workflow);
                _viewModel.StatusText = $"单次运行: {workflow.Name}";
            }
        }

        /// <summary>
        /// TabItem 连续运行/停止点击事件
        /// </summary>
        private void TabItem_ContinuousRun_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is WorkflowTabViewModel workflow)
            {
                _viewModel.WorkflowTabViewModel.ToggleContinuous(workflow);
                var action = workflow.IsRunning ? "开始连续运行" : "停止";
                _viewModel.StatusText = $"{action}: {workflow.Name}";
            }
        }

        /// <summary>
        /// TabItem 删除点击事件
        /// </summary>
        private void TabItem_Delete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is WorkflowTabViewModel workflow)
            {
                if (workflow.IsRunning)
                {
                    System.Windows.MessageBox.Show(
                        "请先停止该工作流",
                        "提示",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Warning);
                    return;
                }

                var result = System.Windows.MessageBox.Show(
                    $"确定要删除工作流 '{workflow.Name}' 吗?",
                    "确认删除",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Question);

                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    if (_viewModel.WorkflowTabViewModel.DeleteWorkflow(workflow))
                    {
                        _viewModel.StatusText = $"已删除工作流: {workflow.Name}";
                    }
                    else
                    {
                        System.Windows.MessageBox.Show(
                            "至少需要保留一个工作流",
                            "提示",
                            System.Windows.MessageBoxButton.OK,
                            System.Windows.MessageBoxImage.Warning);
                    }
                }
            }
        }

        #endregion

        #region WorkflowCanvasControl 事件处理

        /// <summary>
        /// 节点添加事件处理
        /// </summary>
        private void OnWorkflowCanvas_NodeAdded(object sender, WorkflowNode node)
        {
            _viewModel.StatusText = $"添加节点: {node.Name}";
        }

        /// <summary>
        /// 节点选中事件处理
        /// </summary>
        private void OnWorkflowCanvas_NodeSelected(object sender, WorkflowNode node)
        {
            _viewModel.SelectedNode = node;
            _viewModel.LoadNodeProperties(node);
        }

        /// <summary>
        /// 节点双击事件处理
        /// </summary>
        private void OnWorkflowCanvas_NodeDoubleClicked(object sender, WorkflowNode node)
        {
            _viewModel.OpenDebugWindowCommand.Execute(node);
        }

        #endregion

        #region 拖放事件

        private void ToolItem_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is Models.ToolItem tool)
            {
                var dragData = new DataObject("ToolItem", tool);
                DragDrop.DoDragDrop(border, dragData, DragDropEffects.Copy);
            }
        }

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
            // 可选:添加离开画布时的视觉效果
        }

        private void WorkflowCanvas_Drop(object sender, DragEventArgs e)
        {
            try
            {
                if (e.Data.GetData("ToolItem") is Models.ToolItem tool)
                {
                    var position = e.GetPosition(sender as Canvas);

                    // 创建新节点，使用ToolId作为AlgorithmType
                    var node = new WorkflowNode(
                        Guid.NewGuid().ToString(),
                        tool.Name,
                        tool.ToolId  // 使用ToolId而不是AlgorithmType
                    );

                    // 设置拖放位置(居中放置,节点大小140x90)
                    var x = Math.Max(0, position.X - 70);
                    var y = Math.Max(0, position.Y - 45);
                    node.Position = new System.Windows.Point(x, y);

                    // 使用命令模式添加节点
                    _viewModel.AddNodeToWorkflow(node);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"添加节点时出错: {ex.Message}", "错误",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        #endregion

        #region 缩放功能

        /// <summary>
        /// 诊断方法:打印视觉树层次结构
        /// </summary>
        private void PrintVisualTree(DependencyObject parent, int indent = 0)
        {
            string prefix = new string(' ', indent * 2);
            System.Diagnostics.Debug.WriteLine($"{prefix}{parent.GetType().Name}{(parent is FrameworkElement fe && !string.IsNullOrEmpty(fe.Name) ? $" (Name: {fe.Name})" : "")}");

            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                PrintVisualTree(child, indent + 1);
            }
        }

        /// <summary>
        /// 放大画布
        /// </summary>
        private void ZoomIn_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.WorkflowTabViewModel.SelectedTab == null)
                return;

            var workflow = _viewModel.WorkflowTabViewModel.SelectedTab;
            if (workflow.CurrentScale < MaxScale)
            {
                workflow.CurrentScale = Math.Min(workflow.CurrentScale * 1.2, MaxScale);

                // 使用Dispatcher延迟执行,确保TabItem已加载
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    ScrollViewer? scrollViewer = null;
                    Point canvasCenter = new Point(0, 0);

                    // 获取ScrollViewer
                    scrollViewer = GetCurrentScrollViewer();

                    if (scrollViewer != null)
                    {
                        canvasCenter = GetCanvasCenterPosition(scrollViewer);
                    }

                    ApplyZoom(canvasCenter, scrollViewer);
                }), System.Windows.Threading.DispatcherPriority.ContextIdle);
            }
        }

        /// <summary>
        /// 缩小画布
        /// </summary>
        private void ZoomOut_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.WorkflowTabViewModel.SelectedTab == null)
                return;

            var workflow = _viewModel.WorkflowTabViewModel.SelectedTab;
            if (workflow.CurrentScale > MinScale)
            {
                workflow.CurrentScale = Math.Max(workflow.CurrentScale / 1.2, MinScale);

                // 使用Dispatcher延迟执行,确保TabItem已加载
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    ScrollViewer? scrollViewer = null;
                    Point canvasCenter = new Point(0, 0);

                    // 获取ScrollViewer
                    scrollViewer = GetCurrentScrollViewer();

                    if (scrollViewer != null)
                    {
                        canvasCenter = GetCanvasCenterPosition(scrollViewer);
                    }

                    ApplyZoom(canvasCenter, scrollViewer);
                }), System.Windows.Threading.DispatcherPriority.ContextIdle);
            }
        }

        /// <summary>
        /// 适应窗口
        /// </summary>
        private void ZoomFit_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.WorkflowTabViewModel.SelectedTab == null)
                return;

            var workflow = _viewModel.WorkflowTabViewModel.SelectedTab;

            // 延迟执行以确保 UI 已更新
            Dispatcher.BeginInvoke(new Action(() =>
            {
                var currentCanvas = GetCurrentCanvas();
                var scrollViewer = GetCurrentScrollViewer();

                if (currentCanvas != null && scrollViewer != null)
                {
                    var viewportWidth = scrollViewer.ViewportWidth;
                    var viewportHeight = scrollViewer.ViewportHeight;

                    // 计算适合的缩放比例，留出10%边距
                    var scaleX = (viewportWidth * 0.9) / CanvasVirtualWidth;
                    var scaleY = (viewportHeight * 0.9) / CanvasVirtualHeight;
                    workflow.CurrentScale = Math.Min(scaleX, scaleY);

                    // 限制在范围内
                    workflow.CurrentScale = Math.Max(MinScale, Math.Min(MaxScale, workflow.CurrentScale));

                    ApplyZoom();
                }
            }), System.Windows.Threading.DispatcherPriority.Render);
        }

        /// <summary>
        /// 重置缩放为100%
        /// </summary>
        private void ZoomReset_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.WorkflowTabViewModel.SelectedTab != null)
            {
                _viewModel.WorkflowTabViewModel.SelectedTab.CurrentScale = 1.0;
                // 延迟执行以确保 UI 已更新
                Dispatcher.BeginInvoke(new Action(() => ApplyZoom()),
                    System.Windows.Threading.DispatcherPriority.Render);
            }
        }

        /// <summary>
        /// 应用缩放变换（支持围绕指定位置缩放）
        /// </summary>
        /// <param name="centerPosition">缩放中心相对于ScrollViewer的坐标（可选）</param>
        /// <param name="scrollViewer">可用的ScrollViewer实例（可选，如果提供则不需要重新查找）</param>
        private void ApplyZoom(Point? centerPosition = null, ScrollViewer? scrollViewer = null)
        {
            if (_viewModel.WorkflowTabViewModel.SelectedTab == null)
                return;

            var workflow = _viewModel.WorkflowTabViewModel.SelectedTab;
            var oldScale = workflow.ScaleTransform.ScaleX; // 保存旧缩放值
            var newScale = workflow.CurrentScale;

            // 如果没有提供ScrollViewer，尝试查找
            if (scrollViewer == null)
            {
                scrollViewer = GetCurrentScrollViewer();
            }

            // 如果提供了缩放中心且有ScrollViewer，计算并调整滚动偏移
            if (centerPosition.HasValue && scrollViewer != null)
            {
                // 计算缩放前后的比例变化
                var scaleRatio = newScale / oldScale;

                // 如果缩放值没有变化，直接返回
                if (Math.Abs(scaleRatio - 1.0) < 0.0001)
                {
                    return;
                }

                // 获取当前滚动偏移
                var oldHorizontalOffset = scrollViewer.HorizontalOffset;
                var oldVerticalOffset = scrollViewer.VerticalOffset;

                // 计算鼠标在画布坐标系中的位置（考虑当前缩放）
                var mouseInCanvasX = (oldHorizontalOffset + centerPosition.Value.X) / oldScale;
                var mouseInCanvasY = (oldVerticalOffset + centerPosition.Value.Y) / oldScale;

                // 应用新的缩放值（不使用CenterX/CenterY，因为我们在调整滚动偏移）
                workflow.ScaleTransform.CenterX = 0;
                workflow.ScaleTransform.CenterY = 0;
                workflow.ScaleTransform.ScaleX = newScale;
                workflow.ScaleTransform.ScaleY = newScale;

                // 计算新的滚动偏移，保持鼠标指向的内容位置不变
                // 新的滚动偏移 = 鼠标在画布坐标 * 新缩放比例 - 鼠标在ScrollViewer位置
                var newHorizontalOffset = mouseInCanvasX * newScale - centerPosition.Value.X;
                var newVerticalOffset = mouseInCanvasY * newScale - centerPosition.Value.Y;

                // 应用新的滚动偏移
                scrollViewer.ScrollToHorizontalOffset(newHorizontalOffset);
                scrollViewer.ScrollToVerticalOffset(newVerticalOffset);
            }
            else
            {
                // 没有缩放中心或没有ScrollViewer时，直接应用缩放（用于初始加载或重置）
                workflow.ScaleTransform.CenterX = 0;
                workflow.ScaleTransform.CenterY = 0;
                workflow.ScaleTransform.ScaleX = newScale;
                workflow.ScaleTransform.ScaleY = newScale;
            }

            // 更新显示
            UpdateZoomDisplay();
            UpdateZoomIndicator();

            _viewModel.StatusText = $"画布缩放: {Math.Round(workflow.CurrentScale * 100, 0)}%";
        }

        /// <summary>
        /// 更新缩放指示器
        /// </summary>
        private void UpdateZoomIndicator()
        {
            // 在当前Tab中查找缩放指示器
            if (_viewModel.WorkflowTabViewModel.SelectedTab != null)
            {
                var container = WorkflowTabControl.ItemContainerGenerator.ContainerFromItem(_viewModel.WorkflowTabViewModel.SelectedTab);
                if (container is TabItem tabItem)
                {
                    var contentPresenter = FindVisualChild<ContentPresenter>(tabItem);
                    if (contentPresenter != null)
                    {
                        var grid = FindVisualChild<Grid>(contentPresenter);
                        if (grid != null)
                        {
                            // 查找所有 TextBlock 元素
                            var textBlocks = FindAllVisualChildren<TextBlock>(grid);
                            foreach (var textBlock in textBlocks)
                            {
                                if (textBlock.Name == "ZoomIndicatorText")
                                {
                                    int percentage = (int)(_viewModel.WorkflowTabViewModel.SelectedTab.CurrentScale * 100);
                                    textBlock.Text = $"{percentage}%";
                                    return;
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 更新缩放百分比显示
        /// </summary>
        private void UpdateZoomDisplay()
        {
            if (_viewModel.WorkflowTabViewModel.SelectedTab == null)
                return;

            int percentage = (int)(_viewModel.WorkflowTabViewModel.SelectedTab.CurrentScale * 100);

            // 查找工具栏中的ZoomText
            var toolBar = FindVisualChild<ToolBar>(this);
            if (toolBar != null)
            {
                foreach (var child in toolBar.Items)
                {
                    if (child is TextBlock textBlock && textBlock.Name == "ZoomText")
                    {
                        textBlock.Text = $"缩放: {percentage}%";
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// 鼠标滚轮缩放事件
        /// </summary>
        private void CanvasScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (_viewModel.WorkflowTabViewModel.SelectedTab == null)
                return;

            var workflow = _viewModel.WorkflowTabViewModel.SelectedTab;

            // Ctrl+滚轮进行缩放
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                e.Handled = true;

                // sender 就是 ScrollViewer
                if (sender is not ScrollViewer scrollViewer)
                    return;

                // 获取鼠标位置
                var mousePositionInScrollViewer = e.GetPosition(scrollViewer);

                if (e.Delta > 0)
                {
                    // 向上滚动，放大
                    if (workflow.CurrentScale < MaxScale)
                    {
                        workflow.CurrentScale = Math.Min(workflow.CurrentScale * 1.1, MaxScale);
                        ApplyZoom(mousePositionInScrollViewer, scrollViewer); // 鼠标位置作为缩放中心
                    }
                }
                else
                {
                    // 向下滚动，缩小
                    if (workflow.CurrentScale > MinScale)
                    {
                        workflow.CurrentScale = Math.Max(workflow.CurrentScale / 1.1, MinScale);
                        ApplyZoom(mousePositionInScrollViewer, scrollViewer); // 鼠标位置作为缩放中心
                    }
                }
            }
        }

        /// <summary>
        /// 获取当前活动的Canvas
        /// </summary>
        private Canvas GetCurrentCanvas()
        {
            try
            {
                if (_viewModel.WorkflowTabViewModel.SelectedTab == null)
                    return null!;

                var container = WorkflowTabControl.ItemContainerGenerator.ContainerFromItem(_viewModel.WorkflowTabViewModel.SelectedTab);
                if (container is TabItem tabItem)
                {
                    // 在整个 TabItem 中查找所有 Canvas
                    var allCanvases = FindAllVisualChildren<Canvas>(tabItem);

                    foreach (var canvas in allCanvases)
                    {
                        // 找到名为 WorkflowCanvas 的 Canvas
                        if (canvas.Name == "WorkflowCanvas")
                        {
                            return canvas;
                        }
                    }

                    // 如果没有找到名为 WorkflowCanvas 的,返回第一个 Canvas
                    if (allCanvases.Count > 0)
                    {
                        return allCanvases[0];
                    }
                }
            }
            catch (Exception ex)
            {
                // 静默处理异常
            }
            return null!;
        }

        /// <summary>
        /// 获取当前活动的ScrollViewer
        /// </summary>
        private ScrollViewer GetCurrentScrollViewer()
        {
            try
            {
                if (_viewModel.WorkflowTabViewModel.SelectedTab == null)
                {
                    return null!;
                }

                // TabControl的内容通过ContentPresenter显示在模板中,而不是在TabItem的视觉树中
                // 所以直接从WorkflowTabControl的视觉树中查找ScrollViewer
                var allScrollViewers = FindAllVisualChildren<ScrollViewer>(WorkflowTabControl);

                // 查找名为 CanvasScrollViewer 的
                foreach (var sv in allScrollViewers)
                {
                    if (sv.Name == "CanvasScrollViewer")
                    {
                        return sv;
                    }
                }

                // 如果找不到指定名称的,返回第一个
                if (allScrollViewers.Count > 0)
                {
                    return allScrollViewers[0];
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"获取ScrollViewer时出错: {ex.Message}");
            }
            return null!;
        }

        /// <summary>
        /// 获取画布中心在Canvas上的坐标
        /// </summary>
        private Point GetCanvasCenterPosition(ScrollViewer scrollViewer)
        {
            if (scrollViewer == null)
                return new Point(0, 0);

            // 返回视口中心相对于ScrollViewer的坐标（即鼠标在视口中心的位置）
            return new Point(
                scrollViewer.ViewportWidth / 2,
                scrollViewer.ViewportHeight / 2
            );
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 查找父级Canvas
        /// </summary>
        private Canvas FindParentCanvas(DependencyObject element)
        {
            if (element == null)
                return null!;

            var parent = VisualTreeHelper.GetParent(element);
            while (parent != null)
            {
                if (parent is Canvas canvas)
                    return canvas;
                parent = VisualTreeHelper.GetParent(parent);
            }
            return null!;
        }

        #endregion

        #region 框选功能

        /// <summary>
        /// Canvas 鼠标左键按下 - 开始框选或清除选择
        /// </summary>
        private void WorkflowCanvas_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 获取当前Canvas
            var canvas = FindVisualChild<Canvas>(WorkflowTabControl);
            if (canvas == null) return;

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
                // 注意：这里不设置 e.Handled，让事件继续传播到节点
                return;
            }

            // 检查是否按住 Shift 或 Ctrl 键（多选模式）
            bool isMultiSelect = (Keyboard.Modifiers & ModifierKeys.Shift) != 0 ||
                               (Keyboard.Modifiers & ModifierKeys.Control) != 0;

            // 开始框选
            _isBoxSelecting = true;
            _boxSelectStart = e.GetPosition(canvas);

            // 如果不是多选模式，清除所有选择
            if (!isMultiSelect)
            {
                ClearAllSelections();
            }

            // 获取 SelectionBox 控件
            var selectionBox = FindChildByName<Controls.SelectionBox>(WorkflowTabControl, "SelectionBox");
            if (selectionBox != null)
            {
                // 开始显示框选框
                selectionBox.StartSelection(_boxSelectStart);
            }

            canvas.CaptureMouse();
            e.Handled = true;
        }

        /// <summary>
        /// Canvas 鼠标移动 - 更新框选区域
        /// </summary>
        private void WorkflowCanvas_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isBoxSelecting) return;

            var canvas = FindVisualChild<Canvas>(WorkflowTabControl);
            if (canvas == null) return;

            // 获取 SelectionBox 控件
            var selectionBox = FindChildByName<Controls.SelectionBox>(WorkflowTabControl, "SelectionBox");
            if (selectionBox == null) return;

            // 更新框选框
            var currentPoint = e.GetPosition(canvas);
            selectionBox.UpdateSelection(currentPoint);

            // 获取框选区域
            var selectionRect = selectionBox.GetSelectionRect();

            // 更新选中的节点
            if (_viewModel.WorkflowTabViewModel.SelectedTab != null)
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
                selectionBox.SetItemCount(selectedCount);
            }
        }

        /// <summary>
        /// Canvas 鼠标左键释放 - 结束框选
        /// </summary>
        private void WorkflowCanvas_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_isBoxSelecting) return;

            _isBoxSelecting = false;

            // 获取 SelectionBox 控件
            var selectionBox = FindChildByName<Controls.SelectionBox>(WorkflowTabControl, "SelectionBox");
            if (selectionBox != null)
            {
                // 结束框选
                selectionBox.EndSelection();
            }

            var canvas = FindVisualChild<Canvas>(WorkflowTabControl);
            canvas?.ReleaseMouseCapture();

            // 记录选中节点的初始位置（用于批量移动）
            RecordSelectedNodesPositions();

            e.Handled = true;
        }

        /// <summary>
        /// 清除所有节点的选中状态
        /// </summary>
        private void ClearAllSelections()
        {
            if (_viewModel.WorkflowTabViewModel.SelectedTab != null)
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
            if (_viewModel.WorkflowTabViewModel.SelectedTab == null) return;

            var selectedNodes = _viewModel.WorkflowTabViewModel.SelectedTab.WorkflowNodes
                .Where(n => n.IsSelected)
                .ToList();

            _selectedNodesInitialPositions = selectedNodes
                .Select(n => n.Position)
                .ToArray();
        }

        #endregion

        #region 节点拖拽

        private void Node_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Border border || border.Tag is not WorkflowNode node)
            {
                AddLogToUI("[节点点击] ERROR: Sender is not Border or Tag is not WorkflowNode");
                return;
            }

            var debugInfo = $"[节点点击] {node.Name} ({node.Id}), ClickCount: {e.ClickCount}";
            System.Diagnostics.Debug.WriteLine(debugInfo);
            AddLogToUI(debugInfo);

            // 双击事件：打开调试窗口
            if (e.ClickCount == 2)
            {
                if (_viewModel.WorkflowTabViewModel.SelectedTab != null)
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

            AddLogToUI($"  多选模式: {isMultiSelect}, 已选中: {node.IsSelected}");

            // 如果节点未被选中，且不是多选模式，则只选中当前节点
            if (!node.IsSelected && !isMultiSelect)
            {
                ClearAllSelections();
                node.IsSelected = true;
                AddLogToUI($"  清除其他选择, 选中: {node.Name}");
            }
            // 如果是多选模式，切换选中状态
            else if (isMultiSelect)
            {
                node.IsSelected = !node.IsSelected;
                AddLogToUI($"  切换选择状态: {node.IsSelected}");
            }

            _viewModel.SelectedNode = node;

            // 记录所有选中节点的初始位置
            RecordSelectedNodesPositions();

            // 单击事件：拖拽准备
            _isDragging = true;
            _draggedNode = node;
            _initialNodePosition = node.Position;
            var canvas = FindParentCanvas(sender as DependencyObject);
            _startDragPosition = e.GetPosition(canvas);

            AddLogToUI($"  开始拖拽, 初始位置: ({_initialNodePosition.X:F1}, {_initialNodePosition.Y:F1})");

            border.CaptureMouse();

            // 阻止事件冒泡到 Canvas，避免触发框选
            e.Handled = true;
        }

        private void Node_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging && _draggedNode != null)
            {
                // 拖拽结束，执行批量移动命令
                if (_viewModel.WorkflowTabViewModel.SelectedTab != null)
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
                            var command = new Commands.BatchMoveNodesCommand(
                                _viewModel.WorkflowTabViewModel.SelectedTab.WorkflowNodes,
                                offsets
                            );

                            // 获取选中的节点列表（用于命令内部使用）
                            var nodesToMove = new System.Collections.ObjectModel.ObservableCollection<WorkflowNode>(selectedNodes);
                            var batchCommand = new Commands.BatchMoveNodesCommand(nodesToMove, offsets);
                            _viewModel.WorkflowTabViewModel.SelectedTab.CommandManager.Execute(batchCommand);
                        }
                    }
                }

                _isDragging = false;
                _draggedNode = null!;
                (sender as Border)?.ReleaseMouseCapture();
            }
        }

        private void Node_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging && _draggedNode != null && e.LeftButton == MouseButtonState.Pressed)
            {
                var canvas = FindParentCanvas(sender as DependencyObject);
                if (canvas == null)
                {
                    return;
                }

                var currentPosition = e.GetPosition(canvas);

                // 批量移动所有选中的节点
                if (_viewModel.WorkflowTabViewModel.SelectedTab != null &&
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

        private void Node_ClickForConnection(object sender, RoutedEventArgs e)
        {
            // 获取节点对象（支持 Border 或 Ellipse 作为 sender）
            WorkflowNode? targetNode = null;

            if (sender is Border border && border.Tag is WorkflowNode clickedNodeFromBorder)
            {
                targetNode = clickedNodeFromBorder;
                AddLogToUI($"[连接点点击] 类型: Border, 节点: {clickedNodeFromBorder.Name}");
            }
            else if (sender is Ellipse ellipse && ellipse.Tag is WorkflowNode clickedNodeFromEllipse)
            {
                targetNode = clickedNodeFromEllipse;
                AddLogToUI($"[连接点点击] 类型: Ellipse, 节点: {clickedNodeFromEllipse.Name}");
                AddLogToUI($"  IsMouseOver: {ellipse.IsMouseOver}, 大小: {ellipse.ActualWidth:F1}x{ellipse.ActualHeight:F1}");

                // 选中当前节点（连接点点击时也需要选中节点）
                if (_viewModel.WorkflowTabViewModel.SelectedTab != null)
                {
                    // 清除其他节点的选中状态
                    foreach (var n in _viewModel.WorkflowTabViewModel.SelectedTab.WorkflowNodes)
                    {
                        n.IsSelected = (n == targetNode);
                    }
                    _viewModel.SelectedNode = targetNode;
                    AddLogToUI($"  通过连接点选中节点: {targetNode.Name}");
                }
            }
            else
            {
                AddLogToUI($"[连接点点击] ERROR: 无效的sender类型或Tag为null");
                return;
            }

            if (targetNode == null)
            {
                AddLogToUI($"[连接点点击] ERROR: targetNode为null");
                return;
            }

            AddLogToUI($"  节点位置: ({targetNode.Position.X:F1}, {targetNode.Position.Y:F1})");

            // 阻止事件冒泡到节点的点击事件
            e.Handled = true;

            // 检查当前工作流是否为空，如果为空则初始化
            if (_viewModel.WorkflowTabViewModel.SelectedTab != null &&
                _viewModel.WorkflowTabViewModel.SelectedTab.WorkflowNodes.Count == 0)
            {
                AddLogToUI($"  SelectedTab为空, 从MainWindowViewModel初始化");
                // 从 MainWindowViewModel 复制节点和连接到 SelectedTab
                foreach (var node in _viewModel.WorkflowNodes)
                {
                    _viewModel.WorkflowTabViewModel.SelectedTab.WorkflowNodes.Add(node);
                }
                foreach (var conn in _viewModel.WorkflowConnections)
                {
                    _viewModel.WorkflowTabViewModel.SelectedTab.WorkflowConnections.Add(conn);
                }
                AddLogToUI($"  复制了 {_viewModel.WorkflowNodes.Count} 个节点和 {_viewModel.WorkflowConnections.Count} 条连接");
            }

            // 使用 SelectedTab 的连接模式状态（而不是 WorkflowViewModel）
            var selectedTab = _viewModel.WorkflowTabViewModel.SelectedTab;
            if (selectedTab == null)
            {
                AddLogToUI($"[连接点点击] ERROR: SelectedTab为null");
                return;
            }

            // 检查是否在连接模式（使用一个静态变量跟踪）
            if (_connectionSourceNode == null)
            {
                // 进入连接模式
                _connectionSourceNode = targetNode;
                _viewModel.StatusText = $"请选择目标节点进行连接，从: {targetNode.Name}";
                AddLogToUI($"  进入连接模式, 源节点: {targetNode.Name}");
            }
            else
            {
                // 尝试创建连接
                AddLogToUI($"  源节点: {_connectionSourceNode?.Name ?? "null"}, 目标节点: {targetNode.Name}");

                // 检查是否是同一个节点
                if (_connectionSourceNode == targetNode)
                {
                    _viewModel.StatusText = "无法连接到同一个节点";
                    _connectionSourceNode = null;
                    AddLogToUI($"  ERROR: 不能连接到同一个节点");
                    return;
                }

                // 检查连接是否已存在
                var existingConnection = selectedTab.WorkflowConnections.FirstOrDefault(c =>
                    c.SourceNodeId == _connectionSourceNode!.Id && c.TargetNodeId == targetNode.Id);

                if (existingConnection != null)
                {
                    _viewModel.StatusText = "连接已存在";
                    _connectionSourceNode = null;
                    AddLogToUI($"  ERROR: 连接已存在");
                    return;
                }

                // 创建新连接
                var connectionId = $"conn_{Guid.NewGuid().ToString("N")[..8]}";
                var newConnection = new WorkflowConnection(connectionId, _connectionSourceNode.Id, targetNode.Id);

                // 计算连接点位置（节点右中心到左中心）
                var sourcePos = new Point(
                    _connectionSourceNode.Position.X + 140,
                    _connectionSourceNode.Position.Y + 45
                );
                var targetPos = new Point(
                    targetNode.Position.X,
                    targetNode.Position.Y + 45
                );

                newConnection.SourcePosition = sourcePos;
                newConnection.TargetPosition = targetPos;

                AddLogToUI($"  创建连接: {connectionId}");
                AddLogToUI($"    源位置: ({sourcePos.X:F1}, {sourcePos.Y:F1})");
                AddLogToUI($"    目标位置: ({targetPos.X:F1}, {targetPos.Y:F1})");

                // 直接添加到 SelectedTab.WorkflowConnections
                selectedTab.WorkflowConnections.Add(newConnection);

                _viewModel.StatusText = $"成功连接: {_connectionSourceNode.Name} -> {targetNode.Name}";
                AddLogToUI($"  ✓ 成功连接: {_connectionSourceNode.Name} -> {targetNode.Name}");
                AddLogToUI($"  当前连接数: {selectedTab.WorkflowConnections.Count}");

                // 退出连接模式
                _connectionSourceNode = null;
            }
        }

        #endregion

        #region SplitterWithToggle 事件处理

        private double _originalToolboxWidth = 260;
        private double _rightPanelWidth = 500;

        /// <summary>
        /// 工具箱分割器的折叠/展开事件
        /// </summary>
        private void ToolboxSplitter_ToggleClick(object? sender, EventArgs e)
        {
            if (_viewModel.IsToolboxCollapsed)
            {
                // 展开
                ToolboxColumn.Width = new GridLength(_originalToolboxWidth);
                ToolboxContent.Visibility = Visibility.Visible;
                _viewModel.IsToolboxCollapsed = false;
            }
            else
            {
                // 折叠
                _originalToolboxWidth = ToolboxColumn.ActualWidth;
                ToolboxColumn.Width = new GridLength(40);
                ToolboxContent.Visibility = Visibility.Collapsed;
                _viewModel.IsToolboxCollapsed = true;
            }
            UpdateToolboxSplitterArrow();
        }

        /// <summary>
        /// 更新工具箱分割器箭头方向
        /// </summary>
        private void UpdateToolboxSplitterArrow()
        {
            var newDirection = _viewModel.IsToolboxCollapsed
                ? ToggleDirectionType.Right
                : ToggleDirectionType.Left;
            ToolboxSplitter.ToggleDirection = newDirection;
        }

        /// <summary>
        /// 右侧面板分割器的折叠/展开事件
        /// </summary>
        private void RightPanelSplitter_ToggleClick(object? sender, EventArgs e)
        {
            if (_viewModel.IsPropertyPanelCollapsed)
            {
                // 展开整个右侧面板
                RightPanelColumn.Width = new GridLength(_rightPanelWidth);
                _viewModel.IsPropertyPanelCollapsed = false;
            }
            else
            {
                // 折叠整个右侧面板
                _rightPanelWidth = RightPanelColumn.ActualWidth;
                RightPanelColumn.Width = new GridLength(40);
                _viewModel.IsPropertyPanelCollapsed = true;
            }
            UpdateRightPanelSplitterArrow();
        }

        /// <summary>
        /// 更新右侧面板分割器箭头方向
        /// </summary>
        private void UpdateRightPanelSplitterArrow()
        {
            var newDirection = _viewModel.IsPropertyPanelCollapsed
                ? ToggleDirectionType.Left
                : ToggleDirectionType.Right;
            RightPanelSplitter.ToggleDirection = newDirection;
        }

        #endregion

        #region 命令包装器类

        private class PauseCommandWrapper : ICommand
        {
            private readonly ICommand? _command;

            public PauseCommandWrapper(ICommand? command)
            {
                _command = command;
            }

            public event EventHandler? CanExecuteChanged;

            public bool CanExecute(object? parameter)
            {
                return _command?.CanExecute(parameter) ?? false;
            }

            public void Execute(object? parameter)
            {
                _command?.Execute(parameter);
            }
        }

        private class UndoCommandWrapper : ICommand
        {
            private readonly ICommand? _command;

            public UndoCommandWrapper(ICommand? command)
            {
                _command = command;
            }

            public event EventHandler? CanExecuteChanged;

            public bool CanExecute(object? parameter)
            {
                return _command?.CanExecute(parameter) ?? false;
            }

            public void Execute(object? parameter)
            {
                _command?.Execute(parameter);
            }
        }

        private class RedoCommandWrapper : ICommand
        {
            private readonly ICommand? _command;

            public RedoCommandWrapper(ICommand? command)
            {
                _command = command;
            }

            public event EventHandler? CanExecuteChanged;

            public bool CanExecute(object? parameter)
            {
                return _command?.CanExecute(parameter) ?? false;
            }

            public void Execute(object? parameter)
            {
                _command?.Execute(parameter);
            }
        }

        #endregion
    }
}
