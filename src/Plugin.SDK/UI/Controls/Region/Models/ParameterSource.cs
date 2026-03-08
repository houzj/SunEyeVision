using System;
using System.Collections.Generic;
using SunEyeVision.Plugin.SDK.Logging;

namespace SunEyeVision.Plugin.SDK.UI.Controls.Region.Models
{
    /// <summary>
    /// 参数源基类 - 用于参数绑定
    /// </summary>
    public abstract class ParameterSource
    {
        /// <summary>
        /// 绑定类型
        /// </summary>
        public ParameterBindingType BindingType { get; protected set; }

        /// <summary>
        /// 数据类型（用于验证）
        /// </summary>
        public string DataType { get; set; } = "double";

        /// <summary>
        /// 获取参数值
        /// </summary>
        public abstract object? GetValue(IParameterContext? context, ILogger? logger = null);

        /// <summary>
        /// 克隆
        /// </summary>
        public abstract ParameterSource Clone();
    }

    /// <summary>
    /// 常量值源
    /// </summary>
    public class ConstantSource : ParameterSource
    {
        /// <summary>
        /// 常量值
        /// </summary>
        public object? Value { get; set; }

        public ConstantSource()
        {
            BindingType = ParameterBindingType.Constant;
        }

        public ConstantSource(object? value, string dataType = "double")
        {
            BindingType = ParameterBindingType.Constant;
            Value = value;
            DataType = dataType;
        }

        public override object? GetValue(IParameterContext? context, ILogger? logger = null)
        {
            logger?.LogInfo($"参数源获取：常量值，值={Value}，类型={Value?.GetType().Name ?? "null"}", "ParameterSource");
            return Value;
        }

        public override ParameterSource Clone()
        {
            return new ConstantSource(Value, DataType);
        }
    }

    /// <summary>
    /// 节点输出源 - 绑定到其他节点的输出
    /// </summary>
    public class NodeOutputSource : ParameterSource
    {
        /// <summary>
        /// 源节点ID
        /// </summary>
        public string NodeId { get; set; } = string.Empty;

        /// <summary>
        /// 输出端口名称
        /// </summary>
        public string OutputName { get; set; } = string.Empty;

        /// <summary>
        /// 输出索引（用于多值输出）
        /// </summary>
        public int? Index { get; set; }

        /// <summary>
        /// 属性路径（用于访问复合对象的属性，如 "Center.X"）
        /// </summary>
        public string? PropertyPath { get; set; }

        public NodeOutputSource()
        {
            BindingType = ParameterBindingType.NodeOutput;
        }

        public NodeOutputSource(string nodeId, string outputName, int? index = null, string? propertyPath = null)
        {
            BindingType = ParameterBindingType.NodeOutput;
            NodeId = nodeId;
            OutputName = outputName;
            Index = index;
            PropertyPath = propertyPath;
        }

        public override object? GetValue(IParameterContext? context, ILogger? logger = null)
        {
            logger?.LogInfo($"参数源获取：节点输出，节点ID={NodeId}，输出名={OutputName}，索引={Index}，属性路径={PropertyPath ?? "(无)"}", "ParameterSource");

            if (context == null || string.IsNullOrEmpty(NodeId) || string.IsNullOrEmpty(OutputName))
            {
                logger?.LogError($"参数源获取失败：上下文为空或节点信息不完整，节点ID={NodeId}，输出名={OutputName}", "ParameterSource");
                return null;
            }

            var value = context.GetNodeOutputValue(NodeId, OutputName, Index, PropertyPath);

            if (value == null)
            {
                logger?.LogWarning($"参数源获取警告：节点 {NodeId}.{OutputName} 返回null", "ParameterSource");
            }
            else
            {
                logger?.LogSuccess($"参数源获取成功：节点 {NodeId}.{OutputName}，值类型={value.GetType().Name}", "ParameterSource");
            }

            return value;
        }

        public override ParameterSource Clone()
        {
            return new NodeOutputSource(NodeId, OutputName, Index, PropertyPath)
            {
                DataType = DataType
            };
        }
    }

    /// <summary>
    /// 表达式源 - 使用表达式计算值
    /// </summary>
    public class ExpressionSource : ParameterSource
    {
        /// <summary>
        /// 表达式字符串
        /// </summary>
        public string Expression { get; set; } = string.Empty;

        /// <summary>
        /// 表达式变量引用
        /// </summary>
        public Dictionary<string, string> VariableReferences { get; set; } = new();

        public ExpressionSource()
        {
            BindingType = ParameterBindingType.Expression;
        }

        public ExpressionSource(string expression)
        {
            BindingType = ParameterBindingType.Expression;
            Expression = expression;
        }

        public override object? GetValue(IParameterContext? context, ILogger? logger = null)
        {
            logger?.LogInfo($"参数源获取：表达式，表达式={Expression}，变量数={VariableReferences.Count}", "ParameterSource");

            if (context == null || string.IsNullOrEmpty(Expression))
            {
                logger?.LogError($"参数源获取失败：上下文为空或表达式为空", "ParameterSource");
                return null;
            }

            try
            {
                var value = context.EvaluateExpression(Expression, VariableReferences);

                if (value == null)
                {
                    logger?.LogWarning($"参数源获取警告：表达式 {Expression} 计算结果为null", "ParameterSource");
                }
                else
                {
                    logger?.LogSuccess($"参数源获取成功：表达式 {Expression} 计算结果={value}，类型={value.GetType().Name}", "ParameterSource");
                }

                return value;
            }
            catch (Exception ex)
            {
                logger?.LogError($"参数源获取异常：表达式 {Expression} 计算失败 - {ex.Message}", "ParameterSource", ex);
                return null;
            }
        }

        public override ParameterSource Clone()
        {
            var clone = new ExpressionSource(Expression)
            {
                DataType = DataType
            };
            foreach (var kvp in VariableReferences)
            {
                clone.VariableReferences[kvp.Key] = kvp.Value;
            }
            return clone;
        }
    }

    /// <summary>
    /// 变量源 - 引用全局或局部变量
    /// </summary>
    public class VariableSource : ParameterSource
    {
        /// <summary>
        /// 变量名
        /// </summary>
        public string VariableName { get; set; } = string.Empty;

        /// <summary>
        /// 是否为全局变量
        /// </summary>
        public bool IsGlobal { get; set; }

        public VariableSource()
        {
            BindingType = ParameterBindingType.Variable;
        }

        public VariableSource(string variableName, bool isGlobal = false)
        {
            BindingType = ParameterBindingType.Variable;
            VariableName = variableName;
            IsGlobal = isGlobal;
        }

        public override object? GetValue(IParameterContext? context, ILogger? logger = null)
        {
            logger?.LogInfo($"参数源获取：变量，变量名={VariableName}，是否全局={IsGlobal}", "ParameterSource");

            if (context == null || string.IsNullOrEmpty(VariableName))
            {
                logger?.LogError($"参数源获取失败：上下文为空或变量名为空", "ParameterSource");
                return null;
            }

            var value = context.GetVariableValue(VariableName, IsGlobal);

            if (value == null)
            {
                logger?.LogWarning($"参数源获取警告：变量 {VariableName} (全局={IsGlobal}) 返回null", "ParameterSource");
            }
            else
            {
                logger?.LogSuccess($"参数源获取成功：变量 {VariableName} (全局={IsGlobal})，值类型={value.GetType().Name}", "ParameterSource");
            }

            return value;
        }

        public override ParameterSource Clone()
        {
            return new VariableSource(VariableName, IsGlobal)
            {
                DataType = DataType
            };
        }
    }

    /// <summary>
    /// 参数上下文接口 - 用于解析绑定值
    /// </summary>
    public interface IParameterContext
    {
        /// <summary>
        /// 获取节点输出值
        /// </summary>
        object? GetNodeOutputValue(string nodeId, string outputName, int? index = null, string? propertyPath = null);

        /// <summary>
        /// 计算表达式
        /// </summary>
        object? EvaluateExpression(string expression, Dictionary<string, string>? variableReferences = null);

        /// <summary>
        /// 获取变量值
        /// </summary>
        object? GetVariableValue(string variableName, bool isGlobal = false);
    }
}
