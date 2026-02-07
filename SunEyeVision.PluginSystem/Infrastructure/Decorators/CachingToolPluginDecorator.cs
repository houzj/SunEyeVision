using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using SunEyeVision.Interfaces;
using SunEyeVision.Models;
using SunEyeVision.PluginSystem.Base.Interfaces;
using SunEyeVision.PluginSystem.Base.Models;
using SunEyeVision.PluginSystem.Base.Base;
namespace SunEyeVision.PluginSystem.Decorators
{
    /// <summary>
    /// 缓存工具插件装饰器 - 为工具添加缓存功能
    /// </summary>
    public class CachingToolPluginDecorator : IToolPlugin
    {
        private readonly IToolPlugin _innerPlugin;
        private readonly ToolMetadata _toolMetadata;
        private readonly ConcurrentDictionary<string, CacheEntry> _cache;
        private readonly TimeSpan _cacheTtl;

        /// <summary>
        /// 缓存条目
        /// </summary>
        private class CacheEntry
        {
            public object Result { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        public string PluginId => _innerPlugin.PluginId;
        public string Name => $"Cached_{_innerPlugin.Name}";
        public string Version => _innerPlugin.Version;
        public string Author => _innerPlugin.Author;
        public string Description => $"Cached: {_innerPlugin.Description}";
        public string Icon => _innerPlugin.Icon;
        public List<string> Dependencies => _innerPlugin.Dependencies;
        public bool IsLoaded => _innerPlugin.IsLoaded;

        public CachingToolPluginDecorator(IToolPlugin innerPlugin, ToolMetadata toolMetadata)
        {
            _innerPlugin = innerPlugin ?? throw new ArgumentNullException(nameof(innerPlugin));
            _toolMetadata = toolMetadata ?? throw new ArgumentNullException(nameof(toolMetadata));
            _cache = new ConcurrentDictionary<string, CacheEntry>();
            _cacheTtl = TimeSpan.FromMilliseconds(toolMetadata.CacheTtlMs);
        }

        public void Initialize()
        {
            _innerPlugin.Initialize();
        }

        public void Unload()
        {
            _innerPlugin.Unload();
            _cache.Clear();
        }

        public List<Type> GetAlgorithmNodes()
        {
            return _innerPlugin.GetAlgorithmNodes();
        }

        /// <summary>
        /// 获取工具元数据列表
        /// </summary>
        public List<ToolMetadata> GetToolMetadata()
        {
            return _innerPlugin.GetToolMetadata();
        }

        /// <summary>
        /// 创建工具实例(带缓存)
        /// </summary>
        public IImageProcessor CreateToolInstance(string toolId)
        {
            var innerProcessor = _innerPlugin.CreateToolInstance(toolId);
            var metadata = _toolMetadata;

            if (!metadata.SupportCaching)
            {
                return innerProcessor;
            }

            return new CachingImageProcessor(innerProcessor, _cache, _cacheTtl, metadata);
        }

        /// <summary>
        /// 获取工具的默认参数
        /// </summary>
        public AlgorithmParameters GetDefaultParameters(string toolId)
        {
            return _innerPlugin.GetDefaultParameters(toolId);
        }

        /// <summary>
        /// 验证参数
        /// </summary>
        public ValidationResult ValidateParameters(string toolId, AlgorithmParameters parameters)
        {
            return _innerPlugin.ValidateParameters(toolId, parameters);
        }

        /// <summary>
        /// 清空缓存
        /// </summary>
        public void ClearCache()
        {
            _cache.Clear();
        }

        /// <summary>
        /// 获取缓存统计信息
        /// </summary>
        public (int totalCount, int expiredCount) GetCacheStatistics()
        {
            var now = DateTime.Now;
            var totalCount = _cache.Count;
            var expiredCount = 0;

            foreach (var entry in _cache.Values)
            {
                if (now - entry.CreatedAt > _cacheTtl)
                {
                    expiredCount++;
                }
            }

            return (totalCount, expiredCount);
        }

        /// <summary>
        /// 带缓存的图像处理器
        /// </summary>
        private class CachingImageProcessor : IImageProcessor
        {
            private readonly IImageProcessor _innerProcessor;
            private readonly ConcurrentDictionary<string, CacheEntry> _cache;
            private readonly TimeSpan _cacheTtl;
            private readonly ToolMetadata _metadata;

            public CachingImageProcessor(
                IImageProcessor innerProcessor,
                ConcurrentDictionary<string, CacheEntry> cache,
                TimeSpan cacheTtl,
                ToolMetadata metadata)
            {
                _innerProcessor = innerProcessor;
                _cache = cache;
                _cacheTtl = cacheTtl;
                _metadata = metadata;
            }

            public object? Process(object image)
            {
                // 计算缓存键(使用图像哈希作为键)
                var cacheKey = ComputeCacheKey(image);

                // 尝试从缓存获取
                if (_cache.TryGetValue(cacheKey, out var cachedEntry))
                {
                    // 检查缓存是否过期
                    if (DateTime.Now - cachedEntry.CreatedAt <= _cacheTtl)
                    {
                        // 缓存命中
                        return cachedEntry.Result;
                    }
                    else
                    {
                        // 缓存过期,移除
                        _cache.TryRemove(cacheKey, out _);
                    }
                }

                // 缓存未命中,执行实际处理
                var result = _innerProcessor.Process(image);

                // 缓存结果(如果支持缓存)
                if (_metadata.SupportCaching && result != null)
                {
                    var entry = new CacheEntry
                    {
                        Result = result,
                        CreatedAt = DateTime.Now
                    };
                    _cache.TryAdd(cacheKey, entry);
                }

                return result;
            }

            /// <summary>
            /// 计算缓存键
            /// </summary>
            private string ComputeCacheKey(object image)
            {
                // 简单实现: 使用对象哈希码和时间戳
                // 实际应用中应该使用图像内容的哈希值
                if (image == null)
                {
                    return "null_image";
                }

                var hash = image.GetHashCode();
                var timestamp = DateTime.Now.Minute; // 精确到分钟,避免过多缓存
                return $"{image.GetType().Name}_{hash}_{timestamp}";
            }
        }
    }
}
