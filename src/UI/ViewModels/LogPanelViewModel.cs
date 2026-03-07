using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using SunEyeVision.UI.Commands;
using SunEyeVision.UI.Services.Logging;
using SunEyeVision.UI.Adapters;
using SunEyeVision.Plugin.SDK.Logging;
using SunEyeVision.Core.Services.Logging;

namespace SunEyeVision.UI.ViewModels
{
    /// <summary>
    /// 日志面板视图模型
    /// 
    /// 特性：
    /// - 虚拟化渲染：支持万条日志流畅滚动
    /// - 循环缓冲区：O(1) 添加/移除操作
    /// - 增量统计：避免遍历计算
    /// - 过滤功能：按日志级别过滤
    /// - 搜索功能：按消息内容搜索
    /// </summary>
    public class LogPanelViewModel : ViewModelBase, IDisposable
    {
        #region 私有字段

        private readonly UILogWriter _uiLogWriter;
        private readonly VisionLogger _logger;
        private bool _autoScroll = true;
        private bool _showInfo = true;
        private bool _showSuccess = true;
        private bool _showWarning = true;
        private bool _showError = true;
        private string _searchText = string.Empty;
        private bool _isDisposed = false;

        #endregion

        #region 属性

        /// <summary>
        /// 日志条目集合（高性能循环缓冲区）
        /// </summary>
        public CircularBufferLogCollection LogEntries => _uiLogWriter.LogEntries;

        /// <summary>
        /// 日志条目的集合视图（用于过滤）
        /// </summary>
        public ICollectionView LogEntriesView { get; }

        /// <summary>
        /// 是否自动滚动到最新
        /// </summary>
        public bool AutoScroll
        {
            get => _autoScroll;
            set => SetProperty(ref _autoScroll, value);
        }

        /// <summary>
        /// 是否显示信息日志
        /// </summary>
        public bool ShowInfo
        {
            get => _showInfo;
            set
            {
                if (SetProperty(ref _showInfo, value))
                    RefreshFilter();
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
                if (SetProperty(ref _showSuccess, value))
                    RefreshFilter();
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
                if (SetProperty(ref _showWarning, value))
                    RefreshFilter();
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
                if (SetProperty(ref _showError, value))
                    RefreshFilter();
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
                if (SetProperty(ref _searchText, value))
                    RefreshFilter();
            }
        }

        /// <summary>
        /// 日志总数（O(1)访问）- UI显示的数量
        /// </summary>
        public int TotalCount => LogEntries.Count;

        /// <summary>
        /// 总入队数量（不受容量限制影响）
        /// </summary>
        public long TotalEnqueued => _uiLogWriter.TotalEnqueued;

        /// <summary>
        /// 错误数量（O(1)访问）
        /// </summary>
        public int ErrorCount => LogEntries.ErrorCount;

        /// <summary>
        /// 警告数量（O(1)访问）
        /// </summary>
        public int WarningCount => LogEntries.WarningCount;

        /// <summary>
        /// 统计信息
        /// </summary>
        public string Statistics
        {
            get
            {
                var (total, dropped, queueSize, writers) = _logger.GetStatistics();
                return $"已记录: {TotalEnqueued} | 显示: {TotalCount} | 丢弃: {dropped} | 队列: {queueSize}";
            }
        }

        /// <summary>
        /// 统计摘要（用于状态栏）
        /// </summary>
        public string StatisticsSummary =>
            $"已记录: {TotalEnqueued} | 显示: {TotalCount} | 错误: {ErrorCount} | 警告: {WarningCount}";

        #endregion

        #region 命令

        /// <summary>
        /// 清空日志命令
        /// </summary>
        public ICommand ClearCommand { get; }

        /// <summary>
        /// 切换自动滚动命令
        /// </summary>
        public ICommand ToggleAutoScrollCommand { get; }

        /// <summary>
        /// 导出日志命令
        /// </summary>
        public ICommand ExportCommand { get; }

        #endregion

        #region 事件

        /// <summary>
        /// 日志更新事件（用于触发 UI 滚动）
        /// </summary>
        public event EventHandler<LogBatchEventArgs>? LogsUpdated;

        #endregion

        #region 构造函数

        public LogPanelViewModel()
        {
            _logger = VisionLogger.Instance;
            // 使用预创建的 UILogWriter 实例（在应用启动时已初始化）
            // 确保日志从启动开始就能显示到UI
            _uiLogWriter = ServiceInitializer.UILogWriter;

            // 创建集合视图
            LogEntriesView = CollectionViewSource.GetDefaultView(LogEntries);
            LogEntriesView.Filter = FilterLogEntry;

            // 初始化命令
            ClearCommand = new RelayCommand(ClearLogs);
            ToggleAutoScrollCommand = new RelayCommand(() => AutoScroll = !AutoScroll);
            ExportCommand = new RelayCommand(ExportLogs);

            // 订阅日志事件
            _uiLogWriter.LogsAdded += OnLogsAdded;
            _uiLogWriter.LogsCleared += OnLogsCleared;
            _uiLogWriter.StatisticsChanged += OnStatisticsChanged;

            // 订阅集合变化
            LogEntries.CollectionChanged += OnLogEntriesCollectionChanged;
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 刷新过滤器
        /// </summary>
        public void RefreshFilter()
        {
            LogEntriesView.Refresh();
            OnPropertyChanged(nameof(Statistics));
        }

        /// <summary>
        /// 添加日志（通过核心日志器）
        /// </summary>
        public void AddLog(LogLevel level, string message, string? source = null)
        {
            _logger.Log(level, message, source);
        }

        /// <summary>
        /// 添加信息日志
        /// </summary>
        public void LogInfo(string message, string? source = null)
        {
            _logger.Log(LogLevel.Info, message, source);
        }

        /// <summary>
        /// 添加成功日志
        /// </summary>
        public void LogSuccess(string message, string? source = null)
        {
            _logger.Log(LogLevel.Success, message, source);
        }

        /// <summary>
        /// 添加警告日志
        /// </summary>
        public void LogWarning(string message, string? source = null)
        {
            _logger.Log(LogLevel.Warning, message, source);
        }

        /// <summary>
        /// 添加错误日志
        /// </summary>
        public void LogError(string message, string? source = null, Exception? exception = null)
        {
            _logger.Log(LogLevel.Error, message, source, exception);
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 日志过滤器
        /// </summary>
        private bool FilterLogEntry(object obj)
        {
            if (obj is not LogEntry entry)
                return false;

            // 级别过滤
            var levelVisible = entry.Level switch
            {
                LogLevel.Info => ShowInfo,
                LogLevel.Success => ShowSuccess,
                LogLevel.Warning => ShowWarning,
                LogLevel.Error => ShowError,
                _ => true
            };

            if (!levelVisible)
                return false;

            // 搜索过滤
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                return entry.Message.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                       (entry.Source?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false);
            }

            return true;
        }

        /// <summary>
        /// 处理日志批量添加事件
        /// </summary>
        private void OnLogsAdded(object? sender, LogBatchEventArgs e)
        {
            // 触发更新事件
            LogsUpdated?.Invoke(this, e);

            // 更新属性
            OnPropertyChanged(nameof(TotalCount));
            OnPropertyChanged(nameof(TotalEnqueued));
            OnPropertyChanged(nameof(ErrorCount));
            OnPropertyChanged(nameof(WarningCount));
            OnPropertyChanged(nameof(Statistics));
            OnPropertyChanged(nameof(StatisticsSummary));
        }

        /// <summary>
        /// 处理日志清空事件
        /// </summary>
        private void OnLogsCleared(object? sender, EventArgs e)
        {
            OnPropertyChanged(nameof(TotalCount));
            OnPropertyChanged(nameof(TotalEnqueued));
            OnPropertyChanged(nameof(ErrorCount));
            OnPropertyChanged(nameof(WarningCount));
            OnPropertyChanged(nameof(Statistics));
            OnPropertyChanged(nameof(StatisticsSummary));
        }

        /// <summary>
        /// 处理统计变更事件
        /// </summary>
        private void OnStatisticsChanged(object? sender, EventArgs e)
        {
            OnPropertyChanged(nameof(TotalCount));
            OnPropertyChanged(nameof(TotalEnqueued));
            OnPropertyChanged(nameof(ErrorCount));
            OnPropertyChanged(nameof(WarningCount));
            OnPropertyChanged(nameof(Statistics));
            OnPropertyChanged(nameof(StatisticsSummary));
        }

        /// <summary>
        /// 集合变化处理
        /// </summary>
        private void OnLogEntriesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            // 触发统计更新
            OnPropertyChanged(nameof(TotalCount));
        }

        /// <summary>
        /// 清空日志
        /// </summary>
        private void ClearLogs()
        {
            _uiLogWriter.Clear();
            OnPropertyChanged(nameof(Statistics));
            OnPropertyChanged(nameof(StatisticsSummary));
        }

        /// <summary>
        /// 导出日志
        /// </summary>
        private void ExportLogs()
        {
            // TODO: 实现日志导出功能
            // 可以导出为文本文件或 CSV
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            _uiLogWriter.LogsAdded -= OnLogsAdded;
            _uiLogWriter.LogsCleared -= OnLogsCleared;
            _uiLogWriter.StatisticsChanged -= OnStatisticsChanged;
            LogEntries.CollectionChanged -= OnLogEntriesCollectionChanged;

            // 注意：不 Dispose _uiLogWriter，因为它是全局共享实例（由 ServiceInitializer 管理）
            // 每个 LogPanelViewModel 实例只是取消订阅事件

            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
