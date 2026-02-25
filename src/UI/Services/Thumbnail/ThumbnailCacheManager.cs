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
using SunEyeVision.UI.Services.Thumbnail;
using SunEyeVision.UI.Services.Thumbnail.Caching;
using SunEyeVision.UI.Services.Performance;

namespace SunEyeVision.UI.Services.Thumbnail
{
    /// <summary>
    /// ȼ
    /// </summary>
    public enum CleanupPriority
    {
        /// <summary>ȼ - ̨ʱ</summary>
        Low = 0,
        /// <summary>晚优先级 - 泬ʱ/summary>
        Normal = 1,
        /// <summary>ȼ - ڴѹʱ</summary>
        High = 2,
        /// <summary>ȼ - ڴΣʱ</summary>
        Critical = 3
    }

    /// <summary>
    /// 
    /// </summary>
    public class CleanupRequest
    {
        public CleanupPriority Priority { get; set; }
        public long? TargetBytes { get; set; }  // ࠇ释放字节?
        public int? TargetFreeMB { get; set; }  // ࠇ释放MB?
        public string Requester { get; set; }   // Դ־
        public Action<int, int>? ProgressCallback { get; set; } // Ȼص

        public static CleanupRequest FromBytes(long targetBytes, CleanupPriority priority, string requester)
            => new CleanupRequest { TargetBytes = targetBytes, Priority = priority, Requester = requester };

        public static CleanupRequest FromMB(int targetMB, CleanupPriority priority, string requester)
            => new CleanupRequest { TargetFreeMB = targetMB, Priority = priority, Requester = requester };
    }

    /// <summary>
    /// ͳһ- 
    /// ˵
    /// 
    /// ԭ?
    /// 1. Ӧɾʹõ
    /// 2. ʹͨü
    /// 3. 在使用中的文件应跳过清理
    /// </summary>
    public static class CleanupScheduler
    {
        private static readonly object _globalLock = new object();
        private static readonly HashSet<string> _deletedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        
        // ļʹü - ʹõļ
        private static readonly Dictionary<string, int> _fileUseCount = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        
        private static CancellationTokenSource? _currentCancellation;
        private static bool _isRunning;
        private static CleanupPriority _currentPriority = CleanupPriority.Low;

        /// <summary>ȫɾļϣ?/summary>
        public static HashSet<string> DeletedFiles => _deletedFiles;
        
        /// <summary>ǰʹõļ?/summary>
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

        /// <summary>Sִ?/summary>
        public static bool IsRunning => _isRunning;

        /// <summary>当前清理优先?/summary>
        public static CleanupPriority CurrentPriority => _currentPriority;

        /// <summary>
        /// 请求磁盘清理
        /// </summary>
        /// <param name="request">清理请求</param>
        /// <param name="cacheDirectory">缓存ཕ</param>
        /// <param name="cacheIndex">缓存索引引用</param>
        /// <param name="scheduleIndexSave">保存索引的回?/param>
        /// <returns>实际ɾ的文件数?/returns>
        public static int RequestDiskCleanup(
            CleanupRequest request,
            string cacheDirectory,
            ConcurrentDictionary<string, string> cacheIndex,
            Action scheduleIndexSave)
        {
            lock (_globalLock)
            {
                // иڣȡǰ?
                if (_isRunning && request.Priority <= _currentPriority)
                {
                    Debug.WriteLine($"[CleanupScheduler] ?跳过低优先级请求({request.Priority})，当前运行优先级({_currentPriority})");
                    return 0;
                }

                // ȡ
                if (_isRunning && request.Priority > _currentPriority)
                {
                    _currentCancellation?.Cancel();
                    Debug.WriteLine($"[CleanupScheduler] ?取消低优先级任务，启动高优先?{request.Priority})");
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
        /// ڲ
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

            // أ̰߳
            var files = GetCacheFilesSnapshot(cacheDirectory);
            int totalFiles = files.Count;

            // ͷ?
            long targetFreeBytes = request.TargetBytes ?? (request.TargetFreeMB ?? 0) * 1024L * 1024L;

            // K򣨾ɵ
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
                    Debug.WriteLine($"[CleanupScheduler] 已完成清理");
                    break;
                }

                // 是否达到目标
                if (targetFreeBytes > 0 && currentFreeBytes >= targetFreeBytes)
                    break;

                // 安全ɾļ
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

                // Ȼص
                request.ProgressCallback?.Invoke(deletedCount, totalFiles);

                // Ϣ⿨٣
                if (deletedCount % 10 == 0 && deletedCount > 0)
                {
                    Thread.Sleep(10);
                }
            }

            scheduleIndexSave();
            sw.Stop();

            Debug.WriteLine($"[CleanupScheduler] ?清理完成 [{request.Requester}] - 删除{deletedCount}世?{currentFreeBytes / 1024 / 1024:F1}MB) 耗时:{sw.ElapsedMilliseconds}ms 优先?{request.Priority}");

            return deletedCount;
        }

        /// <summary>
        /// ȫֹɾͻ
        /// 核心规则：不ɾ正在使用的文?
        /// </summary>
        public static bool SafeDeleteFile(string filePath, out long fileSize)
        {
            fileSize = 0;
            string fileName = System.IO.Path.GetFileName(filePath);

            // Ƿ?
            lock (_globalLock)
            {
                if (_deletedFiles.Contains(filePath))
                {
                    Debug.WriteLine($"[FileLife] ?AlreadyDeleted | {fileName}");
                    return false;
                }
            }

            // ?ıļǷʹ?
            bool inUse = IsFileInUse(filePath);
            if (inUse)
            {
                // ?ؼ־ʹõļ
                Debug.WriteLine($"[FileLife] 🔒 SkipInUse | {fileName}");
                return false;
            }

            try
            {
                // ٲļǷ?
                if (!File.Exists(filePath))
                {
                    Debug.WriteLine($"[FileLife] ?NotExists | {fileName}");
                    lock (_globalLock)
                    {
                        _deletedFiles.Add(filePath);
                    }
                    return false;
                }

                // ?ɾǰٴȷ?
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

                // ?ؼ־ʼɾ?
                Debug.WriteLine($"[FileLife] 🗑?Deleting | {fileName}");

                File.Delete(filePath);

                // Ϊ
                lock (_globalLock)
                {
                    _deletedFiles.Add(filePath);
                }

                // ?ؼ־ɾ?
                Debug.WriteLine($"[FileLife] ?Deleted | {fileName}");
                return true;
            }
            catch (FileNotFoundException)
            {
                // ?ؼ־ļɾ?
                Debug.WriteLine($"[FileLife] ?DeletedByOther | {fileName}");
                lock (_globalLock)
                {
                    _deletedFiles.Add(filePath);
                }
                return false;
            }
            catch (IOException ex)
            {
                // ?ؼ־ļռ
                Debug.WriteLine($"[FileLife] ?Locked {ex.Message} | {fileName}");
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                Debug.WriteLine($"[FileLife] ?NoAccess | {fileName}");
                return false;
            }
        }

        /// <summary>
        /// أ̰߳
        /// </summary>
        public static List<string> GetCacheFilesSnapshot(string cacheDirectory)
        {
            try
            {
                return Directory.GetFiles(cacheDirectory)
                    .Where(f => System.IO.Path.GetFileName(f) != "cache_index.txt")
                    .ToList();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CleanupScheduler] ?获取文件列表失败: {ex.Message}");
                return new List<string>();
            }
        }

        /// <summary>
        /// 安全ȡļ信息
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
        /// 文件是否已ɾ
        /// </summary>
        public static bool IsFileDeleted(string filePath)
        {
            lock (_globalLock)
            {
                return _deletedFiles.Contains(filePath);
            }
        }

        /// <summary>
        /// ɾļ¼ڼ¼?
        /// </summary>
        public static void ClearDeletedRecords()
        {
            lock (_globalLock)
            {
                _deletedFiles.Clear();
            }
        }

        #region ʹü

        /// <summary>
        /// ʹã
        /// 在加载缓存文件前调用，防止清理器ɾ正在使用的文?
        /// </summary>
        /// <param name="filePath">ļ跾</param>
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
                
                // ?ؼ־¼ļ
                Debug.WriteLine($"[FileLife] 📌 MarkInUse cnt={newCount} | {System.IO.Path.GetFileName(filePath)}");
            }
        }

        /// <summary>
        /// ͷʹã
        /// 在加载缓存文件完成后调用（无论成功或ʧ?
        /// </summary>
        /// <param name="filePath">ļ跾</param>
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
                        // ?ؼ־ļȫ?
                        Debug.WriteLine($"[FileLife] 📤 ReleaseAll | {System.IO.Path.GetFileName(filePath)}");
                    }
                    else
                    {
                        // ?ؼ־ļü?
                        Debug.WriteLine($"[FileLife] 📤 Release cnt={remaining} | {System.IO.Path.GetFileName(filePath)}");
                    }
                }
                else
                {
                    // ?쳣ͷ˖Pǵļ
                    Debug.WriteLine($"[FileLife] ?ReleaseNotMarked | {System.IO.Path.GetFileName(filePath)}");
                }
            }
        }

        /// <summary>
        /// ļǷʹ
        /// ǰӦô˷?
        /// </summary>
        /// <param name="filePath">ļ跾</param>
        /// <returns>ʹ÷ true</returns>
        public static bool IsFileInUse(string filePath)
        {
            lock (_globalLock)
            {
                return _fileUseCount.ContainsKey(filePath) && _fileUseCount[filePath] > 0;
            }
        }

        /// <summary>
        /// ʹõб
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
    /// 缓存管理?- 򻯰3ܹ
    /// 
    /// 㼶?
    /// L1: ڴ滺棨ǿ50?+ k
    /// L2: Shell + 黺油?
    /// 
    /// Żμض?0%?
    /// 
    /// ?ļڹ?
    /// - 通过 IFileAccessManager 统一管理ļ访问
    /// - ɾʹõ
    /// </summary>
    public class ThumbnailCacheManager : IDisposable
    {
        private readonly string _cacheDirectory;
        private readonly int _thumbnailSize = 60;
        private readonly int _jpegQuality = 85;
        private readonly long _maxCacheSizeBytes = 500 * 1024 * 1024; // 500MB
        private readonly PerformanceLogger _logger = new PerformanceLogger("ThumbnailCache");
        private readonly ConcurrentDictionary<string, string> _cacheIndex = new ConcurrentDictionary<string, string>();
        
        // L1棺ǿÝHʹk
        private readonly ConcurrentDictionary<string, BitmapImage> _memoryCache = new ConcurrentDictionary<string, BitmapImage>();
        private const int MAX_MEMORY_CACHE_SIZE = 50; // 大强引用缓存数量
        
        // L1备份：弱引用缓存（可被GC回收?
        private readonly WeakReferenceCache<string, BitmapImage> _weakCache = new WeakReferenceCache<string, BitmapImage>();
        
        // Shell缓存提供者（L2优先策略?
        private readonly WindowsShellThumbnailProvider _shellProvider;
        
        // ?ļʹ棬ͳһļ
        private readonly IFileAccessManager? _fileAccessManager;
        
        private readonly object _indexLock = new object(); // 索引ļ访问?
        
        // ֵ䣬дͬһ
        private readonly ConcurrentDictionary<string, object> _fileLocks = new ConcurrentDictionary<string, object>();
        private Timer? _indexSaveTimer; // 延迟保存索引的定时器
        private bool _indexDirty = false; // 索引昐要保?
        private bool _disposed = false;

        /// <summary>
        /// ͳ
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
        /// ͳϢ
        /// </summary>
        public CacheStatistics Statistics => _statistics;

        /// <summary>
        /// 构函?
        /// </summary>
        /// <param name="fileAccessManager">ʹ棬ͳһڹ?/param>
        public ThumbnailCacheManager(IFileAccessManager? fileAccessManager = null)
        {
            _cacheDirectory = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SunEyeVision",
                "ThumbnailCache");
            
            // Shellṩ?
            _shellProvider = new WindowsShellThumbnailProvider();
            
            // ?ļʹͳһļڹ?
            _fileAccessManager = fileAccessManager;

            InitializeCache();

            // ʱ?뱣һб仯
            _indexSaveTimer = new Timer(_ =>
            {
                if (_indexDirty)
                {
                    SaveCacheIndex();
                }
            }, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
            
            Debug.WriteLine("[ThumbnailCache] 启动缓存功能");
            Debug.WriteLine($"  L1: 内存缓存(强引用{MAX_MEMORY_CACHE_SIZE}项) + 弱引用");
            Debug.WriteLine($"  L2: Shell缓存优先 + 磁盘缓存补充");
            Debug.WriteLine($"  文件访问管理器: {(_fileAccessManager != null ? "已启用" : "未启用")}");
        }

        /// <summary>
        /// 初始化缓存
        /// </summary>
        private void InitializeCache()
        {
            try
            {
                if (!Directory.Exists(_cacheDirectory))
                {
                    Directory.CreateDirectory(_cacheDirectory);
                    Debug.WriteLine($"[ThumbnailCache] 已创建缓存目录: {_cacheDirectory}");
                }

                // 加载缓存索引
                LoadCacheIndex();
                Debug.WriteLine($"[ThumbnailCache] 缓存初始化完成");
                Debug.WriteLine($"[ThumbnailCache]   缓存目录: {_cacheDirectory}");
                Debug.WriteLine($"[ThumbnailCache]   缩略图尺寸: 60x60");
                Debug.WriteLine($"[ThumbnailCache]   JPEG质量: {_jpegQuality}%");
                Debug.WriteLine($"[ThumbnailCache]   最大缓存: {_maxCacheSizeBytes / 1024 / 1024}MB");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ThumbnailCache] 缓存初始化失败: {ex.Message}");
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
                var indexFile = System.IO.Path.Combine(_cacheDirectory, "cache_index.txt");
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
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ThumbnailCache] 加载缓存索引失败: {ex.Message}");
            }

            _logger.LogOperation("加载缓存索引", sw.Elapsed, $"数量: {count}");
        }

        /// <summary>
        /// 保存缓存索引线程安全
        /// </summary>
        private void SaveCacheIndex()
        {
            lock (_indexLock)
            {
                try
                {
                    var indexFile = System.IO.Path.Combine(_cacheDirectory, "cache_index.txt");
                    var lines = _cacheIndex.Select(kvp => $"{kvp.Key}|{kvp.Value}");
                    File.WriteAllLines(indexFile, lines);
                    _indexDirty = false; // 清除脏标?
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ThumbnailCache] ?保存缓存索引失败: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 安排索引保存（延迟批量保存）
        /// </summary>
        private void ScheduleIndexSave()
        {
            _indexDirty = true; // 标索引要保?
            // ʱ?󱣴棬
        }

        /// <summary>
        /// SΨϣ
        /// </summary>
        private string GetFileHash(string filePath)
        {
            using var md5 = MD5.Create();
            var hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(filePath));
            return BitConverter.ToString(hash).Replace("-", "").Substring(0, 16);
        }

        /// <summary>
        /// ȡļ跾
        /// ע⣺ʹJPEGʽ棬չ̶?jpg
        /// </summary>
        private string GetCacheFilePath(string filePath)
        {
            var hash = GetFileHash(filePath);
            return System.IO.Path.Combine(_cacheDirectory, $"{hash}.jpg");
        }

        /// <summary>
        /// ӵڴ滺棨༶?
        /// </summary>
        public void AddToMemoryCache(string filePath, BitmapImage bitmap)
        {
            if (bitmap != null && !string.IsNullOrEmpty(filePath))
            {
                // L1缓存：强引用（有上限?
                if (_memoryCache.Count >= MAX_MEMORY_CACHE_SIZE)
                {
                    // L1ɵƵL2û?
                    var oldestKey = _memoryCache.Keys.FirstOrDefault();
                    if (oldestKey != null && _memoryCache.TryRemove(oldestKey, out var oldBitmap))
                    {
                        _weakCache.Add(oldestKey, oldBitmap);
                    }
                }
                _memoryCache.TryAdd(filePath, bitmap);
                
                // 同时存入L2弱引用缓存（作为备份?
                _weakCache.Add(filePath, bitmap);
                
                // 缓存添加不输出日?
            }
        }

        /// <summary>
        /// 从内存缓存中移除（用于清理远离可视区域的缩略图）
        /// </summary>
        public void RemoveFromMemoryCache(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return;

            // 从L1强引用缓存移?
            // 缓存移除不输出日?

            // 从L2弱引用缓存移?
            _weakCache.Remove(filePath);
        }

        /// <summary>
        /// Դӻͼ?㻺
        /// L1: ڴ滺棨ǿ + k
        /// L2: Shell优先 + 臻̻
        /// ?ʹ FileAccessManager ļʣɮk
        /// </summary>
        public BitmapImage? TryLoadFromCache(string filePath)
        {
            _statistics.TotalRequests++;

            // L1a: 强引用内存缓?
            if (_memoryCache.TryGetValue(filePath, out var cachedBitmap))
            {
                _statistics.CacheHits++;
                return cachedBitmap;
            }

            // L1b: 弱引用缓?
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
                // ӵڴ滺?
                _memoryCache.TryAdd(filePath, shellThumbnail);
                _weakCache.Add(filePath, shellThumbnail);
                return shellThumbnail;
            }

            // L2b: 飨òԣ
            var cacheFilePath = GetCacheFilePath(filePath);
            if (!_cacheIndex.TryGetValue(filePath, out string? cachedPath) || !File.Exists(cacheFilePath))
            {
                _statistics.CacheMisses++;
                return null;
            }

            // ?ģʹ?FileAccessManager ļʣRAIIģʽ?
            if (_fileAccessManager != null)
            {
                using var scope = _fileAccessManager.CreateAccessScope(cacheFilePath, FileAccessIntent.Read, FileType.CacheFile);
                
                if (!scope.IsGranted)
                {
                    Debug.WriteLine($"[ThumbnailCache] ?文件访问袋? {scope.ErrorMessage} file={System.IO.Path.GetFileName(cacheFilePath)}");
                    _statistics.CacheMisses++;
                    return null;
                }
                
                return LoadCacheFileInternal(filePath, cacheFilePath);
            }
            else
            {
                // ģʽʹ?CleanupSchedulerɷʽ?
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
        /// 从缓存文件加载（内部实现?
        /// ?ؼʹ?StreamSource + ڴ滺壬?UriSource ӳټص¾?
        /// 
        /// ?
        /// - BitmapImage.UriSource nټصģ EndInit() ʱȡ
        /// - 清理器可能在 MarkFileInUse() ?EndInit() 之间ɾļ
        /// - 导致 FileNotFoundException 异常
        /// 
        /// ?
        /// - ͬȡļڴ滺
        /// - 再用 MemoryStream 加载，完全避免文件竞?
        /// </summary>
        private BitmapImage? LoadCacheFileInternal(string filePath, string cacheFilePath)
        {
            try
            {
                // ļ[˫ر?
                if (!File.Exists(cacheFilePath))
                {
                    _cacheIndex.TryRemove(filePath, out _);
                    return null;
                }

                // ?ģͬȡļڴ棬 UriSource ӳټ
                byte[] imageBytes;
                using (var fs = new FileStream(cacheFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 8192, FileOptions.SequentialScan))
                {
                    imageBytes = new byte[fs.Length];
                    int bytesRead = fs.Read(imageBytes, 0, imageBytes.Length);
                    // 读取不完整的情况
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
                bitmap.StreamSource = new MemoryStream(imageBytes);  // 使用内存?
                bitmap.EndInit();
                bitmap.Freeze();

                // ӵڴ滺?
                _memoryCache.TryAdd(filePath, bitmap);
                _weakCache.Add(filePath, bitmap);

                _statistics.CacheHits++;
                // в־߲?

                return bitmap;
            }
            catch (FileNotFoundException)
            {
                // 
                Debug.WriteLine($"[ThumbnailCache] ?缓存文件已删? {System.IO.Path.GetFileName(cacheFilePath)}");
                _cacheIndex.TryRemove(filePath, out _);
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ThumbnailCache] ?缓存加载失败: {ex.Message}");
                _cacheIndex.TryRemove(filePath, out _);
                return null;
            }
        }
        
        /// <summary>
        /// 尝试从Shell缓存加载（L2优先策略?
        /// </summary>
        private BitmapImage? TryLoadFromShellCache(string filePath)
        {
            try
            {
                // 仅从系统缓存ȡ，不生成新的缩略?
                var thumbnail = _shellProvider.GetThumbnail(filePath, _thumbnailSize, cacheOnly: true);
                if (thumbnail != null)
                {
                    // 轍为BitmapImage
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
        /// 将BitmapSource轍为BitmapImage
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
        /// ͼ棨ͬ棬
        /// 适用于需要确保缓存立即可用的场景
        /// </summary>
        public void SaveToCache(string filePath, BitmapSource thumbnail)
        {
            try
            {
                var sw = Stopwatch.StartNew();
                var cacheFilePath = GetCacheFilePath(filePath);

                // 浽ڴ滺棨?
                if (thumbnail is BitmapImage bitmap)
                {
                    _memoryCache.TryAdd(filePath, bitmap);
                }

                // 浽?- 벢д?
                var encoder = new JpegBitmapEncoder();
                encoder.QualityLevel = _jpegQuality;
                encoder.Frames.Add(BitmapFrame.Create(thumbnail));

                var encodeSw = Stopwatch.StartNew();
                using var stream = new FileStream(cacheFilePath, FileMode.Create);
                encoder.Save(stream);
                var cacheSize = stream.Length;
                encodeSw.Stop();

                // 索引（延迟保存）
                var indexSw = Stopwatch.StartNew();
                _cacheIndex.TryAdd(filePath, cacheFilePath);
                ScheduleIndexSave(); // 延迟保存索引，不再立即保?
                indexSw.Stop();

                // 查缓存大小并清理
                CheckCacheSizeAndCleanup();

                // 缓存保存ɹ不输出日?
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ThumbnailCache] ?缓存保存失败: {ex.Message}");
            }
        }

        // 磁盘写入跟踪
        private int _pendingDiskWrites = 0;
        private readonly object _diskWriteLock = new object();

        /// <summary>
        /// ͼ棨Ż棩
        /// - ͬHأ
        /// - 챣棨ִ̨У
        /// </summary>
        /// <remarks>
        /// ƣʾӳٴ +10-35ms  0ms
        /// </remarks>
        public void SaveToCacheNonBlocking(string filePath, BitmapSource thumbnail)
        {
            if (thumbnail == null || string.IsNullOrEmpty(filePath))
                return;

            // 1. 立即更新内存缓存（同步，<1ms?
            if (thumbnail is BitmapImage bitmap)
            {
                AddToMemoryCache(filePath, bitmap);
            }

            // 2. 챣浽÷?
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
        /// 浽棨ڲִ̨߳У
        /// </summary>
        private void SaveToDiskCache(string filePath, BitmapSource thumbnail)
        {
            try
            {
                var cacheFilePath = GetCacheFilePath(filePath);
                
                // ?ȡļרдͻ
                var fileLock = _fileLocks.GetOrAdd(cacheFilePath, _ => new object());
                
                lock (fileLock)
                {
                    // JPEG编码并写入文?
                    var encoder = new JpegBitmapEncoder();
                    encoder.QualityLevel = _jpegQuality;
                    encoder.Frames.Add(BitmapFrame.Create(thumbnail));

                    // ?ʹ FileShare.None 
                    using var stream = new FileStream(cacheFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
                    encoder.Save(stream);
                }

                // 索引（延迟保存）
                _cacheIndex.TryAdd(filePath, cacheFilePath);
                ScheduleIndexSave();

                // 查缓存大?
                CheckCacheSizeAndCleanup();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ThumbnailCache] ?磁盘缓存保存失败: {ex.Message}");
            }
        }

        /// <summary>
        /// ȴдɣӦóʱ?
        /// </summary>
        public async Task WaitForPendingSavesAsync(TimeSpan? timeout = null)
        {
            var deadline = timeout.HasValue ? DateTime.Now.Add(timeout.Value) : DateTime.MaxValue;

            while (Interlocked.CompareExchange(ref _pendingDiskWrites, 0, 0) > 0)
            {
                if (DateTime.Now > deadline)
                {
                    Debug.WriteLine("[ThumbnailCache] 等待磁盘写入超时");
                    return;
                }
                await Task.Delay(10);
            }
        }

        /// <summary>
        /// 챣ͼ
        /// </summary>
        public async Task SaveToCacheAsync(string filePath, BitmapSource thumbnail)
        {
            await Task.Run(() => SaveToCache(filePath, thumbnail));
        }

        /// <summary>
        /// 黺Сʹͳ
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
                    Debug.WriteLine($"[ThumbnailCache] ?缓存超限 ({totalSize / 1024 / 1024:F1}MB)，开始清?..");

                    // 计算要释放的空间（清理到80%?
                    var targetSize = (long)(_maxCacheSizeBytes * 0.8);
                    var bytesToFree = totalSize - targetSize;

                    // 使用统一调度器执行清?
                    var request = CleanupRequest.FromBytes(bytesToFree, CleanupPriority.Normal, "CheckCacheSizeAndCleanup");
                    var deletedCount = CleanupScheduler.RequestDiskCleanup(request, _cacheDirectory, _cacheIndex, ScheduleIndexSave);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ThumbnailCache] 缓存清理失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 清除有缓?
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

                Debug.WriteLine("[ThumbnailCache] 缓存已清除");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ThumbnailCache] 清除缓存失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 预生成缩略图缓存（用于批量加载优化）
        /// </summary>
        public async Task PreGenerateCacheAsync(string[] filePaths, Func<string, BitmapSource?> loadFunc)
        {
            Debug.WriteLine($"[ThumbnailCache] ========== 预生成缓存开?==========");
            Debug.WriteLine($"[ThumbnailCache] 待生成数? {filePaths.Length}");

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

                // 强制保存确保不丢失数据
                if (_indexDirty)
                {
                    SaveCacheIndex();
                }

                _memoryCache.Clear(); // 清理内存缓存
                _shellProvider?.Dispose(); // 释放Shell提供?
                _disposed = true;
                Debug.WriteLine("[ThumbnailCache] 资源已释放");
            }
        }

        /// <summary>
        /// ȡ缓存信息
        /// </summary>
        public string GetCacheInfo()
        {
            try
            {
                var files = Directory.GetFiles(_cacheDirectory)
                    .Where(f => System.IO.Path.GetFileName(f) != "cache_index.txt")
                    .ToList();

                var totalSize = files.Sum(f => new FileInfo(f).Length);
                var fileSize = totalSize / 1024.0 / 1024.0;
                var shellStats = _shellProvider.GetStatistics();

                return $"L1:{_memoryCache.Count}?L2弱引?{_weakCache.AliveCount}?磁盘:{files.Count}?{fileSize:F1}MB 命中?{_statistics.HitRate:F1}% | {shellStats}";
            }
            catch
            {
                return "缓存信息ȡʧ";
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
                // ?P1Żʽ̻
                ProgressiveCleanup(100); // ࠇ释放100MB
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
        /// ?P1Żʽڴʹͳ
        /// 棬δ¿?
        /// </summary>
        /// <param name="targetFreeMB">ࠇ释放空间(MB)</param>
        /// <param name="progressCallback">Ȼص(已删除数? 总数?</param>
        public void ProgressiveCleanup(int targetFreeMB, Action<int, int>? progressCallback = null)
        {
            // ݵԴȷ?
            // RespondToMemoryPressure 会根?isCritical 传入不同?targetFreeMB
            // 100MB = 危险级别(Critical), 50MB = 高压?High)
            var priority = targetFreeMB >= 100 ? CleanupPriority.Critical : CleanupPriority.High;

            _ = Task.Run(() =>
            {
                try
                {
                    // 使用统一调度器执行清?
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
                    Debug.WriteLine($"[ThumbnailCache] ?渐进清理失败: {ex.Message}");
                }
            });
        }
    }
}
