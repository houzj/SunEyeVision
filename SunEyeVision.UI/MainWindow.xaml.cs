using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using SunEyeVision.Events;
using SunEyeVision.Interfaces;
using SunEyeVision.UI.Controls;
using SunEyeVision.UI.Events;
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

        // 布局配置
        private LayoutConfig _layoutConfig;

        // EventBus 相关
        private readonly IEventBus _eventBus;
        private readonly UIEventPublisher _eventPublisher;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainWindowViewModel();
            DataContext = _viewModel;

            // 初始化 EventBus
            var logger = new Services.ConsoleLogger();
            _eventBus = new EventBus(logger);
            _eventPublisher = new UIEventPublisher(_eventBus);

            _layoutConfig = LayoutConfig.Load();
            LoadLayoutConfig();

            RegisterHotkeys();
            RegisterLayoutEvents();
            SubscribeToEvents();
            SubscribeToViewModelEvents();
            SubscribeToControlEvents();
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
            // 保存布局配置
            _layoutConfig.LeftColumnWidth = LeftColumn.Width.Value;
            _layoutConfig.RightColumnWidth = RightColumn.Width.Value;
            _layoutConfig.IsLeftPanelCollapsed = LeftColumn.Width.Value == 0;
            _layoutConfig.IsRightPanelCollapsed = RightColumn.Width.Value == 0;
            _layoutConfig.Save();

            // 取消事件订阅
            UnsubscribeFromEvents();

            // 清理资源
            if (_eventBus is IDisposable disposableEventBus)
            {
                disposableEventBus.Dispose();
            }
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

        #region 工具箱拖拽

        private void ToolItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is Models.ToolItem tool)
            {
                try
                {
                    var dragData = new DataObject("ToolItem", tool);
                    DragDrop.DoDragDrop(border, dragData, DragDropEffects.Copy);
                    e.Handled = true;
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"拖拽失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void CategoryHeader_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is ToolCategory category)
            {
                category.IsExpanded = !category.IsExpanded;
            }
        }

        #endregion

        #region 布局调整

        /// <summary>
        /// 注册布局相关事件
        /// </summary>
        private void RegisterLayoutEvents()
        {
            _viewModel.ResetLayoutRequested += OnResetLayoutRequested;
        }

        /// <summary>
        /// 订阅事件
        /// </summary>
        private void SubscribeToEvents()
        {
            // 订阅日志事件
            _eventBus.Subscribe<LogEvent>(OnLogEvent);

            // 订阅错误事件
            _eventBus.Subscribe<ErrorEvent>(OnErrorEvent);

            // 订阅工作流事件
            _eventBus.Subscribe<WorkflowExecutedEvent>(OnWorkflowExecuted);
            _eventBus.Subscribe<WorkflowNodeExecutedEvent>(OnWorkflowNodeExecuted);

            // 记录日志
            _eventBus.Publish(new LogEvent("MainWindow", "MainWindow subscribed to events", LogLevel.Info));
        }

        /// <summary>
        /// 订阅ViewModel事件
        /// </summary>
        private void SubscribeToViewModelEvents()
        {
            _viewModel.WorkflowSwitched += OnWorkflowSwitched;
        }

        /// <summary>
        /// 订阅控件事件
        /// </summary>
        private void SubscribeToControlEvents()
        {
            WorkflowCanvas.WorkflowSwitched += OnWorkflowCanvasWorkflowSwitched;
        }

        /// <summary>
        /// 取消事件订阅
        /// </summary>
        private void UnsubscribeFromEvents()
        {
            _eventBus.Unsubscribe<LogEvent>(OnLogEvent);
            _eventBus.Unsubscribe<ErrorEvent>(OnErrorEvent);
            _eventBus.Unsubscribe<WorkflowExecutedEvent>(OnWorkflowExecuted);
            _eventBus.Unsubscribe<WorkflowNodeExecutedEvent>(OnWorkflowNodeExecuted);

            // 取消ViewModel事件订阅
            _viewModel.WorkflowSwitched -= OnWorkflowSwitched;

            // 取消控件事件订阅
            WorkflowCanvas.WorkflowSwitched -= OnWorkflowCanvasWorkflowSwitched;

            _eventBus.Publish(new LogEvent("MainWindow", "MainWindow unsubscribed from events", LogLevel.Info));
        }

        /// <summary>
        /// 处理日志事件
        /// </summary>
        private void OnLogEvent(LogEvent eventData)
        {
            // 可以在这里更新日志UI
            System.Diagnostics.Debug.WriteLine($"[Log] {eventData.LogLevel}: {eventData.Message}");
        }

        /// <summary>
        /// 处理错误事件
        /// </summary>
        private void OnErrorEvent(ErrorEvent eventData)
        {
            // 可以在这里显示错误提示
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                var icon = eventData.Severity == ErrorSeverity.Critical ?
                    System.Windows.MessageBoxImage.Error :
                    System.Windows.MessageBoxImage.Warning;

                System.Windows.MessageBox.Show(
                    eventData.ErrorMessage,
                    $"错误 - {eventData.Source}",
                    System.Windows.MessageBoxButton.OK,
                    icon);
            });
        }

        /// <summary>
        /// 处理工作流执行事件
        /// </summary>
        private void OnWorkflowExecuted(WorkflowExecutedEvent eventData)
        {
            var status = eventData.Success ? "完成" : "失败";
            var message = $"工作流 '{eventData.WorkflowName}' {status} (耗时: {eventData.ExecutionDurationMs}ms)";

            _eventPublisher.PublishStatusUpdate(message);
        }

        /// <summary>
        /// 处理工作流节点执行事件
        /// </summary>
        private void OnWorkflowNodeExecuted(WorkflowNodeExecutedEvent eventData)
        {
            var status = eventData.Success ? "成功" : "失败";
            var message = $"节点 '{eventData.NodeName}' 执行{status} (耗时: {eventData.ExecutionDurationMs}ms)";

            _eventPublisher.PublishStatusUpdate(message);
        }

        /// <summary>
        /// 处理工作流切换事件
        /// </summary>
        private void OnWorkflowSwitched(object? sender, string workflowName)
        {
            // 状态栏会自动更新，无需额外处理
        }

        /// <summary>
        /// 处理WorkflowCanvas的工作流切换事件
        /// </summary>
        private void OnWorkflowCanvasWorkflowSwitched(object? sender, string workflowName)
        {
            // 同步更新ViewModel的CurrentWorkflow
            var workflow = _viewModel.Workflows.FirstOrDefault(w => w.Name == workflowName);
            if (workflow != null)
            {
                _viewModel.CurrentWorkflow = workflow;
            }
        }

        /// <summary>
        /// 加载布局配置
        /// </summary>
        private void LoadLayoutConfig()
        {
            LeftColumn.Width = new GridLength(_layoutConfig.IsLeftPanelCollapsed ? 0 : _layoutConfig.LeftColumnWidth);
            RightColumn.Width = new GridLength(_layoutConfig.IsRightPanelCollapsed ? 0 : _layoutConfig.RightColumnWidth);

            UpdateCollapseButtonStates();
        }

        /// <summary>
        /// 更新折叠按钮状态
        /// </summary>
        private void UpdateCollapseButtonStates()
        {
            var isLeftCollapsed = LeftColumn.Width.Value == 0;
            var isRightCollapsed = RightColumn.Width.Value == 0;

            LeftCollapseButtonText.Text = isLeftCollapsed ? "▶" : "◀";
            RightCollapseButtonText.Text = isRightCollapsed ? "◀" : "▶";

            LeftSplitterColumn.Width = isLeftCollapsed ? new GridLength(0) : new GridLength(5);
            RightSplitterColumn.Width = isRightCollapsed ? new GridLength(0) : new GridLength(5);

            LeftPanel.Visibility = isLeftCollapsed ? Visibility.Collapsed : Visibility.Visible;
            RightPanel.Visibility = isRightCollapsed ? Visibility.Collapsed : Visibility.Visible;

            // 动态调整按钮位置
            if (isLeftCollapsed)
            {
                // 左侧折叠时，按钮移到中间列，靠近左侧边缘
                Grid.SetColumn(LeftCollapseButton, 2);
                LeftCollapseButton.HorizontalAlignment = HorizontalAlignment.Left;
                LeftCollapseButton.Margin = new Thickness(2, 0, 0, 0);
            }
            else
            {
                // 左侧展开时，按钮在左侧列，靠近右侧边缘
                Grid.SetColumn(LeftCollapseButton, 0);
                LeftCollapseButton.HorizontalAlignment = HorizontalAlignment.Right;
                LeftCollapseButton.Margin = new Thickness(0, 0, -14, 0);
            }

            if (isRightCollapsed)
            {
                // 右侧折叠时，按钮移到中间列，靠近右侧边缘
                Grid.SetColumn(RightCollapseButton, 2);
                RightCollapseButton.HorizontalAlignment = HorizontalAlignment.Right;
                RightCollapseButton.Margin = new Thickness(0, 0, 2, 0);
            }
            else
            {
                // 右侧展开时，按钮在右侧列，靠近左侧边缘
                Grid.SetColumn(RightCollapseButton, 4);
                RightCollapseButton.HorizontalAlignment = HorizontalAlignment.Left;
                RightCollapseButton.Margin = new Thickness(-14, 0, 0, 0);
            }
        }

        /// <summary>
        /// 重置布局事件处理
        /// </summary>
        private void OnResetLayoutRequested(object? sender, EventArgs e)
        {
            _layoutConfig.Reset();
            LoadLayoutConfig();
        }

        #region 左侧分隔符事件

        private void LeftSplitter_DragDelta(object sender, DragDeltaEventArgs e)
        {
            if (LeftColumn.Width.Value == 0) return; // 折叠状态下不允许拖动

            var oldWidth = LeftColumn.Width.Value;
            var newWidth = oldWidth + e.HorizontalChange;
            newWidth = Math.Max(LayoutConfig.GetMinLeftColumnWidth(), Math.Min(LayoutConfig.GetMaxLeftColumnWidth(), newWidth));

            LeftColumn.Width = new GridLength(newWidth);

            // 发布布局改变事件（节流处理）
            // _eventPublisher.PublishLayoutChanged("LeftPanel", oldWidth, newWidth, false);
        }

        private void LeftSplitter_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // 双击折叠/展开左侧面板
            ToggleLeftPanel();
        }

        private void LeftCollapseButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleLeftPanel();
        }

        private void LeftCollapseButton_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Button btn)
            {
                btn.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(200, 230, 240, 255));
            }
        }

        private void LeftCollapseButton_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Button btn)
            {
                btn.Background = System.Windows.Media.Brushes.Transparent;
            }
        }

        private void ToggleLeftPanel()
        {
            var oldWidth = LeftColumn.Width.Value;
            var isCollapsed = oldWidth == 0;

            if (isCollapsed)
            {
                // 展开：使用之前保存的宽度或默认宽度
                var newWidth = _layoutConfig.LeftColumnWidth > 0 ? _layoutConfig.LeftColumnWidth : LayoutConfig.GetMinLeftColumnWidth();
                LeftColumn.Width = new GridLength(newWidth);
                LeftSplitterColumn.Width = new GridLength(5);
                LeftPanel.Visibility = Visibility.Visible;

                // 发布布局改变事件
                _eventPublisher.PublishLayoutChanged("LeftPanel", 0, newWidth, false);
            }
            else
            {
                // 折叠：保存当前宽度并折叠
                _layoutConfig.LeftColumnWidth = oldWidth;
                LeftColumn.Width = new GridLength(0);
                LeftSplitterColumn.Width = new GridLength(0);
                LeftPanel.Visibility = Visibility.Collapsed;

                // 发布布局改变事件
                _eventPublisher.PublishLayoutChanged("LeftPanel", oldWidth, 0, true);
            }

            UpdateCollapseButtonStates();
        }

        #endregion

        #region 右侧分隔符事件

        private void RightSplitter_DragDelta(object sender, DragDeltaEventArgs e)
        {
            if (RightColumn.Width.Value == 0) return; // 折叠状态下不允许拖动

            var oldWidth = RightColumn.Width.Value;
            var newWidth = oldWidth - e.HorizontalChange;
            newWidth = Math.Max(LayoutConfig.GetMinRightColumnWidth(), Math.Min(LayoutConfig.GetMaxRightColumnWidth(), newWidth));

            // 确保中间列有足够空间
            var middleWidth = ActualWidth - LeftColumn.Width.Value - RightSplitterColumn.Width.Value - newWidth;
            if (middleWidth >= LayoutConfig.GetMinMiddleColumnWidth())
            {
                RightColumn.Width = new GridLength(newWidth);

                // 发布布局改变事件（节流处理）
                // _eventPublisher.PublishLayoutChanged("RightPanel", oldWidth, newWidth, false);
            }
        }

        private void RightSplitter_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // 双击折叠/展开右侧面板
            ToggleRightPanel();
        }

        private void RightCollapseButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleRightPanel();
        }

        private void RightCollapseButton_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Button btn)
            {
                btn.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(200, 230, 240, 255));
            }
        }

        private void RightCollapseButton_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Button btn)
            {
                btn.Background = System.Windows.Media.Brushes.Transparent;
            }
        }

        private void ToggleRightPanel()
        {
            var oldWidth = RightColumn.Width.Value;
            var isCollapsed = oldWidth == 0;

            if (isCollapsed)
            {
                // 展开：使用之前保存的宽度或默认宽度
                var newWidth = _layoutConfig.RightColumnWidth > 0 ? _layoutConfig.RightColumnWidth : LayoutConfig.GetMinRightColumnWidth();
                RightColumn.Width = new GridLength(newWidth);
                RightSplitterColumn.Width = new GridLength(5);
                RightPanel.Visibility = Visibility.Visible;

                // 发布布局改变事件
                _eventPublisher.PublishLayoutChanged("RightPanel", 0, newWidth, false);
            }
            else
            {
                // 折叠：保存当前宽度并折叠
                _layoutConfig.RightColumnWidth = oldWidth;
                RightColumn.Width = new GridLength(0);
                RightSplitterColumn.Width = new GridLength(0);
                RightPanel.Visibility = Visibility.Collapsed;

                // 发布布局改变事件
                _eventPublisher.PublishLayoutChanged("RightPanel", oldWidth, 0, true);
            }

            UpdateCollapseButtonStates();
        }

        #endregion

        #region 工作流画布事件

        /// <summary>
        /// 节点双击事件 - 打开调试界面
        /// </summary>
        private void WorkflowCanvas_NodeDoubleClicked(object? sender, Models.WorkflowNode node)
        {
            if (node != null)
            {
                _viewModel.OpenDebugWindowCommand.Execute(node);
            }
        }

        #endregion

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
