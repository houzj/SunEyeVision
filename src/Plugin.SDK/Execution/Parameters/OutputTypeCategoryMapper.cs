using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using OpenCvSharp;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Collections.ObjectModel;

namespace SunEyeVision.Plugin.SDK.Execution.Parameters;

/// <summary>
/// 输出类型分类映射器
/// </summary>
public static class OutputTypeCategoryMapper
{
    /// <summary>
    /// 获取类型所属的分类
    /// </summary>
    public static OutputTypeCategory GetCategory(Type type)
    {
        if (type == null)
            return OutputTypeCategory.Other;
        
        // 处理可空类型
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;
        
        // 1. 数值类型
        if (IsNumericType(underlyingType))
            return OutputTypeCategory.Numeric;
        
        // 2. 图像类型
        if (IsImageType(underlyingType))
            return OutputTypeCategory.Image;
        
        // 3. 形状类型
        if (IsShapeType(underlyingType))
            return OutputTypeCategory.Shape;
        
        // 4. 文本类型
        if (IsTextType(underlyingType))
            return OutputTypeCategory.Text;
        
        // 5. 列表类型
        if (IsListType(underlyingType))
            return OutputTypeCategory.List;
        
        // 6. 其他类型
        return OutputTypeCategory.Other;
    }
    
    /// <summary>
    /// 判断是否为数值类型
    /// </summary>
    private static bool IsNumericType(Type type)
    {
        return type == typeof(int) ||
               type == typeof(double) ||
               type == typeof(float) ||
               type == typeof(bool) ||
               type == typeof(byte) ||
               type == typeof(short) ||
               type == typeof(long) ||
               type == typeof(decimal) ||
               type == typeof(uint) ||
               type == typeof(ushort) ||
               type == typeof(ulong) ||
               type == typeof(sbyte);
    }
    
    /// <summary>
    /// 判断是否为图像类型
    /// </summary>
    private static bool IsImageType(Type type)
    {
        return type == typeof(Mat) ||
               type == typeof(WriteableBitmap) ||
               type.Name.Contains("Image") ||
               type.Name.Contains("Bitmap");
    }
    
    /// <summary>
    /// 判断是否为形状类型
    /// </summary>
    private static bool IsShapeType(Type type)
    {
        return type == typeof(Point) ||
               type == typeof(Point2f) ||
               type == typeof(Point2d) ||
               type == typeof(Rect) ||
               type == typeof(RotatedRect) ||
               type == typeof(Size) ||
               type == typeof(Size2f) ||
               type.Name.Contains("Circle") ||
               type.Name.Contains("Line") ||
               type.Name.Contains("Polygon");
    }
    
    /// <summary>
    /// 判断是否为文本类型
    /// </summary>
    private static bool IsTextType(Type type)
    {
        return type == typeof(string) ||
               type == typeof(char);
    }
    
    /// <summary>
    /// 判断是否为列表类型
    /// </summary>
    private static bool IsListType(Type type)
    {
        return type.IsGenericType &&
               (type.GetGenericTypeDefinition() == typeof(List<>) ||
                type.GetGenericTypeDefinition() == typeof(ObservableCollection<>) ||
                type.GetGenericTypeDefinition() == typeof(IEnumerable<>));
    }
    
    /// <summary>
    /// 判断两个类型是否兼容
    /// </summary>
    public static bool IsTypeCompatible(Type sourceType, Type targetType)
    {
        if (sourceType == null || targetType == null)
            return false;
        
        // 精确匹配
        if (sourceType == targetType)
            return true;
        
        // 处理可空类型
        var underlyingSourceType = Nullable.GetUnderlyingType(sourceType) ?? sourceType;
        var underlyingTargetType = Nullable.GetUnderlyingType(targetType) ?? targetType;
        
        if (underlyingSourceType == underlyingTargetType)
            return true;
        
        // 数值类型兼容性检查
        var sourceCategory = GetCategory(underlyingSourceType);
        var targetCategory = GetCategory(underlyingTargetType);
        
        if (sourceCategory == OutputTypeCategory.Numeric &&
            targetCategory == OutputTypeCategory.Numeric)
        {
            return true; // 所有数值类型互相兼容
        }
        
        return false;
    }
}
