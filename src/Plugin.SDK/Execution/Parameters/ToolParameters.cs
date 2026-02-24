using System;
using System.Collections.Generic;
using System.Reflection;
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
        /// 获取参数元数据
        /// </summary>
        public IReadOnlyList<ParameterMetadata> GetParameterMetadata()
        {
            var metadata = new List<ParameterMetadata>();
            var properties = GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var prop in properties)
            {
                if (prop.Name == nameof(Version)) continue;

                var rangeAttr = prop.GetCustomAttribute<ParameterRangeAttribute>();
                var displayAttr = prop.GetCustomAttribute<ParameterDisplayAttribute>();

                metadata.Add(new ParameterMetadata
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
    }

    /// <summary>
    /// 参数元数据
    /// </summary>
    public sealed class ParameterMetadata
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
}
