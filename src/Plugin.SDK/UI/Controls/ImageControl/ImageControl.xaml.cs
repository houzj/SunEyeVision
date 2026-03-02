using System;
using System.Collections.Generic;
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
        private BitmapSource? _sourceImage;

        // Canvas平移缩放相关
        private bool _isPanning;
        private Point _panStartPoint;
        private double _offsetX;
        private double _offsetY;

        // 窗口自适应相关
        private bool _autoFitOnResize = true;
        private System.Windows.Threading.DispatcherTimer? _resizeTimer;

        #endregion

        #region 依赖属性

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
                    UpdateZoomText();
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
            get => _sourceImage;
            set
            {
                if (_sourceImage != value)
                {
                    _sourceImage = value;
                    DisplayImage.Source = value;

                    // 同步Canvas尺寸和ImageContainer尺寸
                    if (value != null)
                    {
                        OverlayCanvasControl.Width = value.PixelWidth;
                        OverlayCanvasControl.Height = value.PixelHeight;
                        ImageContainer.Width = value.PixelWidth;
                        ImageContainer.Height = value.PixelHeight;
                        ImageSizeText.Text = $"图像: {value.PixelWidth} x {value.PixelHeight}";
                    }
                    else
                    {
                        ImageSizeText.Text = "图像: -";
                    }

                    OnPropertyChanged();
                    OnImageLoaded();
                }
            }
        }

        /// <summary>
        /// 叠加层Canvas - 用于添加自定义元素
        /// </summary>
        public Canvas OverlayCanvas => OverlayCanvasControl;

        /// <summary>
        /// 窗口调整时是否自动适应
        /// </summary>
        public bool AutoFitOnResize
        {
            get => _autoFitOnResize;
            set => _autoFitOnResize = value;
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
            if (SourceImage == null || MainCanvas == null) return;

            var viewWidth = MainCanvas.ActualWidth;
            var viewHeight = MainCanvas.ActualHeight;

            if (viewWidth <= 0 || viewHeight <= 0) return;

            var scaleX = viewWidth / SourceImage.PixelWidth;
            var scaleY = viewHeight / SourceImage.PixelHeight;

            var newZoom = Math.Min(scaleX, scaleY) * 0.95; // 留5%边距
            Zoom = newZoom;

            // 居中显示
            CenterImage();
        }

        /// <summary>
        /// 显示实际大小
        /// </summary>
        public void ActualSize()
        {
            Zoom = 1.0;
            CenterImage();
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

        #endregion

        #region 私有方法

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            UpdateZoomText();
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

        private void UpdateZoomText()
        {
            ZoomText.Text = $"{(int)(Zoom * 100)}%";
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

        private void ActualSize_Click(object sender, RoutedEventArgs e)
        {
            ActualSize();
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
}
