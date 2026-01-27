using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SunEyeVision.UI.Converters
{
    /// <summary>
    /// 点坐标偏移转换器
    /// 用于将点的X或Y坐标减去一个偏移值，用于定位椭圆的中心
    /// </summary>
    public class PointOffsetConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double coordinate && parameter is string offsetStr)
            {
                if (double.TryParse(offsetStr, out double offset))
                {
                    return coordinate + offset;
                }
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
