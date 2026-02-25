using System;
using System.Globalization;
using System.Windows.Data;

namespace SunEyeVision.UI.Converters.UI
{
    /// <summary>
    /// 反向布尔转换器
    /// true -> false, false -> true
    /// </summary>
    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return value;
        }
    }
}
