using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace SunEyeVision.Plugin.SDK.UI.Controls
{
    /// <summary>
    /// 颜色系列，包含基础色和变体
    /// </summary>
    public class ColorSeries
    {
        /// <summary>
        /// 颜色系列名称
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 基础颜色
        /// </summary>
        public Color BaseColor { get; }

        /// <summary>
        /// 浅色变体（从浅到深）
        /// </summary>
        public List<Color> LighterVariants { get; }

        /// <summary>
        /// 深色变体（从深到浅）
        /// </summary>
        public List<Color> DarkerVariants { get; }

        /// <summary>
        /// 所有颜色（浅色变体 + 基础色 + 深色变体）
        /// </summary>
        public List<Color> AllColors { get; }

        public ColorSeries(string name, Color baseColor)
        {
            Name = name;
            BaseColor = baseColor;
            LighterVariants = new List<Color>();
            DarkerVariants = new List<Color>();
            AllColors = new List<Color>();

            // 生成浅色变体（3个，越来越浅）
            for (int i = 3; i >= 1; i--)
            {
                var lighter = LightenColor(baseColor, i * 0.12);
                LighterVariants.Add(lighter);
                AllColors.Add(lighter);
            }

            // 添加基础色
            AllColors.Add(baseColor);

            // 生成深色变体（2个，越来越深）
            for (int i = 1; i <= 2; i++)
            {
                var darker = DarkenColor(baseColor, i * 0.12);
                DarkerVariants.Add(darker);
                AllColors.Add(darker);
            }
        }

        /// <summary>
        /// 使颜色变浅
        /// </summary>
        private static Color LightenColor(Color color, double amount)
        {
            byte r = (byte)Math.Min(255, color.R + (255 - color.R) * amount);
            byte g = (byte)Math.Min(255, color.G + (255 - color.G) * amount);
            byte b = (byte)Math.Min(255, color.B + (255 - color.B) * amount);
            return Color.FromArgb(255, r, g, b);
        }

        /// <summary>
        /// 使颜色变深
        /// </summary>
        private static Color DarkenColor(Color color, double amount)
        {
            byte r = (byte)Math.Max(0, color.R * (1 - amount));
            byte g = (byte)Math.Max(0, color.G * (1 - amount));
            byte b = (byte)Math.Max(0, color.B * (1 - amount));
            return Color.FromArgb(255, r, g, b);
        }
    }

    /// <summary>
    /// 颜色选择器调色板，为 ColorPicker 提供可选颜色
    /// </summary>
    public static class ColorPickerPalette
    {
        /// <summary>
        /// 主题颜色系列列表（8个系列，每系列5个变体）
        /// </summary>
        public static List<ColorSeries> ColorSeriesList { get; }

        /// <summary>
        /// 标准颜色列表（8个常用颜色，无变体）
        /// </summary>
        public static List<Color> StandardColors { get; }

        static ColorPickerPalette()
        {
            // 初始化主题颜色系列（10个系列）
            ColorSeriesList = new List<ColorSeries>
            {
                new ColorSeries("深红", Color.FromRgb(0xC0, 0x00, 0x00)),
                new ColorSeries("红色", Color.FromRgb(0xFF, 0x00, 0x00)),
                new ColorSeries("橙色", Color.FromRgb(0xFF, 0xC0, 0x00)),
                new ColorSeries("黄色", Color.FromRgb(0xFF, 0xFF, 0x00)),
                new ColorSeries("浅绿", Color.FromRgb(0x92, 0xD0, 0x50)),
                new ColorSeries("绿色", Color.FromRgb(0x00, 0xB0, 0x50)),
                new ColorSeries("浅蓝", Color.FromRgb(0x00, 0xB0, 0xF0)),
                new ColorSeries("蓝色", Color.FromRgb(0x00, 0x70, 0xC0)),
                new ColorSeries("紫色", Color.FromRgb(0x70, 0x30, 0xA0)),
                new ColorSeries("品红", Color.FromRgb(0xFF, 0x00, 0xFF)),
            };

            // 初始化标准颜色（10个常用颜色）
            StandardColors = new List<Color>
            {
                Color.FromRgb(0x00, 0x00, 0x00), // 黑色
                Color.FromRgb(0x80, 0x80, 0x80), // 灰色
                Color.FromRgb(0xC0, 0xC0, 0xC0), // 银色
                Color.FromRgb(0xFF, 0xFF, 0xFF), // 白色
                Color.FromRgb(0x80, 0x00, 0x00), // 栗色
                Color.FromRgb(0x80, 0x80, 0x00), // 橄榄色
                Color.FromRgb(0x00, 0x80, 0x00), // 深绿
                Color.FromRgb(0x00, 0x80, 0x80), // 青色
                Color.FromRgb(0x00, 0x00, 0x80), // 深蓝
                Color.FromRgb(0x80, 0x00, 0x80), // 紫色
            };
        }

        /// <summary>
        /// 将 Color 转换为 uint ARGB 格式
        /// </summary>
        public static uint ColorToUInt(Color color)
        {
            return (uint)((color.A << 24) | (color.R << 16) | (color.G << 8) | color.B);
        }

        /// <summary>
        /// 将 uint ARGB 格式转换为 Color
        /// </summary>
        public static Color UIntToColor(uint argb)
        {
            byte a = (byte)((argb >> 24) & 0xFF);
            byte r = (byte)((argb >> 16) & 0xFF);
            byte g = (byte)((argb >> 8) & 0xFF);
            byte b = (byte)(argb & 0xFF);
            return Color.FromArgb(a, r, g, b);
        }
    }
}
