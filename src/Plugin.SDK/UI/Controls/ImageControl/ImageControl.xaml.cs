using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SunEyeVision.Plugin.SDK.UI.Controls
{
    /// <summary>
    /// 图像显示控件 - 提供图像显示、缩放、平移功能
    /// 支持叠加层机制，允许外部添加自定义绘制层
    /// </summary>
    public partial class ImageControl : UserControl, INotifyPropertyChanged
    {
        #region 私有字段

        private double _zoom = 1.0;
        private double _minZoom = 0.1;
        private double _maxZoom = 10.0;
        private double _zoomStep = 0.1;

        // Canvas平移缩放相关
        private bool _isPanning;
        private Point _panStartPoint;
        private double _offsetX;
        private double _offsetY;

        // 窗口自适应相关
        private bool _autoFitOnResize = true;
        private System.Windows.Threading.DispatcherTimer? _resizeTimer;

        // 全屏相关
        private bool _isFullscreen = false;
        private Window? _fullscreenWindow;
        private FrameworkElement? _originalParent;  // 改为 FrameworkElement 支持更多父容器类型
        private int _originalIndex;
        private bool _parentIsPanel;  // 标记父容器是否为 Panel
        private BitmapSource? _savedSourceImage;  // 全屏前保存的图像引用
        private object? _savedDataContext;  // 保存原 DataContext

        #endregion

        #region 调试日志

        /// <summary>
        /// 添加调试日志（输出到调试窗口和Visual Studio输出）
        /// </summary>
        private void AddLog(string message)
        {
            System.Diagnostics.Debug.WriteLine($"[ImageControl] {message}");
        }

        #endregion

        #region 依赖属性

        /// <summary>
        /// 源图像依赖属性
        /// </summary>
        public static readonly DependencyProperty SourceImageProperty =
            DependencyProperty.Register(nameof(SourceImage), typeof(BitmapSource), typeof(ImageControl),
                new PropertyMetadata(null, OnSourceImageChanged));

        /// <summary>
        /// 是否显示工具栏
        /// </summary>
        public static readonly DependencyProperty ShowToolBarProperty =
            DependencyProperty.Register(nameof(ShowToolBar), typeof(bool), typeof(ImageControl),
                new PropertyMetadata(true, OnShowToolBarChanged));

        /// <summary>
        /// 是否显示状态栏
        /// </summary>
        public static readonly DependencyProperty ShowStatusBarProperty =
            DependencyProperty.Register(nameof(ShowStatusBar), typeof(bool), typeof(ImageControl),
                new PropertyMetadata(true, OnShowStatusBarChanged));

        /// <summary>
        /// 是否启用平移
        /// </summary>
        public static readonly DependencyProperty EnablePanningProperty =
            DependencyProperty.Register(nameof(EnablePanning), typeof(bool), typeof(ImageControl),
                new PropertyMetadata(true));

        /// <summary>
        /// 是否启用缩放
        /// </summary>
        public static readonly DependencyProperty EnableZoomProperty =
            DependencyProperty.Register(nameof(EnableZoom), typeof(bool), typeof(ImageControl),
                new PropertyMetadata(true));

        /// <summary>
        /// 是否显示棋盘格背景（透明度指示）
        /// </summary>
        public static readonly DependencyProperty ShowChessboardBackgroundProperty =
            DependencyProperty.Register(nameof(ShowChessboardBackground), typeof(bool), typeof(ImageControl),
                new PropertyMetadata(true, OnShowChessboardBackgroundChanged));

        /// <summary>
        /// 图像源集合 - 用于图像显示选择器下拉框
        /// </summary>
        public static readonly DependencyProperty ImageSourcesProperty =
            DependencyProperty.Register(nameof(ImageSources), typeof(ObservableCollection<ImageSourceInfo>), typeof(ImageControl),
                new PropertyMetadata(null, OnImageSourcesChanged));

        /// <summary>
        /// 当前选中的图像源索引
        /// </summary>
        public static readonly DependencyProperty SelectedImageSourceIndexProperty =
            DependencyProperty.Register(nameof(SelectedImageSourceIndex), typeof(int), typeof(ImageControl),
                new PropertyMetadata(-1, OnSelectedImageSourceIndexChanged));

        #endregion

        #region 属性

        /// <summary>
        /// 是否显示工具栏
        /// </summary>
        public bool ShowToolBar
        {
            get => (bool)GetValue(ShowToolBarProperty);
            set => SetValue(ShowToolBarProperty, value);
        }

        /// <summary>
        /// 是否显示状态栏
        /// </summary>
        public bool ShowStatusBar
        {
            get => (bool)GetValue(ShowStatusBarProperty);
            set => SetValue(ShowStatusBarProperty, value);
        }

        /// <summary>
        /// 是否启用平移
        /// </summary>
        public bool EnablePanning
        {
            get => (bool)GetValue(EnablePanningProperty);
            set => SetValue(EnablePanningProperty, value);
        }

        /// <summary>
        /// 是否启用缩放
        /// </summary>
        public bool EnableZoom
        {
            get => (bool)GetValue(EnableZoomProperty);
            set => SetValue(EnableZoomProperty, value);
        }

        /// <summary>
        /// 是否显示棋盘格背景（透明度指示）
        /// </summary>
        public bool ShowChessboardBackground
        {
            get => (bool)GetValue(ShowChessboardBackgroundProperty);
            set => SetValue(ShowChessboardBackgroundProperty, value);
        }

        /// <summary>
        /// 缩放比例
        /// </summary>
        public double Zoom
        {
            get => _zoom;
            set
            {
                value = Math.Max(_minZoom, Math.Min(_maxZoom, value));
                if (SetProperty(ref _zoom, value))
                {
                    ImageScaleTransform.ScaleX = value;
                    ImageScaleTransform.ScaleY = value;
                    OnZoomChanged();
                }
            }
        }

        /// <summary>
        /// X偏移量
        /// </summary>
        public double OffsetX => _offsetX;

        /// <summary>
        /// Y偏移量
        /// </summary>
        public double OffsetY => _offsetY;

        /// <summary>
        /// 源图像
        /// </summary>
        public BitmapSource? SourceImage
        {
            get => (BitmapSource?)GetValue(SourceImageProperty);
            set => SetValue(SourceImageProperty, value);
        }

        /// <summary>
        /// 叠加层Canvas - 用于添加自定义元素
        /// </summary>
        public Canvas OverlayCanvas => OverlayCanvasControl;

        /// <summary>
        /// OverlayCanvas是否启用命中测试
        /// 默认为false，允许ROI编辑器等控件根据需要启用
        /// </summary>
        public bool OverlayHitTestVisible
        {
            get => OverlayCanvasControl.IsHitTestVisible;
            set => OverlayCanvasControl.IsHitTestVisible = value;
        }

        /// <summary>
        /// 窗口调整时是否自动适应
        /// </summary>
        public bool AutoFitOnResize
        {
            get => _autoFitOnResize;
            set => _autoFitOnResize = value;
        }

        /// <summary>
        /// 图像源集合 - 用于图像显示选择器下拉框
        /// </summary>
        public ObservableCollection<ImageSourceInfo>? ImageSources
        {
            get => (ObservableCollection<ImageSourceInfo>?)GetValue(ImageSourcesProperty);
            set => SetValue(ImageSourcesProperty, value);
        }

        /// <summary>
        /// 当前选中的图像源索引
        /// </summary>
        public int SelectedImageSourceIndex
        {
            get => (int)GetValue(SelectedImageSourceIndexProperty);
            set => SetValue(SelectedImageSourceIndexProperty, value);
        }

        #endregion

        #region 事件

        /// <summary>
        /// 属性变更事件
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// 图像加载完成事件
        /// </summary>
        public event EventHandler<ImageLoadedEventArgs>? ImageLoaded;

        /// <summary>
        /// 缩放变更事件
        /// </summary>
        public event EventHandler<ZoomChangedEventArgs>? ZoomChanged;

        /// <summary>
        /// 视图变换事件（平移或缩放后）
        /// </summary>
        public event EventHandler<ViewTransformEventArgs>? ViewTransformed;

        /// <summary>
        /// 鼠标在图像上移动事件
        /// </summary>
        public event EventHandler<ImageMouseEventArgs>? ImageMouseMove;

        /// <summary>
        /// 鼠标按下事件（在Canvas上）
        /// </summary>
        public event EventHandler<ImageMouseEventArgs>? CanvasMouseLeftButtonDown;

        /// <summary>
        /// 鼠标释放事件（在Canvas上）
        /// </summary>
        public event EventHandler<ImageMouseEventArgs>? CanvasMouseLeftButtonUp;

        /// <summary>
        /// 图像源选择变更事件
        /// </summary>
        public event EventHandler<ImageSourceSelectionChangedEventArgs>? ImageSourceSelectionChanged;

        #endregion

        #region 构造函数

        public ImageControl()
        {
            InitializeComponent();
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 加载图像
        /// </summary>
        public void LoadImage(BitmapSource image)
        {
            SourceImage = image;

            // 重置缩放和偏移
            Zoom = 1.0;
            _offsetX = 0;
            _offsetY = 0;

            // 确保在布局更新后执行自适应
            Dispatcher.BeginInvoke(new Action(() =>
            {
                FitToWindow();
            }), System.Windows.Threading.DispatcherPriority.ContextIdle);
        }

        /// <summary>
        /// 适应窗口大小
        /// </summary>
        public void FitToWindow()
        {
            AddLog($"[FitToWindow] 开始执行");
            AddLog($"[FitToWindow] SourceImage: {(SourceImage != null ? $"{SourceImage.PixelWidth}x{SourceImage.PixelHeight}" : "null")}");
            AddLog($"[FitToWindow] MainCanvas: {(MainCanvas != null ? "存在" : "null")}");
            
            if (SourceImage == null || MainCanvas == null)
            {
                AddLog($"[FitToWindow] ❌ 提前返回: SourceImage或MainCanvas为null");
                return;
            }

            var viewWidth = MainCanvas.ActualWidth;
            var viewHeight = MainCanvas.ActualHeight;
            AddLog($"[FitToWindow] MainCanvas尺寸: {viewWidth}x{viewHeight}");

            if (viewWidth <= 0 || viewHeight <= 0)
            {
                AddLog($"[FitToWindow] ❌ 提前返回: MainCanvas尺寸无效");
                return;
            }

            var scaleX = viewWidth / SourceImage.PixelWidth;
            var scaleY = viewHeight / SourceImage.PixelHeight;
            AddLog($"[FitToWindow] scaleX={scaleX:F4}, scaleY={scaleY:F4}");

            var newZoom = Math.Min(scaleX, scaleY); // 不留边距，完全适应
            AddLog($"[FitToWindow] 计算newZoom={newZoom:F4}, 当前Zoom={Zoom:F4}");
            Zoom = newZoom;

            // 居中显示
            CenterImage();
            AddLog($"[FitToWindow] ✓ 完成");
        }

        /// <summary>
        /// 居中图像
        /// </summary>
        public void CenterImage()
        {
            if (SourceImage == null || MainCanvas == null) return;

            var viewWidth = MainCanvas.ActualWidth;
            var viewHeight = MainCanvas.ActualHeight;
            var imageWidth = SourceImage.PixelWidth * Zoom;
            var imageHeight = SourceImage.PixelHeight * Zoom;

            _offsetX = (viewWidth - imageWidth) / 2;
            _offsetY = (viewHeight - imageHeight) / 2;

            UpdateImageTransform();
        }

        /// <summary>
        /// 屏幕坐标转图像坐标
        /// </summary>
        public Point ScreenToImage(Point screenPoint)
        {
            return new Point(
                (screenPoint.X - _offsetX) / Zoom,
                (screenPoint.Y - _offsetY) / Zoom);
        }

        /// <summary>
        /// 图像坐标转屏幕坐标
        /// </summary>
        public Point ImageToScreen(Point imagePoint)
        {
            return new Point(
                imagePoint.X * Zoom + _offsetX,
                imagePoint.Y * Zoom + _offsetY);
        }

        /// <summary>
        /// 围绕指定点缩放
        /// </summary>
        public void ZoomAroundPoint(Point center, double delta)
        {
            var oldZoom = Zoom;
            var newZoom = Math.Max(_minZoom, Math.Min(_maxZoom, Zoom + delta));

            if (Math.Abs(newZoom - oldZoom) < 0.001) return;

            // 计算缩放前后鼠标在图像坐标系中的位置
            var imagePosBefore = ScreenToImage(center);
            Zoom = newZoom;
            var imagePosAfter = ScreenToImage(center);

            // 调整偏移以保持鼠标位置不变
            _offsetX += (imagePosAfter.X - imagePosBefore.X) * Zoom;
            _offsetY += (imagePosAfter.Y - imagePosBefore.Y) * Zoom;

            UpdateImageTransform();
        }

        /// <summary>
        /// 添加叠加元素
        /// </summary>
        public void AddOverlay(System.Windows.UIElement element)
        {
            OverlayCanvasControl.Children.Add(element);
        }

        /// <summary>
        /// 移除叠加元素
        /// </summary>
        public void RemoveOverlay(System.Windows.UIElement element)
        {
            OverlayCanvasControl.Children.Remove(element);
        }

        /// <summary>
        /// 清除所有叠加元素
        /// </summary>
        public void ClearOverlays()
        {
            OverlayCanvasControl.Children.Clear();
        }

        /// <summary>
        /// 切换全屏状态（公开方法）
        /// </summary>
        public void ToggleFullscreen()
        {
            if (!_isFullscreen)
            {
                EnterFullscreen();
            }
            else
            {
                ExitFullscreen();
            }
        }

        /// <summary>
        /// 进入全屏（公开方法）
        /// </summary>
        public void EnterFullscreenMode()
        {
            if (!_isFullscreen)
            {
                EnterFullscreen();
            }
        }

        /// <summary>
        /// 退出全屏（公开方法）
        /// </summary>
        public void ExitFullscreenMode()
        {
            ExitFullscreen();
        }

        /// <summary>
        /// 获取当前是否处于全屏状态
        /// </summary>
        public bool IsFullscreen => _isFullscreen;

        #endregion

        #region 私有方法

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
        }

        private static void OnShowToolBarChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ImageControl control)
            {
                control.ToolBar.Visibility = (bool)e.NewValue ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private static void OnShowStatusBarChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ImageControl control)
            {
                control.StatusBar.Visibility = (bool)e.NewValue ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private static void OnShowChessboardBackgroundChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ImageControl control && control.ChessboardLayer != null)
            {
                control.ChessboardLayer.Visibility = (bool)e.NewValue ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private static void OnImageSourcesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ImageControl control)
            {
                // 更新 ComboBox 的 ItemsSource
                control.ImageSourceSelector.ItemsSource = e.NewValue as ObservableCollection<ImageSourceInfo>;
            }
        }

        private static void OnSelectedImageSourceIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ImageControl control)
            {
                var newIndex = (int)e.NewValue;
                if (newIndex >= 0 && control.ImageSourceSelector.Items.Count > newIndex)
                {
                    control.ImageSourceSelector.SelectedIndex = newIndex;
                }
            }
        }

        private static void OnSourceImageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ImageControl control)
            {
                var newImage = e.NewValue as BitmapSource;
                control.DisplayImage.Source = newImage;

                // 同步Canvas尺寸和ImageContainer尺寸
                if (newImage != null)
                {
                    control.OverlayCanvasControl.Width = newImage.PixelWidth;
                    control.OverlayCanvasControl.Height = newImage.PixelHeight;
                    control.ImageContainer.Width = newImage.PixelWidth;
                    control.ImageContainer.Height = newImage.PixelHeight;
                    control.ImageSizeText.Text = $"图像: {newImage.PixelWidth} x {newImage.PixelHeight}";
                    
                    // 图像加载后自动适应窗口 - 使用 Loaded 优先级确保布局完成
                    if (control._autoFitOnResize)
                    {
                        control.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            control.FitToWindow();
                        }), System.Windows.Threading.DispatcherPriority.Loaded);
                    }
                }
                else
                {
                    control.ImageSizeText.Text = "图像: -";
                }

                control.OnImageLoaded();
            }
        }

        private void UpdateImageTransform()
        {
            if (ImageScaleTransform == null || ImageTranslateTransform == null) return;

            ImageScaleTransform.ScaleX = Zoom;
            ImageScaleTransform.ScaleY = Zoom;
            ImageTranslateTransform.X = _offsetX;
            ImageTranslateTransform.Y = _offsetY;

            OnViewTransformed();
        }

        private void OnImageLoaded()
        {
            ImageLoaded?.Invoke(this, new ImageLoadedEventArgs(SourceImage));
        }

        private void OnZoomChanged()
        {
            ZoomChanged?.Invoke(this, new ZoomChangedEventArgs(Zoom));
        }

        private void OnViewTransformed()
        {
            ViewTransformed?.Invoke(this, new ViewTransformEventArgs(Zoom, _offsetX, _offsetY));
        }

        #endregion

        #region 事件处理

        private void FitToWindow_Click(object sender, RoutedEventArgs e)
        {
            FitToWindow();
        }

        private void ZoomIn_Click(object sender, RoutedEventArgs e)
        {
            Zoom = Math.Min(_maxZoom, Zoom + _zoomStep);
        }

        private void ZoomOut_Click(object sender, RoutedEventArgs e)
        {
            Zoom = Math.Max(_minZoom, Zoom - _zoomStep);
        }

        private void ToggleFullscreen_Click(object sender, RoutedEventArgs e)
        {
            if (!_isFullscreen)
            {
                EnterFullscreen();
            }
            else
            {
                ExitFullscreen();
            }
        }

        private void EnterFullscreen()
        {
            AddLog($"EnterFullscreen 被调用");
            AddLog($"Parent 类型: {Parent?.GetType().Name ?? "null"}");
            
            // ★ 在移动控件前保存图像引用（绑定可能会丢失）
            _savedSourceImage = SourceImage;
            AddLog($"保存图像引用: {(_savedSourceImage != null ? $"{_savedSourceImage.PixelWidth}x{_savedSourceImage.PixelHeight}" : "null")}");
            
            // 记录原父容器
            _originalParent = Parent as FrameworkElement;
            if (_originalParent == null)
            {
                AddLog($"❌ 父容器为 null，无法进入全屏");
                return;
            }
           

            AddLog($"✓ 父容器类型: {_originalParent.GetType().Name}");
            
            // ★★★ 关键修复：在移除父容器之前保存 DataContext ★★★
            _savedDataContext = _originalParent.DataContext ?? this.DataContext;
            AddLog($"保存 DataContext: {_savedDataContext?.GetType().Name ?? "null"}");

            // 判断父容器类型并保存状态
            if (_originalParent is Panel panel)
            {
                _parentIsPanel = true;
                _originalIndex = panel.Children.IndexOf(this);
                panel.Children.Remove(this);
                AddLog($"✓ 从 Panel 移除，索引: {_originalIndex}");
            }
            else if (_originalParent is Decorator decorator)
            {
                _parentIsPanel = false;
                decorator.Child = null;
                AddLog($"✓ 从 Decorator 移除");
            }
            else
            {
                AddLog($"❌ 不支持的父容器类型: {_originalParent.GetType().Name}");
                _originalParent = null;
                return;
            }

            // 创建全屏窗口
            _fullscreenWindow = new Window
            {
                WindowStyle = WindowStyle.None,
                WindowState = WindowState.Maximized,
                Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x1E)),
                Content = this,
                Title = "全屏预览",
                DataContext = _savedDataContext  // ★ 关键：设置 DataContext 保持绑定有效
            };

            _fullscreenWindow.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Escape)
                {
                    ExitFullscreen();
                }
            };

            _fullscreenWindow.Closed += ExitFullscreen_Handler;

            _isFullscreen = true;
            _fullscreenWindow.Show();
            AddLog($"[EnterFullscreen] ✓ 全屏窗口已显示");
            AddLog($"[EnterFullscreen] 窗口尺寸: {_fullscreenWindow.ActualWidth}x{_fullscreenWindow.ActualHeight}");
            AddLog($"[EnterFullscreen] MainCanvas尺寸: {MainCanvas.ActualWidth}x{MainCanvas.ActualHeight}");
            
            // 全屏窗口内容渲染完成后，自适应图像（确保窗口尺寸已确定）
            _fullscreenWindow.ContentRendered += (s, args) =>
            {
                AddLog($"[ContentRendered] 触发，窗口尺寸: {_fullscreenWindow?.ActualWidth}x{_fullscreenWindow?.ActualHeight}");
                AddLog($"[ContentRendered] MainCanvas尺寸: {MainCanvas.ActualWidth}x{MainCanvas.ActualHeight}");
                AddLog($"[ContentRendered] 当前SourceImage: {(SourceImage != null ? $"{SourceImage.PixelWidth}x{SourceImage.PixelHeight}" : "null")}");
                
                // ★ 如果绑定丢失，恢复保存的图像引用
                if (SourceImage == null && _savedSourceImage != null)
                {
                    AddLog($"[ContentRendered] 恢复保存的图像引用");
                    SourceImage = _savedSourceImage;
                }
                
                FitToWindow();
            };
        }

        private void ExitFullscreen()
        {
            AddLog($"ExitFullscreen 被调用");
            
            if (!_isFullscreen || _originalParent == null)
            {
                AddLog($"❌ 无法退出全屏: _isFullscreen={_isFullscreen}, _originalParent={_originalParent != null}");
                return;
            }

            // 防止重复调用
            _isFullscreen = false;

            // ★ 保存当前图像引用（如果绑定丢失时恢复用）
            var imageToRestore = SourceImage ?? _savedSourceImage;
            AddLog($"保存图像引用用于恢复: {(imageToRestore != null ? $"{imageToRestore.PixelWidth}x{imageToRestore.PixelHeight}" : "null")}");

            if (_fullscreenWindow != null)
            {
                // 先取消订阅事件，防止重复触发
                _fullscreenWindow.Closed -= ExitFullscreen_Handler;
                _fullscreenWindow.Content = null;
                _fullscreenWindow.Close();
                _fullscreenWindow = null;
                AddLog($"✓ 全屏窗口已关闭");
            }

            // 检查控件是否已经有父容器
            if (Parent != null)
            {
                AddLog($"⚠ 控件已有父容器，不再插入");
                return;
            }

            // 将控件返回到原来的父容器
            if (_parentIsPanel && _originalParent is Panel panel)
            {
                panel.Children.Insert(_originalIndex, this);
                AddLog($"✓ 已插入回 Panel，索引: {_originalIndex}");
            }
            else if (!_parentIsPanel && _originalParent is Decorator decorator)
            {
                decorator.Child = this;
                AddLog($"✓ 已设置回 Decorator");
            }
            else
            {
                AddLog($"❌ 无法恢复到父容器");
            }
            
            // ★★★ 关键修复：恢复父容器的 DataContext ★★★
            if (_originalParent != null && _savedDataContext != null)
            {
                _originalParent.DataContext = _savedDataContext;
                AddLog($"✓ 已恢复父容器 DataContext: {_savedDataContext?.GetType().Name ?? "null"}");
            }
            
            // 返回原父容器后，检查绑定是否恢复并自适应图像
            Dispatcher.BeginInvoke(new Action(() =>
            {
                AddLog($"[延迟检查] SourceImage: {(SourceImage != null ? $"{SourceImage.PixelWidth}x{SourceImage.PixelHeight}" : "null")}");
                AddLog($"[延迟检查] this.DataContext: {this.DataContext?.GetType().Name ?? "null"}");
                AddLog($"[延迟检查] _originalParent.DataContext: {(_originalParent as FrameworkElement)?.DataContext?.GetType().Name ?? "null"}");
                
                // ★ 关键修复：如果绑定未恢复，手动设置图像
                if (SourceImage == null && imageToRestore != null)
                {
                    AddLog($"[延迟检查] ⚠ 绑定未恢复，手动设置图像");
                    SourceImage = imageToRestore;
                }
                
                // 清理保存的引用
                _savedSourceImage = null;
                _savedDataContext = null;
                
                // 自适应图像
                FitToWindow();
                AddLog($"[延迟检查] ✓ FitToWindow 完成");
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private void ExitFullscreen_Handler(object? sender, EventArgs e)
        {
            ExitFullscreen();
        }

        private void ImageSourceSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && e.AddedItems[0] is ImageSourceInfo selectedSource)
            {
                // 更新选中索引
                SelectedImageSourceIndex = ImageSourceSelector.SelectedIndex;
                
                // 触发事件
                ImageSourceSelectionChanged?.Invoke(this, new ImageSourceSelectionChangedEventArgs(selectedSource));
            }
        }

        private void MainCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (!EnableZoom) return;

            // 围绕鼠标位置缩放
            var mousePos = e.GetPosition(MainCanvas);
            ZoomAroundPoint(mousePos, e.Delta > 0 ? _zoomStep : -_zoomStep);
            e.Handled = true;
        }

        private void MainCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 计算图像坐标
            var screenPoint = e.GetPosition(MainCanvas);
            var imagePos = ScreenToImage(screenPoint);

            // 触发外部事件，允许外部处理
            var args = new ImageMouseEventArgs(imagePos, screenPoint, e);
            CanvasMouseLeftButtonDown?.Invoke(this, args);

            // 如果外部已处理，则不执行平移
            if (args.Handled)
            {
                return;
            }

            if (!EnablePanning) return;

            // 只有点击位置在图像范围内才启动平移
            if (SourceImage != null)
            {
                // 检查点击位置是否在图像范围内
                if (imagePos.X < 0 || imagePos.X >= SourceImage.PixelWidth ||
                    imagePos.Y < 0 || imagePos.Y >= SourceImage.PixelHeight)
                {
                    // 点击位置在图像外，不启动平移
                    return;
                }
            }

            // 启动平移
            _isPanning = true;
            _panStartPoint = e.GetPosition(MainCanvas);
            MainCanvas.Cursor = Cursors.SizeAll;
            MainCanvas.CaptureMouse();
            e.Handled = true;
        }

        private void MainCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            // 更新位置显示
            if (SourceImage != null)
            {
                var screenPoint = e.GetPosition(MainCanvas);
                var imagePos = ScreenToImage(screenPoint);
                PositionText.Text = $"位置: ({(int)imagePos.X}, {(int)imagePos.Y})";

                // 触发鼠标移动事件
                ImageMouseMove?.Invoke(this, new ImageMouseEventArgs(imagePos, screenPoint, e));
            }

            if (_isPanning && EnablePanning)
            {
                var currentPoint = e.GetPosition(MainCanvas);
                var delta = currentPoint - _panStartPoint;
                _offsetX += delta.X;
                _offsetY += delta.Y;
                _panStartPoint = currentPoint;

                UpdateImageTransform();
                e.Handled = true;
            }
        }

        private void MainCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // 计算图像坐标
            var screenPoint = e.GetPosition(MainCanvas);
            var imagePos = ScreenToImage(screenPoint);

            // 触发外部事件
            var args = new ImageMouseEventArgs(imagePos, screenPoint, e);
            CanvasMouseLeftButtonUp?.Invoke(this, args);

            if (_isPanning)
            {
                _isPanning = false;
                MainCanvas.Cursor = Cursors.Arrow;
                MainCanvas.ReleaseMouseCapture();
                e.Handled = true;
            }
        }

        private void MainCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_autoFitOnResize && SourceImage != null)
            {
                // 延迟执行，避免频繁调用
                _resizeTimer?.Stop();
                _resizeTimer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
                _resizeTimer.Tick += (s, args) =>
                {
                    _resizeTimer.Stop();
                    FitToWindow();
                };
                _resizeTimer.Start();
            }
        }

        #endregion

        #region INotifyPropertyChanged

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }

    /// <summary>
    /// 图像源选择变更事件参数
    /// </summary>
    public class ImageSourceSelectionChangedEventArgs : EventArgs
    {
        /// <summary>
        /// 选中的图像源信息
        /// </summary>
        public ImageSourceInfo SelectedSource { get; }

        public ImageSourceSelectionChangedEventArgs(ImageSourceInfo selectedSource)
        {
            SelectedSource = selectedSource;
        }
    }
}
