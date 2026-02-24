using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using SunEyeVision.Core.IO;
using SunEyeVision.UI.Services.Thumbnail;
using SunEyeVision.UI.Services.Thumbnail.Decoders;

namespace SunEyeVision.UI.Services.Thumbnail
{
    /// <summary>
    /// 智能缩略图加载器 - 简化版4层架�?
    /// 
    /// 加载策略优先级：
    /// 1. L1内存缓存�?ms�?- 强引�?0�?+ 弱引�?
    /// 2. L2磁盘缓存�?-80ms�?- Shell缓存优先 + 自建缓存补充
    /// 3. L3 解码器解码（30-150ms�?- GPU或CPU解码
    /// 4. L4原图解码�?00-800ms�?- 最终回退方案
    /// 
    /// 优化说明�?
    /// - 移除重复的Shell缓存调用（ThumbnailCacheManager内部已处理）
    /// - 统一缓存命中统计
    /// - �?支持多种解码器（IThumbnailDecoder接口�?
    /// - �?方案二优化：高优先级任务使用GPU解码器，普通任务使用CPU解码�?
    /// - �?文件生命周期管理：通过 FileAccessManager 防止竞态条�?
    /// </summary>
    public class SmartThumbnailLoader : IDisposable
    {
        private readonly ThumbnailCacheManager _cacheManager;
        private readonly IThumbnailDecoder _gpuDecoder;  // �?GPU解码器（高优先级任务�?
        private readonly IThumbnailDecoder _cpuDecoder;  // �?CPU解码器（普通任务）
        private readonly IFileAccessManager? _fileAccessManager; // �?文件访问管理�?
        private readonly ConcurrentDictionary<string, byte[]> _prefetchCache;
        private bool _disposed;

        // 统计信息
        private int _cacheHits;
        private int _gpuHits;
        private int _originalHits; // �?P1优化：新增原图加载统�?
        private int _misses;
        private long _totalLoadTimeMs;
        
        // �?日志优化：首张图片追踪（用于诊断日志�?
        private static int _loadCounter = 0;
        private const int FIRST_IMAGE_LOG_COUNT = 3; // �?张图片输出详细日�?

        /// <summary>
        /// 获取统计信息
        /// </summary>
        public string GetStatistics()
        {
            var total = _cacheHits + _gpuHits + _originalHits + _misses;
            if (total == 0) return "No load records";

            var avgTime = total > 0 ? (double)_totalLoadTimeMs / total : 0;
            return $"Cache:{_cacheHits} GPU:{_gpuHits} Original:{_originalHits} Miss:{_misses} Avg:{avgTime:F1}ms";
        }

        /// <summary>
        /// �?日志优化：重置加载计数器（新文件夹加载时调用�?
        /// </summary>
        public static void ResetLoadCounter()
        {
            Interlocked.Exchange(ref _loadCounter, 0);
        }

        /// <summary>
        /// 构造函�?- 方案二：双解码器架构
        /// 高优先级任务使用GPU解码器，普通任务使用CPU解码�?
        /// </summary>
        public SmartThumbnailLoader(
            ThumbnailCacheManager cacheManager,
            IThumbnailDecoder gpuDecoder,
            IThumbnailDecoder cpuDecoder,
            IFileAccessManager? fileAccessManager = null)
        {
            _cacheManager = cacheManager ?? throw new ArgumentNullException(nameof(cacheManager));
            _gpuDecoder = gpuDecoder ?? throw new ArgumentNullException(nameof(gpuDecoder));
            _cpuDecoder = cpuDecoder ?? throw new ArgumentNullException(nameof(cpuDecoder));
            _fileAccessManager = fileAccessManager;
            _prefetchCache = new ConcurrentDictionary<string, byte[]>();
            
            Debug.WriteLine("[SmartThumbnailLoader] Dual decoder init completed");
            Debug.WriteLine($"  GPU Decoder: {_gpuDecoder.GetType().Name}");
            Debug.WriteLine($"  CPU Decoder: {_cpuDecoder.GetType().Name}");
            Debug.WriteLine($"  FileAccessManager: {(_fileAccessManager != null ? "Enabled" : "Disabled")}");
        }
        
        /// <summary>
        /// 兼容旧构造函�?- 单解码器（高优先级和普通任务共用同一解码器）
        /// </summary>
        [Obsolete("建议使用双解码器构造函数以提高性能")]
        public SmartThumbnailLoader(
            ThumbnailCacheManager cacheManager,
            IThumbnailDecoder decoder) : this(cacheManager, decoder, decoder, null)
        {
        }

        /// <summary>
        /// 预读取文件数据（用于并行优化�?
        /// �?优化：使�?CleanupScheduler 保护文件访问
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
                    // �?核心修复：使�?CleanupScheduler 保护预读取操�?
                    CleanupScheduler.MarkFileInUse(filePath);
                    
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

                        // 限制预读取缓存大小（最多保�?0个文件）
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
                    finally
                    {
                        CleanupScheduler.ReleaseFile(filePath);
                    }
                });
            }
            catch { }
        }

        /// <summary>
        /// 智能加载缩略图（自动选择最快方式）
        /// </summary>
        public BitmapImage? LoadThumbnail(string filePath, int size, bool isHighPriority = false)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                Debug.WriteLine("[SmartLoader] �?文件路径为空");
                return null;
            }
            
            if (!File.Exists(filePath))
            {
                Debug.WriteLine($"[SmartLoader] �?文件不存�? {filePath}");
                return null;
            }

            // �?日志优化：判断是否是前几张图片（输出详细日志�?
            int currentCount = Interlocked.Increment(ref _loadCounter);
            bool isFirstFewImages = currentCount <= FIRST_IMAGE_LOG_COUNT;

            var totalSw = Stopwatch.StartNew();
            var stepSw = new Stopwatch();
            string method = "";
            BitmapImage? result = null;
            
            // �?诊断计时变量
            long cacheQueryMs = 0;
            long gpuDecodeMs = 0;
            long originalDecodeMs = 0;

            try
            {
                // ===== L1 + L2: 缓存查询（统一在ThumbnailCacheManager中处理）=====
                // 内部流程：L1a强引�?�?L1b弱引�?�?L2a Shell缓存 �?L2b 自建磁盘缓存
                stepSw.Restart();
                var cached = _cacheManager.TryLoadFromCache(filePath);
                stepSw.Stop();
                cacheQueryMs = stepSw.ElapsedMilliseconds;
                
                if (cached != null)
                {
                    // �?关键诊断：检查缓存缩略图有效�?
                    if (cached.Width > 0 && cached.Height > 0)
                    {
                        method = "缓存命中";
                        Interlocked.Increment(ref _cacheHits);
                        totalSw.Stop();
                        Interlocked.Add(ref _totalLoadTimeMs, totalSw.ElapsedMilliseconds);
                        
                        // �?日志优化：前几张图片输出日志
                        if (isFirstFewImages)
                        {
                            Debug.WriteLine($"[诊断] LoadThumbnail详情: CacheQuery={cacheQueryMs}ms, Result=缓存命中 | file={System.IO.Path.GetFileName(filePath)}");
                            Debug.WriteLine($"[SmartLoader] �?缓存命中 | {totalSw.ElapsedMilliseconds}ms | file={System.IO.Path.GetFileName(filePath)}");
                        }
                        return cached;
                    }
                    else
                    {
                        Debug.WriteLine($"[SmartLoader] �?缓存缩略图无�?size={cached.Width}x{cached.Height} file={System.IO.Path.GetFileName(filePath)}");
                    }
                }

                // ===== L3: 解码器解�?=====
                // �?方案二：根据优先级选择解码�?
                // 高优先级任务使用GPU解码器（快速响应）
                // 普通任务使用CPU解码器（避免阻塞GPU队列�?
                stepSw.Restart();
                result = TryLoadFromDecoder(filePath, size, isFirstFewImages, isHighPriority);
                stepSw.Stop();
                gpuDecodeMs = stepSw.ElapsedMilliseconds;
                
                if (result != null)
                {
                    // 检查解码结果有效�?
                    if (result.Width > 0 && result.Height > 0)
                    {
                        method = "Decoder";
                        Interlocked.Increment(ref _gpuHits);
                        // �?方案二日志：显示使用的解码器类型
                        string decoderName = isHighPriority ? _gpuDecoder.GetType().Name : _cpuDecoder.GetType().Name;
                        Debug.WriteLine($"[Diagnostics] LoadThumbnail details: CacheQuery={cacheQueryMs}ms, Decode={gpuDecodeMs}ms, Decoder={decoderName}, Priority={isHighPriority} | file={System.IO.Path.GetFileName(filePath)}");
                        goto SUCCESS;
                    }
                    else
                    {
                        Debug.WriteLine($"[SmartLoader] �?解码器结果无�?size={result.Width}x{result.Height} file={System.IO.Path.GetFileName(filePath)}");
                        result = null;
                    }
                }

                // ===== L4: 原图解码回退（★ P1优化�?====
                stepSw.Restart();
                result = TryLoadFromOriginal(filePath, size);
                stepSw.Stop();
                originalDecodeMs = stepSw.ElapsedMilliseconds;
                
                if (result != null)
                {
                    // 检查原图解码结果有效�?
                    if (result.Width > 0 && result.Height > 0)
                    {
                        method = "原图解码";
                        Interlocked.Increment(ref _originalHits);
                        Debug.WriteLine($"[诊断] LoadThumbnail详情: CacheQuery={cacheQueryMs}ms, GpuDecode={gpuDecodeMs}ms, OriginalDecode={originalDecodeMs}ms, Result=原图解码 | file={System.IO.Path.GetFileName(filePath)}");
                        if (isFirstFewImages)
                        {
                            Debug.WriteLine($"[SmartLoader] �?L4原图解码 | {totalSw.ElapsedMilliseconds}ms | file={System.IO.Path.GetFileName(filePath)}");
                        }
                        goto SUCCESS;
                    }
                    else
                    {
                        Debug.WriteLine($"[SmartLoader] �?原图解码结果无效 size={result.Width}x{result.Height} file={System.IO.Path.GetFileName(filePath)}");
                        result = null;
                    }
                }

                // 所有策略都失败
                Interlocked.Increment(ref _misses);
                totalSw.Stop();
                Debug.WriteLine($"[诊断] LoadThumbnail详情: CacheQuery={cacheQueryMs}ms, GpuDecode={gpuDecodeMs}ms, OriginalDecode={originalDecodeMs}ms, Result=失败 | file={System.IO.Path.GetFileName(filePath)}");
                Debug.WriteLine($"[SmartLoader] �?所有策略失�?file={System.IO.Path.GetFileName(filePath)}");
                return null;

            SUCCESS:
                // 添加到内存缓存（会自动保存到磁盘缓存�?
                if (result != null)
                {
                    _cacheManager.AddToMemoryCache(filePath, result);
                }

                totalSw.Stop();
                Interlocked.Add(ref _totalLoadTimeMs, totalSw.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                totalSw.Stop();
                Debug.WriteLine($"[SmartLoader] �?加载异常: {ex.Message} file={System.IO.Path.GetFileName(filePath)}");
                Interlocked.Increment(ref _misses);
                return null;
            }
        }

        /// <summary>
        /// 异步加载缩略�?
        /// </summary>
        public async Task<BitmapImage?> LoadThumbnailAsync(string filePath, int size, CancellationToken cancellationToken = default, bool isHighPriority = false)
        {
            return await Task.Run(() => LoadThumbnail(filePath, size, isHighPriority), cancellationToken);
        }

        /// <summary>
        /// 尝试解码器解码（L3策略�?
        /// �?方案二：根据优先级选择GPU或CPU解码�?
        /// �?文件安全访问：通过 FileAccessManager 保护文件访问
        /// </summary>
        private BitmapImage? TryLoadFromDecoder(string filePath, int size, bool verboseLog = false, bool isHighPriority = false)
        {
            try
            {
                byte[]? prefetchedData = null;
                _prefetchCache.TryRemove(filePath, out prefetchedData);

                // �?方案二核心：根据优先级选择解码�?
                // 高优先级任务 �?GPU解码器（WicGpuDecoder�?槽位专用�?
                // 普通任�?�?CPU解码器（ImageSharpDecoder，不占用GPU资源�?
                var decoder = isHighPriority ? _gpuDecoder : _cpuDecoder;
                
                // �?核心修复：始终使用安全解码方�?
                // DecodeThumbnailSafe 内部会使�?CleanupScheduler 保护文件
                // 无论是否�?FileAccessManager，都会调�?MarkFileInUse/ReleaseFile
                BitmapImage? result = decoder.DecodeThumbnailSafe(_fileAccessManager, filePath, size, prefetchedData, verboseLog, isHighPriority);
                
                // 解码成功后异步保存到缓存（不阻塞显示�?
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
        /// �?P1优化：尝试从原图加载（L4最终回退方案�?
        /// 使用WPF内置解码，带缩放优化
        /// �?文件安全访问：通过 FileAccessManager 保护文件访问
        /// </summary>
        private BitmapImage? TryLoadFromOriginal(string filePath, int size)
        {
            // �?使用 FileAccessManager 保护文件访问（RAII模式�?
            if (_fileAccessManager != null)
            {
                using var scope = _fileAccessManager.CreateAccessScope(filePath, FileAccessIntent.Read, FileType.OriginalImage);
                if (!scope.IsGranted)
                {
                    Debug.WriteLine($"[SmartLoader] �?文件访问被拒�? {scope.ErrorMessage} file={System.IO.Path.GetFileName(filePath)}");
                    return null;
                }
                
                return DecodeOriginalInternal(filePath, size);
            }
            else
            {
                return DecodeOriginalInternal(filePath, size);
            }
        }
        
        /// <summary>
        /// 原图解码内部实现
        /// �?优化：使�?CleanupScheduler 保护 + StreamSource 立即加载
        /// </summary>
        private BitmapImage? DecodeOriginalInternal(string filePath, int size)
        {
            // �?核心修复：使�?CleanupScheduler 保护文件访问
            CleanupScheduler.MarkFileInUse(filePath);
            
            try
            {
                var sw = Stopwatch.StartNew();
                
                // �?优化：先读取文件到内存，避免 UriSource 延迟加载问题
                byte[] imageBytes;
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 8192, FileOptions.SequentialScan))
                {
                    imageBytes = new byte[fs.Length];
                    int bytesRead = fs.Read(imageBytes, 0, imageBytes.Length);
                    if (bytesRead != imageBytes.Length && imageBytes.Length > 0)
                    {
                        Array.Resize(ref imageBytes, bytesRead);
                    }
                }
                
                // 使用WPF内置解码，带缩放优化
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
                bitmap.DecodePixelWidth = size; // �?解码时缩放，节省内存
                bitmap.StreamSource = new MemoryStream(imageBytes);
                bitmap.EndInit();
                bitmap.Freeze();
                
                sw.Stop();
                Debug.WriteLine($"[SmartLoader] L4原图解码耗时:{sw.ElapsedMilliseconds}ms file={System.IO.Path.GetFileName(filePath)}");
                
                // 异步保存到缓存（L4解码较慢，值得缓存�?
                if (bitmap.Width > 0 && bitmap.Height > 0)
                {
                    _cacheManager.SaveToCacheNonBlocking(filePath, bitmap);
                }
                
                return bitmap;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SmartLoader] �?L4原图解码失败: {ex.Message} file={System.IO.Path.GetFileName(filePath)}");
                return null;
            }
            finally
            {
                // �?确保释放文件引用
                CleanupScheduler.ReleaseFile(filePath);
            }
        }

        /// <summary>
        /// 批量加载缩略图（用于可视区域批量加载�?
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
        /// 清除预读取缓�?
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
            _originalHits = 0;
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
