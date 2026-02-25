using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SunEyeVision.UI.Converters
{
    /// <summary>
    /// 颜色字符串到画笔转换器
    /// </summary>
    public class ColorToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string colorString && !string.IsNullOrEmpty(colorString))
            {
                try
                {
                    var color = (Color)ColorConverter.ConvertFromString(colorString);
                    return new SolidColorBrush(color);
                }
                catch
                {
                    return Brushes.Gray;
                }
            }
            return Brushes.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SolidColorBrush brush)
            {
                return brush.Color.ToString();
            }
            return null;
        }
    }
}
