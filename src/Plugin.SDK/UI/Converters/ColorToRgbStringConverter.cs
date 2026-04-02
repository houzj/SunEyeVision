using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SunEyeVision.Plugin.SDK.UI.Converters
{
    /// <summary>
    /// Color 到 RGB 字符串的转换器
    /// </summary>
    /// <remarks>
    /// 用于将 WPF Color 类型转换为 RGB 字符串显示。
    /// 输出格式：R=255, G=128, B=0
    /// 
    /// 使用示例：
    /// <code>
    /// &lt;TextBlock Text="{Binding Color, Converter={StaticResource ColorToRgbStringConverter}}" /&gt;
    /// </code>
    /// </remarks>
    public class ColorToRgbStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Color color)
            {
                return $"R={color.R}, G={color.G}, B={color.B}";
            }

            return "R=0, G=0, B=0";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("不支持从 RGB 字符串反向转换");
        }
    }
}
