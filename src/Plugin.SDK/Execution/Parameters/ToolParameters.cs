using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SunEyeVision.Plugin.SDK.Metadata;
using SunEyeVision.Plugin.SDK.Validation;
using ValidationResult = SunEyeVision.Plugin.SDK.Validation.ValidationResult;

namespace SunEyeVision.Plugin.SDK.Execution.Parameters
{
    /// <summary>
    /// 参数范围特性
    /// </summary>
    /// <remarks>
    /// 用于标注参数的有效范围，支持自动验证和UI生成。
    /// 
    /// 使用示例：
    /// <code>
    /// public class CircleFindParams : ToolParamsBase
    /// {
    ///     [ParameterRange(0.1, 1000.0)]
    ///     public double MinRadius { get; set; } = 5.0;
    ///     
    ///     [ParameterRange(0.1, 1000.0)]
    ///     public double MaxRadius { get; set; } = 50.0;
    ///     
    ///     [ParameterRange(0, 255)]
    ///     public int Threshold { get; set; } = 128;
    /// }
    /// </code>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class ParameterRangeAttribute : Attribute
    {
        /// <summary>
        /// 最小值
        /// </summary>
        public double Min { get; }

        /// <summary>
        /// 最大值
        /// </summary>
        public double Max { get; }

        /// <summary>
        /// 步进值（用于UI滑块）
        /// </summary>
        public double Step { get; set; } = 1.0;

        /// <summary>
        /// 单位（用于UI显示）
        /// </summary>
        public string? Unit { get; set; }

        /// <summary>
        /// 显示格式（用于UI显示）
        /// </summary>
        public string? DisplayFormat { get; set; }

        /// <summary>
        /// 创建参数范围特性
        /// </summary>
        /// <param name="min">最小值</param>
        /// <param name="max">最大值</param>
        public ParameterRangeAttribute(double min, double max)
        {
            Min = min;
            Max = max;
        }
    }

    /// <summary>
    /// 参数显示特性
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class ParameterDisplayAttribute : Attribute
    {
        /// <summary>
        /// 显示名称
        /// </summary>
        public string? DisplayName { get; set; }

        /// <summary>
        /// 描述信息
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 分组名称
        /// </summary>
        public string? Group { get; set; }

        /// <summary>
        /// 显示顺序（越小越靠前）
        /// </summary>
        public int Order { get; set; } = int.MaxValue;

        /// <summary>
        /// 是否只读
        /// </summary>
        public bool IsReadOnly { get; set; }

        /// <summary>
        /// 是否高级参数
        /// </summary>
        public bool IsAdvanced { get; set; }
    }

    /// <summary>
    /// 工具参数基类
    /// </summary>
    /// <remarks>
    /// 所有工具参数类的基类，提供参数验证、克隆和元数据功能。
    /// 
    /// 设计理念：
    /// 1. 强类型参数，编译时检查
    /// 2. 自动验证支持，通过特性标注约束
    /// 3. 序列化友好，支持JSON/Binary
    /// 4. UI绑定友好，支持属性变更通知
    /// 
    /// 使用示例：
    /// <code>
    /// public class CircleFindParams : ToolParamsBase
    /// {
    ///     [ParameterRange(1, 100)]
    ///     [ParameterDisplay(DisplayName = "最小半径", Description = "检测圆的最小半径")]
    ///     public double MinRadius { get; set; } = 5.0;
    ///     
    ///     [ParameterRange(1, 100)]
    ///     [ParameterDisplay(DisplayName = "最大半径", Description = "检测圆的最大半径")]
    ///     public double MaxRadius { get; set; } = 50.0;
    ///     
    ///     public override ValidationResult Validate()
    ///     {
    ///         var result = base.Validate();
    ///         if (MinRadius > MaxRadius)
    ///         {
    ///             result.AddError("最小半径不能大于最大半径");
    ///         }
    ///         return result;
    ///     }
    /// }
    /// </code>
    /// </remarks>
    public abstract class ToolParameters
    {
        /// <summary>
        /// 参数版本（用于序列化兼容性）
        /// </summary>
        public int Version { get; set; } = 1;

        /// <summary>
        /// 验证所有参数
        /// </summary>
        /// <returns>验证结果</returns>
        public virtual ValidationResult Validate()
        {
            var result = new ValidationResult();
            var properties = GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var prop in properties)
            {
                // 跳过Version属性
                if (prop.Name == nameof(Version)) continue;

                var value = prop.GetValue(this);
                var rangeAttr = prop.GetCustomAttribute<ParameterRangeAttribute>();

                if (rangeAttr != null && value != null)
                {
                    double numValue = Convert.ToDouble(value);
                    if (numValue < rangeAttr.Min || numValue > rangeAttr.Max)
                    {
                        var displayAttr = prop.GetCustomAttribute<ParameterDisplayAttribute>();
                        var displayName = displayAttr?.DisplayName ?? prop.Name;
                        result.AddError($"{displayName} 值 {numValue} 超出范围 [{rangeAttr.Min}, {rangeAttr.Max}]");
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// 深拷贝参数
        /// </summary>
        public ToolParameters Clone()
        {
            var cloned = (ToolParameters)MemberwiseClone();
            OnClone(cloned);
            return cloned;
        }

        /// <summary>
        /// 派生类可重写此方法执行深拷贝
        /// </summary>
        protected virtual void OnClone(ToolParameters cloned)
        {
            // 派生类可重写此方法执行深拷贝
        }

        /// <summary>
        /// 从另一个参数对象复制值
        /// </summary>
        public virtual void CopyFrom(ToolParameters other)
        {
            if (other == null || other.GetType() != GetType())
                return;

            var properties = GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var prop in properties)
            {
                if (prop.CanRead && prop.CanWrite)
                {
                    prop.SetValue(this, prop.GetValue(other));
                }
            }
        }

        /// <summary>
        /// 获取运行时参数元数据
        /// </summary>
        public IReadOnlyList<RuntimeParameterMetadata> GetRuntimeParameterMetadata()
        {
            var metadata = new List<RuntimeParameterMetadata>();
            var properties = GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var prop in properties)
            {
                if (prop.Name == nameof(Version)) continue;

                var rangeAttr = prop.GetCustomAttribute<ParameterRangeAttribute>();
                var displayAttr = prop.GetCustomAttribute<ParameterDisplayAttribute>();

                metadata.Add(new RuntimeParameterMetadata
                {
                    Name = prop.Name,
                    Type = prop.PropertyType,
                    Value = prop.GetValue(this),
                    Min = rangeAttr?.Min,
                    Max = rangeAttr?.Max,
                    Step = rangeAttr?.Step ?? 1.0,
                    Unit = rangeAttr?.Unit,
                    DisplayName = displayAttr?.DisplayName ?? prop.Name,
                    Description = displayAttr?.Description,
                    Group = displayAttr?.Group,
                    Order = displayAttr?.Order ?? int.MaxValue,
                    IsReadOnly = displayAttr?.IsReadOnly ?? false,
                    IsAdvanced = displayAttr?.IsAdvanced ?? false
                });
            }

            return metadata;
        }

        #region 参数分类查询方法

        /// <summary>
        /// 获取所有输入参数属性
        /// </summary>
        public IEnumerable<PropertyInfo> GetInputParameterProperties()
        {
            return GetPropertiesByCategory(ParamCategory.Input);
        }

        /// <summary>
        /// 获取所有输出参数属性
        /// </summary>
        public IEnumerable<PropertyInfo> GetOutputParameterProperties()
        {
            return GetPropertiesByCategory(ParamCategory.Output);
        }

        /// <summary>
        /// 获取所有配置参数属性
        /// </summary>
        public IEnumerable<PropertyInfo> GetConfigParameterProperties()
        {
            return GetPropertiesByCategory(ParamCategory.Config);
        }

        /// <summary>
        /// 获取所有运行时参数属性
        /// </summary>
        public IEnumerable<PropertyInfo> GetRuntimeParameterProperties()
        {
            return GetPropertiesByCategory(ParamCategory.Runtime);
        }

        /// <summary>
        /// 获取可绑定的输入参数属性
        /// </summary>
        public IEnumerable<PropertyInfo> GetBindableInputProperties()
        {
            return GetInputParameterProperties()
                .Where(p => !p.IsDefined(typeof(IgnoreBindAttribute)));
        }

        /// <summary>
        /// 获取可保存的参数属性（排除标记IgnoreSave的）
        /// </summary>
        public IEnumerable<PropertyInfo> GetSaveableProperties()
        {
            return GetAllParameterProperties()
                .Where(p => !p.IsDefined(typeof(IgnoreSaveAttribute)));
        }

        /// <summary>
        /// 获取需要在UI中显示的参数属性
        /// </summary>
        public IEnumerable<PropertyInfo> GetDisplayableProperties()
        {
            return GetAllParameterProperties()
                .Where(p => !p.IsDefined(typeof(IgnoreDisplayAttribute)))
                .OrderBy(p => p.GetCustomAttribute<ParamAttribute>()?.Order ?? int.MaxValue);
        }

        /// <summary>
        /// 获取所有参数属性（排除Version）
        /// </summary>
        public IEnumerable<PropertyInfo> GetAllParameterProperties()
        {
            return GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.Name != nameof(Version));
        }

        /// <summary>
        /// 根据分类获取参数属性
        /// </summary>
        private IEnumerable<PropertyInfo> GetPropertiesByCategory(ParamCategory category)
        {
            return GetAllParameterProperties()
                .Where(p =>
                {
                    var paramAttr = p.GetCustomAttribute<ParamAttribute>();
                    return paramAttr != null && paramAttr.Category == category;
                });
        }

        #endregion

        #region 参数特性查询方法

        /// <summary>
        /// 判断参数是否需要保存到项目文件
        /// </summary>
        public bool ShouldSave(string propertyName)
        {
            var prop = GetProperty(propertyName);
            if (prop == null) return false;
            return !prop.IsDefined(typeof(IgnoreSaveAttribute));
        }

        /// <summary>
        /// 判断参数是否支持数据绑定
        /// </summary>
        public bool CanBind(string propertyName)
        {
            var prop = GetProperty(propertyName);
            if (prop == null) return false;

            var paramAttr = prop.GetCustomAttribute<ParamAttribute>();
            if (paramAttr == null) return false;

            // 只有输入参数且没有IgnoreBind标记的才支持绑定
            return paramAttr.Category == ParamCategory.Input && 
                   !prop.IsDefined(typeof(IgnoreBindAttribute));
        }

        /// <summary>
        /// 判断参数是否需要在UI显示
        /// </summary>
        public bool ShouldDisplay(string propertyName)
        {
            var prop = GetProperty(propertyName);
            if (prop == null) return false;
            return !prop.IsDefined(typeof(IgnoreDisplayAttribute));
        }

        /// <summary>
        /// 获取参数的完整元数据
        /// </summary>
        public ParameterMetadata? GetParameterMetadata(string propertyName)
        {
            var prop = GetProperty(propertyName);
            if (prop == null) return null;

            var paramAttr = prop.GetCustomAttribute<ParamAttribute>();
            var displayAttr = prop.GetCustomAttribute<ParameterDisplayAttribute>();
            var rangeAttr = prop.GetCustomAttribute<ParameterRangeAttribute>();

            return new ParameterMetadata
            {
                Name = prop.Name,
                DisplayName = paramAttr?.DisplayName ?? displayAttr?.DisplayName ?? prop.Name,
                Description = paramAttr?.Description ?? displayAttr?.Description ?? string.Empty,
                Type = DetermineParamDataType(prop.PropertyType),
                DefaultValue = prop.GetValue(this),
                MinValue = rangeAttr?.Min,
                MaxValue = rangeAttr?.Max,
                Required = paramAttr?.Required ?? true,
                ReadOnly = displayAttr?.IsReadOnly ?? false,
                Category = paramAttr?.Group ?? displayAttr?.Group ?? "基本参数",
                SupportsBinding = paramAttr?.Category == ParamCategory.Input && 
                                  !prop.IsDefined(typeof(IgnoreBindAttribute)),
                BindingHint = paramAttr?.Category == ParamCategory.Input ? "支持从上游节点绑定" : null
            };
        }

        /// <summary>
        /// 获取所有参数的元数据列表
        /// </summary>
        public IReadOnlyList<ParameterMetadata> GetAllParameterMetadata()
        {
            return GetAllParameterProperties()
                .Select(p => GetParameterMetadata(p.Name))
                .Where(m => m != null)
                .Cast<ParameterMetadata>()
                .ToList()
                .AsReadOnly();
        }

        /// <summary>
        /// 获取属性信息
        /// </summary>
        private PropertyInfo? GetProperty(string propertyName)
        {
            return GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        }

        /// <summary>
        /// 根据CLR类型推断参数数据类型
        /// </summary>
        private static ParamDataType DetermineParamDataType(Type type)
        {
            if (type == typeof(int) || type == typeof(long))
                return ParamDataType.Int;
            if (type == typeof(double) || type == typeof(float) || type == typeof(decimal))
                return ParamDataType.Double;
            if (type == typeof(string))
                return ParamDataType.String;
            if (type == typeof(bool))
                return ParamDataType.Bool;
            if (type.IsEnum)
                return ParamDataType.Enum;
            if (type == typeof(OpenCvSharp.Point) || type == typeof(OpenCvSharp.Point2d) ||
                type.FullName?.Contains("Point") == true)
                return ParamDataType.Point;
            if (type == typeof(OpenCvSharp.Size) || type == typeof(OpenCvSharp.Size2d) ||
                type.FullName?.Contains("Size") == true)
                return ParamDataType.Size;
            if (type == typeof(OpenCvSharp.Rect) || type == typeof(OpenCvSharp.Rect2d) ||
                type.FullName?.Contains("Rect") == true)
                return ParamDataType.Rect;
            if (type.Name.Contains("Image") || type.Name.Contains("Mat"))
                return ParamDataType.Image;

            return ParamDataType.Custom;
        }

        #endregion
    }

    /// <summary>
    /// 运行时参数元数据 - 用于运行时参数值跟踪和内省
    /// </summary>
    public sealed class RuntimeParameterMetadata
    {
        /// <summary>
        /// 参数名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 参数类型
        /// </summary>
        public Type Type { get; set; } = typeof(object);

        /// <summary>
        /// 当前值
        /// </summary>
        public object? Value { get; set; }

        /// <summary>
        /// 最小值（如果适用）
        /// </summary>
        public double? Min { get; set; }

        /// <summary>
        /// 最大值（如果适用）
        /// </summary>
        public double? Max { get; set; }

        /// <summary>
        /// 步进值
        /// </summary>
        public double Step { get; set; } = 1.0;

        /// <summary>
        /// 单位
        /// </summary>
        public string? Unit { get; set; }

        /// <summary>
        /// 显示名称
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// 描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 分组
        /// </summary>
        public string? Group { get; set; }

        /// <summary>
        /// 显示顺序
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// 是否只读
        /// </summary>
        public bool IsReadOnly { get; set; }

        /// <summary>
        /// 是否高级参数
        /// </summary>
        public bool IsAdvanced { get; set; }
    }

    /// <summary>
    /// 通用工具参数 - 用于兼容层的具体实现
    /// </summary>
    public class GenericToolParameters : ToolParameters
    {
        private readonly Dictionary<string, object?> _values = new();

        /// <summary>
        /// 设置参数值
        /// </summary>
        public void SetValue(string name, object? value)
        {
            _values[name] = value;
        }

        /// <summary>
        /// 获取参数值
        /// </summary>
        public T? GetValue<T>(string name)
        {
            if (_values.TryGetValue(name, out var value))
            {
                if (value is T typedValue)
                    return typedValue;
                try
                {
                    return (T?)Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    return default;
                }
            }
            return default;
        }

        /// <summary>
        /// 获取所有参数名称
        /// </summary>
        public IEnumerable<string> GetParameterNames() => _values.Keys;
    }
}
