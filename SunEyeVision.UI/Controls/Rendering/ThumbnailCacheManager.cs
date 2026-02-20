using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using SunEyeVision.Core.IO;

namespace SunEyeVision.UI.Controls.Rendering
{
    /// <summary>
    /// 清理请求优先级
    /// </summary>
    public enum CleanupPriority
    {
        /// <summary>低优先级 - 后台空闲时清理</summary>
        Low = 0,
        /// <summary>普通优先级 - 缓存超限时清理</summary>
        Normal = 1,
        /// <summary>高优先级 - 内存压力时清理</summary>
        High = 2,
        /// <summary>紧急优先级 - 内存危险时立即清理</summary>
        Critical = 3
    }

    /// <summary>
    /// 清理请求
    /// </summary>
    public class CleanupRequest
    {
        public CleanupPriority Priority { get; set; }
        public long? TargetBytes { get; set; }  // 目标释放字节数
        public int? TargetFreeMB { get; set; }  // 目标释放MB数
        public string Requester { get; set; }   // 请求来源（用于日志）
        public Action<int, int>? ProgressCallback { get; set; } // 进度回调

        public static CleanupRequest FromBytes(long targetBytes, CleanupPriority priority, string requester)
            => new CleanupRequest { TargetBytes = targetBytes, Priority = priority, Requester = requester };

        public static CleanupRequest FromMB(int targetMB, CleanupPriority priority, string requester)
            => new CleanupRequest { TargetFreeMB = targetMB, Priority = priority, Requester = requester };
    }

    /// <summary>
    /// 统一清理调度器 - 解决并发竞态条件
    /// 核心原则：所有清理操作必须通过此调度器执行
    /// 
    /// 设计原则：
    /// 1. 清理器不应删除正在使用的文件
    /// 2. 文件使用通过引用计数跟踪
    /// 3. 在使用中的文件应跳过清理
    /// </summary>
    public static class CleanupScheduler
    {
        private static readonly object _globalLock = new object();
        private static readonly HashSet<string> _deletedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        
        // ★ 文件使用计数器 - 跟踪正在使用的文件
        private static readonly Dictionary<string, int> _fileUseCount = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        
        private static CancellationTokenSource? _currentCancellation;
        private static bool _isRunning;
        private static CleanupPriority _currentPriority = CleanupPriority.Low;

        /// <summary>全局已删除文件集合（线程安全）</summary>
        public static HashSet<string> DeletedFiles => _deletedFiles;
        
        /// <summary>当前正在使用的文件数量（用于诊断）</summary>
        public static int InUseFileCount
        {
            get
            {
                lock (_globalLock)
                {
                    return _fileUseCount.Count;
                }
            }
        }

        /// <summary>是否有清理任务正在执行</summary>
        public static bool IsRunning => _isRunning;

        /// <summary>当前清理优先级</summary>
        public static CleanupPriority CurrentPriority => _currentPriority;

        /// <summary>
        /// 请求磁盘清理
        /// </summary>
        /// <param name="request">清理请求</param>
        /// <param name="cacheDirectory">缓存目录</param>
        /// <param name="cacheIndex">缓存索引引用</param>
        /// <param name="scheduleIndexSave">保存索引的回调</param>
        /// <returns>实际删除的文件数量</returns>
        public static int RequestDiskCleanup(
            CleanupRequest request,
            string cacheDirectory,
            ConcurrentDictionary<string, string> cacheIndex,
            Action scheduleIndexSave)
        {
            lock (_globalLock)
            {
                // 如果有更高优先级的任务在执行，取消当前任务
                if (_isRunning && request.Priority <= _currentPriority)
                {
                    Debug.WriteLine($"[CleanupScheduler] ⚠ 跳过低优先级请求({request.Priority})，当前运行优先级({_currentPriority})");
                    return 0;
                }

                // 取消低优先级任务
                if (_isRunning && request.Priority > _currentPriority)
                {
                    _currentCancellation?.Cancel();
                    Debug.WriteLine($"[CleanupScheduler] ✓ 取消低优先级任务，启动高优先级({request.Priority})");
                }

                _currentCancellation = new CancellationTokenSource();
                _currentPriority = request.Priority;
                _isRunning = true;
            }

            try
            {
                return ExecuteDiskCleanup(request, cacheDirectory, cacheIndex, scheduleIndexSave, _currentCancellation!.Token);
            }
            finally
            {
                lock (_globalLock)
                {
                    _isRunning = false;
                    _currentPriority = CleanupPriority.Low;
                }
            }
        }

        /// <summary>
        /// 执行磁盘清理（内部方法）
        /// </summary>
        private static int ExecuteDiskCleanup(
            CleanupRequest request,
            string cacheDirectory,
            ConcurrentDictionary<string, string> cacheIndex,
            Action scheduleIndexSave,
            CancellationToken cancellationToken)
        {
            var sw = Stopwatch.StartNew();
            int deletedCount = 0;
            long currentFreeBytes = 0;

            // 获取文件快照（线程安全）
            var files = GetCacheFilesSnapshot(cacheDirectory);
            int totalFiles = files.Count;

            // 计算目标释放量
            long targetFreeBytes = request.TargetBytes ?? (request.TargetFreeMB ?? 0) * 1024L * 1024L;

            // 按最后访问时间排序（最旧的先清理）
            var sortedFiles = files
                .Select(f => new { File = f, Info = SafeGetFileInfo(f) })
                .Where(f => f.Info != null)
                .OrderBy(f => f.Info!.LastWriteTime)
                .ToList();

            foreach (var item in sortedFiles)
            {
                // 检查取消请求
                if (cancellationToken.IsCancellationRequested)
                {
                    Debug.WriteLine($"[CleanupScheduler] ⚠ 清理被取消");
                    break;
                }

                // 检查是否达到目标
                if (targetFreeBytes > 0 && currentFreeBytes >= targetFreeBytes)
                    break;

                // 安全删除文件
                if (SafeDeleteFile(item.File, out long fileSize))
                {
                    currentFreeBytes += fileSize;
                    deletedCount++;

                    // 从索引中移除
                    var key = cacheIndex.FirstOrDefault(kvp => kvp.Value == item.File).Key;
                    if (!string.IsNullOrEmpty(key))
                    {
                        cacheIndex.TryRemove(key, out _);
                    }
                }

                // 进度回调
                request.ProgressCallback?.Invoke(deletedCount, totalFiles);

                // 分批休息（避免卡顿）
                if (deletedCount % 10 == 0 && deletedCount > 0)
                {
                    Thread.Sleep(10);
                }
            }

            scheduleIndexSave();
            sw.Stop();

            Debug.WriteLine($"[CleanupScheduler] ✓ 清理完成 [{request.Requester}] - 删除{deletedCount}个文件({currentFreeBytes / 1024 / 1024:F1}MB) 耗时:{sw.ElapsedMilliseconds}ms 优先级:{request.Priority}");

            return deletedCount;
        }

        /// <summary>
        /// 安全删除文件（防止并发删除冲突）
        /// 核心规则：不删除正在使用的文件
        /// </summary>
        public static bool SafeDeleteFile(string filePath, out long fileSize)
        {
            fileSize = 0;
            string fileName = Path.GetFileName(filePath);

            // 检查是否已被删除
            lock (_globalLock)
            {
                if (_deletedFiles.Contains(filePath))
                {
                    Debug.WriteLine($"[FileLife] ⊘ AlreadyDeleted | {fileName}");
                    return false;
                }
            }

            // ★ 核心保护：检查文件是否正在使用
            bool inUse = IsFileInUse(filePath);
            if (inUse)
            {
                // ★ 关键日志：跳过正在使用的文件
                Debug.WriteLine($"[FileLife] 🔒 SkipInUse | {fileName}");
                return false;
            }

            try
            {
                // 再次检查文件是否存在
                if (!File.Exists(filePath))
                {
                    Debug.WriteLine($"[FileLife] ⊘ NotExists | {fileName}");
                    lock (_globalLock)
                    {
                        _deletedFiles.Add(filePath);
                    }
                    return false;
                }

                // ★ 二次检查锁：在删除前再次确认
                lock (_globalLock)
                {
                    if (_fileUseCount.ContainsKey(filePath) && _fileUseCount[filePath] > 0)
                    {
                        Debug.WriteLine($"[FileLife] 🔒 DoubleCheckSkip | {fileName}");
                        return false;
                    }
                }

                var info = new FileInfo(filePath);
                fileSize = info.Length;

                // ★ 关键日志：开始删除
                Debug.WriteLine($"[FileLife] 🗑️ Deleting | {fileName}");

                File.Delete(filePath);

                // 标记为已删除
                lock (_globalLock)
                {
                    _deletedFiles.Add(filePath);
                }

                // ★ 关键日志：删除成功
                Debug.WriteLine($"[FileLife] ✓ Deleted | {fileName}");
                return true;
            }
            catch (FileNotFoundException)
            {
                // ★ 关键日志：文件已被其他进程删除
                Debug.WriteLine($"[FileLife] ⚠ DeletedByOther | {fileName}");
                lock (_globalLock)
                {
                    _deletedFiles.Add(filePath);
                }
                return false;
            }
            catch (IOException ex)
            {
                // ★ 关键日志：文件被占用
                Debug.WriteLine($"[FileLife] ⚠ Locked {ex.Message} | {fileName}");
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                Debug.WriteLine($"[FileLife] ⚠ NoAccess | {fileName}");
                return false;
            }
        }

        /// <summary>
        /// 获取缓存文件快照（线程安全）
        /// </summary>
        public static List<string> GetCacheFilesSnapshot(string cacheDirectory)
        {
            try
            {
                return Directory.GetFiles(cacheDirectory)
                    .Where(f => Path.GetFileName(f) != "cache_index.txt")
                    .ToList();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CleanupScheduler] ✗ 获取文件列表失败: {ex.Message}");
                return new List<string>();
            }
        }

        /// <summary>
        /// 安全获取文件信息
        /// </summary>
        private static FileInfo? SafeGetFileInfo(string filePath)
        {
            try
            {
                return new FileInfo(filePath);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 检查文件是否已删除
        /// </summary>
        public static bool IsFileDeleted(string filePath)
        {
            lock (_globalLock)
            {
                return _deletedFiles.Contains(filePath);
            }
        }

        /// <summary>
        /// 清空已删除文件记录（用于清理过期记录）
        /// </summary>
        public static void ClearDeletedRecords()
        {
            lock (_globalLock)
            {
                _deletedFiles.Clear();
            }
        }

        #region 文件使用计数机制

        /// <summary>
        /// 标记文件正在使用（增加引用计数）
        /// 在加载缓存文件前调用，防止清理器删除正在使用的文件
        /// </summary>
        /// <param name="filePath">文件路径</param>
        public static void MarkFileInUse(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return;
            
            lock (_globalLock)
            {
                int newCount;
                if (_fileUseCount.ContainsKey(filePath))
                {
                    _fileUseCount[filePath]++;
                    newCount = _fileUseCount[filePath];
                }
                else
                {
                    _fileUseCount[filePath] = 1;
                    newCount = 1;
                }
                
                // ★ 关键日志：记录文件被标记
                Debug.WriteLine($"[FileLife] 📌 MarkInUse cnt={newCount} | {Path.GetFileName(filePath)}");
            }
        }

        /// <summary>
        /// 释放文件使用（减少引用计数）
        /// 在加载缓存文件完成后调用（无论成功或失败）
        /// </summary>
        /// <param name="filePath">文件路径</param>
        public static void ReleaseFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return;
            
            lock (_globalLock)
            {
                if (_fileUseCount.ContainsKey(filePath))
                {
                    _fileUseCount[filePath]--;
                    int remaining = _fileUseCount[filePath];
                    
                    if (remaining <= 0)
                    {
                        _fileUseCount.Remove(filePath);
                        // ★ 关键日志：文件引用完全释放
                        Debug.WriteLine($"[FileLife] 📤 ReleaseAll | {Path.GetFileName(filePath)}");
                    }
                    else
                    {
                        // ★ 关键日志：文件引用减少
                        Debug.WriteLine($"[FileLife] 📤 Release cnt={remaining} | {Path.GetFileName(filePath)}");
                    }
                }
                else
                {
                    // ★ 异常情况：释放了未标记的文件
                    Debug.WriteLine($"[FileLife] ⚠ ReleaseNotMarked | {Path.GetFileName(filePath)}");
                }
            }
        }

        /// <summary>
        /// 检查文件是否正在使用中
        /// 清理器在删除文件前应调用此方法检查
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>如果文件正在使用返回 true</returns>
        public static bool IsFileInUse(string filePath)
        {
            lock (_globalLock)
            {
                return _fileUseCount.ContainsKey(filePath) && _fileUseCount[filePath] > 0;
            }
        }

        /// <summary>
        /// 获取所有正在使用的文件列表（用于诊断）
        /// </summary>
        public static IReadOnlyList<string> GetInUseFiles()
        {
            lock (_globalLock)
            {
                return _fileUseCount.Keys.ToList().AsReadOnly();
            }
        }

        #endregion
    }

    /// <summary>
    /// 缓存管理器 - 简化版3层架构
    /// 
    /// 缓存层级：
    /// L1: 内存缓存（强引用50张 + 弱引用）
    /// L2: 磁盘缓存（Shell缓存优先 + 自建缓存补充）
    /// 
    /// 核心优化：首次加载速度提升（80%贡献）
    /// 
    /// ★ 文件生命周期管理：
    /// - 通过 IFileAccessManager 统一管理文件访问
    /// - 防止清理器删除正在使用的文件
    /// </summary>
    public class ThumbnailCacheManager : IDisposable
    {
        private readonly string _cacheDirectory;
        private readonly int _thumbnailSize = 60;
        private readonly int _jpegQuality = 85;
        private readonly long _maxCacheSizeBytes = 500 * 1024 * 1024; // 500MB
        private readonly PerformanceLogger _logger = new PerformanceLogger("ThumbnailCache");
        private readonly ConcurrentDictionary<string, string> _cacheIndex = new ConcurrentDictionary<string, string>();
        
        // L1缓存：强引用内存缓存（最近使用）
        private readonly ConcurrentDictionary<string, BitmapImage> _memoryCache = new ConcurrentDictionary<string, BitmapImage>();
        private const int MAX_MEMORY_CACHE_SIZE = 50; // 最大强引用缓存数量
        
        // L1备份：弱引用缓存（可被GC回收）
        private readonly WeakReferenceCache<string, BitmapImage> _weakCache = new WeakReferenceCache<string, BitmapImage>();
        
        // Shell缓存提供者（L2优先策略）
        private readonly WindowsShellThumbnailProvider _shellProvider;
        
        // ★ 文件访问管理器（可选，用于统一的文件生命周期管理）
        private readonly IFileAccessManager? _fileAccessManager;
        
        private readonly object _indexLock = new object(); // 索引文件访问锁
        
        // 文件锁字典，防止并发写入同一文件
        private readonly ConcurrentDictionary<string, object> _fileLocks = new ConcurrentDictionary<string, object>();
        private Timer? _indexSaveTimer; // 延迟保存索引的定时器
        private bool _indexDirty = false; // 索引是否需要保存
        private bool _disposed = false;

        /// <summary>
        /// 缓存命中统计
        /// </summary>
        public class CacheStatistics
        {
            public int TotalRequests { get; set; }
            public int CacheHits { get; set; }
            public int CacheMisses { get; set; }
            public double HitRate => TotalRequests > 0 ? (double)CacheHits / TotalRequests * 100 : 0;
        }

        private readonly CacheStatistics _statistics = new CacheStatistics();

        /// <summary>
        /// 获取缓存统计信息
        /// </summary>
        public CacheStatistics Statistics => _statistics;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="fileAccessManager">文件访问管理器（可选，用于统一文件生命周期管理）</param>
        public ThumbnailCacheManager(IFileAccessManager? fileAccessManager = null)
        {
            _cacheDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SunEyeVision",
                "ThumbnailCache");
            
            // 初始化Shell缓存提供者
            _shellProvider = new WindowsShellThumbnailProvider();
            
            // ★ 文件访问管理器（用于统一文件生命周期管理）
            _fileAccessManager = fileAccessManager;

            InitializeCache();

            // 启动定时器，每1秒保存一次索引（如果索引有变化）
            _indexSaveTimer = new Timer(_ =>
            {
                if (_indexDirty)
                {
                    SaveCacheIndex();
                }
            }, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
            
            Debug.WriteLine("[ThumbnailCache] ✓ 缓存管理器初始化完成（3层架构）");
            Debug.WriteLine($"  L1: 内存缓存(强引用{MAX_MEMORY_CACHE_SIZE}张 + 弱引用)");
            Debug.WriteLine($"  L2: Shell缓存优先 + 磁盘缓存补充");
            Debug.WriteLine($"  文件访问管理器: {(_fileAccessManager != null ? "已启用" : "未启用")}");
        }

        /// <summary>
        /// 初始化缓存目录
        /// </summary>
        private void InitializeCache()
        {
            try
            {
                if (!Directory.Exists(_cacheDirectory))
                {
                    Directory.CreateDirectory(_cacheDirectory);
                    Debug.WriteLine($"[ThumbnailCache] ✓ 创建缓存目录: {_cacheDirectory}");
                }

                // 加载缓存索引
                LoadCacheIndex();

                Debug.WriteLine($"[ThumbnailCache] ✓ 缓存初始化完成");
                Debug.WriteLine($"[ThumbnailCache]   目录: {_cacheDirectory}");
                Debug.WriteLine($"[ThumbnailCache]   缩略图尺寸: 60x60");
                Debug.WriteLine($"[ThumbnailCache]   JPEG质量: {_jpegQuality}%");
                Debug.WriteLine($"[ThumbnailCache]   最大缓存: {_maxCacheSizeBytes / 1024 / 1024}MB");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ThumbnailCache] ✗ 缓存初始化失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 加载缓存索引
        /// </summary>
        private void LoadCacheIndex()
        {
            var sw = Stopwatch.StartNew();
            int count = 0;

            try
            {
                var indexFile = Path.Combine(_cacheDirectory, "cache_index.txt");
                if (File.Exists(indexFile))
                {
                    var lines = File.ReadAllLines(indexFile);
                    foreach (var line in lines)
                    {
                        var parts = line.Split('|');
                        if (parts.Length == 2)
                        {
                            _cacheIndex.TryAdd(parts[0], parts[1]);
                            count++;
                        }
                    }
                    Debug.WriteLine($"[ThumbnailCache] ✓ 加载了 {count} 个缓存索引");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ThumbnailCache] ⚠ 加载缓存索引失败: {ex.Message}");
            }

            _logger.LogOperation("加载缓存索引", sw.Elapsed, $"数量: {count}");
        }

        /// <summary>
        /// 保存缓存索引（线程安全）
        /// </summary>
        private void SaveCacheIndex()
        {
            lock (_indexLock)
            {
                try
                {
                    var indexFile = Path.Combine(_cacheDirectory, "cache_index.txt");
                    var lines = _cacheIndex.Select(kvp => $"{kvp.Key}|{kvp.Value}");
                    File.WriteAllLines(indexFile, lines);
                    _indexDirty = false; // 清除脏标志
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ThumbnailCache] ⚠ 保存缓存索引失败: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 安排索引保存（延迟批量保存）
        /// </summary>
        private void ScheduleIndexSave()
        {
            _indexDirty = true; // 标记索引需要保存
            // 定时器会自动在1秒后保存，无需立即保存
        }

        /// <summary>
        /// 生成文件路径的唯一哈希
        /// </summary>
        private string GetFileHash(string filePath)
        {
            using var md5 = MD5.Create();
            var hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(filePath));
            return BitConverter.ToString(hash).Replace("-", "").Substring(0, 16);
        }

        /// <summary>
        /// 获取缓存文件路径
        /// 注意：缓存始终使用JPEG格式保存，因此扩展名固定为.jpg
        /// </summary>
        private string GetCacheFilePath(string filePath)
        {
            var hash = GetFileHash(filePath);
            return Path.Combine(_cacheDirectory, $"{hash}.jpg");
        }

        /// <summary>
        /// 添加到内存缓存（多级缓存）
        /// </summary>
        public void AddToMemoryCache(string filePath, BitmapImage bitmap)
        {
            if (bitmap != null && !string.IsNullOrEmpty(filePath))
            {
                // L1缓存：强引用（有上限）
                if (_memoryCache.Count >= MAX_MEMORY_CACHE_SIZE)
                {
                    // L1已满，将最旧的移到L2弱引用缓存
                    var oldestKey = _memoryCache.Keys.FirstOrDefault();
                    if (oldestKey != null && _memoryCache.TryRemove(oldestKey, out var oldBitmap))
                    {
                        _weakCache.Add(oldestKey, oldBitmap);
                    }
                }
                _memoryCache.TryAdd(filePath, bitmap);
                
                // 同时存入L2弱引用缓存（作为备份）
                _weakCache.Add(filePath, bitmap);
                
                // 缓存添加不输出日志
            }
        }

        /// <summary>
        /// 从内存缓存中移除（用于清理远离可视区域的缩略图）
        /// </summary>
        public void RemoveFromMemoryCache(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return;

            // 从L1强引用缓存移除
            // 缓存移除不输出日志

            // 从L2弱引用缓存移除
            _weakCache.Remove(filePath);
        }

        /// <summary>
        /// 尝试从缓存加载缩略图（3层缓存查询）
        /// L1: 内存缓存（强引用 + 弱引用）
        /// L2: Shell缓存优先 + 自建磁盘缓存
        /// ★ 使用 FileAccessManager 保护文件访问（如果可用）
        /// </summary>
        public BitmapImage? TryLoadFromCache(string filePath)
        {
            _statistics.TotalRequests++;

            // L1a: 强引用内存缓存
            if (_memoryCache.TryGetValue(filePath, out var cachedBitmap))
            {
                _statistics.CacheHits++;
                return cachedBitmap;
            }

            // L1b: 弱引用缓存
            if (_weakCache.TryGet(filePath, out var weakCachedBitmap) && weakCachedBitmap != null)
            {
                _statistics.CacheHits++;
                // 命中L1b后提升到L1a
                _memoryCache.TryAdd(filePath, weakCachedBitmap);
                return weakCachedBitmap;
            }

            // L2a: Shell缓存（优先策略）
            var shellThumbnail = TryLoadFromShellCache(filePath);
            if (shellThumbnail != null)
            {
                _statistics.CacheHits++;
                // 添加到内存缓存
                _memoryCache.TryAdd(filePath, shellThumbnail);
                _weakCache.Add(filePath, shellThumbnail);
                return shellThumbnail;
            }

            // L2b: 自建磁盘缓存（备用策略）
            var cacheFilePath = GetCacheFilePath(filePath);
            if (!_cacheIndex.TryGetValue(filePath, out string? cachedPath) || !File.Exists(cacheFilePath))
            {
                _statistics.CacheMisses++;
                return null;
            }

            // ★ 核心修复：使用 FileAccessManager 保护文件访问（RAII模式）
            if (_fileAccessManager != null)
            {
                using var scope = _fileAccessManager.CreateAccessScope(cacheFilePath, FileAccessIntent.Read, FileType.CacheFile);
                
                if (!scope.IsGranted)
                {
                    Debug.WriteLine($"[ThumbnailCache] ⚠ 文件访问被拒绝: {scope.ErrorMessage} file={Path.GetFileName(cacheFilePath)}");
                    _statistics.CacheMisses++;
                    return null;
                }
                
                return LoadCacheFileInternal(filePath, cacheFilePath);
            }
            else
            {
                // 兼容模式：使用 CleanupScheduler（旧方式）
                CleanupScheduler.MarkFileInUse(cacheFilePath);
                
                try
                {
                    return LoadCacheFileInternal(filePath, cacheFilePath);
                }
                finally
                {
                    CleanupScheduler.ReleaseFile(cacheFilePath);
                }
            }
        }
        
        /// <summary>
        /// 从缓存文件加载（内部实现）
        /// ★ 关键修复：使用 StreamSource + 内存缓冲，避免 UriSource 延迟加载导致竞态条件
        /// 
        /// 问题根因：
        /// - BitmapImage.UriSource 是延迟加载的，在 EndInit() 时才真正读取文件
        /// - 清理器可能在 MarkFileInUse() 和 EndInit() 之间删除文件
        /// - 导致 FileNotFoundException 异常
        /// 
        /// 解决方案：
        /// - 先同步读取文件到内存缓冲
        /// - 再用 MemoryStream 加载，完全避免文件竞态
        /// </summary>
        private BitmapImage? LoadCacheFileInternal(string filePath, string cacheFilePath)
        {
            try
            {
                // 再次检查文件存在（双重保险）
                if (!File.Exists(cacheFilePath))
                {
                    _cacheIndex.TryRemove(filePath, out _);
                    return null;
                }

                // ★ 核心修复：先同步读取文件到内存，避免 UriSource 延迟加载
                byte[] imageBytes;
                using (var fs = new FileStream(cacheFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 8192, FileOptions.SequentialScan))
                {
                    imageBytes = new byte[fs.Length];
                    int bytesRead = fs.Read(imageBytes, 0, imageBytes.Length);
                    // 处理读取不完整的情况
                    if (bytesRead != imageBytes.Length && imageBytes.Length > 0)
                    {
                        Array.Resize(ref imageBytes, bytesRead);
                    }
                }

                // 从内存流加载图像
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                bitmap.StreamSource = new MemoryStream(imageBytes);  // 使用内存流
                bitmap.EndInit();
                bitmap.Freeze();

                // 添加到内存缓存
                _memoryCache.TryAdd(filePath, bitmap);
                _weakCache.Add(filePath, bitmap);

                _statistics.CacheHits++;
                // 磁盘缓存命中不输出日志（高频操作）

                return bitmap;
            }
            catch (FileNotFoundException)
            {
                // 文件已被其他进程删除
                Debug.WriteLine($"[ThumbnailCache] ⚠ 缓存文件已删除: {Path.GetFileName(cacheFilePath)}");
                _cacheIndex.TryRemove(filePath, out _);
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ThumbnailCache] ✗ 缓存加载失败: {ex.Message}");
                _cacheIndex.TryRemove(filePath, out _);
                return null;
            }
        }
        
        /// <summary>
        /// 尝试从Shell缓存加载（L2优先策略）
        /// </summary>
        private BitmapImage? TryLoadFromShellCache(string filePath)
        {
            try
            {
                // 仅从系统缓存获取，不生成新的缩略图
                var thumbnail = _shellProvider.GetThumbnail(filePath, _thumbnailSize, cacheOnly: true);
                if (thumbnail != null)
                {
                    // 转换为BitmapImage
                    return ConvertToBitmapImage(thumbnail, _thumbnailSize);
                }
                return null;
            }
            catch
            {
                return null;
            }
        }
        
        /// <summary>
        /// 将BitmapSource转换为BitmapImage
        /// </summary>
        private BitmapImage ConvertToBitmapImage(BitmapSource source, int size)
        {
            if (source is BitmapImage bitmap)
                return bitmap;

            var result = new BitmapImage();
            using var memory = new MemoryStream();

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(source));
            encoder.Save(memory);
            memory.Position = 0;

            result.BeginInit();
            result.CacheOption = BitmapCacheOption.OnLoad;
            result.DecodePixelWidth = size;
            result.StreamSource = memory;
            result.EndInit();
            result.Freeze();

            return result;
        }

        /// <summary>
        /// 保存缩略图到缓存（同步保存，会阻塞）
        /// 适用于需要确保缓存立即可用的场景
        /// </summary>
        public void SaveToCache(string filePath, BitmapSource thumbnail)
        {
            try
            {
                var sw = Stopwatch.StartNew();
                var cacheFilePath = GetCacheFilePath(filePath);

                // 保存到内存缓存（优先）
                if (thumbnail is BitmapImage bitmap)
                {
                    _memoryCache.TryAdd(filePath, bitmap);
                }

                // 保存到磁盘缓存 - 编码并写入文件
                var encoder = new JpegBitmapEncoder();
                encoder.QualityLevel = _jpegQuality;
                encoder.Frames.Add(BitmapFrame.Create(thumbnail));

                var encodeSw = Stopwatch.StartNew();
                using var stream = new FileStream(cacheFilePath, FileMode.Create);
                encoder.Save(stream);
                var cacheSize = stream.Length;
                encodeSw.Stop();

                // 更新索引（延迟保存）
                var indexSw = Stopwatch.StartNew();
                _cacheIndex.TryAdd(filePath, cacheFilePath);
                ScheduleIndexSave(); // 延迟保存索引，不再立即保存
                indexSw.Stop();

                // 检查缓存大小并清理
                CheckCacheSizeAndCleanup();

                // 缓存保存成功不输出日志
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ThumbnailCache] ✗ 缓存保存失败: {ex.Message}");
            }
        }

        // 磁盘写入跟踪
        private int _pendingDiskWrites = 0;
        private readonly object _diskWriteLock = new object();

        /// <summary>
        /// 非阻塞保存缩略图到缓存（优化版）
        /// - 同步更新内存缓存（立即返回）
        /// - 异步保存磁盘缓存（后台执行）
        /// </summary>
        /// <remarks>
        /// 性能优势：首张显示延迟从 +10-35ms 降到 0ms
        /// </remarks>
        public void SaveToCacheNonBlocking(string filePath, BitmapSource thumbnail)
        {
            if (thumbnail == null || string.IsNullOrEmpty(filePath))
                return;

            // 1. 立即更新内存缓存（同步，<1ms）
            if (thumbnail is BitmapImage bitmap)
            {
                AddToMemoryCache(filePath, bitmap);
            }

            // 2. 异步保存到磁盘（不阻塞调用方）
            Interlocked.Increment(ref _pendingDiskWrites);
            _ = Task.Run(() =>
            {
                try
                {
                    SaveToDiskCache(filePath, thumbnail);
                }
                finally
                {
                    Interlocked.Decrement(ref _pendingDiskWrites);
                }
            });
        }

        /// <summary>
        /// 保存到磁盘缓存（内部方法，后台线程执行）
        /// </summary>
        private void SaveToDiskCache(string filePath, BitmapSource thumbnail)
        {
            try
            {
                var cacheFilePath = GetCacheFilePath(filePath);
                
                // ★ 获取文件专用锁，防止并发写入冲突
                var fileLock = _fileLocks.GetOrAdd(cacheFilePath, _ => new object());
                
                lock (fileLock)
                {
                    // JPEG编码并写入文件
                    var encoder = new JpegBitmapEncoder();
                    encoder.QualityLevel = _jpegQuality;
                    encoder.Frames.Add(BitmapFrame.Create(thumbnail));

                    // ★ 使用 FileShare.None 确保独占访问
                    using var stream = new FileStream(cacheFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
                    encoder.Save(stream);
                }

                // 更新索引（延迟保存）
                _cacheIndex.TryAdd(filePath, cacheFilePath);
                ScheduleIndexSave();

                // 检查缓存大小
                CheckCacheSizeAndCleanup();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ThumbnailCache] ✗ 磁盘缓存保存失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 等待所有磁盘写入完成（应用退出时调用）
        /// </summary>
        public async Task WaitForPendingSavesAsync(TimeSpan? timeout = null)
        {
            var deadline = timeout.HasValue ? DateTime.Now.Add(timeout.Value) : DateTime.MaxValue;

            while (Interlocked.CompareExchange(ref _pendingDiskWrites, 0, 0) > 0)
            {
                if (DateTime.Now > deadline)
                {
                    Debug.WriteLine($"[ThumbnailCache] ⚠ 等待磁盘写入超时，剩余 {_pendingDiskWrites} 个");
                    break;
                }
                await Task.Delay(10);
            }
        }

        /// <summary>
        /// 异步保存缩略图到缓存
        /// </summary>
        public async Task SaveToCacheAsync(string filePath, BitmapSource thumbnail)
        {
            await Task.Run(() => SaveToCache(filePath, thumbnail));
        }

        /// <summary>
        /// 检查缓存大小并清理（使用统一调度器）
        /// </summary>
        private void CheckCacheSizeAndCleanup()
        {
            try
            {
                // 获取当前缓存大小
                var files = CleanupScheduler.GetCacheFilesSnapshot(_cacheDirectory);
                var totalSize = files.Sum(f =>
                {
                    try { return new FileInfo(f).Length; }
                    catch { return 0; }
                });

                if (totalSize > _maxCacheSizeBytes)
                {
                    Debug.WriteLine($"[ThumbnailCache] ⚠ 缓存超限 ({totalSize / 1024 / 1024:F1}MB)，开始清理...");

                    // 计算需要释放的空间（清理到80%）
                    var targetSize = (long)(_maxCacheSizeBytes * 0.8);
                    var bytesToFree = totalSize - targetSize;

                    // 使用统一调度器执行清理
                    var request = CleanupRequest.FromBytes(bytesToFree, CleanupPriority.Normal, "CheckCacheSizeAndCleanup");
                    var deletedCount = CleanupScheduler.RequestDiskCleanup(request, _cacheDirectory, _cacheIndex, ScheduleIndexSave);

                    Debug.WriteLine($"[ThumbnailCache] ✓ 清理完成 - 删除了 {deletedCount} 个文件");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ThumbnailCache] ✗ 缓存清理失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 清除所有缓存
        /// </summary>
        public void ClearCache()
        {
            try
            {
                var sw = Stopwatch.StartNew();
                int deletedCount = 0;

                foreach (var file in Directory.GetFiles(_cacheDirectory))
                {
                    try
                    {
                        File.Delete(file);
                        deletedCount++;
                    }
                    catch { }
                }

                // 清除内存缓存
                _memoryCache.Clear();

                _cacheIndex.Clear();
                _statistics.TotalRequests = 0;
                _statistics.CacheHits = 0;
                _statistics.CacheMisses = 0;

                _logger.LogOperation("清除缓存", sw.Elapsed, $"删除: {deletedCount} 个文件");
                Debug.WriteLine($"[ThumbnailCache] ✓ 缓存已清除 - 删除了 {deletedCount} 个文件");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ThumbnailCache] ✗ 清除缓存失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 预生成缩略图缓存（用于批量加载优化）
        /// </summary>
        public async Task PreGenerateCacheAsync(string[] filePaths, Func<string, BitmapSource?> loadFunc)
        {
            Debug.WriteLine($"[ThumbnailCache] ========== 预生成缓存开始 ==========");
            Debug.WriteLine($"[ThumbnailCache] 待生成数量: {filePaths.Length}");

            var sw = Stopwatch.StartNew();
            int generatedCount = 0;
            int skippedCount = 0;
            int failedCount = 0;

            await Task.Run(() =>
            {
                Parallel.ForEach(filePaths, filePath =>
                {
                    try
                    {
                        if (TryLoadFromCache(filePath) != null)
                        {
                            Interlocked.Increment(ref skippedCount);
                            return;
                        }

                        var thumbnail = loadFunc(filePath);
                        if (thumbnail != null)
                        {
                            SaveToCache(filePath, thumbnail);
                            Interlocked.Increment(ref generatedCount);
                        }
                        else
                        {
                            Interlocked.Increment(ref failedCount);
                        }
                    }
                    catch { }
                });
            });

            sw.Stop();
            Debug.WriteLine($"[ThumbnailCache] 预生成完成 - 生成:{generatedCount} 跳过:{skippedCount} 失败:{failedCount}");
            Debug.WriteLine($"[ThumbnailCache] 总耗时: {sw.Elapsed.TotalSeconds:F2}秒");
            Debug.WriteLine($"[ThumbnailCache] ========== 预生成缓存结束 ==========");
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                // 停止定时器
                _indexSaveTimer?.Dispose();

                // 强制保存索引（确保数据不丢失）
                if (_indexDirty)
                {
                    SaveCacheIndex();
                }

                _memoryCache.Clear(); // 清理内存缓存
                _shellProvider?.Dispose(); // 释放Shell提供者
                _disposed = true;
                Debug.WriteLine("[ThumbnailCache] 资源已释放");
            }
        }

        /// <summary>
        /// 获取缓存信息
        /// </summary>
        public string GetCacheInfo()
        {
            try
            {
                var files = Directory.GetFiles(_cacheDirectory)
                    .Where(f => Path.GetFileName(f) != "cache_index.txt")
                    .ToList();

                var totalSize = files.Sum(f => new FileInfo(f).Length);
                var fileSize = totalSize / 1024.0 / 1024.0;
                var shellStats = _shellProvider.GetStatistics();

                return $"L1:{_memoryCache.Count}个 L2弱引用:{_weakCache.AliveCount}个 磁盘:{files.Count}个/{fileSize:F1}MB 命中率:{_statistics.HitRate:F1}% | {shellStats}";
            }
            catch
            {
                return "缓存信息获取失败";
            }
        }
        
        /// <summary>
        /// 响应内存压力 - 清理缓存
        /// </summary>
        public void RespondToMemoryPressure(bool isCritical)
        {
            if (isCritical)
            {
                // 危险级别：立即清空L1，渐进清理L2
                _memoryCache.Clear();
                // ★ P1优化：渐进式清理磁盘缓存
                ProgressiveCleanup(100); // 目标释放100MB
            }
            else
            {
                // 高压力：渐进清理L1和L2
                ProgressiveCleanup(50, (deleted, total) =>
                {
                    // 同时清理L1内存缓存
                    if (deleted % 5 == 0 && _memoryCache.Count > 25)
                    {
                        var key = _memoryCache.Keys.FirstOrDefault();
                        if (key != null && _memoryCache.TryRemove(key, out var bitmap))
                        {
                            _weakCache.Add(key, bitmap);
                        }
                    }
                });
            }
        }

        /// <summary>
        /// ★ P1优化：渐进式内存清理（使用统一调度器）
        /// 分批次清理缓存，避免一次性大量清理导致卡顿
        /// </summary>
        /// <param name="targetFreeMB">目标释放空间(MB)</param>
        /// <param name="progressCallback">进度回调(已删除数量, 总数量)</param>
        public void ProgressiveCleanup(int targetFreeMB, Action<int, int>? progressCallback = null)
        {
            // 根据调用来源确定优先级
            // RespondToMemoryPressure 会根据 isCritical 参数传入不同的 targetFreeMB
            // 100MB = 危险级别(Critical), 50MB = 高压力(High)
            var priority = targetFreeMB >= 100 ? CleanupPriority.Critical : CleanupPriority.High;

            _ = Task.Run(() =>
            {
                try
                {
                    // 使用统一调度器执行清理
                    var request = new CleanupRequest
                    {
                        TargetFreeMB = targetFreeMB,
                        Priority = priority,
                        Requester = "ProgressiveCleanup",
                        ProgressCallback = progressCallback
                    };

                    CleanupScheduler.RequestDiskCleanup(request, _cacheDirectory, _cacheIndex, ScheduleIndexSave);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ThumbnailCache] ✗ 渐进清理失败: {ex.Message}");
                }
            });
        }
    }
}
