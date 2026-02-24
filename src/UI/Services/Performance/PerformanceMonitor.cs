using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SunEyeVision.UI.Models;
using SunEyeVision.UI.Services.Performance;

namespace SunEyeVision.UI.Services.Performance
{
    /// <summary>
    /// 性能监控�?- 用于监控和记录系统性能指标
    /// </summary>
    public class PerformanceMonitor
    {
        private readonly Dictionary<string, PerformanceMetric> _metrics = new Dictionary<string, PerformanceMetric>();
        private readonly object _lockObj = new object();

        /// <summary>
        /// 开始测�?
        /// </summary>
        public IDisposable StartMeasure(string operationName)
        {
            return new PerformanceMeasurement(this, operationName);
        }

        /// <summary>
        /// 记录操作耗时
        /// </summary>
        public void RecordOperation(string operationName, double milliseconds)
        {
            lock (_lockObj)
            {
                if (!_metrics.ContainsKey(operationName))
                {
                    _metrics[operationName] = new PerformanceMetric
                    {
                        Name = operationName
                    };
                }

                var metric = _metrics[operationName];
                metric.Count++;
                metric.TotalMilliseconds += milliseconds;
                metric.MaxMilliseconds = Math.Max(metric.MaxMilliseconds, metric.MinMilliseconds == 0 ? milliseconds : metric.MaxMilliseconds);
                metric.MinMilliseconds = metric.MinMilliseconds == 0 ? milliseconds : Math.Min(metric.MinMilliseconds, milliseconds);

                // 更新平均�?
                metric.AverageMilliseconds = metric.TotalMilliseconds / metric.Count;
            }
        }

        /// <summary>
        /// 获取性能统计信息
        /// </summary>
        public PerformanceStatistics GetStatistics()
        {
            lock (_lockObj)
            {
                var stats = new PerformanceStatistics();
                stats.Timestamp = DateTime.Now;

                foreach (var kvp in _metrics)
                {
                    stats.Metrics.Add(kvp.Value);
                }

                return stats;
            }
        }

        /// <summary>
        /// 清除所有性能指标
        /// </summary>
        public void Clear()
        {
            lock (_lockObj)
            {
                _metrics.Clear();
            }
        }

        /// <summary>
        /// 打印性能统计信息到控制台
        /// </summary>
        public void PrintStatistics()
        {
            var stats = GetStatistics();

            foreach (var metric in stats.Metrics.OrderByDescending(m => m.TotalMilliseconds))
            {
            }
        }

        /// <summary>
        /// 性能测量辅助�?
        /// </summary>
        private class PerformanceMeasurement : IDisposable
        {
            private readonly PerformanceMonitor _monitor;
            private readonly string _operationName;
            private readonly Stopwatch _stopwatch;

            public PerformanceMeasurement(PerformanceMonitor monitor, string operationName)
            {
                _monitor = monitor;
                _operationName = operationName;
                _stopwatch = Stopwatch.StartNew();
            }

            public void Dispose()
            {
                _stopwatch.Stop();
                _monitor.RecordOperation(_operationName, _stopwatch.Elapsed.TotalMilliseconds);
            }
        }
    }

    /// <summary>
    /// 性能指标
    /// </summary>
    public class PerformanceMetric
    {
        public string Name { get; set; } = string.Empty;
        public int Count { get; set; }
        public double TotalMilliseconds { get; set; }
        public double AverageMilliseconds { get; set; }
        public double MinMilliseconds { get; set; }
        public double MaxMilliseconds { get; set; }
    }

    /// <summary>
    /// 性能统计信息
    /// </summary>
    public class PerformanceStatistics
    {
        public DateTime Timestamp { get; set; }
        public List<PerformanceMetric> Metrics { get; set; } = new List<PerformanceMetric>();
        public int TotalOperations => Metrics.Sum(m => m.Count);

        public override string ToString()
        {
            return $"性能统计 - 时间: {Timestamp:yyyy-MM-dd HH:mm:ss}, 操作�? {TotalOperations}";
        }
    }
}
