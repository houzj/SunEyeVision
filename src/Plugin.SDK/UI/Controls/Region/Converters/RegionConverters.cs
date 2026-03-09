using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using SunEyeVision.Plugin.SDK.Logging;
using SunEyeVision.Plugin.SDK.UI.Controls.Region.Models;

namespace SunEyeVision.Plugin.SDK.UI.Controls.Region.Converters
{
    /// <summary>
    /// 形状类型到图标转换器
    /// </summary>
    public class ShapeTypeToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ShapeType shapeType)
            {
                return shapeType switch
                {
                    ShapeType.Point => Geometry.Parse("M 12,2 L 12,2"), // 点图标
                    ShapeType.Line => Geometry.Parse("M 2,22 L 22,2"), // 线图标
                    ShapeType.Circle => Geometry.Parse("M 12,2 A 10,10 0 1 1 12,22 A 10,10 0 1 1 12,2"), // 圆图标
                    ShapeType.Rectangle => Geometry.Parse("M 2,2 L 22,2 L 22,22 L 2,22 Z"), // 矩形图标
                    ShapeType.RotatedRectangle => Geometry.Parse("M 12,2 L 22,12 L 12,22 L 2,12 Z"), // 旋转矩形图标
                    ShapeType.Polygon => Geometry.Parse("M 12,2 L 20,8 L 18,18 L 6,18 L 4,8 Z"), // 多边形图标
                    _ => Geometry.Empty
                };
            }
            return Geometry.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 布尔值到可见性转换器
    /// </summary>
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                PluginLogger.Info($"[BoolToVisibilityConverter] Convert 被调用: value={value?.ToString() ?? "null"}, value.GetType()={value?.GetType().Name ?? "null"}, targetType={targetType?.Name ?? "null"}", "UI");

                if (value is bool b)
                {
                    var result = b ? Visibility.Visible : Visibility.Collapsed;
                    PluginLogger.Info($"[BoolToVisibilityConverter] 返回: {result}", "UI");
                    return result;
                }

                PluginLogger.Warning($"[BoolToVisibilityConverter] value 不是 bool 类型，返回默认值 Collapsed", "UI");
                return Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                PluginLogger.Error($"[BoolToVisibilityConverter] Convert 发生异常: {ex.Message}\n{ex.StackTrace}", "UI");
                return Visibility.Collapsed;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                PluginLogger.Info($"[BoolToVisibilityConverter] ConvertBack 被调用: value={value?.ToString() ?? "null"}, value.GetType()={value?.GetType().Name ?? "null"}, targetType={targetType?.Name ?? "null"}", "UI");

                if (value is Visibility visibility)
                {
                    var result = visibility == Visibility.Visible;
                    PluginLogger.Info($"[BoolToVisibilityConverter] ConvertBack 返回: {result}", "UI");
                    return result;
                }

                PluginLogger.Warning($"[BoolToVisibilityConverter] value 不是 Visibility 类型，返回默认值 false", "UI");
                return false;
            }
            catch (Exception ex)
            {
                PluginLogger.Error($"[BoolToVisibilityConverter] ConvertBack 发生异常: {ex.Message}\n{ex.StackTrace}", "UI");
                return false;
            }
        }
    }

    /// <summary>
    /// 模式到布尔值转换器
    /// </summary>
    public class ModeToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is RegionDefinitionMode mode && parameter is string targetMode)
            {
                return mode.ToString() == targetMode;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b && b && parameter is string targetMode)
            {
                return Enum.Parse(typeof(RegionDefinitionMode), targetMode);
            }
            return DependencyProperty.UnsetValue;
        }
    }

    /// <summary>
    /// 形状类型到布尔值转换器（用于RadioButton绑定）
    /// </summary>
    public class ShapeTypeToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ShapeType shapeType && parameter is ShapeType targetType2)
            {
                return shapeType == targetType2;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b && b && parameter is ShapeType targetShapeType)
            {
                return targetShapeType;
            }
            return DependencyProperty.UnsetValue;
        }
    }
}
