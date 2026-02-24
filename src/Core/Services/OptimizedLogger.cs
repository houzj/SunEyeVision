using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using SunEyeVision.Core.Interfaces;

namespace SunEyeVision.Core.Services
{
    /// <summary>
    /// 优化的日志记录器 - 支持日志级别控制、采样日志、条件编�?
    /// </summary>
    public class OptimizedLogger : ILogger
    {
        private readonly LogLevel _minLevel;
        private readonly Dictionary<string, int> _eventCounters;
        private readonly int _sampleRate;
        private readonly object _lockObj;

        /// <summary>
        /// 当前日志级别
        /// </summary>
        public LogLevel CurrentLevel { get; set; }

        /// <summary>
        /// 是否启用日志（Release 模式可禁用）
        /// </summary>
        public static bool IsEnabled
        {
#if DEBUG
            get => true;
#else
            get => false;
#endif
        }

        public OptimizedLogger(LogLevel minLevel = LogLevel.Info, int sampleRate = 100)
        {
            _minLevel = minLevel;
            _sampleRate = sampleRate;
            _eventCounters = new Dictionary<string, int>();
            _lockObj = new object();
            CurrentLevel = minLevel;
        }

        public void LogDebug(string message)
        {
            if (!IsEnabled || LogLevel.Debug < _minLevel)
                return;

            WriteLog(LogLevel.Debug, message);
        }

        public void LogInfo(string message)
        {
            if (!IsEnabled || LogLevel.Info < _minLevel)
                return;

            WriteLog(LogLevel.Info, message);
        }

        public void LogWarning(string message)
        {
            if (!IsEnabled || LogLevel.Warning < _minLevel)
                return;

            WriteLog(LogLevel.Warning, message);
        }

        public void LogError(string message, Exception? exception = null)
        {
            if (!IsEnabled || LogLevel.Error < _minLevel)
                return;

            var sb = new StringBuilder();
            sb.Append(message);
            if (exception != null)
            {
                sb.AppendLine();
                sb.Append("Exception: ").Append(exception);
            }

            WriteLog(LogLevel.Error, sb.ToString());
        }

        public void LogFatal(string message, Exception? exception = null)
        {
            if (!IsEnabled || LogLevel.Fatal < _minLevel)
                return;

            var sb = new StringBuilder();
            sb.Append(message);
            if (exception != null)
            {
                sb.AppendLine();
                sb.Append("Exception: ").Append(exception);
            }

            WriteLog(LogLevel.Fatal, sb.ToString());
        }

        /// <summary>
        /// 记录采样日志 - 高频事件使用
        /// </summary>
        /// <param name="eventKey">事件标识�?/param>
        /// <param name="message">日志消息</param>
        public void LogSampled(string eventKey, string message)
        {
            if (!IsEnabled)
                return;

            lock (_lockObj)
            {
                _eventCounters.TryGetValue(eventKey, out int count);
                count++;

                if (count % _sampleRate == 0)
                {
                    _eventCounters[eventKey] = count;
                    WriteLog(LogLevel.Debug, $"{message} (采样: {count})");
                }
                else
                {
                    _eventCounters[eventKey] = count;
                }
            }
        }

        /// <summary>
        /// 记录带条件的日志
        /// </summary>
        /// <param name="level">日志级别</param>
        /// <param name="condition">条件</param>
        /// <param name="message">日志消息</param>
        public void LogIf(LogLevel level, bool condition, string message)
        {
            if (!IsEnabled || !condition || level < _minLevel)
                return;

            WriteLog(level, message);
        }

        /// <summary>
        /// 记录性能日志
        /// </summary>
        /// <param name="operation">操作名称</param>
        /// <param name="duration">持续时间（毫秒）</param>
        public void LogPerformance(string operation, long duration)
        {
            if (!IsEnabled || LogLevel.Info < _minLevel)
                return;

            WriteLog(LogLevel.Debug, $"[性能] {operation}: {duration}ms");
        }

        /// <summary>
        /// 记录带计时的操作
        /// </summary>
        /// <param name="operation">操作名称</param>
        /// <returns>可释放对象，释放时记录耗时</returns>
        public IDisposable LogTiming(string operation)
        {
            return new TimingScope(this, operation);
        }

        private void WriteLog(LogLevel level, string message)
        {
            var levelStr = level.ToString().ToUpper();
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        }

        /// <summary>
        /// 计时作用�?
        /// </summary>
        private class TimingScope : IDisposable
        {
            private readonly OptimizedLogger _logger;
            private readonly string _operation;
            private readonly Stopwatch _stopwatch;

            public TimingScope(OptimizedLogger logger, string operation)
            {
                _logger = logger;
                _operation = operation;
                _stopwatch = Stopwatch.StartNew();
            }

            public void Dispose()
            {
                _stopwatch.Stop();
                _logger.LogPerformance(_operation, _stopwatch.ElapsedMilliseconds);
            }
        }
    }
}
