using System;
using System.Globalization;
using System.Windows.Data;

namespace SunEyeVision.UI.Converters
{
    /// <summary>
    /// 布尔值转选中边框粗细转换器
    /// </summary>
    public class BoolToSelectedBorderThicknessConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isSelected)
            {
                return new System.Windows.Thickness(isSelected ? 2 : 1);
            }
            return new System.Windows.Thickness(1);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
