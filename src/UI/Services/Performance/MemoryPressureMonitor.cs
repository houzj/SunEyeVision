using System;
using System.Diagnostics;
using System.Threading;
using SunEyeVision.UI.Services.Performance;

namespace SunEyeVision.UI.Services.Performance
{
    /// <summary>
    /// 内存压力监控�?- 响应系统内存压力自动调整缓存策略
    /// 核心优化：避免OOM，智能降�?
    /// 
    /// 特点�?
    /// 1. 监控系统可用内存
    /// 2. 内存压力大时自动触发缓存清理
    /// 3. 支持多级压力响应（正常、中等、高、危险）
    /// 4. 轻量化实现，无外部依�?
    /// </summary>
    public class MemoryPressureMonitor : IDisposable
    {
        /// <summary>
        /// 内存压力级别
        /// </summary>
        public enum PressureLevel
        {
            Normal,      // > 30% 可用内存
            Moderate,    // 20-30% 可用内存
            High,        // 10-20% 可用内存
            Critical     // < 10% 可用内存
        }

        /// <summary>
        /// 内存压力变化事件参数
        /// </summary>
        public class MemoryPressureEventArgs : EventArgs
        {
            public PressureLevel Level { get; set; }
            public long AvailableMemoryMB { get; set; }
            public long TotalMemoryMB { get; set; }
            public double AvailablePercent { get; set; }
            public string RecommendedAction { get; set; } = string.Empty;
        }

        /// <summary>
        /// 内存压力变化事件
        /// </summary>
        public event EventHandler<MemoryPressureEventArgs>? MemoryPressureChanged;

        private Timer? _monitorTimer;
        private PressureLevel _currentLevel = PressureLevel.Normal;
        private readonly object _lock = new();
        private bool _disposed = false;

        // 配置参数
        private const int MONITOR_INTERVAL_MS = 2000; // 2秒检查一�?
        private const long MIN_MEMORY_MB = 100; // 最小保留内�?

        /// <summary>
        /// 当前内存压力级别
        /// </summary>
        public PressureLevel CurrentLevel => _currentLevel;

        /// <summary>
        /// 是否启用监控
        /// </summary>
        public bool IsEnabled { get; private set; }

        /// <summary>
        /// 开始监�?
        /// </summary>
        public void Start()
        {
            if (IsEnabled) return;

            IsEnabled = true;
            _monitorTimer = new Timer(CheckMemoryPressure, null, 
                MONITOR_INTERVAL_MS, MONITOR_INTERVAL_MS);

            Debug.WriteLine("[MemoryMonitor] �?内存压力监控已启�?);
        }

        /// <summary>
        /// 停止监控
        /// </summary>
        public void Stop()
        {
            if (!IsEnabled) return;

            IsEnabled = false;
            _monitorTimer?.Dispose();
            _monitorTimer = null;

            Debug.WriteLine("[MemoryMonitor] �?内存压力监控已停�?);
        }

        /// <summary>
        /// 检查内存压�?
        /// </summary>
        private void CheckMemoryPressure(object? state)
        {
            try
            {
                var (availableMB, totalMB, availablePercent) = GetMemoryInfo();
                var newLevel = CalculatePressureLevel(availablePercent);

                // 压力级别变化时触发事�?
                if (newLevel != _currentLevel)
                {
                    var oldLevel = _currentLevel;
                    _currentLevel = newLevel;

                    var args = new MemoryPressureEventArgs
                    {
                        Level = newLevel,
                        AvailableMemoryMB = availableMB,
                        TotalMemoryMB = totalMB,
                        AvailablePercent = availablePercent,
                        RecommendedAction = GetRecommendedAction(newLevel)
                    };

                    MemoryPressureChanged?.Invoke(this, args);

                    Debug.WriteLine($"[MemoryMonitor] �?内存压力变化: {oldLevel} -> {newLevel} " +
                        $"(可用:{availableMB}MB, {availablePercent:F1}%)");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MemoryMonitor] �?检查内存压力失�? {ex.Message}");
            }
        }

        /// <summary>
        /// 获取内存信息
        /// </summary>
        private (long availableMB, long totalMB, double availablePercent) GetMemoryInfo()
        {
            using var proc = Process.GetCurrentProcess();
            var workingSet = proc.WorkingSet64 / (1024 * 1024);

            // 使用GC获取可用内存信息
            var totalMemory = GC.GetTotalMemory(false) / (1024 * 1024);
            
            // 估算可用内存（简化实现，无性能计数器依赖）
            // 实际可用内存需要考虑系统状�?
            var availableMB = Math.Max(0, Environment.WorkingSet / (1024 * 1024));
            
            // 使用更可靠的方式估算
            GCMemoryInfo gcInfo = GC.GetGCMemoryInfo();
            long totalAvailableMemory = gcInfo.TotalAvailableMemoryBytes / (1024 * 1024);
            long memoryLoad = gcInfo.MemoryLoadBytes / (1024 * 1024);
            long estimatedAvailable = Math.Max(0, totalAvailableMemory - memoryLoad);

            double availablePercent = totalAvailableMemory > 0 
                ? (double)estimatedAvailable / totalAvailableMemory * 100 
                : 50;

            return (estimatedAvailable, totalAvailableMemory, availablePercent);
        }

        /// <summary>
        /// 计算压力级别（优化阈值，减少误触发）
        /// Normal: > 30% 
        /// Moderate: 20-30%
        /// High: 10-20%
        /// Critical: < 10%
        /// </summary>
        private PressureLevel CalculatePressureLevel(double availablePercent)
        {
            if (availablePercent > 30) return PressureLevel.Normal;
            if (availablePercent > 20) return PressureLevel.Moderate;
            if (availablePercent > 10) return PressureLevel.High;
            return PressureLevel.Critical;
        }

        /// <summary>
        /// 获取推荐操作
        /// </summary>
        private string GetRecommendedAction(PressureLevel level)
        {
            return level switch
            {
                PressureLevel.Normal => "正常加载",
                PressureLevel.Moderate => "减少预读取数�?,
                PressureLevel.High => "清理弱引用缓�?,
                PressureLevel.Critical => "强制GC，清空缓�?,
                _ => "未知"
            };
        }

        /// <summary>
        /// 手动触发内存检�?
        /// </summary>
        public MemoryPressureEventArgs CheckNow()
        {
            var (availableMB, totalMB, availablePercent) = GetMemoryInfo();
            var level = CalculatePressureLevel(availablePercent);

            return new MemoryPressureEventArgs
            {
                Level = level,
                AvailableMemoryMB = availableMB,
                TotalMemoryMB = totalMB,
                AvailablePercent = availablePercent,
                RecommendedAction = GetRecommendedAction(level)
            };
        }

        /// <summary>
        /// 响应内存压力（建议在压力变化时调用）
        /// </summary>
        public void RespondToPressure(PressureLevel level, Action? onHigh = null, Action? onCritical = null)
        {
            switch (level)
            {
                case PressureLevel.High:
                    // 高压力：清理弱引用缓存，减少预读�?
                    onHigh?.Invoke();
                    GC.Collect(GC.MaxGeneration, GCCollectionMode.Optimized);
                    Debug.WriteLine("[MemoryMonitor] �?高压力响应：执行清理");
                    break;

                case PressureLevel.Critical:
                    // 危险：强制GC，清空缓�?
                    onCritical?.Invoke();
                    GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                    Debug.WriteLine("[MemoryMonitor] �?危险响应：强制GC");
                    break;
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                Stop();
                _disposed = true;
            }
        }
    }
}
