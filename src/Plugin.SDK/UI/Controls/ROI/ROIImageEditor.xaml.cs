using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SunEyeVision.Plugin.SDK.UI.Controls.ROI
{
    /// <summary>
    /// ROI图像编辑器控件 - 使用ImageControl作为图像显示基础
    /// </summary>
    public partial class ROIImageEditor : UserControl, INotifyPropertyChanged
    {
        #region 私有字段

        private readonly List<ROI> _rois = new List<ROI>();
        private readonly EditHistory _editHistory = new EditHistory();

        private ROIMode _currentMode = ROIMode.Inherit;
        private ROITool _currentTool = ROITool.Select;

        private bool _isDrawing;
        private Point _startPoint;
        private ROI? _currentDrawingROI;
        private ROI? _selectedROI;
        private List<ROI> _selectedROIs = new List<ROI>();
        private bool _isDragging;
        private Point _dragStartPoint;

        // 手柄编辑相关
        private HandleType _activeHandle = HandleType.None;
        private Point _handleStartPoint;
        private Rect _originalBounds;
        private Size _originalSize;      // 旋转矩形的原始尺寸
        private Point _originalPosition; // 旋转矩形的原始中心
        private double _originalRotation;
        private double _handleSize = 12;  // 增大手柄大小，提高可点击性
        private readonly List<EditHandle> _editHandles = new List<EditHandle>();

        // ROI编辑层Canvas
        private Canvas ROICanvas;

        #endregion

        #region 属性

        /// <summary>
        /// 当前编辑模式
        /// </summary>
        public ROIMode CurrentMode
        {
            get => _currentMode;
            set
            {
                if (SetProperty(ref _currentMode, value))
                {
                    OnModeChanged();
                }
            }
        }

        /// <summary>
        /// 当前绘制工具
        /// </summary>
        public ROITool CurrentTool
        {
            get => _currentTool;
            set
            {
                if (SetProperty(ref _currentTool, value))
                {
                    OnToolChanged();
                }
            }
        }

        /// <summary>
        /// 是否处于编辑模式
        /// </summary>
        public bool IsEditMode => CurrentMode == ROIMode.Edit;

        /// <summary>
        /// 是否处于绘制模式
        /// </summary>
        public bool IsDrawingMode => CurrentTool == ROITool.Rectangle ||
                                     CurrentTool == ROITool.Circle ||
                                     CurrentTool == ROITool.RotatedRectangle ||
                                     CurrentTool == ROITool.Line;

        /// <summary>
        /// 源图像
        /// </summary>
        public BitmapSource? SourceImage
        {
            get => ImageControl.SourceImage;
            set
            {
                if (ImageControl.SourceImage != value)
                {
                    ImageControl.SourceImage = value;

                    // 同步Canvas尺寸
                    if (value != null)
                    {
                        ROICanvas.Width = value.PixelWidth;
                        ROICanvas.Height = value.PixelHeight;
                    }

                    OnPropertyChanged();
                    UpdateROIOverlay();
                }
            }
        }

        /// <summary>
        /// 缩放比例
        /// </summary>
        public double Zoom => ImageControl.Zoom;

        /// <summary>
        /// ROI集合
        /// </summary>
        public IReadOnlyList<ROI> ROIs => _rois.AsReadOnly();

        /// <summary>
        /// 编辑历史
        /// </summary>
        public EditHistory History => _editHistory;

        /// <summary>
        /// 选中数量
        /// </summary>
        public int SelectedCount => _selectedROIs.Count;

        #endregion

        #region 事件

        /// <summary>
        /// 属性变更事件
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// ROI变更事件
        /// </summary>
        public event EventHandler<ROIChangedEventArgs>? ROIChanged;

        /// <summary>
        /// 选择变更事件
        /// </summary>
        public event EventHandler? SelectionChanged;

        #endregion

        #region 构造函数

        public ROIImageEditor()
        {
            InitializeComponent();
            DataContext = this;

            // 创建ROI编辑层Canvas并添加到ImageControl的OverlayCanvas
            ROICanvas = new Canvas
            {
                IsHitTestVisible = false,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            _editHistory.HistoryChanged += OnHistoryChanged;

            // 绑定键盘快捷键
            KeyDown += OnKeyDown;
            Focusable = true;

            // 订阅ImageControl事件
            ImageControl.ImageMouseMove += ImageControl_ImageMouseMove;
            ImageControl.ViewTransformed += ImageControl_ViewTransformed;
            ImageControl.CanvasMouseLeftButtonDown += ImageControl_CanvasMouseLeftButtonDown;
            ImageControl.CanvasMouseLeftButtonUp += ImageControl_CanvasMouseLeftButtonUp;
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 加载图像
        /// </summary>
        public void LoadImage(BitmapSource image)
        {
            SourceImage = image;
            UpdateStatus("图像已加载");
        }

        /// <summary>
        /// 加载ROI列表
        /// </summary>
        public void LoadROIs(IEnumerable<ROI> rois)
        {
            _rois.Clear();
            _rois.AddRange(rois);
            UpdateROIOverlay();
            UpdateROIcount();
            UpdateStatus($"已加载 {rois.Count()} 个ROI");
        }

        /// <summary>
        /// 获取所有ROI
        /// </summary>
        public IEnumerable<ROI> GetAllROIs()
        {
            return _rois.ToList();
        }

        /// <summary>
        /// 添加ROI
        /// </summary>
        public void AddROI(ROI roi)
        {
            if (!_rois.Contains(roi))
            {
                _rois.Add(roi);
                UpdateROIOverlay();
                UpdateROIcount();
                ROIChanged?.Invoke(this, new ROIChangedEventArgs(ROIChangeType.Added, roi));
            }
        }

        /// <summary>
        /// 移除ROI
        /// </summary>
        public void RemoveROI(Guid roiId)
        {
            var roi = _rois.FirstOrDefault(r => r.ID == roiId);
            if (roi != null)
            {
                _rois.Remove(roi);
                UpdateROIOverlay();
                UpdateROIcount();
                ROIChanged?.Invoke(this, new ROIChangedEventArgs(ROIChangeType.Removed, roi));
            }
        }

        /// <summary>
        /// 获取ROI
        /// </summary>
        public ROI? GetROI(Guid roiId)
        {
            return _rois.FirstOrDefault(r => r.ID == roiId);
        }

        /// <summary>
        /// 清除所有ROI
        /// </summary>
        public void ClearAllROIs()
        {
            _rois.Clear();
            _selectedROIs.Clear();
            UpdateROIOverlay();
            UpdateROIcount();
            ROIChanged?.Invoke(this, new ROIChangedEventArgs(ROIChangeType.Cleared, null));
        }

        /// <summary>
        /// 获取选中的ROI
        /// </summary>
        public IEnumerable<ROI> GetSelectedROIs()
        {
            return _selectedROIs.ToList();
        }

        /// <summary>
        /// 选择ROI
        /// </summary>
        public void SelectROI(ROI roi, bool addToSelection = false)
        {
            if (!addToSelection)
            {
                foreach (var r in _selectedROIs)
                {
                    r.IsSelected = false;
                }
                _selectedROIs.Clear();
            }

            roi.IsSelected = true;
            if (!_selectedROIs.Contains(roi))
            {
                _selectedROIs.Add(roi);
            }

            UpdateROIOverlay();
            SelectionChanged?.Invoke(this, EventArgs.Empty);
            OnPropertyChanged(nameof(SelectedCount));
        }

        /// <summary>
        /// 取消选择
        /// </summary>
        public void DeselectAll()
        {
            foreach (var roi in _selectedROIs)
            {
                roi.IsSelected = false;
            }
            _selectedROIs.Clear();
            _selectedROI = null; // 清除单个选中
            _editHandles.Clear(); // 清除手柄
            UpdateROIOverlay();
            SelectionChanged?.Invoke(this, EventArgs.Empty);
            OnPropertyChanged(nameof(SelectedCount));
        }

        /// <summary>
        /// 刷新可视化
        /// </summary>
        public new void InvalidateVisual()
        {
            UpdateROIOverlay();
        }

        /// <summary>
        /// 屏幕坐标转图像坐标
        /// </summary>
        public Point ScreenToImage(Point screenPoint)
        {
            return ImageControl.ScreenToImage(screenPoint);
        }

        /// <summary>
        /// 图像坐标转屏幕坐标
        /// </summary>
        public Point ImageToScreen(Point imagePoint)
        {
            return ImageControl.ImageToScreen(imagePoint);
        }

        /// <summary>
        /// 适应窗口大小
        /// </summary>
        public void FitToWindow()
        {
            ImageControl.FitToWindow();
        }

        /// <summary>
        /// 显示实际大小
        /// </summary>
        public void ActualSize()
        {
            ImageControl.ActualSize();
        }

        #endregion

        #region 私有方法

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // 将ROICanvas添加到ImageControl的OverlayCanvas（防呆：检查是否已有父级）
            if (ROICanvas.Parent == null)
            {
                ImageControl.OverlayCanvas.Children.Add(ROICanvas);
            }

            UpdateModeButtons();
            UpdateToolButtons();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            // 从ImageControl的OverlayCanvas中移除ROICanvas，防止重复添加
            if (ROICanvas.Parent is Canvas parentCanvas)
            {
                parentCanvas.Children.Remove(ROICanvas);
            }
        }

        private void OnModeChanged()
        {
            UpdateModeButtons();
            UpdateToolButtonsEnabled();
            UpdateModeText();

            if (CurrentMode == ROIMode.Inherit)
            {
                // 切换到继承模式时，取消绘制
                _isDrawing = false;
                _currentDrawingROI = null;
                DeselectAll();
            }
        }

        private void OnToolChanged()
        {
            UpdateToolButtons();
            UpdateStatus($"当前工具: {CurrentTool}");
        }

        private void UpdateModeButtons()
        {
            InheritModeButton.IsChecked = CurrentMode == ROIMode.Inherit;
            EditModeButton.IsChecked = CurrentMode == ROIMode.Edit;
        }

        private void UpdateToolButtons()
        {
            SelectToolButton.IsChecked = CurrentTool == ROITool.Select;
            MultiSelectToolButton.IsChecked = CurrentTool == ROITool.MultiSelect;
            RectangleToolButton.IsChecked = CurrentTool == ROITool.Rectangle;
            CircleToolButton.IsChecked = CurrentTool == ROITool.Circle;
            RotatedRectangleToolButton.IsChecked = CurrentTool == ROITool.RotatedRectangle;
            LineToolButton.IsChecked = CurrentTool == ROITool.Line;
        }

        private void UpdateToolButtonsEnabled()
        {
            var enabled = IsEditMode;
            SelectToolButton.IsEnabled = enabled;
            MultiSelectToolButton.IsEnabled = enabled;
            RectangleToolButton.IsEnabled = enabled;
            CircleToolButton.IsEnabled = enabled;
            RotatedRectangleToolButton.IsEnabled = enabled;
            LineToolButton.IsEnabled = enabled;
        }

        private void UpdateROIOverlay()
        {
            ROICanvas.Children.Clear();

            if (SourceImage == null) return;

            foreach (var roi in _rois)
            {
                var shape = CreateROIShape(roi);
                if (shape != null)
                {
                    ROICanvas.Children.Add(shape);
                }
            }

            // 绘制当前正在创建的ROI
            if (_isDrawing && _currentDrawingROI != null)
            {
                var previewShape = CreateROIShape(_currentDrawingROI, true);
                if (previewShape != null)
                {
                    ROICanvas.Children.Add(previewShape);
                }
            }

            // 绘制选中ROI的编辑手柄
            if (_selectedROI != null && IsEditMode)
            {
                DrawEditHandles(_selectedROI);
            }
        }

        #region 手柄编辑

        private HandleType HitTestHandle(Point point)
        {
            foreach (var handle in _editHandles)
            {
                // 增大容差到 8 像素，提高手柄命中率
                var expandedBounds = new Rect(
                    handle.Bounds.X - 8,
                    handle.Bounds.Y - 8,
                    handle.Bounds.Width + 16,
                    handle.Bounds.Height + 16);
                if (expandedBounds.Contains(point))
                {
                    return handle.Type;
                }
            }
            return HandleType.None;
        }

        private void CreateEditHandles(ROI roi)
        {
            _editHandles.Clear();

            switch (roi.Type)
            {
                case ROIType.Circle:
                    CreateCircleHandles(roi);
                    break;
                case ROIType.RotatedRectangle:
                    CreateRotatedRectangleHandles(roi);
                    break;
                case ROIType.Line:
                    CreateLineHandles(roi);
                    break;
                default:
                    CreateRectangleHandles(roi);
                    break;
            }
        }

        /// <summary>
        /// 创建矩形手柄（8个轴对齐手柄）
        /// </summary>
        private void CreateRectangleHandles(ROI roi)
        {
            var bounds = roi.GetBounds();

            var handles = new[]
            {
                (HandleType.TopLeft, bounds.Left, bounds.Top, Cursors.SizeNWSE),
                (HandleType.TopRight, bounds.Right, bounds.Top, Cursors.SizeNESW),
                (HandleType.BottomLeft, bounds.Left, bounds.Bottom, Cursors.SizeNESW),
                (HandleType.BottomRight, bounds.Right, bounds.Bottom, Cursors.SizeNWSE),
                (HandleType.Top, bounds.Left + bounds.Width / 2, bounds.Top, Cursors.SizeNS),
                (HandleType.Bottom, bounds.Left + bounds.Width / 2, bounds.Bottom, Cursors.SizeNS),
                (HandleType.Left, bounds.Left, bounds.Top + bounds.Height / 2, Cursors.SizeWE),
                (HandleType.Right, bounds.Right, bounds.Top + bounds.Height / 2, Cursors.SizeWE),
            };

            foreach (var (type, x, y, cursor) in handles)
            {
                _editHandles.Add(new EditHandle
                {
                    Type = type,
                    Position = new Point(x, y),
                    Bounds = new Rect(x - _handleSize / 2, y - _handleSize / 2, _handleSize, _handleSize),
                    Cursor = cursor
                });
            }
        }

        /// <summary>
        /// 创建圆形手柄（4个对称半径手柄）
        /// </summary>
        private void CreateCircleHandles(ROI roi)
        {
            var center = roi.Position;
            var radius = roi.Radius;

            // 4个对称点手柄，用于调整半径
            var directions = new[]
            {
                (HandleType.Top, center.X, center.Y - radius),
                (HandleType.Bottom, center.X, center.Y + radius),
                (HandleType.Left, center.X - radius, center.Y),
                (HandleType.Right, center.X + radius, center.Y)
            };

            foreach (var (type, x, y) in directions)
            {
                _editHandles.Add(new EditHandle
                {
                    Type = type,
                    Position = new Point(x, y),
                    Bounds = new Rect(x - _handleSize / 2, y - _handleSize / 2, _handleSize, _handleSize),
                    Cursor = Cursors.SizeAll // 圆形手柄使用统一光标
                });
            }
        }

        /// <summary>
        /// 创建旋转矩形手柄（8个旋转手柄 + 1个旋转手柄）
        /// </summary>
        private void CreateRotatedRectangleHandles(ROI roi)
        {
            var center = roi.Position;
            var w = roi.Size.Width;
            var h = roi.Size.Height;
            var angle = roi.Rotation * Math.PI / 180;
            var cos = Math.Cos(angle);
            var sin = Math.Sin(angle);

            // 8个手柄相对于中心的位置（未旋转）
            var handleOffsets = new (HandleType type, double dx, double dy)[]
            {
                (HandleType.TopLeft, -w/2, -h/2),
                (HandleType.Top, 0, -h/2),
                (HandleType.TopRight, w/2, -h/2),
                (HandleType.Right, w/2, 0),
                (HandleType.BottomRight, w/2, h/2),
                (HandleType.Bottom, 0, h/2),
                (HandleType.BottomLeft, -w/2, h/2),
                (HandleType.Left, -w/2, 0)
            };

            foreach (var (type, dx, dy) in handleOffsets)
            {
                // 旋转变换
                var rotatedX = center.X + dx * cos - dy * sin;
                var rotatedY = center.Y + dx * sin + dy * cos;

                _editHandles.Add(new EditHandle
                {
                    Type = type,
                    Position = new Point(rotatedX, rotatedY),
                    Bounds = new Rect(rotatedX - _handleSize / 2, rotatedY - _handleSize / 2, _handleSize, _handleSize),
                    Cursor = GetCursorForRotatedHandle(type, roi.Rotation)
                });
            }

            // 旋转手柄（在"顶边"中点上方，考虑旋转角度）
            // 顶边中点位置（旋转后）：未旋转时偏移为(0, -h/2)，旋转后为(+h/2*sin, -h/2*cos)
            var topCenterX = center.X + (h/2) * sin;
            var topCenterY = center.Y - (h/2) * cos;
            // 旋转手柄在顶边中点上方20像素（沿法线方向向外）
            var rotateHandleX = topCenterX + 20 * sin;
            var rotateHandleY = topCenterY - 20 * cos;

            _editHandles.Add(new EditHandle
            {
                Type = HandleType.Rotate,
                Position = new Point(rotateHandleX, rotateHandleY),
                Bounds = new Rect(rotateHandleX - _handleSize / 2, rotateHandleY - _handleSize / 2, _handleSize, _handleSize),
                Cursor = Cursors.Hand
            });
        }

        /// <summary>
        /// 创建直线手柄（2个端点手柄）
        /// </summary>
        private void CreateLineHandles(ROI roi)
        {
            // 起点手柄
            _editHandles.Add(new EditHandle
            {
                Type = HandleType.LineStart,
                Position = roi.Position,
                Bounds = new Rect(roi.Position.X - _handleSize / 2, roi.Position.Y - _handleSize / 2, _handleSize, _handleSize),
                Cursor = Cursors.SizeAll
            });

            // 终点手柄
            _editHandles.Add(new EditHandle
            {
                Type = HandleType.LineEnd,
                Position = roi.EndPoint,
                Bounds = new Rect(roi.EndPoint.X - _handleSize / 2, roi.EndPoint.Y - _handleSize / 2, _handleSize, _handleSize),
                Cursor = Cursors.SizeAll
            });
        }

        /// <summary>
        /// 根据旋转角度获取手柄光标
        /// </summary>
        private Cursor GetCursorForRotatedHandle(HandleType handleType, double rotation)
        {
            // 将旋转角度归一化到0-360
            rotation = ((rotation % 360) + 360) % 360;

            // 根据手柄类型和旋转角度计算光标
            // 角落手柄
            if (handleType == HandleType.TopLeft || handleType == HandleType.BottomRight)
            {
                return GetRotatedCursor(rotation, 45, Cursors.SizeNWSE, Cursors.SizeNESW);
            }
            if (handleType == HandleType.TopRight || handleType == HandleType.BottomLeft)
            {
                return GetRotatedCursor(rotation, 45, Cursors.SizeNESW, Cursors.SizeNWSE);
            }

            // 边中点手柄
            if (handleType == HandleType.Top || handleType == HandleType.Bottom)
            {
                return GetRotatedCursor(rotation, 45, Cursors.SizeNS, Cursors.SizeWE);
            }
            if (handleType == HandleType.Left || handleType == HandleType.Right)
            {
                return GetRotatedCursor(rotation, 45, Cursors.SizeWE, Cursors.SizeNS);
            }

            return Cursors.SizeAll;
        }

        /// <summary>
        /// 获取旋转后的光标
        /// </summary>
        private Cursor GetRotatedCursor(double rotation, double threshold, Cursor cursor1, Cursor cursor2)
        {
            // 简化处理：根据旋转角度切换光标
            var normalizedRotation = ((rotation % 90) + 90) % 90;
            return normalizedRotation < threshold || normalizedRotation > (90 - threshold) ? cursor1 : cursor2;
        }

        private void DrawEditHandles(ROI roi)
        {
            CreateEditHandles(roi);

            foreach (var handle in _editHandles)
            {
                // 旋转手柄使用圆形，其他使用方形
                Shape handleShape;
                if (handle.Type == HandleType.Rotate)
                {
                    handleShape = new Ellipse
                    {
                        Width = _handleSize,
                        Height = _handleSize,
                        Fill = Brushes.LightGreen,
                        Stroke = Brushes.Green,
                        StrokeThickness = 1.5
                    };
                }
                else if (roi.Type == ROIType.Circle)
                {
                    // 圆形手柄使用圆形形状
                    handleShape = new Ellipse
                    {
                        Width = _handleSize,
                        Height = _handleSize,
                        Fill = Brushes.White,
                        Stroke = Brushes.Blue,
                        StrokeThickness = 1.5
                    };
                }
                else if (roi.Type == ROIType.Line)
                {
                    // 直线端点使用圆形
                    handleShape = new Ellipse
                    {
                        Width = _handleSize,
                        Height = _handleSize,
                        Fill = Brushes.White,
                        Stroke = Brushes.Blue,
                        StrokeThickness = 1.5
                    };
                }
                else
                {
                    // 矩形和旋转矩形使用方形手柄
                    handleShape = new Rectangle
                    {
                        Width = _handleSize,
                        Height = _handleSize,
                        Fill = Brushes.White,
                        Stroke = Brushes.Blue,
                        StrokeThickness = 1.5
                    };
                }

                Canvas.SetLeft(handleShape, handle.Position.X - _handleSize / 2);
                Canvas.SetTop(handleShape, handle.Position.Y - _handleSize / 2);
                ROICanvas.Children.Add(handleShape);
            }

            // 绘制旋转手柄连接线
            if (roi.Type == ROIType.RotatedRectangle)
            {
                DrawRotateHandleLine(roi);
            }
        }

        /// <summary>
        /// 绘制旋转手柄连接线
        /// </summary>
        private void DrawRotateHandleLine(ROI roi)
        {
            var center = roi.Position;
            var h = roi.Size.Height;
            var angle = roi.Rotation * Math.PI / 180;
            var sin = Math.Sin(angle);
            var cos = Math.Cos(angle);

            // 顶边中点位置（旋转后）
            var topCenterX = center.X + (h / 2) * sin;
            var topCenterY = center.Y - (h / 2) * cos;

            // 旋转手柄位置
            var rotateHandleX = topCenterX + 20 * sin;
            var rotateHandleY = topCenterY - 20 * cos;

            // 绘制连接线
            var line = new Line
            {
                X1 = topCenterX,
                Y1 = topCenterY,
                X2 = rotateHandleX,
                Y2 = rotateHandleY,
                Stroke = Brushes.Green,
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection { 3, 2 }
            };
            ROICanvas.Children.Add(line);
        }

        private void HandleResize(ROI roi, Point currentPosition)
        {
            var delta = currentPosition - _handleStartPoint;

            // 直线端点调整
            if (roi.Type == ROIType.Line)
            {
                HandleLineResize(roi, currentPosition);
                return;
            }

            // 圆形半径调整
            if (roi.Type == ROIType.Circle)
            {
                HandleCircleResize(roi, currentPosition);
                return;
            }

            // 旋转矩形调整
            if (roi.Type == ROIType.RotatedRectangle)
            {
                HandleRotatedRectangleResize(roi, currentPosition, delta);
                return;
            }

            // 普通矩形调整
            switch (_activeHandle)
            {
                case HandleType.TopLeft:
                    ResizeFromCorner(roi, delta, true, true);
                    break;
                case HandleType.TopRight:
                    ResizeFromCorner(roi, delta, false, true);
                    break;
                case HandleType.BottomLeft:
                    ResizeFromCorner(roi, delta, true, false);
                    break;
                case HandleType.BottomRight:
                    ResizeFromCorner(roi, delta, false, false);
                    break;
                case HandleType.Top:
                    ResizeFromEdge(roi, delta, true, true);
                    break;
                case HandleType.Bottom:
                    ResizeFromEdge(roi, delta, true, false);
                    break;
                case HandleType.Left:
                    ResizeFromEdge(roi, delta, false, true);
                    break;
                case HandleType.Right:
                    ResizeFromEdge(roi, delta, false, false);
                    break;
                case HandleType.Rotate:
                    HandleRotate(roi, currentPosition);
                    break;
            }
        }

        /// <summary>
        /// 处理圆形半径调整
        /// </summary>
        private void HandleCircleResize(ROI roi, Point currentPosition)
        {
            // 计算鼠标到圆心的距离作为新半径
            var center = new Point(
                _originalBounds.Left + _originalBounds.Width / 2,
                _originalBounds.Top + _originalBounds.Height / 2);

            var dx = currentPosition.X - center.X;
            var dy = currentPosition.Y - center.Y;
            var newRadius = Math.Sqrt(dx * dx + dy * dy);

            roi.Radius = Math.Max(5, newRadius);
            roi.Position = center;
        }

        /// <summary>
        /// 处理旋转矩形调整（锚点模式：固定对角，被拖动句柄跟随鼠标）
        /// </summary>
        private void HandleRotatedRectangleResize(ROI roi, Point currentPosition, Vector delta)
        {
            if (_activeHandle == HandleType.Rotate)
            {
                HandleRotate(roi, currentPosition);
                return;
            }

            var angle = _originalRotation * Math.PI / 180;
            var cos = Math.Cos(-angle);
            var sin = Math.Sin(-angle);

            // 将 delta 转换到未旋转的本地坐标系
            var localDelta = new Vector(
                delta.X * cos - delta.Y * sin,
                delta.X * sin + delta.Y * cos);

            // 计算原始矩形的四个角点（本地坐标，原点在中心）
            var corners = GetLocalCorners(_originalSize.Width, _originalSize.Height);

            // 根据拖动的句柄计算新的角点位置
            var newCorners = CalculateNewCorners(corners, localDelta, _activeHandle);

            // 从新角点计算新的尺寸和中心
            var (newWidth, newHeight, newCenterLocal) = CalculateFromCorners(newCorners);

            // 将新中心从本地坐标转换回世界坐标
            var cosAngle = Math.Cos(angle);
            var sinAngle = Math.Sin(angle);
            var newCenterWorld = new Point(
                _originalPosition.X + newCenterLocal.X * cosAngle - newCenterLocal.Y * sinAngle,
                _originalPosition.Y + newCenterLocal.X * sinAngle + newCenterLocal.Y * cosAngle);

            roi.Size = new Size(newWidth, newHeight);
            roi.Position = newCenterWorld;
        }

        /// <summary>
        /// 获取矩形四个角点（本地坐标，原点在中心）
        /// </summary>
        private Point[] GetLocalCorners(double width, double height)
        {
            var hw = width / 2;
            var hh = height / 2;
            return new Point[]
            {
                new Point(-hw, -hh), // TopLeft
                new Point( hw, -hh), // TopRight
                new Point( hw,  hh), // BottomRight
                new Point(-hw,  hh)  // BottomLeft
            };
        }

        /// <summary>
        /// 根据拖动的句柄计算新的角点位置（固定对角锚点模式）
        /// </summary>
        private Point[] CalculateNewCorners(Point[] originalCorners, Vector delta, HandleType handle)
        {
            var newCorners = (Point[])originalCorners.Clone();

            switch (handle)
            {
                case HandleType.TopLeft:
                    // TopLeft 移动，BottomRight 固定
                    newCorners[0] = new Point(
                        originalCorners[0].X + delta.X,
                        originalCorners[0].Y + delta.Y);
                    // TopRight 和 BottomLeft 跟随变化（保持矩形形状）
                    newCorners[1] = new Point(originalCorners[1].X, newCorners[0].Y);
                    newCorners[3] = new Point(newCorners[0].X, originalCorners[3].Y);
                    break;

                case HandleType.TopRight:
                    // TopRight 移动，BottomLeft 固定
                    newCorners[1] = new Point(
                        originalCorners[1].X + delta.X,
                        originalCorners[1].Y + delta.Y);
                    newCorners[0] = new Point(originalCorners[0].X, newCorners[1].Y);
                    newCorners[2] = new Point(newCorners[1].X, originalCorners[2].Y);
                    break;

                case HandleType.BottomRight:
                    // BottomRight 移动，TopLeft 固定
                    newCorners[2] = new Point(
                        originalCorners[2].X + delta.X,
                        originalCorners[2].Y + delta.Y);
                    newCorners[1] = new Point(newCorners[2].X, originalCorners[1].Y);
                    newCorners[3] = new Point(originalCorners[3].X, newCorners[2].Y);
                    break;

                case HandleType.BottomLeft:
                    // BottomLeft 移动，TopRight 固定
                    newCorners[3] = new Point(
                        originalCorners[3].X + delta.X,
                        originalCorners[3].Y + delta.Y);
                    newCorners[0] = new Point(newCorners[3].X, originalCorners[0].Y);
                    newCorners[2] = new Point(originalCorners[2].X, newCorners[3].Y);
                    break;

                case HandleType.Top:
                    // 只改变高度，固定 Bottom 边
                    var newTop = originalCorners[0].Y + delta.Y;
                    newCorners[0] = new Point(originalCorners[0].X, newTop);
                    newCorners[1] = new Point(originalCorners[1].X, newTop);
                    break;

                case HandleType.Bottom:
                    // 只改变高度，固定 Top 边
                    var newBottom = originalCorners[2].Y + delta.Y;
                    newCorners[2] = new Point(originalCorners[2].X, newBottom);
                    newCorners[3] = new Point(originalCorners[3].X, newBottom);
                    break;

                case HandleType.Left:
                    // 只改变宽度，固定 Right 边
                    var newLeft = originalCorners[0].X + delta.X;
                    newCorners[0] = new Point(newLeft, originalCorners[0].Y);
                    newCorners[3] = new Point(newLeft, originalCorners[3].Y);
                    break;

                case HandleType.Right:
                    // 只改变宽度，固定 Left 边
                    var newRight = originalCorners[1].X + delta.X;
                    newCorners[1] = new Point(newRight, originalCorners[1].Y);
                    newCorners[2] = new Point(newRight, originalCorners[2].Y);
                    break;
            }

            return newCorners;
        }

        /// <summary>
        /// 从四个角点计算宽度、高度和中心（本地坐标）
        /// </summary>
        private (double width, double height, Point center) CalculateFromCorners(Point[] corners)
        {
            var width = corners[1].X - corners[0].X;
            var height = corners[2].Y - corners[0].Y;

            // 确保最小尺寸，取绝对值
            width = Math.Max(10, Math.Abs(width));
            height = Math.Max(10, Math.Abs(height));

            var center = new Point(
                (corners[0].X + corners[2].X) / 2,
                (corners[0].Y + corners[2].Y) / 2);

            return (width, height, center);
        }

        /// <summary>
        /// 处理直线端点调整
        /// </summary>
        private void HandleLineResize(ROI roi, Point currentPosition)
        {
            switch (_activeHandle)
            {
                case HandleType.LineStart:
                    roi.Position = currentPosition;
                    break;
                case HandleType.LineEnd:
                    roi.EndPoint = currentPosition;
                    break;
            }
        }

        private void ResizeFromCorner(ROI roi, Vector delta, bool isLeft, bool isTop)
        {
            var newWidth = isLeft
                ? _originalBounds.Width - delta.X
                : _originalBounds.Width + delta.X;
            var newHeight = isTop
                ? _originalBounds.Height - delta.Y
                : _originalBounds.Height + delta.Y;

            if (newWidth < 10) newWidth = 10;
            if (newHeight < 10) newHeight = 10;

            var centerX = isLeft
                ? _originalBounds.Right - newWidth / 2
                : _originalBounds.Left + newWidth / 2;
            var centerY = isTop
                ? _originalBounds.Bottom - newHeight / 2
                : _originalBounds.Top + newHeight / 2;

            if (roi.Type == ROIType.Circle)
            {
                roi.Radius = Math.Max(newWidth, newHeight) / 2;
                roi.Position = new Point(
                    _originalBounds.Left + _originalBounds.Width / 2 + delta.X / 2,
                    _originalBounds.Top + _originalBounds.Height / 2 + delta.Y / 2);
            }
            else
            {
                roi.Size = new Size(newWidth, newHeight);
                roi.Position = new Point(centerX, centerY);
            }
        }

        private void ResizeFromEdge(ROI roi, Vector delta, bool isVertical, bool isTopOrLeft)
        {
            if (isVertical)
            {
                var newHeight = isTopOrLeft
                    ? _originalBounds.Height - delta.Y
                    : _originalBounds.Height + delta.Y;
                if (newHeight < 10) newHeight = 10;

                var centerY = isTopOrLeft
                    ? _originalBounds.Bottom - newHeight / 2
                    : _originalBounds.Top + newHeight / 2;

                roi.Size = new Size(roi.Size.Width, newHeight);
                roi.Position = new Point(roi.Position.X, centerY);
            }
            else
            {
                var newWidth = isTopOrLeft
                    ? _originalBounds.Width - delta.X
                    : _originalBounds.Width + delta.X;
                if (newWidth < 10) newWidth = 10;

                var centerX = isTopOrLeft
                    ? _originalBounds.Right - newWidth / 2
                    : _originalBounds.Left + newWidth / 2;

                roi.Size = new Size(newWidth, roi.Size.Height);
                roi.Position = new Point(centerX, roi.Position.Y);
            }
        }

        private void HandleRotate(ROI roi, Point currentPosition)
        {
            var center = roi.Position;
            var angle = Math.Atan2(currentPosition.Y - center.Y, currentPosition.X - center.X) * 180 / Math.PI;
            roi.Rotation = angle + 90; // 调整角度使手柄在顶部
        }

        #endregion

        private Shape? CreateROIShape(ROI roi, bool isPreview = false)
        {
            Shape? shape = null;

            var fillColor = isPreview
                ? new SolidColorBrush(Color.FromArgb(30, 0, 120, 215))
                : new SolidColorBrush(roi.FillColor) { Opacity = roi.Opacity };

            var strokeColor = new SolidColorBrush(roi.IsSelected ? Colors.Blue : roi.StrokeColor);
            var strokeThickness = roi.IsSelected ? 3 : roi.StrokeThickness;

            switch (roi.Type)
            {
                case ROIType.Rectangle:
                    shape = new Rectangle
                    {
                        Width = roi.Size.Width,
                        Height = roi.Size.Height,
                        Fill = fillColor,
                        Stroke = strokeColor,
                        StrokeThickness = strokeThickness,
                        StrokeDashArray = isPreview ? new DoubleCollection { 4, 2 } : new DoubleCollection()
                    };
                    Canvas.SetLeft(shape, roi.Position.X - roi.Size.Width / 2);
                    Canvas.SetTop(shape, roi.Position.Y - roi.Size.Height / 2);
                    break;

                case ROIType.Circle:
                    shape = new Ellipse
                    {
                        Width = roi.Radius * 2,
                        Height = roi.Radius * 2,
                        Fill = fillColor,
                        Stroke = strokeColor,
                        StrokeThickness = strokeThickness,
                        StrokeDashArray = isPreview ? new DoubleCollection { 4, 2 } : new DoubleCollection()
                    };
                    Canvas.SetLeft(shape, roi.Position.X - roi.Radius);
                    Canvas.SetTop(shape, roi.Position.Y - roi.Radius);
                    break;

                case ROIType.RotatedRectangle:
                    var rect = new Rectangle
                    {
                        Width = roi.Size.Width,
                        Height = roi.Size.Height,
                        Fill = fillColor,
                        Stroke = strokeColor,
                        StrokeThickness = strokeThickness,
                        StrokeDashArray = isPreview ? new DoubleCollection { 4, 2 } : new DoubleCollection()
                    };
                    rect.RenderTransform = new RotateTransform(roi.Rotation, roi.Size.Width / 2, roi.Size.Height / 2);
                    Canvas.SetLeft(rect, roi.Position.X - roi.Size.Width / 2);
                    Canvas.SetTop(rect, roi.Position.Y - roi.Size.Height / 2);
                    shape = rect;
                    break;

                case ROIType.Line:
                    var line = new Line
                    {
                        X1 = roi.Position.X,
                        Y1 = roi.Position.Y,
                        X2 = roi.EndPoint.X,
                        Y2 = roi.EndPoint.Y,
                        Stroke = strokeColor,
                        StrokeThickness = strokeThickness,
                        StrokeDashArray = isPreview ? new DoubleCollection { 4, 2 } : new DoubleCollection()
                    };
                    shape = line;
                    break;
            }

            return shape;
        }

        private void UpdateROIcount()
        {
            ROIcountText.Text = $"ROI数量: {_rois.Count}";
        }

        private void UpdateModeText()
        {
            ModeText.Text = $"模式: {(IsEditMode ? "编辑" : "继承")}";
        }

        private void UpdateStatus(string message)
        {
            StatusText.Text = message;
        }

        private ROI? HitTestROI(Point point)
        {
            // 从后往前测试（后绘制的在上面）
            for (int i = _rois.Count - 1; i >= 0; i--)
            {
                if (_rois[i].Contains(point))
                {
                    return _rois[i];
                }
            }
            return null;
        }

        #endregion

        #region 事件处理

        private void ImageControl_ImageMouseMove(object? sender, ImageMouseEventArgs e)
        {
            var position = e.ImagePosition;

            // 手柄编辑模式
            if (_activeHandle != HandleType.None && _selectedROI != null)
            {
                HandleResize(_selectedROI, position);
                UpdateROIOverlay();
                return;
            }

            // 拖动ROI
            if (_isDragging && _selectedROI != null)
            {
                var offset = position - _dragStartPoint;
                _selectedROI.Move(offset);
                _dragStartPoint = position;
                UpdateROIOverlay();
                return;
            }

            // 绘制预览
            if (_isDrawing && _currentDrawingROI != null)
            {
                var dx = position.X - _startPoint.X;
                var dy = position.Y - _startPoint.Y;

                switch (_currentDrawingROI.Type)
                {
                    case ROIType.Rectangle:
                    case ROIType.RotatedRectangle:
                        _currentDrawingROI.Position = new Point(
                            _startPoint.X + dx / 2,
                            _startPoint.Y + dy / 2);
                        _currentDrawingROI.Size = new Size(Math.Abs(dx), Math.Abs(dy));
                        break;

                    case ROIType.Circle:
                        var radius = Math.Sqrt(dx * dx + dy * dy);
                        _currentDrawingROI.Position = _startPoint;
                        _currentDrawingROI.Radius = radius;
                        break;

                    case ROIType.Line:
                        _currentDrawingROI.EndPoint = position;
                        break;
                }

                UpdateROIOverlay();
            }
        }

        private void ImageControl_ViewTransformed(object? sender, ViewTransformEventArgs e)
        {
            // 视图变换时刷新ROI叠加层
            UpdateROIOverlay();
        }

        private void InheritModeButton_Click(object sender, RoutedEventArgs e)
        {
            CurrentMode = ROIMode.Inherit;
        }

        private void EditModeButton_Click(object sender, RoutedEventArgs e)
        {
            CurrentMode = ROIMode.Edit;
        }

        private void ToolButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton button)
            {
                if (button == SelectToolButton)
                    CurrentTool = ROITool.Select;
                else if (button == MultiSelectToolButton)
                    CurrentTool = ROITool.MultiSelect;
                else if (button == RectangleToolButton)
                    CurrentTool = ROITool.Rectangle;
                else if (button == CircleToolButton)
                    CurrentTool = ROITool.Circle;
                else if (button == RotatedRectangleToolButton)
                    CurrentTool = ROITool.RotatedRectangle;
                else if (button == LineToolButton)
                    CurrentTool = ROITool.Line;
            }
        }

        private void ImageControl_CanvasMouseLeftButtonDown(object? sender, ImageMouseEventArgs e)
        {
            if (!IsEditMode) return;

            var position = e.ImagePosition;

            // 绘制模式
            if (IsDrawingMode)
            {
                // 开始绘制
                _isDrawing = true;
                _startPoint = position;
                _currentDrawingROI = new ROI
                {
                    Type = CurrentTool == ROITool.Rectangle ? ROIType.Rectangle :
                           CurrentTool == ROITool.Circle ? ROIType.Circle :
                           CurrentTool == ROITool.RotatedRectangle ? ROIType.RotatedRectangle : ROIType.Line,
                    Position = position,
                    Size = new Size(0, 0),
                    Radius = 0,
                    EndPoint = position,
                    IsEditable = true
                };
                e.Handled = true;
                return;
            }

            // ============ 非绘制模式（选择/多选）============
            // 首先检测是否点击了手柄
            if (_selectedROI != null)
            {
                var hitHandle = HitTestHandle(position);
                if (hitHandle != HandleType.None)
                {
                    _activeHandle = hitHandle;
                    _handleStartPoint = position;
                    _originalBounds = _selectedROI.GetBounds();
                    _originalPosition = _selectedROI.Position;
                    _originalSize = _selectedROI.Size;
                    _originalRotation = _selectedROI.Rotation;
                    UpdateStatus($"手柄编辑: {hitHandle}");
                    e.Handled = true;
                    return;
                }
            }

            var hitROI = HitTestROI(position);
            UpdateStatus(hitROI != null ? $"命中ROI: {hitROI.Type}" : "未命中");

            if (hitROI != null)
            {
                if (CurrentTool == ROITool.MultiSelect && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                {
                    // Ctrl+点击切换选择
                    if (_selectedROIs.Contains(hitROI))
                    {
                        hitROI.IsSelected = false;
                        _selectedROIs.Remove(hitROI);
                        if (_selectedROI == hitROI)
                            _selectedROI = null;
                    }
                    else
                    {
                        SelectROI(hitROI, true);
                    }
                }
                else
                {
                    SelectROI(hitROI);
                    _selectedROI = hitROI;
                    _isDragging = true;
                    _dragStartPoint = position;
                }
                UpdateROIOverlay();
                e.Handled = true;
            }
            else
            {
                // 点击空白区域：取消所有选中状态
                DeselectAll();
            }
        }

        private void ImageControl_CanvasMouseLeftButtonUp(object? sender, ImageMouseEventArgs e)
        {
            // 完成手柄编辑
            if (_activeHandle != HandleType.None && _selectedROI != null)
            {
                _editHistory.AddAction(new ModifyROIAction(_selectedROI, _originalBounds, _originalRotation));
                _activeHandle = HandleType.None;
                UpdateStatus("完成调整");
                return;
            }

            if (_isDragging && _selectedROI != null)
            {
                // 完成拖动，添加到历史
                var position = e.ImagePosition;
                var offset = position - _dragStartPoint;
                if (offset.Length > 1)
                {
                    _editHistory.AddAction(new MoveROIAction(_selectedROI.ID, offset));
                }
                _isDragging = false;
            }
            else if (_isDrawing && _currentDrawingROI != null)
            {
                // 完成绘制
                if (_currentDrawingROI.Size.Width > 5 || _currentDrawingROI.Size.Height > 5 ||
                    _currentDrawingROI.Radius > 5 || _currentDrawingROI.Type == ROIType.Line)
                {
                    _rois.Add(_currentDrawingROI);
                    _editHistory.AddAction(new CreateROIAction(_currentDrawingROI));
                    UpdateROIcount();
                    ROIChanged?.Invoke(this, new ROIChangedEventArgs(ROIChangeType.Added, _currentDrawingROI));
                }

                _isDrawing = false;
                _currentDrawingROI = null;
                UpdateROIOverlay();
            }
        }

        private void DeleteSelectedButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedROIs.Count > 0)
            {
                var action = new BatchDeleteAction(_selectedROIs);
                foreach (var roi in _selectedROIs.ToList())
                {
                    _rois.Remove(roi);
                }
                _editHistory.AddAction(action);
                _selectedROIs.Clear();
                UpdateROIOverlay();
                UpdateROIcount();
                UpdateStatus("已删除选中的ROI");
            }
        }

        private void ClearAllButton_Click(object sender, RoutedEventArgs e)
        {
            if (_rois.Count > 0)
            {
                var action = new ClearAllAction(_rois);
                _editHistory.AddAction(action);
                _rois.Clear();
                _selectedROIs.Clear();
                UpdateROIOverlay();
                UpdateROIcount();
                UpdateStatus("已清除所有ROI");
            }
        }

        private void UndoButton_Click(object sender, RoutedEventArgs e)
        {
            _editHistory.Undo(this);
            UpdateStatus("撤销");
        }

        private void RedoButton_Click(object sender, RoutedEventArgs e)
        {
            _editHistory.Redo(this);
            UpdateStatus("重做");
        }

        private void OnHistoryChanged(object? sender, EventArgs e)
        {
            UndoButton.IsEnabled = _editHistory.CanUndo;
            RedoButton.IsEnabled = _editHistory.CanRedo;
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (e.Key == Key.Z)
                {
                    _editHistory.Undo(this);
                    e.Handled = true;
                }
                else if (e.Key == Key.Y)
                {
                    _editHistory.Redo(this);
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.Delete || e.Key == Key.Back)
            {
                DeleteSelectedButton_Click(sender, e);
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                _isDrawing = false;
                _currentDrawingROI = null;
                DeselectAll();
                UpdateROIOverlay();
                e.Handled = true;
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
    /// ROI变更事件参数
    /// </summary>
    public class ROIChangedEventArgs : EventArgs
    {
        public ROIChangeType ChangeType { get; }
        public ROI? ROI { get; }

        public ROIChangedEventArgs(ROIChangeType changeType, ROI? roi)
        {
            ChangeType = changeType;
            ROI = roi;
        }
    }

    /// <summary>
    /// ROI变更类型
    /// </summary>
    public enum ROIChangeType
    {
        Added,
        Removed,
        Modified,
        Cleared
    }
}
