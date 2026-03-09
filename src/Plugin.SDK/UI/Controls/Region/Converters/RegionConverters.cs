using System;
using System.Globalization;
using System.Linq;
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

    /// <summary>
    /// 形状类型到可见性的转换器
    /// </summary>
    public class ShapeTypeToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                var result = Visibility.Collapsed;

                PluginLogger.Info($"[ShapeTypeToVisibilityConverter] ══════════════════════════════════════════════", "UI");
                PluginLogger.Info($"[ShapeTypeToVisibilityConverter] Convert 开始执行", "UI");
                PluginLogger.Info($"[ShapeTypeToVisibilityConverter] value={value?.ToString() ?? "null"}, value.GetType()={value?.GetType().Name ?? "null"}", "UI");
                PluginLogger.Info($"[ShapeTypeToVisibilityConverter] parameter={parameter?.ToString() ?? "null"}, targetType={targetType?.Name ?? "null"}", "UI");

                // 处理 null 值
                if (value == null)
                {
                    PluginLogger.Info($"[ShapeTypeToVisibilityConverter] ❌ value 为 null，返回 Collapsed", "UI");
                    PluginLogger.Info($"[ShapeTypeToVisibilityConverter] ══════════════════════════════════════════════", "UI");
                    return Visibility.Collapsed;
                }

                // 添加类型检测，防止bool值被错误传递
                if (value is bool)
                {
                    PluginLogger.Error($"[ShapeTypeToVisibilityConverter] ⚠️ 严重错误：收到bool类型值，而不是ShapeType!", "UI");
                    PluginLogger.Error($"[ShapeTypeToVisibilityConverter] value={value}, 实际类型={value.GetType().Name}", "UI");
                    PluginLogger.Error($"[ShapeTypeToVisibilityConverter] 堆栈跟踪: {Environment.StackTrace}", "UI");
                    PluginLogger.Info($"[ShapeTypeToVisibilityConverter] ══════════════════════════════════════════════", "UI");
                    return Visibility.Collapsed;
                }

                // 处理枚举值（包括可空枚举）
                ShapeType? shapeType = null;
                bool isNullable = false;

                // 尝试直接获取 ShapeType（非可空）
                if (value is ShapeType nonNullableShapeType)
                {
                    shapeType = nonNullableShapeType;
                    isNullable = false;
                    PluginLogger.Info($"[ShapeTypeToVisibilityConverter] ✅ 检测到非可空 ShapeType: {shapeType.Value}", "UI");
                }
                // 尝试从可空枚举获取
                else if (value is Enum enumValue)
                {
                    var valueType = enumValue.GetType();
                    PluginLogger.Info($"[ShapeTypeToVisibilityConverter] 检测到枚举类型: {valueType.Name}, IsGenericType={valueType.IsGenericType}", "UI");

                    if (valueType == typeof(ShapeType))
                    {
                        shapeType = (ShapeType)enumValue;
                        isNullable = false;
                        PluginLogger.Info($"[ShapeTypeToVisibilityConverter] ✅ 从 ShapeType 枚举获取: {shapeType.Value}", "UI");
                    }
                    else if (valueType == typeof(ShapeType?) ||
                             (valueType.IsGenericType && valueType.GetGenericTypeDefinition() == typeof(Nullable<>) &&
                              valueType.GetGenericArguments()[0] == typeof(ShapeType)))
                    {
                        shapeType = (ShapeType)enumValue;
                        isNullable = true;
                        PluginLogger.Info($"[ShapeTypeToVisibilityConverter] ✅ 从可空 ShapeType? 枚举获取: {shapeType.Value}", "UI");
                    }
                    else
                    {
                        PluginLogger.Error($"[ShapeTypeToVisibilityConverter] ⚠️ 收到错误的枚举类型: {valueType.FullName}", "UI");
                        PluginLogger.Error($"[ShapeTypeToVisibilityConverter] 期望类型: ShapeType 或 ShapeType?", "UI");
                        PluginLogger.Info($"[ShapeTypeToVisibilityConverter] ══════════════════════════════════════════════", "UI");
                        return Visibility.Collapsed;
                    }
                }
                else
                {
                    PluginLogger.Error($"[ShapeTypeToVisibilityConverter] ⚠️ 收到不支持的类型: {value.GetType().FullName}", "UI");
                    PluginLogger.Error($"[ShapeTypeToVisibilityConverter] 期望类型: ShapeType, ShapeType? 或 null", "UI");
                    PluginLogger.Info($"[ShapeTypeToVisibilityConverter] ══════════════════════════════════════════════", "UI");
                    return Visibility.Collapsed;
                }

                // 如果成功获取形状类型且参数是字符串
                if (shapeType.HasValue && parameter is string types)
                {
                    PluginLogger.Info($"[ShapeTypeToVisibilityConverter] 开始匹配: shapeType={shapeType.Value}, types参数='{types}'", "UI");

                    var typeList = types.Split(',');
                    PluginLogger.Info($"[ShapeTypeToVisibilityConverter] 匹配列表: [{string.Join(", ", typeList.Select(t => t.Trim()))}]", "UI");

                    foreach (var type in typeList)
                    {
                        var trimmedType = type.Trim();
                        PluginLogger.Info($"[ShapeTypeToVisibilityConverter]   尝试匹配: '{trimmedType}'", "UI");

                        if (Enum.TryParse<ShapeType>(trimmedType, out var t))
                        {
                            if (shapeType.Value == t)
                            {
                                result = Visibility.Visible;
                                PluginLogger.Info($"[ShapeTypeToVisibilityConverter] ✅ 匹配成功: {shapeType.Value} == {t}", "UI");
                                PluginLogger.Info($"[ShapeTypeToVisibilityConverter] ✅ 最终结果: {result}", "UI");
                                PluginLogger.Info($"[ShapeTypeToVisibilityConverter] ══════════════════════════════════════════════", "UI");
                                break;
                            }
                            else
                            {
                                PluginLogger.Info($"[ShapeTypeToVisibilityConverter]   不匹配: {shapeType.Value} != {t}", "UI");
                            }
                        }
                        else
                        {
                            PluginLogger.Warning($"[ShapeTypeToVisibilityConverter] ⚠️ 无法解析枚举值: '{trimmedType}'", "UI");
                        }
                    }
                }
                else
                {
                    if (!shapeType.HasValue)
                    {
                        PluginLogger.Warning($"[ShapeTypeToVisibilityConverter] ⚠️ shapeType 为 null，无法进行匹配", "UI");
                    }
                    if (!(parameter is string))
                    {
                        PluginLogger.Warning($"[ShapeTypeToVisibilityConverter] ⚠️ parameter 不是 string 类型: {parameter?.GetType().Name ?? "null"}", "UI");
                    }
                }

                PluginLogger.Info($"[ShapeTypeToVisibilityConverter] 最终返回: {result}", "UI");
                PluginLogger.Info($"[ShapeTypeToVisibilityConverter] ══════════════════════════════════════════════", "UI");
                return result;
            }
            catch (Exception ex)
            {
                PluginLogger.Error($"[ShapeTypeToVisibilityConverter] ══════════════════════════════════════════════", "UI");
                PluginLogger.Error($"[ShapeTypeToVisibilityConverter] ❌ Convert 发生异常: {ex.Message}", "UI", ex);
                PluginLogger.Error($"[ShapeTypeToVisibilityConverter] 异常类型: {ex.GetType().Name}", "UI");
                PluginLogger.Error($"[ShapeTypeToVisibilityConverter] 堆栈跟踪:\n{ex.StackTrace}", "UI");
                PluginLogger.Error($"[ShapeTypeToVisibilityConverter] ══════════════════════════════════════════════", "UI");
                return Visibility.Collapsed;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 反向布尔值到可见性的转换器（true=Collapsed, false=Visible）
    /// </summary>
    public class InverseBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                PluginLogger.Info($"[InverseBooleanToVisibilityConverter] ══════════════════════════════════════════════", "UI");
                PluginLogger.Info($"[InverseBooleanToVisibilityConverter] Convert 开始执行", "UI");
                PluginLogger.Info($"[InverseBooleanToVisibilityConverter] value={value?.ToString() ?? "null"}, value.GetType()={value?.GetType().Name ?? "null"}, targetType={targetType?.Name ?? "null"}", "UI");

                if (value is bool b)
                {
                    var result = b ? Visibility.Collapsed : Visibility.Visible;
                    PluginLogger.Info($"[InverseBooleanToVisibilityConverter] 输入: {b}, 转换规则: true→Collapsed, false→Visible", "UI");
                    PluginLogger.Info($"[InverseBooleanToVisibilityConverter] ✅ 最终结果: {result}", "UI");
                    PluginLogger.Info($"[InverseBooleanToVisibilityConverter] ══════════════════════════════════════════════", "UI");
                    return result;
                }

                PluginLogger.Warning($"[InverseBooleanToVisibilityConverter] ⚠️ value 不是 bool 类型: {value?.GetType().Name ?? "null"}", "UI");
                PluginLogger.Warning($"[InverseBooleanToVisibilityConverter] 返回默认值: Visible", "UI");
                PluginLogger.Info($"[InverseBooleanToVisibilityConverter] ══════════════════════════════════════════════", "UI");
                return Visibility.Visible;
            }
            catch (Exception ex)
            {
                PluginLogger.Error($"[InverseBooleanToVisibilityConverter] ══════════════════════════════════════════════", "UI");
                PluginLogger.Error($"[InverseBooleanToVisibilityConverter] ❌ Convert 发生异常: {ex.Message}", "UI", ex);
                PluginLogger.Error($"[InverseBooleanToVisibilityConverter] 异常类型: {ex.GetType().Name}", "UI");
                PluginLogger.Error($"[InverseBooleanToVisibilityConverter] 堆栈跟踪:\n{ex.StackTrace}", "UI");
                PluginLogger.Error($"[InverseBooleanToVisibilityConverter] ══════════════════════════════════════════════", "UI");
                return Visibility.Visible;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                PluginLogger.Info($"[InverseBooleanToVisibilityConverter] ══════════════════════════════════════════════", "UI");
                PluginLogger.Info($"[InverseBooleanToVisibilityConverter] ConvertBack 开始执行", "UI");
                PluginLogger.Info($"[InverseBooleanToVisibilityConverter] value={value?.ToString() ?? "null"}, value.GetType()={value?.GetType().Name ?? "null"}, targetType={targetType?.Name ?? "null"}", "UI");

                if (value is Visibility v)
                {
                    var result = v != Visibility.Visible;
                    PluginLogger.Info($"[InverseBooleanToVisibilityConverter] 输入: {v}, 转换规则: Visible→false, 其他→true", "UI");
                    PluginLogger.Info($"[InverseBooleanToVisibilityConverter] ✅ 最终结果: {result}", "UI");
                    PluginLogger.Info($"[InverseBooleanToVisibilityConverter] ══════════════════════════════════════════════", "UI");
                    return result;
                }

                PluginLogger.Warning($"[InverseBooleanToVisibilityConverter] ⚠️ value 不是 Visibility 类型: {value?.GetType().Name ?? "null"}", "UI");
                PluginLogger.Warning($"[InverseBooleanToVisibilityConverter] 返回默认值: false", "UI");
                PluginLogger.Info($"[InverseBooleanToVisibilityConverter] ══════════════════════════════════════════════", "UI");
                return false;
            }
            catch (Exception ex)
            {
                PluginLogger.Error($"[InverseBooleanToVisibilityConverter] ══════════════════════════════════════════════", "UI");
                PluginLogger.Error($"[InverseBooleanToVisibilityConverter] ❌ ConvertBack 发生异常: {ex.Message}", "UI", ex);
                PluginLogger.Error($"[InverseBooleanToVisibilityConverter] 异常类型: {ex.GetType().Name}", "UI");
                PluginLogger.Error($"[InverseBooleanToVisibilityConverter] 堆栈跟踪:\n{ex.StackTrace}", "UI");
                PluginLogger.Error($"[InverseBooleanToVisibilityConverter] ══════════════════════════════════════════════", "UI");
                return false;
            }
        }
    }
}
