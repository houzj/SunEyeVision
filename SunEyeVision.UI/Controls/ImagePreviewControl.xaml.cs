using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using SunEyeVision.UI.ViewModels;

namespace SunEyeVision.UI.Controls
{
    /// <summary>
    /// 图像信息项
    /// </summary>
    public class ImageInfo : INotifyPropertyChanged
    {
        private int _displayIndex;
        private bool _isSelected;

        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public BitmapSource? Thumbnail { get; set; }
        public BitmapSource? FullImage { get; set; }
        public DateTime AddedTime { get; set; } = DateTime.Now;

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

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// ImagePreviewControl.xaml 的交互逻辑
    /// </summary>
    public partial class ImagePreviewControl : System.Windows.Controls.UserControl
    {
        public static readonly DependencyProperty AutoSwitchEnabledProperty =
            DependencyProperty.Register("AutoSwitchEnabled", typeof(bool), typeof(ImagePreviewControl),
                new PropertyMetadata(false));

        public static readonly DependencyProperty CurrentImageIndexProperty =
            DependencyProperty.Register("CurrentImageIndex", typeof(int), typeof(ImagePreviewControl),
                new PropertyMetadata(-1));

        public static readonly DependencyProperty ImageCollectionProperty =
            DependencyProperty.Register("ImageCollection", typeof(ObservableCollection<ImageInfo>), typeof(ImagePreviewControl),
                new PropertyMetadata(null, OnImageCollectionChanged));

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
            set
            {
                if ((int)GetValue(CurrentImageIndexProperty) != value)
                {
                    SetValue(CurrentImageIndexProperty, value);
                    UpdateImageSelection();
                }
            }
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
        public ICommand RunAllCommand { get; }

        public ImagePreviewControl()
        {
            InitializeComponent();
            ImageCollection = new ObservableCollection<ImageInfo>();

            AddImageCommand = new RelayCommand(ExecuteAddImage);
            AddFolderCommand = new RelayCommand(ExecuteAddFolder);
            DeleteImageCommand = new RelayCommand(ExecuteDeleteImage, CanExecuteDeleteImage);
            ClearAllCommand = new RelayCommand(ExecuteClearAll, CanExecuteClearAll);
            RunAllCommand = new RelayCommand(ExecuteRunAll, CanExecuteRunAll);

            ImageCollection.CollectionChanged += (s, e) =>
            {
                UpdateDisplayIndices();
                OnPropertyChanged(nameof(ImageCountDisplay));
            };
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
                    // 显示加载提示
                    var loadingWindow = new LoadingWindow($"正在加载 {fileNames.Length} 张图像...");
                    loadingWindow.Show();

                    try
                    {
                        await Task.Run(() =>
                        {
                            var imageInfos = new List<ImageInfo>();

                            // 在后台线程加载所有图像
                            foreach (var filePath in fileNames)
                            {
                                var imageInfo = new ImageInfo
                                {
                                    Name = System.IO.Path.GetFileNameWithoutExtension(filePath),
                                    FilePath = filePath,
                                    Thumbnail = LoadThumbnail(filePath),
                                    FullImage = LoadImage(filePath)
                                };
                                imageInfos.Add(imageInfo);
                            }

                            // 在UI线程上一次性添加所有图像
                            var dispatcher = Application.Current?.Dispatcher;
                            if (dispatcher != null && !dispatcher.HasShutdownStarted && !dispatcher.HasShutdownFinished)
                            {
                                dispatcher.Invoke(() =>
                                {
                                    foreach (var imageInfo in imageInfos)
                                    {
                                        ImageCollection.Add(imageInfo);
                                    }
                                }, System.Windows.Threading.DispatcherPriority.Normal);
                            }
                        });

                        // 如果是第一张图像，设为当前图像
                        if (CurrentImageIndex == -1 && ImageCollection.Count > 0)
                        {
                            CurrentImageIndex = 0;
                        }
                    }
                    finally
                    {
                        // 安全地关闭加载窗口
                        loadingWindow.Dispatcher.Invoke(() => loadingWindow.Close());
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"添加图像失败: {ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
                    var folderPath = System.IO.Path.GetDirectoryName(filePath);

                    if (string.IsNullOrEmpty(folderPath))
                    {
                        MessageBox.Show("无法获取文件夹路径", "错误",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".bmp", ".tiff", ".tif" };

                    var imageFiles = System.IO.Directory.GetFiles(folderPath, "*.*", System.IO.SearchOption.TopDirectoryOnly)
                        .Where(f => imageExtensions.Contains(System.IO.Path.GetExtension(f).ToLower()))
                        .OrderBy(f => f)
                        .ToArray();

                    if (imageFiles.Length == 0)
                    {
                        MessageBox.Show("所选文件夹中没有找到图像文件", "提示",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    // 显示加载提示
                    var loadingWindow = new LoadingWindow($"正在加载 {imageFiles.Length} 张图像...");
                    loadingWindow.Show();

                    try
                    {
                        await Task.Run(() =>
                        {
                            var imageInfos = new List<ImageInfo>();

                            // 在后台线程加载所有图像
                            foreach (var file in imageFiles)
                            {
                                var imageInfo = new ImageInfo
                                {
                                    Name = System.IO.Path.GetFileNameWithoutExtension(file),
                                    FilePath = file,
                                    Thumbnail = LoadThumbnail(file),
                                    FullImage = LoadImage(file)
                                };
                                imageInfos.Add(imageInfo);
                            }

                            // 在UI线程上一次性添加所有图像
                            var dispatcher = Application.Current?.Dispatcher;
                            if (dispatcher != null && !dispatcher.HasShutdownStarted && !dispatcher.HasShutdownFinished)
                            {
                                dispatcher.Invoke(() =>
                                {
                                    foreach (var imageInfo in imageInfos)
                                    {
                                        ImageCollection.Add(imageInfo);
                                    }
                                }, System.Windows.Threading.DispatcherPriority.Normal);
                            }
                        });

                        // 如果是第一张图像，设为当前图像
                        if (CurrentImageIndex == -1 && ImageCollection.Count > 0)
                        {
                            CurrentImageIndex = 0;
                        }
                    }
                    finally
                    {
                        // 安全地关闭加载窗口
                        loadingWindow.Dispatcher.Invoke(() => loadingWindow.Close());
                    }
                }
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

        /// <summary>
        /// 运行全部
        /// </summary>
        private void ExecuteRunAll()
        {
            if (ImageCollection.Count == 0)
            {
                MessageBox.Show("请先添加图像", "提示",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // TODO: 触发运行全部图像处理流程的事件或命令
            // 可以通过事件通知主窗口执行批量处理
            var handler = RunAllRequested;
            handler?.Invoke(this, EventArgs.Empty);
        }

        private bool CanExecuteRunAll()
        {
            return ImageCollection?.Count > 0;
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 图像集合更改回调
        /// </summary>
        private static void OnImageCollectionChanged(DependencyObject d, DependencyPropertyChangedEventArgs _)
        {
            var control = (ImagePreviewControl)d;
            control.UpdateDisplayIndices();
            control.UpdateImageSelection();
            control.OnPropertyChanged(nameof(ImageCountDisplay));
        }

        /// <summary>
        /// 更新图像显示索引
        /// </summary>
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
                }
            }
        }

        /// <summary>
        /// 属性更改通知
        /// </summary>
        private void OnPropertyChanged(string _)
        {
            // 实现属性更改通知逻辑
            // 由于这是 UserControl，我们可以通过事件或其他方式通知
            // 这里暂时不需要实现，因为使用了 DependencyProperty
        }

        /// <summary>
        /// 加载图像
        /// </summary>
        private BitmapImage? LoadImage(string filePath)
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(filePath);
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
        /// 加载缩略图
        /// </summary>
        private BitmapImage? LoadThumbnail(string filePath, int maxWidth = 120, int maxHeight = 120)
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(filePath);
                bitmap.DecodePixelWidth = maxWidth;
                bitmap.DecodePixelHeight = maxHeight;
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
            catch
            {
                return null;
            }
        }

        #endregion

        #region 事件

        /// <summary>
        /// 运行全部请求事件
        /// </summary>
        public event EventHandler? RunAllRequested;

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
                return normalStyle;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
