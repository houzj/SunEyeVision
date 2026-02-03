using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using SunEyeVision.UI.Controls;
using SunEyeVision.UI.Models;
using SunEyeVision.UI.Services;
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
        private Controls.WorkflowCanvasControl? _currentWorkflowCanvas = null;  // 当前显示的WorkflowCanvasControl
        private Controls.AIStudioDiagramControl? _currentAIStudioDiagram = null;  // 当前显示的AIStudioDiagramControl

        // 画布引擎管理器容器
        public System.Windows.Controls.Decorator CanvasContainer { get; private set; } = new System.Windows.Controls.Decorator();

        // 缩放相关
        private const double MinScale = 0.25;  // 25%
        private const double MaxScale = 3.0;   // 300%

        // 画布虚拟大小
        private const double CanvasVirtualWidth = 5000;
        private const double CanvasVirtualHeight = 5000;


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

            // 测试日志系统
            System.Diagnostics.Debug.WriteLine("[系统] 主窗口已启动");
            System.Diagnostics.Debug.WriteLine("[系统] 日志系统测试 - 如果能看到这条消息，说明日志系统正常工作！");

            // 初始化画布引擎管理器 - 设置默认引擎
            CanvasEngineManager.SetDataContext(_viewModel.WorkflowTabViewModel?.SelectedTab);

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

                // 初始化智能路径转换器的节点集合（使用当前选中的 Tab 的节点集合）
                if (_viewModel.WorkflowTabViewModel?.SelectedTab != null)
                {
                    Converters.SmartPathConverter.Nodes = _viewModel.WorkflowTabViewModel.SelectedTab.WorkflowNodes;
                    Converters.SmartPathConverter.Connections = _viewModel.WorkflowTabViewModel.SelectedTab.WorkflowConnections;
                    System.Diagnostics.Debug.WriteLine($"[MainWindow] SmartPathConverter 初始化 - Nodes count: {_viewModel.WorkflowTabViewModel.SelectedTab.WorkflowNodes?.Count ?? 0}");
                }

                // TODO: 加载工作流

                // 延时测试矩形显示（等待所有布局完成）
                System.Windows.Threading.DispatcherTimer timer = new System.Windows.Threading.DispatcherTimer();
                timer.Interval = TimeSpan.FromSeconds(2);
                timer.Tick += (s, args) =>
                {
                    timer.Stop();
                    TestBoundingRectangle();
                };
                timer.Start();
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

        /// <summary>
        /// 测试BoundingRectangle显示
        /// </summary>
        private void TestBoundingRectangle()
        {
            System.Diagnostics.Debug.WriteLine("[TestBoundingRectangle] ========== 开始测试矩形显示 ==========");

            try
            {
                // 获取TabControl
                var tabControl = this.FindName("WorkflowTabControl") as TabControl;
                if (tabControl == null)
                {
                    System.Diagnostics.Debug.WriteLine("[TestBoundingRectangle] ❌ 无法找到TabControl");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"[TestBoundingRectangle] ✓ 找到TabControl，Tab数量: {tabControl.Items.Count}");

                // 获取选中的TabItem
                if (tabControl.SelectedIndex < 0)
                {
                    System.Diagnostics.Debug.WriteLine("[TestBoundingRectangle] ❌ 没有选中的Tab");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"[TestBoundingRectangle] ✓ 选中的Tab索引: {tabControl.SelectedIndex}");

                // 从TabItem的Content中查找WorkflowCanvasControl
                var tabItem = tabControl.ItemContainerGenerator.ContainerFromIndex(tabControl.SelectedIndex) as TabItem;
                if (tabItem == null)
                {
                    System.Diagnostics.Debug.WriteLine("[TestBoundingRectangle] ❌ 无法获取TabItem");
                    return;
                }

                System.Diagnostics.Debug.WriteLine("[TestBoundingRectangle] ✓ 获取到TabItem");
                System.Diagnostics.Debug.WriteLine($"[TestBoundingRectangle] TabItem.Content类型: {tabItem.Content?.GetType().Name ?? "null"}");

                // 检查ScrollViewer
                var scrollViewers = FindVisualChildren<ScrollViewer>(tabItem).ToList();
                System.Diagnostics.Debug.WriteLine($"[TestBoundingRectangle] ScrollViewer count: {scrollViewers.Count}");
                foreach (var sv in scrollViewers)
                {
                    System.Diagnostics.Debug.WriteLine($"[TestBoundingRectangle] ScrollViewer Visibility: {sv.Visibility}, Background: {sv.Background}");
                }

                // 声明 workflowCanvas 变量，以便在整个方法中使用
                WorkflowCanvasControl? workflowCanvas = null;

                // 直接使用已保存的控件引用
                if (_currentWorkflowCanvas != null)
                {
                    System.Diagnostics.Debug.WriteLine("[TestBoundingRectangle] ✓ 使用已保存的WorkflowCanvasControl引用");
                    System.Diagnostics.Debug.WriteLine($"[TestBoundingRectangle] WorkflowCanvasControl.Visibility: {_currentWorkflowCanvas.Visibility}");

                    // 查找WorkflowCanvasControl的父级ScrollViewer
                    var workflowScrollViewer = FindVisualParent<ScrollViewer>(_currentWorkflowCanvas);
                    System.Diagnostics.Debug.WriteLine($"[TestBoundingRectangle] WorkflowCanvas的ScrollViewer: {(workflowScrollViewer != null ? workflowScrollViewer.Visibility.ToString() : "null")}");

                    // 强制显示测试矩形
                    if (_currentWorkflowCanvas.BoundingRectangle != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[TestBoundingRectangle] ✓ BoundingRectangle存在，当前可见性: {_currentWorkflowCanvas.BoundingRectangle.Visibility}");

                        _currentWorkflowCanvas.BoundingRectangle.Visibility = Visibility.Visible;
                        Canvas.SetLeft(_currentWorkflowCanvas.BoundingRectangle, 200);
                        Canvas.SetTop(_currentWorkflowCanvas.BoundingRectangle, 200);
                        _currentWorkflowCanvas.BoundingRectangle.Width = 300;
                        _currentWorkflowCanvas.BoundingRectangle.Height = 150;

                        System.Diagnostics.Debug.WriteLine("[TestBoundingRectangle] ✓ 矩形已设置为可见");
                        System.Diagnostics.Debug.WriteLine("[TestBoundingRectangle] ✓ 位置: (200, 200)");
                        System.Diagnostics.Debug.WriteLine("[TestBoundingRectangle] ✓ 大小: 300 x 150");
                        System.Diagnostics.Debug.WriteLine("[TestBoundingRectangle] ✓ 颜色: 红色");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("[TestBoundingRectangle] ❌ BoundingRectangle为null");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[TestBoundingRectangle] ❌ _currentWorkflowCanvas为null");

                    // 尝试传统方式查找
                    workflowCanvas = FindVisualChild<WorkflowCanvasControl>(tabItem);
                    if (workflowCanvas == null)
                    {
                        System.Diagnostics.Debug.WriteLine("[TestBoundingRectangle] ❌ TabItem中找不到WorkflowCanvasControl");

                        // 尝试从tabItem.Content查找（如果Content是UI元素）
                        if (tabItem.Content is DependencyObject contentObj)
                        {
                            workflowCanvas = FindVisualChild<WorkflowCanvasControl>(contentObj);
                            System.Diagnostics.Debug.WriteLine($"[TestBoundingRectangle] 从Content对象查找: {(workflowCanvas != null ? "成功" : "失败")}");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("[TestBoundingRectangle] ✓ 从TabItem找到WorkflowCanvasControl");
                    }
                }

                if (workflowCanvas != null)
                {
                    System.Diagnostics.Debug.WriteLine("[TestBoundingRectangle] ====== 开始设置矩形 ==========");

                    // 强制显示测试矩形
                    if (workflowCanvas.BoundingRectangle != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[TestBoundingRectangle] ✓ BoundingRectangle存在，当前可见性: {workflowCanvas.BoundingRectangle.Visibility}");

                        workflowCanvas.BoundingRectangle.Visibility = Visibility.Visible;
                        Canvas.SetLeft(workflowCanvas.BoundingRectangle, 200);
                        Canvas.SetTop(workflowCanvas.BoundingRectangle, 200);
                        workflowCanvas.BoundingRectangle.Width = 300;
                        workflowCanvas.BoundingRectangle.Height = 150;

                        System.Diagnostics.Debug.WriteLine("[TestBoundingRectangle] ✓ 矩形已设置为可见");
                        System.Diagnostics.Debug.WriteLine("[TestBoundingRectangle] ✓ 位置: (200, 200)");
                        System.Diagnostics.Debug.WriteLine("[TestBoundingRectangle] ✓ 大小: 300 x 150");
                        System.Diagnostics.Debug.WriteLine("[TestBoundingRectangle] ✓ 颜色: 红色");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("[TestBoundingRectangle] ❌ BoundingRectangle为null");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[TestBoundingRectangle] ❌ WorkflowCanvasControl为null");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TestBoundingRectangle] ❌ 异常: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[TestBoundingRectangle] 堆栈: {ex.StackTrace}");
            }

            System.Diagnostics.Debug.WriteLine("[TestBoundingRectangle] ========== 测试完成 ==========");
        }

        /// <summary>
        /// 检查画布ScrollViewer的Visibility状态
        /// </summary>
        private void CheckCanvasVisibility()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[CheckCanvasVisibility] ========== 开始检查画布Visibility ==========");

                // 获取当前选中的TabItem
                var selectedItem = WorkflowTabControl.SelectedItem;
                System.Diagnostics.Debug.WriteLine($"[CheckCanvasVisibility] SelectedTab type: {selectedItem?.GetType().Name ?? "null"}");

                if (selectedItem is ViewModels.WorkflowTabViewModel tabViewModel)
                {
                    System.Diagnostics.Debug.WriteLine($"[CheckCanvasVisibility] TabViewModel.Name: {tabViewModel.Name}");
                    System.Diagnostics.Debug.WriteLine($"[CheckCanvasVisibility] TabViewModel.CanvasType: {tabViewModel.CanvasType}");

                    // 查找当前TabItem的ContentPresenter
                    var tabItem = FindVisualChild<System.Windows.Controls.TabItem>(WorkflowTabControl);
                    if (tabItem != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[CheckCanvasVisibility] 找到TabItem: {tabItem.Name}");

                        // 查找ScrollViewer
                        var scrollViewer = FindVisualChild<ScrollViewer>(tabItem);
                        System.Diagnostics.Debug.WriteLine($"[CheckCanvasVisibility] ScrollViewer count: {FindVisualChildren<ScrollViewer>(tabItem).Count()}");

                        var scrollViewers = FindVisualChildren<ScrollViewer>(tabItem).ToList();
                        foreach (var sv in scrollViewers)
                        {
                            System.Diagnostics.Debug.WriteLine($"[CheckCanvasVisibility] ScrollViewer Visibility: {sv.Visibility}");
                            System.Diagnostics.Debug.WriteLine($"[CheckCanvasVisibility] ScrollViewer Background: {sv.Background}");
                        }

                        // 查找两个ScrollViewer
                        var workflowScrollViewer = scrollViewers.FirstOrDefault(sv => sv.Background.ToString().Contains("F5F8FC") || sv.Background.ToString().Contains("255, 245, 252"));
                        var aiStudioScrollViewer = scrollViewers.FirstOrDefault(sv => sv.Background.ToString().Contains("1A1A2E") || sv.Background.ToString().Contains("26, 26, 46"));

                        System.Diagnostics.Debug.WriteLine($"[CheckCanvasVisibility] Workflow ScrollViewer: {(workflowScrollViewer != null ? $"Visibility={workflowScrollViewer.Visibility}" : "null")}");
                        System.Diagnostics.Debug.WriteLine($"[CheckCanvasVisibility] AIStudio ScrollViewer: {(aiStudioScrollViewer != null ? $"Visibility={aiStudioScrollViewer.Visibility}" : "null")}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("[CheckCanvasVisibility] TabItem is null");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[CheckCanvasVisibility] SelectedTab is not WorkflowTabViewModel");
                }

                System.Diagnostics.Debug.WriteLine("[CheckCanvasVisibility] ========== 检查完成 ==========");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CheckCanvasVisibility] 错误: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[CheckCanvasVisibility] 堆栈: {ex.StackTrace}");
            }
        }

        #endregion

        #region 初始化

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
        /// WorkflowCanvasControl加载事件 - 保存引用
        /// </summary>
        private void WorkflowCanvasControl_Loaded(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"[WorkflowCanvasControl_Loaded] ===== WorkflowCanvasControl Loaded Event Fired =====");
            System.Diagnostics.Debug.WriteLine($"[WorkflowCanvasControl_Loaded] sender type: {sender?.GetType().Name}");
            System.Diagnostics.Debug.WriteLine($"[WorkflowCanvasControl_Loaded] sender is WorkflowCanvasControl: {sender is Controls.WorkflowCanvasControl}");

            if (sender is Controls.WorkflowCanvasControl workflowCanvas)
            {
                _currentWorkflowCanvas = workflowCanvas;
                System.Diagnostics.Debug.WriteLine($"[MainWindow] WorkflowCanvasControl已加载并保存引用");
                System.Diagnostics.Debug.WriteLine($"[MainWindow] BoundingRectangle元素: {(workflowCanvas.BoundingRectangle != null ? "存在" : "null")}");

                // 检查DataContext
                var dataContext = workflowCanvas.DataContext;
                System.Diagnostics.Debug.WriteLine($"[MainWindow] WorkflowCanvasControl.DataContext type: {dataContext?.GetType().Name ?? "null"}");
                System.Diagnostics.Debug.WriteLine($"[MainWindow] WorkflowCanvasControl.DataContext is WorkflowTabViewModel: {dataContext is ViewModels.WorkflowTabViewModel}");

                // 如果DataContext为null，手动设置为当前选中的Tab
                if (dataContext == null && _viewModel?.WorkflowTabViewModel?.SelectedTab != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[MainWindow] DataContext为null，手动设置为SelectedTab");
                    workflowCanvas.DataContext = _viewModel.WorkflowTabViewModel.SelectedTab;
                    dataContext = workflowCanvas.DataContext;
                    System.Diagnostics.Debug.WriteLine($"[MainWindow] 手动设置后DataContext type: {dataContext?.GetType().Name ?? "null"}");
                }

                // 订阅DataContextChanged事件，以便在CanvasType变化时更新Visibility
                workflowCanvas.DataContextChanged += (s, args) =>
                {
                    System.Diagnostics.Debug.WriteLine($"[WorkflowCanvasControl DataContextChanged] CanvasType更新Visibility");
                    UpdateCanvasVisibility();
                };

                if (dataContext is ViewModels.WorkflowTabViewModel tabViewModel)
                {
                    System.Diagnostics.Debug.WriteLine($"[MainWindow] TabViewModel.Name: {tabViewModel.Name}");
                    System.Diagnostics.Debug.WriteLine($"[MainWindow] TabViewModel.CanvasType: {tabViewModel.CanvasType}");
                    System.Diagnostics.Debug.WriteLine($"[MainWindow] TabViewModel.WorkflowNodes.Count: {tabViewModel.WorkflowNodes.Count}");
                }

                // 立即根据CanvasType更新Visibility
                UpdateCanvasVisibility();
            }
        }

        /// <summary>
        /// AIStudioDiagramControl加载事件 - 保存引用
        /// </summary>
        private void AIStudioDiagramControl_Loaded(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl_Loaded] ===== AIStudioDiagramControl Loaded Event Fired =====");
            System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl_Loaded] sender type: {sender?.GetType().Name}");
            System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl_Loaded] sender is AIStudioDiagramControl: {sender is Controls.AIStudioDiagramControl}");

            if (sender is Controls.AIStudioDiagramControl aiStudioDiagram)
            {
                _currentAIStudioDiagram = aiStudioDiagram;
                System.Diagnostics.Debug.WriteLine($"[MainWindow] AIStudioDiagramControl已加载并保存引用");

                // 检查DataContext
                var dataContext = aiStudioDiagram.DataContext;
                System.Diagnostics.Debug.WriteLine($"[MainWindow] AIStudioDiagramControl.DataContext type: {dataContext?.GetType().Name ?? "null"}");
                System.Diagnostics.Debug.WriteLine($"[MainWindow] AIStudioDiagramControl.DataContext is WorkflowTabViewModel: {dataContext is ViewModels.WorkflowTabViewModel}");

                // 如果DataContext为null，手动设置为当前选中的Tab
                if (dataContext == null && _viewModel?.WorkflowTabViewModel?.SelectedTab != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[MainWindow] DataContext为null，手动设置为SelectedTab");
                    aiStudioDiagram.DataContext = _viewModel.WorkflowTabViewModel.SelectedTab;
                    dataContext = aiStudioDiagram.DataContext;
                    System.Diagnostics.Debug.WriteLine($"[MainWindow] 手动设置后DataContext type: {dataContext?.GetType().Name ?? "null"}");
                }

                // 订阅DataContextChanged事件，以便在CanvasType变化时更新Visibility
                aiStudioDiagram.DataContextChanged += (s, args) =>
                {
                    System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl DataContextChanged] CanvasType更新Visibility");
                    UpdateCanvasVisibility();
                };

                if (dataContext is ViewModels.WorkflowTabViewModel tabViewModel)
                {
                    System.Diagnostics.Debug.WriteLine($"[MainWindow] TabViewModel.CanvasType: {tabViewModel.CanvasType}");
                }

                // 初始化DiagramControl
                aiStudioDiagram.Initialize();

                // 订阅事件
                aiStudioDiagram.NodeAdded += (s, args) =>
                {
                    System.Diagnostics.Debug.WriteLine($"[MainWindow] AIStudioDiagram: 节点已添加");
                };

                aiStudioDiagram.ConnectionAdded += (s, args) =>
                {
                    System.Diagnostics.Debug.WriteLine($"[MainWindow] AIStudioDiagram: 连接已添加");
                };

                // 立即根据CanvasType更新Visibility
                UpdateCanvasVisibility();
            }
        }

        /// <summary>
        /// 根据CanvasType更新画布的Visibility
        /// </summary>
        private void UpdateCanvasVisibility()
        {
            try
            {
                if (_viewModel?.WorkflowTabViewModel?.SelectedTab == null)
                {
                    System.Diagnostics.Debug.WriteLine("[UpdateCanvasVisibility] SelectedTab为null，无法更新");
                    return;
                }

                var currentTab = _viewModel.WorkflowTabViewModel.SelectedTab;
                var canvasType = currentTab.CanvasType;

                System.Diagnostics.Debug.WriteLine($"[UpdateCanvasVisibility] CanvasType: {canvasType}");

                // 查找两个画布的ScrollViewer
                var tabItem = WorkflowTabControl.ItemContainerGenerator.ContainerFromIndex(WorkflowTabControl.SelectedIndex) as TabItem;
                if (tabItem != null)
                {
                    // 查找WorkflowCanvasControl的父级ScrollViewer
                    if (_currentWorkflowCanvas != null)
                    {
                        var workflowScrollViewer = FindVisualParent<ScrollViewer>(_currentWorkflowCanvas);
                        if (workflowScrollViewer != null)
                        {
                            var shouldShow = canvasType == CanvasType.WorkflowCanvas;
                            workflowScrollViewer.Visibility = shouldShow ? Visibility.Visible : Visibility.Collapsed;
                            System.Diagnostics.Debug.WriteLine($"[UpdateCanvasVisibility] WorkflowCanvas ScrollViewer Visibility设置为: {workflowScrollViewer.Visibility}");
                        }
                    }

                    // 查找AIStudioDiagramControl的父级ScrollViewer
                    if (_currentAIStudioDiagram != null)
                    {
                        var aiStudioScrollViewer = FindVisualParent<ScrollViewer>(_currentAIStudioDiagram);
                        if (aiStudioScrollViewer != null)
                        {
                            var shouldShow = canvasType == CanvasType.AIStudioDiagram;
                            aiStudioScrollViewer.Visibility = shouldShow ? Visibility.Visible : Visibility.Collapsed;
                            System.Diagnostics.Debug.WriteLine($"[UpdateCanvasVisibility] AIStudioDiagram ScrollViewer Visibility设置为: {aiStudioScrollViewer.Visibility}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UpdateCanvasVisibility] 错误: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[UpdateCanvasVisibility] 堆栈: {ex.StackTrace}");
            }
        }

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
        private void WorkflowTabControl_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
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
                var workflow = _viewModel.WorkflowTabViewModel.SelectedTab;
                if (workflow != null)
                {
                    var currentScale = workflow.CurrentScale;
                    ApplyZoom(currentScale, currentScale);
                }
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
                    var workflow = _viewModel.WorkflowTabViewModel.SelectedTab;
                    if (workflow != null)
                    {
                        var currentScale = workflow.CurrentScale;
                        ApplyZoom(currentScale, currentScale);
                    }
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
        /// 获取当前显示的WorkflowCanvasControl
        /// </summary>
        public Controls.WorkflowCanvasControl? GetCurrentWorkflowCanvas()
        {
            return _currentWorkflowCanvas;
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
        /// 在视觉树中查找指定类型的所有子元素（返回IEnumerable的便捷方法）
        /// </summary>
        private IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
        {
            var results = new List<T>();
            FindVisualChildren(parent, results);
            return results;
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
            // 注意：此方法未使用，如果需要输出日志，应使用 _viewModel?.AddLog
            // System.Diagnostics.Debug.WriteLine($"{prefix}{parent.GetType().Name}...");
            string prefix = new string(' ', indent * 2);

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
            var oldScale = workflow.CurrentScale;

            if (oldScale < MaxScale)
            {
                var newScale = Math.Min(oldScale * 1.2, MaxScale);

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

                    ApplyZoom(oldScale, newScale, canvasCenter, scrollViewer);
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
            var oldScale = workflow.CurrentScale;

            if (oldScale > MinScale)
            {
                var newScale = Math.Max(oldScale / 1.2, MinScale);

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

                    ApplyZoom(oldScale, newScale, canvasCenter, scrollViewer);
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
            var oldScale = workflow.CurrentScale;

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
                    var newScale = Math.Min(scaleX, scaleY);

                    // 限制在范围内
                    newScale = Math.Max(MinScale, Math.Min(MaxScale, newScale));

                    ApplyZoom(oldScale, newScale);
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
                var workflow = _viewModel.WorkflowTabViewModel.SelectedTab;
                var oldScale = workflow.CurrentScale;
                var newScale = 1.0;
                
                // 延迟执行以确保 UI 已更新
                Dispatcher.BeginInvoke(new Action(() => ApplyZoom(oldScale, newScale)),
                    System.Windows.Threading.DispatcherPriority.Render);
            }
        }

        /// <summary>
        /// 应用缩放变换（支持围绕指定位置缩放）
        /// </summary>
        /// <param name="oldScale">缩放前的缩放值</param>
        /// <param name="newScale">缩放后的缩放值</param>
        /// <param name="centerPosition">缩放中心相对于ScrollViewer的坐标（可选）</param>
        /// <param name="scrollViewer">可用的ScrollViewer实例（可选，如果提供则不需要重新查找）</param>
        private void ApplyZoom(double oldScale, double newScale, Point? centerPosition = null, ScrollViewer? scrollViewer = null)
        {
            if (_viewModel.WorkflowTabViewModel.SelectedTab == null)
                return;

            var workflow = _viewModel.WorkflowTabViewModel.SelectedTab;
            
            // 更新CurrentScale
            workflow.CurrentScale = newScale;

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

                // 计算缩放中心在画布坐标系中的位置（考虑当前缩放）
                var centerInCanvasX = (oldHorizontalOffset + centerPosition.Value.X) / oldScale;
                var centerInCanvasY = (oldVerticalOffset + centerPosition.Value.Y) / oldScale;

                // 应用新的缩放值（不使用CenterX/CenterY，因为我们在调整滚动偏移）
                workflow.ScaleTransform.CenterX = 0;
                workflow.ScaleTransform.CenterY = 0;
                workflow.ScaleTransform.ScaleX = newScale;
                workflow.ScaleTransform.ScaleY = newScale;

                // 计算新的滚动偏移，保持缩放中心指向的内容位置不变
                // 新的滚动偏移 = 缩放中心在画布坐标 * 新缩放比例 - 缩放中心在ScrollViewer位置
                var newHorizontalOffset = centerInCanvasX * newScale - centerPosition.Value.X;
                var newVerticalOffset = centerInCanvasY * newScale - centerPosition.Value.Y;

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

            // 直接使用滚轮进行缩放（不要求Ctrl键）
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
                    var oldScale = workflow.CurrentScale;
                    var newScale = Math.Min(oldScale * 1.1, MaxScale);
                    ApplyZoom(oldScale, newScale, mousePositionInScrollViewer, scrollViewer); // 鼠标位置作为缩放中心
                }
            }
            else
            {
                // 向下滚动，缩小
                if (workflow.CurrentScale > MinScale)
                {
                    var oldScale = workflow.CurrentScale;
                    var newScale = Math.Max(oldScale / 1.1, MinScale);
                    ApplyZoom(oldScale, newScale, mousePositionInScrollViewer, scrollViewer); // 鼠标位置作为缩放中心
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
                // 注意：此处无法访问 _viewModel，因为此方法是辅助方法
                // 如需记录错误日志，可以传入 ViewModel 参数或使用其他日志机制
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

        #region 画布引擎管理器支持

        /// <summary>
        /// 通过代码切换画布类型（用于单元测试和特殊场景）
        /// </summary>
        public void SwitchCanvasType(CanvasType canvasType)
        {
            if (_viewModel?.WorkflowTabViewModel?.SelectedTab == null)
            {
                throw new InvalidOperationException("请先选择一个工作流标签页");
            }

            var currentTab = _viewModel.WorkflowTabViewModel.SelectedTab;
            currentTab.CanvasType = canvasType;
            currentTab.RefreshProperty("CanvasType");
        }

        /// <summary>
        /// 设置路径计算器类型（用于单元测试）
        /// </summary>
        public void SetPathCalculator(string pathCalculatorType)
        {
            System.Diagnostics.Debug.WriteLine($"[MainWindow] 设置路径计算器到: {pathCalculatorType}");

            // 获取当前画布类型
            var canvasType = GetCurrentCanvasType();

            // 根据画布类型调用对应的控件方法
            switch (canvasType)
            {
                case CanvasType.WorkflowCanvas:
                    // 查找当前显示的WorkflowCanvasControl
                    var workflowCanvas = FindVisualChild<WorkflowCanvasControl>(this);
                    if (workflowCanvas != null)
                    {
                        workflowCanvas.SetPathCalculator(pathCalculatorType);
                        System.Diagnostics.Debug.WriteLine($"[MainWindow] ✅ WorkflowCanvasControl 路径计算器已设置");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[MainWindow] ❌ 未找到 WorkflowCanvasControl");
                    }
                    break;

                case CanvasType.AIStudioDiagram:
                    // 查找当前显示的AIStudioDiagramControl
                    var aistudioDiagram = FindVisualChild<AIStudioDiagramControl>(this);
                    if (aistudioDiagram != null)
                    {
                        aistudioDiagram.SetPathCalculator(pathCalculatorType);
                        System.Diagnostics.Debug.WriteLine($"[MainWindow] ✅ AIStudioDiagramControl 路径计算器已设置");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[MainWindow] ❌ 未找到 AIStudioDiagramControl");
                    }
                    break;
            }

            // 同时调用CanvasEngineManager，保持兼容性
            CanvasEngineManager.SetPathCalculator(pathCalculatorType);
        }

        /// <summary>
        /// 获取当前画布类型
        /// </summary>
        public CanvasType GetCurrentCanvasType()
        {
            return _viewModel?.WorkflowTabViewModel?.SelectedTab?.CanvasType ?? CanvasType.WorkflowCanvas;
        }

        /// <summary>
        /// 获取当前画布引擎
        /// </summary>
        public Interfaces.ICanvasEngine? GetCurrentCanvasEngine()
        {
            return CanvasEngineManager.GetCurrentEngine();
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