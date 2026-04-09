using System;
using System.Collections.Generic;

namespace SunEyeVision.Plugin.SDK.Execution.Parameters
{
    /// <summary>
    /// 输出类型分类
    /// </summary>
    /// <remarks>
    /// 按照数据大类分类，符合视觉软件行业标准。
    /// 
    /// 分类逻辑：
    /// - Image: 图像数据（Mat, Image 等）
    /// - Shape: 几何形状（Point, Rect, Circle, Line, Polygon 等）
    /// - Numeric: 数值类型（int, double, float, bool 等）
    /// - Text: 文本类型（string, char 等）
    /// - List: 列表类型（List, Array, ObservableCollection 等）
    /// - Other: 其他类型（自定义类型、复合类型等）
    /// </remarks>
    public enum OutputTypeCategory
    {
        /// <summary>
        /// 图像数据
        /// </summary>
        /// <remarks>
        /// 包括：OpenCvSharp.Mat, System.Drawing.Image, 等
        /// </remarks>
        Image,

        /// <summary>
        /// 几何形状
        /// </summary>
        /// <remarks>
        /// 包括：Point, Rect, Size, Circle, Line, Polygon, RotatedRect 等
        /// </remarks>
        Shape,

        /// <summary>
        /// 数值类型
        /// </summary>
        /// <remarks>
        /// 包括：int, double, float, bool, decimal, long, short, byte 等
        /// </remarks>
        Numeric,

        /// <summary>
        /// 文本类型
        /// </summary>
        /// <remarks>
        /// 包括：string, char 等
        /// </remarks>
        Text,

        /// <summary>
        /// 列表类型
        /// </summary>
        /// <remarks>
        /// 包括：List, Array, ObservableCollection, IEnumerable 等
        /// </remarks>
        List,

        /// <summary>
        /// 其他类型
        /// </summary>
        /// <remarks>
        /// 包括：自定义类型、复合类型、结构体等
        /// </remarks>
        Other
    }

    /// <summary>
    /// 类型分类映射器
    /// </summary>
    /// <remarks>
    /// 负责将 .NET 类型映射到输出类型分类。
    /// 支持自定义类型映射扩展。
    /// 
    /// 使用示例：
    /// <code>
    /// var category = TypeCategoryMapper.GetCategory(typeof(OpenCvSharp.Mat));
    /// // category = OutputTypeCategory.Image
    /// 
    /// var category2 = TypeCategoryMapper.GetCategory(typeof(int));
    /// // category2 = OutputTypeCategory.Numeric
    /// </code>
    /// </remarks>
    public static class TypeCategoryMapper
    {
        /// <summary>
        /// 图像类型集合
        /// </summary>
        private static readonly HashSet<Type> ImageTypes = new HashSet<Type>
        {
            typeof(OpenCvSharp.Mat),
            // 可以添加其他图像类型
            // typeof(System.Drawing.Image),
            // typeof(System.Drawing.Bitmap),
        };

        /// <summary>
        /// 形状类型集合
        /// </summary>
        private static readonly HashSet<Type> ShapeTypes = new HashSet<Type>
        {
            typeof(OpenCvSharp.Point),
            typeof(OpenCvSharp.Point2d),
            typeof(OpenCvSharp.Point2f),
            typeof(OpenCvSharp.Rect),
            typeof(OpenCvSharp.Rect2d),
            typeof(OpenCvSharp.Size),
            typeof(OpenCvSharp.Size2d),
            typeof(OpenCvSharp.RotatedRect),
            // 可以添加其他形状类型
            // typeof(Circle),
            // typeof(Line),
            // typeof(Polygon),
        };

        /// <summary>
        /// 数值类型集合
        /// </summary>
        private static readonly HashSet<Type> NumericTypes = new HashSet<Type>
        {
            typeof(int),
            typeof(long),
            typeof(short),
            typeof(byte),
            typeof(float),
            typeof(double),
            typeof(decimal),
            typeof(bool),
            typeof(uint),
            typeof(ulong),
            typeof(ushort),
            typeof(sbyte),
        };

        /// <summary>
        /// 文本类型集合
        /// </summary>
        private static readonly HashSet<Type> TextTypes = new HashSet<Type>
        {
            typeof(string),
            typeof(char),
        };

        /// <summary>
        /// 自定义类型映射（允许外部扩展）
        /// </summary>
        private static readonly Dictionary<Type, OutputTypeCategory> CustomMappings = new Dictionary<Type, OutputTypeCategory>();

        /// <summary>
        /// 获取类型分类
        /// </summary>
        /// <param name="type">要分类的类型</param>
        /// <returns>输出类型分类</returns>
        public static OutputTypeCategory GetCategory(Type type)
        {
            if (type == null)
            {
                return OutputTypeCategory.Other;
            }

            // 1. 检查自定义映射
            if (CustomMappings.TryGetValue(type, out var customCategory))
            {
                return customCategory;
            }

            // 2. 检查预定义类型集合
            if (ImageTypes.Contains(type))
            {
                return OutputTypeCategory.Image;
            }

            if (ShapeTypes.Contains(type))
            {
                return OutputTypeCategory.Shape;
            }

            if (NumericTypes.Contains(type))
            {
                return OutputTypeCategory.Numeric;
            }

            if (TextTypes.Contains(type))
            {
                return OutputTypeCategory.Text;
            }

            // 3. 检查列表类型（泛型集合）
            if (IsListType(type))
            {
                return OutputTypeCategory.List;
            }

            // 4. 默认为其他类型
            return OutputTypeCategory.Other;
        }

        /// <summary>
        /// 检查是否为列表类型
        /// </summary>
        /// <param name="type">要检查的类型</param>
        /// <returns>是否为列表类型</returns>
        private static bool IsListType(Type type)
        {
            // 检查数组
            if (type.IsArray)
            {
                return true;
            }

            // 检查泛型集合
            if (type.IsGenericType)
            {
                var genericDef = type.GetGenericTypeDefinition();
                
                // 常见的泛型集合类型
                if (genericDef == typeof(List<>) ||
                    genericDef == typeof(IList<>) ||
                    genericDef == typeof(ICollection<>) ||
                    genericDef == typeof(IEnumerable<>) ||
                    genericDef == typeof(System.Collections.ObjectModel.ObservableCollection<>) ||
                    genericDef == typeof(System.Collections.ObjectModel.Collection<>))
                {
                    return true;
                }
            }

            // 检查非泛型集合接口
            if (typeof(System.Collections.IList).IsAssignableFrom(type) ||
                typeof(System.Collections.ICollection).IsAssignableFrom(type) ||
                typeof(System.Collections.IEnumerable).IsAssignableFrom(type))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 注册自定义类型映射
        /// </summary>
        /// <param name="type">类型</param>
        /// <param name="category">分类</param>
        public static void RegisterCustomMapping(Type type, OutputTypeCategory category)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            CustomMappings[type] = category;
        }

        /// <summary>
        /// 移除自定义类型映射
        /// </summary>
        /// <param name="type">类型</param>
        public static void UnregisterCustomMapping(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            CustomMappings.Remove(type);
        }

        /// <summary>
        /// 清除所有自定义映射
        /// </summary>
        public static void ClearCustomMappings()
        {
            CustomMappings.Clear();
        }

        /// <summary>
        /// 获取分类显示名称
        /// </summary>
        /// <param name="category">分类</param>
        /// <returns>显示名称</returns>
        public static string GetCategoryDisplayName(OutputTypeCategory category)
        {
            return category switch
            {
                OutputTypeCategory.Image => "图像",
                OutputTypeCategory.Shape => "形状",
                OutputTypeCategory.Numeric => "数值",
                OutputTypeCategory.Text => "文本",
                OutputTypeCategory.List => "列表",
                OutputTypeCategory.Other => "其他",
                _ => "未知"
            };
        }

        /// <summary>
        /// 获取分类图标
        /// </summary>
        /// <param name="category">分类</param>
        /// <returns>图标路径或名称</returns>
        public static string? GetCategoryIcon(OutputTypeCategory category)
        {
            return category switch
            {
                OutputTypeCategory.Image => "🖼️",
                OutputTypeCategory.Shape => "📐",
                OutputTypeCategory.Numeric => "🔢",
                OutputTypeCategory.Text => "📝",
                OutputTypeCategory.List => "📋",
                OutputTypeCategory.Other => "❓",
                _ => null
            };
        }
    }
}
