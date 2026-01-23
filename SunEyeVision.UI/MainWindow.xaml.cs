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

        #region TabControl 多流程管理事件处理

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
                // 重置标志
                _isTabItemClick = false;
            }), System.Windows.Threading.DispatcherPriority.ContextIdle);
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

            // 自动滚动到新添加的TabItem，使其居中显示，并确保不被右侧面板遮挡
            Dispatcher.BeginInvoke(new Action(() =>
            {
                ScrollToSelectedTabItemAndAddButton();
            }), System.Windows.Threading.DispatcherPriority.ContextIdle);
        }

        /// <summary>
        /// 滚动到添加按钮，确保它可见
        /// </summary>
        private void ScrollToAddButton()
        {
            // 在TabControl的模板中查找ScrollViewer
            var scrollViewer = FindVisualChild<ScrollViewer>(WorkflowTabControl);
            if (scrollViewer == null)
                return;

            // 计算需要滚动的位置，使添加按钮可见
            var desiredOffset = scrollViewer.ScrollableWidth;
            scrollViewer.ScrollToHorizontalOffset(desiredOffset);
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
        /// 滚动到选中的TabItem，使其显示在可见范围的中间
        /// 同时确保右侧添加按钮区域也可见，防止被遮挡
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

                    // ========== 调试信息 ==========
                    System.Diagnostics.Debug.WriteLine("=== ScrollToSelectedTabItem 调试信息 ===");
                    System.Diagnostics.Debug.WriteLine($"TabItem名称: {_viewModel.WorkflowTabViewModel.SelectedTab?.Name}");
                    System.Diagnostics.Debug.WriteLine($"TabItem位置 (position.X): {position.X:F2}");
                    System.Diagnostics.Debug.WriteLine($"TabItem宽度: {selectedTabItem.ActualWidth:F2}");
                    System.Diagnostics.Debug.WriteLine($"ScrollViewer可视宽度 (ViewportWidth): {scrollViewer.ViewportWidth:F2}");
                    System.Diagnostics.Debug.WriteLine($"ScrollViewer可滚动宽度 (ScrollableWidth): {scrollViewer.ScrollableWidth:F2}");
                    System.Diagnostics.Debug.WriteLine($"当前滚动偏移量: {scrollViewer.HorizontalOffset:F2}");
                    // ============================

                    // 计算使TabItem居中的滚动位置
                    // TabItem中心位置 = position.X + selectedTabItem.ActualWidth / 2
                    // 视口中心位置 = scrollViewer.ViewportWidth / 2
                    // 目标滚动位置 = TabItem中心位置 - 视口中心位置
                    var targetOffset = position.X + (selectedTabItem.ActualWidth / 2) - (scrollViewer.ViewportWidth / 2);

                    System.Diagnostics.Debug.WriteLine($"计算的目标滚动位置: {targetOffset:F2}");

                    // 确保滚动位置在有效范围内
                    targetOffset = Math.Max(0, Math.Min(targetOffset, scrollViewer.ScrollableWidth));

                    System.Diagnostics.Debug.WriteLine($"调整后的目标滚动位置: {targetOffset:F2}");

                    // 滚动到目标位置，使TabItem居中显示
                    scrollViewer.ScrollToHorizontalOffset(targetOffset);

                    System.Diagnostics.Debug.WriteLine("=== 调试信息结束 ===\n");
                }
            }
        }

        /// <summary>
        /// 滚动到选中的TabItem并确保不被右侧面板遮挡（用于添加工作流时）
        /// </summary>
        private void ScrollToSelectedTabItemAndAddButton()
        {
            var selectedTabItem = FindTabItem(_viewModel.WorkflowTabViewModel.SelectedTab);
            if (selectedTabItem == null)
                return;

            var scrollViewer = FindVisualParent<ScrollViewer>(selectedTabItem);
            if (scrollViewer == null)
                return;

            // 获取TabPanel（内容容器）
            var tabPanel = FindVisualChild<TabPanel>(scrollViewer);
            if (tabPanel == null)
                return;

            // 计算TabItem相对于TabPanel的位置（内容区域的绝对位置）
            var transform = selectedTabItem.TransformToVisual(tabPanel);
            var position = transform.Transform(new Point(0, 0));

            // ========== 调试信息 ==========
            System.Diagnostics.Debug.WriteLine("=== ScrollToSelectedTabItemAndAddButton 调试信息 ===");
            System.Diagnostics.Debug.WriteLine($"TabItem名称: {_viewModel.WorkflowTabViewModel.SelectedTab?.Name}");
            System.Diagnostics.Debug.WriteLine($"TabItem位置 (position.X): {position.X:F2}");
            System.Diagnostics.Debug.WriteLine($"TabItem宽度: {selectedTabItem.ActualWidth:F2}");
            System.Diagnostics.Debug.WriteLine($"ScrollViewer可视宽度 (ViewportWidth): {scrollViewer.ViewportWidth:F2}");
            System.Diagnostics.Debug.WriteLine($"ScrollViewer可滚动宽度 (ScrollableWidth): {scrollViewer.ScrollableWidth:F2}");
            System.Diagnostics.Debug.WriteLine($"当前滚动偏移量: {scrollViewer.HorizontalOffset:F2}");
            // ============================

            // ScrollViewer有Margin="0,0,8,0"，所以实际可视宽度比ViewportWidth少8px
            const double scrollViewerRightMargin = 8;
            double availableWidth = scrollViewer.ViewportWidth - scrollViewerRightMargin;

            // 确保TabItem右边界至少距离可视区域右边缘50px（为添加按钮留出空间）
            const double minRightMargin = 50;

            // 计算使TabItem居中的滚动位置
            double targetOffset = position.X + (selectedTabItem.ActualWidth / 2) - (scrollViewer.ViewportWidth / 2);

            System.Diagnostics.Debug.WriteLine($"计算的居中滚动位置: {targetOffset:F2}");

            // 如果居中会导致TabItem右边界超出安全区域，则调整
            // 居中后TabItem右边界在视口中的位置 = position.X + selectedTabItem.ActualWidth - targetOffset
            double tabRightEdgeAfterCentering = position.X + selectedTabItem.ActualWidth - targetOffset;
            System.Diagnostics.Debug.WriteLine($"居中后TabItem右边界: {tabRightEdgeAfterCentering:F2}");
            System.Diagnostics.Debug.WriteLine($"安全区域最大边界: {availableWidth - minRightMargin:F2}");

            if (tabRightEdgeAfterCentering > availableWidth - minRightMargin)
            {
                // 调整到TabItem右边界刚好在安全区域的位置
                targetOffset = position.X + selectedTabItem.ActualWidth - availableWidth + minRightMargin;
                System.Diagnostics.Debug.WriteLine($"超出安全区域，调整为: {targetOffset:F2}");
            }

            // 确保滚动位置在有效范围内
            targetOffset = Math.Max(0, Math.Min(targetOffset, scrollViewer.ScrollableWidth));

            System.Diagnostics.Debug.WriteLine($"最终滚动位置: {targetOffset:F2}");

            // 滚动到目标位置
            scrollViewer.ScrollToHorizontalOffset(targetOffset);

            System.Diagnostics.Debug.WriteLine("=== 调试信息结束 ===\n");
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
