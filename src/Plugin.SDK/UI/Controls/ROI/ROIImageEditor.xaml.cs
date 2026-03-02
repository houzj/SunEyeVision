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
    /// 交互操作类型
    /// </summary>
    public enum InteractionType
    {
        None,           // 无操作
        Drawing,        // 绘制中
        Dragging,       // 拖动中
        HandleEditing   // 手柄编辑中
    }

    /// <summary>
    /// ROI图像编辑器控件 - 使用ImageControl作为图像显示基础
    /// </summary>
    public partial class ROIImageEditor : UserControl, INotifyPropertyChanged
    {
        #region 私有字段

        private readonly List<ROI> _rois = new List<ROI>();
        private readonly EditHistory _editHistory = new EditHistory();
        private ROIInfoViewModel? _infoViewModel;

        private ROIMode _currentMode = ROIMode.Inherit;
        private ROITool _currentTool = ROITool.Select;

        // 统一的交互状态管理
        private InteractionType _currentInteraction = InteractionType.None;

        // ROI名称索引管理（全局索引 + 优先补充空缺）
        private readonly HashSet<int> _usedIndices = new HashSet<int>();
        private readonly SortedSet<int> _vacantIndices = new SortedSet<int>();
        private int _maxIndex = 0;

        // 编辑器设置
        private ROIEditorSettings _settings = ROIEditorSettings.Default;

        // 保留原有布尔标志以兼容现有代码
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
        
        // 旋转操作专用字段（解决角度跳变问题）
        private double _rotateStartAngle;      // 旋转开始时的ROI角度
        private double _rotateStartMouseAngle; // 旋转开始时鼠标相对于中心的角度

        #endregion

        #region 属性

        /// <summary>
        /// 获取OverlayCanvas（直接使用ImageControl的OverlayCanvas）
        /// </summary>
        private Canvas OverlayCanvas => ImageControl.OverlayCanvas;

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

                    // OverlayCanvas的尺寸由ImageControl自动管理，不需要手动设置

                    // 图像加载后自适应显示
                    if (value != null)
                    {
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            ImageControl.FitToWindow();
                        }), System.Windows.Threading.DispatcherPriority.Loaded);
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

        /// <summary>
        /// 编辑器显示设置
        /// </summary>
        public ROIEditorSettings Settings
        {
            get => _settings;
            set
            {
                if (SetProperty(ref _settings, value))
                {
                    UpdateROIOverlay();
                }
            }
        }

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

            // 初始化信息面板视图模型
            _infoViewModel = new ROIInfoViewModel(this);
            InfoPanel.ViewModel = _infoViewModel;

            // 直接使用ImageControl.OverlayCanvas，不再创建独立的ROICanvas

            _editHistory.HistoryChanged += OnHistoryChanged;

            // 绑定键盘快捷键
            KeyDown += OnKeyDown;
            Focusable = true;

            // 订阅ImageControl事件
            ImageControl.ImageMouseMove += ImageControl_ImageMouseMove;
            ImageControl.ViewTransformed += ImageControl_ViewTransformed;
            ImageControl.CanvasMouseLeftButtonDown += ImageControl_CanvasMouseLeftButtonDown;
            ImageControl.CanvasMouseLeftButtonUp += ImageControl_CanvasMouseLeftButtonUp;

            // 添加自己的鼠标事件处理（当CaptureMouse时ImageControl不会收到事件）
            MouseMove += ROIImageEditor_MouseMove;
            MouseLeftButtonUp += ROIImageEditor_MouseLeftButtonUp;

            // 添加鼠标离开处理作为安全机制
            MouseLeave += ROIImageEditor_MouseLeave;

            // 添加全局鼠标捕获丢失处理
            LostMouseCapture += ROIImageEditor_LostMouseCapture;
        }

        /// <summary>
        /// 鼠标离开控件时的处理（简化版 - CaptureMouse机制已覆盖大部分场景）
        /// </summary>
        private void ROIImageEditor_MouseLeave(object sender, MouseEventArgs e)
        {
            // 由于使用了 CaptureMouse 机制，鼠标离开控件时仍能收到 MouseUp 事件
            // 这里只处理非交互状态下的清理，作为额外的安全措施
            // 不需要额外处理，LostMouseCapture 会处理异常中断
        }

        /// <summary>
        /// 鼠标移动处理（当CaptureMouse时使用此事件）
        /// </summary>
        private void ROIImageEditor_MouseMove(object sender, MouseEventArgs e)
        {
            // 只在捕获鼠标时处理（交互状态）
            if (_currentInteraction == InteractionType.None)
                return;

            var screenPoint = e.GetPosition(ImageControl.MainCanvas);
            var position = ImageControl.ScreenToImage(screenPoint);

            // 手柄编辑模式
            if (_currentInteraction == InteractionType.HandleEditing && _activeHandle != HandleType.None && _selectedROI != null)
            {
                HandleResize(_selectedROI, position);
                UpdateROIOverlay();
                // 通知ROI尺寸变化，用于更新信息面板
                ROIChanged?.Invoke(this, new ROIChangedEventArgs(ROIChangeType.Modified, _selectedROI));
                return;
            }

            // 拖动ROI
            if (_currentInteraction == InteractionType.Dragging && _isDragging && _selectedROI != null)
            {
                var offset = position - _dragStartPoint;

                var tempROI = (ROI)_selectedROI.Clone();
                tempROI.Move(offset);

                if (IsROIInImageBounds(tempROI, SourceImage))
                {
                    _selectedROI.Move(offset);
                    _dragStartPoint = position;
                    UpdateROIOverlay();
                    // 通知ROI位置变化，用于更新信息面板
                    ROIChanged?.Invoke(this, new ROIChangedEventArgs(ROIChangeType.Modified, _selectedROI));
                }
                else
                {
                    UpdateStatus("无法拖动到图像边界外");
                }
                return;
            }

            // 绘制预览
            if (_currentInteraction == InteractionType.Drawing && _isDrawing && _currentDrawingROI != null)
            {
                var dx = position.X - _startPoint.X;
                var dy = position.Y - _startPoint.Y;

                var endPoint = new Point(_startPoint.X + dx, _startPoint.Y + dy);
                if (IsPointInImageBounds(endPoint, SourceImage))
                {
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
                else
                {
                    UpdateStatus("绘制超出图像范围");
                }
            }
        }

        /// <summary>
        /// 鼠标左键释放处理（当CaptureMouse时使用此事件）
        /// </summary>
        private void ROIImageEditor_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // 只在捕获鼠标时处理
            if (_currentInteraction == InteractionType.None)
                return;

            var screenPoint = e.GetPosition(ImageControl.MainCanvas);
            var position = ImageControl.ScreenToImage(screenPoint);

            // 完成手柄编辑
            if (_currentInteraction == InteractionType.HandleEditing && _activeHandle != HandleType.None)
            {
                if (_selectedROI != null)
                {
                    if (!IsROIInImageBounds(_selectedROI, SourceImage))
                    {
                        _selectedROI.Position = _originalPosition;
                        _selectedROI.Size = _originalSize;
                        _selectedROI.Rotation = _originalRotation;
                        UpdateStatus("调整超出图像边界，已回滚");
                    }
                    else
                    {
                        _editHistory.AddAction(new ModifyROIAction(_selectedROI, _originalBounds, _originalRotation));
                        UpdateStatus("完成调整");
                    }
                }
            }
            // 完成拖动
            else if (_currentInteraction == InteractionType.Dragging && _isDragging)
            {
                if (_selectedROI != null)
                {
                    var offset = position - _dragStartPoint;
                    if (offset.Length > 1)
                    {
                        _editHistory.AddAction(new MoveROIAction(_selectedROI.ID, offset));
                    }
                }
                UpdateStatus("完成拖动");
            }
            // 完成绘制
            else if (_currentInteraction == InteractionType.Drawing && _isDrawing)
            {
                if (_currentDrawingROI != null)
                {
                    var roi = _currentDrawingROI;
                    bool isWithinBounds = IsROIInImageBounds(roi, SourceImage);

                    if (isWithinBounds &&
                        (roi.Size.Width > 5 || roi.Size.Height > 5 ||
                         roi.Radius > 5 || roi.Type == ROIType.Line))
                    {
                        _rois.Add(_currentDrawingROI);
                        _editHistory.AddAction(new CreateROIAction(_currentDrawingROI));
                        UpdateROIcount();
                        ROIChanged?.Invoke(this, new ROIChangedEventArgs(ROIChangeType.Added, _currentDrawingROI));
                        UpdateStatus("创建ROI成功");
                    }
                    else
                    {
                        UpdateStatus(isWithinBounds ? "ROI太小，已忽略" : "ROI超出图像范围，已忽略");
                    }
                }
                else
                {
                    UpdateStatus("绘制取消");
                }
            }

            // 统一结束交互
            EndInteraction();
            UpdateROIOverlay();
        }

        /// <summary>
        /// 鼠标捕获丢失时的处理（关键安全后备）
        /// </summary>
        private void ROIImageEditor_LostMouseCapture(object sender, MouseEventArgs e)
        {
            // 如果是正常的操作结束，_currentInteraction 已经是 None
            // 这里处理的是异常情况（如窗口切换、Alt+Tab等）

            if (_currentInteraction == InteractionType.HandleEditing && _selectedROI != null)
            {
                // 手柄编辑被中断，回滚到原始状态
                _selectedROI.Position = _originalPosition;
                _selectedROI.Size = _originalSize;
                _selectedROI.Rotation = _originalRotation;
                UpdateStatus("操作被中断，已回滚");
            }

            // 统一重置所有状态
            EndInteraction();
            UpdateROIOverlay();
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
            UpdateROICountersFromExisting(); // 更新索引计数器
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
                // 释放索引
                if (TryParseROIIndex(roi.Name, out int index))
                {
                    ReleaseIndex(index);
                }

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

            // 重置索引状态
            _usedIndices.Clear();
            _vacantIndices.Clear();
            _maxIndex = 0;

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
                _selectedROI = roi; // 单选模式：设置当前选中ROI
            }
            else
            {
                // 多选模式：只有在只有一个ROI被选中时才设置 _selectedROI
                // 多个ROI选中时不支持同时编辑手柄
                if (_selectedROIs.Count == 0)
                {
                    _selectedROI = roi;
                }
                else
                {
                    _selectedROI = null; // 多选时清除单个选中
                }
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
            ImageControl.Zoom = 1.0;
            ImageControl.CenterImage();
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 生成ROI名称（全局索引，优先补充空缺）
        /// </summary>
        private string GenerateROIName()
        {
            int index = AllocateNextIndex();
            return $"区域_{index}";
        }

        /// <summary>
        /// 分配下一个可用的ROI索引（优先填充空缺）
        /// </summary>
        private int AllocateNextIndex()
        {
            int index;

            if (_vacantIndices.Count > 0)
            {
                // 优先从空缺池取最小索引
                index = _vacantIndices.Min;
                _vacantIndices.Remove(index);
            }
            else
            {
                // 无空缺，分配新索引
                _maxIndex++;
                index = _maxIndex;
            }

            _usedIndices.Add(index);
            return index;
        }

        /// <summary>
        /// 释放索引到空缺池
        /// </summary>
        private void ReleaseIndex(int index)
        {
            _usedIndices.Remove(index);
            if (index > 0)
            {
                _vacantIndices.Add(index);
            }
        }

        /// <summary>
        /// 从ROI名称解析索引号
        /// </summary>
        private bool TryParseROIIndex(string name, out int index)
        {
            index = 0;
            if (string.IsNullOrEmpty(name)) return false;

            var parts = name.Split('_');
            if (parts.Length == 2 && int.TryParse(parts[1], out index))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// 重置ROI索引计数器
        /// </summary>
        public void ResetROICounters()
        {
            _usedIndices.Clear();
            _vacantIndices.Clear();
            _maxIndex = 0;
        }

        /// <summary>
        /// 根据现有ROI重建索引状态
        /// </summary>
        private void UpdateROICountersFromExisting()
        {
            _usedIndices.Clear();
            _vacantIndices.Clear();
            _maxIndex = 0;

            foreach (var roi in _rois)
            {
                if (TryParseROIIndex(roi.Name, out int index))
                {
                    _usedIndices.Add(index);
                    _maxIndex = Math.Max(_maxIndex, index);
                }
            }

            // 计算空缺索引
            for (int i = 1; i <= _maxIndex; i++)
            {
                if (!_usedIndices.Contains(i))
                {
                    _vacantIndices.Add(i);
                }
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // 启用OverlayCanvas命中测试，让ROI形状可以被选中
            ImageControl.OverlayHitTestVisible = true;

            UpdateModeButtons();
            UpdateToolButtons();

            // 绑定设置到信息面板
            InfoPanel.EditorSettings = Settings;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            // 清除ROI元素（但保留OverlayCanvas本身）
            OverlayCanvas.Children.Clear();

            // 清理信息面板视图模型
            _infoViewModel?.Cleanup();
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

        #region 交互操作管理

        /// <summary>
        /// 开始交互操作
        /// </summary>
        /// <param name="type">交互类型</param>
        /// <returns>是否成功开始</returns>
        private bool BeginInteraction(InteractionType type)
        {
            // 已有操作进行中，不允许开始新操作
            if (_currentInteraction != InteractionType.None)
                return false;

            _currentInteraction = type;

            // 关键：捕获鼠标，确保后续事件不会丢失
            CaptureMouse();

            return true;
        }

        /// <summary>
        /// 结束交互操作
        /// </summary>
        private void EndInteraction()
        {
            if (_currentInteraction == InteractionType.None)
                return;

            _currentInteraction = InteractionType.None;

            // 关键：释放鼠标捕获
            ReleaseMouseCapture();

            // 重置相关状态
            _isDragging = false;
            _isDrawing = false;
            _activeHandle = HandleType.None;
            _currentDrawingROI = null;
        }

        /// <summary>
        /// 检查是否处于交互状态
        /// </summary>
        private bool IsInInteraction => _currentInteraction != InteractionType.None;

        #endregion

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
            OverlayCanvas.Children.Clear();

            if (SourceImage == null) return;

            foreach (var roi in _rois)
            {
                var shape = CreateROIShape(roi);
                if (shape != null)
                {
                    OverlayCanvas.Children.Add(shape);
                }

                // 绘制名称标签
                DrawROILabel(roi);

                // 为旋转矩形显示方向箭头（颜色与边框一致）
                if (roi.Type == ROIType.RotatedRectangle)
                {
                    var arrowColor = roi.IsSelected ? Brushes.Blue : new SolidColorBrush(roi.StrokeColor);
                    DrawDirectionArrow(roi, arrowColor);
                }
            }

            // 绘制当前正在创建的ROI
            if (_isDrawing && _currentDrawingROI != null)
            {
                var previewShape = CreateROIShape(_currentDrawingROI, true);
                if (previewShape != null)
                {
                    OverlayCanvas.Children.Add(previewShape);
                }

                // 预览时也显示名称标签
                if (Settings.ShowLabelOnPreview && !string.IsNullOrEmpty(_currentDrawingROI.Name))
                {
                    DrawROILabel(_currentDrawingROI, true);
                }

                // 预览时也显示方向箭头（预览颜色：红色）
                if (_currentDrawingROI.Type == ROIType.RotatedRectangle)
                {
                    DrawDirectionArrow(_currentDrawingROI, Brushes.Red);
                }
            }

            // 绘制选中ROI的编辑手柄
            if (_selectedROI != null && IsEditMode)
            {
                DrawEditHandles(_selectedROI);
            }
        }

        /// <summary>
        /// 绘制ROI名称标签
        /// </summary>
        private void DrawROILabel(ROI roi, bool isPreview = false)
        {
            if (string.IsNullOrEmpty(roi.Name)) return;

            bool showLabel = isPreview ? Settings.ShowLabelOnPreview : Settings.ShowLabelOnEdit;
            if (!showLabel) return;

            var bounds = roi.GetBounds();
            double labelTop = bounds.Top - Settings.LabelOffset - Settings.LabelFontSize - 4;

            // 确保标签不会超出画布顶部
            if (labelTop < 0) labelTop = bounds.Bottom + Settings.LabelOffset;

            var label = new TextBlock
            {
                Text = roi.Name,
                FontSize = Settings.LabelFontSize,
                FontFamily = new FontFamily(Settings.LabelFontFamily),
                Foreground = new SolidColorBrush(Settings.LabelForeground),
                Background = new SolidColorBrush(Settings.LabelBackground),
                Padding = new Thickness(4, 2, 4, 2),
                Tag = roi.ID
            };

            // 测量文本宽度
            label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            double labelWidth = label.DesiredSize.Width;
            double labelHeight = label.DesiredSize.Height;

            // 标签居中于ROI上方
            double labelLeft = bounds.Left + (bounds.Width - labelWidth) / 2;

            // 确保标签不会超出画布边界
            if (labelLeft < 0) labelLeft = 0;
            if (labelLeft + labelWidth > OverlayCanvas.ActualWidth && OverlayCanvas.ActualWidth > 0)
                labelLeft = OverlayCanvas.ActualWidth - labelWidth;

            Canvas.SetLeft(label, labelLeft);
            Canvas.SetTop(label, labelTop);

            OverlayCanvas.Children.Add(label);
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
        /// 创建旋转矩形手柄（8个缩放手柄 + 1个旋转手柄 + 方向箭头）
        /// </summary>
        private void CreateRotatedRectangleHandles(ROI roi)
        {
            // 使用ROI.GetCorners()获取精确角点
            var corners = roi.GetCorners();
            if (corners.Length != 4) return;

            // 角点顺序：TopLeft, TopRight, BottomRight, BottomLeft
            var handleTypes = new HandleType[]
            {
                HandleType.TopLeft, HandleType.Top, HandleType.TopRight,
                HandleType.Right, HandleType.BottomRight, HandleType.Bottom,
                HandleType.BottomLeft, HandleType.Left
            };

            // 计算各边中点
            var topCenter = new Point(
                (corners[0].X + corners[1].X) / 2,
                (corners[0].Y + corners[1].Y) / 2
            );
            var rightCenter = new Point(
                (corners[1].X + corners[2].X) / 2,
                (corners[1].Y + corners[2].Y) / 2
            );
            var bottomCenter = new Point(
                (corners[2].X + corners[3].X) / 2,
                (corners[2].Y + corners[3].Y) / 2
            );
            var leftCenter = new Point(
                (corners[3].X + corners[0].X) / 2,
                (corners[3].Y + corners[0].Y) / 2
            );

            // 按顺序添加手柄：角点 + 边中点
            var handlePositions = new Point[]
            {
                corners[0],      // TopLeft
                topCenter,       // Top
                corners[1],      // TopRight
                rightCenter,     // Right
                corners[2],      // BottomRight
                bottomCenter,    // Bottom
                corners[3],      // BottomLeft
                leftCenter       // Left
            };

            for (int i = 0; i < 8; i++)
            {
                var pos = handlePositions[i];
                _editHandles.Add(new EditHandle
                {
                    Type = handleTypes[i],
                    Position = pos,
                    Bounds = new Rect(pos.X - _handleSize / 2, pos.Y - _handleSize / 2, _handleSize, _handleSize),
                    Cursor = GetCursorForRotatedHandle(handleTypes[i], roi.Rotation)
                });
            }

            // 获取中心点
            var center = roi.Position;

            // 中心手柄（用于拖动）
            _editHandles.Add(new EditHandle
            {
                Type = HandleType.Center,
                Position = center,
                Bounds = new Rect(center.X - _handleSize / 2, center.Y - _handleSize / 2, _handleSize, _handleSize),
                Cursor = Cursors.SizeAll
            });

            // 旋转手柄（在顶边中点上方）
            // 方向：从中心指向顶边中点的方向（矩形的"上方"方向）
            var direction = topCenter - center;
            var length = direction.Length;
            if (length > 0)
            {
                // 归一化方向向量并延伸20像素
                var unitDir = direction / length;
                var rotateHandlePos = new Point(
                    topCenter.X + unitDir.X * 25,
                    topCenter.Y + unitDir.Y * 25
                );

                _editHandles.Add(new EditHandle
                {
                    Type = HandleType.Rotate,
                    Position = rotateHandlePos,
                    Bounds = new Rect(rotateHandlePos.X - _handleSize / 2, rotateHandlePos.Y - _handleSize / 2, _handleSize, _handleSize),
                    Cursor = Cursors.Hand
                });
            }
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
                OverlayCanvas.Children.Add(handleShape);
            }

            // 绘制旋转手柄连接线
            if (roi.Type == ROIType.RotatedRectangle)
            {
                DrawRotateHandleLine(roi);
            }
        }

        /// <summary>
        /// 绘制方向箭头（独立方法，可用于预览和编辑状态）
        /// </summary>
        /// <param name="roi">旋转矩形ROI</param>
        /// <param name="strokeBrush">箭头颜色</param>
        private void DrawDirectionArrow(ROI roi, Brush strokeBrush)
        {
            if (roi.Type != ROIType.RotatedRectangle) return;

            var arrow = roi.GetDirectionArrow();
            var center = arrow.Start;
            var topCenter = arrow.End;

            // 检查尺寸是否足够绘制箭头
            if (center.X == topCenter.X && center.Y == topCenter.Y) return;

            // 绘制主箭头线（从中心到顶边中点）
            var arrowLine = new Line
            {
                X1 = center.X,
                Y1 = center.Y,
                X2 = topCenter.X,
                Y2 = topCenter.Y,
                Stroke = strokeBrush,
                StrokeThickness = 2
            };
            OverlayCanvas.Children.Add(arrowLine);

            // 计算箭头方向
            var dx = topCenter.X - center.X;
            var dy = topCenter.Y - center.Y;
            var length = Math.Sqrt(dx * dx + dy * dy);
            if (length < 5) return;

            // 归一化方向向量
            var ux = dx / length;
            var uy = dy / length;

            // 箭头参数
            var arrowSize = 10;
            var arrowAngle = 25 * Math.PI / 180; // 箭头夹角25°

            // 计算箭头两翼的端点
            var cosA = Math.Cos(arrowAngle);
            var sinA = Math.Sin(arrowAngle);

            // 左翼（逆时针旋转）
            var leftX = topCenter.X - arrowSize * (ux * cosA + uy * sinA);
            var leftY = topCenter.Y - arrowSize * (-ux * sinA + uy * cosA);

            // 右翼（顺时针旋转）
            var rightX = topCenter.X - arrowSize * (ux * cosA - uy * sinA);
            var rightY = topCenter.Y - arrowSize * (ux * sinA + uy * cosA);

            // 绘制箭头两翼
            var leftWing = new Line
            {
                X1 = topCenter.X,
                Y1 = topCenter.Y,
                X2 = leftX,
                Y2 = leftY,
                Stroke = strokeBrush,
                StrokeThickness = 2
            };
            OverlayCanvas.Children.Add(leftWing);

            var rightWing = new Line
            {
                X1 = topCenter.X,
                Y1 = topCenter.Y,
                X2 = rightX,
                Y2 = rightY,
                Stroke = strokeBrush,
                StrokeThickness = 2
            };
            OverlayCanvas.Children.Add(rightWing);
        }

        /// <summary>
        /// 绘制旋转矩形的方向箭头和旋转手柄连接线
        /// 箭头从中心指向顶边中点，直观显示矩形的"上方"方向
        /// </summary>
        private void DrawRotateHandleLine(ROI roi)
        {
            // 绘制方向箭头
            DrawDirectionArrow(roi, Brushes.Blue);

            // 绘制从顶边中点到旋转手柄的连接线（虚线）
            var arrow = roi.GetDirectionArrow();
            var center = arrow.Start;
            var topCenter = arrow.End;

            // 计算旋转手柄位置
            var direction = topCenter - center;
            var dirLength = direction.Length;
            if (dirLength > 0)
            {
                var unitDir = direction / dirLength;
                var rotateHandlePos = new Point(
                    topCenter.X + unitDir.X * 25,
                    topCenter.Y + unitDir.Y * 25
                );

                var connectorLine = new Line
                {
                    X1 = topCenter.X,
                    Y1 = topCenter.Y,
                    X2 = rotateHandlePos.X,
                    Y2 = rotateHandlePos.Y,
                    Stroke = Brushes.Blue,
                    StrokeThickness = 1,
                    StrokeDashArray = new DoubleCollection { 3, 2 }
                };
                OverlayCanvas.Children.Add(connectorLine);
            }
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

            // 计算当前鼠标相对于中心的角度（数学坐标系，逆时针为正）
            var currentMouseAngle = Math.Atan2(
                currentPosition.Y - center.Y,
                currentPosition.X - center.X
            ) * 180 / Math.PI;

            // 计算角度增量（当前鼠标角度 - 起始鼠标角度）
            var deltaAngle = currentMouseAngle - _rotateStartMouseAngle;

            // 应用角度增量到原始旋转角度
            // 这样可以避免角度跳变问题，实现连续旋转
            // ROI.Rotation的setter会自动规范化到[-180°, 180°]范围
            roi.Rotation = _rotateStartAngle + deltaAngle;
        }

        #endregion

        private Shape? CreateROIShape(ROI roi, bool isPreview = false)
        {
            Shape? shape = null;

            var fillColor = isPreview
                ? new SolidColorBrush(Color.FromArgb(30, 0, 120, 215))
                : new SolidColorBrush(roi.FillColor) { Opacity = roi.Opacity };

            var strokeColor = new SolidColorBrush(roi.IsSelected ? Colors.Blue : roi.StrokeColor);
            var strokeThickness = roi.IsSelected ? Settings.SelectedStrokeThickness : Settings.DefaultStrokeThickness;

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

        /// <summary>
        /// 创建ROI名称标签
        /// </summary>
        private TextBlock CreateROILabel(ROI roi, double topPosition)
        {
            return new TextBlock
            {
                Text = roi.Name,
                FontSize = Settings.LabelFontSize,
                FontFamily = new FontFamily(Settings.LabelFontFamily),
                Foreground = new SolidColorBrush(Settings.LabelForeground),
                Background = new SolidColorBrush(Settings.LabelBackground),
                Padding = new Thickness(4, 2, 4, 2),
                Tag = roi.ID // 用于标识标签属于哪个ROI
            };
        }

        /// <summary>
        /// 创建带名称标签的ROI容器
        /// </summary>
        private FrameworkElement? CreateROIWithLabel(ROI roi, bool isPreview = false)
        {
            var shape = CreateROIShape(roi, isPreview);
            if (shape == null) return null;

            // 检查是否需要显示标签
            bool showLabel = isPreview ? Settings.ShowLabelOnPreview : Settings.ShowLabelOnEdit;
            if (!showLabel || string.IsNullOrEmpty(roi.Name))
            {
                return shape;
            }

            // 获取ROI的边界
            var bounds = roi.GetBounds();
            double labelTop = bounds.Top - Settings.LabelOffset - Settings.LabelFontSize - 4;
            double labelLeft = bounds.Left + bounds.Width / 2;

            // 创建标签
            var label = CreateROILabel(roi, labelTop);

            // 计算标签位置（居中于ROI上方）
            Canvas.SetLeft(label, labelLeft);
            Canvas.SetTop(label, labelTop);

            // 返回形状，标签单独添加到Canvas
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

        #region 辅助方法

        /// <summary>
        /// 检查点是否在图像范围内
        /// </summary>
        private bool IsPointInImageBounds(Point point, BitmapSource image)
        {
            if (image == null) return false;
            return point.X >= 0 && point.X < image.PixelWidth && 
                   point.Y >= 0 && point.Y < image.PixelHeight;
        }

        /// <summary>
        /// 检查ROI是否在图像范围内（改进版 - 检查整个ROI边界）
        /// </summary>
        private bool IsROIInImageBounds(ROI roi, BitmapSource image)
        {
            if (image == null) return false;
            
            switch (roi.Type)
            {
                case ROIType.Rectangle:
                    var rect = roi.GetBounds();
                    // 检查矩形的四个角都在图像范围内
                    return rect.Left >= 0 && rect.Right <= image.PixelWidth &&
                           rect.Top >= 0 && rect.Bottom <= image.PixelHeight;
                    
                case ROIType.Circle:
                    // 检查圆形的包围盒（圆心±半径）
                    return roi.Position.X - roi.Radius >= 0 && 
                           roi.Position.X + roi.Radius <= image.PixelWidth &&
                           roi.Position.Y - roi.Radius >= 0 && 
                           roi.Position.Y + roi.Radius <= image.PixelHeight;
                    
                case ROIType.RotatedRectangle:
                    // 旋转矩形检查包围盒
                    var bounds = roi.GetBounds();
                    return bounds.Left >= 0 && bounds.Right <= image.PixelWidth &&
                           bounds.Top >= 0 && bounds.Bottom <= image.PixelHeight;
                    
                case ROIType.Line:
                    // 检查直线的两个端点
                    return IsPointInImageBounds(roi.Position, image) && 
                           IsPointInImageBounds(roi.EndPoint, image);
                    
                default:
                    return true;
            }
        }

        #endregion

        #region 事件处理

        private void ImageControl_ImageMouseMove(object? sender, ImageMouseEventArgs e)
        {
            var position = e.ImagePosition;

            // 手柄编辑模式（CaptureMouse机制已保证事件流程的正确性，无需检查鼠标状态）
            if (_currentInteraction == InteractionType.HandleEditing && _activeHandle != HandleType.None && _selectedROI != null)
            {
                HandleResize(_selectedROI, position);
                UpdateROIOverlay();
                // 通知ROI尺寸变化，用于更新信息面板
                ROIChanged?.Invoke(this, new ROIChangedEventArgs(ROIChangeType.Modified, _selectedROI));
                return;
            }

            // 拖动ROI
            if (_currentInteraction == InteractionType.Dragging && _isDragging && _selectedROI != null)
            {
                var offset = position - _dragStartPoint;

                // 创建临时ROI用于边界检查（检查整个ROI是否在边界内）
                var tempROI = (ROI)_selectedROI.Clone();
                tempROI.Move(offset);

                if (IsROIInImageBounds(tempROI, SourceImage))
                {
                    _selectedROI.Move(offset);
                    _dragStartPoint = position;
                    UpdateROIOverlay();
                    // 通知ROI位置变化，用于更新信息面板
                    ROIChanged?.Invoke(this, new ROIChangedEventArgs(ROIChangeType.Modified, _selectedROI));
                }
                else
                {
                    UpdateStatus("无法拖动到图像边界外");
                }
                return;
            }

            // 绘制预览
            if (_currentInteraction == InteractionType.Drawing && _isDrawing && _currentDrawingROI != null)
            {
                var dx = position.X - _startPoint.X;
                var dy = position.Y - _startPoint.Y;

                // 检查终点是否在图像范围内
                var endPoint = new Point(_startPoint.X + dx, _startPoint.Y + dy);
                if (IsPointInImageBounds(endPoint, SourceImage))
                {
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
                else
                {
                    UpdateStatus("绘制超出图像范围");
                }
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
            var position = e.ImagePosition;

            // 继承模式：不处理任何交互，允许ImageControl平移
            if (!IsEditMode) return;

            // 绘制模式
            if (IsDrawingMode)
            {
                // 检查起点是否在图像范围内
                if (!IsPointInImageBounds(position, SourceImage))
                {
                    UpdateStatus("起点超出图像范围，请重新选择");
                    return;
                }
                
                // 开始绘制 - 使用统一入口
                if (!BeginInteraction(InteractionType.Drawing))
                    return;
                
                _isDrawing = true;
                _startPoint = position;
                var roiType = CurrentTool == ROITool.Rectangle ? ROIType.Rectangle :
                       CurrentTool == ROITool.Circle ? ROIType.Circle :
                       CurrentTool == ROITool.RotatedRectangle ? ROIType.RotatedRectangle : ROIType.Line;
                _currentDrawingROI = new ROI
                {
                    Type = roiType,
                    Position = position,
                    Size = new Size(0, 0),
                    Radius = 0,
                    EndPoint = position,
                    IsEditable = true,
                    Name = GenerateROIName() // 自动生成名称
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
                    // 中心手柄特殊处理：触发拖动模式
                    if (hitHandle == HandleType.Center)
                    {
                        // 开始拖动 - 使用统一入口
                        if (!BeginInteraction(InteractionType.Dragging))
                            return;
                        
                        _selectedROI = _selectedROI;
                        _isDragging = true;
                        _dragStartPoint = position;
                        UpdateStatus("拖动ROI");
                        e.Handled = true;
                        return;
                    }
                    
                    // 其他手柄：开始手柄编辑 - 使用统一入口
                    if (!BeginInteraction(InteractionType.HandleEditing))
                        return;
                    
                    _activeHandle = hitHandle;
                    _handleStartPoint = position;
                    _originalBounds = _selectedROI.GetBounds();
                    _originalPosition = _selectedROI.Position;
                    _originalSize = _selectedROI.Size;
                    _originalRotation = _selectedROI.Rotation;
                    
                    // 如果是旋转手柄，记录起始角度（解决角度跳变问题）
                    if (hitHandle == HandleType.Rotate)
                    {
                        _rotateStartAngle = _selectedROI.Rotation;
                        var center = _selectedROI.Position;
                        _rotateStartMouseAngle = Math.Atan2(
                            position.Y - center.Y,
                            position.X - center.X
                        ) * 180 / Math.PI;
                    }
                    
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
                    // Ctrl+点击切换选择（不需要捕获鼠标）
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
                    // 开始拖动 - 使用统一入口
                    if (!BeginInteraction(InteractionType.Dragging))
                        return;
                    
                    SelectROI(hitROI);
                    _selectedROI = hitROI;
                    _isDragging = true;
                    _dragStartPoint = position;
                }
                UpdateROIOverlay();
                // 命中ROI时阻止ImageControl平移
                e.Handled = true;
            }
            else
            {
                // 点击空白区域：取消所有选中状态，不设置e.Handled，允许ImageControl平移图像
                DeselectAll();
            }
        }

        private void ImageControl_CanvasMouseLeftButtonUp(object? sender, ImageMouseEventArgs e)
        {
            // 记录是否需要处理事件
            bool handled = false;

            // 完成手柄编辑
            if (_currentInteraction == InteractionType.HandleEditing && _activeHandle != HandleType.None)
            {
                if (_selectedROI != null)
                {
                    // 检查调整后的ROI是否在图像边界内
                    if (!IsROIInImageBounds(_selectedROI, SourceImage))
                    {
                        // 回滚到原始状态
                        _selectedROI.Position = _originalPosition;
                        _selectedROI.Size = _originalSize;
                        _selectedROI.Rotation = _originalRotation;
                        UpdateStatus("调整超出图像边界，已回滚");
                    }
                    else
                    {
                        _editHistory.AddAction(new ModifyROIAction(_selectedROI, _originalBounds, _originalRotation));
                        UpdateStatus("完成调整");
                    }
                }
                handled = true;
            }
            // 完成拖动
            else if (_currentInteraction == InteractionType.Dragging && _isDragging)
            {
                if (_selectedROI != null)
                {
                    var position = e.ImagePosition;
                    var offset = position - _dragStartPoint;
                    if (offset.Length > 1)
                    {
                        _editHistory.AddAction(new MoveROIAction(_selectedROI.ID, offset));
                    }
                }
                UpdateStatus("完成拖动");
                handled = true;
            }
            // 完成绘制
            else if (_currentInteraction == InteractionType.Drawing && _isDrawing)
            {
                if (_currentDrawingROI != null)
                {
                    // 检查ROI是否完全在图像范围内（使用改进的边界检查）
                    var roi = _currentDrawingROI;
                    bool isWithinBounds = IsROIInImageBounds(roi, SourceImage);

                    if (isWithinBounds && 
                        (roi.Size.Width > 5 || roi.Size.Height > 5 ||
                         roi.Radius > 5 || roi.Type == ROIType.Line))
                    {
                        _rois.Add(_currentDrawingROI);
                        _editHistory.AddAction(new CreateROIAction(_currentDrawingROI));
                        UpdateROIcount();
                        ROIChanged?.Invoke(this, new ROIChangedEventArgs(ROIChangeType.Added, _currentDrawingROI));
                        UpdateStatus("创建ROI成功");
                    }
                    else
                    {
                        UpdateStatus(isWithinBounds ? "ROI太小，已忽略" : "ROI超出图像范围，已忽略");
                    }
                }
                else
                {
                    UpdateStatus("绘制取消");
                }
                handled = true;
            }

            // 统一结束交互（释放鼠标捕获并重置状态）
            EndInteraction();
            UpdateROIOverlay();

            if (handled)
            {
                e.Handled = true;
            }
        }

        private void DeleteSelectedButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedROIs.Count > 0)
            {
                var action = new BatchDeleteAction(_selectedROIs);

                // 释放所有选中ROI的索引
                foreach (var roi in _selectedROIs.ToList())
                {
                    if (TryParseROIIndex(roi.Name, out int index))
                    {
                        ReleaseIndex(index);
                    }
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
