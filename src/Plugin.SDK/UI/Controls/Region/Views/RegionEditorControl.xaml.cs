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
using SunEyeVision.Plugin.SDK.Logging;
using SunEyeVision.Plugin.SDK.UI.Controls.Region.Models;
using SunEyeVision.Plugin.SDK.UI.Controls.Region.ViewModels;
using SunEyeVision.Plugin.SDK.UI.Controls;
using SunEyeVision.Plugin.SDK.UI.Controls.Region.Logic;
using SunEyeVision.Plugin.SDK.UI.Controls.Region.Rendering;
using SunEyeVision.Plugin.SDK.Logging;
using SunEyeVision.Plugin.SDK.Execution.Parameters;

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

        // 同步机制：外部集合跟踪
        private System.Collections.ObjectModel.ObservableCollection<RegionData>? _externalRegions;
        private bool _isUpdatingFromExternal = false;

        // 区域名称索引管理
        private int _regionIndex = 0;

        // 手柄管理
        private HandleManager _handleManager;
        private RegionEditorSettings _settings;

        // ROI编辑器风格的手柄管理
        private Rendering.EditHandle[] _currentHandles = null;
        private Rendering.HandleType _activeHandleType = Rendering.HandleType.None;
        private ShapeParameters? _originalShapeDefinition = null;
        private Point _originalPosition;
        private Size _originalSize;
        private double _originalRotation;
        private double? _rotateStartAngle = null;
        private double? _rotateStartMouseAngle = null;

        // WPF Shape渲染器（对象池 + 增量更新）
        private WpfShapeRenderer? _renderer;

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
                // 记录 Regions DependencyProperty 变更
                if (e.NewValue != e.OldValue)
                {
                    VisionLogger.Instance.Log(LogLevel.Info,
                        $"[OnRegionsChanged] Regions DependencyProperty 变更: OldCount={control._externalRegions?.Count ?? 0}, NewValue={e.NewValue?.GetType().Name ?? "null"}",
                        "RegionEditorControl");
                }

                if (e.NewValue is IEnumerable<RegionData> regions)
                {
                    control.SetRegions(regions);
                    // 订阅外部集合的变更（用于外部 → 内部同步）
                    control.SubscribeToExternalRegions(regions);
                }
                // 取消订阅旧集合
                if (e.OldValue is IEnumerable<RegionData> oldRegions)
                {
                    control.UnsubscribeFromExternalRegions(oldRegions);
                }
            }
        }

        /// <summary>
        /// AvailableDataSources 透传属性（用于内部 RegionSubscribePanel）
        /// </summary>
        public static readonly DependencyProperty AvailableDataSourcesProperty =
            DependencyProperty.Register(nameof(AvailableDataSources),
                typeof(System.Collections.ObjectModel.ObservableCollection<AvailableDataSource>),
                typeof(RegionEditorControl),
                new PropertyMetadata(null));

        public System.Collections.ObjectModel.ObservableCollection<AvailableDataSource>? AvailableDataSources
        {
            get => (System.Collections.ObjectModel.ObservableCollection<AvailableDataSource>?)GetValue(AvailableDataSourcesProperty);
            set => SetValue(AvailableDataSourcesProperty, value);
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
                EnsureViewModel();
                _handleManager = new HandleManager();
                _settings = RegionEditorSettings.Default;
                return;
            }

            InitializeComponent();
            EnsureViewModel();
            _handleManager = new HandleManager();
            _settings = RegionEditorSettings.Default;

            // 启用键盘焦点
            Focusable = true;
            FocusVisualStyle = null;

            // 绑定键盘快捷键
            Loaded += (s, e) => Keyboard.Focus(this);
        }

        /// <summary>
        /// 确保 ViewModel 已创建（懒加载）
        /// </summary>
        private void EnsureViewModel()
        {
            if (_viewModel == null)
            {
                VisionLogger.Instance.Log(LogLevel.Info,
                    "[EnsureViewModel] 创建新的 ViewModel 实例",
                    "RegionEditorControl");

                _viewModel = new RegionEditorViewModel();
                DataContext = _viewModel;

                // 订阅事件
                _viewModel.PropertyChanged += OnViewModelPropertyChanged;
                _viewModel.RegionChanged += OnRegionChanged;

                // 订阅内部 Regions 集合的变更（用于内部 → 外部同步）
                _viewModel.Regions.CollectionChanged += OnInternalRegionsCollectionChanged;

                VisionLogger.Instance.Log(LogLevel.Success,
                    "[EnsureViewModel] ViewModel 创建并初始化完成",
                    "RegionEditorControl");
            }
            else
            {
                VisionLogger.Instance.Log(LogLevel.Info,
                    "[EnsureViewModel] ViewModel 已存在，跳过创建",
                    "RegionEditorControl");
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            VisionLogger.Instance.Log(LogLevel.Info,
                $"[OnLoaded] 控件 Loaded，外部集合类型: {_externalRegions?.GetType().Name ?? "null"}, 外部集合数量: {_externalRegions?.Count ?? 0}",
                "RegionEditorControl");

            try
            {
                // 获取父级 ImageControl
                _mainImageControl = FindVisualParent<ImageControl>(this);

                if (_mainImageControl != null)
                {
                    // 订阅 ImageControl 事件
                    _mainImageControl.ImageMouseMove += ImageControl_ImageMouseMove;
                    _mainImageControl.ViewTransformed += ImageControl_ViewTransformed;
                    _mainImageControl.CanvasMouseLeftButtonDown += ImageControl_CanvasMouseLeftButtonDown;

                    VisionLogger.Instance.Log(LogLevel.Success,
                        $"[OnLoaded] 已订阅 ImageControl 事件",
                        "RegionEditorControl");
                }
                else
                {
                    VisionLogger.Instance.Log(LogLevel.Warning,
                        "[OnLoaded] 未找到父级 ImageControl",
                        "RegionEditorControl");
                }

                // 订阅自身的鼠标事件
                MouseMove += RegionEditorControl_MouseMove;
                MouseLeftButtonUp += RegionEditorControl_MouseLeftButtonUp;
                LostMouseCapture += RegionEditorControl_LostMouseCapture;

                // 检查是否已有 Regions 绑定
                if (_externalRegions == null)
                {
                    var boundRegions = GetValue(RegionsProperty) as IEnumerable<RegionData>;
                    if (boundRegions != null)
                    {
                        VisionLogger.Instance.Log(LogLevel.Info,
                            $"[OnLoaded] 发现 Regions 绑定，尝试订阅: {boundRegions.GetType().Name}",
                            "RegionEditorControl");
                        SubscribeToExternalRegions(boundRegions);
                    }
                }

                // 更新区域叠加层
                UpdateRegionOverlay();

                VisionLogger.Instance.Log(LogLevel.Success,
                    "[OnLoaded] 控件 Loaded 完成",
                    "RegionEditorControl");
            }
            catch (Exception ex)
            {
                VisionLogger.Instance.Log(LogLevel.Error,
                    $"[OnLoaded] Loaded 事件处理失败: {ex.Message}",
                    "RegionEditorControl",
                    ex);
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            VisionLogger.Instance.Log(LogLevel.Info,
                $"[OnUnloaded] 控件 Unloaded，外部集合数量: {_externalRegions?.Count ?? 0}",
                "RegionEditorControl");

            try
            {
                // 取消订阅外部集合
                UnsubscribeFromExternalRegions(_externalRegions);

                // 取消订阅内部集合
                _viewModel.Regions.CollectionChanged -= OnInternalRegionsCollectionChanged;

                // 取消订阅 ViewModel 事件
                _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
                _viewModel.RegionChanged -= OnRegionChanged;

                // 不再 Dispose ViewModel，保留用于下次 Loaded

                // 取消订阅 ImageControl 事件
                if (_mainImageControl != null)
                {
                    _mainImageControl.ImageMouseMove -= ImageControl_ImageMouseMove;
                    _mainImageControl.ViewTransformed -= ImageControl_ViewTransformed;
                    _mainImageControl.CanvasMouseLeftButtonDown -= ImageControl_CanvasMouseLeftButtonDown;
                }

                // 取消自身的鼠标事件订阅
                MouseMove -= RegionEditorControl_MouseMove;
                MouseLeftButtonUp -= RegionEditorControl_MouseLeftButtonUp;
                LostMouseCapture -= RegionEditorControl_LostMouseCapture;

                VisionLogger.Instance.Log(LogLevel.Success,
                    "[OnUnloaded] 控件 Unloaded 完成",
                    "RegionEditorControl");
            }
            catch (Exception ex)
            {
                VisionLogger.Instance.Log(LogLevel.Error,
                    $"[OnUnloaded] Unloaded 事件处理失败: {ex.Message}",
                    "RegionEditorControl",
                    ex);
            }
        }

        /// <summary>
        /// 设置主界面ImageControl引用
        /// </summary>
        public void SetMainImageControl(ImageControl imageControl)
        {
            // 如果是同一个实例，不做任何操作
            if (_mainImageControl == imageControl)
                return;

            // 先取消旧的事件订阅
            if (_mainImageControl != null)
            {
                _mainImageControl.ImageMouseMove -= ImageControl_ImageMouseMove;
                _mainImageControl.ViewTransformed -= ImageControl_ViewTransformed;
                _mainImageControl.CanvasMouseLeftButtonDown -= ImageControl_CanvasMouseLeftButtonDown;
            }

            // 取消自身的鼠标事件订阅
            MouseMove -= RegionEditorControl_MouseMove;
            MouseLeftButtonUp -= RegionEditorControl_MouseLeftButtonUp;
            LostMouseCapture -= RegionEditorControl_LostMouseCapture;

            // 设置新的引用
            _mainImageControl = imageControl;
            
            if (_mainImageControl != null)
            {
                // 启用OverlayCanvas命中测试
                _mainImageControl.OverlayHitTestVisible = true;

                // 订阅ImageControl事件
                _mainImageControl.ImageMouseMove += ImageControl_ImageMouseMove;
                _mainImageControl.ViewTransformed += ImageControl_ViewTransformed;
                _mainImageControl.CanvasMouseLeftButtonDown += ImageControl_CanvasMouseLeftButtonDown;

                // 添加自己的鼠标事件处理（当CaptureMouse时使用）
                MouseMove += RegionEditorControl_MouseMove;
                MouseLeftButtonUp += RegionEditorControl_MouseLeftButtonUp;
                LostMouseCapture += RegionEditorControl_LostMouseCapture;

                // 初始化WPF Shape渲染器
                if (OverlayCanvas != null)
                {
                    _renderer?.Dispose();
                    _renderer = new WpfShapeRenderer(OverlayCanvas);
                }
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

            MouseMove -= RegionEditorControl_MouseMove;
            MouseLeftButtonUp -= RegionEditorControl_MouseLeftButtonUp;
            LostMouseCapture -= RegionEditorControl_LostMouseCapture;

                // 清理覆盖层
                _mainImageControl.OverlayCanvas.Children.Clear();
            }

            // 清理渲染器
            _renderer?.Dispose();
            _renderer = null;

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
            var regionList = regions.ToList();
            VisionLogger.Instance.Log(LogLevel.Info,
                $"[SetRegions] 设置区域数据: RegionCount={regionList.Count}, CollectionType={regions.GetType().Name}",
                "RegionEditorControl");

            try
            {
                _viewModel.Regions.Clear();
                foreach (var region in regionList)
                {
                    _viewModel.Regions.Add(region);
                    VisionLogger.Instance.Log(LogLevel.Info,
                        $"  → 添加区域: RegionId={region.Id}, RegionType={region.GetShapeType()?.ToString() ?? "Unknown"}",
                        "RegionEditorControl");
                }
                UpdateRegionOverlay();

                VisionLogger.Instance.Log(LogLevel.Success,
                    $"[SetRegions] 区域数据设置完成，最终区域数: {_viewModel.Regions.Count}",
                    "RegionEditorControl");
            }
            catch (Exception ex)
            {
                VisionLogger.Instance.Log(LogLevel.Error,
                    $"[SetRegions] 设置区域数据失败: {ex.Message}",
                    "RegionEditorControl",
                    ex);
            }
        }

        /// <summary>
        /// 订阅外部集合的变更（外部 → 内部同步）
        /// </summary>
        private void SubscribeToExternalRegions(IEnumerable<RegionData> regions)
        {
            // 取消旧订阅
            UnsubscribeFromExternalRegions(_externalRegions);

            // 订阅新集合
            if (regions is System.Collections.ObjectModel.ObservableCollection<RegionData> observableCollection)
            {
                _externalRegions = observableCollection;
                _externalRegions.CollectionChanged += OnExternalRegionsCollectionChanged;

                VisionLogger.Instance.Log(LogLevel.Info,
                    $"[SubscribeToExternalRegions] 已订阅外部集合，当前区域数: {_externalRegions.Count}",
                    "RegionEditorControl");
            }
            else
            {
                VisionLogger.Instance.Log(LogLevel.Warning,
                    $"[SubscribeToExternalRegions] 外部集合不是 ObservableCollection 类型: {regions?.GetType().Name ?? "null"}",
                    "RegionEditorControl");
            }
        }

        /// <summary>
        /// 取消订阅外部集合的变更
        /// </summary>
        private void UnsubscribeFromExternalRegions(IEnumerable<RegionData>? regions)
        {
            if (regions is System.Collections.ObjectModel.ObservableCollection<RegionData> observableCollection)
            {
                observableCollection.CollectionChanged -= OnExternalRegionsCollectionChanged;

                if (_externalRegions == observableCollection)
                {
                    VisionLogger.Instance.Log(LogLevel.Info,
                        "[UnsubscribeFromExternalRegions] 已取消订阅外部集合",
                        "RegionEditorControl");
                }
            }
        }

        /// <summary>
        /// 外部集合变更处理（外部 → 内部同步）
        /// </summary>
        private void OnExternalRegionsCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (_isUpdatingFromExternal) return; // 避免递归

            VisionLogger.Instance.Log(LogLevel.Info,
                $"[OnExternalRegionsCollectionChanged] 外部集合变更: Action={e.Action}, NewItems={e.NewItems?.Count ?? 0}, OldItems={e.OldItems?.Count ?? 0}",
                "RegionEditorControl");

            _isUpdatingFromExternal = true;
            try
            {
                switch (e.Action)
                {
                    case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                        if (e.NewItems != null)
                        {
                            foreach (RegionData item in e.NewItems)
                            {
                                _viewModel.Regions.Add(item);
                                VisionLogger.Instance.Log(LogLevel.Info,
                                    $"  → 添加区域到内部: RegionId={item.Id}, RegionType={item.GetShapeType()?.ToString() ?? "Unknown"}",
                                    "RegionEditorControl");
                            }
                        }
                        break;

                    case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                        if (e.OldItems != null)
                        {
                            foreach (RegionData item in e.OldItems)
                            {
                                _viewModel.Regions.Remove(item);
                                VisionLogger.Instance.Log(LogLevel.Info,
                                    $"  → 从内部移除区域: RegionId={item.Id}",
                                    "RegionEditorControl");
                            }
                        }
                        break;

                    case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:
                        if (e.NewItems != null && e.OldItems != null)
                        {
                            foreach (RegionData item in e.OldItems)
                            {
                                _viewModel.Regions.Remove(item);
                            }
                            foreach (RegionData item in e.NewItems)
                            {
                                _viewModel.Regions.Add(item);
                            }
                            VisionLogger.Instance.Log(LogLevel.Info,
                                $"  → 替换区域: OldCount={e.OldItems.Count}, NewCount={e.NewItems.Count}",
                                "RegionEditorControl");
                        }
                        break;

                    case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                        SetRegions(GetValue(RegionsProperty) as IEnumerable<RegionData> ?? Enumerable.Empty<RegionData>());
                        VisionLogger.Instance.Log(LogLevel.Warning,
                            "  → 外部集合 Reset，重新加载所有区域",
                            "RegionEditorControl");
                        break;
                }

                UpdateRegionOverlay();
            }
            catch (Exception ex)
            {
                VisionLogger.Instance.Log(LogLevel.Error,
                    $"[OnExternalRegionsCollectionChanged] 外部集合同步失败: {ex.Message}",
                    "RegionEditorControl",
                    ex);
            }
            finally
            {
                _isUpdatingFromExternal = false;
            }
        }

        /// <summary>
        /// 内部集合变更处理（内部 → 外部同步）
        /// </summary>
        private void OnInternalRegionsCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (_isUpdatingFromExternal) return; // 避免递归
            if (_externalRegions == null) return; // 没有外部集合绑定

            VisionLogger.Instance.Log(LogLevel.Info,
                $"[OnInternalRegionsCollectionChanged] 内部集合变更: Action={e.Action}, NewItems={e.NewItems?.Count ?? 0}, OldItems={e.OldItems?.Count ?? 0}",
                "RegionEditorControl");

            _isUpdatingFromExternal = true;
            try
            {
                switch (e.Action)
                {
                    case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                        if (e.NewItems != null)
                        {
                            foreach (RegionData item in e.NewItems)
                            {
                                _externalRegions.Add(item);
                                VisionLogger.Instance.Log(LogLevel.Info,
                                    $"  → 添加区域到外部: RegionId={item.Id}, RegionType={item.GetShapeType()?.ToString() ?? "Unknown"}",
                                    "RegionEditorControl");
                            }
                        }
                        break;

                    case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                        if (e.OldItems != null)
                        {
                            foreach (RegionData item in e.OldItems)
                            {
                                _externalRegions.Remove(item);
                                VisionLogger.Instance.Log(LogLevel.Info,
                                    $"  → 从外部移除区域: RegionId={item.Id}",
                                    "RegionEditorControl");
                            }
                        }
                        break;

                    case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:
                        if (e.NewItems != null && e.OldItems != null)
                        {
                            foreach (RegionData item in e.OldItems)
                            {
                                _externalRegions.Remove(item);
                            }
                            foreach (RegionData item in e.NewItems)
                            {
                                _externalRegions.Add(item);
                            }
                            VisionLogger.Instance.Log(LogLevel.Info,
                                $"  → 替换区域: OldCount={e.OldItems.Count}, NewCount={e.NewItems.Count}",
                                "RegionEditorControl");
                        }
                        break;

                    case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                        _externalRegions.Clear();
                        foreach (var item in _viewModel.Regions)
                        {
                            _externalRegions.Add(item);
                        }
                        VisionLogger.Instance.Log(LogLevel.Warning,
                            $"  → 内部集合 Reset，同步到外部: 内部区域数={_viewModel.Regions.Count}",
                            "RegionEditorControl");
                        break;
                }
            }
            catch (Exception ex)
            {
                VisionLogger.Instance.Log(LogLevel.Error,
                    $"[OnInternalRegionsCollectionChanged] 内部集合同步失败: {ex.Message}",
                    "RegionEditorControl",
                    ex);
            }
            finally
            {
                _isUpdatingFromExternal = false;
            }
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
            VisionLogger.Instance.Log(LogLevel.Info,
                $"[AddRegion] 添加区域: RegionId={region.Id}, RegionType={region.GetShapeType()?.ToString() ?? "Unknown"}",
                "RegionEditorControl");

            try
            {
                _viewModel.Regions.Add(region);
                UpdateRegionOverlay();
                // RegionChanged 事件会通过 OnRegionChanged 自动转发到 RegionDataChanged
                // 这里不再手动触发，避免重复通知
            }
            catch (Exception ex)
            {
                VisionLogger.Instance.Log(LogLevel.Error,
                    $"[AddRegion] 添加区域失败: RegionId={region.Id}, 错误={ex.Message}",
                    "RegionEditorControl",
                    ex);
            }
        }

        /// <summary>
        /// 移除区域
        /// </summary>
        public void RemoveRegion(RegionData region)
        {
            VisionLogger.Instance.Log(LogLevel.Info,
                $"[RemoveRegion] 移除区域: RegionId={region.Id}, RegionType={region.GetShapeType()?.ToString() ?? "Unknown"}",
                "RegionEditorControl");

            try
            {
                _viewModel.Regions.Remove(region);
                if (_selectedRegion == region)
                    _selectedRegion = null;
                UpdateRegionOverlay();
                // RegionChanged 事件会通过 OnRegionChanged 自动转发到 RegionDataChanged
                // 这里不再手动触发，避免重复通知
            }
            catch (Exception ex)
            {
                VisionLogger.Instance.Log(LogLevel.Error,
                    $"[RemoveRegion] 移除区域失败: RegionId={region.Id}, 错误={ex.Message}",
                    "RegionEditorControl",
                    ex);
            }
        }

        /// <summary>
        /// 解析所有区域
        /// </summary>
        public List<Logic.ResolvedRegion> ResolveAllRegions()
        {
            return _viewModel.ResolveAllRegions();
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

            // 拖动ROI编辑器风格的手柄编辑
            if (_isDraggingHandle && _activeHandleType != Rendering.HandleType.None && _selectedRegion?.Parameters is ShapeParameters shape)
            {
                HandleShapeEdit(shape, position);
                UpdateSingleRegionFast(_selectedRegion);
                return;
            }

            // 拖动区域
            if (_isDragging && _selectedRegion != null)
            {
                var offset = position - _dragStartPoint;
                MoveRegion(_selectedRegion, offset);
                _dragStartPoint = position;
                UpdateSingleRegionFast(_selectedRegion);
                return;
            }

            // 绘制预览
            if (_isDrawing && _currentDrawingRegion != null)
            {
                UpdateDrawingPreview(position);
                UpdateSingleRegionFast(_currentDrawingRegion);
            }
        }

        private void RegionEditorControl_MouseMove(object sender, MouseEventArgs e)
        {
            if (_mainImageControl == null || OverlayCanvas == null) return;

            // 只在捕获鼠标时处理
            if (!_isDrawing && !_isDragging && !_isDraggingHandle) return;

            var screenPoint = e.GetPosition(_mainImageControl.MainCanvas);
            var position = _mainImageControl.ScreenToImage(screenPoint);

            // 拖动ROI编辑器风格的手柄编辑
            if (_isDraggingHandle && _activeHandleType != Rendering.HandleType.None && _selectedRegion?.Parameters is ShapeParameters shape)
            {
                HandleShapeEdit(shape, position);
                UpdateSingleRegionFast(_selectedRegion);
            }
            else if (_isDragging && _selectedRegion != null)
            {
                var offset = position - _dragStartPoint;
                MoveRegion(_selectedRegion, offset);
                _dragStartPoint = position;
                UpdateSingleRegionFast(_selectedRegion);
            }
            else if (_isDrawing && _currentDrawingRegion != null)
            {
                UpdateDrawingPreview(position);
                UpdateSingleRegionFast(_currentDrawingRegion);
            }
        }

        private void ImageControl_CanvasMouseLeftButtonDown(object? sender, ImageMouseEventArgs e)
        {
            if (OverlayCanvas == null) return;

            var position = e.ImagePosition;

            // ========== 绘制模式 ==========
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

                if (_currentDrawingRegion.Parameters is ShapeParameters shapeDef)
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
                            shapeDef.Width = 0;
                            shapeDef.Height = 0;
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

            // ========== 非绘制模式（选择/编辑） ==========
            // 首先检测是否命中手柄（仅当有选中区域时）
            if (_selectedRegion != null)
            {
                if (_currentHandles != null)
                {
                    var hitHandleType = Rendering.HandleRenderer.HitTestHandle(position, _currentHandles, 8);

                    if (hitHandleType != Rendering.HandleType.None)
                    {
                        // 中心手柄：触发拖动
                        if (hitHandleType == Rendering.HandleType.Center)
                        {
                            _isDragging = true;
                            _dragStartPoint = position;
                            CaptureMouse();
                            _viewModel.StatusMessage = "正在拖动区域";
                            e.Handled = true;
                            return;
                        }

                        // 其他手柄：开始编辑
                        _isDraggingHandle = true;
                        _activeHandleType = hitHandleType;
                        _handleDragStartPoint = position;

                        // 保存原始状态
                        if (_selectedRegion.Parameters is ShapeParameters shapeDef)
                        {
                            _originalShapeDefinition = shapeDef.Clone() as ShapeParameters;
                            _originalPosition = new Point(shapeDef.CenterX, shapeDef.CenterY);
                            _originalSize = new Size(shapeDef.Width, shapeDef.Height);
                            _originalRotation = shapeDef.Angle;

                            // 如果是旋转手柄，记录起始角度
                            if (hitHandleType == Rendering.HandleType.Rotate)
                            {
                                _rotateStartAngle = shapeDef.Angle;
                                var center = new Point(shapeDef.CenterX, shapeDef.CenterY);
                                _rotateStartMouseAngle = CalculateMouseAngle(center, position);
                            }
                        }

                        CaptureMouse();
                        _viewModel.StatusMessage = $"正在拖动 {hitHandleType} 手柄";
                        e.Handled = true;
                        return;
                    }
                }
            }

            // ========== 检测区域命中 ==========
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
                // 完成绘制，设置预览状态为 false
                _currentDrawingRegion.IsPreview = false;
                
                var shapeDef = _currentDrawingRegion.Parameters as ShapeParameters;
                if (shapeDef != null)
                {
                    // 统一使用大尺寸算法，并对长宽增加限制：任意尺寸太小都不生成区域
                    // 矩形和旋转矩形：最小宽度和高度都是10像素，同时满足面积≥100
                    bool isValid = _currentTool switch
                    {
                        ShapeType.Rectangle or ShapeType.RotatedRectangle =>
                            shapeDef.Width >= 10 &&
                            shapeDef.Height >= 10 &&
                            shapeDef.Width * shapeDef.Height >= 100,
                        ShapeType.Circle =>
                            shapeDef.Radius >= 5.64 && // 面积≥100对应的半径
                            Math.PI * shapeDef.Radius * shapeDef.Radius >= 100,
                        ShapeType.Line => shapeDef.GetLineLength() >= 10,
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
            _currentDrawingRegion = null;
            ReleaseMouseCapture();

            // 清理ROI编辑器风格的手柄状态
            _activeHandleType = Rendering.HandleType.None;
            _originalShapeDefinition = null;
            _rotateStartAngle = null;
            _rotateStartMouseAngle = null;

            UpdateRegionOverlay();
        }

        #endregion

        #region 绘制和渲染

        private void UpdateDrawingPreview(Point currentPosition)
        {
            if (_currentDrawingRegion?.Parameters is not ShapeParameters shapeDef)
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
                    shapeDef.Width = radius * 2;
                    shapeDef.Height = radius * 2;
                    break;

                case ShapeType.Line:
                    shapeDef.EndX = currentPosition.X;
                    shapeDef.EndY = currentPosition.Y;
                    break;
            }
        }

        private void MoveRegion(RegionData region, Vector offset)
        {
            if (region.Parameters is ShapeParameters shapeDef)
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
        private void UpdateShapeByHandle(RegionData region, Rendering.HandleType handleType, double dx, double dy)
        {
            if (region.Parameters is not ShapeParameters shapeDef)
                return;

            switch (shapeDef.ShapeType)
            {
                case ShapeType.Rectangle:
                    // 使用ResizeRectangleFromCorner和ResizeRectangleFromEdge方法
                    // 这些方法在HandleShapeEdit中调用
                    break;
                case ShapeType.RotatedRectangle:
                    // 旋转矩形在HandleShapeEdit中通过HandleRotatedRectangleResize处理
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
        /// 更新矩形形状（通过手柄，使用原始状态避免累积误差）
        /// </summary>
        private void UpdateRectangleByHandle(ShapeParameters shapeDef, Rendering.HandleType handleType, double dx, double dy)
        {
            // 此方法已弃用，现在使用ResizeRectangleFromCorner和ResizeRectangleFromEdge方法
            // 这些方法使用原始状态保存机制，避免累积误差
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
        private void UpdateCircleByHandle(ShapeParameters shapeDef, Rendering.HandleType handleType, double dx, double dy)
        {
            switch (handleType)
            {
                case Rendering.HandleType.Center:
                    // 中心手柄用于拖动整个圆形
                    shapeDef.CenterX += dx;
                    shapeDef.CenterY += dy;
                    break;
                default:
                    // 圆形手柄用于调整半径
                    var delta = Math.Sqrt(dx * dx + dy * dy);
                    switch (handleType)
                    {
                        case Rendering.HandleType.Top:
                            shapeDef.Radius += dy; // 向上拖动减小半径
                            break;
                        case Rendering.HandleType.Bottom:
                            shapeDef.Radius += dy; // 向下拖动增大半径
                            break;
                        case Rendering.HandleType.Left:
                            shapeDef.Radius += dx; // 向左拖动减小半径
                            break;
                        case Rendering.HandleType.Right:
                            shapeDef.Radius += dx; // 向右拖动增大半径
                            break;
                    }

                    // 确保半径不为负
                    if (shapeDef.Radius < 5) shapeDef.Radius = 5;

                    // 同步更新 Width 和 Height
                    shapeDef.Width = shapeDef.Radius * 2;
                    shapeDef.Height = shapeDef.Radius * 2;
                    break;
            }
        }

        /// <summary>
        /// 更新直线形状（通过手柄）
        /// </summary>
        private void UpdateLineByHandle(ShapeParameters shapeDef, Rendering.HandleType handleType, double dx, double dy)
        {
            switch (handleType)
            {
                case Rendering.HandleType.LineStart:
                    shapeDef.StartX += dx;
                    shapeDef.StartY += dy;
                    break;
                case Rendering.HandleType.LineEnd:
                    shapeDef.EndX += dx;
                    shapeDef.EndY += dy;
                    break;
            }
        }

        /// <summary>
        /// ROI编辑器风格的手柄编辑处理
        /// 参考ROI编辑器的编辑操作逻辑（ROIImageEditor.xaml.cs 1461-1816行）
        /// </summary>
        private void HandleShapeEdit(ShapeParameters shape, Point currentPosition)
        {
            var delta = currentPosition - _handleDragStartPoint;

            // 直线端点调整
            if (shape.ShapeType == ShapeType.Line)
            {
                HandleLineEndpoint(shape, currentPosition);
                return;
            }

            // 圆形半径调整
            if (shape.ShapeType == ShapeType.Circle)
            {
                HandleCircleResize(shape, currentPosition);
                return;
            }

            // 旋转矩形调整
            if (shape.ShapeType == ShapeType.RotatedRectangle)
            {
                if (_activeHandleType == Rendering.HandleType.Rotate)
                {
                    HandleRotate(shape, currentPosition);
                }
                else
                {
                    HandleRotatedRectangleResize(shape, currentPosition, delta);
                }
                return;
            }

            // 普通矩形调整
            if (shape.ShapeType == ShapeType.Rectangle)
            {
                switch (_activeHandleType)
                {
                    case Rendering.HandleType.TopLeft:
                        ResizeRectangleFromCorner(shape, delta, true, true);
                        break;
                    case Rendering.HandleType.TopRight:
                        ResizeRectangleFromCorner(shape, delta, false, true);
                        break;
                    case Rendering.HandleType.BottomLeft:
                        ResizeRectangleFromCorner(shape, delta, true, false);
                        break;
                    case Rendering.HandleType.BottomRight:
                        ResizeRectangleFromCorner(shape, delta, false, false);
                        break;
                    case Rendering.HandleType.Top:
                        ResizeRectangleFromEdge(shape, delta, true, true);
                        break;
                    case Rendering.HandleType.Bottom:
                        ResizeRectangleFromEdge(shape, delta, true, false);
                        break;
                    case Rendering.HandleType.Left:
                        ResizeRectangleFromEdge(shape, delta, false, true);
                        break;
                    case Rendering.HandleType.Right:
                        ResizeRectangleFromEdge(shape, delta, false, false);
                        break;
                }
            }

            // 触发变更事件
            if (_selectedRegion != null)
            {
                _selectedRegion.MarkModified();
                RegionDataChanged?.Invoke(this, _selectedRegion);
            }
        }

        /// <summary>
        /// 处理旋转操作（参考ROI编辑器HandleRotate 1792-1816行）
        /// </summary>
        private void HandleRotate(ShapeParameters shape, Point currentPosition)
        {
            var center = new Point(shape.CenterX, shape.CenterY);

            // 保存旋转开始时的状态（第一次调用时）
            if (_rotateStartAngle == null)
            {
                _rotateStartAngle = shape.Angle;
                _rotateStartMouseAngle = CalculateMouseAngle(center, _handleDragStartPoint);
            }

            // 计算当前鼠标角度
            var currentMouseAngle = CalculateMouseAngle(center, currentPosition);

            // 计算角度增量
            var deltaAngle = currentMouseAngle - _rotateStartMouseAngle.Value;

            // 应用角度增量（使用RotatedRectangleHelper.NormalizeAngle）
            shape.Angle = RotatedRectangleHelper.NormalizeAngle(_rotateStartAngle.Value + deltaAngle);
        }

        /// <summary>
        /// 计算鼠标相对于中心的图像坐标系角度
        /// </summary>
        private double CalculateMouseAngle(Point center, Point mousePos)
        {
            // 计算鼠标相对于中心的数学角度
            // Math.Atan2返回数学角度：逆时针为正，0°向右
            var mathAngle = Math.Atan2(mousePos.Y - center.Y, mousePos.X - center.X) * 180 / Math.PI;

            // 转换为图像坐标系角度（顺时针为正，0°向下）
            // 公式：imageAngle = -mathAngle + 90
            return -mathAngle + 90;
        }

        /// <summary>
        /// 处理旋转矩形大小调整（使用坐标变换，参考ROI编辑器）
        /// </summary>
        private void HandleRotatedRectangleResize(ShapeParameters shape, Point currentPosition, Vector delta)
        {
            if (_originalShapeDefinition == null) return;

            // 1. 将世界坐标系的 delta 转换为局部坐标系的 delta
            var localDelta = WorldToLocalDelta(delta, _originalRotation);

            // 2. 计算原始矩形的四个角点（局部坐标，原点在中心）
            var corners = GetLocalCorners(_originalSize.Width, _originalSize.Height);

            // 3. 根据拖动的手柄计算新的角点位置
            var newCorners = CalculateNewCorners(corners, localDelta, _activeHandleType);

            // 4. 从新角点计算新的尺寸和中心
            var (newWidth, newHeight, newCenterLocal) = CalculateFromCorners(newCorners);

            // 5. 将新中心从局部坐标转换回世界坐标
            var newCenterWorld = LocalToWorldPoint(newCenterLocal, _originalPosition, _originalRotation);

            // 6. 应用新的状态
            shape.Width = newWidth;
            shape.Height = newHeight;
            shape.CenterX = newCenterWorld.X;
            shape.CenterY = newCenterWorld.Y;
        }

        /// <summary>
        /// 将世界坐标系的 delta 转换为局部坐标系的 delta（参考ROI编辑器）
        /// </summary>
        private Vector WorldToLocalDelta(Vector worldDelta, double rotation)
        {
            // 将角度转换为弧度
            var angle = -rotation * Math.PI / 180;
            var cos = Math.Cos(angle);
            var sin = Math.Sin(angle);

            // 使用逆旋转矩阵：local = transpose(R) * world
            return new Vector(
                worldDelta.X * cos + worldDelta.Y * sin,
                -worldDelta.X * sin + worldDelta.Y * cos);
        }

        /// <summary>
        /// 将局部坐标系的点转换为世界坐标系的点（参考ROI编辑器）
        /// </summary>
        private Point LocalToWorldPoint(Point localPoint, Point center, double rotation)
        {
            // 将角度转换为弧度
            var angle = -rotation * Math.PI / 180;
            var cos = Math.Cos(angle);
            var sin = Math.Sin(angle);

            return new Point(
                center.X + localPoint.X * cos - localPoint.Y * sin,
                center.Y + localPoint.X * sin + localPoint.Y * cos);
        }

        /// <summary>
        /// 获取矩形四个角点（局部坐标，原点在中心）
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
        /// 根据拖动的手柄计算新的角点位置（固定对角锚点模式）
        /// </summary>
        private Point[] CalculateNewCorners(Point[] originalCorners, Vector delta, Rendering.HandleType handle)
        {
            var newCorners = (Point[])originalCorners.Clone();

            switch (handle)
            {
                case Rendering.HandleType.TopLeft:
                    // TopLeft 移动，BottomRight 固定
                    newCorners[0] = new Point(
                        originalCorners[0].X + delta.X,
                        originalCorners[0].Y + delta.Y);
                    // TopRight 和 BottomLeft 跟随变化（保持矩形形状）
                    newCorners[1] = new Point(originalCorners[1].X, newCorners[0].Y);
                    newCorners[3] = new Point(newCorners[0].X, originalCorners[3].Y);
                    break;

                case Rendering.HandleType.TopRight:
                    // TopRight 移动，BottomLeft 固定
                    newCorners[1] = new Point(
                        originalCorners[1].X + delta.X,
                        originalCorners[1].Y + delta.Y);
                    newCorners[0] = new Point(originalCorners[0].X, newCorners[1].Y);
                    newCorners[2] = new Point(newCorners[1].X, originalCorners[2].Y);
                    break;

                case Rendering.HandleType.BottomRight:
                    // BottomRight 移动，TopLeft 固定
                    newCorners[2] = new Point(
                        originalCorners[2].X + delta.X,
                        originalCorners[2].Y + delta.Y);
                    newCorners[1] = new Point(newCorners[2].X, originalCorners[1].Y);
                    newCorners[3] = new Point(originalCorners[3].X, newCorners[2].Y);
                    break;

                case Rendering.HandleType.BottomLeft:
                    // BottomLeft 移动，TopRight 固定
                    newCorners[3] = new Point(
                        originalCorners[3].X + delta.X,
                        originalCorners[3].Y + delta.Y);
                    newCorners[0] = new Point(newCorners[3].X, originalCorners[0].Y);
                    newCorners[2] = new Point(originalCorners[2].X, newCorners[3].Y);
                    break;

                case Rendering.HandleType.Top:
                    // 只改变高度，固定 Bottom 边
                    var newTop = originalCorners[0].Y + delta.Y;
                    newCorners[0] = new Point(originalCorners[0].X, newTop);
                    newCorners[1] = new Point(originalCorners[1].X, newTop);
                    break;

                case Rendering.HandleType.Bottom:
                    // 只改变高度，固定 Top 边
                    var newBottom = originalCorners[2].Y + delta.Y;
                    newCorners[2] = new Point(originalCorners[2].X, newBottom);
                    newCorners[3] = new Point(originalCorners[3].X, newBottom);
                    break;

                case Rendering.HandleType.Left:
                    // 只改变宽度，固定 Right 边
                    var newLeft = originalCorners[0].X + delta.X;
                    newCorners[0] = new Point(newLeft, originalCorners[0].Y);
                    newCorners[3] = new Point(newLeft, originalCorners[3].Y);
                    break;

                case Rendering.HandleType.Right:
                    // 只改变宽度，固定 Left 边
                    var newRight = originalCorners[1].X + delta.X;
                    newCorners[1] = new Point(newRight, originalCorners[1].Y);
                    newCorners[2] = new Point(newRight, originalCorners[2].Y);
                    break;
            }

            return newCorners;
        }

        /// <summary>
        /// 从四个角点计算宽度、高度和中心（局部坐标）
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
        /// 处理圆形半径调整（参考ROI编辑器HandleCircleResize 1473-1477行）
        /// </summary>
        private void HandleCircleResize(ShapeParameters shape, Point currentPosition)
        {
            var center = new Point(shape.CenterX, shape.CenterY);

            // 计算从中心到鼠标的距离作为新半径
            var dx = currentPosition.X - center.X;
            var dy = currentPosition.Y - center.Y;
            var newRadius = Math.Sqrt(dx * dx + dy * dy);

            if (newRadius < 5) newRadius = 5;

            shape.Radius = newRadius;
            
            // 同步更新 Width 和 Height，确保手柄位置正确
            shape.Width = newRadius * 2;
            shape.Height = newRadius * 2;
        }

        /// <summary>
        /// 处理直线端点调整（参考ROI编辑器HandleLineResize 1466-1470行）
        /// </summary>
        private void HandleLineEndpoint(ShapeParameters shape, Point currentPosition)
        {
            if (_activeHandleType == Rendering.HandleType.LineStart)
            {
                shape.StartX = currentPosition.X;
                shape.StartY = currentPosition.Y;
            }
            else if (_activeHandleType == Rendering.HandleType.LineEnd)
            {
                shape.EndX = currentPosition.X;
                shape.EndY = currentPosition.Y;
            }
        }

        /// <summary>
        /// 从角落调整矩形（不对称编辑 - 固定对角点模式）
        /// 参考ROI编辑器旋转矩形的HandleRotatedRectangleResize逻辑
        /// </summary>
        private void ResizeRectangleFromCorner(ShapeParameters shape, Vector delta, bool isLeft, bool isTop)
        {
            if (_originalShapeDefinition == null) return;

            var original = _originalShapeDefinition;

            // 计算原始矩形的四个角点（局部坐标，原点在中心）
            var hw = original.Width / 2;
            var hh = original.Height / 2;
            var corners = new Point[]
            {
                new Point(-hw, -hh), // TopLeft
                new Point( hw, -hh), // TopRight
                new Point( hw,  hh), // BottomRight
                new Point(-hw,  hh)  // BottomLeft
            };

            // 根据拖动的角点计算新的角点位置（固定对角锚点模式）
            Point[] newCorners;

            // 确定拖动的手柄类型
            if (isLeft && isTop) // TopLeft
            {
                // TopLeft 移动，BottomRight 固定
                newCorners = new Point[]
                {
                    new Point(corners[0].X + delta.X, corners[0].Y + delta.Y),
                    new Point(corners[1].X, corners[0].Y + delta.Y),
                    new Point(corners[2].X, corners[2].Y),
                    new Point(corners[0].X + delta.X, corners[3].Y)
                };
            }
            else if (!isLeft && isTop) // TopRight
            {
                // TopRight 移动，BottomLeft 固定
                newCorners = new Point[]
                {
                    new Point(corners[0].X, corners[0].Y + delta.Y),
                    new Point(corners[1].X + delta.X, corners[1].Y + delta.Y),
                    new Point(corners[1].X + delta.X, corners[2].Y),
                    new Point(corners[3].X, corners[3].Y)
                };
            }
            else if (isLeft && !isTop) // BottomLeft
            {
                // BottomLeft 移动，TopRight 固定
                // TopLeft.X 跟随 BottomLeft.X，BottomRight.Y 跟随 BottomLeft.Y
                newCorners = new Point[]
                {
                    new Point(corners[0].X + delta.X, corners[0].Y),
                    new Point(corners[1].X, corners[1].Y),
                    new Point(corners[2].X, corners[2].Y + delta.Y),
                    new Point(corners[3].X + delta.X, corners[3].Y + delta.Y)
                };
            }
            else // BottomRight
            {
                // BottomRight 移动，TopLeft 固定
                newCorners = new Point[]
                {
                    new Point(corners[0].X, corners[0].Y),
                    new Point(corners[1].X + delta.X, corners[1].Y),
                    new Point(corners[2].X + delta.X, corners[2].Y + delta.Y),
                    new Point(corners[3].X, corners[3].Y + delta.Y)
                };
            }

            // 从新角点计算新的尺寸和中心
            var newWidth = Math.Max(10, newCorners[1].X - newCorners[0].X);
            var newHeight = Math.Max(10, newCorners[2].Y - newCorners[0].Y);
            var centerLocal = new Point(
                (newCorners[0].X + newCorners[2].X) / 2,
                (newCorners[0].Y + newCorners[2].Y) / 2);

            // 将局部坐标转换为世界坐标
            shape.Width = newWidth;
            shape.Height = newHeight;
            shape.CenterX = original.CenterX + centerLocal.X;
            shape.CenterY = original.CenterY + centerLocal.Y;
        }

        /// <summary>
        /// 从边缘调整矩形（参考ROI编辑器ResizeFromEdge 1760-1790行）
        /// </summary>
        /// <summary>
        /// 从边缘调整矩形（非对称编辑 - 固定对边模式）
        /// 使用与 ResizeRectangleFromCorner 相同的角点计算逻辑
        /// </summary>
        private void ResizeRectangleFromEdge(ShapeParameters shape, Vector delta, bool isVertical, bool isTopOrLeft)
        {
            if (_originalShapeDefinition == null) return;

            var original = _originalShapeDefinition;

            // 计算原始矩形的四个角点（局部坐标，原点在中心）
            var hw = original.Width / 2;
            var hh = original.Height / 2;
            var corners = new Point[]
            {
                new Point(-hw, -hh), // TopLeft
                new Point( hw, -hh), // TopRight
                new Point( hw,  hh), // BottomRight
                new Point(-hw,  hh)  // BottomLeft
            };

            // 根据拖动的边缘计算新的角点位置（固定对边模式）
            Point[] newCorners;

            if (isVertical)
            {
                if (isTopOrLeft) // Top 边：移动 TopLeft 和 TopRight，Bottom 边固定
                {
                    newCorners = new Point[]
                    {
                        new Point(corners[0].X, corners[0].Y + delta.Y), // TopLeft 移动
                        new Point(corners[1].X, corners[1].Y + delta.Y), // TopRight 移动
                        new Point(corners[2].X, corners[2].Y),           // BottomRight 固定
                        new Point(corners[3].X, corners[3].Y)            // BottomLeft 固定
                    };
                }
                else // Bottom 边：移动 BottomLeft 和 BottomRight，Top 边固定
                {
                    newCorners = new Point[]
                    {
                        new Point(corners[0].X, corners[0].Y),           // TopLeft 固定
                        new Point(corners[1].X, corners[1].Y),           // TopRight 固定
                        new Point(corners[2].X, corners[2].Y + delta.Y), // BottomRight 移动
                        new Point(corners[3].X, corners[3].Y + delta.Y)  // BottomLeft 移动
                    };
                }
            }
            else
            {
                if (isTopOrLeft) // Left 边：移动 TopLeft 和 BottomLeft，Right 边固定
                {
                    newCorners = new Point[]
                    {
                        new Point(corners[0].X + delta.X, corners[0].Y), // TopLeft 移动
                        new Point(corners[1].X, corners[1].Y),           // TopRight 固定
                        new Point(corners[2].X, corners[2].Y),           // BottomRight 固定
                        new Point(corners[3].X + delta.X, corners[3].Y)  // BottomLeft 移动
                    };
                }
                else // Right 边：移动 TopRight 和 BottomRight，Left 边固定
                {
                    newCorners = new Point[]
                    {
                        new Point(corners[0].X, corners[0].Y),           // TopLeft 固定
                        new Point(corners[1].X + delta.X, corners[1].Y), // TopRight 移动
                        new Point(corners[2].X + delta.X, corners[2].Y), // BottomRight 移动
                        new Point(corners[3].X, corners[3].Y)            // BottomLeft 固定
                    };
                }
            }

            // 从新角点计算新的尺寸和中心
            var newWidth = Math.Max(10, newCorners[1].X - newCorners[0].X);
            var newHeight = Math.Max(10, newCorners[2].Y - newCorners[0].Y);
            var centerLocal = new Point(
                (newCorners[0].X + newCorners[2].X) / 2,
                (newCorners[0].Y + newCorners[2].Y) / 2);

            // 将局部坐标转换为世界坐标
            shape.Width = newWidth;
            shape.Height = newHeight;
            shape.CenterX = original.CenterX + centerLocal.X;
            shape.CenterY = original.CenterY + centerLocal.Y;
        }

        private void UpdateRegionOverlay()
        {
            if (OverlayCanvas == null)
            {
                return;
            }

            // 使用 WpfShapeRenderer 渲染区域（对象池 + 增量更新）
            if (_renderer != null)
            {
                var contexts = new List<RegionRenderContext>();

                // 添加所有可见区域
                foreach (var region in _viewModel.Regions)
                {
                    if (!region.IsVisible || region.Parameters is not ShapeParameters shapeDef)
                        continue;

                    contexts.Add(CreateRenderContext(region, shapeDef, region.IsPreview));
                }

                // 添加绘制预览
                if (_isDrawing && _currentDrawingRegion?.Parameters is ShapeParameters previewShape)
                {
                    contexts.Add(CreateRenderContext(_currentDrawingRegion, previewShape, isPreview: true));
                }

                // 渲染
                _renderer.Render(contexts, new RegionRenderOptions
                {
                    ShowLabels = true,
                    ShowHandles = false  // 手柄单独处理
                });

                // 清理手柄（辅助元素由渲染器管理）
                Rendering.HandleRenderer.ClearHandles(OverlayCanvas);
            }
            else
            {
                // 回退到传统渲染方式（渲染器未初始化时）
                OverlayCanvas.Children.Clear();

                foreach (var region in _viewModel.Regions)
                {
                    if (!region.IsVisible) continue;

                    var shape = CreateRegionShape(region);
                    if (shape != null)
                    {
                        OverlayCanvas.Children.Add(shape);
                    }

                    DrawRegionLabel(region);
                }

                if (_isDrawing && _currentDrawingRegion != null)
                {
                    var parametersShape = CreateRegionShape(_currentDrawingRegion, true);
                    if (parametersShape != null)
                    {
                        OverlayCanvas.Children.Add(parametersShape);
                    }
                }
            }

            // 绘制选中区域的编辑手柄（独立于区域渲染）
            if (_selectedRegion != null && !_isDrawing)
            {
                DrawEditHandles(_selectedRegion);
            }
        }

        /// <summary>
        /// 创建渲染上下文
        /// </summary>
        private RegionRenderContext CreateRenderContext(RegionData region, ShapeParameters shape, bool isPreview = false)
        {
            // 日志监控（每次调用都记录，因为这个方法调用频率不高）
            VisionLogger.Instance.Log(LogLevel.Info,
                $"[CreateRenderContext] RegionId={region.Id}, IsPreview={isPreview}, ShapeType={shape.ShapeType}",
                "RegionEditorControl");

            var context = new RegionRenderContext
            {
                Id = region.Id,
                ShapeType = shape.ShapeType,
                IsSelected = region == _selectedRegion,
                IsPreview = isPreview,
                IsVisible = true,

                // 几何参数
                Center = new Point(shape.CenterX, shape.CenterY),
                Width = shape.Width,
                Height = shape.Height,
                Angle = shape.Angle,
                Radius = shape.Radius,
                LineStart = new Point(shape.StartX, shape.StartY),
                LineEnd = new Point(shape.EndX, shape.EndY),
                PolygonPoints = shape.Points?.Select(p => new Point(p.X, p.Y)).ToList() ?? new List<Point>(),

                // 样式参数
                FillColor = Color.FromArgb(
                    (byte)((shape.FillColorArgb >> 24) & 0xFF),
                    (byte)((shape.FillColorArgb >> 16) & 0xFF),
                    (byte)((shape.FillColorArgb >> 8) & 0xFF),
                    (byte)(shape.FillColorArgb & 0xFF)
                ),
                StrokeColor = Color.FromArgb(
                    (byte)((shape.StrokeColorArgb >> 24) & 0xFF),
                    (byte)((shape.StrokeColorArgb >> 16) & 0xFF),
                    (byte)((shape.StrokeColorArgb >> 8) & 0xFF),
                    (byte)(shape.StrokeColorArgb & 0xFF)
                ),
                StrokeThickness = shape.StrokeThickness,
                Opacity = shape.Opacity,

                // 标签
                Label = region.Name
            };

            context.CalculateBounds();
            return context;
        }

        /// <summary>
        /// 绘制编辑手柄（ROI编辑器风格）
        /// </summary>
        private void DrawEditHandles(RegionData region)
        {
            if (region.Parameters is not ShapeParameters shapeDef || OverlayCanvas == null)
                return;

            // 使用ROI编辑器的手柄渲染器创建手柄
            CreateHandlesForShape(shapeDef);

            if (_currentHandles != null && _currentHandles.Length > 0)
            {
                // 使用ROI编辑器的手柄渲染器绘制手柄
                Rendering.HandleRenderer.DrawHandles(OverlayCanvas, _currentHandles, shapeDef.ShapeType);
            }
        }

        /// <summary>
        /// 为形状创建ROI编辑器风格的手柄
        /// </summary>
        private void CreateHandlesForShape(ShapeParameters shape)
        {
            switch (shape.ShapeType)
            {
                case ShapeType.Rectangle:
                    var bounds = new Rect(
                        shape.CenterX - shape.Width / 2,
                        shape.CenterY - shape.Height / 2,
                        shape.Width,
                        shape.Height
                    );
                    _currentHandles = Rendering.HandleRenderer.CreateRectangleHandles(bounds);
                    break;

                case ShapeType.Circle:
                    var center = new Point(shape.CenterX, shape.CenterY);
                    _currentHandles = Rendering.HandleRenderer.CreateCircleHandles(center, shape.Radius);
                    break;

                case ShapeType.RotatedRectangle:
                    var rotatedCenter = new Point(shape.CenterX, shape.CenterY);
                    var corners = RotatedRectangleHelper.GetCorners(
                        rotatedCenter,
                        shape.Width,
                        shape.Height,
                        shape.Angle
                    );
                    var bottomCenter = CalculateRotatedRectBottomCenter(rotatedCenter, shape.Height, shape.Angle);
                    _currentHandles = Rendering.HandleRenderer.CreateRotatedRectangleHandles(
                        corners,
                        shape.Angle,
                        bottomCenter
                    );
                    break;

                case ShapeType.Line:
                    var startPoint = new Point(shape.StartX, shape.StartY);
                    var endPoint = new Point(shape.EndX, shape.EndY);
                    _currentHandles = Rendering.HandleRenderer.CreateLineHandles(startPoint, endPoint);
                    break;
            }
        }

        /// <summary>
        /// 计算旋转矩形的下边中点
        /// </summary>
        private Point CalculateRotatedRectBottomCenter(Point center, double height, double angle)
        {
            var angleRad = angle * Math.PI / 180;
            var sin = Math.Sin(angleRad);
            var cos = Math.Cos(angleRad);

            return new Point(
                center.X + (height / 2) * sin,
                center.Y + (height / 2) * cos
            );
        }

        /// <summary>
        /// 在可视化树中向上查找指定类型的父级元素
        /// </summary>
        private static T? FindVisualParent<T>(DependencyObject? child) where T : DependencyObject
        {
            if (child == null)
                return null;

            DependencyObject? parent = VisualTreeHelper.GetParent(child);
            while (parent != null)
            {
                if (parent is T result)
                    return result;
                parent = VisualTreeHelper.GetParent(parent);
            }
            return null;
        }

        private Shape? CreateRegionShape(RegionData region, bool isPreview = false)
        {
            if (region.Parameters is not ShapeParameters shapeDef)
                return null;

            // 使用ShapeRenderer创建形状（参考ROI编辑器）
            var isSelected = region == _selectedRegion;
            var shape = ShapeRenderer.CreateShape(shapeDef, isSelected, isPreview);

            if (shape != null)
            {
                // 根据shape.Tag获取位置信息
                var tag = shape.Tag;
                if (tag != null && tag.GetType().GetProperty("X") != null && tag.GetType().GetProperty("Y") != null)
                {
                    var xProp = tag.GetType().GetProperty("X");
                    var yProp = tag.GetType().GetProperty("Y");
                    var x = (double?)xProp.GetValue(tag);
                    var y = (double?)yProp.GetValue(tag);

                    if (x.HasValue && y.HasValue)
                    {
                        Canvas.SetLeft(shape, x.Value);
                        Canvas.SetTop(shape, y.Value);
                    }
                }
                // 直线不需要Canvas定位
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
            if (region.Parameters is not ShapeParameters shapeDef)
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
            // 从后往前测试（后绘制的在上面）
            foreach (var region in _viewModel.Regions.Reverse())
            {
                if (!region.IsVisible) continue;

                if (IsPointInRegion(point, region))
                {
                    return region;
                }
            }

            return null;
        }

        private bool IsPointInRegion(Point point, RegionData region)
        {
            if (region.Parameters is not ShapeParameters shapeDef)
                return false;

            return shapeDef.ShapeType switch
            {
                ShapeType.Rectangle => IsPointInRectangle(point, shapeDef),
                ShapeType.RotatedRectangle => IsPointInRotatedRectangle(point, shapeDef),
                ShapeType.Circle => IsPointInCircle(point, shapeDef),
                ShapeType.Line => IsPointNearLine(point, shapeDef),
                _ => false
            };
        }

        /// <summary>
        /// 判断点是否在矩形内（参考ROI编辑器使用GetBounds和Contains）
        /// </summary>
        private bool IsPointInRectangle(Point point, ShapeParameters shapeDef)
        {
            var bounds = new Rect(
                shapeDef.CenterX - shapeDef.Width / 2,
                shapeDef.CenterY - shapeDef.Height / 2,
                shapeDef.Width,
                shapeDef.Height
            );
            return bounds.Contains(point);
        }

        /// <summary>
        /// 判断点是否在圆形内
        /// </summary>
        private bool IsPointInCircle(Point point, ShapeParameters shapeDef)
        {
            var distance = Math.Sqrt(
                Math.Pow(point.X - shapeDef.CenterX, 2) +
                Math.Pow(point.Y - shapeDef.CenterY, 2));
            return distance <= shapeDef.Radius;
        }

        /// <summary>
        /// 判断点是否在直线附近（10像素容差）
        /// </summary>
        private bool IsPointNearLine(Point point, ShapeParameters shapeDef)
        {
            return DistanceToLine(point,
                new Point(shapeDef.StartX, shapeDef.StartY),
                new Point(shapeDef.EndX, shapeDef.EndY)) < 10;
        }

        /// <summary>
        /// 判断点是否在旋转矩形内（使用坐标变换法）
        /// </summary>
        private bool IsPointInRotatedRectangle(Point point, ShapeParameters shapeDef)
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

            // 清理ROI编辑器风格的手柄状态
            _currentHandles = null;
            _activeHandleType = Rendering.HandleType.None;
            _originalShapeDefinition = null;
            _rotateStartAngle = null;
            _rotateStartMouseAngle = null;

            UpdateRegionOverlay();
        }

        /// <summary>
        /// 快速更新单个区域（用于拖动时的增量更新）
        /// </summary>
        private void UpdateSingleRegionFast(RegionData region)
        {
            if (_renderer == null || region.Parameters is not ShapeParameters shape) return;

            // 使用 region.IsPreview 自动识别预览状态
            var context = CreateRenderContext(region, shape, region.IsPreview);
            _renderer.UpdateRegion(context);

            // 更新手柄位置（手柄独立于区域渲染）
            if (region == _selectedRegion && OverlayCanvas != null)
            {
                Rendering.HandleRenderer.ClearHandles(OverlayCanvas);  // 清理手柄
                DrawEditHandles(region);  // 绘制新的手柄
            }
        }

        #endregion

        #region ViewModel事件处理

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // 可以根据需要响应ViewModel属性变更
        }

        private void OnRegionChanged(object? sender, RegionChangedEventArgs e)
        {
            VisionLogger.Instance.Log(LogLevel.Info,
                $"[OnRegionChanged] ViewModel 触发 RegionChanged: RegionId={e.Region?.Id}, ChangeType={e.ChangeType}",
                "RegionEditorControl");

            try
            {
                UpdateRegionOverlay();
                if (e.Region != null)
                {
                    RegionDataChanged?.Invoke(this, e.Region);
                }
            }
            catch (Exception ex)
            {
                VisionLogger.Instance.Log(LogLevel.Error,
                    $"[OnRegionChanged] 区域变更处理失败: {ex.Message}",
                    "RegionEditorControl",
                    ex);
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

            // 切换到绘制模式
            _viewModel.CurrentMode = RegionDefinitionMode.Drawing;
            _viewModel.IsDrawing = true;

            // 手动触发 IsDrawingMode 的 PropertyChanged 事件
            // 因为 CurrentMode 的默认值已经是 Drawing，所以 setter 不会触发 PropertyChanged
            _viewModel.NotifyIsDrawingModeChanged();

            // 设置 SelectedShapeType（如果值变化，setter 会自动调用 UpdateParameters）
            // 如果值相同，setter 不会调用 UpdateParameters，所以我们需要手动调用
            var oldShapeType = _viewModel.SelectedShapeType;
            _viewModel.SelectedShapeType = shapeType;

            // 只有当形状类型未变化时，才手动调用 UpdateParameters
            // 因为这种情况下 setter 不会调用 UpdateParameters
            // 同时需要手动触发 SelectedShapeType 的 PropertyChanged 事件，以确保参数面板可见性更新
            if (oldShapeType == shapeType)
            {
                _viewModel.CallUpdateParameters();
                _viewModel.NotifySelectedShapeTypeChanged();
            }

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
