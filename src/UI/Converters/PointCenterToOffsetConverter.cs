using System;
using System.Globalization;
using System.Windows.Data;

namespace SunEyeVision.UI.Converters
{
    /// <summary>
    /// 将中心点坐标转换为Canvas偏移坐标
    /// 用途：Position属性存储中心点，但Canvas.Left/Top需要左上角坐标
    /// </summary>
    public class PointCenterToOffsetConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double centerPoint && parameter is string offsetStr)
            {
                if (double.TryParse(offsetStr, out double offset))
                {
                    return centerPoint + offset;
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
