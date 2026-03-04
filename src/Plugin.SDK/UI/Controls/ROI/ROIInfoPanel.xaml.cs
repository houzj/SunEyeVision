using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace SunEyeVision.Plugin.SDK.UI.Controls.ROI
{
    /// <summary>
    /// ROI信息面板控件
    /// </summary>
    public partial class ROIInfoPanel : UserControl
    {
        private ROIInfoViewModel? _viewModel;

        /// <summary>
        /// 视图模型
        /// </summary>
        public ROIInfoViewModel? ViewModel
        {
            get => _viewModel;
            set
            {
                if (_viewModel != null)
                {
                    _viewModel.ROIPropertyChanged -= OnROIPropertyChanged;
                }

                _viewModel = value;
                DataContext = _viewModel;

                if (_viewModel != null)
                {
                    _viewModel.ROIPropertyChanged += OnROIPropertyChanged;
                }
            }
        }

        /// <summary>
        /// 编辑器设置（用于绑定显示设置控件）
        /// </summary>
        public ROIEditorSettings? EditorSettings
        {
            get => (ROIEditorSettings?)GetValue(EditorSettingsProperty);
            set => SetValue(EditorSettingsProperty, value);
        }

        /// <summary>
        /// EditorSettings依赖属性
        /// </summary>
        public static readonly DependencyProperty EditorSettingsProperty =
            DependencyProperty.Register(nameof(EditorSettings), typeof(ROIEditorSettings), typeof(ROIInfoPanel),
                new PropertyMetadata(null, OnEditorSettingsChanged));

        private static void OnEditorSettingsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // 设置变更时的处理（可选）
        }

        public ROIInfoPanel()
        {
            // 设计时跳过资源加载
            if (!System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
            {
                // 在运行时加载资源字典
                try
                {
                    var resourceDict = new ResourceDictionary
                    {
                        Source = new Uri("pack://application:,,,/SunEyeVision.Plugin.SDK;component/UI/Themes/Generic.xaml", UriKind.Absolute)
                    };
                    Resources.MergedDictionaries.Add(resourceDict);
                }
                catch
                {
                    // 忽略资源加载失败
                }
            }

            InitializeComponent();
        }

        /// <summary>
        /// ROI属性变更处理 - 通知编辑器刷新
        /// </summary>
        private void OnROIPropertyChanged(object? sender, ROIPropertyChangedEventArgs e)
        {
            // 触发编辑器可视化刷新
            // 注意：实际的刷新由ViewModel直接调用编辑器的InvalidateVisual
        }
    }

    /// <summary>
    /// ROI类型到可见性的转换器
    /// </summary>
    public class ROITypeToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ROIType type && parameter is string param)
            {
                switch (param)
                {
                    case "Rectangle":
                        return type == ROIType.Rectangle || type == ROIType.RotatedRectangle
                            ? Visibility.Visible
                            : Visibility.Collapsed;
                    case "RotatedRectangle":
                        return type == ROIType.RotatedRectangle
                            ? Visibility.Visible
                            : Visibility.Collapsed;
                    case "Circle":
                        return type == ROIType.Circle
                            ? Visibility.Visible
                            : Visibility.Collapsed;
                    case "Line":
                        return type == ROIType.Line
                            ? Visibility.Visible
                            : Visibility.Collapsed;
                    case "NotLine":
                        return type != ROIType.Line
                            ? Visibility.Visible
                            : Visibility.Collapsed;
                    default:
                        return Visibility.Collapsed;
                }
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 布尔值到可见性的转换器
    /// </summary>
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b && b)
                return Visibility.Visible;
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is Visibility v && v == Visibility.Visible;
        }
    }

    /// <summary>
    /// 反转布尔值到可见性的转换器
    /// </summary>
    public class InverseBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b && !b)
                return Visibility.Visible;
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is Visibility v && v != Visibility.Visible;
        }
    }
}
