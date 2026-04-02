using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SunEyeVision.Plugin.SDK.UI.Converters
{
    /// <summary>
    /// uint ARGB 值到 SolidColorBrush 的转换器
    /// </summary>
    /// <remarks>
    /// 用于将 uint 类型的 ARGB 颜色值转换为 WPF SolidColorBrush 类型。
    /// 
    /// 使用示例：
    /// <code>
    /// &lt;Border Background="{Binding DisplayColor, Converter={StaticResource UIntToBrushConverter}}" /&gt;
    /// </code>
    /// </remarks>
    public class UIntToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is uint uintValue)
            {
                byte a = (byte)((uintValue >> 24) & 0xFF);
                byte r = (byte)((uintValue >> 16) & 0xFF);
                byte g = (byte)((uintValue >> 8) & 0xFF);
                byte b = (byte)(uintValue & 0xFF);

                return new SolidColorBrush(Color.FromArgb(a, r, g, b));
            }

            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("不支持从 Brush 反向转换");
        }
    }
}
