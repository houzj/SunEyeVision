using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;
using SunEyeVision.UI.Services.Logging;
using SunEyeVision.UI.ViewModels;
using SunEyeVision.Plugin.SDK.Logging;

namespace SunEyeVision.UI.Views.Controls.Panels
{
    // 使用别名解决 PropertyItem 歧义
    using Models;
    using ModelsPropertyItem = SunEyeVision.UI.Models.PropertyItem;
    using ModelsPropertyGroup = SunEyeVision.UI.Models.PropertyGroup;
    /// <summary>
    /// PropertyPanelControl.xaml 的交互逻辑
    /// 高性能日志面板集成
    /// </summary>
    public partial class PropertyPanelControl : UserControl, INotifyPropertyChanged
    {
        #region 私有字段

        private bool _autoScroll = true;
        private bool _showInfo = true;
        private bool _showSuccess = true;
        private bool _showWarning = true;
        private bool _showError = true;
        private string _searchText = string.Empty;
        private readonly LogPanelViewModel _logViewModel;
        private readonly DispatcherTimer _scrollTimer;
        private DateTime _lastScrollTime = DateTime.MinValue;
        private readonly TimeSpan _scrollThrottleInterval = TimeSpan.FromMilliseconds(200);

        #endregion

        #region 依赖属性

        public static readonly DependencyProperty PropertyGroupsProperty =
            DependencyProperty.Register("PropertyGroups", typeof(ObservableCollection<ModelsPropertyGroup>), typeof(PropertyPanelControl),
                new PropertyMetadata(new ObservableCollection<ModelsPropertyGroup>(), OnPropertyGroupsChanged));

        public static readonly DependencyProperty SelectedNodeProperty =
            DependencyProperty.Register("SelectedNode", typeof(WorkflowNode), typeof(PropertyPanelControl),
                new PropertyMetadata(null, OnSelectedNodeChanged));

        // 保留旧的 LogText 属性以保持向后兼容
        public static readonly DependencyProperty LogTextProperty =
            DependencyProperty.Register("LogText", typeof(string), typeof(PropertyPanelControl),
                new PropertyMetadata("", OnLogTextChanged));

        public static readonly DependencyProperty ClearLogCommandProperty =
            DependencyProperty.Register("ClearLogCommand", typeof(System.Windows.Input.ICommand), typeof(PropertyPanelControl),
                new PropertyMetadata(null));

        public static readonly DependencyProperty CopyCommandProperty =
            DependencyProperty.Register("CopyCommand", typeof(System.Windows.Input.ICommand), typeof(PropertyPanelControl),
                new PropertyMetadata(null));

        public static readonly DependencyProperty CopyAllCommandProperty =
            DependencyProperty.Register("CopyAllCommand", typeof(System.Windows.Input.ICommand), typeof(PropertyPanelControl),
                new PropertyMetadata(null));

        public static readonly DependencyProperty TestCommandProperty =
            DependencyProperty.Register("TestCommand", typeof(System.Windows.Input.ICommand), typeof(PropertyPanelControl),
                new PropertyMetadata(null));

        public static readonly DependencyProperty TestClearCommandProperty =
            DependencyProperty.Register("TestClearCommand", typeof(System.Windows.Input.ICommand), typeof(PropertyPanelControl),
                new PropertyMetadata(null));

        #endregion

        #region 属性

        public ObservableCollection<ModelsPropertyGroup> PropertyGroups
        {
            get => (ObservableCollection<ModelsPropertyGroup>)GetValue(PropertyGroupsProperty);
            set => SetValue(PropertyGroupsProperty, value);
        }

        public WorkflowNode SelectedNode
        {
            get => (WorkflowNode)GetValue(SelectedNodeProperty);
            set => SetValue(SelectedNodeProperty, value);
        }

        public string LogText
        {
            get => (string)GetValue(LogTextProperty);
            set => SetValue(LogTextProperty, value);
        }

        public System.Windows.Input.ICommand ClearLogCommand
        {
            get => (System.Windows.Input.ICommand)GetValue(ClearLogCommandProperty);
            set => SetValue(ClearLogCommandProperty, value);
        }

        public System.Windows.Input.ICommand CopyCommand
        {
            get => (System.Windows.Input.ICommand)GetValue(CopyCommandProperty);
            set => SetValue(CopyCommandProperty, value);
        }

        public System.Windows.Input.ICommand CopyAllCommand
        {
            get => (System.Windows.Input.ICommand)GetValue(CopyAllCommandProperty);
            set => SetValue(CopyAllCommandProperty, value);
        }

        public System.Windows.Input.ICommand TestCommand
        {
            get => (System.Windows.Input.ICommand)GetValue(TestCommandProperty);
            set => SetValue(TestCommandProperty, value);
        }

        public System.Windows.Input.ICommand TestClearCommand
        {
            get => (System.Windows.Input.ICommand)GetValue(TestClearCommandProperty);
            set => SetValue(TestClearCommandProperty, value);
        }

        // ========== 高性能日志面板属性 ==========

        /// <summary>
        /// 日志条目集合视图
        /// </summary>
        public ICollectionView LogEntriesView => _logViewModel.LogEntriesView;

        /// <summary>
        /// 是否自动滚动
        /// </summary>
        public bool AutoScroll
        {
            get => _autoScroll;
            set
            {
                if (_autoScroll != value)
                {
                    _autoScroll = value;
                    OnPropertyChanged(nameof(AutoScroll));
                }
            }
        }

        /// <summary>
        /// 是否显示信息日志
        /// </summary>
        public bool ShowInfo
        {
            get => _showInfo;
            set
            {
                if (_showInfo != value)
                {
                    _showInfo = value;
                    _logViewModel.ShowInfo = value;
                    OnPropertyChanged(nameof(ShowInfo));
                }
            }
        }

        /// <summary>
        /// 是否显示成功日志
        /// </summary>
        public bool ShowSuccess
        {
            get => _showSuccess;
            set
            {
                if (_showSuccess != value)
                {
                    _showSuccess = value;
                    _logViewModel.ShowSuccess = value;
                    OnPropertyChanged(nameof(ShowSuccess));
                }
            }
        }

        /// <summary>
        /// 是否显示警告日志
        /// </summary>
        public bool ShowWarning
        {
            get => _showWarning;
            set
            {
                if (_showWarning != value)
                {
                    _showWarning = value;
                    _logViewModel.ShowWarning = value;
                    OnPropertyChanged(nameof(ShowWarning));
                }
            }
        }

        /// <summary>
        /// 是否显示错误日志
        /// </summary>
        public bool ShowError
        {
            get => _showError;
            set
            {
                if (_showError != value)
                {
                    _showError = value;
                    _logViewModel.ShowError = value;
                    OnPropertyChanged(nameof(ShowError));
                }
            }
        }

        /// <summary>
        /// 搜索文本
        /// </summary>
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    _logViewModel.SearchText = value;
                    OnPropertyChanged(nameof(SearchText));
                }
            }
        }

        /// <summary>
        /// 日志总数（UI显示数量）
        /// </summary>
        public int TotalCount => _logViewModel.TotalCount;

        /// <summary>
        /// 总入队数量（不受容量限制影响）
        /// </summary>
        public long TotalEnqueued => _logViewModel.TotalEnqueued;

        /// <summary>
        /// 错误数量
        /// </summary>
        public int ErrorCount => _logViewModel.ErrorCount;

        /// <summary>
        /// 警告数量
        /// </summary>
        public int WarningCount => _logViewModel.WarningCount;

        /// <summary>
        /// 统计信息
        /// </summary>
        public string Statistics => _logViewModel.Statistics;

        /// <summary>
        /// 统计摘要
        /// </summary>
        public string StatisticsSummary => _logViewModel.StatisticsSummary;

        #endregion

        #region 构造函数

        public PropertyPanelControl()
        {
            InitializeComponent();
            PropertyGroups = new ObservableCollection<ModelsPropertyGroup>();

            try
            {
                // 初始化高性能日志 ViewModel
                _logViewModel = new LogPanelViewModel();
                
                // 订阅日志更新事件（添加异常处理）
                if (_logViewModel != null)
                {
                    try
                    {
                        _logViewModel.LogsUpdated += OnLogsUpdated;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"订阅 LogsUpdated 事件失败: {ex.Message}");
                        _logViewModel.LogError($"订阅 LogsUpdated 事件失败: {ex.Message}");
                    }
                }

                // 初始化滚动定时器（节流控制）
                _scrollTimer = new DispatcherTimer
                {
                    Interval = _scrollThrottleInterval
                };
                
                // 订阅定时器事件（添加异常处理）
                if (_scrollTimer != null)
                {
                    try
                    {
                        _scrollTimer.Tick += OnScrollTimerTick;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"订阅 Tick 事件失败: {ex.Message}");
                        _logViewModel?.LogError($"订阅 Tick 事件失败: {ex.Message}");
                    }
                }

                // 初始化命令
                ClearLogCommand = _logViewModel?.ClearCommand;
                CopyCommand = new RelayCommand(() => CopySelectedLogsFromDataGrid());
                CopyAllCommand = _logViewModel?.CopyAllCommand;
                TestCommand = new RelayCommand(() => TestButton());
                TestClearCommand = new RelayCommand(() => ClearLogsDirect());

                // 直接设置按钮命令，绕过 XAML 绑定问题（添加空引用检查）
                if (TestBtn != null) TestBtn.Command = TestCommand;
                if (TestClearBtn != null) TestClearBtn.Command = TestClearCommand;
                if (CopyBtn != null) CopyBtn.Command = CopyCommand;
                if (CopyAllBtn != null) CopyAllBtn.Command = CopyAllCommand;
                if (ClearLogBtn != null) ClearLogBtn.Command = ClearLogCommand;

                // 同步过滤设置
                if (_logViewModel != null)
                {
                    _logViewModel.ShowInfo = _showInfo;
                    _logViewModel.ShowSuccess = _showSuccess;
                    _logViewModel.ShowWarning = _showWarning;
                    _logViewModel.ShowError = _showError;

                    // 验证命令绑定
                    _logViewModel.LogInfo("属性面板控件初始化完成，命令已绑定");
                }
            }
            catch (Exception ex)
            {
                _logViewModel?.LogError($"属性面板控件初始化失败: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"PropertyPanelControl 初始化异常: {ex}");
            }
        }

        #endregion

        #region 私有方法

        private static void OnPropertyGroupsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PropertyPanelControl control)
            {
                control.UpdateNoSelectionTextVisibility();
            }
        }

        private static void OnSelectedNodeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PropertyPanelControl control)
            {
                control.LoadNodeProperties(e.NewValue as WorkflowNode);
            }
        }

        private static void OnLogTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // 保持向后兼容：如果外部设置了 LogText，添加到日志系统
            if (d is PropertyPanelControl control && !string.IsNullOrEmpty(e.NewValue as string))
            {
                control.AddLogEntry(e.NewValue as string);
            }
        }

        private void UpdateNoSelectionTextVisibility()
        {
            NoSelectionText.Visibility = PropertyGroups.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void LoadNodeProperties(WorkflowNode node)
        {
            PropertyGroups.Clear();

            if (node == null)
            {
                UpdateNoSelectionTextVisibility();
                return;
            }

            // 基本信息
            var basicGroup = new ModelsPropertyGroup
            {
                Name = "📋 基本信息",
                IsExpanded = true,
                Parameters = new ObservableCollection<ModelsPropertyItem>
                {
                    new ModelsPropertyItem { Label = "名称:", Value = node.Name },
                    new ModelsPropertyItem { Label = "ID:", Value = node.Id },
                    new ModelsPropertyItem { Label = "类型:", Value = node.ToolType }
                }
            };

            PropertyGroups.Add(basicGroup);

            // 参数配置
            if (node.Parameters != null)
            {
                var runtimeMetadata = node.Parameters.GetRuntimeParameterMetadata();
                if (runtimeMetadata.Count > 0)
                {
                    var paramGroup = new ModelsPropertyGroup
                    {
                        Name = "🔧 参数配置",
                        IsExpanded = true,
                        Parameters = new ObservableCollection<ModelsPropertyItem>()
                    };

                    foreach (var meta in runtimeMetadata)
                    {
                        paramGroup.Parameters.Add(new ModelsPropertyItem
                        {
                            Label = $"{meta.DisplayName}:",
                            Value = meta.Value?.ToString() ?? ""
                        });
                    }

                    PropertyGroups.Add(paramGroup);
                }
            }

            // 性能统计
            var perfGroup = new ModelsPropertyGroup
            {
                Name = "📊 性能统计",
                IsExpanded = true,
                Parameters = new ObservableCollection<ModelsPropertyItem>
                {
                    new ModelsPropertyItem { Label = "状态:", Value = node.Status }
                }
            };

            PropertyGroups.Add(perfGroup);

            UpdateNoSelectionTextVisibility();
        }

        /// <summary>
        /// 日志更新事件处理 - 自动滚动
        /// </summary>
        private void OnLogsUpdated(object? sender, LogBatchEventArgs e)
        {
            // 节流控制：使用定时器限制滚动频率
            if (AutoScroll && LogDataGrid != null)
            {
                // 检查时间间隔，避免频繁滚动
                var now = DateTime.UtcNow;
                if ((now - _lastScrollTime).TotalMilliseconds >= _scrollThrottleInterval.TotalMilliseconds)
                {
                    _lastScrollTime = now;

                    if (!_scrollTimer.IsEnabled)
                    {
                        _scrollTimer.Start();
                    }
                }
            }

            // 更新统计属性
            OnPropertyChanged(nameof(TotalCount));
            OnPropertyChanged(nameof(TotalEnqueued));
            OnPropertyChanged(nameof(ErrorCount));
            OnPropertyChanged(nameof(WarningCount));
            OnPropertyChanged(nameof(Statistics));
            OnPropertyChanged(nameof(StatisticsSummary));
        }

        /// <summary>
        /// 滚动定时器事件处理
        /// </summary>
        private void OnScrollTimerTick(object? sender, EventArgs e)
        {
            _scrollTimer.Stop();

            try
            {
                if (AutoScroll && LogDataGrid != null && LogDataGrid.Items.Count > 0)
                {
                    var lastItem = LogDataGrid.Items[^1];
                    LogDataGrid.ScrollIntoView(lastItem);
                }
            }
            catch (ArgumentOutOfRangeException)
            {
                // 集合可能正在更新，忽略此异常
            }
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 添加日志条目（高性能）
        /// </summary>
        public void AddLogEntry(string message)
        {
            _logViewModel.LogInfo(message);
        }

        /// <summary>
        /// 添加带级别的日志
        /// </summary>
        public void AddLog(LogLevel level, string message, string? source = null)
        {
            _logViewModel.AddLog(level, message, source);
        }

        /// <summary>
        /// 添加信息日志
        /// </summary>
        public void LogInfo(string message, string? source = null)
            => _logViewModel.LogInfo(message, source);

        /// <summary>
        /// 添加成功日志
        /// </summary>
        public void LogSuccess(string message, string? source = null)
            => _logViewModel.LogSuccess(message, source);

        /// <summary>
        /// 添加警告日志
        /// </summary>
        public void LogWarning(string message, string? source = null)
            => _logViewModel.LogWarning(message, source);

        /// <summary>
        /// 添加错误日志
        /// </summary>
        public void LogError(string message, string? source = null)
            => _logViewModel.LogError(message, source);

        #endregion

        #region 事件处理

        private void LogDataGrid_Loaded(object sender, RoutedEventArgs e)
        {
            // 初始滚动到底部
            if (LogDataGrid.Items.Count > 0)
            {
                LogDataGrid.ScrollIntoView(LogDataGrid.Items[LogDataGrid.Items.Count - 1]);
            }
        }

        private void LogDataGrid_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            // 检测是否滚动到底部
            if (e.VerticalOffset + e.ViewportHeight >= e.ExtentHeight - 1)
            {
                AutoScroll = true;
            }
            else
            {
                // 用户手动向上滚动时禁用自动滚动
                if (e.VerticalChange < 0)
                {
                    AutoScroll = false;
                }
            }
        }

        private void CopyMessage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (LogDataGrid?.SelectedItem is LogEntry entry && !string.IsNullOrEmpty(entry.Message))
                {
                    Clipboard.SetText(entry.Message);
                    _logViewModel?.LogSuccess("已复制消息到剪贴板");
                }
                else
                {
                    _logViewModel?.LogWarning("未选中任何日志条目或消息为空");
                }
            }
            catch (Exception ex)
            {
                _logViewModel?.LogError($"复制消息失败: {ex.Message}");
            }
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 空实现，预留扩展
        }

        /// <summary>
        /// 测试按钮方法
        /// </summary>
        private void TestButton()
        {
            _logViewModel.LogSuccess("测试按钮被点击了！");
            _logViewModel.LogInfo($"当前日志数量: {_logViewModel.LogEntries.Count}");
        }

        /// <summary>
        /// 复制选中的日志（从 DataGrid 获取选中项）
        /// </summary>
        private void CopySelectedLogsFromDataGrid()
        {
            CopySelectedLogsFromDataGrid_Click(null, null);
        }

        /// <summary>
        /// 复制选中的日志（从 DataGrid 获取选中项）- Click 事件处理
        /// </summary>
        private void CopySelectedLogsFromDataGrid_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 空引用检查
                if (LogDataGrid == null)
                {
                    _logViewModel.LogError("LogDataGrid 未初始化");
                    return;
                }

                if (LogDataGrid.SelectedItems == null || LogDataGrid.SelectedItems.Count == 0)
                {
                    _logViewModel.LogWarning("未选中任何日志条目");
                    return;
                }

                var selectedItems = LogDataGrid.SelectedItems.Cast<LogEntry>().Where(entry => entry != null).ToList();
                if (selectedItems.Count == 0)
                {
                    _logViewModel.LogWarning("选中的日志条目无效");
                    return;
                }

                _logViewModel.LogInfo($"DataGrid 选中: {selectedItems.Count} 项");

                var logText = string.Join(Environment.NewLine, selectedItems.Select(entry =>
                    $"[{entry.FormattedTime}] [{entry.Level}] {entry.DisplayMessage}"));

                if (_logViewModel?.CopyToClipboardInternal(logText) == true)
                {
                    _logViewModel.LogSuccess($"已复制 {selectedItems.Count} 条日志到剪贴板");
                }
            }
            catch (NullReferenceException ex)
            {
                _logViewModel?.LogError($"空引用异常: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"CopySelectedLogsFromDataGrid 空引用: {ex}");
            }
            catch (Exception ex)
            {
                _logViewModel?.LogError($"复制失败: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"CopySelectedLogsFromDataGrid 异常: {ex}");
            }
        }

        /// <summary>
        /// 直接清空方法
        /// </summary>
        private void ClearLogsDirect()
        {
        }

        /// <summary>
        /// 清空日志 - Click 事件处理
        /// </summary>
        private void ClearLogs_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _logViewModel?.ClearCommand?.Execute(null);
            }
            catch (Exception ex)
            {
                _logViewModel?.LogError($"清空日志失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 全选 - Click 事件处理
        /// </summary>
        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (LogDataGrid != null)
                {
                    LogDataGrid.SelectAll();
                }
            }
            catch (Exception ex)
            {
                _logViewModel?.LogError($"全选失败: {ex.Message}");
            }
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
