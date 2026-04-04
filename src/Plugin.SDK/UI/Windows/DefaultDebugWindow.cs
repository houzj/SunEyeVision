using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using SunEyeVision.Plugin.SDK.Logging;

namespace SunEyeVision.Plugin.SDK.UI.Windows
{
    /// <summary>
    /// 普通窗口壳 - 包装工具调试控件为标准Windows窗口
    /// </summary>
    /// <remarks>
    /// 架构说明：
    /// - 轻量级窗口壳，提供窗口容器功能
    /// - 接受任何 UserControl 作为内容
    /// - 自动添加底部按钮栏（连续运行、运行、确定）
    /// - 通过反射自动连接 UserControl 的事件
    /// 
    /// 使用场景：
    /// - 大部分标准工具（阈值、滤波、定位等）
    /// - 需要标准窗口样式的工具
    /// 
    /// 不适用场景：
    /// - 需要无边框窗口的工具（使用 CustomDebugWindow）
    /// </remarks>
    public class DefaultDebugWindow : Window
    {
        #region 字段

        private readonly UserControl _control;
        private readonly DispatcherTimer? _continuousExecuteTimer;
        
        // 底部按钮
        private readonly Button _continuousRunButton;
        private readonly Button _runButton;
        private readonly Button _confirmButton;
        
        // 状态
        private bool _isContinuousRunning;
        private bool _isExecuting;
        
        // 标题栏
        private Border? _titleBar;
        private TextBlock? _titleTextBlock;

        #endregion

        #region 构造函数

        /// <summary>
        /// 创建普通窗口壳
        /// </summary>
        /// <param name="control">工具调试控件实例</param>
        public DefaultDebugWindow(UserControl control)
        {
            _control = control;

            PluginLogger.Info("DefaultDebugWindow 构造函数开始", "DefaultDebugWindow");

            #region 窗口属性设置

            Title = "工具调试窗口";
            Width = 450;
            Height = 700;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            
            // 无边框窗口样式（用于自定义标题栏）
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ResizeMode = ResizeMode.NoResize;

            // 设置窗口图标（如果有）
            try
            {
                Icon = CreateWindowIcon();
            }
            catch
            {
                // 图标加载失败不影响窗口创建
                PluginLogger.Warning("窗口图标加载失败", "DefaultDebugWindow");
            }

            #endregion

            #region 创建底部按钮栏

            var buttonPanel = CreateButtonPanel();
            _continuousRunButton = (Button)buttonPanel.Children[0];
            _runButton = (Button)buttonPanel.Children[1];
            _confirmButton = (Button)buttonPanel.Children[2];

            #endregion

            #region 创建主布局

            // 创建外层容器（白色背景 + 阴影）
            var outerBorder = new Border
            {
                Background = (Brush)FindResource("BackgroundBrush"),
                CornerRadius = new CornerRadius(0),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CCCCCC")),
                BorderThickness = new Thickness(1)
            };

            // 添加阴影效果
            var effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                Color = Colors.Black,
                Opacity = 0.2,
                ShadowDepth = 2,
                BlurRadius = 8
            };
            outerBorder.Effect = effect;

            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });  // 标题栏
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });  // 内容区
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(40) });  // 底部按钮栏

            // 添加标题栏
            var titleBar = CreateTitleBar();
            Grid.SetRow(titleBar, 0);
            mainGrid.Children.Add(titleBar);

            // 添加控件到主布局
            Grid.SetRow(_control, 1);
            mainGrid.Children.Add(_control);

            // 添加底部按钮栏
            var buttonBar = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2D2D2D")),  // 深色背景
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#404040")),
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(16, 6, 16, 6),
                Child = buttonPanel
            };
            Grid.SetRow(buttonBar, 2);
            mainGrid.Children.Add(buttonBar);

            outerBorder.Child = mainGrid;
            Content = outerBorder;

            #endregion

            #region 订阅事件

            // 订阅UserControl的事件
            SubscribeControlEvents(_control);

            // 窗口关闭时清理
            Closed += OnWindowClosed;

            // 监听 Title 属性变化，同步更新标题栏显示
            var dpDescriptor = System.ComponentModel.DependencyPropertyDescriptor.FromProperty(
                Window.TitleProperty, typeof(Window));
            dpDescriptor.AddValueChanged(this, (s, e) =>
            {
                if (_titleTextBlock != null)
                {
                    _titleTextBlock.Text = Title;
                }
            });

            #endregion

            PluginLogger.Success("DefaultDebugWindow 构造函数完成", "DefaultDebugWindow");
        }

        #endregion

        #region 创建UI

        /// <summary>
        /// 创建标题栏
        /// </summary>
        private Border CreateTitleBar()
        {
            _titleBar = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E1E1E")),  // 深色背景
                Height = 32,
                Padding = new Thickness(12, 0, 8, 0)
            };

            var titleGrid = new Grid();
            titleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            titleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // 标题文字
            _titleTextBlock = new TextBlock
            {
                Text = Title,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 13
            };
            Grid.SetColumn(_titleTextBlock, 0);
            titleGrid.Children.Add(_titleTextBlock);

            // 关闭按钮
            var closeButton = new Button
            {
                Content = "✕",
                Width = 32,
                Height = 32,
                Background = Brushes.Transparent,
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                FontSize = 14,
                Cursor = System.Windows.Input.Cursors.Hand
            };
            closeButton.Click += (s, e) => Close();
            
            // 鼠标悬停效果
            closeButton.MouseEnter += (s, e) => closeButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E81123"));
            closeButton.MouseLeave += (s, e) => closeButton.Background = Brushes.Transparent;
            
            Grid.SetColumn(closeButton, 1);
            titleGrid.Children.Add(closeButton);

            _titleBar.Child = titleGrid;

            // 支持窗口拖拽
            _titleBar.MouseLeftButtonDown += (s, e) =>
            {
                if (e.ButtonState == System.Windows.Input.MouseButtonState.Pressed)
                {
                    DragMove();
                }
            };

            return _titleBar;
        }

        /// <summary>
        /// 创建底部按钮面板
        /// </summary>
        private StackPanel CreateButtonPanel()
        {
            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            // 连续运行按钮
            var continuousRunButton = new Button
            {
                Content = "连续执行",
                Width = 100,
                Height = 28,
                Margin = new Thickness(0, 0, 8, 0),
                Cursor = System.Windows.Input.Cursors.Hand,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3A3A3A")),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#505050")),
                BorderThickness = new Thickness(1)
            };
            continuousRunButton.Click += OnContinuousRunClick;
            panel.Children.Add(continuousRunButton);

            // 运行按钮
            var runButton = new Button
            {
                Content = "执行",
                Width = 100,
                Height = 28,
                Margin = new Thickness(0, 0, 8, 0),
                Cursor = System.Windows.Input.Cursors.Hand,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3A3A3A")),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#505050")),
                BorderThickness = new Thickness(1)
            };
            runButton.Click += OnRunClick;
            panel.Children.Add(runButton);

            // 确定按钮
            var confirmButton = new Button
            {
                Content = "确定",
                Width = 100,
                Height = 28,
                Cursor = System.Windows.Input.Cursors.Hand,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3A3A3A")),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#505050")),
                BorderThickness = new Thickness(1)
            };
            confirmButton.Click += OnConfirmClick;
            panel.Children.Add(confirmButton);

            return panel;
        }

        /// <summary>
        /// 创建窗口图标
        /// </summary>
        private ImageSource? CreateWindowIcon()
        {
            // TODO: 从资源加载窗口图标
            // 暂时返回 null，后续可以添加图标资源
            return null;
        }

        #endregion

        #region 事件订阅

        /// <summary>
        /// 订阅UserControl的事件
        /// </summary>
        private void SubscribeControlEvents(UserControl control)
        {
            // 订阅 ExecuteRequested 事件
            var executeEvent = control.GetType().GetEvent("ExecuteRequested");
            if (executeEvent != null)
            {
                try
                {
                    var handler = Delegate.CreateDelegate(executeEvent.EventHandlerType, this, nameof(OnExecuteRequested));
                    executeEvent.AddEventHandler(control, handler);
                    PluginLogger.Info("已订阅 ExecuteRequested 事件", "DefaultDebugWindow");
                }
                catch (Exception ex)
                {
                    PluginLogger.Warning($"订阅 ExecuteRequested 事件失败: {ex.Message}", "DefaultDebugWindow");
                }
            }

            // 订阅 ConfirmClicked 事件
            var confirmEvent = control.GetType().GetEvent("ConfirmClicked");
            if (confirmEvent != null)
            {
                try
                {
                    var handler = Delegate.CreateDelegate(confirmEvent.EventHandlerType, this, nameof(OnConfirmClicked));
                    confirmEvent.AddEventHandler(control, handler);
                    PluginLogger.Info("已订阅 ConfirmClicked 事件", "DefaultDebugWindow");
                }
                catch (Exception ex)
                {
                    PluginLogger.Warning($"订阅 ConfirmClicked 事件失败: {ex.Message}", "DefaultDebugWindow");
                }
            }

            // 订阅 ToolExecutionCompleted 事件（用于恢复按钮状态）
            var completedEvent = control.GetType().GetEvent("ToolExecutionCompleted");
            if (completedEvent != null)
            {
                try
                {
                    // 获取事件类型（可能是 EventHandler<T> 或 EventHandler）
                    var eventHandlerType = completedEvent.EventHandlerType;
                    
                    // 检查是否为泛型事件类型 EventHandler<T>
                    if (eventHandlerType.IsGenericType && 
                        eventHandlerType.GetGenericTypeDefinition() == typeof(EventHandler<>))
                    {
                        // 提取泛型参数类型（如 ThresholdResults）
                        var resultType = eventHandlerType.GetGenericArguments()[0];
                        
                        // 获取泛型处理方法 OnToolExecutionCompleted<T>
                        var handlerMethod = typeof(DefaultDebugWindow).GetMethod(
                            nameof(OnToolExecutionCompletedGeneric), 
                            BindingFlags.NonPublic | BindingFlags.Instance);
                        
                        // 创建泛型方法（如 OnToolExecutionCompleted<ThresholdResults>）
                        var genericHandler = handlerMethod!.MakeGenericMethod(resultType);
                        
                        // 创建匹配事件签名的委托
                        var handler = Delegate.CreateDelegate(eventHandlerType, this, genericHandler);
                        completedEvent.AddEventHandler(control, handler);
                        
                        PluginLogger.Success($"已订阅 ToolExecutionCompleted<{resultType.Name}> 事件", "DefaultDebugWindow");
                    }
                    else
                    {
                        // 非泛型事件，直接使用 EventHandler
                        var handler = Delegate.CreateDelegate(eventHandlerType, this, nameof(OnToolExecutionCompleted));
                        completedEvent.AddEventHandler(control, handler);
                        PluginLogger.Info("已订阅 ToolExecutionCompleted 事件", "DefaultDebugWindow");
                    }
                }
                catch (Exception ex)
                {
                    PluginLogger.Error($"订阅 ToolExecutionCompleted 事件失败: {ex.Message}", "DefaultDebugWindow");
                }
            }
        }

        #endregion

        #region 按钮事件处理

        /// <summary>
        /// 连续运行按钮点击
        /// </summary>
        private void OnContinuousRunClick(object sender, RoutedEventArgs e)
        {
            if (_isContinuousRunning)
            {
                // 停止连续运行
                _isContinuousRunning = false;
                _continuousRunButton.Content = "连续运行";
                _runButton.Visibility = Visibility.Visible;
                
                PluginLogger.Info("停止连续运行", "DefaultDebugWindow");
            }
            else
            {
                // 开始连续运行
                _isContinuousRunning = true;
                _continuousRunButton.Content = "停止运行";
                _runButton.Visibility = Visibility.Collapsed;
                
                PluginLogger.Info("开始连续运行", "DefaultDebugWindow");
                
                // 触发第一次执行
                TriggerExecute();
            }
        }

        /// <summary>
        /// 运行按钮点击
        /// </summary>
        private void OnRunClick(object sender, RoutedEventArgs e)
        {
            PluginLogger.Info("运行按钮点击", "DefaultDebugWindow");
            TriggerExecute();
        }

        /// <summary>
        /// 确定按钮点击
        /// </summary>
        private void OnConfirmClick(object sender, RoutedEventArgs e)
        {
            PluginLogger.Info("确定按钮点击", "DefaultDebugWindow");
            
            // 执行参数验证
            if (ValidateParameters())
            {
                PluginLogger.Success("参数验证通过，关闭窗口", "DefaultDebugWindow");
                Close();
            }
            else
            {
                PluginLogger.Warning("参数验证失败", "DefaultDebugWindow");
                MessageBox.Show("参数验证失败，请检查输入参数。", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// 触发执行
        /// </summary>
        private void TriggerExecute()
        {
            if (_isExecuting) return;
            
            _isExecuting = true;
            SetButtonsEnabled(false);
            
            // 触发 ExecuteRequested 事件
            var executeEvent = _control.GetType().GetEvent("ExecuteRequested");
            if (executeEvent != null)
            {
                try
                {
                    var method = _control.GetType().GetMethod("OnExecuteRequested", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    method?.Invoke(_control, null);
                }
                catch (Exception ex)
                {
                    PluginLogger.Error($"触发执行失败: {ex.Message}", "DefaultDebugWindow");
                    OnToolExecutionCompleted(_control, EventArgs.Empty);
                }
            }
            else
            {
                // 没有事件，直接恢复按钮状态
                OnToolExecutionCompleted(_control, EventArgs.Empty);
            }
        }

        #endregion

        #region UserControl事件处理

        /// <summary>
        /// 执行请求事件处理
        /// </summary>
        private void OnExecuteRequested(object? sender, EventArgs e)
        {
            PluginLogger.Info("收到执行请求", "DefaultDebugWindow");
        }

        /// <summary>
        /// 确认按钮点击事件处理
        /// </summary>
        private void OnConfirmClicked(object? sender, EventArgs e)
        {
            PluginLogger.Info("确认按钮已点击，关闭窗口", "DefaultDebugWindow");
            Close();
        }

        /// <summary>
        /// 工具执行完成事件处理（非泛型版本）
        /// </summary>
        private void OnToolExecutionCompleted(object? sender, EventArgs e)
        {
            OnToolExecutionCompletedCore();
        }
        
        /// <summary>
        /// 工具执行完成事件处理（泛型版本）
        /// </summary>
        private void OnToolExecutionCompletedGeneric<T>(object? sender, T e)
        {
            OnToolExecutionCompletedCore();
        }
        
        /// <summary>
        /// 工具执行完成的核心处理逻辑
        /// </summary>
        private void OnToolExecutionCompletedCore()
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
            try
            {
                var method = _control.GetType().GetMethod("SetCurrentNode");
                if (method != null)
                {
                    method.Invoke(_control, new object[] { node });
                    PluginLogger.Success(
                        $"已设置当前节点到调试控件: {_control.GetType().Name}", 
                        "DefaultDebugWindow");
                }
                else
                {
                    PluginLogger.Warning(
                        $"控件 {_control.GetType().Name} 不支持 SetCurrentNode", 
                        "DefaultDebugWindow");
                }
            }
            catch (Exception ex)
            {
                PluginLogger.Error(
                    $"设置当前节点失败: {ex.Message}", 
                    "DefaultDebugWindow", ex);
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
            try
            {
                var method = _control.GetType().GetMethod("SetDataProvider");
                if (method != null)
                {
                    method.Invoke(_control, new object[] { dataProvider });
                    PluginLogger.Success(
                        $"已将数据提供者注入到调试控件: {_control.GetType().Name}", 
                        "DefaultDebugWindow");
                }
                else
                {
                    PluginLogger.Warning(
                        $"控件 {_control.GetType().Name} 不支持 SetDataProvider", 
                        "DefaultDebugWindow");
                }
            }
            catch (Exception ex)
            {
                PluginLogger.Error(
                    $"设置数据提供者失败: {ex.Message}", 
                    "DefaultDebugWindow", ex);
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
                _continuousRunButton.IsEnabled = enabled;
            }
            
            if (_runButton.Visibility == Visibility.Visible)
            {
                _runButton.IsEnabled = enabled;
            }
            
            _confirmButton.IsEnabled = enabled;
        }

        /// <summary>
        /// 执行参数验证
        /// </summary>
        private bool ValidateParameters()
        {
            try
            {
                // 尝试调用控件的验证方法（如果有）
                var validateMethod = _control.GetType().GetMethod("ValidateParameters");
                if (validateMethod != null)
                {
                    var result = validateMethod.Invoke(_control, null);
                    if (result is bool isValid)
                    {
                        return isValid;
                    }
                }
                
                // 默认返回true
                return true;
            }
            catch (Exception ex)
            {
                PluginLogger.Error($"参数验证异常: {ex.Message}", "DefaultDebugWindow");
                return false;
            }
        }

        /// <summary>
        /// 窗口关闭事件处理
        /// </summary>
        private void OnWindowClosed(object? sender, EventArgs e)
        {
            // 停止连续运行
            _isContinuousRunning = false;
            
            // 清理事件绑定
            if (_control != null)
            {
                var executeEvent = _control.GetType().GetEvent("ExecuteRequested");
                if (executeEvent != null)
                {
                    try
                    {
                        var handler = Delegate.CreateDelegate(executeEvent.EventHandlerType, this, nameof(OnExecuteRequested));
                        executeEvent.RemoveEventHandler(_control, handler);
                    }
                    catch { }
                }

                var confirmEvent = _control.GetType().GetEvent("ConfirmClicked");
                if (confirmEvent != null)
                {
                    try
                    {
                        var handler = Delegate.CreateDelegate(confirmEvent.EventHandlerType, this, nameof(OnConfirmClicked));
                        confirmEvent.RemoveEventHandler(_control, handler);
                    }
                    catch { }
                }

                var completedEvent = _control.GetType().GetEvent("ToolExecutionCompleted");
                if (completedEvent != null)
                {
                    try
                    {
                        // 与订阅逻辑相同，使用反射创建正确的委托类型
                        var eventHandlerType = completedEvent.EventHandlerType;
                        
                        if (eventHandlerType.IsGenericType && 
                            eventHandlerType.GetGenericTypeDefinition() == typeof(EventHandler<>))
                        {
                            var resultType = eventHandlerType.GetGenericArguments()[0];
                            var handlerMethod = typeof(DefaultDebugWindow).GetMethod(
                                nameof(OnToolExecutionCompletedGeneric), 
                                BindingFlags.NonPublic | BindingFlags.Instance);
                            var genericHandler = handlerMethod!.MakeGenericMethod(resultType);
                            var handler = Delegate.CreateDelegate(eventHandlerType, this, genericHandler);
                            completedEvent.RemoveEventHandler(_control, handler);
                        }
                        else
                        {
                            var handler = Delegate.CreateDelegate(eventHandlerType, this, nameof(OnToolExecutionCompleted));
                            completedEvent.RemoveEventHandler(_control, handler);
                        }
                    }
                    catch { }
                }
            }

            PluginLogger.Info("窗口已关闭，事件已清理", "DefaultDebugWindow");
        }

        #endregion
    }
}
