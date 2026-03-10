using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
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
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // 取消订阅旧ViewModel的事件
            if (e.OldValue is RegionEditorViewModel oldViewModel)
            {
                oldViewModel.PropertyChanged -= OnViewModelPropertyChanged;
            }

            // 订阅新ViewModel的事件
            if (e.NewValue is RegionEditorViewModel newViewModel)
            {
                _viewModel = newViewModel;
                newViewModel.PropertyChanged += OnViewModelPropertyChanged;
            }
            else
            {
                _viewModel = null;
            }
        }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // 当SelectedShapeType或Parameters属性变化时，刷新绑定
            if (e.PropertyName == nameof(RegionEditorViewModel.SelectedShapeType) ||
                e.PropertyName == nameof(RegionEditorViewModel.Parameters))
            {
                InvalidateVisual();
                UpdateLayout();
            }
        }
    }

    /// <summary>
    /// Null值到可见性转换器
    /// </summary>
    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool invert = parameter?.ToString() == "Invert";
            bool isNull = value == null;
            return (isNull ^ invert) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
