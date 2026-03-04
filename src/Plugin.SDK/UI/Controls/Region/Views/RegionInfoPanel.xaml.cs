using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SunEyeVision.Plugin.SDK.UI.Controls.Region.Views
{
    /// <summary>
    /// 区域信息面板
    /// </summary>
    public partial class RegionInfoPanel : UserControl
    {
        public RegionInfoPanel()
        {
            // 设计时跳过初始化
            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
            {
                InitializeComponent();
                return;
            }

            InitializeComponent();
        }

        private void SelectColorButton_Click(object sender, RoutedEventArgs e)
        {
            // 简化实现：使用预设颜色列表
            // 实际项目中应该使用专业的颜色选择器控件
            if (DataContext is ViewModels.RegionEditorViewModel viewModel && 
                viewModel.SelectedRegion != null)
            {
                // 默认设置为红色
                viewModel.SelectedRegion.DisplayColor = 0xFFFF0000;
            }
        }
    }

    /// <summary>
    /// 形状类型到可见性的转换器
    /// </summary>
    public class ShapeTypeToVisibilityConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is Models.ShapeType shapeType && parameter is string types)
            {
                var typeList = types.Split(',');
                foreach (var type in typeList)
                {
                    if (Enum.TryParse<Models.ShapeType>(type.Trim(), out var t) && shapeType == t)
                        return Visibility.Visible;
                }
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 反向布尔值到可见性的转换器（true=Collapsed, false=Visible）
    /// </summary>
    public class InverseBooleanToVisibilityConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool b)
                return b ? Visibility.Collapsed : Visibility.Visible;
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is Visibility v)
                return v != Visibility.Visible;
            return false;
        }
    }
}
