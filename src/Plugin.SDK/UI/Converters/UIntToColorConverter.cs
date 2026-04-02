using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SunEyeVision.Plugin.SDK.UI.Converters
{
    /// <summary>
    /// uint ARGB 值到 Color 的转换器
    /// </summary>
    /// <remarks>
    /// 用于将 uint 类型的 ARGB 颜色值转换为 WPF Color 类型。
    /// 支持 uint ↔ Color 双向转换。
    /// 
    /// 使用示例：
    /// <code>
    /// &lt;controls:ColorPicker SelectedColor="{Binding DisplayColor, Converter={StaticResource UIntToColorConverter}}" /&gt;
    /// </code>
    /// </remarks>
    public class UIntToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is uint uintValue)
            {
                byte a = (byte)((uintValue >> 24) & 0xFF);
                byte r = (byte)((uintValue >> 16) & 0xFF);
                byte g = (byte)((uintValue >> 8) & 0xFF);
                byte b = (byte)(uintValue & 0xFF);

                return Color.FromArgb(a, r, g, b);
            }

            return Colors.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Color color)
            {
                return (uint)((color.A << 24) | (color.R << 16) | (color.G << 8) | color.B);
            }

            return 0x00000000;
        }
    }
}
