using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace SunEyeVision.UI.Controls.Rendering
{
    /// <summary>
    /// 智能缩略图加载器 - 简化版3层架构
    /// 
    /// 加载策略优先级：
    /// 1. L1内存缓存（0ms） - 强引用50张 + 弱引用
    /// 2. L2磁盘缓存（5-80ms） - Shell缓存优先 + 自建缓存补充
    /// 3. L3 GPU解码（50-500ms） - 最终回退方案
    /// 
    /// 优化说明：
    /// - 移除重复的Shell缓存调用（ThumbnailCacheManager内部已处理）
    /// - 统一缓存命中统计
    /// </summary>
    public class SmartThumbnailLoader : IDisposable
    {
        private readonly ThumbnailCacheManager _cacheManager;
        private readonly WicGpuDecoder _gpuDecoder;
        private readonly ConcurrentDictionary<string, byte[]> _prefetchCache;
        private bool _disposed;

        // 统计信息
        private int _cacheHits;
        private int _gpuHits;
        private int _misses;
        private long _totalLoadTimeMs;

        /// <summary>
        /// 获取统计信息
        /// </summary>
        public string GetStatistics()
        {
            var total = _cacheHits + _gpuHits + _misses;
            if (total == 0) return "无加载记录";

            var avgTime = total > 0 ? (double)_totalLoadTimeMs / total : 0;
            return $"缓存:{_cacheHits} GPU:{_gpuHits} 未命中:{_misses} 平均:{avgTime:F1}ms";
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        public SmartThumbnailLoader(
            ThumbnailCacheManager cacheManager,
            WicGpuDecoder gpuDecoder)
        {
            _cacheManager = cacheManager ?? throw new ArgumentNullException(nameof(cacheManager));
            _gpuDecoder = gpuDecoder ?? throw new ArgumentNullException(nameof(gpuDecoder));
            _prefetchCache = new ConcurrentDictionary<string, byte[]>();
        }

        /// <summary>
        /// 预读取文件数据（用于并行优化）
        /// </summary>
        public void PrefetchFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return;

            if (_prefetchCache.ContainsKey(filePath))
                return;

            try
            {
                Task.Run(() =>
                {
                    try
                    {
                        using var fs = new FileStream(
                            filePath,
                            FileMode.Open,
                            FileAccess.Read,
                            FileShare.Read,
                            bufferSize: 8192,
                            FileOptions.SequentialScan);

                        var buffer = new byte[fs.Length];
                        int bytesRead = fs.Read(buffer, 0, buffer.Length);
                        if (bytesRead != buffer.Length && buffer.Length > 0)
                        {
                            Array.Resize(ref buffer, bytesRead);
                        }

                        // 限制预读取缓存大小（最多保留10个文件）
                        if (_prefetchCache.Count > 10)
                        {
                            foreach (var key in _prefetchCache.Keys)
                            {
                                _prefetchCache.TryRemove(key, out _);
                                if (_prefetchCache.Count <= 10)
                                    break;
                            }
                        }

                        _prefetchCache.TryAdd(filePath, buffer);
                    }
                    catch { }
                });
            }
            catch { }
        }

        /// <summary>
        /// 智能加载缩略图（自动选择最快方式）
        /// </summary>
        public BitmapImage? LoadThumbnail(string filePath, int size)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                Debug.WriteLine("[SmartLoader] ⚠ 文件路径为空");
                return null;
            }
            
            if (!File.Exists(filePath))
            {
                Debug.WriteLine($"[SmartLoader] ⚠ 文件不存在: {filePath}");
                return null;
            }

            var sw = Stopwatch.StartNew();
            string method = "";
            BitmapImage? result = null;

            try
            {
                // ===== L1 + L2: 缓存查询（统一在ThumbnailCacheManager中处理）=====
                // 内部流程：L1a强引用 → L1b弱引用 → L2a Shell缓存 → L2b 自建磁盘缓存
                var cached = _cacheManager.TryLoadFromCache(filePath);
                if (cached != null)
                {
                    // ★ 关键诊断：检查缓存缩略图有效性
                    if (cached.Width > 0 && cached.Height > 0)
                    {
                        method = "缓存命中";
                        Interlocked.Increment(ref _cacheHits);
                        sw.Stop();
                        Interlocked.Add(ref _totalLoadTimeMs, sw.ElapsedMilliseconds);
                        // 缓存命中不输出日志（太多）
                        return cached;
                    }
                    else
                    {
                        Debug.WriteLine($"[SmartLoader] ⚠ 缓存缩略图无效 size={cached.Width}x{cached.Height} file={Path.GetFileName(filePath)}");
                    }
                }

                // ===== L3: GPU解码（最终回退方案）=====
                result = TryLoadFromGpu(filePath, size);
                if (result != null)
                {
                    // 检查GPU解码结果有效性
                    if (result.Width > 0 && result.Height > 0)
                    {
                        method = "GPU解码";
                        Interlocked.Increment(ref _gpuHits);
                        goto SUCCESS;
                    }
                    else
                    {
                        Debug.WriteLine($"[SmartLoader] ⚠ GPU解码结果无效 size={result.Width}x{result.Height} file={Path.GetFileName(filePath)}");
                        result = null;
                    }
                }

                // 所有策略都失败
                Interlocked.Increment(ref _misses);
                sw.Stop();
                Debug.WriteLine($"[SmartLoader] ✗ 所有策略失败 file={Path.GetFileName(filePath)}");
                return null;

            SUCCESS:
                // 添加到内存缓存（会自动保存到磁盘缓存）
                if (result != null)
                {
                    _cacheManager.AddToMemoryCache(filePath, result);
                }

                sw.Stop();
                Interlocked.Add(ref _totalLoadTimeMs, sw.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                sw.Stop();
                Debug.WriteLine($"[SmartLoader] ✗ 加载异常: {ex.Message} file={Path.GetFileName(filePath)}");
                Interlocked.Increment(ref _misses);
                return null;
            }
        }

        /// <summary>
        /// 异步加载缩略图
        /// </summary>
        public async Task<BitmapImage?> LoadThumbnailAsync(string filePath, int size, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() => LoadThumbnail(filePath, size), cancellationToken);
        }

        /// <summary>
        /// 尝试GPU解码（L3最终策略）
        /// </summary>
        private BitmapImage? TryLoadFromGpu(string filePath, int size)
        {
            try
            {
                byte[]? prefetchedData = null;
                _prefetchCache.TryRemove(filePath, out prefetchedData);

                var result = _gpuDecoder.DecodeThumbnail(filePath, size, prefetchedData);
                
                // 解码成功后异步保存到缓存（不阻塞显示）
                if (result != null)
                {
                    _cacheManager.SaveToCacheNonBlocking(filePath, result);
                }
                
                return result;  // 立即返回，不等待磁盘写入
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 批量加载缩略图（用于可视区域批量加载）
        /// </summary>
        public async Task<System.Collections.Generic.Dictionary<string, BitmapImage>> LoadThumbnailsBatchAsync(
            string[] filePaths,
            int size,
            IProgress<int>? progress = null,
            CancellationToken cancellationToken = default)
        {
            var results = new System.Collections.Generic.Dictionary<string, BitmapImage>();
            int completed = 0;

            await Task.Run(() =>
            {
                Parallel.ForEach(filePaths, new ParallelOptions
                {
                    MaxDegreeOfParallelism = Environment.ProcessorCount,
                    CancellationToken = cancellationToken
                }, filePath =>
                {
                    if (cancellationToken.IsCancellationRequested) return;
                    
                    var thumbnail = LoadThumbnail(filePath, size);
                    if (thumbnail != null)
                    {
                        lock (results)
                        {
                            results[filePath] = thumbnail;
                        }
                    }

                    var current = Interlocked.Increment(ref completed);
                    progress?.Report(current);
                });
            }, cancellationToken);

            return results;
        }

        /// <summary>
        /// 清除预读取缓存
        /// </summary>
        public void ClearPrefetchCache()
        {
            _prefetchCache.Clear();
        }

        /// <summary>
        /// 重置统计信息
        /// </summary>
        public void ResetStatistics()
        {
            _cacheHits = 0;
            _gpuHits = 0;
            _misses = 0;
            _totalLoadTimeMs = 0;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _prefetchCache.Clear();
                _disposed = true;
            }
        }
    }
}
