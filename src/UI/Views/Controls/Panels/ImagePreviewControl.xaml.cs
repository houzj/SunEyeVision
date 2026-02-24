using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using SunEyeVision.UI.ViewModels;
using SunEyeVision.UI.Services.Thumbnail;
using SunEyeVision.UI.Services.Performance;
using SunEyeVision.UI.Services.Thumbnail.Decoders;

namespace SunEyeVision.UI.Views.Controls.Panels
{
    /// <summary>
    /// 图像运行模式枚举
    /// </summary>
    public enum ImageRunMode
    {
        运行全部 = 0,
        运行选择 = 1
    }

    /// <summary>
    /// 工作流执行请求事件参�?
    /// </summary>
    public class WorkflowExecutionRequestEventArgs : EventArgs
    {
        public ImageInfo ImageInfo { get; set; } = null!;
        public int Index { get; set; }
    }

    /// <summary>
    /// 图像信息�?
    /// </summary>
    public class ImageInfo : INotifyPropertyChanged
    {
        private int _displayIndex;
        private bool _isSelected;
        private bool _isForRun = false;
        private BitmapSource? _fullImage;
        private bool _isFullImageLoaded;

        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public BitmapSource? Thumbnail 
        { 
            get => _thumbnail;
            set
            {
                if (_thumbnail != value)
                {
                    _thumbnail = value;
                    OnPropertyChanged(nameof(Thumbnail));
                }
            }
        }
        private BitmapSource? _thumbnail;
        public DateTime AddedTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 懒加载的全分辨率图像，只在首次访问时加载
        /// </summary>
        public BitmapSource? FullImage
        {
            get
            {
                // 首次访问时才加载
                if (!_isFullImageLoaded && _fullImage == null && !string.IsNullOrEmpty(FilePath))
                {
                    _fullImage = ImagePreviewControl.LoadImageOptimized(FilePath);
                    _isFullImageLoaded = true;
                }
                return _fullImage;
            }
            set
            {
                if (_fullImage != value)
                {
                    _fullImage = value;
                    _isFullImageLoaded = (value != null);
                    OnPropertyChanged(nameof(FullImage));
                }
            }
        }

        /// <summary>
        /// 是否已加载全分辨率图�?
        /// </summary>
        public bool IsFullImageLoaded => _isFullImageLoaded && _fullImage != null;

        public int DisplayIndex
        {
            get => _displayIndex;
            set
            {
                if (_displayIndex != value)
                {
                    _displayIndex = value;
                    OnPropertyChanged(nameof(DisplayIndex));
                }
            }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged(nameof(IsSelected));
                }
            }
        }

        /// <summary>
        /// 是否被选中用于运行（运行选择模式时使用）
        /// </summary>
        public bool IsForRun
        {
            get => _isForRun;
            set
            {
                if (_isForRun != value)
                {
                    _isForRun = value;
                    OnPropertyChanged(nameof(IsForRun));
                }
            }
        }

        /// <summary>
        /// 手动设置已加载的全分辨率图像（用于异步加载完成后的更新）
        /// </summary>
        public void SetFullImage(BitmapSource? image)
        {
            _fullImage = image;
            _isFullImageLoaded = (image != null);
            OnPropertyChanged(nameof(FullImage));
        }

        /// <summary>
        /// 释放全分辨率图像以节省内�?
        /// </summary>
        public void ReleaseFullImage()
        {
            // 只有当图像实际已加载时才触发属性变更事�?
            bool wasLoaded = (_fullImage != null);
            _fullImage = null;
            _isFullImageLoaded = false;
            
            if (wasLoaded)
            {
                OnPropertyChanged(nameof(FullImage));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// 加载进度信息
    /// </summary>
    public class LoadProgress
    {
        public int CurrentIndex { get; set; }
        public int TotalCount { get; set; }
        public string CurrentFile { get; set; } = string.Empty;
        public double ProgressPercentage => TotalCount > 0 ? (double)CurrentIndex / TotalCount * 100 : 0;
    }

    /// <summary>
    /// LRU图像缓存
    /// </summary>
    public class ImageCache
    {
        private readonly int _maxCacheSize;
        private readonly Dictionary<string, LinkedListNode<CacheEntry>> _cacheMap;
        private readonly LinkedList<CacheEntry> _lruList;

        public ImageCache(int maxCacheSize = 30)
        {
            _maxCacheSize = maxCacheSize;
            _cacheMap = new Dictionary<string, LinkedListNode<CacheEntry>>();
            _lruList = new LinkedList<CacheEntry>();
        }

        /// <summary>
        /// 获取或添加缓�?
        /// </summary>
        public BitmapSource? GetOrAdd(string filePath, Func<string, BitmapSource?> loader)
        {
            if (_cacheMap.TryGetValue(filePath, out var node))
            {
                // 命中缓存，移到最�?
                _lruList.Remove(node);
                _lruList.AddFirst(node);
                return node.Value.Bitmap;
            }

            // 未命中，加载图像
            var bitmap = loader(filePath);
            if (bitmap == null) return null;

            // 添加到缓�?
            var entry = new CacheEntry(filePath, bitmap);
            var newNode = _lruList.AddFirst(entry);
            _cacheMap[filePath] = newNode;

            // 超出容量，移除最少使用的
            if (_lruList.Count > _maxCacheSize)
            {
                var lastNode = _lruList.Last;
                if (lastNode != null)
                {
                    _cacheMap.Remove(lastNode.Value.FilePath);
                    _lruList.RemoveLast();
                }
            }

            return bitmap;
        }

        /// <summary>
        /// 清除所有缓�?
        /// </summary>
        public void Clear()
        {
            _cacheMap.Clear();
            _lruList.Clear();
        }

        private class CacheEntry
        {
            public string FilePath { get; }
            public BitmapSource Bitmap { get; }

            public CacheEntry(string filePath, BitmapSource bitmap)
            {
                FilePath = filePath;
                Bitmap = bitmap;
            }
        }
    }

    /// <summary>
    /// ImagePreviewControl.xaml 的交互逻辑
    /// </summary>
    public partial class ImagePreviewControl : System.Windows.Controls.UserControl, INotifyPropertyChanged
    {
        // UI尺寸配置（集中管理，避免硬编码）
        private static class ThumbnailSizes
        {
            // XAML中定义的尺寸（必须与XAML完全一致）
            public const double BorderWidth = 70;          // Border的Width="70"
            public const double BorderHeight = 70;         // Border的Height="70"
            public const double ImageWidth = 60;           // Image的Width="60"
            public const double ImageHeight = 60;          // Image的Height="60"
            public const double PlaceholderWidth = 60;     // 占位符的Width
            public const double PlaceholderHeight = 60;    // 占位符的Height（匹配Image�?

            // 布局间距（从XAML Margin="1"计算得出�?
            public const double HorizontalMargin = 2;      // 左右各Margin 1

            // 计算属性（供算法使用）
            public static double ItemWidth => BorderWidth + HorizontalMargin;  // 72.0
            public static int ThumbnailLoadSize => (int)ImageWidth;             // 60
        }

        // 图像缓存（LRU�?
        private static readonly ImageCache s_fullImageCache = new ImageCache(maxCacheSize: 30);

        // �?方案二：双解码器架构
        // GPU解码�?- 高优先级任务专用（Critical/High优先级）
        private static readonly WicGpuDecoder s_gpuDecoder = new WicGpuDecoder();
        // CPU解码�?- 普通任务专用（Medium/Low/Idle优先级）
        private static readonly ImageSharpDecoder s_cpuDecoder = new ImageSharpDecoder();

        // 磁盘缓存管理器（60x60高质量缩略图�?
        private static readonly ThumbnailCacheManager s_thumbnailCache = new ThumbnailCacheManager();

        // 智能缩略图加载器（组合策略：L1内存 �?L2磁盘 �?Shell缓存 �?EXIF �?GPU/CPU解码�?
        // �?方案二：传入双解码器，根据优先级自动选择
        private static readonly SmartThumbnailLoader s_smartLoader = new SmartThumbnailLoader(s_thumbnailCache, s_gpuDecoder, s_cpuDecoder);

        // 内存压力监控器（响应系统内存压力�?
        private static readonly MemoryPressureMonitor s_memoryMonitor = new MemoryPressureMonitor();

        // ListBox控件引用（用于滚动监听）
        private ListBox? _thumbnailListBox;

        // 取消令牌�?
        private CancellationTokenSource? _loadingCancellationTokenSource;

        // 预加载相�?
        private Task? _preloadTask;
        private int _lastPreloadIndex = -1;

        // 滚动防抖定时�?
        private DispatcherTimer? _updateRangeTimer;

        // 性能日志统计
        private int _scrollEventCount = 0;
        private DateTime _lastScrollTime = DateTime.MinValue;

        // ===== P0优化: 滚动暂停加载 =====
        private double _lastScrollOffset = 0;
        private double _scrollSpeed = 0; // 滚动速度（像�?秒）
        private DateTime _lastScrollSpeedTime = DateTime.MinValue;
        private const double SCROLL_SPEED_THRESHOLD = 500; // 快速滚动阈值（像素/秒）
        private bool _isFastScrolling = false;
        private DispatcherTimer? _scrollStopTimer; // 滚动停止检测定时器

        // ===== P0优化: 动态图像质�?=====
        private bool _useLowQuality = false; // 是否使用低质量缩略图
        private const int LOW_QUALITY_SIZE = 40; // 快速滚动时的缩略图尺寸
        private const int HIGH_QUALITY_SIZE = 60; // 正常的缩略图尺寸
        private HashSet<int> _pendingHighQualityIndices = new HashSet<int>(); // 待加载高质量的索�?

        // 优先级缩略图加载器（替代双系统）
        private readonly PriorityThumbnailLoader _priorityLoader = new PriorityThumbnailLoader();

        public static readonly DependencyProperty AutoSwitchEnabledProperty =
            DependencyProperty.Register("AutoSwitchEnabled", typeof(bool), typeof(ImagePreviewControl),
                new PropertyMetadata(false));

        public static readonly DependencyProperty CurrentImageIndexProperty =
            DependencyProperty.Register("CurrentImageIndex", typeof(int), typeof(ImagePreviewControl),
                new PropertyMetadata(-1, OnCurrentImageIndexChanged));

        public static readonly DependencyProperty ImageCollectionProperty =
            DependencyProperty.Register("ImageCollection", typeof(BatchObservableCollection<ImageInfo>), typeof(ImagePreviewControl),
                new PropertyMetadata(null, OnImageCollectionChanged));

        public static readonly DependencyProperty ImageRunModeProperty =
            DependencyProperty.Register("ImageRunMode", typeof(ImageRunMode), typeof(ImagePreviewControl),
                new PropertyMetadata(ImageRunMode.运行全部, OnImageRunModeChanged));

        /// <summary>
        /// 是否启用自动切换
        /// </summary>
        public bool AutoSwitchEnabled
        {
            get => (bool)GetValue(AutoSwitchEnabledProperty);
            set => SetValue(AutoSwitchEnabledProperty, value);
        }

        /// <summary>
        /// 当前显示的图像索�?
        /// </summary>
        public int CurrentImageIndex
        {
            get => (int)GetValue(CurrentImageIndexProperty);
            set => SetValue(CurrentImageIndexProperty, value);
        }

        /// <summary>
        /// 图像集合（使用批量操作优化集合）
        /// </summary>
        public BatchObservableCollection<ImageInfo> ImageCollection
        {
            get => (BatchObservableCollection<ImageInfo>)GetValue(ImageCollectionProperty);
            set => SetValue(ImageCollectionProperty, value);
        }

/// <summary>
    /// 图像运行模式
    /// </summary>
    public ImageRunMode ImageRunMode
    {
        get => (ImageRunMode)GetValue(ImageRunModeProperty);
        set => SetValue(ImageRunModeProperty, value);
    }

    /// <summary>
    /// 工作流执行请求事�?- 当用户点击图片请求执行工作流时触�?
    /// </summary>
    public event EventHandler<WorkflowExecutionRequestEventArgs>? WorkflowExecutionRequested;

    /// <summary>
    /// 图像计数显示文本
    /// </summary>
        public string ImageCountDisplay
        {
            get
            {
                int count = ImageCollection?.Count ?? 0;
                int current = CurrentImageIndex >= 0 && CurrentImageIndex < count ? CurrentImageIndex + 1 : 0;
                return count > 0 ? $"图像�?({current}/{count})" : "图像�?;
            }
        }

        public ICommand AddImageCommand { get; }
        public ICommand AddFolderCommand { get; }
        public ICommand DeleteSingleImageCommand { get; }
        public ICommand ClearAllCommand { get; }

        public ImagePreviewControl()
        {
            InitializeComponent();
            ImageCollection = new BatchObservableCollection<ImageInfo>();

            // 设置优先级加载器的委托（包含实时可视范围获取�?
            _priorityLoader.SetLoadThumbnailFunc(
                (filePath, size, isHighPriority) => LoadThumbnailOptimized(filePath, size, isHighPriority), 
                (filePath, thumbnail) => s_thumbnailCache?.AddToMemoryCache(filePath, thumbnail),
                () => GetVisibleRange());  // �?实时获取可视范围

            // 订阅可视区域加载完成事件
            _priorityLoader.VisibleAreaLoadingCompleted += OnVisibleAreaLoadingCompleted;

            // ===== 内存压力监控初始�?=====
            s_memoryMonitor.MemoryPressureChanged += OnMemoryPressureChanged;
            s_memoryMonitor.Start();
            Debug.WriteLine("[ImagePreviewControl] �?内存压力监控已启�?);

            // 订阅 Unloaded 事件以清理资�?
            Unloaded += (s, e) =>
            {
                _priorityLoader.VisibleAreaLoadingCompleted -= OnVisibleAreaLoadingCompleted;
                s_memoryMonitor.MemoryPressureChanged -= OnMemoryPressureChanged;
            };

            // 输出加载器状�?
            Debug.WriteLine("========================================");
            Debug.WriteLine("   图像预览控件 - 智能缩略图加载器");
            Debug.WriteLine("========================================");
            Debug.WriteLine("�?加载策略优先级（3层架构）�?);
            Debug.WriteLine("  1. L1内存缓存 (0ms) - 强引�?0�?+ 弱引�?);
            Debug.WriteLine("  2. L2磁盘缓存 (5-80ms) - Shell缓存优先 + 自建缓存");
            Debug.WriteLine("  3. L3 GPU解码 (50-500ms) - 最终回退方案");
            Debug.WriteLine("========================================");

            AddImageCommand = new RelayCommand(ExecuteAddImage);
            AddFolderCommand = new RelayCommand(ExecuteAddFolder);
            DeleteSingleImageCommand = new RelayCommand<ImageInfo>(ExecuteDeleteSingleImage);
            ClearAllCommand = new RelayCommand(ExecuteClearAll, CanExecuteClearAll);

            ImageCollection.CollectionChanged += (s, e) =>
            {
                // UpdateDisplayIndices(); // 不再需要显示序号，移除此调�?
                OnPropertyChanged(nameof(ImageCountDisplay));
            };

            Loaded += OnControlLoaded;
            Unloaded += OnUnloaded;

            // 初始化ListBox引用（需要在Loaded之后�?
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                _thumbnailListBox = FindName("ThumbnailListBox") as ListBox;
            }));
        }

        /// <summary>
        /// 控件加载完成
        /// </summary>
        private void OnControlLoaded(object sender, RoutedEventArgs e)
        {
            // ===== P2优化: GPU缓存预热 =====
            PreloadGPUCache();
            
            // 延迟初始化，确保ListBox已完全加�?
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                _thumbnailListBox = FindName("ThumbnailListBox") as ListBox;
                if (_thumbnailListBox != null)
                {
                    var scrollViewer = FindVisualChild<ScrollViewer>(_thumbnailListBox);
                    if (scrollViewer != null)
                    {
                        scrollViewer.ScrollChanged += OnScrollChanged;
                        // 注意：不在Loaded事件中调用UpdateLoadRange()
                        // 首屏加载由LoadImagesOptimizedAsync完全控制
                        // 后续滚动会通过OnScrollChanged事件触发UpdateLoadRange
                    }
                    
                    // ===== 容器回收事件监听已禁�?=====
                    // 原因：快速来回滚动时会频繁触发容器回收，导致性能下降
                    // 
                    // 状态不一致问题已通过以下方式解决�?
                    // 1. MemoryPressureMonitor.SyncLoadedIndicesWithActualThumbnails()
                    //    在内存压力大时自动同步状�?
                    // 2. 保留 Thumbnail 可以让滚回时即时显示
                    //
                    // 优点�?
                    // - 避免频繁加载/卸载循环
                    // - 滚动流畅，内存由压力机制自动管理
                    // - 已加载的缩略图保留在内存中，滚回时即时显�?
                    // =====
                    // VirtualizingStackPanel.AddCleanUpVirtualizedItemHandler(
                    //     _thumbnailListBox, 
                    //     OnCleanUpVirtualizedItem);
                }
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }
        
        // ===== 容器回收事件处理已禁用（原因见上�?====
        // /// <summary>
        // /// 虚拟化容器回收事件处�?
        // /// </summary>
        // private void OnCleanUpVirtualizedItem(object sender, CleanUpVirtualizedItemEventArgs e)
        // {
        //     if (e.Value is ImageInfo imageInfo && ImageCollection != null)
        //     {
        //         var index = ImageCollection.IndexOf(imageInfo);
        //         if (index >= 0)
        //         {
        //             var (firstVisible, lastVisible) = GetVisibleRange();
        //             int cleanupMargin = 30;
        //             bool isFarFromVisible = (index < firstVisible - cleanupMargin) || 
        //                                     (index > lastVisible + cleanupMargin);
        //             if (isFarFromVisible)
        //             {
        //                 _priorityLoader.MarkAsUnloaded(index);
        //                 imageInfo.Thumbnail = null;
        //             }
        //         }
        //     }
        // }


        /// <summary>
        /// P2优化: 解码器初始化（减少首次解码延迟）
        /// �?方案二：同时初始化GPU和CPU解码�?
        /// </summary>
        private void PreloadGPUCache()
        {
            Task.Run(() =>
            {
                try
                {
                    var sw = Stopwatch.StartNew();
                    // 初始化两个解码器
                    s_gpuDecoder.Initialize();
                    s_cpuDecoder.Initialize();
                    sw.Stop();
                    Debug.WriteLine($"[ImagePreviewControl] �?双解码器初始化完�?- 耗时:{sw.ElapsedMilliseconds}ms");
                    Debug.WriteLine($"  GPU解码�? {s_gpuDecoder.GetType().Name} (硬件加�?{s_gpuDecoder.IsHardwareAccelerated})");
                    Debug.WriteLine($"  CPU解码�? {s_cpuDecoder.GetType().Name} (硬件加�?{s_cpuDecoder.IsHardwareAccelerated})");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ImagePreviewControl] �?解码器初始化失败（非致命�? {ex.Message}");
                }
            });
        }

        /// <summary>
        /// 查找视觉子元�?
        /// </summary>
        private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T result)
                    return result;

                var resultOfChild = FindVisualChild<T>(child);
                if (resultOfChild != null)
                    return resultOfChild;
            }
            return null;
        }

        /// <summary>
        /// 控件卸载时清理资�?
        /// </summary>
        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            // 取消正在进行的加载操�?
            _loadingCancellationTokenSource?.Cancel();
            _loadingCancellationTokenSource?.Dispose();
            _loadingCancellationTokenSource = null;

            // 取消预加载任务（异步等待，不阻塞UI线程�?
            if (_preloadTask != null)
            {
                // 不等待，直接取消，让任务自然完成
                _preloadTask = null;
            }

            // 清理优先级加载器
            _priorityLoader.Dispose();

            // 清理所有图像资�?
            if (ImageCollection != null)
            {
                foreach (var imageInfo in ImageCollection)
                {
                    imageInfo.Thumbnail = null;
                    imageInfo.ReleaseFullImage();
                }
                ImageCollection.Clear();
            }
        }

        /// <summary>
        /// 优化的全分辨率图像加载（静态方法，可从ImageInfo调用�?
        /// �?优化：使�?CleanupScheduler 保护 + StreamSource 立即加载
        /// </summary>
        public static BitmapImage? LoadImageOptimized(string filePath)
        {
            // �?修复：加载前检查文件是否存在，避免抛出 FileNotFoundException
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                return null;
            }
            
            // �?核心修复：使�?CleanupScheduler 保护文件访问
            CleanupScheduler.MarkFileInUse(filePath);
            
            try
            {
                return s_fullImageCache.GetOrAdd(filePath, fp =>
                {
                    // 再次检查（缓存内部可能访问不同的路径）
                    if (string.IsNullOrEmpty(fp) || !File.Exists(fp))
                    {
                        return null;
                    }
                    
                    try
                    {
                        // �?优化：先读取文件到内存，避免 UriSource 延迟加载问题
                        byte[] imageBytes;
                        using (var fs = new FileStream(fp, FileMode.Open, FileAccess.Read, FileShare.Read, 8192, FileOptions.SequentialScan))
                        {
                            imageBytes = new byte[fs.Length];
                            int bytesRead = fs.Read(imageBytes, 0, imageBytes.Length);
                            if (bytesRead != imageBytes.Length && imageBytes.Length > 0)
                            {
                                Array.Resize(ref imageBytes, bytesRead);
                            }
                        }
                        
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
                        bitmap.StreamSource = new MemoryStream(imageBytes);
                        bitmap.EndInit();
                        bitmap.Freeze();
                        return bitmap;
                    }
                    catch
                    {
                        return null;
                    }
                }) as BitmapImage;
            }
            finally
            {
                // �?确保释放文件引用
                CleanupScheduler.ReleaseFile(filePath);
            }
        }

        /// <summary>
        /// 优化的缩略图加载（智能加载器 - 3层架构）
        /// 加载策略优先级：L1内存 �?L2磁盘(Shell优先) �?GPU解码
        /// </summary>
        private static BitmapImage? LoadThumbnailOptimized(string filePath, int size = -1, bool isHighPriority = false)
        {
            // 如果size�?1，使用配置的缩略图尺�?
            if (size < 0)
            {
                size = ThumbnailSizes.ThumbnailLoadSize;
            }

            // 使用智能加载器（自动选择最快方式）
            var thumbnail = s_smartLoader.LoadThumbnail(filePath, size, isHighPriority);

            return thumbnail;
        }

        /// <summary>
        /// 异步加载缩略�?
        /// </summary>
        private static Task<BitmapImage?> LoadThumbnailAsync(string filePath, int size = 80)
        {
            return Task.Run(() => LoadThumbnailOptimized(filePath, size));
        }

        /// <summary>
        /// 智能预加载相邻图像（基于可视区域的动态范围）
        /// </summary>
        private void PreloadAdjacentImages(int currentIndex)
        {
            if (ImageCollection == null || ImageCollection.Count == 0)
            {
                return;
            }

            // 检查是否正在加载缩略图（避免影响加载性能�?
            if (_lastPreloadIndex == -999)
            {
                return;
            }

            // 避免重复预加�?
            if (_lastPreloadIndex == currentIndex)
            {
                return;
            }

            _lastPreloadIndex = currentIndex;

            var (immediateDisplayCount, _, _) = CalculateDynamicLoadCounts();
            int preloadRange = Math.Max(3, immediateDisplayCount / 2);

            var imagesToPreload = new List<(int index, string filePath)>();
            var preloadOffsets = Enumerable.Range(-preloadRange, preloadRange * 2 + 1).Where(x => x != 0).ToArray();

            foreach (var offset in preloadOffsets)
            {
                var index = currentIndex + offset;
                if (index >= 0 && index < ImageCollection.Count)
                {
                    var imageInfo = ImageCollection[index];
                    bool isLoaded = imageInfo.IsFullImageLoaded;

                    if (!isLoaded)
                    {
                        imagesToPreload.Add((index, imageInfo.FilePath));
                    }
                }
            }

            // 启动新的预加载任务（在后台线程中加载图像�?
            _preloadTask = Task.Run(() =>
            {
                int loadedCount = 0;

                foreach (var (index, filePath) in imagesToPreload)
                {
                    try
                    {
                        var fullImage = LoadImageOptimized(filePath);
                        if (fullImage != null)
                        {
                            // 使用BeginInvoke避免阻塞后台线程
                            Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
                            {
                                try
                                {
                                    if (index < ImageCollection.Count)
                                    {
                                        ImageCollection[index].SetFullImage(fullImage);
                                        Interlocked.Increment(ref loadedCount);
                                        // 后台预加载不输出日志
                                    }
                                }
                                catch { }
                            }), System.Windows.Threading.DispatcherPriority.Background);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[ImagePreviewControl] �?后台预加载失�? 索引{index}, 错误:{ex.Message}");
                    }
                }

                // 后台预加载完成不输出日志
            });
        }

        /// <summary>
        /// 滚动变化事件处理（带防抖机制 + P0优化：滚动暂停加�?+ 动态质量）
        /// </summary>
        private void OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            var now = DateTime.Now;
            var timeSinceLast = now - _lastScrollTime;
            _lastScrollTime = now;
            _scrollEventCount++;

            // ===== 滚动状态监控日�?=====
            // �?0次事件更新一次状态（静默处理�?
            if (_scrollEventCount % 10 == 0)
            {
                // �?关键修复：先检查集合是否有效，避免竞态条�?
                var collection = ImageCollection;
                if (collection != null && collection.Count > 0)
                {
                    var scrollViewer = FindVisualChild<ScrollViewer>(_thumbnailListBox);
                    if (scrollViewer != null)
                    {
                        var (firstVis, lastVis) = GetVisibleRange();
                    }
                }
            }

            // ===== P0优化: 计算滚动速度 =====
            if (_lastScrollSpeedTime != DateTime.MinValue && timeSinceLast.TotalSeconds > 0)
            {
                var scrollViewer = FindVisualChild<ScrollViewer>(_thumbnailListBox);
                if (scrollViewer != null)
                {
                    var currentOffset = scrollViewer.HorizontalOffset;
                    var offsetDelta = Math.Abs(currentOffset - _lastScrollOffset);
                    _scrollSpeed = offsetDelta / timeSinceLast.TotalSeconds; // 像素/�?
                    _lastScrollOffset = currentOffset;

                    // 判断是否快速滚�?
                    bool wasFastScrolling = _isFastScrolling;
                    _isFastScrolling = _scrollSpeed > SCROLL_SPEED_THRESHOLD;

                    // 快速滚动开始：使用低质量模�?
                    if (_isFastScrolling && !wasFastScrolling)
                    {
                        // 快速滚动不输出日志
                        _useLowQuality = true;
                        _priorityLoader.UseLowQuality = true;
                    }

                    // 滚动停止：恢复高质量
                    if (!_isFastScrolling && wasFastScrolling)
                    {
                        // 滚动停止不输出日�?
                        _useLowQuality = false;
                        _priorityLoader.UseLowQuality = false;
                        
                        // �?关键修复：先检查集合是否有�?
                        var collection = ImageCollection;
                        if (collection != null && collection.Count > 0)
                        {
                            // 立即触发加载新进入可视区域的缩略�?
                            try
                            {
                                UpdateLoadRange();
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"[Scroll] �?UpdateLoadRange异常: {ex.Message}");
                            }
                            
                            // 延迟加载高质量缩略图
                            Dispatcher.BeginInvoke(new Action(() =>
                            {
                                try
                                {
                                    // �?延迟回调中再次检查，因为集合可能在等待期间被清空
                                    var innerCollection = ImageCollection;
                                    if (innerCollection != null && innerCollection.Count > 0)
                                    {
                                        UpgradeToHighQuality();
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine($"[Scroll] �?UpgradeToHighQuality延迟调用异常: {ex.Message}");
                                }
                            }), DispatcherPriority.Background);
                        }
                    }
                }
            }
            _lastScrollSpeedTime = now;

            // �?P0优化: 立即入队可视区域图片（不等防抖）
            try
            {
                // �?关键修复：先检查集合是否有效，避免竞态条�?
                var collection = ImageCollection;
                if (collection == null || collection.Count == 0)
                {
                    Debug.WriteLine("[Scroll] �?跳过UpdateVisibleRangeImmediate - ImageCollection为空");
                    return;
                }
                
                var (firstVis, lastVis) = GetVisibleRange();
                if (firstVis >= 0 && lastVis >= 0)
                {
                    _priorityLoader.UpdateVisibleRangeImmediate(firstVis, lastVis, collection.Count);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Scroll] �?UpdateVisibleRangeImmediate异常: {ex.Message}");
            }

            // 防抖：滚动停�?00ms后才触发清理和预加载范围调整
            // 快速滚动时延长防抖时间
            var debounceTime = _isFastScrolling ? 400 : 200;
            
            if (_updateRangeTimer == null)
            {
                _updateRangeTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(debounceTime)
                };
                _updateRangeTimer.Tick += (s, args) =>
                {
                    try
                    {
                        // �?关键修复：先检查集合是否有�?
                        var collection = ImageCollection;
                        if (collection == null || collection.Count == 0)
                        {
                            Debug.WriteLine("[Scroll] �?跳过防抖UpdateLoadRange - ImageCollection为空");
                            return;
                        }
                        
                        // 防抖后执行：清理远离可视区域的缩略图
                        var (first, last) = GetVisibleRange();
                        if (first >= 0)
                        {
                            _priorityLoader.UpdateVisibleRange(first, last, collection.Count);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[Scroll] �?防抖UpdateLoadRange异常: {ex.Message}");
                    }
                    _updateRangeTimer?.Stop();
                    
                    // 滚动停止后升级到高质�?
                    if (_useLowQuality)
                    {
                        _useLowQuality = false;
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            try
                            {
                                // �?关键修复：延迟回调中检查集合有效�?
                                var collection = ImageCollection;
                                if (collection != null && collection.Count > 0)
                                {
                                    UpgradeToHighQuality();
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"[Scroll] �?防抖UpgradeToHighQuality异常: {ex.Message}");
                            }
                        }), DispatcherPriority.Background);
                    }
                };
            }
            else
            {
                _updateRangeTimer.Interval = TimeSpan.FromMilliseconds(debounceTime);
            }
            _updateRangeTimer.Stop();
            _updateRangeTimer.Start();
        }

        /// <summary>
        /// P0优化: 将低质量缩略图升级为高质量（同时处理无缩略图的情况）
        /// 注意：必须在UI线程调用此方法，内部会切换到后台线程处理
        /// �?虚拟化安全：使用 ItemContainerGenerator 精确获取可视范围
        /// </summary>
        private void UpgradeToHighQuality()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            
            // ===== 关键修复：在UI线程获取所有需要的数据 =====
            if (ImageCollection == null || _thumbnailListBox == null)
            {
                Debug.WriteLine($"[UpgradeToHighQuality] �?跳过 - ImageCollection或ListBox为空");
                return;
            }

            // �?使用虚拟化安全的方法获取可视范围
            var (firstVisible, lastVisible) = GetVisibleRange();
            
            if (firstVisible == -1 || lastVisible == -1)
            {
                Debug.WriteLine($"[UpgradeToHighQuality] �?未找到可视项 - 可能正在快速滚动或布局未完�?);
                return;
            }

            var totalCount = ImageCollection.Count;

            // 在UI线程提取需要处理的�?
            var itemsToProcess = new List<(int Index, string FilePath, bool HasThumbnail, double ThumbnailWidth)>();
            for (int i = firstVisible; i <= lastVisible; i++)
            {
                if (i >= 0 && i < ImageCollection.Count)
                {
                    var imageInfo = ImageCollection[i];
                    itemsToProcess.Add((i, imageInfo.FilePath, imageInfo.Thumbnail != null, 
                        imageInfo.Thumbnail?.Width ?? 0));
                }
            }

            // 异步加载高质量缩略图
            Task.Run(() =>
            {
                int loadedCount = 0;
                int upgradedCount = 0;
                int errorCount = 0;
                
                try
                {
                    foreach (var item in itemsToProcess)
                    {
                        // 情况1：无缩略�?- 直接加载高质�?
                        if (!item.HasThumbnail)
                        {
                            try
                            {
                                var highQuality = LoadThumbnailOptimized(item.FilePath, HIGH_QUALITY_SIZE);
                                if (highQuality != null)
                                {
                                    // 使用BeginInvoke更新UI，并传递索�?
                                    int index = item.Index;
                                    Application.Current?.Dispatcher.BeginInvoke(() =>
                                    {
                                        if (index >= 0 && index < ImageCollection.Count)
                                        {
                                            ImageCollection[index].Thumbnail = highQuality;
                                        }
                                    }, DispatcherPriority.Background);
                                    loadedCount++;
                                }
                            }
                            catch (Exception ex)
                            {
                                errorCount++;
                                Debug.WriteLine($"[UpgradeToHighQuality] �?加载失败 index={item.Index}: {ex.Message}");
                            }
                        }
                        // 情况2：低质量缩略�?- 升级为高质量
                        else if (item.ThumbnailWidth < HIGH_QUALITY_SIZE)
                        {
                            try
                            {
                                var highQuality = LoadThumbnailOptimized(item.FilePath, HIGH_QUALITY_SIZE);
                                if (highQuality != null)
                                {
                                    // 使用BeginInvoke更新UI，并传递索�?
                                    int index = item.Index;
                                    Application.Current?.Dispatcher.BeginInvoke(() =>
                                    {
                                        if (index >= 0 && index < ImageCollection.Count)
                                        {
                                            ImageCollection[index].Thumbnail = highQuality;
                                        }
                                    }, DispatcherPriority.Background);
                                    upgradedCount++;
                                }
                            }
                            catch (Exception ex)
                            {
                                errorCount++;
                                Debug.WriteLine($"[UpgradeToHighQuality] �?升级失败 index={item.Index}: {ex.Message}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[UpgradeToHighQuality] �?严重错误: {ex.Message}\n{ex.StackTrace}");
                }
                
                sw.Stop();
            });
        }

        /// <summary>
        /// 释放远离当前索引的图像全分辨率缓存（基于可视区域的动态范围）
        /// </summary>
        private void ReleaseDistantImages(int currentIndex)
        {
            if (ImageCollection == null || ImageCollection.Count == 0)
            {
                return;
            }

            // 动态计算保留范�?
            var (immediateDisplayCount, _, _) = CalculateDynamicLoadCounts();
            int keepRange = Math.Max(2, immediateDisplayCount / 10); // 保留范围为显示数量的10%，最�?�?

            int releaseCount = 0;

            // 只保留当前和相邻图像的全分辨率（距离<=keepRange�?
            for (int i = 0; i < ImageCollection.Count; i++)
            {
                int distance = Math.Abs(i - currentIndex);
                bool shouldRelease = distance > keepRange;

                if (shouldRelease)
                {
                    var imageInfo = ImageCollection[i];
                    bool wasLoaded = imageInfo.IsFullImageLoaded;

                    if (wasLoaded)
                    {
                        ImageCollection[i].ReleaseFullImage();
                        releaseCount++;
                    }
                }
            }


            // 释放完成（静默处理）
        }

        #region 命令实现

        /// <summary>
        /// 添加图像
        /// </summary>
        private async void ExecuteAddImage()
        {
            try
            {
                var openFileDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "图像文件|*.jpg;*.jpeg;*.png;*.bmp;*.tiff|所有文件|*.*",
                    Title = "选择图像文件",
                    Multiselect = true
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    var fileNames = openFileDialog.FileNames;

                    // 创建新的取消令牌
                    _loadingCancellationTokenSource?.Cancel();
                    _loadingCancellationTokenSource?.Dispose();
                    _loadingCancellationTokenSource = new CancellationTokenSource();

                    var cancellationToken = _loadingCancellationTokenSource.Token;

                    await LoadImagesOptimizedAsync(fileNames, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // 用户取消了操�?
            }
            catch (Exception ex)
            {
                MessageBox.Show($"添加图像失败: {ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 计算基于可视区域的动态加载数�?
        /// �?虚拟化安全：使用 ItemContainerGenerator 精确计算
        /// </summary>
        private (int immediateDisplayCount, int immediateThumbnailCount, int batchSize) CalculateDynamicLoadCounts()
        {
            if (_thumbnailListBox == null || ImageCollection == null)
            {
                // 如果ListBox未初始化，使用默认�?
                return (20, 5, 10);
            }

            // �?使用虚拟化安全的方法获取可视范围
            var (firstVisible, lastVisible) = GetVisibleRange();
            
            if (firstVisible == -1 || lastVisible == -1)
            {
                // 如果未找到可视项，使用默认�?
                return (20, 5, 10);
            }

            // 计算可视区域能显示多少图�?
            int viewportCapacity = lastVisible - firstVisible + 1;
            int immediateDisplayCount = viewportCapacity;

            // 缩略图数量为显示数量�?/4（最�?张）
            int immediateThumbnailCount = Math.Max(3, immediateDisplayCount / 4);

            // 批次大小为显示数量的1/2（最�?张）
            int batchSize = Math.Max(5, immediateDisplayCount / 2);

            return (immediateDisplayCount, immediateThumbnailCount, batchSize);
        }

        /// <summary>
        /// 计算最优并发数（动态并发数优化�?
        /// </summary>
        private int CalculateOptimalConcurrency()
        {
            int cpuCount = Environment.ProcessorCount;
            bool isGpuInitialized = s_gpuDecoder.IsInitialized;
            bool isCpuInitialized = s_cpuDecoder.IsInitialized;

            if (isGpuInitialized || isCpuInitialized)
            {
                // 双解码器模式：适中并发数，充分利用并行能力
                int concurrency = Math.Min(6, Math.Max(3, (int)(cpuCount / 1.5)));
                Debug.WriteLine($"[ImagePreviewControl] 动态并发数: {concurrency} (CPU核心�?{cpuCount})");
                return concurrency;
            }
            else
            {
                // CPU模式：充分利用多�?
                int cpuConcurrency = Math.Max(2, (int)(cpuCount * 0.75));
                Debug.WriteLine($"[ImagePreviewControl] 动态并发数（CPU模式�? {cpuConcurrency} (CPU核心�?{cpuCount})");
                return cpuConcurrency;
            }
        }

        /// <summary>
        /// 优化的图像加载方法（简化版：使用优先级加载器）
        /// 优化：统一计时日志、文件预取、等待可视区域完�?
        /// </summary>
        private async Task LoadImagesOptimizedAsync(
            string[] fileNames,
            CancellationToken cancellationToken)
        {
            // ===== 统一计时：从用户角度记录有意义的时间 =====
            var totalSw = Stopwatch.StartNew();
            var firstImageSw = Stopwatch.StartNew();
            TimeSpan? firstImageTime = null;
            TimeSpan? visibleAreaTime = null;

            try
            {
                // 清空优先级加载器的状�?
                _priorityLoader.ClearState();
                
                // �?日志优化：重置首张图片追踪计数器
                SmartThumbnailLoader.ResetLoadCounter();

                Debug.WriteLine("");
                Debug.WriteLine("╔══════════════════════════════════════════════════════════════╗");
                Debug.WriteLine("�?          图像加载 - 性能计时开�?                             �?);
                Debug.WriteLine($"�? 图片数量: {fileNames.Length} �?);
                Debug.WriteLine("╚══════════════════════════════════════════════════════════════╝");

                // ===== 步骤1：预创建所有ImageInfo对象 =====
                var step1Sw = Stopwatch.StartNew();
                var allImages = new ImageInfo[fileNames.Length];
                for (int i = 0; i < fileNames.Length; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    allImages[i] = new ImageInfo
                    {
                        Name = Path.GetFileNameWithoutExtension(fileNames[i]),
                        FilePath = fileNames[i],
                        Thumbnail = null,
                        FullImage = null
                    };
                }
                step1Sw.Stop();
                Debug.WriteLine($"[LoadImages] 步骤1-创建对象: {step1Sw.ElapsedMilliseconds}ms");

                // ===== 步骤2：更新UI（批量添加，性能优化�?=====
                var step2Sw = Stopwatch.StartNew();
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    // 使用批量替换，只触发一次Reset事件，性能提升30-50�?
                    ImageCollection.ReplaceRange(allImages);

                    if (ImageCollection.Count > 0)
                    {
                        CurrentImageIndex = 0;
                    }

                    // 设置图像集合引用
                    _priorityLoader.SetImageCollection(ImageCollection);
                    
                    // 注意：移除UpdateLayout()调用，依赖虚拟化自动处理
                }, System.Windows.Threading.DispatcherPriority.Normal);
                step2Sw.Stop();
                Debug.WriteLine($"[LoadImages] 步骤2-更新UI(批量优化): {step2Sw.ElapsedMilliseconds}ms");

                // ===== 步骤3：使用优先级加载器加载首�?=====
                var step3Sw = Stopwatch.StartNew();
                
                // 设置首张图片加载完成的回�?
                var firstImageTcs = new TaskCompletionSource<bool>();
                
                await _priorityLoader.LoadInitialScreenAsync(fileNames, ImageCollection, index =>
                {
                    // 首张图片加载完成
                    firstImageSw.Stop();
                    firstImageTime = firstImageSw.Elapsed;
                    Debug.WriteLine($"[LoadImages] ★★�?首张缩略图显�?- 耗时: {firstImageTime.Value.TotalMilliseconds:F0}ms ★★�?);
                    firstImageTcs.TrySetResult(true);
                });
                step3Sw.Stop();

                // 等待首张图片加载完成
                await firstImageTcs.Task;

                // ===== 步骤4：文件预取（在首张显示后启动，避免I/O竞争�?=====
                var prefetchSw = Stopwatch.StartNew();
                int prefetchCount = Math.Min(20, fileNames.Length);
                _ = Task.Run(() =>
                {
                    try
                    {
                        for (int i = 0; i < prefetchCount; i++)
                        {
                            if (cancellationToken.IsCancellationRequested) break;
                            s_smartLoader.PrefetchFile(fileNames[i]);
                        }
                    }
                    catch { }
                }, cancellationToken);
                prefetchSw.Stop();
                Debug.WriteLine($"[LoadImages] 文件预取启动: {prefetchSw.ElapsedMilliseconds}ms (预取{prefetchCount}�?");

                // ===== 移除步骤4：等待可视区域加载完�?=====
                // 原因：PriorityThumbnailLoader 已有完整的可视区域监控和报告
                // 移除冗余逻辑，避免阻塞和错误计算

                // ===== 步骤5：后台预加载全分辨率图像 =====
                _ = Task.Run(() =>
                {
                    int preloadCount = Math.Min(3, fileNames.Length);
                    for (int i = 0; i < preloadCount; i++)
                    {
                        try
                        {
                            if (cancellationToken.IsCancellationRequested) break;

                            var fullImage = LoadImageOptimized(fileNames[i]);
                            if (fullImage != null && !cancellationToken.IsCancellationRequested)
                            {
                                Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
                                {
                                    try
                                    {
                                        if (i < ImageCollection.Count)
                                        {
                                            ImageCollection[i].SetFullImage(fullImage);
                                        }
                                    }
                                    catch { }
                                }), System.Windows.Threading.DispatcherPriority.Background);
                            }
                        }
                        catch { }
                    }
                }, cancellationToken);

                totalSw.Stop();

                // ===== 简化的性能报告 =====
                Debug.WriteLine("");
                Debug.WriteLine("╔══════════════════════════════════════════════════════════════╗");
                Debug.WriteLine("�?          图像加载 - 性能报告                                  �?);
                Debug.WriteLine("╠══════════════════════════════════════════════════════════════╣");
                Debug.WriteLine($"�? 总图片数:      {fileNames.Length} �?);
                Debug.WriteLine($"�? �?首张缩略�?   {firstImageTime?.TotalMilliseconds:F0}ms (用户首次看到图片)");
                Debug.WriteLine($"�? 总耗时:        {totalSw.ElapsedMilliseconds}ms");
                Debug.WriteLine("�? 可视区域加载报告 �?�?PriorityLoader 日志");
                Debug.WriteLine("╚══════════════════════════════════════════════════════════════╝");
                Debug.WriteLine("");
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine($"[LoadImages] �?加载被取�?);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LoadImages] �?加载异常: {ex.Message}");
            }
        }



        /// <summary>
        /// 添加文件�?
        /// </summary>
        private async void ExecuteAddFolder()
        {
            try
            {
                var openFileDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "图像文件|*.jpg;*.jpeg;*.png;*.bmp;*.tiff|所有文件|*.*",
                    Title = "选择文件夹中的任意一个图像文�?,
                    Multiselect = false
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    var filePath = openFileDialog.FileName;
                    var folderPath = Path.GetDirectoryName(filePath);

                    if (string.IsNullOrEmpty(folderPath))
                    {
                        MessageBox.Show("无法获取文件夹路�?, "错误",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".bmp", ".tiff", ".tif" };

                    var imageFiles = Directory.GetFiles(folderPath, "*.*", SearchOption.TopDirectoryOnly)
                        .Where(f => imageExtensions.Contains(Path.GetExtension(f).ToLower()))
                        .OrderBy(f => f)
                        .ToArray();

                    if (imageFiles.Length == 0)
                    {
                        MessageBox.Show("所选文件夹中没有找到图像文�?, "提示",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    // 创建新的取消令牌
                    _loadingCancellationTokenSource?.Cancel();
                    _loadingCancellationTokenSource?.Dispose();
                    _loadingCancellationTokenSource = new CancellationTokenSource();

                    var cancellationToken = _loadingCancellationTokenSource.Token;

                    await LoadImagesOptimizedAsync(imageFiles, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // 用户取消了操�?
            }
            catch (Exception ex)
            {
                MessageBox.Show($"添加文件夹失�? {ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 删除单个图像（通过缩略图上的删除按钮）
        /// </summary>
        private void ExecuteDeleteSingleImage(ImageInfo? imageInfo)
        {
            Debug.WriteLine($"[ImagePreviewControl] ExecuteDeleteSingleImage 被调�?- imageInfo: {imageInfo?.Name ?? "null"}");

            if (imageInfo == null)
            {
                Debug.WriteLine($"[ImagePreviewControl] ExecuteDeleteSingleImage - imageInfo �?null，返�?);
                return;
            }

            int index = ImageCollection.IndexOf(imageInfo);
            if (index >= 0)
            {
                Debug.WriteLine($"[ImagePreviewControl] 准备删除索引 {index} 的图�?- {imageInfo.Name}");
                Debug.WriteLine($"[ImagePreviewControl] 删除�?- ImageCollection.Count:{ImageCollection.Count}");

                ImageCollection.RemoveAt(index);

                Debug.WriteLine($"[ImagePreviewControl] 删除�?- ImageCollection.Count:{ImageCollection.Count}");

                // 关键修复：删除图片后，所有后续图片的索引都发生了错位
                // 必须清除PriorityLoader的_loadedIndices缓存，否则会导致"假已加载"状�?
                Debug.WriteLine($"[ImagePreviewControl] 清除PriorityLoader的缓存（修复索引错位问题�?..");
                _priorityLoader.ClearState();
                Debug.WriteLine($"[ImagePreviewControl] �?PriorityLoader缓存已清�?);

                // 调整当前索引
                int oldIndex = CurrentImageIndex;
                if (ImageCollection.Count == 0)
                {
                    CurrentImageIndex = -1;
                }
                else if (CurrentImageIndex >= ImageCollection.Count)
                {
                    CurrentImageIndex = ImageCollection.Count - 1;
                }
                else if (CurrentImageIndex > index)
                {
                    // 如果删除的是当前索引之前的图像，当前索引需要减1
                    CurrentImageIndex--;
                }

                Debug.WriteLine($"[ImagePreviewControl] 索引调整 - 旧索�?{oldIndex}, 新索�?{CurrentImageIndex}, 删除的索�?{index}");

                // 无论索引是否变化，都强制刷新相关UI状�?
                RefreshImageDisplayState();
            }
        }

        /// <summary>
        /// 清除所有图�?
        /// </summary>
        private void ExecuteClearAll()
        {
            if (ImageCollection.Count == 0)
                return;

            var result = MessageBox.Show(
                $"确定要清除所�?{ImageCollection.Count} 张图像吗?",
                "确认清除",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                ImageCollection.Clear();
                CurrentImageIndex = -1;
            }
        }

        private bool CanExecuteClearAll()
        {
            return ImageCollection?.Count > 0;
        }



        #endregion

        #region 辅助方法

        /// <summary>
        /// 当前图像索引更改回调
        /// </summary>
        private static void OnCurrentImageIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs _)
        {
            var control = (ImagePreviewControl)d;
            control.UpdateImageSelection();
            control.OnPropertyChanged(nameof(ImageCountDisplay));

            // 同步ListBox的SelectedItem
            if (control._thumbnailListBox != null && control.ImageCollection != null &&
                control.CurrentImageIndex >= 0 && control.CurrentImageIndex < control.ImageCollection.Count)
            {
                var selectedImage = control.ImageCollection[control.CurrentImageIndex];
                control._thumbnailListBox.SelectedItem = selectedImage;
            }

            // �?关键修复：先检查集合是否有�?
            var collection = control.ImageCollection;
            if (collection != null && collection.Count > 0 && control.CurrentImageIndex >= 0)
            {
                // 智能预加载相邻图�?
                control.PreloadAdjacentImages(control.CurrentImageIndex);

                // 释放远离当前索引的图�?
                control.ReleaseDistantImages(control.CurrentImageIndex);
            }
        }

        /// <summary>
        /// 图像集合更改回调
        /// 优化：延迟渲�?+ 占位符策略，避免切换节点时卡�?
        /// </summary>
        private static void OnImageCollectionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (ImagePreviewControl)d;
            var oldCollection = e.OldValue as BatchObservableCollection<ImageInfo>;
            var newCollection = e.NewValue as BatchObservableCollection<ImageInfo>;

            // �?优化：如果新旧集合引用相同，跳过处理
            // 这避免了从非采集节点切回采集节点时不必要的缩略图清空
            if (oldCollection == newCollection && newCollection != null)
            {
                Debug.WriteLine($"[OnImageCollectionChanged] 相同集合引用，跳过处�?);
                return;
            }

            // 更新基本属�?
            control.UpdateImageSelection();
            control.OnPropertyChanged(nameof(ImageCountDisplay));

            // �?关键修复：当集合变为空或 null 时，必须清理加载器状�?
            // 否则 _priorityLoader 仍持有旧集合引用，导致异步任务操作旧数据产生 NullReferenceException
            if (newCollection == null || newCollection.Count == 0)
            {
                control._priorityLoader.ClearState();
                control._priorityLoader.SetImageCollection(null);
                Debug.WriteLine($"[OnImageCollectionChanged] 集合变为空，已清理加载器状�?);
                return;
            }

            // 延迟渲染优化：异步加载可视区域缩略图
            control.ScheduleDeferredThumbnailLoading(newCollection);
        }

        /// <summary>
        /// 延迟缩略图加载计时器
        /// </summary>
        private DispatcherTimer? _deferredLoadingTimer;

        /// <summary>
        /// 安排延迟缩略图加载（避免切换节点时卡顿）
        /// </summary>
        private void ScheduleDeferredThumbnailLoading(BatchObservableCollection<ImageInfo> collection)
        {
            Debug.WriteLine($"[ScheduleDeferred] 开始安排延迟加�? collection.Count={collection?.Count ?? -1}");
            
            // 取消之前的延迟加载任�?
            _deferredLoadingTimer?.Stop();
            _priorityLoader.ClearState();

            // �?关键修复：无论是否有缩略图，都必须设置新的图像集合引�?
            // 否则 _priorityLoader �?_imageCollection 仍指向旧节点，导致索引越�?
            _priorityLoader.SetImageCollection(collection);

            // 如果集合中没有已加载的缩略图，需要主动触发加�?
            bool hasAnyThumbnail = collection.Any(img => img.Thumbnail != null);
            Debug.WriteLine($"[ScheduleDeferred] hasAnyThumbnail={hasAnyThumbnail}");
            
            // �?修复：无论是否有缩略图，都需要触发可视区域加�?
            // 原问题：当没有缩略图时直接返回，依赖滚动事件触发，但切换节点后可能没有滚动事�?
            if (!hasAnyThumbnail)
            {
                // 没有已加载的缩略图，延迟后触发可视区域加�?
                // 使用更长的延迟确保UI布局完成
                _deferredLoadingTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(100) // 100ms 延迟，确保布局完成
                };
                _deferredLoadingTimer.Tick += (s, args) =>
                {
                    _deferredLoadingTimer?.Stop();
                    // �?关键修复：重新验证集合是否仍然有�?
                    var currentCollection = ImageCollection;
                    if (currentCollection == null || currentCollection.Count == 0)
                    {
                        Debug.WriteLine($"[DeferredLoading] �?定时器触发时集合已失效，跳过加载");
                        return;
                    }
                    Debug.WriteLine($"[DeferredLoading] 定时器触发，开始加载可视区�?);
                    LoadVisibleRangeThumbnails(currentCollection);
                };
                _deferredLoadingTimer.Start();
                return;
            }

            // 情况2：集合中已有缩略图（从其他节点切换过来）
            // 策略：先清空显示占位符，再异步加载可视区�?
            var sw = Stopwatch.StartNew();

            // 清空所有缩略图显示（保留文件路径数据）
            foreach (var image in collection)
            {
                image.Thumbnail = null;
            }
            sw.Stop();
            Debug.WriteLine($"[DeferredLoading] 清空缩略图显�? {collection.Count}�? 耗时:{sw.ElapsedMilliseconds}ms");

            // 延迟一帧后开始加载可视区域（确保占位符已渲染�?
            _deferredLoadingTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(50) // 50ms 延迟，确�?UI 更新完成
            };
            _deferredLoadingTimer.Tick += (s, args) =>
            {
                _deferredLoadingTimer?.Stop();
                // �?关键修复：重新验证集合是否仍然有�?
                var currentCollection = ImageCollection;
                if (currentCollection == null || currentCollection.Count == 0)
                {
                    Debug.WriteLine($"[DeferredLoading] �?定时器触发时集合已失效，跳过加载");
                    return;
                }
                Debug.WriteLine($"[DeferredLoading] 定时器触发，开始加载可视区�?);
                LoadVisibleRangeThumbnails(currentCollection);
            };
            _deferredLoadingTimer.Start();
        }

        /// <summary>
        /// 异步加载可视区域的缩略图
        /// �?增强重试机制：当可视范围获取失败时自动重�?
        /// </summary>
        private void LoadVisibleRangeThumbnails(BatchObservableCollection<ImageInfo> collection)
        {
            if (collection == null || collection.Count == 0)
                return;

            var sw = Stopwatch.StartNew();
            Debug.WriteLine($"[DeferredLoading] 开始异步加载可视区域缩略图...");

            // 获取可视范围
            var (firstVisible, lastVisible) = GetVisibleRange();

            // �?增强重试：如果可视范围无效，延迟重试
            if (firstVisible == -1 || lastVisible == -1)
            {
                Debug.WriteLine($"[DeferredLoading] �?可视范围无效，延迟重�?..");
                
                // 延迟重试（最多重�?次）
                _deferredLoadingTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(100)
                };
                int retryCount = 0;
                _deferredLoadingTimer.Tick += (s, args) =>
                {
                    retryCount++;
                    var (retryFirst, retryLast) = GetVisibleRange();
                    
                    if (retryFirst >= 0 && retryLast >= 0)
                    {
                        _deferredLoadingTimer?.Stop();
                        Debug.WriteLine($"[DeferredLoading] �?重试成功，可视范�? [{retryFirst}, {retryLast}]");
                        ExecuteLoadVisibleRange(collection, retryFirst, retryLast, sw);
                    }
                    else if (retryCount >= 2)
                    {
                        // 重试失败，使用默认值强制加�?
                        _deferredLoadingTimer?.Stop();
                        int defaultLast = Math.Min(collection.Count - 1, 15);
                        Debug.WriteLine($"[DeferredLoading] �?重试失败，使用默认范�? [0, {defaultLast}]");
                        ExecuteLoadVisibleRange(collection, 0, defaultLast, sw);
                    }
                };
                _deferredLoadingTimer.Start();
                return;
            }

            Debug.WriteLine($"[DeferredLoading] 可视范围: [{firstVisible}, {lastVisible}]");
            ExecuteLoadVisibleRange(collection, firstVisible, lastVisible, sw);
        }

        /// <summary>
        /// 执行可视区域加载
        /// </summary>
        private void ExecuteLoadVisibleRange(BatchObservableCollection<ImageInfo> collection, int firstVisible, int lastVisible, Stopwatch sw)
        {
            // 设置当前图像索引
            if (CurrentImageIndex < 0 && collection.Count > 0)
            {
                CurrentImageIndex = 0;
            }

            // 使用优先级加载器加载可视区域
            var filePaths = collection.Select(img => img.FilePath).ToArray();
            _ = _priorityLoader.LoadInitialScreenAsync(filePaths, collection, index =>
            {
                // 首张图片加载完成
                sw.Stop();
                Debug.WriteLine($"[DeferredLoading] �?首张缩略图加载完�?- 耗时: {sw.ElapsedMilliseconds}ms");
            });
        }

        /// <summary>
        /// 图像运行模式更改回调
        /// </summary>
        private static void OnImageRunModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs _)
        {
            var control = (ImagePreviewControl)d;
            control.OnPropertyChanged(nameof(ImageRunMode));
            
            // 刷新命令状�?
            CommandManager.InvalidateRequerySuggested();
        }




        /// <summary>
        /// 刷新图像显示状态（修复删除图片后UI不更新和缩略图显示问题）
        /// </summary>
        private void RefreshImageDisplayState()
        {
            // 强制刷新ImageCountDisplay（确保删除后总数正确更新�?
            OnPropertyChanged(nameof(ImageCountDisplay));

            // 强制刷新缩略图选中状�?
            UpdateImageSelection();

            // 确保触发预加载和释放机制（修复缩略图只显示占位图问题�?
            if (CurrentImageIndex >= 0)
            {
                PreloadAdjacentImages(CurrentImageIndex);
                ReleaseDistantImages(CurrentImageIndex);

                // 同步ListBox的SelectedItem
                if (_thumbnailListBox != null && ImageCollection != null &&
                    CurrentImageIndex >= 0 && CurrentImageIndex < ImageCollection.Count)
                {
                    var selectedImage = ImageCollection[CurrentImageIndex];
                    _thumbnailListBox.SelectedItem = selectedImage;
                }

                // 触发可视区域更新
                UpdateLoadRange();
            }
            else
            {
                // 如果没有选中图像，清除ListBox选中�?
                if (_thumbnailListBox != null)
                {
                    _thumbnailListBox.SelectedItem = null;
                }
            }
        }

        /// <summary>
        /// 更新图像选中状�?
        /// </summary>
        private void UpdateImageSelection()
        {
            if (ImageCollection == null) return;

            for (int i = 0; i < ImageCollection.Count; i++)
            {
                ImageCollection[i].IsSelected = (i == CurrentImageIndex);
            }
        }

        /// <summary>
        /// 缩略图点击事件处�?
        /// </summary>
        private void OnThumbnailClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Debug.WriteLine($"[ImagePreviewControl] OnThumbnailClick 触发 - e.Handled: {e.Handled}, OriginalSource: {e.OriginalSource?.GetType().Name}");

            // 如果点击的是删除按钮或CheckBox，跳过处�?
            DependencyObject? current = e.OriginalSource as DependencyObject;
            while (current != null)
            {
                if (current is Button || current is CheckBox)
                {
                    Debug.WriteLine($"[ImagePreviewControl] OnThumbnailClick - 点击的是按钮或CheckBox，跳过处�?);
                    return;
                }
                current = VisualTreeHelper.GetParent(current);
            }

            if (sender is Border border && border.Tag is ImageInfo imageInfo)
            {
                int index = ImageCollection.IndexOf(imageInfo);
                Debug.WriteLine($"[ImagePreviewControl] OnThumbnailClick - 图像索引: {index}, 文件�? {imageInfo.Name}");

                if (index >= 0)
                {
                    if (ImageRunMode == ImageRunMode.运行选择)
                    {
                        // �?运行选择模式：只有已勾选的图片才执行工作流
                        if (imageInfo.IsForRun)
                        {
                            Debug.WriteLine($"[ImagePreviewControl] �?执行工作�?- 模式:运行选择, 图像:{imageInfo.Name}, 索引:{index}");
                            RequestWorkflowExecution(imageInfo, index);
                        }
                        else
                        {
                            Debug.WriteLine($"[ImagePreviewControl] 图片未勾�?- 图像:{imageInfo.Name}，不执行工作�?);
                        }
                    }
                    else
                    {
                        // �?运行全部模式：点击任意图片执行工作流
                        Debug.WriteLine($"[ImagePreviewControl] �?执行工作�?- 模式:运行全部, 图像:{imageInfo.Name}, 索引:{index}");
                        RequestWorkflowExecution(imageInfo, index);
                    }
                }
            }
        }

        /// <summary>
        /// 请求执行工作�?
        /// </summary>
        private void RequestWorkflowExecution(ImageInfo imageInfo, int index)
        {
            // 切换当前显示的图�?
            CurrentImageIndex = index;
            
            // 触发工作流执行请求事�?
            WorkflowExecutionRequested?.Invoke(this, new WorkflowExecutionRequestEventArgs
            {
                ImageInfo = imageInfo,
                Index = index
            });
        }

        /// <summary>
        /// 删除按钮预点击事件处�?- 用于日志记录，不阻止事件传�?
        /// </summary>
        private void OnDeleteButtonPreview(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Debug.WriteLine($"[ImagePreviewControl] OnDeleteButtonPreview 触发 - Sender: {sender?.GetType().Name}, OriginalSource: {e.OriginalSource?.GetType().Name}");

            // 不设�?e.Handled = true，让事件继续传递到按钮�?Click 事件
            // PreviewMouseLeftButtonDown 会先�?Click 事件触发
        }

        /// <summary>
        /// 缩略图ListBox选择改变事件处理
        /// </summary>
        private void OnThumbnailListBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && e.AddedItems[0] is ImageInfo selectedImage)
            {
                int index = ImageCollection?.IndexOf(selectedImage) ?? -1;

                if (index >= 0)
                {
                    CurrentImageIndex = index;
                }
            }
        }

        /// <summary>
        /// 运行模式按钮点击事件处理
        /// </summary>
        private void OnRunModeButtonClick(object sender, RoutedEventArgs e)
        {
            if (RunModePopup != null)
            {
                RunModePopup.IsOpen = !RunModePopup.IsOpen;
            }
        }

        /// <summary>
        /// 运行模式下拉列表鼠标按下事件处理
        /// 使用PreviewMouseDown确保在点击任意项时都能立即响�?
        /// </summary>
        private void OnRunModeListBoxPreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (RunModeListBox == null || RunModePopup == null)
                return;

            // 获取点击的源元素
            var originalSource = e.OriginalSource as DependencyObject;
            if (originalSource == null)
                return;

            // 查找对应的ListBoxItem
            var listBoxItem = FindParent<ListBoxItem>(originalSource);
            if (listBoxItem == null)
                return;

            // 获取选中�?
            ImageRunMode? selectedMode = null;
            if (listBoxItem.Content is TextBlock textBlock && textBlock.DataContext is ImageRunMode mode1)
            {
                selectedMode = mode1;
            }
            else if (listBoxItem.DataContext is ImageRunMode mode2)
            {
                selectedMode = mode2;
            }

            // 更新运行模式
            if (selectedMode.HasValue)
            {
                ImageRunMode = selectedMode.Value;
            }

            // 立即关闭Popup
            RunModePopup.IsOpen = false;

            // 标记事件已处理，防止冒泡导致其他问题
            e.Handled = true;
        }

        /// <summary>
        /// 查找父元�?
        /// </summary>
        private static T? FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            var parentObject = VisualTreeHelper.GetParent(child);

            if (parentObject == null)
                return null;

            if (parentObject is T parent)
                return parent;

            return FindParent<T>(parentObject);
        }



        /// <summary>
        /// 属性更改通知
        /// </summary>
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// 加载图像（已废弃，请使用LoadImageOptimized�?
        /// </summary>
        [System.Obsolete("请使用LoadImageOptimized代替")]
        private BitmapImage? LoadImage(string filePath)
        {
            return LoadImageOptimized(filePath);
        }




        /// <summary>
        /// 更新加载范围（滚动时调用�?
        /// 使用优先级加载器
        /// �?虚拟化安全：使用 ItemContainerGenerator 精确获取可视范围
        /// </summary>
        private void UpdateLoadRange()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            
            if (_thumbnailListBox == null || ImageCollection == null || ImageCollection.Count == 0)
            {
                Debug.WriteLine($"[UpdateLoadRange] �?跳过 - ListBox或集合为�?(listBox:{_thumbnailListBox != null} collection:{ImageCollection != null} count:{ImageCollection?.Count ?? 0})");
                return;
            }

            // �?使用虚拟化安全的方法获取可视范围
            var (firstVisible, lastVisible) = GetVisibleRange();
            
            if (firstVisible == -1 || lastVisible == -1)
            {
                Debug.WriteLine($"[UpdateLoadRange] �?未找到可视项 - GetVisibleRange返回(-1,-1)");
                return;
            }

            var viewportWidth = _thumbnailListBox.ActualWidth;
            
            // ===== 缩略图显示状态监控（增强诊断�?=====
            int loadedCount = 0, emptyCount = 0, validCount = 0, invalidCount = 0;
            var emptyIndices = new System.Text.StringBuilder();
            var invalidDetails = new System.Text.StringBuilder();
            
            for (int i = firstVisible; i <= lastVisible && i < ImageCollection.Count; i++)
            {
                var item = ImageCollection[i];
                if (item.Thumbnail != null)
                {
                    loadedCount++;
                    // �?诊断：检查缩略图是否真的有效（有宽高�?
                    if (item.Thumbnail.Width > 0 && item.Thumbnail.Height > 0)
                    {
                        validCount++;
                    }
                    else
                    {
                        invalidCount++;
                        if (invalidDetails.Length < 200)
                            invalidDetails.Append($"[{i}:W{item.Thumbnail.Width:F0}xH{item.Thumbnail.Height:F0}]");
                    }
                }
                else
                {
                    emptyCount++;
                    if (emptyIndices.Length < 100) // 限制长度
                        emptyIndices.Append($"{i},");
                }
            }
            
            // 缩略图有效性监控（静默处理�?
            
            if (invalidCount > 0)
            {
                Debug.WriteLine($"[ThumbnailMonitor] �?无效缩略�? {invalidDetails}");
            }
            
            if (emptyCount > 0)
            {
                Debug.WriteLine($"[ThumbnailMonitor] 空白索引:[{emptyIndices}]");
            }

            // 委托给优先级加载器（�?关键修复：先检查集合是否有效）
            var collection = ImageCollection;
            if (collection != null && collection.Count > 0)
            {
                _priorityLoader.UpdateVisibleRange(firstVisible, lastVisible, collection.Count);
            }
            else
            {
                Debug.WriteLine($"[UpdateLoadRange] �?跳过委托调用 - 集合已变为空");
            }
            
            sw.Stop();
        }

        #endregion

        #region 事件

        /// <summary>
        /// 获取当前可视区域内的数据项索引范围（虚拟化安全版本）
        /// 核心原理：虚拟化模式�?ScrollViewer �?HorizontalOffset 返回的是"项索�?而非像素
        /// </summary>
        /// <returns>返回(firstVisible, lastVisible)，如果找不到返回(-1, -1)</returns>
        private (int firstVisible, int lastVisible) GetVisibleRange()
        {
            // ===== 诊断日志：检查前置条�?=====
            var callStack = new System.Diagnostics.StackTrace();
            var caller = callStack.GetFrame(1)?.GetMethod()?.Name ?? "unknown";
            
            if (_thumbnailListBox == null)
            {
                Debug.WriteLine($"[GetVisibleRange] �?返回(-1,-1) - _thumbnailListBox为null, caller={caller}");
                return (-1, -1);
            }
            
            if (ImageCollection == null || ImageCollection.Count == 0)
            {
                Debug.WriteLine($"[GetVisibleRange] �?返回(-1,-1) - ImageCollection为空 (null:{ImageCollection == null}, count:{ImageCollection?.Count ?? 0}), caller={caller}");
                return (-1, -1);
            }

            var scrollViewer = FindVisualChild<ScrollViewer>(_thumbnailListBox);
            if (scrollViewer == null)
            {
                Debug.WriteLine($"[GetVisibleRange] �?返回(-1,-1) - scrollViewer为null, caller={caller}");
                return (-1, -1);
            }

            double itemWidth = ThumbnailSizes.ItemWidth;
            if (itemWidth <= 0)
            {
                Debug.WriteLine($"[GetVisibleRange] �?返回(-1,-1) - itemWidth无效: {itemWidth}, caller={caller}");
                return (-1, -1);
            }

            // ===== 关键修复：虚拟化模式�?ScrollViewer 的值含�?=====
            // HorizontalOffset = 当前滚动位置（项索引，不是像素！�?
            // ExtentWidth = 总项数（不是像素宽度！）
            // ViewportWidth = 可见项数（不是像素宽度！�?
            
            double offset = scrollViewer.HorizontalOffset;
            double extentWidth = scrollViewer.ExtentWidth;
            double scrollViewportWidth = scrollViewer.ViewportWidth;
            
            // 判断 ScrollViewer 是否处于虚拟化模�?
            // 如果 extentWidth �?图片数量，说明是虚拟化模式，offset 是项索引
            bool isVirtualizationMode = ImageCollection.Count > 0 && 
                                         Math.Abs(extentWidth - ImageCollection.Count) < ImageCollection.Count * 0.1;
            
            int firstVisible;
            int lastVisible;
            int visibleCount;
            
            if (isVirtualizationMode && extentWidth > 0 && scrollViewportWidth > 0)
            {
                // 虚拟化模式：直接使用 ScrollViewer 的值（已经是项索引�?
                // ViewportWidth 已经是精确的可见项数
                visibleCount = (int)Math.Ceiling(scrollViewportWidth);
                firstVisible = Math.Max(0, (int)offset);
                lastVisible = Math.Min(ImageCollection.Count - 1, firstVisible + visibleCount - 1);
                
                // �?仅添�?张缓冲（防止滚动时边缘白屏）
                firstVisible = Math.Max(0, firstVisible - 1);
                lastVisible = Math.Min(ImageCollection.Count - 1, lastVisible + 1);
            }
            else
            {
                // 非虚拟化模式或回退：基于像素宽度计�?
                double viewportWidth = scrollViewportWidth > itemWidth ? scrollViewportWidth : 
                                       _thumbnailListBox.ActualWidth > itemWidth ? _thumbnailListBox.ActualWidth : 
                                       this.ActualWidth;
                
                if (viewportWidth < itemWidth)
                {
                    Debug.WriteLine($"[GetVisibleRange] �?返回(-1,-1) - 视口宽度无效: {viewportWidth:F1}");
                    return (-1, -1);
                }
                
                visibleCount = (int)(viewportWidth / itemWidth) + 1; // 只加1作为舍入补偿
                firstVisible = Math.Max(0, (int)(offset / itemWidth));
                lastVisible = Math.Min(ImageCollection.Count - 1, (int)((offset + viewportWidth) / itemWidth));
                
                // �?仅添�?张缓�?
                firstVisible = Math.Max(0, firstVisible - 1);
                lastVisible = Math.Min(ImageCollection.Count - 1, lastVisible + 1);
            }

            int itemsInViewport = lastVisible - firstVisible + 1;

            return (firstVisible, lastVisible);
        }

        /// <summary>
        /// 可视区域加载完成事件处理
        /// </summary>
        private void OnVisibleAreaLoadingCompleted(int loadedCount, TimeSpan totalDuration)
        {
            Debug.WriteLine("");
            Debug.WriteLine("========================================");
            Debug.WriteLine("★★�?可视区域加载完成 ★★�?);
            Debug.WriteLine($"  加载数量: {loadedCount} �?);
            Debug.WriteLine($"  总耗时: {totalDuration.TotalMilliseconds:F2}ms");
            Debug.WriteLine("========================================");
            Debug.WriteLine("");
        }

        /// <summary>
        /// 内存压力变化事件处理（P0优化：自适应策略�?
        /// </summary>
        private void OnMemoryPressureChanged(object? sender, MemoryPressureMonitor.MemoryPressureEventArgs e)
        {
            Debug.WriteLine($"[ImagePreviewControl] �?内存压力变化: {e.Level} (可用:{e.AvailableMemoryMB}MB, {e.AvailablePercent:F1}%)");
            Debug.WriteLine($"[ImagePreviewControl]   建议操作: {e.RecommendedAction}");

            // �?P1优化：通知加载器调整并发度
            _priorityLoader.SetMemoryPressure(e.Level);

            // 根据压力级别自适应响应
            switch (e.Level)
            {
                case MemoryPressureMonitor.PressureLevel.Moderate:
                    // 中等压力：减少预读取，保持稳�?
                    s_smartLoader.ClearPrefetchCache(); // 清除预读取缓�?
                    // �?方案A: 同步状�?
                    _priorityLoader.SyncLoadedIndicesWithActualThumbnails();
                    Debug.WriteLine("[ImagePreviewControl] �?中等压力响应：清除预读取缓存+状态同�?);
                    break;

                case MemoryPressureMonitor.PressureLevel.High:
                    // 高压力：清理缓存，降低质�?
                    s_thumbnailCache.RespondToMemoryPressure(isCritical: false);
                    s_smartLoader.ClearPrefetchCache();
                    // �?方案A: 同步状�?
                    _priorityLoader.SyncLoadedIndicesWithActualThumbnails();
                    Debug.WriteLine("[ImagePreviewControl] �?高压力响应：清理缓存+低质量模�?状态同�?);
                    break;

                case MemoryPressureMonitor.PressureLevel.Critical:
                    // 危险：强制GC，清空缓存，持续低质量模�?
                    s_thumbnailCache.RespondToMemoryPressure(isCritical: true);
                    s_smartLoader.ClearPrefetchCache();
                    // �?方案A: 同步状�?
                    _priorityLoader.SyncLoadedIndicesWithActualThumbnails();
                    
                    // 触发GC回收
                    Task.Run(() =>
                    {
                        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
                        GC.WaitForPendingFinalizers();
                        GC.Collect();
                    });
                    Debug.WriteLine("[ImagePreviewControl] �?危险压力响应：强制GC+清空缓存+低质量模�?状态同�?);
                    break;

                case MemoryPressureMonitor.PressureLevel.Normal:
                    // 恢复正常：恢复高质量模式
                    Debug.WriteLine("[ImagePreviewControl] �?压力恢复：高质量模式");
                    break;
            }
        }

        /// <summary>
        /// INotifyPropertyChanged接口实现
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        #endregion
    }

    /// <summary>
    /// 缩略图样式转换器
    /// </summary>
    public class ThumbnailStyleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 尝试从当前应用、窗口或控件资源中查�?
            var currentApp = Application.Current;
            if (currentApp == null) return null!;

            var mainWindow = currentApp.MainWindow;
            if (mainWindow == null) return null!;

            // 优先从窗口资源查找，如果没有则从应用资源查找
            Style? selectedStyle = null;
            Style? normalStyle = null;

            if (value is bool isSelected && isSelected)
            {
                // 查找选中样式
                if (mainWindow.TryFindResource("ThumbnailCardSelected") is Style style1)
                    selectedStyle = style1;
                else if (currentApp.TryFindResource("ThumbnailCardSelected") is Style style2)
                    selectedStyle = style2;

                // 如果没有找到选中样式，返回普通样�?
                if (selectedStyle == null)
                {
                    if (mainWindow.TryFindResource("ThumbnailCard") is Style normalStyle1)
                        return normalStyle1;
                    return currentApp.TryFindResource("ThumbnailCard") ?? null!;
                }
                return selectedStyle;
            }
            else
            {
                // 查找普通样�?
                if (mainWindow.TryFindResource("ThumbnailCard") is Style style1)
                    normalStyle = style1;
                else
                    normalStyle = currentApp.TryFindResource("ThumbnailCard") as Style;
                return normalStyle ?? null!;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 图像运行模式显示转换�?
    /// </summary>
    public class ImageRunModeDisplayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return "运行全部";

            // 尝试直接转换为ImageRunMode
            if (value is ImageRunMode runMode)
            {
                return runMode switch
                {
                    ImageRunMode.运行全部 => "运行全部",
                    ImageRunMode.运行选择 => "运行选中",
                    _ => "运行全部"
                };
            }


            return "运行全部";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str)
            {
                return str switch
                {
                    "运行全部" => ImageRunMode.运行全部,
                    "运行选中" => ImageRunMode.运行选择,
                    _ => ImageRunMode.运行全部
                };
            }
            return ImageRunMode.运行全部;
        }
    }

    /// <summary>
    /// Boolean到Brush转换器（用于自动切换按钮�?
    /// </summary>
    public class BooleanToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isChecked)
            {
                return isChecked ? new SolidColorBrush(Colors.Orange) : new SolidColorBrush(Color.FromRgb(192, 192, 192));
            }
            return new SolidColorBrush(Color.FromRgb(192, 192, 192));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 反向布尔转换�?
    /// </summary>
    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return false;
        }
    }

    /// <summary>
    /// Null到Opacity转换器（用于控制加载动画的显�?隐藏�?
    /// </summary>
    public class NullToOpacityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null ? 1.0 : 0.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 运行模式到可见性转换器（运行选择模式时显示）
    /// </summary>
    public class RunModeToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ImageRunMode mode && mode == ImageRunMode.运行选择)
            {
                return Visibility.Visible;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
