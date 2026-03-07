using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using SunEyeVision.Plugin.SDK.UI.Controls.Region.Models;
using SunEyeVision.Plugin.SDK.UI.Controls.Region.ViewModels;
using SunEyeVision.Plugin.SDK.UI.Controls;
using SunEyeVision.Plugin.SDK.UI.Controls.Region.Logic;

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
        private Button? _activeShapeButton;

        // 绘制状态
        private bool _isDrawing;
        private bool _isDragging;
        private bool _isDraggingHandle;
        private Point _startPoint;
        private RegionData? _currentDrawingRegion;
        private RegionData? _selectedRegion;
        private Point _dragStartPoint;
        private Point _handleDragStartPoint;

        // 旋转操作专用字段（解决角度跳变问题）
        private double _rotateStartAngle;      // 旋转开始时的形状角度
        private double _rotateStartMouseAngle; // 旋转开始时鼠标相对于中心的角度

        // 区域名称索引管理
        private int _regionIndex = 0;

        // 手柄管理
        private HandleManager _handleManager;
        private EditHandle? _activeHandle;
        private RegionEditorSettings _settings;

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
                _handleManager = new HandleManager();
                _settings = RegionEditorSettings.Default;
                return;
            }

            InitializeComponent();
            _viewModel = new RegionEditorViewModel();
            DataContext = _viewModel;
            _handleManager = new HandleManager();
            _settings = RegionEditorSettings.Default;

            // 订阅事件
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            _viewModel.RegionChanged += OnRegionChanged;

            // 启用键盘焦点
            Focusable = true;
            FocusVisualStyle = null;

            // 绑定键盘快捷键
            Loaded += (s, e) => Keyboard.Focus(this);
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
            // 初始化完成，获取键盘焦点
            Keyboard.Focus(this);
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            // 取消订阅 ViewModel 事件
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            _viewModel.RegionChanged -= OnRegionChanged;
            _viewModel.Dispose();

            // 注意：不要调用 ClearMainOverlay() 清空 _mainImageControl
            // 因为控件重新加载时（如 Tab 切换），需要保持 ImageControl 引用
            // 只在显式调用 ClearMainOverlay() 或控件销毁时才清理
        }

        #region 键盘快捷键

        /// <summary>
        /// 处理键盘按键
        /// </summary>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            // Ctrl+Z: 撤销
            if (e.Key == Key.Z && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (_viewModel.CanUndo)
                {
                    _viewModel.Undo();
                    UpdateRegionOverlay();
                    e.Handled = true;
                }
                return;
            }

            // Ctrl+Y: 重做
            if (e.Key == Key.Y && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (_viewModel.CanRedo)
                {
                    _viewModel.Redo();
                    UpdateRegionOverlay();
                    e.Handled = true;
                }
                return;
            }

            // Delete: 删除选中区域
            if (e.Key == Key.Delete && _selectedRegion != null)
            {
                _viewModel.RemoveSelectedRegion();
                _selectedRegion = null;
                UpdateRegionOverlay();
                e.Handled = true;
                return;
            }

            // Escape: 取消当前操作
            if (e.Key == Key.Escape)
            {
                if (_isDrawing)
                {
                    _isDrawing = false;
                    _currentDrawingRegion = null;
                    ReleaseMouseCapture();
                    UpdateRegionOverlay();
                    _viewModel.StatusMessage = "已取消绘制";
                }
                else if (_isDragging || _isDraggingHandle)
                {
                    _isDragging = false;
                    _isDraggingHandle = false;
                    _activeHandle = null;
                    ReleaseMouseCapture();
                    UpdateRegionOverlay();
                    _viewModel.StatusMessage = "已取消编辑";
                }
                else
                {
                    DeselectAll();
                }
                e.Handled = true;
                return;
            }

            // 快捷键选择形状工具
            if (_viewModel.IsDrawingMode)
            {
                switch (e.Key)
                {
                    case Key.V:
                        EnterSelectMode(null);
                        e.Handled = true;
                        break;
                    case Key.R:
                        SelectShapeTool(ShapeType.Rectangle, null);
                        e.Handled = true;
                        break;
                    case Key.C:
                        SelectShapeTool(ShapeType.Circle, null);
                        e.Handled = true;
                        break;
                    case Key.O:
                        SelectShapeTool(ShapeType.RotatedRectangle, null);
                        e.Handled = true;
                        break;
                    case Key.L:
                        SelectShapeTool(ShapeType.Line, null);
                        e.Handled = true;
                        break;
                }
            }
            else
            {
                // 非绘制模式下，V键也可以切换到选择模式
                if (e.Key == Key.V)
                {
                    EnterSelectMode(null);
                    e.Handled = true;
                }
            }
        }

        #endregion

        #region 鼠标事件处理

        private void ImageControl_ImageMouseMove(object? sender, ImageMouseEventArgs e)
        {
            if (OverlayCanvas == null) return;

            var position = e.ImagePosition;

            // 拖动手柄编辑
            if (_isDraggingHandle && _activeHandle != null && _selectedRegion != null)
            {
                var dx = position.X - _handleDragStartPoint.X;
                var dy = position.Y - _handleDragStartPoint.Y;
                UpdateShapeByHandle(_selectedRegion, _activeHandle.Type, dx, dy);
                _handleDragStartPoint = position;
                UpdateRegionOverlay();
                return;
            }

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
            if (!_isDrawing && !_isDragging && !_isDraggingHandle) return;

            var screenPoint = e.GetPosition(_mainImageControl.MainCanvas);
            var position = _mainImageControl.ScreenToImage(screenPoint);

            // 拖动手柄编辑
            if (_isDraggingHandle && _activeHandle != null && _selectedRegion != null)
            {
                var dx = position.X - _handleDragStartPoint.X;
                var dy = position.Y - _handleDragStartPoint.Y;
                UpdateShapeByHandle(_selectedRegion, _activeHandle.Type, dx, dy);
                _handleDragStartPoint = position;
                UpdateRegionOverlay();
            }
            else if (_isDragging && _selectedRegion != null)
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

            // 首先检测是否命中编辑手柄（仅当有选中区域且不在绘制状态时）
            if (_selectedRegion != null && !_isDrawing && !_viewModel.IsDrawing)
            {
                var point2D = new Point2D(position.X, position.Y);
                var hitHandleType = _handleManager.HitTest(point2D);

                if (hitHandleType != HandleType.None)
                {
                    // 找到被命中的手柄
                    _activeHandle = _handleManager.Handles.FirstOrDefault(h => h.Type == hitHandleType);
                    if (_activeHandle != null)
                    {
                        _isDraggingHandle = true;
                        _handleDragStartPoint = position;
                        
                        // 如果是旋转手柄，记录初始状态
                        if (hitHandleType == HandleType.Rotate && _selectedRegion?.Definition is ShapeDefinition shapeDef)
                        {
                            _rotateStartAngle = shapeDef.Angle;
                            var center = new Point(shapeDef.CenterX, shapeDef.CenterY);
                            _rotateStartMouseAngle = Math.Atan2(
                                position.Y - center.Y,
                                position.X - center.X
                            ) * 180 / Math.PI;
                        }
                        
                        CaptureMouse();
                        _viewModel.StatusMessage = $"正在拖动 {hitHandleType} 手柄";
                        e.Handled = true;
                        return;
                    }
                }
            }

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
            _isDraggingHandle = false;
            _activeHandle = null;
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

        /// <summary>
        /// 根据手柄类型更新形状
        /// </summary>
        private void UpdateShapeByHandle(RegionData region, HandleType handleType, double dx, double dy)
        {
            if (region.Definition is not ShapeDefinition shapeDef)
                return;

            switch (shapeDef.ShapeType)
            {
                case ShapeType.Rectangle:
                    UpdateRectangleByHandle(shapeDef, handleType, dx, dy);
                    break;
                case ShapeType.RotatedRectangle:
                    UpdateRotatedRectangleByHandle(shapeDef, handleType, dx, dy);
                    break;
                case ShapeType.Circle:
                    UpdateCircleByHandle(shapeDef, handleType, dx, dy);
                    break;
                case ShapeType.Line:
                    UpdateLineByHandle(shapeDef, handleType, dx, dy);
                    break;
            }

            region.MarkModified();
            RegionDataChanged?.Invoke(this, region);
        }

        /// <summary>
        /// 更新矩形形状（通过手柄）
        /// </summary>
        private void UpdateRectangleByHandle(ShapeDefinition shapeDef, HandleType handleType, double dx, double dy)
        {
            switch (handleType)
            {
                case HandleType.TopLeft:
                    shapeDef.CenterX += dx / 2;
                    shapeDef.CenterY += dy / 2;
                    shapeDef.Width -= dx;
                    shapeDef.Height -= dy;
                    break;
                case HandleType.TopRight:
                    shapeDef.CenterX += dx / 2;
                    shapeDef.CenterY += dy / 2;
                    shapeDef.Width += dx;
                    shapeDef.Height -= dy;
                    break;
                case HandleType.BottomLeft:
                    shapeDef.CenterX += dx / 2;
                    shapeDef.CenterY += dy / 2;
                    shapeDef.Width -= dx;
                    shapeDef.Height += dy;
                    break;
                case HandleType.BottomRight:
                    shapeDef.CenterX += dx / 2;
                    shapeDef.CenterY += dy / 2;
                    shapeDef.Width += dx;
                    shapeDef.Height += dy;
                    break;
                case HandleType.Top:
                    shapeDef.CenterY += dy / 2;
                    shapeDef.Height -= dy;
                    break;
                case HandleType.Bottom:
                    shapeDef.CenterY += dy / 2;
                    shapeDef.Height += dy;
                    break;
                case HandleType.Left:
                    shapeDef.CenterX += dx / 2;
                    shapeDef.Width -= dx;
                    break;
                case HandleType.Right:
                    shapeDef.CenterX += dx / 2;
                    shapeDef.Width += dx;
                    break;
            }

            // 确保尺寸不为负
            if (shapeDef.Width < 5) shapeDef.Width = 5;
            if (shapeDef.Height < 5) shapeDef.Height = 5;
        }

        /// <summary>
        /// 更新旋转矩形形状（通过手柄）
        /// </summary>
        private void UpdateRotatedRectangleByHandle(ShapeDefinition shapeDef, HandleType handleType, double dx, double dy)
        {
            if (handleType == HandleType.Rotate)
            {
                // 使用增量角度计算，避免角度跳变
                var center = new Point(shapeDef.CenterX, shapeDef.CenterY);
                var currentMouseAngle = Math.Atan2(
                    _handleDragStartPoint.Y + dy - center.Y,
                    _handleDragStartPoint.X + dx - center.X
                ) * 180 / Math.PI;
                
                // 计算角度增量
                var deltaAngle = currentMouseAngle - _rotateStartMouseAngle;
                
                // 最终角度 = 起始角度 + 增量
                shapeDef.Angle = NormalizeAngle(_rotateStartAngle + deltaAngle);
            }
            else if (handleType == HandleType.Center)
            {
                // 中心手柄：移动整个形状
                shapeDef.CenterX += dx;
                shapeDef.CenterY += dy;
            }
            else
            {
                // 其他手柄：缩放操作（简化处理，保持角度不变）
                UpdateRectangleByHandle(shapeDef, handleType, dx, dy);
            }
        }

        /// <summary>
        /// 将角度规范化到[-180°, 180°]范围
        /// </summary>
        private static double NormalizeAngle(double angle)
        {
            angle = angle % 360;
            if (angle < 0) angle += 360;
            if (angle > 180) angle -= 360;
            return angle;
        }

        /// <summary>
        /// 更新圆形形状（通过手柄）
        /// </summary>
        private void UpdateCircleByHandle(ShapeDefinition shapeDef, HandleType handleType, double dx, double dy)
        {
            // 圆形手柄用于调整半径
            var delta = Math.Sqrt(dx * dx + dy * dy);

            switch (handleType)
            {
                case HandleType.Top:
                    shapeDef.Radius += dy; // 向上拖动减小半径
                    break;
                case HandleType.Bottom:
                    shapeDef.Radius += dy; // 向下拖动增大半径
                    break;
                case HandleType.Left:
                    shapeDef.Radius += dx; // 向左拖动减小半径
                    break;
                case HandleType.Right:
                    shapeDef.Radius += dx; // 向右拖动增大半径
                    break;
            }

            // 确保半径不为负
            if (shapeDef.Radius < 5) shapeDef.Radius = 5;
        }

        /// <summary>
        /// 更新直线形状（通过手柄）
        /// </summary>
        private void UpdateLineByHandle(ShapeDefinition shapeDef, HandleType handleType, double dx, double dy)
        {
            switch (handleType)
            {
                case HandleType.LineStart:
                    shapeDef.StartX += dx;
                    shapeDef.StartY += dy;
                    break;
                case HandleType.LineEnd:
                    shapeDef.EndX += dx;
                    shapeDef.EndY += dy;
                    break;
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

            // 绘制选中区域的编辑手柄
            if (_selectedRegion != null && !_isDrawing)
            {
                DrawEditHandles(_selectedRegion);
            }
        }

        /// <summary>
        /// 绘制编辑手柄
        /// </summary>
        private void DrawEditHandles(RegionData region)
        {
            if (region.Definition is not ShapeDefinition shapeDef || OverlayCanvas == null)
                return;

            _handleManager.CreateHandles(shapeDef);
            var handles = _handleManager.Handles;

            foreach (var handle in handles)
            {
                // 旋转手柄使用绿色圆形，其他使用白色
                Ellipse ellipse;
                if (handle.Type == HandleType.Rotate)
                {
                    ellipse = new Ellipse
                    {
                        Width = _settings.HandleSize,
                        Height = _settings.HandleSize,
                        Fill = Brushes.LightGreen,
                        Stroke = Brushes.Green,
                        StrokeThickness = 1.5,
                        Tag = handle
                    };
                }
                else
                {
                    ellipse = new Ellipse
                    {
                        Width = _settings.HandleSize,
                        Height = _settings.HandleSize,
                        Fill = Brushes.White,
                        Stroke = new SolidColorBrush(_settings.SelectedColor),
                        StrokeThickness = 1.5,
                        Tag = handle
                    };
                }

                Canvas.SetLeft(ellipse, handle.Position.X - _settings.HandleSize / 2);
                Canvas.SetTop(ellipse, handle.Position.Y - _settings.HandleSize / 2);
                OverlayCanvas.Children.Add(ellipse);
            }

            // 为旋转矩形绘制旋转手柄连接线
            if (shapeDef.ShapeType == ShapeType.RotatedRectangle)
            {
                DrawRotateHandleLine(shapeDef, new SolidColorBrush(_settings.SelectedColor));
            }
        }

        /// <summary>
        /// 绘制旋转矩形的方向箭头
        /// 箭头从中心指向右边中点，表示矩形的"宽度方向"（0°方向）
        /// </summary>
        private void DrawDirectionArrow(ShapeDefinition shapeDef, Brush strokeBrush)
        {
            if (OverlayCanvas == null) return;

            var center = new Point(shapeDef.CenterX, shapeDef.CenterY);
            var w = shapeDef.Width;
            var angleRad = shapeDef.Angle * Math.PI / 180;
            var sin = Math.Sin(angleRad);
            var cos = Math.Cos(angleRad);

            // 右边中点：本地坐标 (w/2, 0)
            // 在屏幕坐标系中应用逆时针旋转变换
            var rightCenterX = center.X + (w / 2) * cos;
            var rightCenterY = center.Y - (w / 2) * sin;
            var rightCenter = new Point(rightCenterX, rightCenterY);

            // 绘制主箭头线
            var arrowLine = new Line
            {
                X1 = center.X,
                Y1 = center.Y,
                X2 = rightCenter.X,
                Y2 = rightCenter.Y,
                Stroke = strokeBrush,
                StrokeThickness = 2
            };
            OverlayCanvas.Children.Add(arrowLine);

            // 计算箭头方向
            var dx = rightCenter.X - center.X;
            var dy = rightCenter.Y - center.Y;
            var length = Math.Sqrt(dx * dx + dy * dy);
            if (length < 5) return;

            // 归一化方向向量
            var ux = dx / length;
            var uy = dy / length;

            // 箭头参数
            var arrowSize = 10;
            var arrowAngle = 25 * Math.PI / 180;
            var cosA = Math.Cos(arrowAngle);
            var sinA = Math.Sin(arrowAngle);

            // 左翼（逆时针旋转）
            var leftX = rightCenter.X - arrowSize * (ux * cosA + uy * sinA);
            var leftY = rightCenter.Y - arrowSize * (-ux * sinA + uy * cosA);

            // 右翼（顺时针旋转）
            var rightX = rightCenter.X - arrowSize * (ux * cosA - uy * sinA);
            var rightY = rightCenter.Y - arrowSize * (ux * sinA + uy * cosA);

            // 绘制箭头两翼
            OverlayCanvas.Children.Add(new Line
            {
                X1 = rightCenter.X, Y1 = rightCenter.Y,
                X2 = leftX, Y2 = leftY,
                Stroke = strokeBrush, StrokeThickness = 2
            });
            OverlayCanvas.Children.Add(new Line
            {
                X1 = rightCenter.X, Y1 = rightCenter.Y,
                X2 = rightX, Y2 = rightY,
                Stroke = strokeBrush, StrokeThickness = 2
            });
        }

        /// <summary>
        /// 绘制旋转手柄连接线（从顶边中点到旋转手柄的虚线）
        /// </summary>
        private void DrawRotateHandleLine(ShapeDefinition shapeDef, Brush strokeBrush)
        {
            if (OverlayCanvas == null) return;

            var center = new Point(shapeDef.CenterX, shapeDef.CenterY);
            var h = shapeDef.Height;
            var angleRad = shapeDef.Angle * Math.PI / 180;
            var sin = Math.Sin(angleRad);
            var cos = Math.Cos(angleRad);

            // 顶边中点：本地坐标 (0, -h/2)
            var topCenterX = center.X + (-h / 2) * sin;
            var topCenterY = center.Y + (-h / 2) * cos;
            var topCenter = new Point(topCenterX, topCenterY);

            // 计算旋转手柄位置（从顶边中点延伸25像素）
            var direction = new Point(topCenter.X - center.X, topCenter.Y - center.Y);
            var length = Math.Sqrt(direction.X * direction.X + direction.Y * direction.Y);
            if (length <= 0) return;

            var unitDir = new Point(direction.X / length, direction.Y / length);
            var rotateHandlePos = new Point(
                topCenter.X + unitDir.X * 25,
                topCenter.Y + unitDir.Y * 25
            );

            // 绘制虚线连接
            var connectorLine = new Line
            {
                X1 = topCenter.X,
                Y1 = topCenter.Y,
                X2 = rotateHandlePos.X,
                Y2 = rotateHandlePos.Y,
                Stroke = strokeBrush,
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection { 3, 2 }
            };
            OverlayCanvas.Children.Add(connectorLine);
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
                ? new SolidColorBrush(_settings.PreviewColor)
                : new SolidColorBrush(color) { Opacity = region.DisplayOpacity };

            var strokeColor = region == _selectedRegion
                ? new SolidColorBrush(_settings.SelectedColor)
                : new SolidColorBrush(color);

            var strokeThickness = region == _selectedRegion
                ? _settings.SelectedBorderThickness
                : _settings.DefaultBorderThickness;

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
                    // 绘制方向箭头
                    DrawDirectionArrow(shapeDef, strokeColor);
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
                ShapeType.RotatedRectangle => IsPointInRotatedRectangle(point, shapeDef),
                ShapeType.Circle => Math.Sqrt(
                    Math.Pow(point.X - shapeDef.CenterX, 2) +
                    Math.Pow(point.Y - shapeDef.CenterY, 2)) <= shapeDef.Radius,
                ShapeType.Line => DistanceToLine(point,
                    new Point(shapeDef.StartX, shapeDef.StartY),
                    new Point(shapeDef.EndX, shapeDef.EndY)) < 5,
                _ => false
            };
        }

        /// <summary>
        /// 判断点是否在旋转矩形内（使用坐标变换法）
        /// </summary>
        private bool IsPointInRotatedRectangle(Point point, ShapeDefinition shapeDef)
        {
            // 1. 将测试点平移到以矩形中心为原点
            double dx = point.X - shapeDef.CenterX;
            double dy = point.Y - shapeDef.CenterY;

            // 2. 逆旋转角度（弧度），将点变换到矩形的本地坐标系
            // 使用数学角度系统：逆时针为正
            double angleRad = -shapeDef.Angle * Math.PI / 180.0;
            double cos = Math.Cos(angleRad);
            double sin = Math.Sin(angleRad);

            // 3. 计算本地坐标（旋转后的坐标）
            double localX = dx * cos - dy * sin;
            double localY = dx * sin + dy * cos;

            // 4. 判断是否在轴对齐矩形内
            double halfW = shapeDef.Width / 2;
            double halfH = shapeDef.Height / 2;

            return Math.Abs(localX) <= halfW && Math.Abs(localY) <= halfH;
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

        #region 形状按钮点击事件

        /// <summary>
        /// 选择按钮点击事件 - 进入选择/编辑模式
        /// </summary>
        private void OnSelectButtonClick(object sender, RoutedEventArgs e)
        {
            EnterSelectMode(sender as Button);
        }

        private void OnRectangleButtonClick(object sender, RoutedEventArgs e)
        {
            SelectShapeTool(ShapeType.Rectangle, sender as Button);
        }

        private void OnCircleButtonClick(object sender, RoutedEventArgs e)
        {
            SelectShapeTool(ShapeType.Circle, sender as Button);
        }

        private void OnRotatedRectangleButtonClick(object sender, RoutedEventArgs e)
        {
            SelectShapeTool(ShapeType.RotatedRectangle, sender as Button);
        }

        private void OnLineButtonClick(object sender, RoutedEventArgs e)
        {
            SelectShapeTool(ShapeType.Line, sender as Button);
        }

        /// <summary>
        /// 进入选择/编辑模式
        /// </summary>
        private void EnterSelectMode(Button? clickedButton)
        {
            // 确保处于绘制模式（启用编辑功能）
            _viewModel.CurrentMode = RegionDefinitionMode.Drawing;
            
            // 退出绘制状态，进入选择状态
            _viewModel.IsDrawing = false;
            _currentTool = ShapeType.Point; // Point 表示选择工具

            // 更新按钮高亮状态
            UpdateShapeButtonHighlight(clickedButton);

            // 更新状态消息
            _viewModel.StatusMessage = "选择/编辑模式 - 点击区域可编辑，滚轮缩放，拖拽平移";
        }

        /// <summary>
        /// 选择形状工具并开始绘制
        /// </summary>
        private void SelectShapeTool(ShapeType shapeType, Button? clickedButton)
        {
            // 设置当前工具
            _currentTool = shapeType;
            _viewModel.CurrentShapeType = shapeType;

            // 切换到绘制模式
            _viewModel.CurrentMode = RegionDefinitionMode.Drawing;
            _viewModel.IsDrawing = true;
            _viewModel.DrawingShapeType = shapeType;

            // 更新按钮高亮状态
            UpdateShapeButtonHighlight(clickedButton);

            // 更新状态消息
            var shapeName = shapeType switch
            {
                ShapeType.Rectangle => "矩形",
                ShapeType.Circle => "圆形",
                ShapeType.RotatedRectangle => "旋转矩形",
                ShapeType.Line => "直线",
                _ => shapeType.ToString()
            };
            _viewModel.StatusMessage = $"已选择 {shapeName} 工具，在图像上拖动绘制";
        }

        /// <summary>
        /// 更新形状按钮高亮状态
        /// </summary>
        private void UpdateShapeButtonHighlight(Button? activeButton)
        {
            // 遍历所有形状按钮并重置样式
            var shapeGroupBox = FindShapeGroupBox();
            if (shapeGroupBox != null)
            {
                var uniformGrid = FindVisualChild<UniformGrid>(shapeGroupBox);
                if (uniformGrid != null)
                {
                    foreach (var child in uniformGrid.Children)
                    {
                        if (child is Button button)
                        {
                            ResetShapeButtonStyle(button);
                        }
                    }
                }
            }

            // 高亮选中按钮
            if (activeButton != null)
            {
                activeButton.Background = new SolidColorBrush(Color.FromRgb(232, 244, 252));
                activeButton.BorderBrush = new SolidColorBrush(Color.FromRgb(33, 150, 243));
                activeButton.BorderThickness = new Thickness(2);

                // 更新图标颜色 - 支持Grid包含Shape或Path
                if (activeButton.Content is Grid grid && grid.Children.Count > 0)
                {
                    var icon = grid.Children[0];
                    if (icon is Shape shape)
                    {
                        shape.Stroke = new SolidColorBrush(Color.FromRgb(25, 118, 210));
                        if (shape.Fill != Brushes.Transparent)
                        {
                            shape.Fill = new SolidColorBrush(Color.FromRgb(25, 118, 210));
                        }
                    }
                    else if (icon is Path path)
                    {
                        path.Stroke = new SolidColorBrush(Color.FromRgb(25, 118, 210));
                        path.Fill = new SolidColorBrush(Color.FromRgb(25, 118, 210));
                    }
                }
            }

            _activeShapeButton = activeButton;
        }

        /// <summary>
        /// 查找形状选择区域
        /// </summary>
        private GroupBox? FindShapeGroupBox()
        {
            return FindVisualChild<GroupBox>(this, g => g.Header?.ToString() == "形状");
        }

        /// <summary>
        /// 查找可视化树中的子元素
        /// </summary>
        private static T? FindVisualChild<T>(DependencyObject parent, Func<T, bool>? condition = null) where T : DependencyObject
        {
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild && (condition == null || condition(typedChild)))
                {
                    return typedChild;
                }

                var result = FindVisualChild(child, condition);
                if (result != null)
                {
                    return result;
                }
            }
            return null;
        }

        /// <summary>
        /// 重置形状按钮样式
        /// </summary>
        private void ResetShapeButtonStyle(Button? button)
        {
            if (button == null) return;

            button.Background = Brushes.Transparent;
            button.BorderBrush = new SolidColorBrush(Color.FromRgb(192, 192, 192));
            button.BorderThickness = new Thickness(1);

            // 重置图标颜色 - 支持Grid包含Shape或Path
            if (button.Content is Grid grid && grid.Children.Count > 0)
            {
                var icon = grid.Children[0];
                if (icon is Shape shape)
                {
                    shape.Stroke = new SolidColorBrush(Color.FromRgb(85, 85, 85));
                    if (shape.Fill != Brushes.Transparent)
                    {
                        shape.Fill = new SolidColorBrush(Color.FromRgb(85, 85, 85));
                    }
                }
                else if (icon is Path path)
                {
                    path.Stroke = new SolidColorBrush(Color.FromRgb(85, 85, 85));
                    path.Fill = new SolidColorBrush(Color.FromRgb(85, 85, 85));
                }
            }
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
