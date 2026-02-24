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
using SunEyeVision.UI.Services.Performance;
using SunEyeVision.UI.Services.Thumbnail;
using SunEyeVision.UI.Views.Controls.Panels;

namespace SunEyeVision.UI.Services.Thumbnail
{
    /// <summary>
    /// 加载优先级枚�?
    /// </summary>
    public enum LoadPriority
    {
        /// <summary>关键优先�?- 首张图片立即显示</summary>
        Critical = 0,
        /// <summary>高优先级 - 可见区域图片</summary>
        High = 1,
        /// <summary>中等优先�?- 预加载区�?/summary>
        Medium = 2,
        /// <summary>低优先级 - 后台空闲加载</summary>
        Low = 3,
        /// <summary>空闲优先�?- 仅在空闲时加载，可随时取�?/summary>
        Idle = 4
    }

    /// <summary>
    /// 缓冲区域类型
    /// </summary>
    public enum BufferZone
    {
        /// <summary>活跃区域 - 当前可视范围，最高优先级</summary>
        Active,
        /// <summary>预加载区�?- 可视范围边缘，中等优先级</summary>
        Prefetch,
        /// <summary>后备区域 - 远离可视范围，空闲时加载</summary>
        Reserve
    }

    /// <summary>
    /// 滚动类型枚举
    /// </summary>
    public enum ScrollType
    {
        /// <summary>停止滚动</summary>
        Stopped,
        /// <summary>慢速滚�?/summary>
        Slow,
        /// <summary>快速滚�?/summary>
        Fast,
        /// <summary>超快滚动（跳过预加载�?/summary>
        UltraFast
    }

    /// <summary>
    /// 动态加载策略配�?
    /// �?单一数据源架构：所有值基于实际可视数量动态计�?
    /// 核心设计：限制总入队数量，避免1000+张图片全入队
    /// 动态范�?= 可视区域 + 缓冲区域 + 预测区域
    /// </summary>
    public class DynamicLoadingPolicy
    {
        /// <summary>实际可视区域大小（由外部设置，基于GetVisibleRange()�?/summary>
        public int ActualVisibleSize { get; private set; } = 10; // 默认值，首屏加载前会被更�?
        
        /// <summary>缓冲区域倍数（可视区�?× 此值）</summary>
        /// <remarks>�?方案B优化：从1.0降到0.5，减少首屏入队数�?/remarks>
        public double BufferMultiplier { get; set; } = 0.5;
        
        /// <summary>预测区域倍数（可视区�?× 此值）</summary>
        /// <remarks>�?方案B优化：从3.0降到1.0，大幅减少首屏入队数�?/remarks>
        public double PrefetchMultiplier { get; set; } = 1.0;
        
        /// <summary>快速滚动阈值（�?秒）</summary>
        public double FastScrollThreshold { get; set; } = 5.0;
        
        /// <summary>超快滚动阈值（�?秒）</summary>
        public double UltraFastScrollThreshold { get; set; } = 15.0;
        
        /// <summary>最大预加载范围（限制总入队数量）</summary>
        public int MaxPrefetchRange { get; set; } = 200;
        
        /// <summary>
        /// �?单一数据源：设置实际可视数量，所有派生值自动更�?
        /// </summary>
        public void SetActualVisibleSize(int visibleSize)
        {
            ActualVisibleSize = Math.Max(1, visibleSize);
        }
        
        /// <summary>可视区域大小（派生自ActualVisibleSize�?/summary>
        public int VisibleSize => ActualVisibleSize;
        
        /// <summary>缓冲区域大小（派生：可视区域 × BufferMultiplier�?/summary>
        public int BufferSize => (int)(ActualVisibleSize * BufferMultiplier);
        
        /// <summary>预测区域大小（派生：可视区域 × PrefetchMultiplier�?/summary>
        public int PrefetchSize => (int)(ActualVisibleSize * PrefetchMultiplier);
        
        /// <summary>
        /// 计算动态范围总数
        /// 动态范�?= 可视区域 + 缓冲区域 + 预测区域
        /// </summary>
        public int TotalDynamicRange => VisibleSize + BufferSize + PrefetchSize;
        
        /// <summary>
        /// 根据滚动类型获取预加载倍数
        /// </summary>
        public double GetPrefetchMultiplier(ScrollType scrollType)
        {
            return scrollType switch
            {
                ScrollType.Stopped => 1.0,
                ScrollType.Slow => 1.5,
                ScrollType.Fast => 2.0,
                ScrollType.UltraFast => 0.5, // 超快滚动时减少预加载
                _ => 1.0
            };
        }
    }

    /// <summary>
    /// 缓冲区域配置
    /// �?单一数据源架构：所有值基于实际可视数量动态派�?
    /// </summary>
    public class BufferZoneConfig
    {
        private int _actualVisibleSize = 10; // 由外部设�?
        
        /// <summary>
        /// �?单一数据源：设置实际可视数量
        /// </summary>
        public void SetActualVisibleSize(int visibleSize)
        {
            _actualVisibleSize = Math.Max(1, visibleSize);
        }
        
        /// <summary>活跃区域大小 = 实际可视范围（派生）</summary>
        public int ActiveSize => _actualVisibleSize;
        
        /// <summary>预加载区域大小（单侧，派生：可视区域�?0%�?/summary>
        public int PrefetchSize => Math.Max(3, _actualVisibleSize / 2);
        
        /// <summary>后备区域大小（单侧，派生：可视区域的3倍）</summary>
        public int ReserveSize => _actualVisibleSize * 3;
        
        /// <summary>快速滚动时预加载区域倍增因子</summary>
        public double FastScrollPrefetchMultiplier { get; set; } = 2.0;
        
        /// <summary>静止时后备区域激活延�?ms)</summary>
        public int ReserveActivationDelay { get; set; } = 2000;
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
            // 同优先级按索引排�?
            return Index.CompareTo(other.Index);
        }
    }

    /// <summary>
    /// 优先级缩略图加载�?- 单一加载系统，消除双系统协调问题
    /// 
    /// 核心设计�?
    /// 1. 使用优先级队列替代双加载系统
    /// 2. 无pause/resume机制，通过优先级自然调�?
    /// 3. 首屏和滚动加载统一处理
    /// </summary>
    public class PriorityThumbnailLoader : IDisposable
    {
        #region 字段

        // 缩略图加载委�?
        private Func<string, int, bool, BitmapImage?>? _loadThumbnailFunc;
        private Action<string, BitmapImage?>? _addToCacheAction;
        
        // �?实时获取可视范围的委托（解决可视范围缓存过时问题�?
        private Func<(int first, int last)>? _getVisibleRangeFunc;

        // 优先级队列（线程安全�?
        private readonly PriorityQueue<LoadTask, int> _loadQueue = new PriorityQueue<LoadTask, int>();
        private readonly HashSet<int> _queuedIndices = new HashSet<int>(); // 已入队索�?
        private readonly HashSet<int> _loadedIndices = new HashSet<int>(); // 已加载索引（Thumbnail已成功设置）
        private readonly HashSet<int> _decodingIndices = new HashSet<int>(); // �?新增：正在解码中的索�?
        private readonly object _queueLock = new object();

        // 任务管理
        private Task? _loadTask;
        private CancellationTokenSource? _cancellationTokenSource;
        private int _activeTaskCount = 0;
        
        // �?P0优化：分离的高优先级线程�?
        private readonly SemaphoreSlim _highPrioritySemaphore;
        private readonly SemaphoreSlim _normalPrioritySemaphore;
        private const int HIGH_PRIORITY_THREADS = 4; // �?协调GPU解码能力：与WicGpuDecoder并发限制(4)匹配，避免队列积�?

        // 集合引用（支持批量操作集合）
        private BatchObservableCollection<ImageInfo>? _imageCollection;

        // 性能监控
        private readonly PerformanceLogger _logger = new PerformanceLogger("PriorityLoader");
        private readonly Queue<long> _decodeTimes = new Queue<long>();
        private int _dynamicConcurrency = 4;
        private DateTime _lastConcurrencyAdjust = DateTime.MinValue;
        private DateTime _startupTime = DateTime.MinValue; // �?新增：启动时间，用于首屏延迟调整
        private const int MIN_CONCURRENCY = 4; // �?优化：提高最小并发度，避免过�?
        private const int MAX_CONCURRENCY = 8;
        private const int STARTUP_GRACE_PERIOD_MS = 3000; // �?新增：首屏并发调整延�?�?

        // �?P1优化：内存压力级�?
        private MemoryPressureMonitor.PressureLevel _currentMemoryPressure = MemoryPressureMonitor.PressureLevel.Normal;

        // 滚动状态追�?
        private int _lastFirstVisible = -1;
        private int _lastLastVisible = -1;
        private int _scrollDirection = 0; // 1=向右, -1=向左, 0=静止
        private double _scrollVelocity = 0; // �?P1优化：滚动速度（项/秒）
        private DateTime _lastScrollTime = DateTime.MinValue;
        private bool _isFastScrolling = false;
        private ScrollType _currentScrollType = ScrollType.Stopped; // �?动态范围：滚动类型

        // �?动态范围加载策�?
        private readonly DynamicLoadingPolicy _dynamicPolicy = new DynamicLoadingPolicy();

        // �?P2优化：缓冲区域管�?
        private readonly BufferZoneConfig _bufferConfig = new BufferZoneConfig();
        private DateTime _lastScrollStopTime = DateTime.MinValue;
        private int _currentPrefetchSize = 10;
        private DispatcherTimer? _reserveActivationTimer;

        // 缓冲区域范围缓存
        private (int start, int end) _activeZone = (0, 0);
        private (int start, int end) _prefetchZoneLeft = (0, 0);
        private (int start, int end) _prefetchZoneRight = (0, 0);
        private (int start, int end) _reserveZoneLeft = (0, 0);
        private (int start, int end) _reserveZoneRight = (0, 0);

        // UI批量更新
        private readonly ConcurrentQueue<UIUpdateRequest> _uiUpdateQueue = new ConcurrentQueue<UIUpdateRequest>();
        private DispatcherTimer? _uiUpdateTimer;

        // 动态质�?
        private bool _useLowQuality = false;

        // 可视区域加载监控
        private DateTime? _visibleAreaLoadStartTime = null;
        private int _visibleAreaCount = 0;
        private int _loadedInVisibleArea = 0;

        // 清理阈�?
        private const int CLEANUP_THRESHOLD = 50;
        private const int CLEANUP_KEEP_MARGIN = 30;

        private bool _disposed = false;

        #endregion

        #region 属�?

        /// <summary>是否使用低质量缩略图（快速滚动时�?/summary>
        public bool UseLowQuality
        {
            get => _useLowQuality;
            set
            {
                if (_useLowQuality != value)
                {
                    _useLowQuality = value;
                    Debug.WriteLine($"[PriorityLoader] Quality mode: {(value ? "Low(Fast)" : "High")}");
                }
            }
        }

        /// <summary>当前活动任务�?/summary>
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

        /// <summary>已加载数�?/summary>
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

        /// <summary>是否快速滚动中</summary>
        public bool IsFastScrolling => _isFastScrolling;

        /// <summary>当前缓冲区域配置（只读）</summary>
        public BufferZoneConfig BufferConfig => _bufferConfig;

        #endregion

        #region 事件

        /// <summary>可视区域加载完成事件</summary>
        public event Action<int, TimeSpan>? VisibleAreaLoadingCompleted;

        #endregion

        #region 构造函�?

        public PriorityThumbnailLoader()
        {
            // �?P0+P2优化：计算总线程数并分离线程池
            int totalThreads = Math.Max(8, Environment.ProcessorCount);
            int normalThreads = Math.Max(4, totalThreads - HIGH_PRIORITY_THREADS);
            
            // 初始化分离的线程�?
            _highPrioritySemaphore = new SemaphoreSlim(HIGH_PRIORITY_THREADS);
            _normalPrioritySemaphore = new SemaphoreSlim(normalThreads);
            
            Debug.WriteLine($"[PriorityLoader] �?线程池分�?- 高优先级:{HIGH_PRIORITY_THREADS}, 普�?{normalThreads}, 总计:{totalThreads}");
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 取消并重置所有加载任�?
        /// �?首屏优化：在加载新目录前清理旧任务，防止资源竞争
        /// </summary>
        public void CancelAndReset()
        {
            var diagSw = Stopwatch.StartNew();
            
            // 取消当前加载任务
            _cancellationTokenSource?.Cancel();
            diagSw.Stop();
            long cancelMs = diagSw.ElapsedMilliseconds;
            
            // 等待一小段时间让任务响应取�?
            diagSw.Restart();
            Thread.Sleep(10);
            diagSw.Stop();
            long sleepMs = diagSw.ElapsedMilliseconds;
            
            // 清空队列
            diagSw.Restart();
            lock (_queueLock)
            {
                _loadQueue.Clear();
                _queuedIndices.Clear();
                _decodingIndices.Clear();
                _loadedIndices.Clear();
            }
            diagSw.Stop();
            long clearQueueMs = diagSw.ElapsedMilliseconds;
            
            // 清空UI更新队列
            diagSw.Restart();
            while (_uiUpdateQueue.TryDequeue(out _)) { }
            diagSw.Stop();
            long clearUiQueueMs = diagSw.ElapsedMilliseconds;
            
            // 重置状�?
            _loadedInVisibleArea = 0;
            _visibleAreaCount = 0;
            
            Debug.WriteLine($"[Diagnostics] CancelAndReset details: Cancel={cancelMs}ms, Sleep={sleepMs}ms, ClearQueue={clearQueueMs}ms, ClearUiQueue={clearUiQueueMs}ms");
            Debug.WriteLine("[PriorityLoader] Canceled old tasks and reset state.");
        }

        /// <summary>
        /// 首屏加载 - 单一数据源架�?
        /// �?核心优化：基于实际可视数量动态计算加载范�?
        /// 
        /// 数据流（统一）：
        /// 1. GetVisibleRange() �?实际可视数量（如10张）
        /// 2. 可视区域加载 �?入队10张（Critical/High�?
        /// 3. 可视区域完成判断 �?检�?0张是否全部加�?
        /// </summary>
        public async Task LoadInitialScreenAsync(
            string[] fileNames,
            BatchObservableCollection<ImageInfo> imageCollection,
            Action<int>? onFirstImageLoaded = null)
        {
            // ===== �?诊断计时：总计时开�?=====
            var totalDiagSw = Stopwatch.StartNew();
            
            // ===== �?首屏优化：取消之前的所有加载任�?=====
            // 防止旧任务与Critical任务竞争资源
            var stepSw = Stopwatch.StartNew();
            CancelAndReset();
            stepSw.Stop();
            long cancelAndResetMs = stepSw.ElapsedMilliseconds;
            
            _imageCollection = imageCollection;
            
            // �?新增：记录启动时间，用于首屏并发调整延迟
            _startupTime = DateTime.Now;

            // ===== �?内存预判：评估系统内存压�?=====
            stepSw.Restart();
            var memoryInfo = GC.GetGCMemoryInfo();
            long availableMemoryMB = memoryInfo.TotalAvailableMemoryBytes / (1024 * 1024);
            long memoryLoadPercent = 100 - (memoryInfo.TotalAvailableMemoryBytes * 100 / memoryInfo.MemoryLoadBytes);
            bool isMemoryConstrained = availableMemoryMB < 2000 || memoryLoadPercent > 70;
            stepSw.Stop();
            long memoryCheckMs = stepSw.ElapsedMilliseconds;
            
            if (isMemoryConstrained)
            {
                Debug.WriteLine($"[PriorityLoader] �?内存紧张 - 可用:{availableMemoryMB}MB 负载:{memoryLoadPercent}% 启用保守策略");
            }

            // ===== �?单一数据源：获取实际可视数量 =====
            stepSw.Restart();
            int visibleCount = CalculateVisibleCount();
            
            // �?如果委托返回无效值，等待UI布局完成后重�?
            if (visibleCount <= 1 && _getVisibleRangeFunc != null)
            {
                // UI可能还没布局完成，延迟获�?
                await Task.Delay(50);
                visibleCount = CalculateVisibleCount();
            }
            stepSw.Stop();
            long calcVisibleMs = stepSw.ElapsedMilliseconds;
            
            // 确保至少加载1�?
            visibleCount = Math.Max(1, visibleCount);
            int loadCount = Math.Min(visibleCount, fileNames.Length);

            // �?内存预判：动态调整预加载数量
            stepSw.Restart();
            int bufferSize, prefetchSize, totalEnqueueCount;
            if (isMemoryConstrained)
            {
                // 内存紧张：减少预加载
                bufferSize = Math.Min(_dynamicPolicy.BufferSize / 2, fileNames.Length - visibleCount);
                prefetchSize = Math.Min(_dynamicPolicy.PrefetchSize / 3, fileNames.Length - visibleCount - bufferSize);
                totalEnqueueCount = Math.Min(visibleCount + bufferSize + prefetchSize, fileNames.Length);
                Debug.WriteLine($"[PriorityLoader] 保守策略 - 入队:{totalEnqueueCount} (可视:{visibleCount} 缓冲:{bufferSize} 预取:{prefetchSize})");
            }
            else
            {
                // 正常策略
                int visibleSize = Math.Min(_dynamicPolicy.VisibleSize, fileNames.Length);
                bufferSize = Math.Min(_dynamicPolicy.BufferSize, fileNames.Length - visibleSize);
                prefetchSize = Math.Min(_dynamicPolicy.PrefetchSize, fileNames.Length - visibleSize - bufferSize);
                totalEnqueueCount = Math.Min(visibleSize + bufferSize + prefetchSize, fileNames.Length);
            }
            stepSw.Stop();
            long policyCalcMs = stepSw.ElapsedMilliseconds;

            // 设置可视区域范围（首屏加载时�?开始）
            _lastFirstVisible = 0;
            _lastLastVisible = loadCount - 1;
            _visibleAreaLoadStartTime = DateTime.Now;
            _visibleAreaCount = loadCount;  // �?统一：使用实际可视数�?
            _loadedInVisibleArea = 0;

            // ===== �?诊断日志：前置准备阶�?=====
            totalDiagSw.Stop();
            Debug.WriteLine($"[诊断] CancelAndReset: {cancelAndResetMs}ms");
            Debug.WriteLine($"[诊断] 内存预判: {memoryCheckMs}ms");
            Debug.WriteLine($"[诊断] 计算可视数量: {calcVisibleMs}ms, visibleCount={visibleCount}");
            Debug.WriteLine($"[诊断] 策略计算: {policyCalcMs}ms");
            Debug.WriteLine($"[诊断] �?前置准备总计: {totalDiagSw.ElapsedMilliseconds}ms");

            // ===== �?P0优化：Critical任务直接同步执行，消除线程池冷启动开销 =====
            // 首张图片不入队，直接同步解码
            long criticalDecodeMs = 0;
            long uiUpdateMs = 0;
            long callbackMs = 0;
            
            if (fileNames.Length > 0 && imageCollection.Count > 0)
            {
                string firstFileName = System.IO.Path.GetFileName(fileNames[0]);
                
                Debug.WriteLine($"[PriorityLoader] �?Critical同步加载开�?| file={firstFileName}");
                
                // �?P0优化：直接同步执行，无线程池调度开销
                // �?优先级感知：Critical任务使用高优先级GPU解码
                stepSw.Restart();
                var thumbnail = LoadThumbnailOptimized(fileNames[0], 60, isHighPriority: true);
                stepSw.Stop();
                criticalDecodeMs = stepSw.ElapsedMilliseconds;
                
                if (thumbnail != null)
                {
                    // �?关键优化：使用Normal优先级，避免被后台任务饿�?
                    // Loaded优先级太低，在大量后台任务时会延�?-2�?
                    stepSw.Restart();
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        imageCollection[0].Thumbnail = thumbnail;
                    }, DispatcherPriority.Normal);
                    stepSw.Stop();
                    uiUpdateMs = stepSw.ElapsedMilliseconds;
                    
                    // 添加到缓�?
                    _addToCacheAction?.Invoke(fileNames[0], thumbnail);
                    
                    // 标记已加�?
                    lock (_queueLock)
                    {
                        _loadedIndices.Add(0);
                    }
                    
                    Interlocked.Increment(ref _loadedInVisibleArea);
                    
                    // �?修复：使用实际解码时间判断加载方�?
                    string loadMethod = criticalDecodeMs < 10 ? "缓存命中" : "GPU解码";
                    Debug.WriteLine($"[PriorityLoader] �?Critical同步加载完成 | {loadMethod} | 解码耗时:{criticalDecodeMs}ms | file={firstFileName}");
                    
                    // 立即回调通知首张图片已加�?
                    stepSw.Restart();
                    onFirstImageLoaded?.Invoke(0);
                    stepSw.Stop();
                    callbackMs = stepSw.ElapsedMilliseconds;
                    
                    // ===== �?诊断日志：Critical阶段 =====
                    Debug.WriteLine($"[诊断] Critical解码: {criticalDecodeMs}ms");
                    Debug.WriteLine($"[诊断] UI更新(InvokeAsync): {uiUpdateMs}ms");
                    Debug.WriteLine($"[诊断] 回调执行: {callbackMs}ms");
                }
                else
                {
                    Debug.WriteLine($"[PriorityLoader] �?Critical同步加载失败 | file={firstFileName}");
                }
            }

            // 第一批：可视区域剩余部分（High�? 从index=1开�?
            for (int i = 1; i < visibleCount; i++)
            {
                EnqueueLoadTask(i, fileNames[i], LoadPriority.High);
            }

            // 第二批：缓冲区域（Medium�? 预加载边�?
            for (int i = visibleCount; i < visibleCount + bufferSize && i < fileNames.Length; i++)
            {
                EnqueueLoadTask(i, fileNames[i], LoadPriority.Medium);
            }

            // 第三批：预测区域（Low�? 后台预加�?
            for (int i = visibleCount + bufferSize; i < totalEnqueueCount; i++)
            {
                EnqueueLoadTask(i, fileNames[i], LoadPriority.Low);
            }

            Debug.WriteLine($"[PriorityLoader] �?单源数据架构 - 首屏加载:");
            Debug.WriteLine($"  实际可视数量: {visibleCount}�?(来自GetVisibleRange)");
            Debug.WriteLine($"  可视区域: [0同步] + [1-{visibleCount - 1}] ({visibleCount}�?");
            Debug.WriteLine($"  缓冲区域(Medium): [{visibleCount}-{visibleCount + bufferSize - 1}] ({bufferSize}�?");
            Debug.WriteLine($"  预测区域(Low): [{visibleCount + bufferSize}-{totalEnqueueCount - 1}] ({prefetchSize}�?");
            Debug.WriteLine($"  �?入队总数: {totalEnqueueCount - 1} / {fileNames.Length} (首张已同步加�?");

            // 启动后台加载任务（处理剩余任务）
            StartLoading();
        }

        /// <summary>
        /// 更新可视区域 - 滚动时调�?
        /// �?单一数据源架构：使用实际可视数量进行判断
        /// </summary>
        public void UpdateVisibleRange(int firstVisible, int lastVisible, int totalCount)
        {
            var sw = Stopwatch.StartNew();

            if (totalCount <= 0 || _imageCollection == null) return;

            // �?单一数据源：更新所有配置的实际可视数量
            int actualVisibleCount = lastVisible - firstVisible + 1;
            _dynamicPolicy.SetActualVisibleSize(actualVisibleCount);
            _bufferConfig.SetActualVisibleSize(actualVisibleCount);

            // �?P2优化：更新滚动状态并计算缓冲区域
            UpdateScrollState(firstVisible, lastVisible, totalCount);

            // �?方案C：在检查可视区域状态前，先同步该区域内的状�?
            int fixedCount = SyncVisibleAreaState(firstVisible, lastVisible);

            // �?单一数据源：使用实际可视数量进行状态监�?
            int visibleCount = actualVisibleCount;  // 统一使用实际�?
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

            // 可视区域加载状态监控（静默处理�?

            if (needLoadCount > 0 && _visibleAreaLoadStartTime == null)
            {
                _visibleAreaLoadStartTime = DateTime.Now;
                _visibleAreaCount = visibleCount;  // �?统一：使用实际可视数�?
                _loadedInVisibleArea = 0;
            }

            // �?关键日志：记录实际入队的索引
            var newEnqueuedIndices = new List<int>();

            // �?P2优化：根据缓冲区域入队任�?
            // 活跃区域（可视范围）- High/Critical优先�?
            for (int i = firstVisible; i <= lastVisible; i++)
            {
                if (i >= 0 && i < totalCount)
                {
                    var filePath = _imageCollection[i].FilePath;
                    var priority = i == firstVisible ? LoadPriority.Critical : LoadPriority.High;
                    if (EnqueueLoadTaskWithLog(i, filePath, priority))
                    {
                        newEnqueuedIndices.Add(i);
                    }
                }
            }

            // 预加载区�?- Medium优先�?
            // 根据滚动方向优先预加载一�?
            if (_scrollDirection >= 0) // 向右滚动或静�?
            {
                for (int i = _prefetchZoneRight.start; i <= _prefetchZoneRight.end && i < totalCount; i++)
                {
                    if (i >= 0)
                    {
                        var filePath = _imageCollection[i].FilePath;
                        EnqueueLoadTask(i, filePath, LoadPriority.Medium);
                    }
                }
            }
            if (_scrollDirection <= 0) // 向左滚动或静�?
            {
                for (int i = _prefetchZoneLeft.start; i <= _prefetchZoneLeft.end && i < totalCount; i++)
                {
                    if (i >= 0)
                    {
                        var filePath = _imageCollection[i].FilePath;
                        EnqueueLoadTask(i, filePath, LoadPriority.Medium);
                    }
                }
            }

            // 清理远离可视区域的缩略图
            CleanupOutOfRangeThumbnails(firstVisible, lastVisible, totalCount);

            // 启动加载
            StartLoading();

            sw.Stop();
        }

        /// <summary>
        /// �?P0优化：立即更新可视区域（不等防抖�?
        /// �?动态范围：超快滚动时只入队可视区域，不预加�?
        /// </summary>
        public void UpdateVisibleRangeImmediate(int firstVisible, int lastVisible, int totalCount)
        {
            if (totalCount <= 0 || _imageCollection == null) return;

            // 同步可视区域状�?
            SyncVisibleAreaState(firstVisible, lastVisible);

            // �?动态范围：超快滚动时只入队可视区域
            if (_currentScrollType == ScrollType.UltraFast)
            {
                // 超快滚动：只入队首张（Critical�?
                if (firstVisible >= 0 && firstVisible < totalCount)
                {
                    var filePath = _imageCollection[firstVisible].FilePath;
                    EnqueueLoadTask(firstVisible, filePath, LoadPriority.Critical);
                }
            }
            else
            {
                // 正常/快速滚动：入队可视区域（High优先级）
                for (int i = firstVisible; i <= lastVisible; i++)
                {
                    if (i >= 0 && i < totalCount)
                    {
                        var filePath = _imageCollection[i].FilePath;
                        var priority = i == firstVisible ? LoadPriority.Critical : LoadPriority.High;
                        EnqueueLoadTask(i, filePath, priority);
                    }
                }
            }

            // 启动加载
            StartLoading();
        }

        /// <summary>
        /// 同步可视区域内的状态（解决 _loadedIndices �?Thumbnail 不一致）
        /// �?方案C核心：滚动时主动修复状�?
        /// </summary>
        private int SyncVisibleAreaState(int firstVisible, int lastVisible)
        {
            // �?修复竞态条件：捕获集合引用
            var imageCollection = _imageCollection;
            if (imageCollection == null) return 0;

            int fixedCount = 0;
            lock (_queueLock)
            {
                for (int i = firstVisible; i <= lastVisible && i < imageCollection.Count; i++)
                {
                    // 检查：_loadedIndices 标记已加载，�?Thumbnail 实际�?null
                    if (_loadedIndices.Contains(i) && imageCollection[i].Thumbnail == null)
                    {
                        _loadedIndices.Remove(i);
                        _queuedIndices.Remove(i);
                        _decodingIndices.Remove(i);  // �?新增：清理解码中索引
                        fixedCount++;
                    }
                    
                    // �?新增：检查解码超时（超过5秒仍在解码中，可能卡住）
                    if (_decodingIndices.Contains(i) && imageCollection[i].Thumbnail == null)
                    {
                        // 如果已经在解码中�?Thumbnail 为空，检查是否需要重新加�?
                        // 这里不做处理，让自然超时机制处理
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
                _decodingIndices.Clear();  // �?新增：清理解码中索引
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
        /// �?关键：解决状态不一致问�?
        /// </summary>
        public void MarkAsUnloaded(int index)
        {
            lock (_queueLock)
            {
                _loadedIndices.Remove(index);
                _queuedIndices.Remove(index);
                _decodingIndices.Remove(index);  // �?新增：清理解码中索引
            }
        }

        /// <summary>
        /// 同步已加载索引与实际缩略图状�?
        /// �?关键：解决缓存清理后状态不一致问�?
        /// 当内存压力导致缓存被清理时，_loadedIndices 可能与实�?Thumbnail 状态不同步
        /// </summary>
        public void SyncLoadedIndicesWithActualThumbnails()
        {
            // �?修复竞态条件：捕获集合引用
            var imageCollection = _imageCollection;
            if (imageCollection == null) return;

            var toRemove = new List<int>();
            lock (_queueLock)
            {
                foreach (var index in _loadedIndices)
                {
                    if (index < 0 || index >= imageCollection.Count)
                    {
                        toRemove.Add(index);
                    }
                    else if (imageCollection[index].Thumbnail == null)
                    {
                        // Thumbnail已被GC清理，但索引还在已加载集合中
                        toRemove.Add(index);
                    }
                }

                foreach (var index in toRemove)
                {
                    _loadedIndices.Remove(index);
                    _queuedIndices.Remove(index);
                    _decodingIndices.Remove(index);  // �?新增：清理解码中索引
                }
            }

            if (toRemove.Count > 0)
            {
                Debug.WriteLine($"[PriorityLoader] State sync completed - cleared invalid indices: {toRemove.Count}");
            }
        }

        /// <summary>
        /// 设置图像集合引用
        /// </summary>
        public void SetImageCollection(BatchObservableCollection<ImageInfo> collection)
        {
            _imageCollection = collection;
            
            // �?关键修复：当集合被清空时，同时清除委托引�?
            // 防止后台任务通过委托访问已清空的集合
            if (collection == null)
            {
                _getVisibleRangeFunc = null;
                Debug.WriteLine("[SetImageCollection] �?集合已清空，同时清除_getVisibleRangeFunc委托");
            }
        }

        /// <summary>
        /// 设置缩略图加载函数（委托模式�?
        /// </summary>
        public void SetLoadThumbnailFunc(Func<string, int, bool, BitmapImage?> loadFunc, Action<string, BitmapImage?>? addToCacheAction = null, Func<(int first, int last)>? getVisibleRangeFunc = null)
        {
            _loadThumbnailFunc = loadFunc;
            _addToCacheAction = addToCacheAction;
            _getVisibleRangeFunc = getVisibleRangeFunc;
        }

        /// <summary>
        /// �?P1优化：根据内存压力动态调整并发度
        /// �?ImagePreviewControl 中连接内存监控事件时调用
        /// </summary>
        public void SetMemoryPressure(MemoryPressureMonitor.PressureLevel level)
        {
            _currentMemoryPressure = level;
            
            // 根据压力调整并发�?
            _dynamicConcurrency = level switch
            {
                MemoryPressureMonitor.PressureLevel.Normal => 8,
                MemoryPressureMonitor.PressureLevel.Moderate => 4,
                MemoryPressureMonitor.PressureLevel.High => 2,
                MemoryPressureMonitor.PressureLevel.Critical => 1,
                _ => 4
            };
            
            // 高压力时取消低优先级任务
            if (level >= MemoryPressureMonitor.PressureLevel.High)
            {
                CancelLowPriorityTasks();
                _useLowQuality = true;
            }
            else
            {
                _useLowQuality = false;
            }
            
            Debug.WriteLine($"[PriorityLoader] 内存压力:{level} �?并发�?{_dynamicConcurrency} 低质�?{_useLowQuality}");
        }

        /// <summary>
        /// �?P1优化：取消低优先级任�?
        /// </summary>
        private void CancelLowPriorityTasks()
        {
            lock (_queueLock)
            {
                var toRemove = new List<int>();
                var tempQueue = new PriorityQueue<LoadTask, int>();
                
                while (_loadQueue.TryDequeue(out var task, out var priority))
                {
                    // 只保�?Critical、High、Medium 优先级任�?
                    if (task.Priority >= LoadPriority.Low)
                    {
                        _queuedIndices.Remove(task.Index);
                        toRemove.Add(task.Index);
                    }
                    else
                    {
                        tempQueue.Enqueue(task, priority);
                    }
                }
                
                // 重新入队保留的任�?
                while (tempQueue.TryDequeue(out var task, out var priority))
                {
                    _loadQueue.Enqueue(task, priority);
                }
                
                if (toRemove.Count > 0)
                {
                    Debug.WriteLine($"[PriorityLoader] Canceled low priority tasks: {toRemove.Count}");
                }
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// �?单一数据源：计算可视区域能显示多少图�?
        /// 使用 _getVisibleRangeFunc 委托获取UI实际可视范围
        /// </summary>
        private int CalculateVisibleCount()
        {
            // �?优先使用委托获取实际可视范围
            if (_getVisibleRangeFunc != null)
            {
                try
                {
                    var (first, last) = _getVisibleRangeFunc();
                    if (first >= 0 && last >= first)
                    {
                        int count = last - first + 1;
                        // �?同步更新所有配置的单源数据
                        _dynamicPolicy.SetActualVisibleSize(count);
                        _bufferConfig.SetActualVisibleSize(count);
                        return count;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[PriorityLoader] �?GetVisibleRange委托异常: {ex.Message}");
                }
            }
            
            // 回退：使用当前配置值（可能过时�?
            return _dynamicPolicy.ActualVisibleSize;
        }

        /// <summary>
        /// �?P2优化：计算缓冲区域范�?
        /// �?动态范围：根据滚动类型动态调整各区域大小
        /// </summary>
        private void CalculateBufferZones(int firstVisible, int lastVisible, int totalCount)
        {
            // 活跃区域 = 当前可视范围
            _activeZone = (firstVisible, lastVisible);

            // �?动态范围：根据滚动类型动态调整预加载区域大小
            double prefetchMultiplier = _dynamicPolicy.GetPrefetchMultiplier(_currentScrollType);
            int basePrefetchSize = _bufferConfig.PrefetchSize;
            int prefetchSize = (int)(basePrefetchSize * prefetchMultiplier);
            
            // 限制最大预加载范围
            prefetchSize = Math.Min(prefetchSize, _dynamicPolicy.MaxPrefetchRange);
            _currentPrefetchSize = prefetchSize;

            // 根据滚动方向优化预加载范�?
            int leftPrefetchSize = prefetchSize;
            int rightPrefetchSize = prefetchSize;
            
            if (_scrollDirection > 0)
            {
                // 向右滚动：优先预加载右侧
                rightPrefetchSize = (int)(prefetchSize * 1.5);
                leftPrefetchSize = (int)(prefetchSize * 0.5);
            }
            else if (_scrollDirection < 0)
            {
                // 向左滚动：优先预加载左侧
                leftPrefetchSize = (int)(prefetchSize * 1.5);
                rightPrefetchSize = (int)(prefetchSize * 0.5);
            }

            // 预加载区域（左右两侧�?
            _prefetchZoneLeft = (
                Math.Max(0, firstVisible - leftPrefetchSize),
                Math.Max(0, firstVisible - 1)
            );
            _prefetchZoneRight = (
                Math.Min(totalCount - 1, lastVisible + 1),
                Math.Min(totalCount - 1, lastVisible + rightPrefetchSize)
            );

            // 后备区域（预加载区域外）- 仅在静止时激�?
            int reserveSize = _currentScrollType == ScrollType.Stopped ? _bufferConfig.ReserveSize : 0;
            _reserveZoneLeft = (
                Math.Max(0, _prefetchZoneLeft.start - reserveSize),
                Math.Max(0, _prefetchZoneLeft.start - 1)
            );
            _reserveZoneRight = (
                Math.Min(totalCount - 1, _prefetchZoneRight.end + 1),
                Math.Min(totalCount - 1, _prefetchZoneRight.end + reserveSize)
            );
        }

        /// <summary>
        /// �?P2优化：获取索引所属的缓冲区域
        /// </summary>
        private BufferZone GetBufferZone(int index)
        {
            // 活跃区域
            if (index >= _activeZone.start && index <= _activeZone.end)
                return BufferZone.Active;

            // 预加载区�?
            if ((index >= _prefetchZoneLeft.start && index <= _prefetchZoneLeft.end) ||
                (index >= _prefetchZoneRight.start && index <= _prefetchZoneRight.end))
                return BufferZone.Prefetch;

            // 后备区域
            if ((index >= _reserveZoneLeft.start && index <= _reserveZoneLeft.end) ||
                (index >= _reserveZoneRight.start && index <= _reserveZoneRight.end))
                return BufferZone.Reserve;

            // 超出所有缓冲区�?
            return BufferZone.Reserve;
        }

        /// <summary>
        /// �?P2优化：根据缓冲区域获取加载优先级
        /// </summary>
        private LoadPriority GetPriorityForBufferZone(BufferZone zone, int index, int firstVisible)
        {
            return zone switch
            {
                BufferZone.Active => index == firstVisible ? LoadPriority.Critical : LoadPriority.High,
                BufferZone.Prefetch => LoadPriority.Medium,
                BufferZone.Reserve => LoadPriority.Idle, // �?P3空闲优先�?
                _ => LoadPriority.Low
            };
        }

        /// <summary>
        /// �?P2优化：更新滚动状态并触发缓冲区域更新
        /// �?动态范围：根据滚动类型动态调整预加载范围
        /// </summary>
        private void UpdateScrollState(int firstVisible, int lastVisible, int totalCount)
        {
            var now = DateTime.Now;

            // 计算滚动方向
            if (_lastFirstVisible >= 0)
            {
                int indexDelta = firstVisible - _lastFirstVisible;
                int previousDirection = _scrollDirection;
                _scrollDirection = indexDelta > 0 ? 1 : (indexDelta < 0 ? -1 : 0);

                // 计算滚动速度（项/秒）
                if (_lastScrollTime != DateTime.MinValue)
                {
                    var timeDelta = (now - _lastScrollTime).TotalSeconds;
                    if (timeDelta > 0)
                    {
                        _scrollVelocity = Math.Abs(indexDelta) / timeDelta;
                    }
                }

                // �?动态范围：判断滚动类型
                var previousScrollType = _currentScrollType;
                _currentScrollType = DetermineScrollType(_scrollVelocity);
                
                // 判断是否快速滚�?
                bool wasFastScrolling = _isFastScrolling;
                _isFastScrolling = _scrollVelocity > _dynamicPolicy.FastScrollThreshold;

                // 滚动类型变化处理
                if (_currentScrollType != previousScrollType)
                {
                    if (_currentScrollType == ScrollType.Fast || _currentScrollType == ScrollType.UltraFast)
                    {
                        Debug.WriteLine($"[DynamicRange] Scroll type changed: {previousScrollType} -> {_currentScrollType} (Speed:{_scrollVelocity:F1} items/s)");
                    }
                    else if (_currentScrollType == ScrollType.Stopped && previousScrollType != ScrollType.Stopped)
                    {
                        Debug.WriteLine("[DynamicRange] Scroll stopped - starting visible area loading");
                    }
                }

                // 快速滚动开�?停止处理
                if (_isFastScrolling && !wasFastScrolling)
                {
                    Debug.WriteLine($"[DynamicRange] Fast scroll started - Speed:{_scrollVelocity:F1} items/s");
                }
                else if (!_isFastScrolling && wasFastScrolling)
                {
                    _lastScrollStopTime = now;
                    Debug.WriteLine($"[DynamicRange] 快速滚动停�?- 启动后备区域激活定时器");
                    StartReserveActivationTimer(totalCount);
                }

                // 静止状�?
                if (_scrollDirection == 0 && previousDirection != 0)
                {
                    _lastScrollStopTime = now;
                    StartReserveActivationTimer(totalCount);
                }
            }

            // �?首屏加载保护：不在首屏加载期间更新计数范�?
            // 防止ScrollViewer布局事件导致_lastLastVisible被扩大，从而产生计数溢�?
            if (!_visibleAreaLoadStartTime.HasValue)
            {
                _lastScrollTime = now;
                _lastFirstVisible = firstVisible;
                _lastLastVisible = lastVisible;
            }

            // 计算缓冲区域
            CalculateBufferZones(firstVisible, lastVisible, totalCount);
        }

        /// <summary>
        /// �?动态范围：根据滚动速度判断滚动类型
        /// </summary>
        private ScrollType DetermineScrollType(double velocity)
        {
            if (velocity <= 0.1) return ScrollType.Stopped;
            if (velocity < _dynamicPolicy.FastScrollThreshold) return ScrollType.Slow;
            if (velocity < _dynamicPolicy.UltraFastScrollThreshold) return ScrollType.Fast;
            return ScrollType.UltraFast;
        }

        /// <summary>
        /// �?P2优化：启动后备区域激活定时器
        /// 滚动停止后延迟一段时间，再加载后备区域的图片
        /// </summary>
        private void StartReserveActivationTimer(int totalCount)
        {
            // 停止现有定时�?
            _reserveActivationTimer?.Stop();

            // 创建新定时器
            if (_reserveActivationTimer == null)
            {
                _reserveActivationTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(_bufferConfig.ReserveActivationDelay)
                };
                _reserveActivationTimer.Tick += (s, e) =>
                {
                    _reserveActivationTimer?.Stop();
                    LoadReserveZone(totalCount);
                };
            }

            _reserveActivationTimer.Interval = TimeSpan.FromMilliseconds(_bufferConfig.ReserveActivationDelay);
            _reserveActivationTimer.Start();
        }

        /// <summary>
        /// �?P2优化：加载后备区域的图片（空闲时�?
        /// </summary>
        private void LoadReserveZone(int totalCount)
        {
            if (_imageCollection == null || totalCount <= 0) return;

            int leftCount = _reserveZoneLeft.end - _reserveZoneLeft.start + 1;
            int rightCount = _reserveZoneRight.end - _reserveZoneRight.start + 1;

            if (leftCount <= 0 && rightCount <= 0) return;

            Debug.WriteLine($"[BufferZone] 后备区域激�?- �?[{_reserveZoneLeft.start}-{_reserveZoneLeft.end}] �?[{_reserveZoneRight.start}-{_reserveZoneRight.end}]");

            // 入队后备区域图片（Idle优先级）
            for (int i = _reserveZoneLeft.start; i <= _reserveZoneLeft.end && i < totalCount; i++)
            {
                if (i >= 0)
                {
                    var filePath = _imageCollection[i].FilePath;
                    EnqueueLoadTask(i, filePath, LoadPriority.Idle);
                }
            }

            for (int i = _reserveZoneRight.start; i <= _reserveZoneRight.end && i < totalCount; i++)
            {
                if (i >= 0)
                {
                    var filePath = _imageCollection[i].FilePath;
                    EnqueueLoadTask(i, filePath, LoadPriority.Idle);
                }
            }

            StartLoading();
        }

        /// <summary>
        /// 入队加载任务
        /// </summary>
        private void EnqueueLoadTask(int index, string filePath, LoadPriority priority)
        {
            lock (_queueLock)
            {
                // �?优化：检查是否真正需要加载（Thumbnail 是否已存在）
                if (_loadedIndices.Contains(index) || _queuedIndices.Contains(index) || _decodingIndices.Contains(index))
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
        /// �?诊断用：返回是否成功入队
        /// </summary>
        private bool EnqueueLoadTaskWithLog(int index, string filePath, LoadPriority priority)
        {
            lock (_queueLock)
            {
                // �?优化：检查是否真正需要加载（Thumbnail 是否已存在）
                if (_loadedIndices.Contains(index))
                {
                    // 检查是否真的已加载（Thumbnail不为null�?
                    if (_imageCollection != null && index < _imageCollection.Count)
                    {
                        var thumbnail = _imageCollection[index].Thumbnail;
                        if (thumbnail == null)
                        {
                            // �?状态不一致：_loadedIndices标记已加载，但实际Thumbnail为null
                            Debug.WriteLine($"[PriorityLoader] �?状态不一�?index={index} 在loadedIndices但Thumbnail=null，修复中...");
                            _loadedIndices.Remove(index);
                            _decodingIndices.Remove(index);
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

                // �?新增：检查是否正在解码中
                if (_decodingIndices.Contains(index))
                {
                    return false; // 正在解码，跳�?
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

            // 启动UI更新定时�?
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
                    // 队列为空，等待一下再检�?
                    await Task.Delay(50, cancellationToken);
                    continue;
                }

                // �?P0优化：根据优先级选择线程�?
                // �?P2优化：Idle优先级使用普通线程池，且检查是否有更高优先级任�?
                var semaphore = task.Priority <= LoadPriority.High 
                    ? _highPrioritySemaphore 
                    : _normalPrioritySemaphore;
                
                await semaphore.WaitAsync(cancellationToken);

                _ = Task.Run(async () =>
                {
                    Interlocked.Increment(ref _activeTaskCount);
                    var taskSw = Stopwatch.StartNew();
                    try
                    {
                        await LoadSingleThumbnailAsync(task, cancellationToken);
                        taskSw.Stop();

                        // 调整并发�?
                        AdjustConcurrencyIfNeeded(taskSw.ElapsedMilliseconds);

                        Interlocked.Increment(ref processedCount);
                    }
                    catch (OperationCanceledException)
                    {
                        // 取消时不做处�?
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[PriorityLoader] 加载异常 index={task.Index}: {ex.Message}");
                    }
                    finally
                    {
                        semaphore.Release();
                        Interlocked.Decrement(ref _activeTaskCount);
                    }
                }, cancellationToken);

                // 进度不输出日�?
            }

            sw.Stop();
            // 队列处理完成不输出日�?
        }

        /// <summary>
        /// 加载单个缩略�?
        /// </summary>
        private async Task LoadSingleThumbnailAsync(LoadTask task, CancellationToken cancellationToken)
        {
            // �?修复竞态条件：在方法开始时捕获集合引用，避免在异步执行过程中集合被其他线程置空
            var imageCollection = _imageCollection;
            if (imageCollection == null || task.Index < 0 || task.Index >= imageCollection.Count)
            {
                Debug.WriteLine($"[PriorityLoader] �?跳过加载 - 无效参数 index={task.Index} collection={imageCollection != null} count={imageCollection?.Count ?? 0}");
                return;
            }

            // �?P2优化 - Idle优先级任务：可随时取�?
            if (task.Priority == LoadPriority.Idle)
            {
                // 检查是否有更高优先级任�?
                if (HasHigherPriorityTasks())
                {
                    Debug.WriteLine($"[PriorityLoader] �?取消Idle任务 index={task.Index}（有更高优先级任务）");
                    return; // 直接放弃，不重新入队
                }

                // 检查是否还在缓冲区域内
                var zone = GetBufferZone(task.Index);
                if (zone == BufferZone.Reserve)
                {
                    // 仍在后备区域，继续加�?
                }
                else
                {
                    // 已移入更高级别区域，让它以更高优先级重新入队
                    Debug.WriteLine($"[PriorityLoader] �?Idle任务升级 index={task.Index} 移入{zone}区域");
                    return;
                }
            }

            // �?P0优化 - 阶段1：开始前检�?- 低优先级任务在高负载时放�?
            if (task.Priority >= LoadPriority.Low && _useLowQuality)
            {
                Debug.WriteLine($"[PriorityLoader] Dropped Low task index={task.Index} (low quality mode)");
                return;
            }

            var imageInfo = imageCollection[task.Index];

            // �?优化：检查是否真正需要加载（Thumbnail 是否已存在）
            lock (_queueLock)
            {
                if (_loadedIndices.Contains(task.Index))
                {
                    // 检查是否真的已加载
                    if (imageInfo.Thumbnail == null)
                    {
                        // 状态不一致：_loadedIndices标记已加载，但实际Thumbnail为null
                        // 修复状态，继续加载
                        _loadedIndices.Remove(task.Index);
                        _queuedIndices.Remove(task.Index);
                        _decodingIndices.Remove(task.Index);
                    }
                    else
                    {
                        return; // 确实已加载，跳过
                    }
                }
                
                // �?关键修改：使�?_decodingIndices 标记正在解码，而不�?_loadedIndices
                _decodingIndices.Add(task.Index);
            }

            if (imageInfo.Thumbnail != null)
            {
                Debug.WriteLine($"[PriorityLoader] Skipped - index={task.Index} Thumbnail already exists");
                lock (_queueLock)
                {
                    _decodingIndices.Remove(task.Index);
                    _loadedIndices.Add(task.Index); // 同步状�?
                }
                return;
            }

            var sw = Stopwatch.StartNew();
            int thumbnailSize = _useLowQuality ? 40 : 60;

            try
            {
                // �?P0优化 - 阶段2：解码前再次检查优先级（Idle任务再次检查）
                if (task.Priority == LoadPriority.Idle && HasHigherPriorityTasks())
                {
                    Debug.WriteLine($"[PriorityLoader] �?中断Idle任务 index={task.Index}（有更高优先级任务）");
                    lock (_queueLock)
                    {
                        _decodingIndices.Remove(task.Index);
                    }
                    return; // 不重新入队，让更高优先级任务先执�?
                }

                // Medium/Low任务检�?
                if (task.Priority >= LoadPriority.Medium && task.Priority < LoadPriority.Idle && HasHigherPriorityTasks())
                {
                    Debug.WriteLine($"[PriorityLoader] �?推迟Medium/Low任务 index={task.Index}（有更高优先级任务）");
                    lock (_queueLock)
                    {
                        _decodingIndices.Remove(task.Index);
                        // 重新入队，等待下次调�?
                        _loadQueue.Enqueue(task, (int)task.Priority);
                    }
                    return;
                }

                // 后台线程加载
                // �?优先级感知：Critical/High任务使用高优先级GPU解码
                bool isHighPriorityForGpu = task.Priority <= LoadPriority.High;
                var thumbnail = await Task.Run(() => LoadThumbnailOptimized(task.FilePath, thumbnailSize, isHighPriorityForGpu));
                sw.Stop();

                // �?P0优化 - 阶段3：UI更新前检查取�?
                if (cancellationToken.IsCancellationRequested || thumbnail == null)
                {
                    Debug.WriteLine($"[PriorityLoader] �?加载返回null index={task.Index} file={task.FilePath} 耗时:{sw.ElapsedMilliseconds}ms");
                    lock (_queueLock)
                    {
                        _decodingIndices.Remove(task.Index);
                    }
                    return;
                }

                // �?关键诊断：检查缩略图有效�?
                if (thumbnail.Width <= 0 || thumbnail.Height <= 0)
                {
                    Debug.WriteLine($"[PriorityLoader] �?无效缩略�?index={task.Index} size={thumbnail.Width}x{thumbnail.Height} 耗时:{sw.ElapsedMilliseconds}ms");
                    lock (_queueLock)
                    {
                        _decodingIndices.Remove(task.Index);
                    }
                    return;
                }

                // 加入UI更新队列（包含解码成功标记）
                _uiUpdateQueue.Enqueue(new UIUpdateRequest
                {
                    Index = task.Index,
                    Thumbnail = thumbnail,
                    ImageInfo = imageInfo,
                    FilePath = task.FilePath,
                    DecodeSuccess = true  // �?标记解码成功
                });

                // �?方案B优化：移除提前计数，改为�?ProcessUIUpdates 中实际设置成功后计数
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PriorityLoader] �?加载异常 index={task.Index}: {ex.Message}");
            }
        }

        /// <summary>
        /// �?P0优化：检查是否有更高优先级任务等�?
        /// �?P2优化：支持Idle优先级检�?
        /// </summary>
        private bool HasHigherPriorityTasks()
        {
            lock (_queueLock)
            {
                // 检查队列中是否有High或Critical任务
                return _loadQueue.UnorderedItems.Any(item => item.Element.Priority <= LoadPriority.High);
            }
        }

        /// <summary>
        /// �?P2优化：检查是否有中高优先级任务（用于Idle任务判断�?
        /// </summary>
        private bool HasMediumOrHigherPriorityTasks()
        {
            lock (_queueLock)
            {
                // 检查队列中是否有Medium及以上优先级任务
                return _loadQueue.UnorderedItems.Any(item => item.Element.Priority <= LoadPriority.Medium);
            }
        }

        /// <summary>
        /// 加载缩略图（使用委托调用实际加载方法�?
        /// </summary>
        private BitmapImage? LoadThumbnailOptimized(string filePath, int size, bool isHighPriority = false)
        {
            try
            {
                if (_loadThumbnailFunc != null)
                {
                    return _loadThumbnailFunc(filePath, size, isHighPriority);
                }
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PriorityLoader] 缩略图加载失�? {filePath} - {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 动态调整并发数
        /// �?优化：首屏加载期间延�?秒调整，避免误降并发
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

                // �?新增：首屏加载期间不调整并发，避免误�?
                if (_startupTime != DateTime.MinValue && 
                    (DateTime.Now - _startupTime).TotalMilliseconds < STARTUP_GRACE_PERIOD_MS)
                {
                    return; // 首屏3秒内不调整并�?
                }

                var avgTime = _decodeTimes.Average();

                if (avgTime < 50 && _dynamicConcurrency < MAX_CONCURRENCY)
                {
                    _dynamicConcurrency = Math.Min(MAX_CONCURRENCY, _dynamicConcurrency + 1);
                    Debug.WriteLine($"[PriorityLoader] �?增加并发: {_dynamicConcurrency}");
                }
                else if (avgTime > 150 && _dynamicConcurrency > MIN_CONCURRENCY)
                {
                    _dynamicConcurrency = Math.Max(MIN_CONCURRENCY, _dynamicConcurrency - 1);
                    Debug.WriteLine($"[PriorityLoader] �?减少并发: {_dynamicConcurrency}");
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
            // 等待超时不输出日�?
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
                    // 清理不输出日�?
                }, DispatcherPriority.Background);
            }
        }

        /// <summary>
        /// 初始化UI更新定时�?
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
            // �?关键修复：捕获集合引用，防止委托调用后集合变为null
            var imageCollection = _imageCollection;
            
            if (_uiUpdateQueue.Count == 0 || imageCollection == null)
            {
                if (imageCollection == null && _uiUpdateQueue.Count > 0)
                {
                    Debug.WriteLine($"[ProcessUIUpdates] Skipped update - _imageCollection is null, {_uiUpdateQueue.Count} items in queue");
                }
                return;
            }

            var updates = new List<UIUpdateRequest>();
            while (_uiUpdateQueue.TryDequeue(out var request))
            {
                updates.Add(request);
            }

            if (updates.Count == 0) return;

            // �?获取实时可视范围（优先使用委托，否则使用缓存值）
            int firstVis = _lastFirstVisible;
            int lastVis = _lastLastVisible;
            if (_getVisibleRangeFunc != null)
            {
                try
                {
                    var (f, l) = _getVisibleRangeFunc();
                    Debug.WriteLine($"[ProcessUIUpdates] GetVisibleRange返回: ({f}, {l}), imageCollection.Count={imageCollection?.Count ?? -1}");
                    if (f >= 0 && l >= 0)
                    {
                        firstVis = f;
                        lastVis = l;
                    }
                    else
                    {
                        // �?新增：如果可视范围无效，跳过本次更新
                        Debug.WriteLine($"[ProcessUIUpdates] �?可视范围无效({f}, {l})，跳过UI更新");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ProcessUIUpdates] �?GetVisibleRange委托异常: {ex.Message}");
                    return;
                }
            }

            // �?二次检查：委托调用后集合可能已变化
            if (imageCollection == null || imageCollection.Count == 0)
            {
                Debug.WriteLine($"[ProcessUIUpdates] �?委托调用后集合已为空，跳过UI更新");
                return;
            }

            // 分离可视区域和后台更�?
            var visibleUpdates = updates.Where(u =>
                u.Index >= firstVis && u.Index <= lastVis).ToList();

            // 可视区域立即更新
            int updatedCount = 0;
            int skippedCount = 0;
            foreach (var update in visibleUpdates)
            {
                if (update.Index < imageCollection.Count &&
                    imageCollection[update.Index] == update.ImageInfo)
                {
                    // 验证缩略图有效�?
                    if (update.Thumbnail != null && update.Thumbnail.Width > 0 && update.Thumbnail.Height > 0)
                    {
                        update.ImageInfo.Thumbnail = update.Thumbnail;
                        _addToCacheAction?.Invoke(update.FilePath, update.Thumbnail);
                        updatedCount++;
                        
                        // �?关键修复：Thumbnail 成功设置后，才添加到 _loadedIndices
                        lock (_queueLock)
                        {
                            _decodingIndices.Remove(update.Index);
                            _loadedIndices.Add(update.Index);
                        }
                        
                        // �?方案B优化：实际设置成功后才计�?
                        Interlocked.Increment(ref _loadedInVisibleArea);
                    }
                    else
                    {
                        skippedCount++;
                        // 缩略图无效，从解码中移除
                        lock (_queueLock)
                        {
                            _decodingIndices.Remove(update.Index);
                        }
                    }
                }
                else
                {
                    skippedCount++;
                    // 索引不匹配，从解码中移除
                    lock (_queueLock)
                    {
                        _decodingIndices.Remove(update.Index);
                    }
                }
            }

            // 后台更新
            var backgroundUpdates = updates.Except(visibleUpdates).ToList();
            int bgUpdatedCount = 0;
            foreach (var update in backgroundUpdates)
            {
                if (update.Index < imageCollection.Count &&
                    imageCollection[update.Index] == update.ImageInfo)
                {
                    imageCollection[update.Index].Thumbnail = update.Thumbnail;
                    _addToCacheAction?.Invoke(update.FilePath, update.Thumbnail);
                    bgUpdatedCount++;
                    
                    // �?关键修复：Thumbnail 成功设置后，才添加到 _loadedIndices
                    lock (_queueLock)
                    {
                        _decodingIndices.Remove(update.Index);
                        _loadedIndices.Add(update.Index);
                    }
                }
            }

            // �?方案B优化：使用实时可视范围判断完�?
            // 检查可视区域是否加载完成（基于实时范围，而非预期数量�?
            if (_visibleAreaLoadStartTime.HasValue)
            {
                // 获取当前可视范围内的实际已加载数�?
                int actualVisibleCount = 0;
                int actualLoadedCount = 0;
                
                for (int i = firstVis; i <= lastVis && i < imageCollection.Count; i++)
                {
                    actualVisibleCount++;
                    if (imageCollection[i].Thumbnail != null)
                    {
                        actualLoadedCount++;
                    }
                }
                
                // 当实际已加载数量等于可视范围数量时，报告完成
                if (actualLoadedCount >= actualVisibleCount)
                {
                    var duration = DateTime.Now - _visibleAreaLoadStartTime.Value;
                    VisibleAreaLoadingCompleted?.Invoke(actualLoadedCount, duration);
                    _visibleAreaLoadStartTime = null;
                    _loadedInVisibleArea = 0;
                }
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
                _highPrioritySemaphore?.Dispose();
                _normalPrioritySemaphore?.Dispose();
                _uiUpdateTimer?.Stop();
                _reserveActivationTimer?.Stop(); // �?P2优化：清理后备区域激活定时器

                lock (_queueLock)
                {
                    _loadQueue.Clear();
                    _queuedIndices.Clear();
                    _loadedIndices.Clear();
                    _decodingIndices.Clear();  // �?新增：清理解码中索引
                }

                _disposed = true;
            }
        }

        #endregion

        #region 内部�?

        private class UIUpdateRequest
        {
            public int Index { get; set; }
            public BitmapImage? Thumbnail { get; set; }
            public ImageInfo? ImageInfo { get; set; }
            public string FilePath { get; set; } = string.Empty;
            public bool DecodeSuccess { get; set; }  // �?新增：标记解码是否成�?
        }

        #endregion
    }
}
