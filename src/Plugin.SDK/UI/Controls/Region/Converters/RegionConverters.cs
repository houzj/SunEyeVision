using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
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
            if (value is bool b)
            {
                return b ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                return visibility == Visibility.Visible;
            }
            return false;
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
