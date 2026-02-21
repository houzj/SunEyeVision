using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SunEyeVision.UI.Converters.Visibility
{
    /// <summary>
    /// 反向布尔到可见性转换器
    /// true -> Collapsed, false -> Visible
    /// </summary>
    public class InverseBoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
            }
            return System.Windows.Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is System.Windows.Visibility visibility)
            {
                return visibility != System.Windows.Visibility.Visible;
            }
            return true;
        }
    }
}
