using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SunEyeVision.Plugin.SDK.Execution.Parameters
{
    /// <summary>
    /// 参数设置模型
    /// </summary>
    /// <remarks>
    /// 定义单个参数的设置配置，支持常量值和动态绑定两种模式。
    /// 
    /// 核心概念：
    /// 1. 绑定类型（BindingType）：回答"值从哪里来"
    ///    - Constant: 固定常量值
    ///    - Binding: 从其他节点输出获取
    /// 
    /// 2. 数据类型（DataType）：回答"值是什么类型"
    ///    - 由 System.Type 定义，支持精确类型匹配
    ///    - 支持所有 .NET 基础类型和自定义类型
    /// 
    /// 3. 值转换（TransformExpression）：可选的表达式转换
    ///    - 两种绑定模式都可用
    ///    - 示例: "value * 1.5", "Math.Max(value, 10)"
    /// 
    /// 使用示例：
    /// <code>
    /// // 创建常量设置
    /// var thresholdSetting = ParamSetting.CreateConstant("Threshold", 128);
    /// 
    /// // 创建动态绑定设置
    /// var radiusSetting = ParamSetting.CreateBinding("MinRadius", "circle_find_001", "Radius");
    /// 
    /// // 创建带转换表达式的设置
    /// var settingWithTransform = ParamSetting.CreateBinding("Radius", "node_001", "Radius", "value * 0.9");
    /// </code>
    /// </remarks>
    public class ParamSetting
    {
        /// <summary>
        /// 参数名称
        /// </summary>
        /// <remarks>
        /// 要设置的参数名称，对应 ToolParameters 类中的属性名。
        /// </remarks>
        public string ParameterName { get; set; } = string.Empty;

        /// <summary>
        /// 绑定类型
        /// </summary>
        /// <remarks>
        /// 只有两种基本绑定类型：Constant（常量）和 Binding（动态绑定）。
        /// </remarks>
        public BindingType BindingType { get; set; } = BindingType.Constant;

        /// <summary>
        /// 常量值
        /// </summary>
        /// <remarks>
        /// 当 BindingType 为 Constant 时使用此值。
        /// 可以是任意类型：int、double、string、bool、Point等。
        /// 数据类型由 ParameterMetadata.DataType 或 BindableParameter.DataType 决定。
        /// </remarks>
        public object? ConstantValue { get; set; }

        /// <summary>
        /// 源节点ID
        /// </summary>
        /// <remarks>
        /// 当 BindingType 为 Binding 时，指定数据来源的节点ID。
        /// </remarks>
        public string? SourceNodeId { get; set; }

        /// <summary>
        /// 源属性名称
        /// </summary>
        /// <remarks>
        /// 当 BindingType 为 Binding 时，指定从源节点结果的哪个属性获取值。
        /// 示例: "Radius", "Center.X", "CircleFound.Radius"
        /// </remarks>
        public string? SourceProperty { get; set; }

        /// <summary>
        /// 转换表达式
        /// </summary>
        /// <remarks>
        /// 可选的值转换表达式，两种绑定模式都可用。
        /// 示例: "value * 1.5", "Math.Max(value, 10)", "value.ToString()"
        /// 变量 'value' 代表获取的原始值。
        /// </remarks>
        public string? TransformExpression { get; set; }

        /// <summary>
        /// 目标参数类型
        /// </summary>
        /// <remarks>
        /// 参数的目标类型，用于类型转换和验证。
        /// 类型信息由泛型参数 T 自动推断，无需序列化。
        /// </remarks>
        [JsonIgnore]
        public Type? TargetType { get; set; }

        /// <summary>
        /// 是否有效
        /// </summary>
        public bool IsValid => Validate().IsValid;

        /// <summary>
        /// 创建常量设置
        /// </summary>
        /// <param name="parameterName">参数名称</param>
        /// <param name="value">常量值</param>
        /// <returns>参数设置实例</returns>
        public static ParamSetting CreateConstant(string parameterName, object? value)
        {
            return new ParamSetting
            {
                ParameterName = parameterName,
                BindingType = BindingType.Constant,
                ConstantValue = value,
                TargetType = value?.GetType()
            };
        }

        /// <summary>
        /// 创建动态绑定设置
        /// </summary>
        /// <param name="parameterName">参数名称</param>
        /// <param name="sourceNodeId">源节点ID</param>
        /// <param name="sourceProperty">源属性名称</param>
        /// <param name="transformExpression">转换表达式（可选）</param>
        /// <returns>参数设置实例</returns>
        public static ParamSetting CreateBinding(
            string parameterName,
            string sourceNodeId,
            string sourceProperty,
            string? transformExpression = null)
        {
            return new ParamSetting
            {
                ParameterName = parameterName,
                BindingType = BindingType.Binding,
                SourceNodeId = sourceNodeId,
                SourceProperty = sourceProperty,
                TransformExpression = transformExpression
            };
        }

        /// <summary>
        /// 验证设置配置
        /// </summary>
        /// <returns>验证结果</returns>
        public SettingValidationResult Validate()
        {
            var result = new SettingValidationResult { IsValid = true };

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

                case BindingType.Binding:
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
            }

            return result;
        }

        /// <summary>
        /// 克隆当前设置
        /// </summary>
        /// <returns>克隆的设置实例</returns>
        public ParamSetting Clone()
        {
            return new ParamSetting
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
        /// 从字典创建设置（用于反序列化）
        /// </summary>
        public static ParamSetting FromDictionary(Dictionary<string, object> dict)
        {
            var setting = new ParamSetting
            {
                ParameterName = dict.TryGetValue("ParameterName", out var name) ? name?.ToString() ?? string.Empty : string.Empty
            };

            // ✅ 修复：BindingType 可能是 JsonElement，需要先转换为字符串
            if (dict.TryGetValue("BindingType", out var type))
            {
                var typeStr = type?.ToString();
                if (int.TryParse(typeStr, out int typeInt))
                {
                    setting.BindingType = (BindingType)typeInt;
                }
                else
                {
                    setting.BindingType = BindingType.Constant;
                }
            }
            else
            {
                setting.BindingType = BindingType.Constant;
            }

            if (dict.TryGetValue("ConstantValue", out var value) && value != null)
            {
                if (dict.TryGetValue("ConstantValueType", out var typeName))
                {
                    var typeNameStr = typeName?.ToString();
                    var valueType = string.IsNullOrEmpty(typeNameStr) ? null : Type.GetType(typeNameStr);
                    if (valueType != null && value.GetType() != valueType)
                    {
                        try
                        {
                            setting.ConstantValue = Convert.ChangeType(value, valueType);
                        }
                        catch
                        {
                            setting.ConstantValue = value;
                        }
                    }
                    else
                    {
                        setting.ConstantValue = value;
                    }
                }
                else
                {
                    setting.ConstantValue = value;
                }
            }

            if (dict.TryGetValue("SourceNodeId", out var nodeId))
                setting.SourceNodeId = nodeId?.ToString();

            if (dict.TryGetValue("SourceProperty", out var prop))
                setting.SourceProperty = prop?.ToString();

            if (dict.TryGetValue("TransformExpression", out var expr))
                setting.TransformExpression = expr?.ToString();

            if (dict.TryGetValue("TargetType", out var targetType))
            {
                var targetTypeStr = targetType?.ToString();
                if (!string.IsNullOrEmpty(targetTypeStr))
                {
                    setting.TargetType = Type.GetType(targetTypeStr);
                }
            }

            return setting;
        }

        /// <summary>
        /// 获取描述字符串
        /// </summary>
        public override string ToString()
        {
            return BindingType switch
            {
                BindingType.Constant => string.IsNullOrEmpty(TransformExpression)
                    ? $"{ParameterName} = {ConstantValue ?? "null"}"
                    : $"{ParameterName} = {ConstantValue ?? "null"} ({TransformExpression})",
                BindingType.Binding => string.IsNullOrEmpty(TransformExpression)
                    ? $"{ParameterName} <- {SourceNodeId}.{SourceProperty}"
                    : $"{ParameterName} <- {SourceNodeId}.{SourceProperty} ({TransformExpression})",
                _ => $"{ParameterName} ({BindingType})"
            };
        }
    }

    /// <summary>
    /// 设置验证结果
    /// </summary>
    public class SettingValidationResult
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
