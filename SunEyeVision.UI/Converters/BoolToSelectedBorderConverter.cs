using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SunEyeVision.UI.Converters
{
    /// <summary>
    /// 布尔值转选中边框转换�?
    /// </summary>
    public class BoolToSelectedBorderConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isSelected)
            {
                return isSelected ? new SolidColorBrush(Color.FromRgb(0, 102, 204)) : new SolidColorBrush(Color.FromRgb(204, 204, 204));
            }
            return new SolidColorBrush(Color.FromRgb(204, 204, 204));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
