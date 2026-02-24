using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SunEyeVision.UI.Services.Thumbnail.Caching;

namespace SunEyeVision.UI.Services.Thumbnail.Caching
{
    /// <summary>
    /// 弱引用缓�?- L2缓存�?
    /// 核心优化：避免内存溢出，支持GC自动回收
    /// 
    /// 特点�?
    /// 1. 使用弱引用存储，GC可自动回�?
    /// 2. 线程安全的ConcurrentDictionary
    /// 3. 自动清理死亡的引�?
    /// 4. 缓存命中统计
    /// </summary>
    /// <typeparam name="TKey">键类�?/typeparam>
    /// <typeparam name="TValue">值类型（必须是引用类型）</typeparam>
    public class WeakReferenceCache<TKey, TValue> where TValue : class
    {
        private readonly ConcurrentDictionary<TKey, WeakReference<TValue>> _cache = new();
        private readonly object _cleanupLock = new();
        private int _cleanupCounter = 0;
        private const int CLEANUP_INTERVAL = 100; // �?00次操作清理一次死引用

        /// <summary>
        /// 缓存统计信息
        /// </summary>
        public class CacheStatistics
        {
            public int TotalRequests { get; set; }
            public int CacheHits { get; set; }
            public int CacheMisses { get; set; }
            public int WeakRefRevived { get; set; } // 弱引用复活次�?
            public double HitRate => TotalRequests > 0 ? (double)CacheHits / TotalRequests * 100 : 0;
        }

        private readonly CacheStatistics _statistics = new();
        public CacheStatistics Statistics => _statistics;

        /// <summary>
        /// 尝试从缓存获取�?
        /// </summary>
        public bool TryGet(TKey key, out TValue? value)
        {
            _statistics.TotalRequests++;

            if (_cache.TryGetValue(key, out var weakRef))
            {
                if (weakRef.TryGetTarget(out var target) && target != null)
                {
                    value = target;
                    _statistics.CacheHits++;
                    _statistics.WeakRefRevived++;
                    return true;
                }
                else
                {
                    // 弱引用已死，移除条目
                    _cache.TryRemove(key, out _);
                    _statistics.CacheMisses++;
                }
            }
            else
            {
                _statistics.CacheMisses++;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// 添加到缓�?
        /// </summary>
        public void Add(TKey key, TValue value)
        {
            if (value == null) return;

            _cache[key] = new WeakReference<TValue>(value);

            // 定期清理死引�?
            if (Interlocked.Increment(ref _cleanupCounter) >= CLEANUP_INTERVAL)
            {
                CleanupDeadReferences();
                _cleanupCounter = 0;
            }
        }

        /// <summary>
        /// 移除缓存�?
        /// </summary>
        public bool Remove(TKey key)
        {
            return _cache.TryRemove(key, out _);
        }

        /// <summary>
        /// 检查是否包含键
        /// </summary>
        public bool ContainsKey(TKey key)
        {
            if (_cache.TryGetValue(key, out var weakRef))
            {
                return weakRef.TryGetTarget(out var target) && target != null;
            }
            return false;
        }

        /// <summary>
        /// 清理已死亡的弱引�?
        /// </summary>
        private void CleanupDeadReferences()
        {
            lock (_cleanupLock)
            {
                var deadKeys = new List<TKey>();

                foreach (var kvp in _cache)
                {
                    if (!kvp.Value.TryGetTarget(out var target) || target == null)
                    {
                        deadKeys.Add(kvp.Key);
                    }
                }

                foreach (var key in deadKeys)
                {
                    _cache.TryRemove(key, out _);
                }

                if (deadKeys.Count > 0)
                {
                    Debug.WriteLine($"[WeakReferenceCache] �?已清�?{deadKeys.Count} 个死引用，剩�? {_cache.Count}");
                }
            }
        }

        /// <summary>
        /// 获取存活的缓存项数量
        /// </summary>
        public int AliveCount
        {
            get
            {
                int count = 0;
                foreach (var kvp in _cache)
                {
                    if (kvp.Value.TryGetTarget(out var target) && target != null)
                    {
                        count++;
                    }
                }
                return count;
            }
        }

        /// <summary>
        /// 获取总条目数（包括死亡引用）
        /// </summary>
        public int TotalCount => _cache.Count;

        /// <summary>
        /// 清空缓存
        /// </summary>
        public void Clear()
        {
            _cache.Clear();
            _statistics.TotalRequests = 0;
            _statistics.CacheHits = 0;
            _statistics.CacheMisses = 0;
            _statistics.WeakRefRevived = 0;
        }

        /// <summary>
        /// 获取缓存信息
        /// </summary>
        public string GetCacheInfo()
        {
            return $"弱引用缓�? 总条�?{TotalCount}, 存活:{AliveCount}, 命中�?{_statistics.HitRate:F1}%, 复活:{_statistics.WeakRefRevived}";
        }
    }
}
