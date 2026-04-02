using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SunEyeVision.Plugin.SDK.UI.Converters
{
    /// <summary>
    /// 反向布尔值到可见性的转换器（true=Collapsed, false=Visible）
    /// </summary>
    /// <remarks>
    /// 通用转换器，适用于所有需要反向布尔可见性控制的场景。
    /// 
    /// 使用示例：
    /// <code>
    /// &lt;TextBlock Visibility="{Binding IsEmpty, Converter={StaticResource InverseBooleanToVisibilityConverter}}" /&gt;
    /// </code>
    /// </remarks>
    public class InverseBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
            {
                return b ? Visibility.Collapsed : Visibility.Visible;
            }
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility v)
            {
                return v != Visibility.Visible;
            }
            return false;
        }
    }
}
