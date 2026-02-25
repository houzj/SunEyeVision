using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SunEyeVision.UI.Converters.Path
{
    /// <summary>
    /// 点偏移转换器 - 对 Point 进行偏移
    /// </summary>
    public class PointOffsetConverter : IValueConverter
    {
        public double OffsetX { get; set; } = 0;
        public double OffsetY { get; set; } = 0;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Point point)
            {
                return new Point(point.X + OffsetX, point.Y + OffsetY);
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
