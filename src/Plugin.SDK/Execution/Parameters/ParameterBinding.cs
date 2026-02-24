using System;
using System.Collections.Generic;

namespace SunEyeVision.Plugin.SDK.Execution.Parameters
{
    /// <summary>
    /// 参数绑定模型
    /// </summary>
    /// <remarks>
    /// 定义单个参数的绑定配置，支持常量值和动态绑定两种模式。
    /// 
    /// 核心功能：
    /// 1. 支持常量值绑定（Constant）
    /// 2. 支持动态绑定到父节点输出（DynamicBinding）
    /// 3. 支持简单的值转换表达式
    /// 4. 支持序列化和反序列化
    /// 
    /// 使用示例：
    /// <code>
    /// // 创建常量绑定
    /// var thresholdBinding = new ParameterBinding
    /// {
    ///     ParameterName = "Threshold",
    ///     BindingType = BindingType.Constant,
    ///     ConstantValue = 128
    /// };
    /// 
    /// // 创建动态绑定
    /// var radiusBinding = new ParameterBinding
    /// {
    ///     ParameterName = "MinRadius",
    ///     BindingType = BindingType.DynamicBinding,
    ///     SourceNodeId = "circle_find_001",
    ///     SourceProperty = "Radius",
    ///     TransformExpression = "value * 0.9"  // 取90%的值
    /// };
    /// </code>
    /// </remarks>
    public class ParameterBinding
    {
        /// <summary>
        /// 参数名称
        /// </summary>
        /// <remarks>
        /// 要绑定的参数名称，对应 ToolParameters 类中的属性名。
        /// </remarks>
        public string ParameterName { get; set; } = string.Empty;

        /// <summary>
        /// 绑定类型
        /// </summary>
        public BindingType BindingType { get; set; } = BindingType.Constant;

        /// <summary>
        /// 常量值
        /// </summary>
        /// <remarks>
        /// 当 BindingType 为 Constant 时使用此值。
        /// 可以是任意类型：int、double、string、bool、Point等。
        /// </remarks>
        public object? ConstantValue { get; set; }

        /// <summary>
        /// 源节点ID
        /// </summary>
        /// <remarks>
        /// 当 BindingType 为 DynamicBinding 时，指定数据来源的节点ID。
        /// </remarks>
        public string? SourceNodeId { get; set; }

        /// <summary>
        /// 源属性名称
        /// </summary>
        /// <remarks>
        /// 当 BindingType 为 DynamicBinding 时，指定从源节点结果的哪个属性获取值。
        /// 示例: "Radius", "Center.X", "CircleFound.Radius"
        /// </remarks>
        public string? SourceProperty { get; set; }

        /// <summary>
        /// 转换表达式
        /// </summary>
        /// <remarks>
        /// 可选的值转换表达式，用于对获取的值进行简单转换。
        /// 示例: "value * 1.5", "Math.Max(value, 10)", "value.ToString()"
        /// 变量 'value' 代表从源属性获取的原始值。
        /// </remarks>
        public string? TransformExpression { get; set; }

        /// <summary>
        /// 目标参数类型
        /// </summary>
        /// <remarks>
        /// 参数的目标类型，用于类型转换和验证。
        /// </remarks>
        public Type? TargetType { get; set; }

        /// <summary>
        /// 是否有效
        /// </summary>
        public bool IsValid => Validate().IsValid;

        /// <summary>
        /// 创建常量绑定
        /// </summary>
        /// <param name="parameterName">参数名称</param>
        /// <param name="value">常量值</param>
        /// <returns>参数绑定实例</returns>
        public static ParameterBinding CreateConstant(string parameterName, object? value)
        {
            return new ParameterBinding
            {
                ParameterName = parameterName,
                BindingType = BindingType.Constant,
                ConstantValue = value,
                TargetType = value?.GetType()
            };
        }

        /// <summary>
        /// 创建动态绑定
        /// </summary>
        /// <param name="parameterName">参数名称</param>
        /// <param name="sourceNodeId">源节点ID</param>
        /// <param name="sourceProperty">源属性名称</param>
        /// <param name="transformExpression">转换表达式（可选）</param>
        /// <returns>参数绑定实例</returns>
        public static ParameterBinding CreateDynamic(
            string parameterName,
            string sourceNodeId,
            string sourceProperty,
            string? transformExpression = null)
        {
            return new ParameterBinding
            {
                ParameterName = parameterName,
                BindingType = BindingType.DynamicBinding,
                SourceNodeId = sourceNodeId,
                SourceProperty = sourceProperty,
                TransformExpression = transformExpression
            };
        }

        /// <summary>
        /// 验证绑定配置
        /// </summary>
        /// <returns>验证结果</returns>
        public BindingValidationResult Validate()
        {
            var result = new BindingValidationResult { IsValid = true };

            if (string.IsNullOrWhiteSpace(ParameterName))
            {
                result.IsValid = false;
                result.Errors.Add("参数名称不能为空");
            }

            switch (BindingType)
            {
                case BindingType.Constant:
                    // 常量值可以为null，某些参数可能接受null
                    break;

                case BindingType.DynamicBinding:
                    if (string.IsNullOrWhiteSpace(SourceNodeId))
                    {
                        result.IsValid = false;
                        result.Errors.Add("动态绑定必须指定源节点ID");
                    }
                    if (string.IsNullOrWhiteSpace(SourceProperty))
                    {
                        result.IsValid = false;
                        result.Errors.Add("动态绑定必须指定源属性名称");
                    }
                    break;

                case BindingType.Expression:
                    if (string.IsNullOrWhiteSpace(TransformExpression))
                    {
                        result.IsValid = false;
                        result.Errors.Add("表达式绑定必须指定转换表达式");
                    }
                    break;
            }

            return result;
        }

        /// <summary>
        /// 克隆当前绑定
        /// </summary>
        /// <returns>克隆的绑定实例</returns>
        public ParameterBinding Clone()
        {
            return new ParameterBinding
            {
                ParameterName = ParameterName,
                BindingType = BindingType,
                ConstantValue = ConstantValue,
                SourceNodeId = SourceNodeId,
                SourceProperty = SourceProperty,
                TransformExpression = TransformExpression,
                TargetType = TargetType
            };
        }

        /// <summary>
        /// 转换为字典（用于序列化）
        /// </summary>
        public Dictionary<string, object> ToDictionary()
        {
            var dict = new Dictionary<string, object>
            {
                ["ParameterName"] = ParameterName,
                ["BindingType"] = (int)BindingType
            };

            if (ConstantValue != null)
            {
                dict["ConstantValue"] = ConstantValue;
                dict["ConstantValueType"] = ConstantValue.GetType().AssemblyQualifiedName ?? ConstantValue.GetType().FullName!;
            }

            if (!string.IsNullOrEmpty(SourceNodeId))
                dict["SourceNodeId"] = SourceNodeId;

            if (!string.IsNullOrEmpty(SourceProperty))
                dict["SourceProperty"] = SourceProperty;

            if (!string.IsNullOrEmpty(TransformExpression))
                dict["TransformExpression"] = TransformExpression;

            if (TargetType != null)
                dict["TargetType"] = TargetType.AssemblyQualifiedName ?? TargetType.FullName!;

            return dict;
        }

        /// <summary>
        /// 从字典创建绑定（用于反序列化）
        /// </summary>
        public static ParameterBinding FromDictionary(Dictionary<string, object> dict)
        {
            var binding = new ParameterBinding
            {
                ParameterName = dict.TryGetValue("ParameterName", out var name) ? name?.ToString() ?? string.Empty : string.Empty,
                BindingType = dict.TryGetValue("BindingType", out var type) ? (BindingType)Convert.ToInt32(type) : BindingType.Constant
            };

            if (dict.TryGetValue("ConstantValue", out var value) && value != null)
            {
                if (dict.TryGetValue("ConstantValueType", out var typeName))
                {
                    var valueType = Type.GetType(typeName?.ToString() ?? string.Empty);
                    if (valueType != null && value.GetType() != valueType)
                    {
                        try
                        {
                            binding.ConstantValue = Convert.ChangeType(value, valueType);
                        }
                        catch
                        {
                            binding.ConstantValue = value;
                        }
                    }
                    else
                    {
                        binding.ConstantValue = value;
                    }
                }
                else
                {
                    binding.ConstantValue = value;
                }
            }

            if (dict.TryGetValue("SourceNodeId", out var nodeId))
                binding.SourceNodeId = nodeId?.ToString();

            if (dict.TryGetValue("SourceProperty", out var prop))
                binding.SourceProperty = prop?.ToString();

            if (dict.TryGetValue("TransformExpression", out var expr))
                binding.TransformExpression = expr?.ToString();

            if (dict.TryGetValue("TargetType", out var targetType))
            {
                binding.TargetType = Type.GetType(targetType?.ToString() ?? string.Empty);
            }

            return binding;
        }

        /// <summary>
        /// 获取描述字符串
        /// </summary>
        public override string ToString()
        {
            return BindingType switch
            {
                BindingType.Constant => $"{ParameterName} = {ConstantValue ?? "null"}",
                BindingType.DynamicBinding => $"{ParameterName} <- {SourceNodeId}.{SourceProperty}",
                BindingType.Expression => $"{ParameterName} = {TransformExpression}",
                _ => $"{ParameterName} ({BindingType})"
            };
        }
    }

    /// <summary>
    /// 绑定验证结果
    /// </summary>
    public class BindingValidationResult
    {
        /// <summary>
        /// 是否有效
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// 错误信息列表
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();

        /// <summary>
        /// 警告信息列表
        /// </summary>
        public List<string> Warnings { get; set; } = new List<string>();
    }
}
