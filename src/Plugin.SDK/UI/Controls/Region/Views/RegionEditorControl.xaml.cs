using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using SunEyeVision.Plugin.SDK.UI.Controls.Region.Models;
using SunEyeVision.Plugin.SDK.UI.Controls.Region.ViewModels;
using SunEyeVision.Plugin.SDK.UI.Controls;

namespace SunEyeVision.Plugin.SDK.UI.Controls.Region.Views
{
    /// <summary>
    /// 区域编辑控件 - 纯UI组件，不负责数据持久化
    /// </summary>
    public partial class RegionEditorControl : UserControl, INotifyPropertyChanged
    {
        private RegionEditorViewModel _viewModel;
        private ShapeType _currentTool = ShapeType.Rectangle;
        private ImageControl? _mainImageControl;

        // 绘制状态
        private bool _isDrawing;
        private bool _isDragging;
        private Point _startPoint;
        private RegionData? _currentDrawingRegion;
        private RegionData? _selectedRegion;
        private Point _dragStartPoint;

        // 区域名称索引管理
        private int _regionIndex = 0;

        public RegionEditorViewModel ViewModel => _viewModel;

        /// <summary>
        /// 获取OverlayCanvas
        /// </summary>
        private Canvas? OverlayCanvas => _mainImageControl?.OverlayCanvas;

        #region 依赖属性

        /// <summary>
        /// 区域数据集合
        /// </summary>
        public static readonly DependencyProperty RegionsProperty =
            DependencyProperty.Register(nameof(Regions), typeof(IEnumerable<RegionData>), typeof(RegionEditorControl),
                new PropertyMetadata(null, OnRegionsChanged));

        public IEnumerable<RegionData>? Regions
        {
            get => (IEnumerable<RegionData>?)GetValue(RegionsProperty);
            set => SetValue(RegionsProperty, value);
        }

        private static void OnRegionsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is RegionEditorControl control)
            {
                if (e.NewValue is IEnumerable<RegionData> regions)
                {
                    control.SetRegions(regions);
                }
            }
        }

        #endregion

        #region 事件

        /// <summary>
        /// 区域数据变更事件
        /// </summary>
        public event EventHandler<RegionData>? RegionDataChanged;

        #endregion

        public RegionEditorControl()
        {
            // 设计时跳过初始化
            if (DesignerProperties.GetIsInDesignMode(this))
            {
                InitializeComponent();
                _viewModel = new RegionEditorViewModel();
                DataContext = _viewModel;
                return;
            }

            InitializeComponent();
            _viewModel = new RegionEditorViewModel();
            DataContext = _viewModel;

            // 订阅事件
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            _viewModel.RegionChanged += OnRegionChanged;
        }

        #region 公共方法

        /// <summary>
        /// 设置主界面ImageControl引用
        /// </summary>
        public void SetMainImageControl(ImageControl imageControl)
        {
            _mainImageControl = imageControl;
            
            if (_mainImageControl != null)
            {
                // 启用OverlayCanvas命中测试
                _mainImageControl.OverlayHitTestVisible = true;

                // 订阅ImageControl事件
                _mainImageControl.ImageMouseMove += ImageControl_ImageMouseMove;
                _mainImageControl.ViewTransformed += ImageControl_ViewTransformed;
                _mainImageControl.CanvasMouseLeftButtonDown += ImageControl_CanvasMouseLeftButtonDown;
                _mainImageControl.CanvasMouseLeftButtonUp += ImageControl_CanvasMouseLeftButtonUp;

                // 添加自己的鼠标事件处理（当CaptureMouse时使用）
                MouseMove += RegionEditorControl_MouseMove;
                MouseLeftButtonUp += RegionEditorControl_MouseLeftButtonUp;
                LostMouseCapture += RegionEditorControl_LostMouseCapture;
            }

            UpdateRegionOverlay();
        }

        /// <summary>
        /// 清理主界面覆盖层
        /// </summary>
        public void ClearMainOverlay()
        {
            if (_mainImageControl != null)
            {
                // 取消订阅事件
                _mainImageControl.ImageMouseMove -= ImageControl_ImageMouseMove;
                _mainImageControl.ViewTransformed -= ImageControl_ViewTransformed;
                _mainImageControl.CanvasMouseLeftButtonDown -= ImageControl_CanvasMouseLeftButtonDown;
                _mainImageControl.CanvasMouseLeftButtonUp -= ImageControl_CanvasMouseLeftButtonUp;

                MouseMove -= RegionEditorControl_MouseMove;
                MouseLeftButtonUp -= RegionEditorControl_MouseLeftButtonUp;
                LostMouseCapture -= RegionEditorControl_LostMouseCapture;

                // 清理覆盖层
                _mainImageControl.OverlayCanvas.Children.Clear();
            }

            _mainImageControl = null;
        }

        /// <summary>
        /// 加载图像
        /// </summary>
        public void LoadImage(BitmapSource image)
        {
            if (_mainImageControl != null)
            {
                _mainImageControl.SourceImage = image;
            }
        }

        /// <summary>
        /// 获取所有区域数据
        /// </summary>
        public IEnumerable<RegionData> GetRegions()
        {
            return _viewModel.Regions;
        }

        /// <summary>
        /// 设置区域数据
        /// </summary>
        public void SetRegions(IEnumerable<RegionData> regions)
        {
            _viewModel.Regions.Clear();
            foreach (var region in regions)
            {
                _viewModel.Regions.Add(region);
            }
            UpdateRegionOverlay();
        }

        /// <summary>
        /// 获取选中的区域
        /// </summary>
        public RegionData? GetSelectedRegion()
        {
            return _selectedRegion;
        }

        /// <summary>
        /// 设置选中的区域
        /// </summary>
        public void SetSelectedRegion(RegionData? region)
        {
            SelectRegion(region);
        }

        /// <summary>
        /// 添加区域
        /// </summary>
        public void AddRegion(RegionData region)
        {
            _viewModel.Regions.Add(region);
            UpdateRegionOverlay();
            RegionDataChanged?.Invoke(this, region);
        }

        /// <summary>
        /// 移除区域
        /// </summary>
        public void RemoveRegion(RegionData region)
        {
            _viewModel.Regions.Remove(region);
            if (_selectedRegion == region)
                _selectedRegion = null;
            UpdateRegionOverlay();
            RegionDataChanged?.Invoke(this, region);
        }

        /// <summary>
        /// 解析所有区域
        /// </summary>
        public List<Logic.ResolvedRegion> ResolveAllRegions()
        {
            return _viewModel.ResolveAllRegions();
        }

        #endregion

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // 初始化完成
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            // 取消订阅事件
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            _viewModel.RegionChanged -= OnRegionChanged;
            _viewModel.Dispose();

            // 清理主界面引用
            ClearMainOverlay();
        }

        #region 鼠标事件处理

        private void ImageControl_ImageMouseMove(object? sender, ImageMouseEventArgs e)
        {
            if (OverlayCanvas == null) return;

            var position = e.ImagePosition;

            // 拖动区域
            if (_isDragging && _selectedRegion != null)
            {
                var offset = position - _dragStartPoint;
                MoveRegion(_selectedRegion, offset);
                _dragStartPoint = position;
                UpdateRegionOverlay();
                return;
            }

            // 绘制预览
            if (_isDrawing && _currentDrawingRegion != null)
            {
                UpdateDrawingPreview(position);
                UpdateRegionOverlay();
            }
        }

        private void RegionEditorControl_MouseMove(object sender, MouseEventArgs e)
        {
            if (_mainImageControl == null || OverlayCanvas == null) return;
            
            // 只在捕获鼠标时处理
            if (!_isDrawing && !_isDragging) return;

            var screenPoint = e.GetPosition(_mainImageControl.MainCanvas);
            var position = _mainImageControl.ScreenToImage(screenPoint);

            if (_isDragging && _selectedRegion != null)
            {
                var offset = position - _dragStartPoint;
                MoveRegion(_selectedRegion, offset);
                _dragStartPoint = position;
                UpdateRegionOverlay();
            }
            else if (_isDrawing && _currentDrawingRegion != null)
            {
                UpdateDrawingPreview(position);
                UpdateRegionOverlay();
            }
        }

        private void ImageControl_CanvasMouseLeftButtonDown(object? sender, ImageMouseEventArgs e)
        {
            if (OverlayCanvas == null) return;

            var position = e.ImagePosition;

            // 只在绘制模式处理
            if (!_viewModel.IsDrawingMode) return;

            // 绘制工具
            if (_viewModel.IsDrawing && _currentTool != ShapeType.Point)
            {
                // 开始绘制
                _isDrawing = true;
                _startPoint = position;
                CaptureMouse();

                _currentDrawingRegion = RegionData.CreateDrawingRegion(
                    GenerateRegionName(),
                    _currentTool
                );

                if (_currentDrawingRegion.Definition is ShapeDefinition shapeDef)
                {
                    switch (_currentTool)
                    {
                        case ShapeType.Rectangle:
                        case ShapeType.RotatedRectangle:
                            shapeDef.CenterX = position.X;
                            shapeDef.CenterY = position.Y;
                            shapeDef.Width = 0;
                            shapeDef.Height = 0;
                            break;
                        case ShapeType.Circle:
                            shapeDef.CenterX = position.X;
                            shapeDef.CenterY = position.Y;
                            shapeDef.Radius = 0;
                            break;
                        case ShapeType.Line:
                            shapeDef.StartX = position.X;
                            shapeDef.StartY = position.Y;
                            shapeDef.EndX = position.X;
                            shapeDef.EndY = position.Y;
                            break;
                    }
                }

                e.Handled = true;
                return;
            }

            // 选择模式 - 检测命中
            var hitRegion = HitTestRegion(position);
            if (hitRegion != null)
            {
                SelectRegion(hitRegion);
                _isDragging = true;
                _dragStartPoint = position;
                CaptureMouse();
                e.Handled = true;
            }
            else
            {
                DeselectAll();
            }
        }

        private void ImageControl_CanvasMouseLeftButtonUp(object? sender, ImageMouseEventArgs e)
        {
            FinishInteraction();
        }

        private void RegionEditorControl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            FinishInteraction();
        }

        private void RegionEditorControl_LostMouseCapture(object sender, MouseEventArgs e)
        {
            FinishInteraction();
        }

        private void ImageControl_ViewTransformed(object? sender, ViewTransformEventArgs e)
        {
            UpdateRegionOverlay();
        }

        private void FinishInteraction()
        {
            if (_isDrawing && _currentDrawingRegion != null)
            {
                // 完成绘制
                var shapeDef = _currentDrawingRegion.Definition as ShapeDefinition;
                if (shapeDef != null)
                {
                    bool isValid = _currentTool switch
                    {
                        ShapeType.Rectangle or ShapeType.RotatedRectangle => shapeDef.Width > 5 && shapeDef.Height > 5,
                        ShapeType.Circle => shapeDef.Radius > 5,
                        ShapeType.Line => shapeDef.GetLineLength() > 5,
                        _ => false
                    };

                    if (isValid)
                    {
                        _viewModel.Regions.Add(_currentDrawingRegion);
                        _viewModel.SelectedRegion = _currentDrawingRegion;
                        _viewModel.StatusMessage = $"已创建 {_currentTool} 区域";
                        RegionDataChanged?.Invoke(this, _currentDrawingRegion);
                    }
                    else
                    {
                        _viewModel.StatusMessage = "区域太小，已忽略";
                    }
                }
            }

            _isDrawing = false;
            _isDragging = false;
            _currentDrawingRegion = null;
            ReleaseMouseCapture();
            UpdateRegionOverlay();
        }

        #endregion

        #region 绘制和渲染

        private void UpdateDrawingPreview(Point currentPosition)
        {
            if (_currentDrawingRegion?.Definition is not ShapeDefinition shapeDef)
                return;

            var dx = currentPosition.X - _startPoint.X;
            var dy = currentPosition.Y - _startPoint.Y;

            switch (_currentTool)
            {
                case ShapeType.Rectangle:
                case ShapeType.RotatedRectangle:
                    shapeDef.CenterX = _startPoint.X + dx / 2;
                    shapeDef.CenterY = _startPoint.Y + dy / 2;
                    shapeDef.Width = Math.Abs(dx);
                    shapeDef.Height = Math.Abs(dy);
                    break;

                case ShapeType.Circle:
                    var radius = Math.Sqrt(dx * dx + dy * dy);
                    shapeDef.Radius = radius;
                    break;

                case ShapeType.Line:
                    shapeDef.EndX = currentPosition.X;
                    shapeDef.EndY = currentPosition.Y;
                    break;
            }
        }

        private void MoveRegion(RegionData region, Vector offset)
        {
            if (region.Definition is ShapeDefinition shapeDef)
            {
                shapeDef.CenterX += offset.X;
                shapeDef.CenterY += offset.Y;
                shapeDef.StartX += offset.X;
                shapeDef.StartY += offset.Y;
                shapeDef.EndX += offset.X;
                shapeDef.EndY += offset.Y;

                // 移动多边形顶点
                for (int i = 0; i < shapeDef.Points.Count; i++)
                {
                    var pt = shapeDef.Points[i];
                    shapeDef.Points[i] = new Point2D(pt.X + offset.X, pt.Y + offset.Y);
                }

                region.MarkModified();
                RegionDataChanged?.Invoke(this, region);
            }
        }

        private void UpdateRegionOverlay()
        {
            if (OverlayCanvas == null) return;

            OverlayCanvas.Children.Clear();

            foreach (var region in _viewModel.Regions)
            {
                if (!region.IsVisible) continue;

                var shape = CreateRegionShape(region);
                if (shape != null)
                {
                    OverlayCanvas.Children.Add(shape);
                }

                // 绘制名称标签
                DrawRegionLabel(region);
            }

            // 绘制当前正在创建的区域
            if (_isDrawing && _currentDrawingRegion != null)
            {
                var previewShape = CreateRegionShape(_currentDrawingRegion, true);
                if (previewShape != null)
                {
                    OverlayCanvas.Children.Add(previewShape);
                }
            }
        }

        private Shape? CreateRegionShape(RegionData region, bool isPreview = false)
        {
            if (region.Definition is not ShapeDefinition shapeDef)
                return null;

            var color = Color.FromRgb(
                (byte)(region.DisplayColor >> 16 & 0xFF),
                (byte)(region.DisplayColor >> 8 & 0xFF),
                (byte)(region.DisplayColor & 0xFF)
            );

            var fillColor = isPreview
                ? new SolidColorBrush(Color.FromArgb(30, 0, 120, 215))
                : new SolidColorBrush(color) { Opacity = region.DisplayOpacity };

            var strokeColor = region == _selectedRegion
                ? Brushes.Blue
                : new SolidColorBrush(color);

            var strokeThickness = region == _selectedRegion ? 2 : 1;

            Shape? shape = null;

            switch (shapeDef.ShapeType)
            {
                case ShapeType.Rectangle:
                    shape = new Rectangle
                    {
                        Width = shapeDef.Width,
                        Height = shapeDef.Height,
                        Fill = fillColor,
                        Stroke = strokeColor,
                        StrokeThickness = strokeThickness,
                        StrokeDashArray = isPreview ? new DoubleCollection { 4, 2 } : new DoubleCollection()
                    };
                    Canvas.SetLeft(shape, shapeDef.CenterX - shapeDef.Width / 2);
                    Canvas.SetTop(shape, shapeDef.CenterY - shapeDef.Height / 2);
                    break;

                case ShapeType.RotatedRectangle:
                    var rotRect = new Rectangle
                    {
                        Width = shapeDef.Width,
                        Height = shapeDef.Height,
                        Fill = fillColor,
                        Stroke = strokeColor,
                        StrokeThickness = strokeThickness,
                        StrokeDashArray = isPreview ? new DoubleCollection { 4, 2 } : new DoubleCollection()
                    };
                    rotRect.RenderTransform = new RotateTransform(-shapeDef.Angle, shapeDef.Width / 2, shapeDef.Height / 2);
                    Canvas.SetLeft(rotRect, shapeDef.CenterX - shapeDef.Width / 2);
                    Canvas.SetTop(rotRect, shapeDef.CenterY - shapeDef.Height / 2);
                    shape = rotRect;
                    break;

                case ShapeType.Circle:
                    shape = new Ellipse
                    {
                        Width = shapeDef.Radius * 2,
                        Height = shapeDef.Radius * 2,
                        Fill = fillColor,
                        Stroke = strokeColor,
                        StrokeThickness = strokeThickness,
                        StrokeDashArray = isPreview ? new DoubleCollection { 4, 2 } : new DoubleCollection()
                    };
                    Canvas.SetLeft(shape, shapeDef.CenterX - shapeDef.Radius);
                    Canvas.SetTop(shape, shapeDef.CenterY - shapeDef.Radius);
                    break;

                case ShapeType.Line:
                    shape = new Line
                    {
                        X1 = shapeDef.StartX,
                        Y1 = shapeDef.StartY,
                        X2 = shapeDef.EndX,
                        Y2 = shapeDef.EndY,
                        Stroke = strokeColor,
                        StrokeThickness = strokeThickness,
                        StrokeDashArray = isPreview ? new DoubleCollection { 4, 2 } : new DoubleCollection()
                    };
                    break;
            }

            return shape;
        }

        private void DrawRegionLabel(RegionData region)
        {
            if (OverlayCanvas == null || string.IsNullOrEmpty(region.Name)) return;

            var bounds = GetRegionBounds(region);
            double labelTop = bounds.Top - 18;

            if (labelTop < 0) labelTop = bounds.Bottom + 2;

            var label = new TextBlock
            {
                Text = region.Name,
                FontSize = 12,
                Foreground = Brushes.White,
                Background = new SolidColorBrush(Color.FromArgb(180, 0, 0, 0)),
                Padding = new Thickness(4, 2, 4, 2),
                Tag = region.Id
            };

            label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            double labelWidth = label.DesiredSize.Width;

            double labelLeft = bounds.Left + (bounds.Width - labelWidth) / 2;
            if (labelLeft < 0) labelLeft = 0;

            Canvas.SetLeft(label, labelLeft);
            Canvas.SetTop(label, labelTop);
            OverlayCanvas.Children.Add(label);
        }

        private Rect GetRegionBounds(RegionData region)
        {
            if (region.Definition is not ShapeDefinition shapeDef)
                return Rect.Empty;

            return shapeDef.ShapeType switch
            {
                ShapeType.Rectangle => new Rect(
                    shapeDef.CenterX - shapeDef.Width / 2,
                    shapeDef.CenterY - shapeDef.Height / 2,
                    shapeDef.Width,
                    shapeDef.Height),
                ShapeType.RotatedRectangle => new Rect(
                    shapeDef.CenterX - shapeDef.Width / 2,
                    shapeDef.CenterY - shapeDef.Height / 2,
                    shapeDef.Width,
                    shapeDef.Height),
                ShapeType.Circle => new Rect(
                    shapeDef.CenterX - shapeDef.Radius,
                    shapeDef.CenterY - shapeDef.Radius,
                    shapeDef.Radius * 2,
                    shapeDef.Radius * 2),
                ShapeType.Line => new Rect(
                    Math.Min(shapeDef.StartX, shapeDef.EndX),
                    Math.Min(shapeDef.StartY, shapeDef.EndY),
                    Math.Abs(shapeDef.EndX - shapeDef.StartX),
                    Math.Abs(shapeDef.EndY - shapeDef.StartY)),
                _ => Rect.Empty
            };
        }

        #endregion

        #region 命中测试和选择

        private RegionData? HitTestRegion(Point point)
        {
            foreach (var region in _viewModel.Regions.Reverse())
            {
                if (!region.IsVisible) continue;
                if (IsPointInRegion(point, region))
                    return region;
            }
            return null;
        }

        private bool IsPointInRegion(Point point, RegionData region)
        {
            if (region.Definition is not ShapeDefinition shapeDef)
                return false;

            return shapeDef.ShapeType switch
            {
                ShapeType.Rectangle => point.X >= shapeDef.CenterX - shapeDef.Width / 2 &&
                                       point.X <= shapeDef.CenterX + shapeDef.Width / 2 &&
                                       point.Y >= shapeDef.CenterY - shapeDef.Height / 2 &&
                                       point.Y <= shapeDef.CenterY + shapeDef.Height / 2,
                ShapeType.Circle => Math.Sqrt(
                    Math.Pow(point.X - shapeDef.CenterX, 2) +
                    Math.Pow(point.Y - shapeDef.CenterY, 2)) <= shapeDef.Radius,
                ShapeType.Line => DistanceToLine(point,
                    new Point(shapeDef.StartX, shapeDef.StartY),
                    new Point(shapeDef.EndX, shapeDef.EndY)) < 5,
                _ => false
            };
        }

        private double DistanceToLine(Point point, Point lineStart, Point lineEnd)
        {
            var dx = lineEnd.X - lineStart.X;
            var dy = lineEnd.Y - lineStart.Y;
            var length = Math.Sqrt(dx * dx + dy * dy);

            if (length < 0.001) return Point.Subtract(point, lineStart).Length;

            var t = Math.Max(0, Math.Min(1,
                ((point.X - lineStart.X) * dx + (point.Y - lineStart.Y) * dy) / (length * length)));

            var projection = new Point(lineStart.X + t * dx, lineStart.Y + t * dy);
            return Point.Subtract(point, projection).Length;
        }

        private void SelectRegion(RegionData? region)
        {
            _selectedRegion = region;
            _viewModel.SelectedRegion = region;
            UpdateRegionOverlay();
        }

        private void DeselectAll()
        {
            _selectedRegion = null;
            _viewModel.SelectedRegion = null;
            UpdateRegionOverlay();
        }

        #endregion

        #region ViewModel事件处理

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // 可以根据需要响应ViewModel属性变更
        }

        private void OnRegionChanged(object? sender, RegionChangedEventArgs e)
        {
            UpdateRegionOverlay();
            if (e.Region != null)
            {
                RegionDataChanged?.Invoke(this, e.Region);
            }
        }

        #endregion

        private string GenerateRegionName()
        {
            _regionIndex++;
            return $"区域_{_regionIndex}";
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
