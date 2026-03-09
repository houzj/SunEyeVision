using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using SunEyeVision.Plugin.SDK.Logging;
using SunEyeVision.Plugin.SDK.UI.Controls.Region.ViewModels;

namespace SunEyeVision.Plugin.SDK.UI.Controls.Region.Views
{
    /// <summary>
    /// 绘制参数面板
    /// </summary>
    public partial class DrawingParameterPanel : UserControl
    {
        private RegionEditorViewModel? _viewModel;

        public DrawingParameterPanel()
        {
            PluginLogger.Info($"[DrawingParameterPanel] 构造函数被调用", "UI");
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
            Loaded += OnLoaded;
            IsVisibleChanged += OnIsVisibleChanged;
            PluginLogger.Info($"[DrawingParameterPanel] 构造函数完成，初始化组件", "UI");
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            PluginLogger.Info($"[DrawingParameterPanel] Loaded 事件触发，面板已加载", "UI");

            // 尝试强制刷新绑定
            InvalidateVisual();
            UpdateLayout();

            LogBindingInfo();
            CheckParametersBinding();
        }

        private void CheckParametersBinding()
        {
            PluginLogger.Info($"[DrawingParameterPanel] ========== 检查Parameters绑定 ==========", "UI");

            if (DataContext is RegionEditorViewModel viewModel)
            {
                PluginLogger.Info($"[DrawingParameterPanel] DataContext类型正确: RegionEditorViewModel", "UI");

                var parameters = viewModel.Parameters;
                PluginLogger.Info($"[DrawingParameterPanel] Parameters={parameters?.ShapeType.ToString() ?? "null"}, Hash={parameters?.GetHashCode() ?? 0}", "UI");

                if (parameters != null)
                {
                    PluginLogger.Info($"[DrawingParameterPanel] Parameters实现了INotifyPropertyChanged: {parameters is INotifyPropertyChanged}", "UI");

                    // 尝试手动触发属性变化
                    PluginLogger.Info($"[DrawingParameterPanel] 尝试手动触发Parameters属性变化", "UI");

                    var oldX = parameters.CenterX;
                    parameters.CenterX = oldX + 0.001;
                    parameters.CenterX = oldX;

                    PluginLogger.Info($"[DrawingParameterPanel] ✓ Parameters属性已触发变化", "UI");
                }
            }
            else
            {
                PluginLogger.Warning($"[DrawingParameterPanel] DataContext类型不正确: {DataContext?.GetType().Name ?? "null"}", "UI");
            }

            PluginLogger.Info($"[DrawingParameterPanel] =========================================", "UI");
        }

        private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            try
            {
                // 添加类型检查，防止初始化阶段的类型错误
                Visibility newVisibility;
                if (e.NewValue is Visibility)
                {
                    newVisibility = (Visibility)e.NewValue;
                }
                else if (e.NewValue is bool boolValue)
                {
                    // 处理bool值（在初始化阶段可能出现）
                    newVisibility = boolValue ? Visibility.Visible : Visibility.Collapsed;
                    PluginLogger.Warning($"[DrawingParameterPanel] ⚠️ IsVisibleChanged 收到bool值: {boolValue}, 已转换为 {newVisibility}", "UI");
                }
                else
                {
                    PluginLogger.Error($"[DrawingParameterPanel] ⚠️ IsVisibleChanged 收到未知类型: {e.NewValue?.GetType().Name ?? "null"}, 值: {e.NewValue}", "UI");
                    return;
                }

                PluginLogger.Info($"[DrawingParameterPanel] IsVisibleChanged: NewValue={newVisibility}", "UI");

                if (newVisibility == Visibility.Visible)
                {
                    LogBindingInfo();
                }
            }
            catch (Exception ex)
            {
                PluginLogger.Error($"[DrawingParameterPanel] OnIsVisibleChanged 发生异常: {ex.Message}", "UI", ex);
                PluginLogger.Error($"[DrawingParameterPanel] e.NewValue={e.NewValue}, e.NewValue.GetType()={e.NewValue?.GetType().Name ?? "null"}", "UI");
            }
        }

        private void OnDataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            var newDataContext = e.NewValue;
            var newDataContextType = newDataContext?.GetType().Name ?? "null";
            PluginLogger.Info($"[DrawingParameterPanel] DataContextChanged: OldValue={e.OldValue?.GetType().Name ?? "null"}, NewValue={newDataContextType}", "UI");

            // 取消订阅旧ViewModel的事件
            if (e.OldValue is RegionEditorViewModel oldViewModel)
            {
                oldViewModel.PropertyChanged -= OnViewModelPropertyChanged;
                PluginLogger.Info($"[DrawingParameterPanel] 已取消订阅旧ViewModel的PropertyChanged事件", "UI");
            }

            // 订阅新ViewModel的事件
            if (newDataContext is RegionEditorViewModel newViewModel)
            {
                _viewModel = newViewModel;
                newViewModel.PropertyChanged += OnViewModelPropertyChanged;
                PluginLogger.Info($"[DrawingParameterPanel] 已订阅新ViewModel的PropertyChanged事件", "UI");

                var selectedShapeTypeProperty = newDataContext.GetType().GetProperty("SelectedShapeType");
                if (selectedShapeTypeProperty != null)
                {
                    var value = selectedShapeTypeProperty.GetValue(newDataContext);
                    PluginLogger.Info($"[DrawingParameterPanel] ✓ SelectedShapeType 属性存在，值={value}", "UI");
                }
                else
                {
                    PluginLogger.Warning($"[DrawingParameterPanel] ⚠️ SelectedShapeType 属性不存在", "UI");
                }
            }
            else
            {
                _viewModel = null;
            }
        }

        private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            PluginLogger.Info($"[DrawingParameterPanel] PropertyChanged 事件: propertyName={e.PropertyName}", "UI");

            // 当SelectedShapeType或Parameters属性变化时，刷新绑定
            if (e.PropertyName == nameof(RegionEditorViewModel.SelectedShapeType) ||
                e.PropertyName == nameof(RegionEditorViewModel.Parameters))
            {
                PluginLogger.Info($"[DrawingParameterPanel] 属性 {e.PropertyName} 变化，刷新绑定", "UI");
                
                // 强制刷新绑定
                InvalidateVisual();
                UpdateLayout();

                PluginLogger.Info($"[DrawingParameterPanel] 绑定已刷新", "UI");
            }
        }

        private void LogBindingInfo()
        {
            PluginLogger.Info($"[DrawingParameterPanel] ========== 绑定信息 ==========", "UI");
            PluginLogger.Info($"[DrawingParameterPanel] Visibility={Visibility}", "UI");
            PluginLogger.Info($"[DrawingParameterPanel] DataContext={DataContext?.GetType().Name ?? "null"}", "UI");

            if (DataContext != null)
            {
                var selectedShapeTypeProperty = DataContext.GetType().GetProperty("SelectedShapeType");
                if (selectedShapeTypeProperty != null)
                {
                    var selectedShapeType = selectedShapeTypeProperty.GetValue(DataContext);
                    PluginLogger.Info($"[DrawingParameterPanel] SelectedShapeType={selectedShapeType}", "UI");
                }

                var parametersProperty = DataContext.GetType().GetProperty("Parameters");
                if (parametersProperty != null)
                {
                    var parameters = parametersProperty.GetValue(DataContext);
                    PluginLogger.Info($"[DrawingParameterPanel] Parameters={parameters?.ToString() ?? "null"}", "UI");
                }
            }
            PluginLogger.Info($"[DrawingParameterPanel] ================================", "UI");
        }
    }

    /// <summary>
    /// Null值到可见性转换器
    /// </summary>
    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                PluginLogger.Info($"[NullToVisibilityConverter] ══════════════════════════════════════════════", "UI");
                PluginLogger.Info($"[NullToVisibilityConverter] Convert 开始执行", "UI");
                PluginLogger.Info($"[NullToVisibilityConverter] value={value?.ToString() ?? "null"}, value.GetType()={value?.GetType().Name ?? "null"}", "UI");
                PluginLogger.Info($"[NullToVisibilityConverter] parameter={parameter?.ToString() ?? "null"}, targetType={targetType?.Name ?? "null"}", "UI");

                bool invert = parameter?.ToString() == "Invert";
                PluginLogger.Info($"[NullToVisibilityConverter] invert={invert}", "UI");

                bool isNull = value == null;
                PluginLogger.Info($"[NullToVisibilityConverter] isNull={isNull}", "UI");

                // 如果invert=true，则null返回Visible，否则返回Collapsed
                // 如果invert=false，则null返回Collapsed，否则返回Visible
                var result = (isNull ^ invert) ? Visibility.Collapsed : Visibility.Visible;
                
                PluginLogger.Info($"[NullToVisibilityConverter] 最终结果: {result} (isNull={isNull}, invert={invert})", "UI");
                PluginLogger.Info($"[NullToVisibilityConverter] ══════════════════════════════════════════════", "UI");
                
                return result;
            }
            catch (Exception ex)
            {
                PluginLogger.Error($"[NullToVisibilityConverter] ══════════════════════════════════════════════", "UI");
                PluginLogger.Error($"[NullToVisibilityConverter] ❌ Convert 发生异常: {ex.Message}", "UI", ex);
                PluginLogger.Error($"[NullToVisibilityConverter] 异常类型: {ex.GetType().Name}", "UI");
                PluginLogger.Error($"[NullToVisibilityConverter] 堆栈跟踪:\n{ex.StackTrace}", "UI");
                PluginLogger.Error($"[NullToVisibilityConverter] ══════════════════════════════════════════════", "UI");
                
                // 发生异常时返回 Visible 作为默认值
                return Visibility.Visible;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                PluginLogger.Info($"[NullToVisibilityConverter] ConvertBack 开始执行", "UI");
                PluginLogger.Info($"[NullToVisibilityConverter] value={value?.ToString() ?? "null"}, targetType={targetType?.Name ?? "null"}", "UI");
                
                throw new NotImplementedException();
            }
            catch (Exception ex)
            {
                PluginLogger.Error($"[NullToVisibilityConverter] ❌ ConvertBack 发生异常: {ex.Message}", "UI", ex);
                throw;
            }
        }
    }

    /// <summary>
    /// 形状类型到可见性的转换器
    /// </summary>
    public class ShapeTypeToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                var result = Visibility.Collapsed;

                PluginLogger.Info($"[ShapeTypeToVisibilityConverter] ══════════════════════════════════════════════", "UI");
                PluginLogger.Info($"[ShapeTypeToVisibilityConverter] Convert 开始执行", "UI");
                PluginLogger.Info($"[ShapeTypeToVisibilityConverter] value={value?.ToString() ?? "null"}, value.GetType()={value?.GetType().Name ?? "null"}", "UI");
                PluginLogger.Info($"[ShapeTypeToVisibilityConverter] parameter={parameter?.ToString() ?? "null"}, targetType={targetType?.Name ?? "null"}", "UI");

                // 处理 null 值
                if (value == null)
                {
                    PluginLogger.Info($"[ShapeTypeToVisibilityConverter] ❌ value 为 null，返回 Collapsed", "UI");
                    PluginLogger.Info($"[ShapeTypeToVisibilityConverter] ══════════════════════════════════════════════", "UI");
                    return Visibility.Collapsed;
                }

                // 添加类型检测，防止bool值被错误传递
                if (value is bool)
                {
                    PluginLogger.Error($"[ShapeTypeToVisibilityConverter] ⚠️ 严重错误：收到bool类型值，而不是ShapeType!", "UI");
                    PluginLogger.Error($"[ShapeTypeToVisibilityConverter] value={value}, 实际类型={value.GetType().Name}", "UI");
                    PluginLogger.Error($"[ShapeTypeToVisibilityConverter] 堆栈跟踪: {Environment.StackTrace}", "UI");
                    PluginLogger.Info($"[ShapeTypeToVisibilityConverter] ══════════════════════════════════════════════", "UI");
                    return Visibility.Collapsed;
                }

                // 处理枚举值（包括可空枚举）
                Models.ShapeType? shapeType = null;
                bool isNullable = false;

                // 尝试直接获取 ShapeType（非可空）
                if (value is Models.ShapeType nonNullableShapeType)
                {
                    shapeType = nonNullableShapeType;
                    isNullable = false;
                    PluginLogger.Info($"[ShapeTypeToVisibilityConverter] ✅ 检测到非可空 ShapeType: {shapeType.Value}", "UI");
                }
                // 尝试从可空枚举获取
                else if (value is Enum enumValue)
                {
                    var valueType = enumValue.GetType();
                    PluginLogger.Info($"[ShapeTypeToVisibilityConverter] 检测到枚举类型: {valueType.Name}, IsGenericType={valueType.IsGenericType}", "UI");

                    if (valueType == typeof(Models.ShapeType))
                    {
                        shapeType = (Models.ShapeType)enumValue;
                        isNullable = false;
                        PluginLogger.Info($"[ShapeTypeToVisibilityConverter] ✅ 从 ShapeType 枚举获取: {shapeType.Value}", "UI");
                    }
                    else if (valueType == typeof(Models.ShapeType?) || 
                             (valueType.IsGenericType && valueType.GetGenericTypeDefinition() == typeof(Nullable<>) &&
                              valueType.GetGenericArguments()[0] == typeof(Models.ShapeType)))
                    {
                        shapeType = (Models.ShapeType)enumValue;
                        isNullable = true;
                        PluginLogger.Info($"[ShapeTypeToVisibilityConverter] ✅ 从可空 ShapeType? 枚举获取: {shapeType.Value}", "UI");
                    }
                    else
                    {
                        PluginLogger.Error($"[ShapeTypeToVisibilityConverter] ⚠️ 收到错误的枚举类型: {valueType.FullName}", "UI");
                        PluginLogger.Error($"[ShapeTypeToVisibilityConverter] 期望类型: ShapeType 或 ShapeType?", "UI");
                        PluginLogger.Info($"[ShapeTypeToVisibilityConverter] ══════════════════════════════════════════════", "UI");
                        return Visibility.Collapsed;
                    }
                }
                else
                {
                    PluginLogger.Error($"[ShapeTypeToVisibilityConverter] ⚠️ 收到不支持的类型: {value.GetType().FullName}", "UI");
                    PluginLogger.Error($"[ShapeTypeToVisibilityConverter] 期望类型: ShapeType, ShapeType? 或 null", "UI");
                    PluginLogger.Info($"[ShapeTypeToVisibilityConverter] ══════════════════════════════════════════════", "UI");
                    return Visibility.Collapsed;
                }

                // 如果成功获取形状类型且参数是字符串
                if (shapeType.HasValue && parameter is string types)
                {
                    PluginLogger.Info($"[ShapeTypeToVisibilityConverter] 开始匹配: shapeType={shapeType.Value}, types参数='{types}'", "UI");
                    
                    var typeList = types.Split(',');
                    PluginLogger.Info($"[ShapeTypeToVisibilityConverter] 匹配列表: [{string.Join(", ", typeList.Select(t => t.Trim()))}]", "UI");
                    
                    foreach (var type in typeList)
                    {
                        var trimmedType = type.Trim();
                        PluginLogger.Info($"[ShapeTypeToVisibilityConverter]   尝试匹配: '{trimmedType}'", "UI");
                        
                        if (Enum.TryParse<Models.ShapeType>(trimmedType, out var t))
                        {
                            if (shapeType.Value == t)
                            {
                                result = Visibility.Visible;
                                PluginLogger.Info($"[ShapeTypeToVisibilityConverter] ✅ 匹配成功: {shapeType.Value} == {t}", "UI");
                                PluginLogger.Info($"[ShapeTypeToVisibilityConverter] ✅ 最终结果: {result}", "UI");
                                PluginLogger.Info($"[ShapeTypeToVisibilityConverter] ══════════════════════════════════════════════", "UI");
                                break;
                            }
                            else
                            {
                                PluginLogger.Info($"[ShapeTypeToVisibilityConverter]   不匹配: {shapeType.Value} != {t}", "UI");
                            }
                        }
                        else
                        {
                            PluginLogger.Warning($"[ShapeTypeToVisibilityConverter] ⚠️ 无法解析枚举值: '{trimmedType}'", "UI");
                        }
                    }
                }
                else
                {
                    if (!shapeType.HasValue)
                    {
                        PluginLogger.Warning($"[ShapeTypeToVisibilityConverter] ⚠️ shapeType 为 null，无法进行匹配", "UI");
                    }
                    if (!(parameter is string))
                    {
                        PluginLogger.Warning($"[ShapeTypeToVisibilityConverter] ⚠️ parameter 不是 string 类型: {parameter?.GetType().Name ?? "null"}", "UI");
                    }
                }

                PluginLogger.Info($"[ShapeTypeToVisibilityConverter] 最终返回: {result}", "UI");
                PluginLogger.Info($"[ShapeTypeToVisibilityConverter] ══════════════════════════════════════════════", "UI");
                return result;
            }
            catch (Exception ex)
            {
                PluginLogger.Error($"[ShapeTypeToVisibilityConverter] ══════════════════════════════════════════════", "UI");
                PluginLogger.Error($"[ShapeTypeToVisibilityConverter] ❌ Convert 发生异常: {ex.Message}", "UI", ex);
                PluginLogger.Error($"[ShapeTypeToVisibilityConverter] 异常类型: {ex.GetType().Name}", "UI");
                PluginLogger.Error($"[ShapeTypeToVisibilityConverter] 堆栈跟踪:\n{ex.StackTrace}", "UI");
                PluginLogger.Error($"[ShapeTypeToVisibilityConverter] ══════════════════════════════════════════════", "UI");
                return Visibility.Collapsed;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
