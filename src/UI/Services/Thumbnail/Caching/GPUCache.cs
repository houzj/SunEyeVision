using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using SunEyeVision.UI.Services.Thumbnail.Caching;

namespace SunEyeVision.UI.Services.Thumbnail.Caching
{
    /// <summary>
    /// GPU缓存管理?- 提供内存缓存和磁盘持久化缓存
    /// </summary>
    public class GPUCache
    {
        private readonly int _maxMemoryCacheSize;
        private readonly int _maxDiskCacheSizeMB;
        private readonly string _cachePath;
        private readonly object _lockObj = new object();

        // 内存缓存（LRU?
        private readonly LinkedList<CacheEntry> _lruList = new LinkedList<CacheEntry>();
        private readonly Dictionary<string, LinkedListNode<CacheEntry>> _memoryCache = new Dictionary<string, LinkedListNode<CacheEntry>>();

        // 磁盘缓存标记
        private readonly HashSet<string> _diskCacheKeys = new HashSet<string>();

        private static GPUCache? _instance;

        /// <summary>
        /// 获取单例实例
        /// </summary>
        public static GPUCache Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new GPUCache();
                }
                return _instance;
            }
        }

        public GPUCache()
        {
            _maxMemoryCacheSize = 100; // 内存缓存100?
            _maxDiskCacheSizeMB = 500; // 磁盘缓存500MB

            // 初始化磁盘缓存路?
            _cachePath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SunEyeVision", "ThumbnailCache");

            if (!Directory.Exists(_cachePath))
            {
                Directory.CreateDirectory(_cachePath);
            }

            Debug.WriteLine($"[GPUCache] 初始化完?- 内存缓存:{_maxMemoryCacheSize}? 磁盘缓存:{_maxDiskCacheSizeMB}MB, 路径:{_cachePath}");

            // 启动时清理过期缓?
            Task.Run(() => CleanupOldCache());
        }

        /// <summary>
        /// 获取或加载缩略图（三级缓存：内存 -> 磁盘 -> 文件?
        /// </summary>
        public BitmapImage? GetOrLoadThumbnail(string filePath, int size, Func<string, int, BitmapImage?> loader)
        {
            var cacheKey = GetCacheKey(filePath, size);
            // 高频操作不输出日?

            // L1: 内存缓存
            var cached = GetFromMemoryCache(cacheKey);
            if (cached != null)
            {
                return cached;
            }

            // L2: 磁盘缓存
            cached = GetFromDiskCache(cacheKey);
            if (cached != null)
            {
                // 加入内存缓存
                AddToMemoryCache(cacheKey, cached);
                return cached;
            }

            // L3: 从文件加?
            var bitmap = loader(filePath, size);
            if (bitmap != null)
            {
                // 加入内存缓存
                AddToMemoryCache(cacheKey, bitmap);

                // 异步保存到磁盘缓?
                _ = Task.Run(() => SaveToDiskCache(cacheKey, bitmap));
            }

            return bitmap;
        }

        /// <summary>
        /// 从内存缓存获?
        /// </summary>
        private BitmapImage? GetFromMemoryCache(string cacheKey)
        {
            lock (_lockObj)
            {
                if (_memoryCache.TryGetValue(cacheKey, out var node))
                {
                    // LRU: 移到最?
                    _lruList.Remove(node);
                    _lruList.AddFirst(node);
                    return node.Value.Bitmap;
                }
                return null;
            }
        }

        /// <summary>
        /// 添加到内存缓?
        /// </summary>
        private void AddToMemoryCache(string cacheKey, BitmapImage bitmap)
        {
            lock (_lockObj)
            {
                if (_memoryCache.ContainsKey(cacheKey))
                {
                    // 已存在，更新
                    var node = _memoryCache[cacheKey];
                    _lruList.Remove(node);
                    node.Value.Bitmap = bitmap;
                    _lruList.AddFirst(node);
                }
                else
                {
                    // 新增
                    var entry = new CacheEntry(cacheKey, bitmap);
                    var node = _lruList.AddFirst(entry);
                    _memoryCache[cacheKey] = node;
                }

                // 超出容量，移除最旧的
                while (_lruList.Count > _maxMemoryCacheSize)
                {
                    var lastNode = _lruList.Last;
                    if (lastNode != null)
                    {
                        _memoryCache.Remove(lastNode.Value.CacheKey);
                        _lruList.RemoveLast();
                    }
                }
            }
        }

        /// <summary>
        /// 从磁盘缓存获?
        /// </summary>
        private BitmapImage? GetFromDiskCache(string cacheKey)
        {
            var cacheFile = System.IO.Path.Combine(_cachePath, $"{cacheKey}.cache");

            if (!File.Exists(cacheFile))
            {
                return null;
            }

            try
            {
                using var stream = File.OpenRead(cacheFile);
                var decoder = BitmapDecoder.Create(
                    stream,
                    BitmapCreateOptions.PreservePixelFormat,
                    BitmapCacheOption.OnLoad);

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = stream;
                bitmap.EndInit();
                bitmap.Freeze();

                return bitmap;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GPUCache] 磁盘缓存读取失败: {ex.Message}");
                // 删除损坏的缓存文?
                try
                {
                    File.Delete(cacheFile);
                }
                catch { }
            }

            return null;
        }

        /// <summary>
        /// 保存到磁盘缓?
        /// </summary>
        private void SaveToDiskCache(string cacheKey, BitmapImage bitmap)
        {
            var cacheFile = System.IO.Path.Combine(_cachePath, $"{cacheKey}.cache");

            try
            {
                using var stream = File.Create(cacheFile);
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));
                encoder.Save(stream);

                lock (_lockObj)
                {
                    _diskCacheKeys.Add(cacheKey);
                }

                Debug.WriteLine($"[GPUCache] ?已保存到磁盘缓存: {System.IO.Path.GetFileName(cacheFile)}");

                // 随机触发清理?%概率?
                if (new Random().Next(100) < 5)
                {
                    CleanupOldCache();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GPUCache] 磁盘缓存保存失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 清理旧的磁盘缓存（LRU策略?
        /// </summary>
        private void CleanupOldCache()
        {
            try
            {
                var files = Directory.GetFiles(_cachePath, "*.cache")
                    .Select(f => new FileInfo(f))
                    .OrderBy(f => f.LastAccessTime)
                    .ToList();

                long totalSize = files.Sum(f => f.Length);
                long targetSize = _maxDiskCacheSizeMB * 1024L * 1024L;

                int removedCount = 0;
                while (totalSize > targetSize && files.Count > 0)
                {
                    var file = files[0];
                    try
                    {
                        file.Delete();
                        totalSize -= file.Length;
                        removedCount++;

                        lock (_lockObj)
                        {
                            _diskCacheKeys.Remove(System.IO.Path.GetFileNameWithoutExtension(file.Name));
                        }
                    }
                    catch { }
                    files.RemoveAt(0);
                }

                if (removedCount > 0)
                {
                    Debug.WriteLine($"[GPUCache] 已清理磁盘缓存 - 删除了 {removedCount} 个文件");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GPUCache] 磁盘缓存清理失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 清除所有缓?
        /// </summary>
        public void ClearAll()
        {
            lock (_lockObj)
            {
                _memoryCache.Clear();
                _lruList.Clear();
                _diskCacheKeys.Clear();
            }

            // 删除磁盘缓存
            try
            {
                var files = Directory.GetFiles(_cachePath, "*.cache");
                foreach (var file in files)
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch { }
                }

                Debug.WriteLine($"[GPUCache] 已清除所有缓存 - 删除了 {files.Length} 个磁盘缓存文件");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GPUCache] 清除缓存失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 生成缓存?
        /// </summary>
        private string GetCacheKey(string filePath, int size)
        {
            var input = $"{filePath}_{size}_{File.GetLastWriteTime(filePath).Ticks}";
            var hash = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(input);
            var hashBytes = hash.ComputeHash(bytes);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }

        /// <summary>
        /// 缓存条目
        /// </summary>
        private class CacheEntry
        {
            public string CacheKey { get; }
            public BitmapImage Bitmap { get; set; }

            public CacheEntry(string cacheKey, BitmapImage bitmap)
            {
                CacheKey = cacheKey;
                Bitmap = bitmap;
            }
        }
    }
}
