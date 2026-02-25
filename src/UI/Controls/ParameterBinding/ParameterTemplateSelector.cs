using System;
using System.Windows;
using System.Windows.Controls;
using SunEyeVision.UI.ViewModels;

namespace SunEyeVision.UI.Controls.ParameterBinding
{
    /// <summary>
    /// 参数模板选择器
    /// </summary>
    /// <remarks>
    /// 根据参数类型选择合适的数据模板。
    /// 支持常见类型：int, double, string, bool, enum, Point, Size 等。
    /// </remarks>
    public class ParameterTemplateSelector : DataTemplateSelector
    {
        /// <summary>
        /// 整数类型模板
        /// </summary>
        public DataTemplate? IntegerTemplate { get; set; }

        /// <summary>
        /// 浮点类型模板
        /// </summary>
        public DataTemplate? DoubleTemplate { get; set; }

        /// <summary>
        /// 字符串类型模板
        /// </summary>
        public DataTemplate? StringTemplate { get; set; }

        /// <summary>
        /// 布尔类型模板
        /// </summary>
        public DataTemplate? BooleanTemplate { get; set; }

        /// <summary>
        /// 枚举类型模板
        /// </summary>
        public DataTemplate? EnumTemplate { get; set; }

        /// <summary>
        /// 点类型模板
        /// </summary>
        public DataTemplate? PointTemplate { get; set; }

        /// <summary>
        /// 尺寸类型模板
        /// </summary>
        public DataTemplate? SizeTemplate { get; set; }

        /// <summary>
        /// 矩形类型模板
        /// </summary>
        public DataTemplate? RectTemplate { get; set; }

        /// <summary>
        /// 颜色类型模板
        /// </summary>
        public DataTemplate? ColorTemplate { get; set; }

        /// <summary>
        /// 数组/集合类型模板
        /// </summary>
        public DataTemplate? CollectionTemplate { get; set; }

        /// <summary>
        /// 默认模板
        /// </summary>
        public DataTemplate? DefaultTemplate { get; set; }

        /// <summary>
        /// 选择模板
        /// </summary>
        public override DataTemplate? SelectTemplate(object item, DependencyObject container)
        {
            if (item is ParameterBindingViewModel viewModel)
            {
                var type = viewModel.ParameterType;

                // 整数类型
                if (type == typeof(int) || type == typeof(long) || type == typeof(short) || type == typeof(byte))
                {
                    return IntegerTemplate ?? DefaultTemplate;
                }

                // 浮点类型
                if (type == typeof(double) || type == typeof(float) || type == typeof(decimal))
                {
                    return DoubleTemplate ?? DefaultTemplate;
                }

                // 字符串类型
                if (type == typeof(string))
                {
                    return StringTemplate ?? DefaultTemplate;
                }

                // 布尔类型
                if (type == typeof(bool))
                {
                    return BooleanTemplate ?? DefaultTemplate;
                }

                // 枚举类型
                if (type.IsEnum)
                {
                    return EnumTemplate ?? DefaultTemplate;
                }

                // OpenCvSharp.Point
                if (type.Name == "Point" || type.FullName?.Contains("Point") == true)
                {
                    return PointTemplate ?? DefaultTemplate;
                }

                // OpenCvSharp.Size
                if (type.Name == "Size" || type.FullName?.Contains("Size") == true)
                {
                    return SizeTemplate ?? DefaultTemplate;
                }

                // OpenCvSharp.Rect
                if (type.Name == "Rect" || type.Name == "Rect2d" || type.FullName?.Contains("Rect") == true)
                {
                    return RectTemplate ?? DefaultTemplate;
                }

                // 颜色类型
                if (type.Name == "Color" || type.Name == "Scalar" || type.FullName?.Contains("Color") == true)
                {
                    return ColorTemplate ?? DefaultTemplate;
                }

                // 数组/集合类型
                if (type.IsArray || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(System.Collections.Generic.List<>)))
                {
                    return CollectionTemplate ?? DefaultTemplate;
                }

                // 默认模板
                return DefaultTemplate;
            }

            return base.SelectTemplate(item, container);
        }
    }

    /// <summary>
    /// 参数类型组
    /// </summary>
    public enum ParameterTypeGroup
    {
        /// <summary>
        /// 数值类型
        /// </summary>
        Numeric,

        /// <summary>
        /// 文本类型
        /// </summary>
        Text,

        /// <summary>
        /// 布尔类型
        /// </summary>
        Boolean,

        /// <summary>
        /// 枚举类型
        /// </summary>
        Enumeration,

        /// <summary>
        /// 几何类型
        /// </summary>
        Geometry,

        /// <summary>
        /// 颜色类型
        /// </summary>
        Color,

        /// <summary>
        /// 集合类型
        /// </summary>
        Collection,

        /// <summary>
        /// 其他类型
        /// </summary>
        Other
    }

    /// <summary>
    /// 参数类型辅助类
    /// </summary>
    public static class ParameterTypeHelper
    {
        /// <summary>
        /// 获取参数类型组
        /// </summary>
        public static ParameterTypeGroup GetTypeGroup(Type type)
        {
            // 数值类型
            if (type == typeof(int) || type == typeof(long) || type == typeof(short) ||
                type == typeof(byte) || type == typeof(double) || type == typeof(float) ||
                type == typeof(decimal))
            {
                return ParameterTypeGroup.Numeric;
            }

            // 文本类型
            if (type == typeof(string))
            {
                return ParameterTypeGroup.Text;
            }

            // 布尔类型
            if (type == typeof(bool))
            {
                return ParameterTypeGroup.Boolean;
            }

            // 枚举类型
            if (type.IsEnum)
            {
                return ParameterTypeGroup.Enumeration;
            }

            // 几何类型
            if (type.Name == "Point" || type.Name == "Size" || type.Name == "Rect" ||
                type.Name == "Point2d" || type.Name == "Size2d" || type.Name == "Rect2d" ||
                type.FullName?.Contains("Point") == true || type.FullName?.Contains("Size") == true ||
                type.FullName?.Contains("Rect") == true)
            {
                return ParameterTypeGroup.Geometry;
            }

            // 颜色类型
            if (type.Name == "Color" || type.Name == "Scalar" || type.FullName?.Contains("Color") == true)
            {
                return ParameterTypeGroup.Color;
            }

            // 集合类型
            if (type.IsArray || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(System.Collections.Generic.List<>)))
            {
                return ParameterTypeGroup.Collection;
            }

            return ParameterTypeGroup.Other;
        }

        /// <summary>
        /// 获取类型显示名称
        /// </summary>
        public static string GetTypeDisplayName(Type type)
        {
            var group = GetTypeGroup(type);
            return group switch
            {
                ParameterTypeGroup.Numeric => "数值",
                ParameterTypeGroup.Text => "文本",
                ParameterTypeGroup.Boolean => "布尔",
                ParameterTypeGroup.Enumeration => "枚举",
                ParameterTypeGroup.Geometry => "几何",
                ParameterTypeGroup.Color => "颜色",
                ParameterTypeGroup.Collection => "集合",
                _ => type.Name
            };
        }

        /// <summary>
        /// 获取类型的默认值
        /// </summary>
        public static object? GetDefaultValue(Type type)
        {
            if (type.IsValueType)
            {
                return Activator.CreateInstance(type);
            }

            return null;
        }
    }
}
