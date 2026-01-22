using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SunEyeVision.UI.Converters
{
    /// <summary>
    /// 布尔值转运行边框转换器
    /// </summary>
    public class BoolToRunningBorderConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isRunning)
            {
                return isRunning ? new SolidColorBrush(Color.FromRgb(255, 153, 153)) : new SolidColorBrush(Color.FromRgb(0, 102, 204));
            }
            return new SolidColorBrush(Color.FromRgb(0, 102, 204));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
