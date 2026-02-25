using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SunEyeVision.UI.Converters
{
    /// <summary>
    /// 布尔值转运行背景转换器
    /// </summary>
    public class BoolToRunningBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isRunning)
            {
                return isRunning ? new SolidColorBrush(Color.FromRgb(255, 240, 240)) : new SolidColorBrush(Color.FromRgb(240, 248, 255));
            }
            return new SolidColorBrush(Color.FromRgb(240, 248, 255));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
