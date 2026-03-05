using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Media.Imaging;

namespace SunEyeVision.UI.Services.Performance
{
    /// <summary>
    /// 预执行缓存 - 用于图像切换优化
    /// 支持LRU淘汰策略和过期清理
    /// </summary>
    public class PreExecutionCache : IDisposable
    {
        private readonly ConcurrentDictionary<string, CachedExecutionResult> _cache;
        private readonly int _maxCacheSize;
        private readonly int _expirationSeconds;
        private long _currentMemoryUsage;
        private readonly long _maxMemoryBytes;
        private bool _disposed;
        private Timer? _cleanupTimer;

        /// <summary>
        /// 创建预执行缓存
        /// </summary>
        /// <param name="maxCacheSize">最大缓存项数（默认50）</param>
        /// <param name="maxMemoryMB">最大内存使用（MB，默认100MB）</param>
        /// <param name="expirationSeconds">缓存过期时间（秒，默认300秒）</param>
        public PreExecutionCache(int maxCacheSize = 50, int maxMemoryMB = 100, int expirationSeconds = 300)
        {
            _cache = new ConcurrentDictionary<string, CachedExecutionResult>();
            _maxCacheSize = maxCacheSize;
            _maxMemoryBytes = maxMemoryMB * 1024L * 1024L;
            _expirationSeconds = expirationSeconds;
            _currentMemoryUsage = 0;

            // 启动定期清理（每60秒）
            _cleanupTimer = new Timer(CleanupExpired, null, TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(60));
        }

        /// <summary>
        /// 尝试获取缓存结果
        /// </summary>
        /// <param name="nodeId">节点ID</param>
        /// <param name="imageSourceId">图像源ID</param>
        /// <param name="result">缓存结果</param>
        /// <returns>是否命中缓存</returns>
        public bool TryGet(string nodeId, string imageSourceId, out CachedExecutionResult? result)
        {
            var key = $"{nodeId}_{imageSourceId}";

            if (_cache.TryGetValue(key, out var cached))
            {
                // 检查是否过期
                if (cached.IsExpired(_expirationSeconds))
                {
                    Remove(key);
                    result = null;
                    return false;
                }

                cached.Touch();
                result = cached;
                return true;
            }

            result = null;
            return false;
        }

        /// <summary>
        /// 添加或更新缓存
        /// </summary>
        /// <param name="nodeId">节点ID</param>
        /// <param name="imageSourceId">图像源ID</param>
        /// <param name="processedImage">处理后的图像</param>
        /// <param name="originalImage">原始图像（可选）</param>
        /// <param name="executionTimeMs">执行时间（毫秒）</param>
        public void Add(string nodeId, string imageSourceId, BitmapSource processedImage,
            BitmapSource? originalImage = null, long executionTimeMs = 0)
        {
            var key = $"{nodeId}_{imageSourceId}";

            var cached = new CachedExecutionResult
            {
                NodeId = nodeId,
                ImageSourceId = imageSourceId,
                ProcessedImage = processedImage,
                OriginalImage = originalImage,
                ExecutionTimeMs = executionTimeMs,
                CreatedTime = DateTime.Now,
                LastAccessTime = DateTime.Now
            };

            cached.CalculateSize();

            // 检查是否需要淘汰
            EnsureCapacity(cached.EstimatedSize);

            // 如果已存在，先移除旧的
            if (_cache.TryRemove(key, out var oldCached))
            {
                Interlocked.Add(ref _currentMemoryUsage, -oldCached.EstimatedSize);
            }

            // 添加新的
            _cache.TryAdd(key, cached);
            Interlocked.Add(ref _currentMemoryUsage, cached.EstimatedSize);
        }

        /// <summary>
        /// 移除缓存项
        /// </summary>
        /// <param name="nodeId">节点ID</param>
        /// <param name="imageSourceId">图像源ID</param>
        public void Remove(string nodeId, string imageSourceId)
        {
            var key = $"{nodeId}_{imageSourceId}";
            Remove(key);
        }

        private void Remove(string key)
        {
            if (_cache.TryRemove(key, out var cached))
            {
                Interlocked.Add(ref _currentMemoryUsage, -cached.EstimatedSize);
            }
        }

        /// <summary>
        /// 清空所有缓存
        /// </summary>
        public void Clear()
        {
            _cache.Clear();
            Interlocked.Exchange(ref _currentMemoryUsage, 0);
        }

        /// <summary>
        /// 获取缓存统计信息
        /// </summary>
        public (int Count, long MemoryBytes, double MemoryMB) GetStatistics()
        {
            return (_cache.Count, _currentMemoryUsage, _currentMemoryUsage / (1024.0 * 1024.0));
        }

        /// <summary>
        /// 确保容量充足（LRU淘汰）
        /// </summary>
        private void EnsureCapacity(long newItemSize)
        {
            // 检查数量限制
            while (_cache.Count >= _maxCacheSize)
            {
                EvictLRU();
            }

            // 检查内存限制
            while (_currentMemoryUsage + newItemSize > _maxMemoryBytes && _cache.Count > 0)
            {
                EvictLRU();
            }
        }

        /// <summary>
        /// 淘汰最久未使用的缓存项
        /// </summary>
        private void EvictLRU()
        {
            var lruItem = _cache.OrderBy(x => x.Value.LastAccessTime).FirstOrDefault();

            if (!string.IsNullOrEmpty(lruItem.Key))
            {
                Remove(lruItem.Key);
            }
        }

        /// <summary>
        /// 定期清理过期缓存
        /// </summary>
        private void CleanupExpired(object? state)
        {
            var expiredKeys = _cache
                .Where(x => x.Value.IsExpired(_expirationSeconds))
                .Select(x => x.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                Remove(key);
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _cleanupTimer?.Dispose();
            Clear();
        }
    }
}
