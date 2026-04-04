using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SunEyeVision.Plugin.SDK.Commands;
using SunEyeVision.Plugin.SDK.Logging;
using SunEyeVision.Plugin.SDK.UI.Controls;
using SunEyeVision.Plugin.SDK.UI.Events;

namespace SunEyeVision.Plugin.SDK.UI.Windows
{
    /// <summary>
    /// 普通窗口壳 - 包装工具调试控件为标准 Windows 窗口
    /// </summary>
    /// <remarks>
    /// 架构说明：
    /// - 轻量级窗口壳，提供窗口容器功能
    /// - 接受任何 UserControl 作为内容
    /// - 自动添加底部按钮栏（连续运行、运行、确定）
    /// - 使用路由命令和路由事件实现声明式编程
    /// 
    /// 使用场景：
    /// - 大部分标准工具（阈值、滤波、定位等）
    /// - 需要标准窗口样式的工具
    /// 
    /// 不适用场景：
    /// - 需要无边框窗口的工具（使用 CustomDebugWindow）
    /// 
    /// 声明式编程支持：
    /// - 使用路由命令（ToolCommands）绑定按钮
    /// - 使用路由事件（ToolExecutionCompleted）监听执行完成
    /// - 工具控件通过继承 ToolDebugControlBase 实现标准接口
    /// </remarks>
    public partial class DefaultDebugWindow : Window
    {
        #region 字段

        private readonly UserControl _control;
        private readonly ToolDebugControlBase? _toolControl;
        
        // 状态
        private bool _isContinuousRunning;
        private bool _isExecuting;

        #endregion

        #region 构造函数

        /// <summary>
        /// 创建普通窗口壳
        /// </summary>
        /// <param name="control">工具调试控件实例</param>
        public DefaultDebugWindow(UserControl control)
        {
            InitializeComponent();
            
            _control = control;
            _toolControl = control as ToolDebugControlBase;

            PluginLogger.Info("DefaultDebugWindow 构造函数开始", "DefaultDebugWindow");

            // 设置内容
            ContentHost.Content = _control;

            // 订阅路由事件
            SubscribeRoutedEvents();

            // 窗口关闭时清理
            Closed += OnWindowClosed;

            // 监听 Title 属性变化，同步更新标题栏显示
            var dpDescriptor = System.ComponentModel.DependencyPropertyDescriptor.FromProperty(
                Window.TitleProperty, typeof(Window));
            dpDescriptor.AddValueChanged(this, (s, e) =>
            {
                TitleTextBlock.Text = Title;
            });

            PluginLogger.Success("DefaultDebugWindow 构造函数完成", "DefaultDebugWindow");
        }

        #endregion

        #region 路由事件订阅

        /// <summary>
        /// 订阅路由事件
        /// </summary>
        private void SubscribeRoutedEvents()
        {
            // 订阅工具执行完成事件
            _control.AddHandler(
                ToolDebugControlBase.ToolExecutionCompletedEvent,
                new ToolExecutionCompletedEventHandler(OnToolExecutionCompleted));
            
            PluginLogger.Info("已订阅 ToolExecutionCompleted 路由事件", "DefaultDebugWindow");
        }

        /// <summary>
        /// 取消订阅路由事件
        /// </summary>
        private void UnsubscribeRoutedEvents()
        {
            _control.RemoveHandler(
                ToolDebugControlBase.ToolExecutionCompletedEvent,
                new ToolExecutionCompletedEventHandler(OnToolExecutionCompleted));
            
            PluginLogger.Info("已取消订阅 ToolExecutionCompleted 路由事件", "DefaultDebugWindow");
        }

        #endregion

        #region 命令处理

        /// <summary>
        /// 执行命令处理
        /// </summary>
        private void OnExecuteCommand(object sender, ExecutedRoutedEventArgs e)
        {
            if (_isExecuting) return;

            _isExecuting = true;
            SetButtonsEnabled(false);

            PluginLogger.Info("执行命令触发", "DefaultDebugWindow");

            // 命令会由工具控件处理，这里只需要设置状态
        }

        /// <summary>
        /// 判断是否可执行
        /// </summary>
        private void CanExecuteCommand(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !_isExecuting;
        }

        /// <summary>
        /// 确认命令处理
        /// </summary>
        private void OnConfirmCommand(object sender, ExecutedRoutedEventArgs e)
        {
            PluginLogger.Info("确认命令触发", "DefaultDebugWindow");

            // 执行参数验证
            if (ValidateParameters())
            {
                PluginLogger.Success("参数验证通过，关闭窗口", "DefaultDebugWindow");
                Close();
            }
            else
            {
                PluginLogger.Warning("参数验证失败", "DefaultDebugWindow");
                MessageBox.Show("参数验证失败，请检查输入参数。", "验证失败", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// 连续执行命令处理
        /// </summary>
        private void OnContinuousExecuteCommand(object sender, ExecutedRoutedEventArgs e)
        {
            if (_isContinuousRunning)
            {
                // 停止连续运行
                _isContinuousRunning = false;
                ContinuousRunButton.Content = "连续执行";
                RunButton.Visibility = Visibility.Visible;
                
                PluginLogger.Info("停止连续运行", "DefaultDebugWindow");
            }
            else
            {
                // 开始连续运行
                _isContinuousRunning = true;
                ContinuousRunButton.Content = "停止运行";
                RunButton.Visibility = Visibility.Collapsed;
                
                PluginLogger.Info("开始连续运行", "DefaultDebugWindow");
                
                // 触发第一次执行
                TriggerExecute();
            }
        }

        #endregion

        #region 工具事件处理

        /// <summary>
        /// 工具执行完成事件处理
        /// </summary>
        private void OnToolExecutionCompleted(object sender, ToolExecutionCompletedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                _isExecuting = false;
                SetButtonsEnabled(true);
                
                PluginLogger.Info("工具执行完成，按钮已恢复", "DefaultDebugWindow");
                
                // 如果是连续运行模式，继续下一次执行
                if (_isContinuousRunning && IsLoaded)
                {
                    // 延迟500ms后再次执行
                    Task.Delay(500).ContinueWith(_ =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            if (_isContinuousRunning && IsLoaded)
                            {
                                TriggerExecute();
                            }
                        });
                    });
                }
            });
        }

        /// <summary>
        /// 触发执行
        /// </summary>
        private void TriggerExecute()
        {
            if (_isExecuting) return;

            _isExecuting = true;
            SetButtonsEnabled(false);

            // 触发路由命令
            if (_control.CommandBindings.Count > 0)
            {
                // 执行命令会由工具控件处理
                CommandManager.InvalidateRequerySuggested();
            }
        }

        #endregion

        #region 数据注入转发

        /// <summary>
        /// 设置当前节点（委托给内部控件）
        /// </summary>
        /// <remarks>
        /// 必须在 SetDataProvider 之前调用，因为 SetDataProvider 内部会恢复配置，
        /// 需要当前节点引用已设置。
        /// 
        /// 调用链：
        /// MainWindowViewModel → DefaultDebugWindow.SetCurrentNode → ThresholdToolDebugControl.SetCurrentNode
        /// </remarks>
        public void SetCurrentNode(object node)
        {
            if (_toolControl != null)
            {
                _toolControl.SetCurrentNode(node);
                PluginLogger.Success(
                    $"已设置当前节点到调试控件: {_control.GetType().Name}", 
                    "DefaultDebugWindow");
            }
            else
            {
                PluginLogger.Warning(
                    $"控件 {_control.GetType().Name} 未实现 IToolDebugControl 接口", 
                    "DefaultDebugWindow");
            }
        }

        /// <summary>
        /// 设置数据提供者（委托给内部控件）
        /// </summary>
        /// <remarks>
        /// 数据提供者包含所有前驱节点的输出数据，用于填充图像源选择器。
        /// 此方法必须在 SetCurrentNode 之后调用。
        /// 
        /// 调用链：
        /// MainWindowViewModel → DefaultDebugWindow.SetDataProvider → ThresholdToolDebugControl.SetDataProvider
        /// </remarks>
        public void SetDataProvider(object dataProvider)
        {
            if (_toolControl != null)
            {
                _toolControl.SetDataProvider(dataProvider);
                PluginLogger.Success(
                    $"已将数据提供者注入到调试控件: {_control.GetType().Name}", 
                    "DefaultDebugWindow");
            }
            else
            {
                PluginLogger.Warning(
                    $"控件 {_control.GetType().Name} 未继承 ToolDebugControlBase 基类", 
                    "DefaultDebugWindow");
            }
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 设置按钮启用状态
        /// </summary>
        private void SetButtonsEnabled(bool enabled)
        {
            if (!_isContinuousRunning)
            {
                ContinuousRunButton.IsEnabled = enabled;
            }
            
            if (RunButton.Visibility == Visibility.Visible)
            {
                RunButton.IsEnabled = enabled;
            }
            
            ConfirmButton.IsEnabled = enabled;
        }

        /// <summary>
        /// 执行参数验证
        /// </summary>
        private bool ValidateParameters()
        {
            if (_toolControl != null)
            {
                return _toolControl.ValidateParameters();
            }

            // 默认返回true
            return true;
        }

        #endregion

        #region 窗口事件处理

        /// <summary>
        /// 标题栏鼠标左键按下（支持拖拽）
        /// </summary>
        private void OnTitleBarMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        /// <summary>
        /// 关闭按钮点击
        /// </summary>
        private void OnCloseButtonClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        /// <summary>
        /// 窗口关闭事件处理
        /// </summary>
        private void OnWindowClosed(object? sender, EventArgs e)
        {
            // 停止连续运行
            _isContinuousRunning = false;
            
            // 清理路由事件订阅
            UnsubscribeRoutedEvents();

            PluginLogger.Info("窗口已关闭，事件已清理", "DefaultDebugWindow");
        }

        #endregion
    }
}
