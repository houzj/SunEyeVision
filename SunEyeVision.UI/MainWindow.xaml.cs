using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
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
        private double _currentScale = 1.0;
        private const double MinScale = 0.25;  // 25%
        private const double MaxScale = 3.0;   // 300%
        private ScaleTransform _scaleTransform = new ScaleTransform(1.0, 1.0);

        // 缓存 Canvas 的变换,因为 Canvas 可能还没有加载
        private Dictionary<WorkflowTabViewModel, ScaleTransform> _canvasTransforms = new Dictionary<WorkflowTabViewModel, ScaleTransform>();

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
                    if (container is TabItem tab)
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

                                        // 应用初始缩放变换
                                        canvas.LayoutTransform = _scaleTransform;
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
                return results;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T t)
                    results.Add(t);

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

        /// <summary>
        /// Canvas 加载完成事件 - 应用缩放变换
        /// </summary>
        private void WorkflowCanvas_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is Canvas canvas)
            {
                // 为当前 Tab 创建或获取缩放变换
                if (_viewModel.WorkflowTabViewModel.SelectedTab != null)
                {
                    if (!_canvasTransforms.ContainsKey(_viewModel.WorkflowTabViewModel.SelectedTab))
                    {
                        _canvasTransforms[_viewModel.WorkflowTabViewModel.SelectedTab] = _scaleTransform;
                    }

                    // 应用缩放变换
                    canvas.LayoutTransform = _canvasTransforms[_viewModel.WorkflowTabViewModel.SelectedTab];
                }
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

                    // 在UI线程中添加节点到当前选中的工作流
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (_viewModel.WorkflowTabViewModel.SelectedTab != null)
                        {
                            _viewModel.WorkflowTabViewModel.SelectedTab.WorkflowNodes.Add(node);
                            _viewModel.StatusText = $"添加节点: {tool.Name}";
                        }
                    });
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
        /// 放大画布
        /// </summary>
        private void ZoomIn_Click(object sender, RoutedEventArgs e)
        {
            if (_currentScale < MaxScale)
            {
                _currentScale = Math.Min(_currentScale * 1.2, MaxScale);
                // 延迟执行以确保 UI 已更新
                Dispatcher.BeginInvoke(new Action(() => ApplyZoom()),
                    System.Windows.Threading.DispatcherPriority.Render);
            }
        }

        /// <summary>
        /// 缩小画布
        /// </summary>
        private void ZoomOut_Click(object sender, RoutedEventArgs e)
        {
            if (_currentScale > MinScale)
            {
                _currentScale = Math.Max(_currentScale / 1.2, MinScale);
                // 延迟执行以确保 UI 已更新
                Dispatcher.BeginInvoke(new Action(() => ApplyZoom()),
                    System.Windows.Threading.DispatcherPriority.Render);
            }
        }

        /// <summary>
        /// 适应窗口
        /// </summary>
        private void ZoomFit_Click(object sender, RoutedEventArgs e)
        {
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
                    _currentScale = Math.Min(scaleX, scaleY);

                    // 限制在范围内
                    _currentScale = Math.Max(MinScale, Math.Min(MaxScale, _currentScale));

                    ApplyZoom();
                }
            }), System.Windows.Threading.DispatcherPriority.Render);
        }

        /// <summary>
        /// 重置缩放为100%
        /// </summary>
        private void ZoomReset_Click(object sender, RoutedEventArgs e)
        {
            _currentScale = 1.0;
            // 延迟执行以确保 UI 已更新
            Dispatcher.BeginInvoke(new Action(() => ApplyZoom()),
                System.Windows.Threading.DispatcherPriority.Render);
        }

        /// <summary>
        /// 应用缩放变换
        /// </summary>
        private void ApplyZoom()
        {
            // 更新缩放变换
            _scaleTransform.ScaleX = _currentScale;
            _scaleTransform.ScaleY = _currentScale;

            // 为当前 Tab 缓存缩放变换
            if (_viewModel.WorkflowTabViewModel.SelectedTab != null)
            {
                _canvasTransforms[_viewModel.WorkflowTabViewModel.SelectedTab] = _scaleTransform;
            }

            // 如果 Canvas 已经加载,直接应用
            var currentCanvas = GetCurrentCanvas();
            if (currentCanvas != null)
            {
                currentCanvas.LayoutTransform = _scaleTransform;
                currentCanvas.UpdateLayout();
            }

            // 更新缩放百分比显示
            UpdateZoomDisplay();

            // 更新指示器
            UpdateZoomIndicator();

            _viewModel.StatusText = $"画布缩放: {Math.Round(_currentScale * 100, 0)}%";
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
                                    int percentage = (int)(_currentScale * 100);
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
            int percentage = (int)(_currentScale * 100);

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
            // Ctrl+滚轮进行缩放
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                e.Handled = true;

                if (e.Delta > 0)
                {
                    // 向上滚动，放大
                    if (_currentScale < MaxScale)
                    {
                        _currentScale = Math.Min(_currentScale * 1.1, MaxScale);
                        ApplyZoom();
                    }
                }
                else
                {
                    // 向下滚动，缩小
                    if (_currentScale > MinScale)
                    {
                        _currentScale = Math.Max(_currentScale / 1.1, MinScale);
                        ApplyZoom();
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
                System.Diagnostics.Debug.WriteLine($"获取Canvas时出错: {ex.Message}");
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
                if (_viewModel.WorkflowTabViewModel.SelectedTab != null)
                {
                    var container = WorkflowTabControl.ItemContainerGenerator.ContainerFromItem(_viewModel.WorkflowTabViewModel.SelectedTab);
                    if (container is TabItem tabItem)
                    {
                        var contentPresenter = FindVisualChild<ContentPresenter>(tabItem);
                        if (contentPresenter != null)
                        {
                            // 查找 Grid (DataTemplate 的根元素)
                            var grid = FindVisualChild<Grid>(contentPresenter);
                            if (grid != null)
                            {
                                // 在 Grid 中查找名为 CanvasScrollViewer 的 ScrollViewer
                                var children = FindAllVisualChildren<ScrollViewer>(grid);
                                foreach (var sv in children)
                                {
                                    if (sv.Name == "CanvasScrollViewer")
                                    {
                                        return sv;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"获取ScrollViewer时出错: {ex.Message}");
            }
            return null!;
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

        #region 节点拖拽

        private bool _isDragging;
        private WorkflowNode? _draggedNode;
        private System.Windows.Point _startDragPosition;

        private void Node_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Border border || border.Tag is not WorkflowNode node)
                return;

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

            // 单击事件：拖拽准备
            _isDragging = true;
            _draggedNode = node;
            var canvas = FindParentCanvas(sender as DependencyObject);
            _startDragPosition = e.GetPosition(canvas);

            // 更新选中状态
            if (_viewModel.WorkflowTabViewModel.SelectedTab != null)
            {
                foreach (var n in _viewModel.WorkflowTabViewModel.SelectedTab.WorkflowNodes)
                {
                    n.IsSelected = (n == node);
                }
            }
            _viewModel.SelectedNode = node;

            border.CaptureMouse();
        }

        private void Node_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
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
                var currentPosition = e.GetPosition(canvas);
                var offset = currentPosition - _startDragPosition;

                _draggedNode.Position = new System.Windows.Point(
                    _draggedNode.Position.X + offset.X,
                    _draggedNode.Position.Y + offset.Y
                );

                _startDragPosition = currentPosition;
            }
        }

        private void Node_ClickForConnection(object sender, RoutedEventArgs e)
        {
            // 连接模式下点击节点作为目标
            if (sender is Border border && border.Tag is WorkflowNode targetNode)
            {
                if (_viewModel.WorkflowViewModel.IsInConnectionMode)
                {
                    var success = _viewModel.WorkflowViewModel.TryConnectNode(targetNode);

                    if (success)
                    {
                        var sourceNode = _viewModel.WorkflowViewModel.ConnectionSourceNode;
                        _viewModel.StatusText = $"成功连接: {sourceNode?.Name} -> {targetNode.Name}";

                        // 同步连接到当前选中的工作流
                        if (_viewModel.WorkflowTabViewModel.SelectedTab != null &&
                            _viewModel.WorkflowViewModel.Connections.LastOrDefault() is WorkflowConnection connection)
                        {
                            _viewModel.WorkflowTabViewModel.SelectedTab.WorkflowConnections.Add(connection);
                        }
                    }
                    else
                    {
                        _viewModel.StatusText = $"连接失败：目标节点无效或连接已存在";
                    }
                }
                else
                {
                    // 非连接模式下，只是选中节点
                    if (_viewModel.WorkflowTabViewModel.SelectedTab != null)
                    {
                        foreach (var n in _viewModel.WorkflowTabViewModel.SelectedTab.WorkflowNodes)
                        {
                            n.IsSelected = (n == targetNode);
                        }
                    }
                    _viewModel.SelectedNode = targetNode;
                    _viewModel.WorkflowViewModel.SelectedNode = targetNode;
                }
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
            System.Diagnostics.Debug.WriteLine($"[ToolboxSplitter_ToggleClick] START");
            System.Diagnostics.Debug.WriteLine($"  Before: IsToolboxCollapsed={_viewModel.IsToolboxCollapsed}, IsPropertyPanelCollapsed={_viewModel.IsPropertyPanelCollapsed}");

            if (_viewModel.IsToolboxCollapsed)
            {
                // 展开
                ToolboxColumn.Width = new GridLength(_originalToolboxWidth);
                ToolboxContent.Visibility = Visibility.Visible;
                _viewModel.IsToolboxCollapsed = false;
                System.Diagnostics.Debug.WriteLine($"Toolbox expanded, width={_originalToolboxWidth}");
            }
            else
            {
                // 折叠
                _originalToolboxWidth = ToolboxColumn.ActualWidth;
                ToolboxColumn.Width = new GridLength(40);
                ToolboxContent.Visibility = Visibility.Collapsed;
                _viewModel.IsToolboxCollapsed = true;
                System.Diagnostics.Debug.WriteLine($"Toolbox collapsed, saved width={_originalToolboxWidth}");
            }
            UpdateToolboxSplitterArrow();

            System.Diagnostics.Debug.WriteLine($"  After: IsToolboxCollapsed={_viewModel.IsToolboxCollapsed}, IsPropertyPanelCollapsed={_viewModel.IsPropertyPanelCollapsed}");
        }

        /// <summary>
        /// 更新工具箱分割器箭头方向
        /// </summary>
        private void UpdateToolboxSplitterArrow()
        {
            var newDirection = _viewModel.IsToolboxCollapsed
                ? ToggleDirectionType.Right
                : ToggleDirectionType.Left;
            System.Diagnostics.Debug.WriteLine($"  [UpdateToolboxSplitterArrow] Setting ToggleDirection to {newDirection}");
            ToolboxSplitter.ToggleDirection = newDirection;
        }

        /// <summary>
        /// 右侧面板分割器的折叠/展开事件
        /// </summary>
        private void RightPanelSplitter_ToggleClick(object? sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"[RightPanelSplitter_ToggleClick] START");
            System.Diagnostics.Debug.WriteLine($"  Before: IsPropertyPanelCollapsed={_viewModel.IsPropertyPanelCollapsed}");

            if (_viewModel.IsPropertyPanelCollapsed)
            {
                // 展开整个右侧面板
                RightPanelColumn.Width = new GridLength(_rightPanelWidth);
                _viewModel.IsPropertyPanelCollapsed = false;
                System.Diagnostics.Debug.WriteLine($"Right panel expanded, width={_rightPanelWidth}");
            }
            else
            {
                // 折叠整个右侧面板
                _rightPanelWidth = RightPanelColumn.ActualWidth;
                RightPanelColumn.Width = new GridLength(40);
                _viewModel.IsPropertyPanelCollapsed = true;
                System.Diagnostics.Debug.WriteLine($"Right panel collapsed, saved width={_rightPanelWidth}");
            }
            UpdateRightPanelSplitterArrow();

            System.Diagnostics.Debug.WriteLine($"  After: IsPropertyPanelCollapsed={_viewModel.IsPropertyPanelCollapsed}");
        }

        /// <summary>
        /// 更新右侧面板分割器箭头方向
        /// </summary>
        private void UpdateRightPanelSplitterArrow()
        {
            var newDirection = _viewModel.IsPropertyPanelCollapsed
                ? ToggleDirectionType.Left
                : ToggleDirectionType.Right;
            System.Diagnostics.Debug.WriteLine($"  [UpdateRightPanelSplitterArrow] Setting ToggleDirection to {newDirection}");
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
