using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SunEyeVision.UI.Converters
{
    /// <summary>
    /// 布尔值转激活状态转换器
    /// 用于显示运行状态的绿色指示器
    /// </summary>
    public class BoolToActiveConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isActive)
            {
                // 运行时显示绿色，停止时显示灰色
                return isActive ? new SolidColorBrush(Color.FromRgb(76, 175, 80)) : new SolidColorBrush(Color.FromRgb(158, 158, 158));
            }
            return new SolidColorBrush(Color.FromRgb(158, 158, 158));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
