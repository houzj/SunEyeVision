using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace SunEyeVision.UI.Controls.Rendering
{
    /// <summary>
    /// 加载优先级枚举
    /// </summary>
    public enum LoadPriority
    {
        /// <summary>关键优先级 - 首张图片立即显示</summary>
        Critical = 0,
        /// <summary>高优先级 - 可见区域图片</summary>
        High = 1,
        /// <summary>中等优先级 - 预加载区域</summary>
        Medium = 2,
        /// <summary>低优先级 - 后台空闲加载</summary>
        Low = 3
    }

    /// <summary>
    /// 加载任务
    /// </summary>
    public class LoadTask : IComparable<LoadTask>
    {
        public int Index { get; set; }
        public LoadPriority Priority { get; set; }
        public DateTime EnqueueTime { get; set; }
        public string FilePath { get; set; } = string.Empty;

        public int CompareTo(LoadTask? other)
        {
            if (other == null) return 1;
            // 优先级高的排前面（数值小的优先级高）
            int priorityCompare = Priority.CompareTo(other.Priority);
            if (priorityCompare != 0) return priorityCompare;
            // 同优先级按索引排序
            return Index.CompareTo(other.Index);
        }
    }

    /// <summary>
    /// 优先级缩略图加载器 - 单一加载系统，消除双系统协调问题
    /// 
    /// 核心设计：
    /// 1. 使用优先级队列替代双加载系统
    /// 2. 无pause/resume机制，通过优先级自然调度
    /// 3. 首屏和滚动加载统一处理
    /// </summary>
    public class PriorityThumbnailLoader : IDisposable
    {
        #region 字段

        // 缩略图加载委托
        private Func<string, int, BitmapImage?>? _loadThumbnailFunc;
        private Action<string, BitmapImage?>? _addToCacheAction;
        
        // ★ 实时获取可视范围的委托（解决可视范围缓存过时问题）
        private Func<(int first, int last)>? _getVisibleRangeFunc;

        // 优先级队列（线程安全）
        private readonly PriorityQueue<LoadTask, int> _loadQueue = new PriorityQueue<LoadTask, int>();
        private readonly HashSet<int> _queuedIndices = new HashSet<int>(); // 已入队索引
        private readonly HashSet<int> _loadedIndices = new HashSet<int>(); // 已加载索引
        private readonly object _queueLock = new object();

        // 任务管理
        private Task? _loadTask;
        private CancellationTokenSource? _cancellationTokenSource;
        private int _activeTaskCount = 0;
        private readonly SemaphoreSlim _concurrencySemaphore;

        // 集合引用（支持批量操作集合）
        private BatchObservableCollection<ImageInfo>? _imageCollection;

        // 性能监控
        private readonly PerformanceLogger _logger = new PerformanceLogger("PriorityLoader");
        private readonly Queue<long> _decodeTimes = new Queue<long>();
        private int _dynamicConcurrency = 4;
        private DateTime _lastConcurrencyAdjust = DateTime.MinValue;
        private const int MIN_CONCURRENCY = 2;
        private const int MAX_CONCURRENCY = 8;

        // 滚动状态追踪
        private int _lastFirstVisible = -1;
        private int _lastLastVisible = -1;
        private int _scrollDirection = 0; // 1=向右, -1=向左, 0=静止

        // UI批量更新
        private readonly ConcurrentQueue<UIUpdateRequest> _uiUpdateQueue = new ConcurrentQueue<UIUpdateRequest>();
        private DispatcherTimer? _uiUpdateTimer;

        // 动态质量
        private bool _useLowQuality = false;

        // 可视区域加载监控
        private DateTime? _visibleAreaLoadStartTime = null;
        private int _visibleAreaCount = 0;
        private int _loadedInVisibleArea = 0;

        // 清理阈值
        private const int CLEANUP_THRESHOLD = 50;
        private const int CLEANUP_KEEP_MARGIN = 30;

        private bool _disposed = false;

        #endregion

        #region 属性

        /// <summary>是否使用低质量缩略图（快速滚动时）</summary>
        public bool UseLowQuality
        {
            get => _useLowQuality;
            set
            {
                if (_useLowQuality != value)
                {
                    _useLowQuality = value;
                    Debug.WriteLine($"[PriorityLoader] 质量模式: {(value ? "低质量(快速)" : "高质量")}");
                }
            }
        }

        /// <summary>当前活动任务数</summary>
        public int ActiveTaskCount => _activeTaskCount;

        /// <summary>队列中待加载数量</summary>
        public int PendingCount
        {
            get
            {
                lock (_queueLock)
                {
                    return _loadQueue.Count;
                }
            }
        }

        /// <summary>已加载数量</summary>
        public int LoadedCount
        {
            get
            {
                lock (_queueLock)
                {
                    return _loadedIndices.Count;
                }
            }
        }

        #endregion

        #region 事件

        /// <summary>可视区域加载完成事件</summary>
        public event Action<int, TimeSpan>? VisibleAreaLoadingCompleted;

        #endregion

        #region 构造函数

        public PriorityThumbnailLoader()
        {
            _concurrencySemaphore = new SemaphoreSlim(_dynamicConcurrency);
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 首屏加载 - 三级优先级架构
        /// 1. 首屏25张：Critical/High 立即加载
        /// 2. 预加载区域25张：Medium 预加载
        /// 3. 其余全部：Low 后台预缓存
        /// </summary>
        public async Task LoadInitialScreenAsync(
            string[] fileNames,
            BatchObservableCollection<ImageInfo> imageCollection,
            Action<int>? onFirstImageLoaded = null)
        {
            _imageCollection = imageCollection;

            // 计算可视区域大小
            int visibleCount = CalculateVisibleCount();
            int loadCount = Math.Min(visibleCount, fileNames.Length);

            // 设置可视区域范围（首屏加载时从0开始）
            _lastFirstVisible = 0;
            _lastLastVisible = loadCount - 1;
            _visibleAreaLoadStartTime = DateTime.Now;
            _visibleAreaCount = loadCount;
            _loadedInVisibleArea = 0;

            // ===== 三级优先级架构：所有图片都入队 =====

            // 第一批：首屏立即加载（Critical + High）
            int firstScreenCount = Math.Min(25, fileNames.Length); // 首屏固定25张
            for (int i = 0; i < firstScreenCount; i++)
            {
                var priority = i == 0 ? LoadPriority.Critical : LoadPriority.High;
                EnqueueLoadTask(i, fileNames[i], priority);
            }

            // 第二批：预加载缓冲区（Medium）
            int prefetchStart = firstScreenCount;
            int prefetchEnd = Math.Min(50, fileNames.Length);
            for (int i = prefetchStart; i < prefetchEnd; i++)
            {
                EnqueueLoadTask(i, fileNames[i], LoadPriority.Medium);
            }

            // 第三批：后台预缓存（Low）← ★ 关键优化：其余所有图片
            for (int i = prefetchEnd; i < fileNames.Length; i++)
            {
                EnqueueLoadTask(i, fileNames[i], LoadPriority.Low);
            }

            Debug.WriteLine($"[PriorityLoader] 首屏加载策略:");
            Debug.WriteLine($"  Critical/High: [0-{firstScreenCount - 1}] ({firstScreenCount}张)");
            Debug.WriteLine($"  Medium: [{prefetchStart}-{prefetchEnd - 1}] ({prefetchEnd - prefetchStart}张)");
            Debug.WriteLine($"  Low: [{prefetchEnd}-{fileNames.Length - 1}] ({fileNames.Length - prefetchEnd}张)");
            Debug.WriteLine($"  总数: {fileNames.Length}");

            // 启动后台加载任务
            StartLoading();

            // 等待首张图片加载完成
            if (fileNames.Length > 0)
            {
                await WaitForIndexLoadedAsync(0, timeoutMs: 5000);
                onFirstImageLoaded?.Invoke(0);
            }
        }

        /// <summary>
        /// 更新可视区域 - 滚动时调用
        /// </summary>
        public void UpdateVisibleRange(int firstVisible, int lastVisible, int totalCount)
        {
            var sw = Stopwatch.StartNew();

            if (totalCount <= 0 || _imageCollection == null) return;

            // 检测滚动方向
            if (_lastFirstVisible >= 0)
            {
                _scrollDirection = firstVisible > _lastFirstVisible ? 1 :
                                   firstVisible < _lastFirstVisible ? -1 : 0;
            }

            _lastFirstVisible = firstVisible;
            _lastLastVisible = lastVisible;

            // ★ 方案C：在检查可视区域状态前，先同步该区域内的状态
            // 解决 _loadedIndices 与实际 Thumbnail 不一致的问题
            int fixedCount = SyncVisibleAreaState(firstVisible, lastVisible);

            // 记录可视区域加载开始
            int visibleCount = lastVisible - firstVisible + 1;
            int needLoadCount = 0;
            int alreadyLoadedCount = 0;
            int alreadyQueuedCount = 0;
            
            lock (_queueLock)
            {
                for (int i = firstVisible; i <= lastVisible; i++)
                {
                    if (_loadedIndices.Contains(i))
                        alreadyLoadedCount++;
                    else if (_queuedIndices.Contains(i))
                        alreadyQueuedCount++;
                    else
                        needLoadCount++;
                }
            }

            // 可视区域加载状态监控（静默处理）

            if (needLoadCount > 0 && _visibleAreaLoadStartTime == null)
            {
                _visibleAreaLoadStartTime = DateTime.Now;
                _visibleAreaCount = visibleCount;
                _loadedInVisibleArea = 0;
            }

            // ★ 关键日志：记录实际入队的索引
            var newEnqueuedIndices = new List<int>();

            // 入队可视区域图片（High优先级）
            for (int i = firstVisible; i <= lastVisible; i++)
            {
                if (i >= 0 && i < totalCount)
                {
                    var filePath = _imageCollection[i].FilePath;
                    if (EnqueueLoadTaskWithLog(i, filePath, LoadPriority.High))
                    {
                        newEnqueuedIndices.Add(i);
                    }
                }
            }

            // 预加载相邻区域（Medium优先级）
            int prefetchMargin = 5;
            int prefetchStart = Math.Max(0, firstVisible - prefetchMargin);
            int prefetchEnd = Math.Min(totalCount - 1, lastVisible + prefetchMargin);

            for (int i = prefetchStart; i <= prefetchEnd; i++)
            {
                if (i < firstVisible || i > lastVisible)
                {
                    if (i >= 0 && i < totalCount)
                    {
                        var filePath = _imageCollection[i].FilePath;
                        EnqueueLoadTask(i, filePath, LoadPriority.Medium);
                    }
                }
            }

            // 新任务入队（静默处理）

            // 清理远离可视区域的缩略图
            CleanupOutOfRangeThumbnails(firstVisible, lastVisible, totalCount);

            // 启动加载
            StartLoading();

            sw.Stop();
        }

        /// <summary>
        /// 同步可视区域内的状态（解决 _loadedIndices 与 Thumbnail 不一致）
        /// ★ 方案C核心：滚动时主动修复状态
        /// </summary>
        private int SyncVisibleAreaState(int firstVisible, int lastVisible)
        {
            if (_imageCollection == null) return 0;

            int fixedCount = 0;
            lock (_queueLock)
            {
                for (int i = firstVisible; i <= lastVisible && i < _imageCollection.Count; i++)
                {
                    // 检查：_loadedIndices 标记已加载，但 Thumbnail 实际为 null
                    if (_loadedIndices.Contains(i) && _imageCollection[i].Thumbnail == null)
                    {
                        _loadedIndices.Remove(i);
                        _queuedIndices.Remove(i);
                        fixedCount++;
                    }
                }
            }

            return fixedCount;
        }

        /// <summary>
        /// 清空加载状态（加载新文件夹时调用）
        /// </summary>
        public void ClearState()
        {
            lock (_queueLock)
            {
                _loadQueue.Clear();
                _queuedIndices.Clear();
                _loadedIndices.Clear();
            }

            while (_uiUpdateQueue.TryDequeue(out _)) { }

            _lastFirstVisible = -1;
            _lastLastVisible = -1;
            _visibleAreaLoadStartTime = null;
            _visibleAreaCount = 0;
            _loadedInVisibleArea = 0;

            // 状态清空不输出日志
        }

        /// <summary>
        /// 标记指定索引为未加载状态（容器回收时调用）
        /// ★ 关键：解决状态不一致问题
        /// </summary>
        public void MarkAsUnloaded(int index)
        {
            lock (_queueLock)
            {
                _loadedIndices.Remove(index);
                _queuedIndices.Remove(index);
            }
        }

        /// <summary>
        /// 同步已加载索引与实际缩略图状态
        /// ★ 关键：解决缓存清理后状态不一致问题
        /// 当内存压力导致缓存被清理时，_loadedIndices 可能与实际 Thumbnail 状态不同步
        /// </summary>
        public void SyncLoadedIndicesWithActualThumbnails()
        {
            if (_imageCollection == null) return;

            var toRemove = new List<int>();
            lock (_queueLock)
            {
                foreach (var index in _loadedIndices)
                {
                    if (index < 0 || index >= _imageCollection.Count)
                    {
                        toRemove.Add(index);
                    }
                    else if (_imageCollection[index].Thumbnail == null)
                    {
                        // Thumbnail已被GC清理，但索引还在已加载集合中
                        toRemove.Add(index);
                    }
                }

                foreach (var index in toRemove)
                {
                    _loadedIndices.Remove(index);
                    _queuedIndices.Remove(index);
                }
            }

            if (toRemove.Count > 0)
            {
                Debug.WriteLine($"[PriorityLoader] ✓ 状态同步完成 - 清除无效索引:{toRemove.Count}个");
            }
        }

        /// <summary>
        /// 设置图像集合引用
        /// </summary>
        public void SetImageCollection(BatchObservableCollection<ImageInfo> collection)
        {
            _imageCollection = collection;
        }

        /// <summary>
        /// 设置缩略图加载函数（委托模式）
        /// </summary>
        public void SetLoadThumbnailFunc(Func<string, int, BitmapImage?> loadFunc, Action<string, BitmapImage?>? addToCacheAction = null, Func<(int first, int last)>? getVisibleRangeFunc = null)
        {
            _loadThumbnailFunc = loadFunc;
            _addToCacheAction = addToCacheAction;
            _getVisibleRangeFunc = getVisibleRangeFunc;
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 计算可视区域能显示多少图片
        /// </summary>
        private int CalculateVisibleCount()
        {
            // 默认返回一个合理值，实际应该从UI获取
            return 15;
        }

        /// <summary>
        /// 入队加载任务
        /// </summary>
        private void EnqueueLoadTask(int index, string filePath, LoadPriority priority)
        {
            lock (_queueLock)
            {
                if (_loadedIndices.Contains(index) || _queuedIndices.Contains(index))
                    return;

                var task = new LoadTask
                {
                    Index = index,
                    FilePath = filePath,
                    Priority = priority,
                    EnqueueTime = DateTime.Now
                };

                // 优先级数值越小越优先
                _loadQueue.Enqueue(task, (int)priority);
                _queuedIndices.Add(index);
            }
        }

        /// <summary>
        /// 入队加载任务（带日志返回值）
        /// ★ 诊断用：返回是否成功入队
        /// </summary>
        private bool EnqueueLoadTaskWithLog(int index, string filePath, LoadPriority priority)
        {
            lock (_queueLock)
            {
                // ★ 关键诊断：检查跳过原因
                if (_loadedIndices.Contains(index))
                {
                    // 检查是否真的已加载（Thumbnail不为null）
                    if (_imageCollection != null && index < _imageCollection.Count)
                    {
                        var thumbnail = _imageCollection[index].Thumbnail;
                        if (thumbnail == null)
                        {
                            // ★ 状态不一致：_loadedIndices标记已加载，但实际Thumbnail为null
                            Debug.WriteLine($"[PriorityLoader] ⚠ 状态不一致 index={index} 在loadedIndices但Thumbnail=null，修复中...");
                            _loadedIndices.Remove(index);
                            // 继续入队
                        }
                        else
                        {
                            return false; // 确实已加载，跳过
                        }
                    }
                    else
                    {
                        return false; // 已加载，跳过
                    }
                }

                if (_queuedIndices.Contains(index))
                    return false; // 已在队列中，跳过

                var task = new LoadTask
                {
                    Index = index,
                    FilePath = filePath,
                    Priority = priority,
                    EnqueueTime = DateTime.Now
                };

                // 优先级数值越小越优先
                _loadQueue.Enqueue(task, (int)priority);
                _queuedIndices.Add(index);
                return true;
            }
        }

        /// <summary>
        /// 启动后台加载任务
        /// </summary>
        private void StartLoading()
        {
            if (_loadTask == null || _loadTask.IsCompleted)
            {
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = new CancellationTokenSource();
                _loadTask = ProcessLoadQueueAsync(_cancellationTokenSource.Token);
            }

            // 启动UI更新定时器
            InitializeUIUpdateTimer();
        }

        /// <summary>
        /// 处理加载队列
        /// </summary>
        private async Task ProcessLoadQueueAsync(CancellationToken cancellationToken)
        {
            var sw = Stopwatch.StartNew();
            int processedCount = 0;

            // 开始处理队列不输出日志

            while (!cancellationToken.IsCancellationRequested)
            {
                LoadTask? task = null;
                lock (_queueLock)
                {
                    if (_loadQueue.Count > 0)
                    {
                        _loadQueue.TryDequeue(out task, out _);
                        if (task != null)
                        {
                            _queuedIndices.Remove(task.Index);
                        }
                    }
                }

                if (task == null)
                {
                    // 队列为空，等待一下再检查
                    await Task.Delay(50, cancellationToken);
                    continue;
                }

                // 并发限制
                await _concurrencySemaphore.WaitAsync(cancellationToken);

                _ = Task.Run(async () =>
                {
                    Interlocked.Increment(ref _activeTaskCount);
                    var taskSw = Stopwatch.StartNew();
                    try
                    {
                        await LoadSingleThumbnailAsync(task, cancellationToken);
                        taskSw.Stop();

                        // 调整并发数
                        AdjustConcurrencyIfNeeded(taskSw.ElapsedMilliseconds);

                        Interlocked.Increment(ref processedCount);
                    }
                    catch (OperationCanceledException)
                    {
                        // 取消时不做处理
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[PriorityLoader] 加载异常 index={task.Index}: {ex.Message}");
                    }
                    finally
                    {
                        _concurrencySemaphore.Release();
                        Interlocked.Decrement(ref _activeTaskCount);
                    }
                }, cancellationToken);

                // 进度不输出日志
            }

            sw.Stop();
            // 队列处理完成不输出日志
        }

        /// <summary>
        /// 加载单个缩略图
        /// </summary>
        private async Task LoadSingleThumbnailAsync(LoadTask task, CancellationToken cancellationToken)
        {
            if (_imageCollection == null || task.Index < 0 || task.Index >= _imageCollection.Count)
            {
                Debug.WriteLine($"[PriorityLoader] ⚠ 跳过加载 - 无效参数 index={task.Index} collection={_imageCollection != null} count={_imageCollection?.Count ?? 0}");
                return;
            }

            var imageInfo = _imageCollection[task.Index];

            // 检查是否已加载
            lock (_queueLock)
            {
                if (_loadedIndices.Contains(task.Index))
                {
                    // ★ 诊断：检查是否真的已加载
                    if (imageInfo.Thumbnail == null)
                    {
                        Debug.WriteLine($"[PriorityLoader] ⚠ 状态不一致 - index={task.Index} 在loadedIndices但Thumbnail=null，继续加载");
                        _loadedIndices.Remove(task.Index);
                    }
                    else
                    {
                        return;
                    }
                }

                _loadedIndices.Add(task.Index);
            }

            if (imageInfo.Thumbnail != null)
            {
                Debug.WriteLine($"[PriorityLoader] ⚠ 跳过 - index={task.Index} Thumbnail已存在");
                return;
            }

            var sw = Stopwatch.StartNew();
            int thumbnailSize = _useLowQuality ? 40 : 60;

            try
            {
                // 后台线程加载
                var thumbnail = await Task.Run(() => LoadThumbnailOptimized(task.FilePath, thumbnailSize));
                sw.Stop();

                if (thumbnail == null)
                {
                    Debug.WriteLine($"[PriorityLoader] ✗ 加载返回null index={task.Index} file={task.FilePath} 耗时:{sw.ElapsedMilliseconds}ms");
                    return;
                }

                // ★ 关键诊断：检查缩略图有效性
                if (thumbnail.Width <= 0 || thumbnail.Height <= 0)
                {
                    Debug.WriteLine($"[PriorityLoader] ✗ 无效缩略图 index={task.Index} size={thumbnail.Width}x{thumbnail.Height} 耗时:{sw.ElapsedMilliseconds}ms");
                    return;
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    Debug.WriteLine($"[PriorityLoader] ⚠ 取消 - index={task.Index} 已被取消");
                    return;
                }

                // 加入UI更新队列
                _uiUpdateQueue.Enqueue(new UIUpdateRequest
                {
                    Index = task.Index,
                    Thumbnail = thumbnail,
                    ImageInfo = imageInfo,
                    FilePath = task.FilePath
                });

                // 更新可视区域加载计数
                if (task.Index >= _lastFirstVisible && task.Index <= _lastLastVisible)
                {
                    Interlocked.Increment(ref _loadedInVisibleArea);
                }

                // 可视区域内的高优先级加载完成（静默处理）
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PriorityLoader] ✗ 加载异常 index={task.Index}: {ex.Message}");
            }
        }

        /// <summary>
        /// 加载缩略图（使用委托调用实际加载方法）
        /// </summary>
        private BitmapImage? LoadThumbnailOptimized(string filePath, int size)
        {
            try
            {
                if (_loadThumbnailFunc != null)
                {
                    return _loadThumbnailFunc(filePath, size);
                }
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PriorityLoader] 缩略图加载失败: {filePath} - {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 动态调整并发数
        /// </summary>
        private void AdjustConcurrencyIfNeeded(long decodeTimeMs)
        {
            lock (_decodeTimes)
            {
                _decodeTimes.Enqueue(decodeTimeMs);
                while (_decodeTimes.Count > 20)
                    _decodeTimes.Dequeue();

                if ((DateTime.Now - _lastConcurrencyAdjust).TotalMilliseconds < 500)
                    return;

                _lastConcurrencyAdjust = DateTime.Now;
                if (_decodeTimes.Count < 10) return;

                var avgTime = _decodeTimes.Average();

                if (avgTime < 50 && _dynamicConcurrency < MAX_CONCURRENCY)
                {
                    _dynamicConcurrency = Math.Min(MAX_CONCURRENCY, _dynamicConcurrency + 1);
                    Debug.WriteLine($"[PriorityLoader] ↑ 增加并发: {_dynamicConcurrency}");
                }
                else if (avgTime > 150 && _dynamicConcurrency > MIN_CONCURRENCY)
                {
                    _dynamicConcurrency = Math.Max(MIN_CONCURRENCY, _dynamicConcurrency - 1);
                    Debug.WriteLine($"[PriorityLoader] ↓ 减少并发: {_dynamicConcurrency}");
                }
            }
        }

        /// <summary>
        /// 等待指定索引加载完成
        /// </summary>
        private async Task WaitForIndexLoadedAsync(int index, int timeoutMs = 5000)
        {
            var sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                lock (_queueLock)
                {
                    if (_loadedIndices.Contains(index))
                        return;
                }
                await Task.Delay(20);
            }
            // 等待超时不输出日志
        }

        /// <summary>
        /// 清理远离可视区域的缩略图
        /// </summary>
        private void CleanupOutOfRangeThumbnails(int firstVisible, int lastVisible, int totalCount)
        {
            lock (_queueLock)
            {
                if (_loadedIndices.Count < CLEANUP_THRESHOLD)
                    return;
            }

            var cleanupStart = Math.Max(0, firstVisible - CLEANUP_KEEP_MARGIN);
            var cleanupEnd = Math.Min(totalCount - 1, lastVisible + CLEANUP_KEEP_MARGIN);

            var indicesToRemove = new List<int>();
            lock (_queueLock)
            {
                foreach (var index in _loadedIndices)
                {
                    if (index < cleanupStart || index > cleanupEnd)
                        indicesToRemove.Add(index);
                }
            }

            if (indicesToRemove.Count > 0 && _imageCollection != null)
            {
                Application.Current?.Dispatcher.BeginInvoke(() =>
                {
                    foreach (var index in indicesToRemove)
                    {
                        if (index >= 0 && index < _imageCollection.Count)
                        {
                            _imageCollection[index].Thumbnail = null;
                        }

                        lock (_queueLock)
                        {
                            _loadedIndices.Remove(index);
                        }
                    }
                    // 清理不输出日志
                }, DispatcherPriority.Background);
            }
        }

        /// <summary>
        /// 初始化UI更新定时器
        /// </summary>
        private void InitializeUIUpdateTimer()
        {
            if (_uiUpdateTimer == null)
            {
                _uiUpdateTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(30)
                };
                _uiUpdateTimer.Tick += ProcessUIUpdates;
                _uiUpdateTimer.Start();
            }
        }

        /// <summary>
        /// 批量处理UI更新
        /// </summary>
        private void ProcessUIUpdates(object? sender, EventArgs e)
        {
            if (_uiUpdateQueue.Count == 0 || _imageCollection == null)
                return;

            var updates = new List<UIUpdateRequest>();
            while (_uiUpdateQueue.TryDequeue(out var request))
            {
                updates.Add(request);
            }

            if (updates.Count == 0) return;

            // ★ 获取实时可视范围（优先使用委托，否则使用缓存值）
            int firstVis = _lastFirstVisible;
            int lastVis = _lastLastVisible;
            if (_getVisibleRangeFunc != null)
            {
                var (f, l) = _getVisibleRangeFunc();
                if (f >= 0 && l >= 0)
                {
                    firstVis = f;
                    lastVis = l;
                }
            }

            // 分离可视区域和后台更新
            var visibleUpdates = updates.Where(u =>
                u.Index >= firstVis && u.Index <= lastVis).ToList();

            // 可视区域立即更新
            int updatedCount = 0;
            int skippedCount = 0;
            foreach (var update in visibleUpdates)
            {
                if (update.Index < _imageCollection.Count &&
                    _imageCollection[update.Index] == update.ImageInfo)
                {
                    // 验证缩略图有效性
                    if (update.Thumbnail != null && update.Thumbnail.Width > 0 && update.Thumbnail.Height > 0)
                    {
                        update.ImageInfo.Thumbnail = update.Thumbnail;
                        _addToCacheAction?.Invoke(update.FilePath, update.Thumbnail);
                        updatedCount++;
                    }
                    else
                    {
                        skippedCount++;
                    }
                }
                else
                {
                    skippedCount++;
                }
            }

            // 后台更新
            var backgroundUpdates = updates.Except(visibleUpdates).ToList();
            int bgUpdatedCount = 0;
            foreach (var update in backgroundUpdates)
            {
                if (update.Index < _imageCollection.Count &&
                    _imageCollection[update.Index] == update.ImageInfo)
                {
                    _imageCollection[update.Index].Thumbnail = update.Thumbnail;
                    _addToCacheAction?.Invoke(update.FilePath, update.Thumbnail);
                    bgUpdatedCount++;
                }
            }

            // 检查可视区域是否加载完成
            if (_visibleAreaLoadStartTime.HasValue && _loadedInVisibleArea >= _visibleAreaCount)
            {
                var duration = DateTime.Now - _visibleAreaLoadStartTime.Value;
                VisibleAreaLoadingCompleted?.Invoke(_loadedInVisibleArea, duration);
                _visibleAreaLoadStartTime = null;
                _loadedInVisibleArea = 0;
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (!_disposed)
            {
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();
                _concurrencySemaphore?.Dispose();
                _uiUpdateTimer?.Stop();

                lock (_queueLock)
                {
                    _loadQueue.Clear();
                    _queuedIndices.Clear();
                    _loadedIndices.Clear();
                }

                _disposed = true;
            }
        }

        #endregion

        #region 内部类

        private class UIUpdateRequest
        {
            public int Index { get; set; }
            public BitmapImage? Thumbnail { get; set; }
            public ImageInfo? ImageInfo { get; set; }
            public string FilePath { get; set; } = string.Empty;
        }

        #endregion
    }
}
