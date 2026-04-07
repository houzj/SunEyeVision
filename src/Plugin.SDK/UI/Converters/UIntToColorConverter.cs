using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using SunEyeVision.Plugin.SDK.Logging;

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
            // 源是 Color，目标是 uint
            if (value is Color color)
            {
                uint result = (uint)((color.A << 24) | (color.R << 16) | (color.G << 8) | color.B);
                PluginLogger.Info($"Convert: Color({color.A},{color.R},{color.G},{color.B}) → uint(0x{result:X8})", "UIntToColorConverter");
                return result;
            }
            else
            {
                PluginLogger.Warning($"Convert: 未知源类型 {value?.GetType().Name}", "UIntToColorConverter");
            }

            return 0xFF000000; // 默认黑色（不透明）
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 源是 uint，目标是 Color
            if (value is uint uintValue)
            {
                byte a = (byte)((uintValue >> 24) & 0xFF);
                byte r = (byte)((uintValue >> 16) & 0xFF);
                byte g = (byte)((uintValue >> 8) & 0xFF);
                byte b = (byte)(uintValue & 0xFF);

                Color result = Color.FromArgb(a, r, g, b);
                PluginLogger.Info($"ConvertBack: uint(0x{uintValue:X8}) → Color({a},{r},{g},{b})", "UIntToColorConverter");
                return result;
            }
            else
            {
                PluginLogger.Warning($"ConvertBack: 未知源类型 {value?.GetType().Name}", "UIntToColorConverter");
            }

            return Colors.Transparent;
        }
    }
}
