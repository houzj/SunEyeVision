using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using SunEyeVision.Plugin.SDK.UI.Controls.Region.Models;
using SunEyeVision.Plugin.SDK.UI.Controls.Region.Logic;

namespace SunEyeVision.Plugin.SDK.UI.Controls.Region.ViewModels
{
    /// <summary>
    /// 区域编辑器视图模型
    /// </summary>
    public class RegionEditorViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly Logic.RegionResolver _resolver;
        private readonly EditHistory _editHistory;
        private RegionData? _selectedRegion;
        private ShapeDefinition? _editingShape;
        private bool _isEditing;
        private bool _isDrawing;
        private ShapeType _drawingShapeType = ShapeType.Rectangle;
        private RegionDefinitionMode _currentMode = RegionDefinitionMode.Drawing;
        private string _statusMessage = "就绪";
        private bool _isInfoPanelVisible = true;

        // 新增：子ViewModel
        private ParameterPanelViewModel? _parameterPanel;
        private NodeSelectorViewModel? _nodeSelector;
        private IRegionDataSourceProvider? _dataProvider;

        /// <summary>
        /// 区域列表
        /// </summary>
        public ObservableCollection<RegionData> Regions { get; } = new();

        /// <summary>
        /// 选中的区域
        /// </summary>
        public RegionData? SelectedRegion
        {
            get => _selectedRegion;
            set
            {
                if (SetProperty(ref _selectedRegion, value))
                {
                    UpdateEditingShape();
                    OnPropertyChanged(nameof(HasSelection));
                    OnPropertyChanged(nameof(CanEdit));
                    OnPropertyChanged(nameof(SelectedRegionMode));
                    OnPropertyChanged(nameof(SelectedRegionShapeType));
                    OnPropertyChanged(nameof(IsDrawingMode));
                    OnPropertyChanged(nameof(IsSubscribeByRegionMode));
                    OnPropertyChanged(nameof(IsSubscribeByParameterMode));
                    OnPropertyChanged(nameof(ParameterBindings));
                }
            }
        }

        /// <summary>
        /// 当前编辑的形状定义
        /// </summary>
        public ShapeDefinition? EditingShape
        {
            get => _editingShape;
            private set => SetProperty(ref _editingShape, value);
        }

        /// <summary>
        /// 是否正在编辑
        /// </summary>
        public bool IsEditing
        {
            get => _isEditing;
            set => SetProperty(ref _isEditing, value);
        }

        /// <summary>
        /// 是否正在绘制
        /// </summary>
        public bool IsDrawing
        {
            get => _isDrawing;
            set
            {
                if (SetProperty(ref _isDrawing, value))
                {
                    OnPropertyChanged(nameof(IsSelectMode));
                    OnPropertyChanged(nameof(CurrentToolMode));
                }
            }
        }

        /// <summary>
        /// 是否在选择模式
        /// </summary>
        public bool IsSelectMode => !IsDrawing;

        /// <summary>
        /// 当前工具模式描述（用于状态栏显示）
        /// </summary>
        public string CurrentToolMode
        {
            get
            {
                if (!IsDrawingMode)
                    return "订阅模式";

                return IsDrawing
                    ? $"绘制{GetShapeTypeName(DrawingShapeType)}"
                    : "选择模式";
            }
        }

        /// <summary>
        /// 获取形状类型的中文名称
        /// </summary>
        private static string GetShapeTypeName(ShapeType shapeType)
        {
            return shapeType switch
            {
                ShapeType.Rectangle => "矩形",
                ShapeType.Circle => "圆形",
                ShapeType.RotatedRectangle => "旋转矩形",
                ShapeType.Line => "直线",
                ShapeType.Point => "点",
                ShapeType.Polygon => "多边形",
                _ => shapeType.ToString()
            };
        }

        /// <summary>
        /// 绘制的形状类型
        /// </summary>
        public ShapeType DrawingShapeType
        {
            get => _drawingShapeType;
            set
            {
                if (SetProperty(ref _drawingShapeType, value))
                {
                    OnPropertyChanged(nameof(CurrentToolMode));
                }
            }
        }

        /// <summary>
        /// 当前模式
        /// </summary>
        public RegionDefinitionMode CurrentMode
        {
            get => _currentMode;
            set
            {
                if (SetProperty(ref _currentMode, value))
                {
                    OnPropertyChanged(nameof(IsDrawingMode));
                    OnPropertyChanged(nameof(IsSubscribeMode));
                    OnPropertyChanged(nameof(IsSubscribeByRegionMode));
                    OnPropertyChanged(nameof(IsSubscribeByParameterMode));
                    OnPropertyChanged(nameof(IsSubscribeByRegion));
                    OnPropertyChanged(nameof(IsSubscribeByParameter));
                    OnPropertyChanged(nameof(ShowShapeTypeSelector));
                    OnPropertyChanged(nameof(CurrentToolMode));
                }
            }
        }

        /// <summary>
        /// 是否为绘制模式
        /// </summary>
        public bool IsDrawingMode
        {
            get => CurrentMode == RegionDefinitionMode.Drawing;
            set
            {
                if (value && CurrentMode != RegionDefinitionMode.Drawing)
                {
                    CurrentMode = RegionDefinitionMode.Drawing;
                    OnPropertyChanged(nameof(IsSubscribeMode));
                    OnPropertyChanged(nameof(ShowShapeTypeSelector));
                }
            }
        }

        /// <summary>
        /// 是否为订阅模式
        /// </summary>
        public bool IsSubscribeMode
        {
            get => CurrentMode == RegionDefinitionMode.SubscribeByRegion || 
                   CurrentMode == RegionDefinitionMode.SubscribeByParameter;
            set
            {
                if (value && CurrentMode == RegionDefinitionMode.Drawing)
                {
                    CurrentMode = RegionDefinitionMode.SubscribeByRegion;
                    OnPropertyChanged(nameof(IsDrawingMode));
                    OnPropertyChanged(nameof(ShowShapeTypeSelector));
                }
            }
        }

        /// <summary>
        /// 是否为按区域订阅模式
        /// </summary>
        public bool IsSubscribeByRegionMode
        {
            get => CurrentMode == RegionDefinitionMode.SubscribeByRegion;
            set
            {
                if (value)
                {
                    CurrentMode = RegionDefinitionMode.SubscribeByRegion;
                    OnPropertyChanged(nameof(IsDrawingMode));
                    OnPropertyChanged(nameof(IsSubscribeMode));
                    OnPropertyChanged(nameof(IsSubscribeByParameter));
                    OnPropertyChanged(nameof(ShowShapeTypeSelector));
                }
            }
        }

        /// <summary>
        /// 是否为按参数订阅模式
        /// </summary>
        public bool IsSubscribeByParameterMode
        {
            get => CurrentMode == RegionDefinitionMode.SubscribeByParameter;
            set
            {
                if (value)
                {
                    CurrentMode = RegionDefinitionMode.SubscribeByParameter;
                    OnPropertyChanged(nameof(IsDrawingMode));
                    OnPropertyChanged(nameof(IsSubscribeMode));
                    OnPropertyChanged(nameof(IsSubscribeByRegion));
                    OnPropertyChanged(nameof(ShowShapeTypeSelector));
                }
            }
        }

        /// <summary>
        /// 是否为按区域订阅
        /// </summary>
        public bool IsSubscribeByRegion
        {
            get => CurrentMode == RegionDefinitionMode.SubscribeByRegion;
            set
            {
                if (value && CurrentMode != RegionDefinitionMode.SubscribeByRegion)
                {
                    CurrentMode = RegionDefinitionMode.SubscribeByRegion;
                }
            }
        }

        /// <summary>
        /// 是否为按参数订阅
        /// </summary>
        public bool IsSubscribeByParameter
        {
            get => CurrentMode == RegionDefinitionMode.SubscribeByParameter;
            set
            {
                if (value && CurrentMode != RegionDefinitionMode.SubscribeByParameter)
                {
                    CurrentMode = RegionDefinitionMode.SubscribeByParameter;
                }
            }
        }

        /// <summary>
        /// 是否显示图形类型选择器
        /// </summary>
        public bool ShowShapeTypeSelector => IsDrawingMode || IsSubscribeByParameterMode;

        /// <summary>
        /// 当前图形类型
        /// </summary>
        public ShapeType CurrentShapeType
        {
            get => _drawingShapeType;
            set
            {
                if (SetProperty(ref _drawingShapeType, value))
                {
                    OnPropertyChanged(nameof(IsRectangleSelected));
                    OnPropertyChanged(nameof(IsCircleSelected));
                    OnPropertyChanged(nameof(IsRotatedRectangleSelected));
                    OnPropertyChanged(nameof(IsLineSelected));
                }
            }
        }

        /// <summary>
        /// 是否选中矩形
        /// </summary>
        public bool IsRectangleSelected
        {
            get => CurrentShapeType == ShapeType.Rectangle;
            set { if (value) CurrentShapeType = ShapeType.Rectangle; }
        }

        /// <summary>
        /// 是否选中圆形
        /// </summary>
        public bool IsCircleSelected
        {
            get => CurrentShapeType == ShapeType.Circle;
            set { if (value) CurrentShapeType = ShapeType.Circle; }
        }

        /// <summary>
        /// 是否选中旋转矩形
        /// </summary>
        public bool IsRotatedRectangleSelected
        {
            get => CurrentShapeType == ShapeType.RotatedRectangle;
            set { if (value) CurrentShapeType = ShapeType.RotatedRectangle; }
        }

        /// <summary>
        /// 是否选中直线
        /// </summary>
        public bool IsLineSelected
        {
            get => CurrentShapeType == ShapeType.Line;
            set { if (value) CurrentShapeType = ShapeType.Line; }
        }

        /// <summary>
        /// 选中区域的定义模式
        /// </summary>
        public RegionDefinitionMode SelectedRegionMode => SelectedRegion?.GetMode() ?? RegionDefinitionMode.Drawing;

        /// <summary>
        /// 选中区域的形状类型
        /// </summary>
        public ShapeType? SelectedRegionShapeType => SelectedRegion?.GetShapeType();

        /// <summary>
        /// 是否有选中项
        /// </summary>
        public bool HasSelection => SelectedRegion != null;

        /// <summary>
        /// 选中区域的参数绑定（用于参数订阅模式）
        /// </summary>
        public Dictionary<string, ParameterSource>? ParameterBindings =>
            (SelectedRegion?.Definition as ComputedRegion)?.ParameterBindings;

        /// <summary>
        /// 是否可编辑
        /// </summary>
        public bool CanEdit => SelectedRegion != null && SelectedRegion.IsEditable;

        /// <summary>
        /// 状态消息
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        /// <summary>
        /// 信息面板是否可见
        /// </summary>
        public bool IsInfoPanelVisible
        {
            get => _isInfoPanelVisible;
            set => SetProperty(ref _isInfoPanelVisible, value);
        }

        /// <summary>
        /// 编辑历史管理器
        /// </summary>
        public EditHistory EditHistory => _editHistory;

        /// <summary>
        /// 是否可撤销
        /// </summary>
        public bool CanUndo => _editHistory.CanUndo;

        /// <summary>
        /// 是否可重做
        /// </summary>
        public bool CanRedo => _editHistory.CanRedo;

        /// <summary>
        /// 可用的形状类型列表
        /// </summary>
        public IReadOnlyList<ShapeType> AvailableShapeTypes { get; } = new[]
        {
            ShapeType.Point,
            ShapeType.Line,
            ShapeType.Circle,
            ShapeType.Rectangle,
            ShapeType.RotatedRectangle,
            ShapeType.Polygon
        };

        /// <summary>
        /// 参数面板视图模型
        /// </summary>
        public ParameterPanelViewModel? ParameterPanel
        {
            get => _parameterPanel;
            private set => SetProperty(ref _parameterPanel, value);
        }

        /// <summary>
        /// 节点选择器视图模型
        /// </summary>
        public NodeSelectorViewModel? NodeSelector
        {
            get => _nodeSelector;
            private set => SetProperty(ref _nodeSelector, value);
        }

        #region 命令

        public ICommand AddRegionCommand { get; }
        public ICommand RemoveRegionCommand { get; }
        public ICommand ClearAllCommand { get; }
        public ICommand SetDrawingModeCommand { get; }
        public ICommand SetSubscribeByRegionModeCommand { get; }
        public ICommand SetSubscribeByParameterModeCommand { get; }
        public ICommand ToggleInfoPanelCommand { get; }
        public ICommand UndoCommand { get; }
        public ICommand RedoCommand { get; }

        #endregion

        /// <summary>
        /// 区域变更事件
        /// </summary>
        public event EventHandler<RegionChangedEventArgs>? RegionChanged;

        /// <summary>
        /// 区域选择变更事件
        /// </summary>
        public event EventHandler? SelectionChanged;

        public RegionEditorViewModel()
        {
            _resolver = new Logic.RegionResolver();
            _editHistory = new EditHistory();

            AddRegionCommand = new RelayCommand<ShapeType>(AddRegion);
            RemoveRegionCommand = new RelayCommand(RemoveSelectedRegion, () => HasSelection);
            ClearAllCommand = new RelayCommand(ClearAllRegions, () => Regions.Count > 0);
            SetDrawingModeCommand = new RelayCommand(() => CurrentMode = RegionDefinitionMode.Drawing);
            SetSubscribeByRegionModeCommand = new RelayCommand(() => CurrentMode = RegionDefinitionMode.SubscribeByRegion);
            SetSubscribeByParameterModeCommand = new RelayCommand(() => CurrentMode = RegionDefinitionMode.SubscribeByParameter);
            ToggleInfoPanelCommand = new RelayCommand(() => IsInfoPanelVisible = !IsInfoPanelVisible);
            UndoCommand = new RelayCommand(Undo, () => CanUndo);
            RedoCommand = new RelayCommand(Redo, () => CanRedo);

            // 订阅编辑历史变更事件
            _editHistory.HistoryChanged += OnEditHistoryChanged;
        }

        /// <summary>
        /// 初始化数据源提供者
        /// </summary>
        public void Initialize(IRegionDataSourceProvider dataProvider)
        {
            _dataProvider = dataProvider;
            _parameterPanel = new ParameterPanelViewModel(dataProvider);
            _nodeSelector = new NodeSelectorViewModel(dataProvider);

            // 订阅事件
            _nodeSelector.SelectionConfirmed += OnNodeSelectionConfirmed;
            _parameterPanel.ParameterBindingChanged += OnParameterBindingChanged;

            OnPropertyChanged(nameof(ParameterPanel));
            OnPropertyChanged(nameof(NodeSelector));
        }

        /// <summary>
        /// 节点选择确认事件处理
        /// </summary>
        private void OnNodeSelectionConfirmed(object? sender, NodeOutputInfo? selectedNode)
        {
            if (selectedNode != null && _parameterPanel != null)
            {
                _parameterPanel.ApplyBindingSelection(selectedNode);
                StatusMessage = $"已绑定: {selectedNode.DisplayPath}";
            }
        }

        /// <summary>
        /// 参数绑定变更事件处理
        /// </summary>
        private void OnParameterBindingChanged(object? sender, ParameterBindingChangedEventArgs e)
        {
            if (SelectedRegion?.Definition is ComputedRegion computed)
            {
                if (e.NewSource != null)
                {
                    computed.SetParameterBinding(e.ParameterName, e.NewSource);
                }
                else
                {
                    computed.RemoveParameterBinding(e.ParameterName);
                }
                
                SelectedRegion.MarkModified();
                RegionChanged?.Invoke(this, new RegionChangedEventArgs(SelectedRegion, RegionChangeType.Modified));
            }
        }

        /// <summary>
        /// 添加区域
        /// </summary>
        public void AddRegion(ShapeType shapeType)
        {
            var region = RegionData.CreateDrawingRegion($"区域_{Regions.Count + 1}", shapeType);

            // 设置默认参数
            if (region.Definition is ShapeDefinition shapeDef)
            {
                shapeDef.CenterX = 100;
                shapeDef.CenterY = 100;
                shapeDef.Width = 100;
                shapeDef.Height = 100;
                shapeDef.Radius = 50;
            }

            // 记录编辑历史
            _editHistory.ExecuteAction(new CreateRegionAction(region, this));

            Regions.Add(region);
            SelectedRegion = region;
            StatusMessage = $"已添加 {shapeType} 区域";

            RegionChanged?.Invoke(this, new RegionChangedEventArgs(region, RegionChangeType.Added));
        }

        /// <summary>
        /// 移除选中区域
        /// </summary>
        public void RemoveSelectedRegion()
        {
            if (SelectedRegion == null) return;

            var region = SelectedRegion;

            // 记录编辑历史
            _editHistory.ExecuteAction(new DeleteRegionAction(region, this));

            Regions.Remove(region);
            SelectedRegion = Regions.FirstOrDefault();
            StatusMessage = $"已移除区域 {region.Name}";

            RegionChanged?.Invoke(this, new RegionChangedEventArgs(region, RegionChangeType.Removed));
        }

        /// <summary>
        /// 清除所有区域
        /// </summary>
        public void ClearAllRegions()
        {
            if (Regions.Count == 0) return;

            var count = Regions.Count;
            var deletedRegions = Regions.ToList();

            // 记录编辑历史
            _editHistory.ExecuteAction(new ClearAllAction(deletedRegions, this));

            Regions.Clear();
            SelectedRegion = null;
            StatusMessage = $"已清除 {count} 个区域";

            RegionChanged?.Invoke(this, new RegionChangedEventArgs(null, RegionChangeType.Cleared));
        }

        /// <summary>
        /// 撤销操作
        /// </summary>
        public void Undo()
        {
            if (_editHistory.CanUndo)
            {
                _editHistory.Undo();
                StatusMessage = "已撤销";
            }
        }

        /// <summary>
        /// 重做操作
        /// </summary>
        public void Redo()
        {
            if (_editHistory.CanRedo)
            {
                _editHistory.Redo();
                StatusMessage = "已重做";
            }
        }

        /// <summary>
        /// 编辑历史变更处理
        /// </summary>
        private void OnEditHistoryChanged(object? sender, EventArgs e)
        {
            OnPropertyChanged(nameof(CanUndo));
            OnPropertyChanged(nameof(CanRedo));
            CommandManager.InvalidateRequerySuggested();
        }

        /// <summary>
        /// 内部添加区域（用于撤销/重做）
        /// </summary>
        internal void AddRegionInternal(RegionData region)
        {
            Regions.Add(region);
            RegionChanged?.Invoke(this, new RegionChangedEventArgs(region, RegionChangeType.Added));
        }

        /// <summary>
        /// 内部移除区域（用于撤销/重做）
        /// </summary>
        internal void RemoveRegionInternal(RegionData region)
        {
            Regions.Remove(region);
            if (SelectedRegion == region)
                SelectedRegion = null;
            RegionChanged?.Invoke(this, new RegionChangedEventArgs(region, RegionChangeType.Removed));
        }

        /// <summary>
        /// 内部设置区域列表（用于撤销/重做）
        /// </summary>
        internal void SetRegionsInternal(IEnumerable<RegionData> regions)
        {
            Regions.Clear();
            foreach (var region in regions)
            {
                Regions.Add(region);
            }
            SelectedRegion = null;
            RegionChanged?.Invoke(this, new RegionChangedEventArgs(null, RegionChangeType.Cleared));
        }

        /// <summary>
        /// 更新编辑形状
        /// </summary>
        private void UpdateEditingShape()
        {
            if (SelectedRegion?.Definition is ShapeDefinition shapeDef)
            {
                EditingShape = shapeDef;
            }
            else
            {
                EditingShape = null;
            }

            // 更新参数面板
            if (_parameterPanel != null && SelectedRegion?.Definition != null)
            {
                if (SelectedRegion.Definition is ComputedRegion computedRegion)
                {
                    _parameterPanel.LoadFromComputedRegion(computedRegion);
                }
                else if (SelectedRegion.Definition is ShapeDefinition definition)
                {
                    _parameterPanel.CurrentShapeType = definition.ShapeType;
                }
            }
        }

        /// <summary>
        /// 开始绘制
        /// </summary>
        public void StartDrawing(ShapeType shapeType)
        {
            IsDrawing = true;
            DrawingShapeType = shapeType;
            StatusMessage = $"正在绘制 {shapeType}";
        }

        /// <summary>
        /// 结束绘制
        /// </summary>
        public void EndDrawing()
        {
            IsDrawing = false;
            StatusMessage = "绘制完成";
        }

        /// <summary>
        /// 更新形状参数
        /// </summary>
        public void UpdateShapeParameter(string parameterName, object value)
        {
            if (EditingShape == null) return;

            var prop = typeof(ShapeDefinition).GetProperty(parameterName);
            if (prop != null && prop.CanWrite)
            {
                prop.SetValue(EditingShape, Convert.ChangeType(value, prop.PropertyType));
                SelectedRegion?.MarkModified();
                RegionChanged?.Invoke(this, new RegionChangedEventArgs(SelectedRegion, RegionChangeType.Modified));
            }
        }

        /// <summary>
        /// 设置区域订阅源
        /// </summary>
        public void SetRegionSubscription(string nodeId, string outputName, int? index = null)
        {
            if (SelectedRegion == null) return;

            SelectedRegion.Definition = new FixedRegion(nodeId, outputName, index);
            SelectedRegion.MarkModified();

            RegionChanged?.Invoke(this, new RegionChangedEventArgs(SelectedRegion, RegionChangeType.Modified));
            StatusMessage = $"已设置订阅源: {outputName}";
        }

        /// <summary>
        /// 设置参数绑定
        /// </summary>
        public void SetParameterBinding(string parameterName, ParameterSource source)
        {
            if (SelectedRegion?.Definition is ComputedRegion computedDef)
            {
                computedDef.SetParameterBinding(parameterName, source);
                SelectedRegion.MarkModified();

                RegionChanged?.Invoke(this, new RegionChangedEventArgs(SelectedRegion, RegionChangeType.Modified));
                StatusMessage = $"已设置参数绑定: {parameterName}";
            }
        }

        /// <summary>
        /// 解析选中区域
        /// </summary>
        public Logic.ResolvedRegion? ResolveSelectedRegion()
        {
            return _resolver.Resolve(SelectedRegion);
        }

        /// <summary>
        /// 解析所有区域
        /// </summary>
        public List<Logic.ResolvedRegion> ResolveAllRegions()
        {
            return _resolver.ResolveAll(Regions);
        }

        public void Dispose()
        {
            // 清理资源
            _editHistory.HistoryChanged -= OnEditHistoryChanged;

            if (_parameterPanel != null)
            {
                _parameterPanel.ParameterBindingChanged -= OnParameterBindingChanged;
                _parameterPanel.Dispose();
            }

            if (_nodeSelector != null)
            {
                _nodeSelector.SelectionConfirmed -= OnNodeSelectionConfirmed;
                _nodeSelector.Dispose();
            }
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

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
    /// 区域变更事件参数
    /// </summary>
    public class RegionChangedEventArgs : EventArgs
    {
        public RegionData? Region { get; }
        public RegionChangeType ChangeType { get; }

        public RegionChangedEventArgs(RegionData? region, RegionChangeType changeType)
        {
            Region = region;
            ChangeType = changeType;
        }
    }

    /// <summary>
    /// 区域变更类型
    /// </summary>
    public enum RegionChangeType
    {
        Added,
        Removed,
        Modified,
        Cleared
    }

    /// <summary>
    /// 泛型命令实现
    /// </summary>
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T?> _execute;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Action<T?> execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

        public void Execute(object? parameter)
        {
            if (parameter == null)
                _execute(default);
            else if (parameter is T t)
                _execute(t);
            else
                _execute((T)Convert.ChangeType(parameter, typeof(T)));
        }
    }

    /// <summary>
    /// 非泛型命令实现
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

        public void Execute(object? parameter) => _execute();

        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }
}
