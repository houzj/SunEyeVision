using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
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
            PluginLogger.Info($"[DrawingParameterPanel] 构造函数被调用, HashCode={this.GetHashCode()}", "UI");
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
            Loaded += OnLoaded;
            IsVisibleChanged += OnIsVisibleChanged;
            PluginLogger.Info($"[DrawingParameterPanel] 构造函数完成，初始化组件, HashCode={this.GetHashCode()}", "UI");
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
            PluginLogger.Info($"[DrawingParameterPanel] DataContextChanged: HashCode={this.GetHashCode()}, OldValue={e.OldValue?.GetType().Name ?? "null"}, NewValue={newDataContextType}", "UI");

            // 取消订阅旧ViewModel的事件
            if (e.OldValue is RegionEditorViewModel oldViewModel)
            {
                oldViewModel.PropertyChanged -= OnViewModelPropertyChanged;
                PluginLogger.Info($"[DrawingParameterPanel] 已取消订阅旧ViewModel(HashCode={oldViewModel.GetHashCode()})的PropertyChanged事件", "UI");
            }

            // 订阅新ViewModel的事件
            if (newDataContext is RegionEditorViewModel newViewModel)
            {
                _viewModel = newViewModel;
                PluginLogger.Info($"[DrawingParameterPanel] 准备订阅新ViewModel(HashCode={newViewModel.GetHashCode()})的PropertyChanged事件", "UI");
                newViewModel.PropertyChanged += OnViewModelPropertyChanged;
                PluginLogger.Info($"[DrawingParameterPanel] 已订阅新ViewModel(HashCode={newViewModel.GetHashCode()})的PropertyChanged事件", "UI");

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
            PluginLogger.Info($"[DrawingParameterPanel] PropertyChanged 事件触发: propertyName={e.PropertyName}, sender.HashCode={sender?.GetHashCode()}, sender.Type={sender?.GetType().Name}, DrawingParameterPanel.HashCode={this.GetHashCode()}", "UI");

            // 当SelectedShapeType或Parameters属性变化时，刷新绑定
            if (e.PropertyName == nameof(RegionEditorViewModel.SelectedShapeType) ||
                e.PropertyName == nameof(RegionEditorViewModel.Parameters))
            {
                PluginLogger.Info($"[DrawingParameterPanel] 属性 {e.PropertyName} 变化，刷新绑定", "UI");

                if (_viewModel != null)
                {
                    PluginLogger.Info($"[DrawingParameterPanel] _viewModel.SelectedShapeType={_viewModel.SelectedShapeType}", "UI");
                    PluginLogger.Info($"[DrawingParameterPanel] _viewModel.Parameters={_viewModel.Parameters?.ShapeType.ToString() ?? "null"}", "UI");
                }

                // 强制刷新绑定
                InvalidateVisual();
                UpdateLayout();

                PluginLogger.Info($"[DrawingParameterPanel] 绑定已刷新", "UI");
            }
        }

        private void LogBindingInfo()
        {
            PluginLogger.Info($"[DrawingParameterPanel] ========== 绑定信息 ==========", "UI");
            PluginLogger.Info($"[DrawingParameterPanel] DrawingParameterPanel.Visibility={Visibility}", "UI");
            PluginLogger.Info($"[DrawingParameterPanel] DrawingParameterPanel.DataContext={DataContext?.GetType().Name ?? "null"}", "UI");
            PluginLogger.Info($"[DrawingParameterPanel] DrawingParameterPanel.ActualWidth={ActualWidth}, ActualHeight={ActualHeight}", "UI");
            PluginLogger.Info($"[DrawingParameterPanel] DrawingParameterPanel.Visibility={Visibility}, IsVisible={IsVisible}", "UI");

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
}
