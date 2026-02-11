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

namespace SunEyeVision.UI.Controls
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
    /// 图像信息项
    /// </summary>
    public class ImageInfo : INotifyPropertyChanged
    {
        private int _displayIndex;
        private bool _isSelected;
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
        /// 是否已加载全分辨率图像
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
        /// 手动设置已加载的全分辨率图像（用于异步加载完成后的更新）
        /// </summary>
        public void SetFullImage(BitmapSource? image)
        {
            _fullImage = image;
            _isFullImageLoaded = (image != null);
            OnPropertyChanged(nameof(FullImage));
        }

        /// <summary>
        /// 释放全分辨率图像以节省内存
        /// </summary>
        public void ReleaseFullImage()
        {
            // 只有当图像实际已加载时才触发属性变更事件
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
    /// 性能日志记录器
    /// </summary>
    internal class PerformanceLogger
    {
        private readonly string _category;
        private static readonly object _lock = new object();
        private static int _logCounter = 0;

        public PerformanceLogger(string category)
        {
            _category = category;
        }

        /// <summary>
        /// 记录操作耗时
        /// </summary>
        public void LogOperation(string operation, TimeSpan elapsed, string details = "")
        {
            int id = Interlocked.Increment(ref _logCounter);
            var logMsg = $"[{id:D4}] [{_category}] {operation} - 耗时: {elapsed.TotalMilliseconds:F2}ms";
            if (!string.IsNullOrEmpty(details))
            {
                logMsg += $" | {details}";
            }
            Debug.WriteLine(logMsg);
        }

        /// <summary>
        /// 执行并计时
        /// </summary>
        public T ExecuteAndTime<T>(string operation, Func<T> func, string details = "")
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var result = func();
                sw.Stop();
                LogOperation(operation, sw.Elapsed, details);
                return result;
            }
            catch
            {
                sw.Stop();
                LogOperation($"{operation} (异常)", sw.Elapsed, details);
                throw;
            }
        }

        /// <summary>
        /// 异步执行并计时
        /// </summary>
        public async Task<T> ExecuteAndTimeAsync<T>(string operation, Func<Task<T>> func, string details = "")
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var result = await func();
                sw.Stop();
                LogOperation(operation, sw.Elapsed, details);
                return result;
            }
            catch
            {
                sw.Stop();
                LogOperation($"{operation} (异常)", sw.Elapsed, details);
                throw;
            }
        }
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
        /// 获取或添加缓存
        /// </summary>
        public BitmapSource? GetOrAdd(string filePath, Func<string, BitmapSource?> loader)
        {
            if (_cacheMap.TryGetValue(filePath, out var node))
            {
                // 命中缓存，移到最前
                _lruList.Remove(node);
                _lruList.AddFirst(node);
                return node.Value.Bitmap;
            }

            // 未命中，加载图像
            var bitmap = loader(filePath);
            if (bitmap == null) return null;

            // 添加到缓存
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
        /// 清除所有缓存
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
        // 图像缓存（LRU）
        private static readonly ImageCache s_fullImageCache = new ImageCache(maxCacheSize: 30);

        // ListBox控件引用（用于滚动监听）
        private ListBox? _thumbnailListBox;

        // 取消令牌源
        private CancellationTokenSource? _loadingCancellationTokenSource;

        // 预加载相关
        private Task? _preloadTask;
        private int _lastPreloadIndex = -1;

        // 滚动防抖定时器
        private DispatcherTimer? _updateRangeTimer;

        // 性能日志统计
        private int _scrollEventCount = 0;
        private DateTime _lastScrollTime = DateTime.MinValue;

        // 智能缩略图加载器
        private readonly SmartThumbnailLoader _smartLoader = new SmartThumbnailLoader();

        public static readonly DependencyProperty AutoSwitchEnabledProperty =
            DependencyProperty.Register("AutoSwitchEnabled", typeof(bool), typeof(ImagePreviewControl),
                new PropertyMetadata(false));

        public static readonly DependencyProperty CurrentImageIndexProperty =
            DependencyProperty.Register("CurrentImageIndex", typeof(int), typeof(ImagePreviewControl),
                new PropertyMetadata(-1, OnCurrentImageIndexChanged));

        public static readonly DependencyProperty ImageCollectionProperty =
            DependencyProperty.Register("ImageCollection", typeof(ObservableCollection<ImageInfo>), typeof(ImagePreviewControl),
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
        /// 当前显示的图像索引
        /// </summary>
        public int CurrentImageIndex
        {
            get => (int)GetValue(CurrentImageIndexProperty);
            set => SetValue(CurrentImageIndexProperty, value);
        }

        /// <summary>
        /// 图像集合
        /// </summary>
        public ObservableCollection<ImageInfo> ImageCollection
        {
            get => (ObservableCollection<ImageInfo>)GetValue(ImageCollectionProperty);
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
        /// 图像计数显示文本
        /// </summary>
        public string ImageCountDisplay
        {
            get
            {
                int count = ImageCollection?.Count ?? 0;
                int current = CurrentImageIndex >= 0 && CurrentImageIndex < count ? CurrentImageIndex + 1 : 0;
                return count > 0 ? $"图像源 ({current}/{count})" : "图像源";
            }
        }

        public ICommand AddImageCommand { get; }
        public ICommand AddFolderCommand { get; }
        public ICommand DeleteImageCommand { get; }
        public ICommand ClearAllCommand { get; }

        public ImagePreviewControl()
        {
            InitializeComponent();
            ImageCollection = new ObservableCollection<ImageInfo>();

            AddImageCommand = new RelayCommand(ExecuteAddImage);
            AddFolderCommand = new RelayCommand(ExecuteAddFolder);
            DeleteImageCommand = new RelayCommand(ExecuteDeleteImage, CanExecuteDeleteImage);
            ClearAllCommand = new RelayCommand(ExecuteClearAll, CanExecuteClearAll);

            ImageCollection.CollectionChanged += (s, e) =>
            {
                // UpdateDisplayIndices(); // 不再需要显示序号，移除此调用
                OnPropertyChanged(nameof(ImageCountDisplay));
            };

            Loaded += OnControlLoaded;
            Unloaded += OnUnloaded;

            // 初始化ListBox引用（需要在Loaded之后）
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
            // 延迟初始化，确保ListBox已完全加载
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                _thumbnailListBox = FindName("ThumbnailListBox") as ListBox;
                if (_thumbnailListBox != null)
                {
                    var scrollViewer = FindVisualChild<ScrollViewer>(_thumbnailListBox);
                    if (scrollViewer != null)
                    {
                        scrollViewer.ScrollChanged += OnScrollChanged;
                        // 初始加载可见区域
                        UpdateLoadRange();
                    }
                }
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        /// <summary>
        /// 查找视觉子元素
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
        /// 控件卸载时清理资源
        /// </summary>
        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            // 取消正在进行的加载操作
            _loadingCancellationTokenSource?.Cancel();
            _loadingCancellationTokenSource?.Dispose();
            _loadingCancellationTokenSource = null;

            // 取消预加载任务
            if (_preloadTask != null)
            {
                // 等待预加载任务完成或取消（最多等待500ms）
                try
                {
                    if (!_preloadTask.IsCompleted)
                    {
                        Task.Run(async () => await Task.WhenAny(_preloadTask, Task.Delay(500))).Wait(500);
                    }
                }
                catch { }
                _preloadTask = null;
            }

            // 清理智能缩略图加载器
            _smartLoader.CancelAndDispose();

            // 停止防抖定时器
            if (_updateRangeTimer != null)
            {
                _updateRangeTimer.Stop();
                _updateRangeTimer = null;
            }

            // 停止防抖定时器
            if (_updateRangeTimer != null)
            {
                _updateRangeTimer.Stop();
                _updateRangeTimer = null;
            }

            // 清理所有图像资源
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
        /// 优化的全分辨率图像加载（静态方法，可从ImageInfo调用）
        /// </summary>
        public static BitmapImage? LoadImageOptimized(string filePath)
        {
            return s_fullImageCache.GetOrAdd(filePath, fp =>
            {
                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.CreateOptions = BitmapCreateOptions.DelayCreation;
                    bitmap.UriSource = new Uri(fp);
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

        /// <summary>
        /// 优化的缩略图加载（只设置宽度，保持宽高比）
        /// </summary>
        private static BitmapImage? LoadThumbnailOptimized(string filePath, int size = 80)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    return null;
                }

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.CreateOptions = BitmapCreateOptions.DelayCreation;
                bitmap.UriSource = new Uri(filePath);
                bitmap.DecodePixelWidth = size;
                bitmap.Rotation = Rotation.Rotate0;
                bitmap.EndInit();
                bitmap.Freeze();

                return bitmap;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 异步加载缩略图
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

            // 检查特殊值，阻止预加载（用于优化图像加载过程）
            if (currentIndex == -999 || _lastPreloadIndex == -999)
            {
                return;
            }

            // 如果索引未变化，不重复预加载
            if (currentIndex == _lastPreloadIndex)
            {
                return;
            }

            _lastPreloadIndex = currentIndex;

            // 取消之前的预加载任务
            if (_preloadTask != null && !_preloadTask.IsCompleted)
            {
                return; // 等待前一个任务完成
            }

            // 动态计算预加载范围
            var (immediateDisplayCount, _, _) = CalculateDynamicLoadCounts();
            int preloadRange = Math.Max(2, immediateDisplayCount / 10); // 预加载范围为显示数量的10%，最少2张

            // 在UI线程上收集需要预加载的图像信息
            var imagesToPreload = new List<(int index, string filePath)>();
            var preloadOffsets = Enumerable.Range(-preloadRange, preloadRange * 2 + 1).Where(x => x != 0).ToArray();
            foreach (var offset in preloadOffsets)
            {
                var index = currentIndex + offset;
                if (index >= 0 && index < ImageCollection.Count)
                {
                    var imageInfo = ImageCollection[index];
                    if (!imageInfo.IsFullImageLoaded)
                    {
                        imagesToPreload.Add((index, imageInfo.FilePath));
                    }
                }
            }

            // 启动新的预加载任务（在后台线程中加载图像）
            _preloadTask = Task.Run(() =>
            {
                foreach (var (index, filePath) in imagesToPreload)
                {
                    try
                    {
                        var fullImage = LoadImageOptimized(filePath);
                        if (fullImage != null)
                        {
                            Application.Current?.Dispatcher.Invoke(() =>
                            {
                                if (index < ImageCollection.Count)
                                {
                                    ImageCollection[index].SetFullImage(fullImage);
                                }
                            }, System.Windows.Threading.DispatcherPriority.Background);
                        }
                    }
                    catch { }
                }
            });
        }

        /// <summary>
        /// 滚动变化事件处理（带防抖机制）
        /// </summary>
        private void OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            var now = DateTime.Now;
            var timeSinceLast = now - _lastScrollTime;
            _lastScrollTime = now;
            _scrollEventCount++;

            // 防抖：滚动停止200ms后才触发加载，避免频繁更新队列
            if (_updateRangeTimer == null)
            {
                _updateRangeTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(200)
                };
                _updateRangeTimer.Tick += (s, args) =>
                {
                    UpdateLoadRange();
                    _updateRangeTimer?.Stop();
                };
            }
            _updateRangeTimer.Stop();
            _updateRangeTimer.Start();
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

            // 动态计算保留范围
            var (immediateDisplayCount, _, _) = CalculateDynamicLoadCounts();
            int keepRange = Math.Max(2, immediateDisplayCount / 10); // 保留范围为显示数量的10%，最少2张

            // 只保留当前和相邻图像的全分辨率（距离<=keepRange）
            for (int i = 0; i < ImageCollection.Count; i++)
            {
                if (Math.Abs(i - currentIndex) > keepRange)
                {
                    ImageCollection[i].ReleaseFullImage();
                }
            }
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
                // 用户取消了操作
            }
            catch (Exception ex)
            {
                MessageBox.Show($"添加图像失败: {ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 计算基于可视区域的动态加载数量
        /// </summary>
        private (int immediateDisplayCount, int immediateThumbnailCount, int batchSize) CalculateDynamicLoadCounts()
        {
            if (_thumbnailListBox == null)
            {
                // 如果ListBox未初始化，使用默认值
                return (20, 5, 10);
            }

            var scrollViewer = FindVisualChild<ScrollViewer>(_thumbnailListBox);
            if (scrollViewer == null || scrollViewer.ViewportWidth <= 0)
            {
                // 如果ScrollViewer未就绪，使用默认值
                return (20, 5, 10);
            }

            var viewportWidth = scrollViewer.ViewportWidth;
            var itemWidth = 92.0; // 缩略图宽度90 + 边距2

            // 计算视口能容纳的图片数量（+4作为缓冲区）
            int viewportCapacity = (int)(viewportWidth / itemWidth) + 4;
            int immediateDisplayCount = Math.Max(10, viewportCapacity); // 最少10张

            // 缩略图数量为显示数量的1/4（最少3张）
            int immediateThumbnailCount = Math.Max(3, immediateDisplayCount / 4);

            // 批次大小为显示数量的1/2（最少5张）
            int batchSize = Math.Max(5, immediateDisplayCount / 2);

            return (immediateDisplayCount, immediateThumbnailCount, batchSize);
        }

        /// <summary>
        /// 优化的图像加载方法（即时显示优化：分批次流式加载）- 四大优化策略版本
        /// </summary>
        private async Task LoadImagesOptimizedAsync(
            string[] fileNames,
            CancellationToken cancellationToken)
        {
            var totalSw = Stopwatch.StartNew();
            var logger = new PerformanceLogger("LoadImages");

            // ===== 策略1：预计算图片总数 =====
            logger.LogOperation("步骤1-预计算总数", totalSw.Elapsed, $"准备加载 {fileNames.Length} 张图片");

            // 动态计算基于可视区域的加载数量
            var (immediateDisplayCount, immediateThumbnailCount, _) = CalculateDynamicLoadCounts();

            // 预创建所有ImageInfo对象（不添加到集合）
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
            logger.LogOperation("步骤1-预创建ImageInfo", totalSw.Elapsed, $"创建了 {fileNames.Length} 个ImageInfo对象");

            // 步骤2期间禁用FullImage预加载，避免影响缩略图显示性能
            // 标记为正在加载缩略图，PreloadAdjacentImages会检查此标志
            var originalLastPreloadIndex = _lastPreloadIndex;
            _lastPreloadIndex = -999; // 设置一个特殊值，阻止所有PreloadAdjacentImages调用

            // ===== 策略2：立即更新ListBox宽度及滚动条 =====
            logger.LogOperation("步骤2-预计算布局", totalSw.Elapsed);
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                // 估算总宽度
                const double itemWidth = 92.0; // 缩略图宽度90 + 边距2
                var estimatedTotalWidth = fileNames.Length * itemWidth;

                logger.LogOperation("步骤2-布局预留", TimeSpan.Zero, $"预估宽度:{estimatedTotalWidth:F2}px");

                // 先创建所有ImageInfo占位（无缩略图）到集合中，确保滚动条正确显示
                int startIndex = ImageCollection.Count;
                foreach (var imageInfo in allImages)
                {
                    ImageCollection.Add(imageInfo);
                }

                if (ImageCollection.Count > 0)
                {
                    CurrentImageIndex = startIndex;
                }

                // 强制更新布局，确保滚动条显示
                UpdateLayout();
                if (_thumbnailListBox != null)
                {
                    _thumbnailListBox.UpdateLayout();
                    var scrollViewer = FindVisualChild<ScrollViewer>(_thumbnailListBox);
                    if (scrollViewer != null)
                    {
                        scrollViewer.UpdateLayout();
                    }
                }

                logger.LogOperation("步骤2-添加占位并更新布局", TimeSpan.Zero, $"当前集合数:{ImageCollection.Count}");
            }, System.Windows.Threading.DispatcherPriority.Normal);

            // ===== 策略3：零等待快速加载可见区域的预览图 =====
            logger.LogOperation("步骤3-加载可见区域", totalSw.Elapsed);

            // 计算当前可见区域
            int firstVisible = 0;
            int lastVisible = Math.Min(immediateThumbnailCount - 1, fileNames.Length - 1);

            // 并行加载前3张缩略图，让第一张图片立即显示
            var immediateThumbnailTasks = new List<Task<(int index, BitmapImage thumbnail)>>();
            int immediateLoadCount = Math.Min(3, lastVisible - firstVisible + 1);
            var loadImmediateSw = Stopwatch.StartNew();

            for (int i = firstVisible; i < firstVisible + immediateLoadCount && i < fileNames.Length; i++)
            {
                int index = i;
                var task = Task.Run(() =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var thumbnail = LoadThumbnailOptimized(allImages[index].FilePath);
                    return (index, thumbnail);
                }, cancellationToken);
                immediateThumbnailTasks.Add(task);
            }

            // 等待第一张缩略图完成并立即显示
            var firstThumbnailTask = await Task.WhenAny(immediateThumbnailTasks);
            var (firstIndex, firstThumbnail) = await firstThumbnailTask;

            if (firstThumbnail != null && firstIndex < ImageCollection.Count)
            {
                var updateFirstSw = Stopwatch.StartNew();
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    ImageCollection[firstIndex].Thumbnail = firstThumbnail;
                }, System.Windows.Threading.DispatcherPriority.Render);
                logger.LogOperation("步骤3-首张缩略图显示", updateFirstSw.Elapsed, $"索引:{firstIndex}");
            }

            // 等待所有立即加载的缩略图完成
            var completedThumbnails = new List<(int index, BitmapImage thumbnail)>();
            await Task.WhenAll(immediateThumbnailTasks);
            foreach (var task in immediateThumbnailTasks)
            {
                var (index, thumbnail) = await task;
                if (thumbnail != null)
                {
                    completedThumbnails.Add((index, thumbnail));
                }
            }

            logger.LogOperation("步骤3-前3张缩略图加载", loadImmediateSw.Elapsed, $"加载了 {completedThumbnails.Count} 张");

            // 批量更新UI（减少PropertyChanged触发次数）
            var updateVisibleSw = Stopwatch.StartNew();
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                foreach (var (index, thumbnail) in completedThumbnails)
                {
                    if (index < ImageCollection.Count && index != firstIndex)
                    {
                        ImageCollection[index].Thumbnail = thumbnail;
                    }
                }
            }, System.Windows.Threading.DispatcherPriority.Normal);

            logger.LogOperation("步骤3-批量更新可见区域", updateVisibleSw.Elapsed);

            // 清除已加载缩略图的记录
            _smartLoader.ClearLoadedIndices();

            // 立即触发缩略图加载（优先当前可见区域）
            UpdateLoadRange();

            logger.LogOperation("步骤3-触发智能加载", TimeSpan.Zero);

            // ===== 策略4：把剩余未加载移交后台 =====
            logger.LogOperation("步骤4-后台批量加载", totalSw.Elapsed);

            // 剩余图片已经在SmartLoader的队列中，后台任务会自动处理
            // 不需要额外的代码，SmartLoader会接管

            // 预加载前几张图像的全分辨率（后台任务）- 使用动态计算的数量
            int preFullImageLoadCount = Math.Max(2, immediateDisplayCount / 10); // 预加载数量为显示数量的10%，最少2张

            _ = Task.Run(() =>
            {
                for (int i = 0; i < Math.Min(preFullImageLoadCount, fileNames.Length); i++)
                {
                    try
                    {
                        if (cancellationToken.IsCancellationRequested) break;

                        var fullImage = LoadImageOptimized(fileNames[i]);
                        if (fullImage != null && !cancellationToken.IsCancellationRequested)
                        {
                            Application.Current?.Dispatcher.Invoke(() =>
                            {
                                if (i < ImageCollection.Count)
                                {
                                    ImageCollection[i].SetFullImage(fullImage);
                                }
                            }, System.Windows.Threading.DispatcherPriority.Background);
                        }
                    }
                    catch { }
                }
            }, cancellationToken);

            // 步骤3和4完成后，恢复预加载功能
            _lastPreloadIndex = originalLastPreloadIndex; // 恢复原值，允许PreloadAdjacentImages再次执行

            totalSw.Stop();
            logger.LogOperation("加载流程完成", totalSw.Elapsed, $"总图片数:{fileNames.Length} 立即加载:{immediateThumbnailCount}");
        }



        /// <summary>
        /// 添加文件夹
        /// </summary>
        private async void ExecuteAddFolder()
        {
            try
            {
                var openFileDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "图像文件|*.jpg;*.jpeg;*.png;*.bmp;*.tiff|所有文件|*.*",
                    Title = "选择文件夹中的任意一个图像文件",
                    Multiselect = false
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    var filePath = openFileDialog.FileName;
                    var folderPath = Path.GetDirectoryName(filePath);

                    if (string.IsNullOrEmpty(folderPath))
                    {
                        MessageBox.Show("无法获取文件夹路径", "错误",
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
                        MessageBox.Show("所选文件夹中没有找到图像文件", "提示",
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
                // 用户取消了操作
            }
            catch (Exception ex)
            {
                MessageBox.Show($"添加文件夹失败: {ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 删除图像
        /// </summary>
        private void ExecuteDeleteImage()
        {
            if (CurrentImageIndex < 0 || CurrentImageIndex >= ImageCollection.Count)
                return;

            var result = MessageBox.Show(
                $"确定要删除当前图像吗?",
                "确认删除",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                ImageCollection.RemoveAt(CurrentImageIndex);

                // 调整当前索引
                if (ImageCollection.Count == 0)
                {
                    CurrentImageIndex = -1;
                }
                else if (CurrentImageIndex >= ImageCollection.Count)
                {
                    CurrentImageIndex = ImageCollection.Count - 1;
                }
            }
        }

        private bool CanExecuteDeleteImage()
        {
            return ImageCollection != null && CurrentImageIndex >= 0 && CurrentImageIndex < ImageCollection.Count;
        }

        /// <summary>
        /// 清除所有图像
        /// </summary>
        private void ExecuteClearAll()
        {
            if (ImageCollection.Count == 0)
                return;

            var result = MessageBox.Show(
                $"确定要清除所有 {ImageCollection.Count} 张图像吗?",
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

            // 智能预加载相邻图像
            control.PreloadAdjacentImages(control.CurrentImageIndex);

            // 释放远离当前索引的图像
            control.ReleaseDistantImages(control.CurrentImageIndex);
        }

        /// <summary>
        /// 图像集合更改回调
        /// </summary>
        private static void OnImageCollectionChanged(DependencyObject d, DependencyPropertyChangedEventArgs _)
        {
            var control = (ImagePreviewControl)d;
            // control.UpdateDisplayIndices(); // 不再需要显示序号，移除此调用
            control.UpdateImageSelection();
            control.OnPropertyChanged(nameof(ImageCountDisplay));
        }

        /// <summary>
        /// 图像运行模式更改回调
        /// </summary>
        private static void OnImageRunModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs _)
        {
            var control = (ImagePreviewControl)d;
            control.OnPropertyChanged(nameof(ImageRunMode));
        }



        /// <summary>
        /// 更新图像显示索引（已废弃，不再使用）
        /// </summary>
        [System.Obsolete("此方法已废弃，不再需要显示缩略图序号")]
        private void UpdateDisplayIndices()
        {
            if (ImageCollection == null) return;

            for (int i = 0; i < ImageCollection.Count; i++)
            {
                ImageCollection[i].DisplayIndex = i + 1;
            }
        }

        /// <summary>
        /// 更新图像选中状态
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
        /// 缩略图点击事件处理
        /// </summary>
        private void OnThumbnailClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is ImageInfo imageInfo)
            {
                int index = ImageCollection.IndexOf(imageInfo);
                if (index >= 0)
                {
                    CurrentImageIndex = index;

                    // 点击后立即加载该图像的缩略图（如果尚未加载）
                    if (imageInfo.Thumbnail == null)
                    {
                        _smartLoader.UpdateLoadRange(index, index, ImageCollection.Count, ImageCollection);
                    }
                }
            }
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
        /// 使用PreviewMouseDown确保在点击任意项时都能立即响应
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

            // 获取选中项
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
        /// 查找父元素
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
        /// 加载图像（已废弃，请使用LoadImageOptimized）
        /// </summary>
        [System.Obsolete("请使用LoadImageOptimized代替")]
        private BitmapImage? LoadImage(string filePath)
        {
            return LoadImageOptimized(filePath);
        }

        /// <summary>
        /// 加载缩略图（已废弃，请使用LoadThumbnailOptimized）
        /// </summary>
        [System.Obsolete("请使用LoadThumbnailOptimized代替")]
        private BitmapImage? LoadThumbnail(string filePath, int maxWidth = 120, int maxHeight = 120)
        {
            return LoadThumbnailOptimized(filePath, maxWidth);
        }

        /// <summary>
        /// 智能缩略图加载器 - 支持渐进式顺序加载（优化版：不取消已加载任务）- 带性能日志
        /// </summary>
        private class SmartThumbnailLoader
        {
            private readonly SortedSet<int> _loadQueue = new SortedSet<int>();
            private readonly HashSet<int> _loadedIndices = new HashSet<int>();
            private Task? _loadTask;
            private CancellationTokenSource? _cancellationTokenSource;
            private ObservableCollection<ImageInfo>? _imageCollection;
            private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(Math.Max(4, Environment.ProcessorCount / 2)); // 限制并发数为4-8个，提升加载速度
            private readonly object _lockObj = new object(); // 用于线程安全
            private readonly PerformanceLogger _logger = new PerformanceLogger("SmartLoader");

            private int _totalAdded = 0; // 总共添加到队列的数量
            private int _totalLoaded = 0; // 总共加载完成的数量

            // 任务移交队列（后台加载）
            private readonly ConcurrentQueue<int> _backgroundLoadQueue = new ConcurrentQueue<int>();
            private Task? _backgroundLoadTask;
            private CancellationTokenSource? _backgroundCancellationTokenSource;

            // 滚动状态追踪
            private int _lastFirstVisible = -1;
            private int _lastLastVisible = -1;

            /// <summary>
            /// 更新加载范围（滚动时调用）- 优化：不取消已开始的加载任务
            /// </summary>
            public void UpdateLoadRange(int firstIndex, int lastIndex, int imageCount, ObservableCollection<ImageInfo> imageCollection)
            {
                var sw = Stopwatch.StartNew();
                _imageCollection = imageCollection;

                // 优化：不再取消之前的任务，只添加新的索引到队列
                // 这样已经加载的缩略图不会被浪费

                int movedOutCount = 0;
                // 检测哪些索引移出了视野，移交到后台加载
                if (_lastFirstVisible >= 0 && _lastLastVisible >= 0)
                {
                    var movedOutIndices = new List<int>();
                    // 检查之前可见的索引是否现在不可见
                    for (int i = _lastFirstVisible; i <= _lastLastVisible; i++)
                    {
                        if (i < firstIndex || i > lastIndex)
                        {
                            movedOutIndices.Add(i);
                        }
                    }

                    // 移交移出的任务到后台
                    foreach (var index in movedOutIndices)
                    {
                        if (index >= 0 && index < imageCount && !_loadedIndices.Contains(index) && !_loadQueue.Contains(index))
                        {
                            TransferToBackground(index);
                            movedOutCount++;
                        }
                    }
                }

                // 更新滚动状态追踪
                _lastFirstVisible = firstIndex;
                _lastLastVisible = lastIndex;

                int addedCount = 0;
                lock (_lockObj)
                {
                    // 将新范围内的索引加入队列（自动去重和排序）
                    for (int i = firstIndex; i <= lastIndex; i++)
                    {
                        if (i >= 0 && i < imageCount && !_loadedIndices.Contains(i) && !_loadQueue.Contains(i))
                        {
                            _loadQueue.Add(i);
                            addedCount++;
                            _totalAdded++;
                        }
                    }
                }

                _logger.LogOperation("UpdateLoadRange", sw.Elapsed,
                    $"范围:[{firstIndex}-{lastIndex}] 添加:{addedCount} 队列:{_loadQueue.Count} 已加载:{_loadedIndices.Count} 总添加:{_totalAdded} 移交后台:{movedOutCount}");

                // 启动新的加载任务（如果还没启动或已完成）
                if (_loadTask == null || _loadTask.IsCompleted)
                {
                    // 创建新的取消令牌
                    if (_cancellationTokenSource != null)
                    {
                        _cancellationTokenSource.Dispose();
                    }
                    _cancellationTokenSource = new CancellationTokenSource();
                    _loadTask = ProcessLoadQueue(_cancellationTokenSource.Token);
                }
            }

            /// <summary>
            /// 清除已加载索引
            /// </summary>
            public void ClearLoadedIndices()
            {
                var sw = Stopwatch.StartNew();
                lock (_lockObj)
                {
                    _loadedIndices.Clear();
                    _loadQueue.Clear();
                    _totalAdded = 0;
                    _totalLoaded = 0;
                }
                _logger.LogOperation("ClearLoadedIndices", sw.Elapsed);
            }

            /// <summary>
            /// 标记为已加载
            /// </summary>
            public void MarkAsLoaded(int index)
            {
                _loadedIndices.Add(index);
                _loadQueue.Remove(index);
            }

            /// <summary>
            /// 移交任务到后台（当缩略图滚动出视野时）
            /// </summary>
            public void TransferToBackground(int index)
            {
                if (!_loadedIndices.Contains(index) && !_loadQueue.Contains(index))
                {
                    _backgroundLoadQueue.Enqueue(index);
                    StartBackgroundLoading();
                }
            }

            /// <summary>
            /// 启动后台加载任务
            /// </summary>
            private void StartBackgroundLoading()
            {
                if (_backgroundLoadTask == null || _backgroundLoadTask.IsCompleted)
                {
                    _backgroundCancellationTokenSource = new CancellationTokenSource();
                    _backgroundLoadTask = ProcessBackgroundQueue(_backgroundCancellationTokenSource.Token);
                }
            }

            /// <summary>
            /// 处理后台任务队列
            /// </summary>
            private async Task ProcessBackgroundQueue(CancellationToken cancellationToken)
            {
                while (!cancellationToken.IsCancellationRequested && _backgroundLoadQueue.TryDequeue(out int index))
                {
                    try
                    {
                        await LoadSingleThumbnail(index, cancellationToken, isBackground: true);
                    }
                    catch
                    {
                        // 忽略后台加载错误，避免中断整个流程
                    }
                }
            }

            /// <summary>
            /// 取消并释放资源
            /// </summary>
            public void CancelAndDispose()
            {
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
                _loadQueue.Clear();
                _loadedIndices.Clear();

                // 清理后台加载任务
                _backgroundCancellationTokenSource?.Cancel();
                _backgroundCancellationTokenSource?.Dispose();
                _backgroundCancellationTokenSource = null;
                _backgroundLoadQueue.Clear();
            }

            /// <summary>
            /// 处理加载队列（渐进式顺序加载）- 优化：并发数匹配SemaphoreSlim - 带性能日志
            /// </summary>
            private async Task ProcessLoadQueue(CancellationToken cancellationToken)
            {
                var sw = Stopwatch.StartNew();
                var concurrencyLimit = Math.Max(4, Environment.ProcessorCount / 2);
                var activeTasks = new List<Task>();
                int processedCount = 0;
                var processSw = Stopwatch.StartNew();

                _logger.LogOperation("ProcessLoadQueue开始", TimeSpan.Zero,
                    $"并发限制:{concurrencyLimit} 队列:{_loadQueue.Count}");

                while ((!cancellationToken.IsCancellationRequested) && (activeTasks.Count > 0 || _loadQueue.Count > 0))
                {
                    // 启动新任务直到达到并发限制
                    int newTasksStarted = 0;
                    lock (_lockObj)
                    {
                        while (activeTasks.Count < concurrencyLimit && _loadQueue.Count > 0)
                        {
                            var index = _loadQueue.Min; // 取出最小的索引
                            _loadQueue.Remove(index);

                            var task = Task.Run(async () =>
                            {
                                await _semaphore.WaitAsync(cancellationToken);
                                try
                                {
                                    if (!cancellationToken.IsCancellationRequested)
                                    {
                                        await LoadSingleThumbnail(index, cancellationToken);
                                    }
                                }
                                finally
                                {
                                    _semaphore.Release();
                                }
                            }, cancellationToken);
                            activeTasks.Add(task);
                            newTasksStarted++;
                        }
                    }

                    if (newTasksStarted > 0)
                    {
                        _logger.LogOperation("启动新任务", processSw.Elapsed,
                            $"数量:{newTasksStarted} 活动任务:{activeTasks.Count} 队列剩余:{_loadQueue.Count}");
                    }

                    // 等待至少一个任务完成
                    if (activeTasks.Count > 0)
                    {
                        var waitSw = Stopwatch.StartNew();
                        var completedTask = await Task.WhenAny(activeTasks);
                        waitSw.Stop();
                        activeTasks.Remove(completedTask);
                        processedCount++;

                        // 每10个任务输出一次进度
                        if (processedCount % 10 == 0)
                        {
                            _logger.LogOperation("处理进度", processSw.Elapsed,
                                $"已完成:{processedCount}/{_totalAdded} 活动任务:{activeTasks.Count} 队列剩余:{_loadQueue.Count}");
                        }
                    }

                    processSw.Restart();
                }

                sw.Stop();
                _logger.LogOperation("ProcessLoadQueue完成", sw.Elapsed,
                    $"处理总数:{processedCount} 总耗时:{sw.Elapsed.TotalMilliseconds:F2}ms 平均:{sw.Elapsed.TotalMilliseconds / Math.Max(1, processedCount):F2}ms/张");
            }

            /// <summary>
            /// 加载单个缩略图（优化：避免重复加载）
            /// </summary>
            private async Task LoadSingleThumbnail(int index, CancellationToken cancellationToken, bool isBackground = false)
            {
                var sw = Stopwatch.StartNew();
                string fileName = "";

                if (_imageCollection == null || index < 0 || index >= _imageCollection.Count)
                {
                    _logger.LogOperation($"索引{index}超出范围", sw.Elapsed);
                    return;
                }

                ImageInfo? imageInfo = null;
                lock (_lockObj)
                {
                    // 再次检查，避免并发加载同一图片
                    if (_loadedIndices.Contains(index))
                    {
                        _logger.LogOperation($"索引{index}已加载", sw.Elapsed, "跳过");
                        return;
                    }

                    imageInfo = _imageCollection[index];
                    if (imageInfo.Thumbnail != null)
                    {
                        _loadedIndices.Add(index);
                        _logger.LogOperation($"索引{index}已有缩略图", sw.Elapsed, "跳过");
                        return; // 已加载
                    }
                    fileName = Path.GetFileName(imageInfo.FilePath);
                }

                var checkSw = sw.Elapsed;

                try
                {
                    // 阶段1: 实际加载（使用Task.Run包装同步操作）
                    sw.Restart();
                    var thumbnail = await Task.Run(() => LoadThumbnailOptimized(imageInfo.FilePath));
                    var loadSw = sw.Elapsed;

                    if (thumbnail != null && !cancellationToken.IsCancellationRequested)
                    {
                        // 阶段2: UI更新
                        sw.Restart();
                        await Application.Current?.Dispatcher.InvokeAsync(() =>
                        {
                            if (index < _imageCollection.Count && _imageCollection[index] == imageInfo)
                            {
                                imageInfo.Thumbnail = thumbnail;
                                lock (_lockObj)
                                {
                                    _loadedIndices.Add(index);
                                    _totalLoaded++;
                                }
                            }
                        }, System.Windows.Threading.DispatcherPriority.Background)!;
                        var updateSw = sw.Elapsed;

                        _logger.LogOperation($"加载缩略图[{index}]", checkSw + loadSw + updateSw,
                            $"文件:{fileName} 检查:{checkSw.TotalMilliseconds:F2}ms 加载:{loadSw.TotalMilliseconds:F2}ms UI:{updateSw.TotalMilliseconds:F2}ms");
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogOperation($"索引{index}被取消", sw.Elapsed, fileName);
                }
                catch (Exception ex)
                {
                    _logger.LogOperation($"索引{index}加载失败", sw.Elapsed, $"{fileName} 错误:{ex.Message}");
                }

            }
        }

        /// <summary>
        /// 更新加载范围（滚动时调用）
        /// </summary>
        private void UpdateLoadRange()
        {
            var sw = Stopwatch.StartNew();

            if (_thumbnailListBox == null || ImageCollection == null || ImageCollection.Count == 0)
            {
                return;
            }

            var scrollViewer = FindVisualChild<ScrollViewer>(_thumbnailListBox);
            if (scrollViewer == null)
            {
                return;
            }

            // 强制更新布局，确保ScrollViewer的尺寸正确
            _thumbnailListBox.UpdateLayout();
            scrollViewer.UpdateLayout();

            // 计算可见区域
            var viewportWidth = scrollViewer.ViewportWidth;
            var horizontalOffset = scrollViewer.HorizontalOffset;
            var itemWidth = 92; // 缩略图宽度90 + 边距2

            var firstVisible = Math.Max(0, (int)(horizontalOffset / itemWidth));
            var lastVisible = Math.Min(ImageCollection.Count - 1,
                (int)((horizontalOffset + viewportWidth) / itemWidth) + 2);

            // 动态计算缓冲区（基于视口容量）
            var viewportCapacity = (int)(viewportWidth / itemWidth);
            var bufferZone = Math.Max(2, viewportCapacity / 10); // 缓冲区为视口容量的10%，最少2张

            // 扩展预加载范围（前后各多加载bufferZone张）
            firstVisible = Math.Max(0, firstVisible - bufferZone);
            lastVisible = Math.Min(ImageCollection.Count - 1, lastVisible + bufferZone);

            // 委托给智能加载器
            _smartLoader.UpdateLoadRange(firstVisible, lastVisible, ImageCollection.Count, ImageCollection);
        }

        #endregion

        #region 事件

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
            // 尝试从当前应用、窗口或控件资源中查找
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

                // 如果没有找到选中样式，返回普通样式
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
                // 查找普通样式
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
    /// 图像运行模式显示转换器
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
    /// Boolean到Brush转换器（用于自动切换按钮）
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
    /// 反向布尔转换器
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
    /// Null到Opacity转换器（用于控制加载动画的显示/隐藏）
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
}
