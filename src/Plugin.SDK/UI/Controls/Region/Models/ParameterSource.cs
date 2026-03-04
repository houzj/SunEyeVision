using System;
using System.Collections.Generic;

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
        public abstract object? GetValue(IParameterContext? context);

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

        public override object? GetValue(IParameterContext? context)
        {
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

        public override object? GetValue(IParameterContext? context)
        {
            if (context == null || string.IsNullOrEmpty(NodeId) || string.IsNullOrEmpty(OutputName))
                return null;

            return context.GetNodeOutputValue(NodeId, OutputName, Index, PropertyPath);
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

        public override object? GetValue(IParameterContext? context)
        {
            if (context == null || string.IsNullOrEmpty(Expression))
                return null;

            return context.EvaluateExpression(Expression, VariableReferences);
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

        public override object? GetValue(IParameterContext? context)
        {
            if (context == null || string.IsNullOrEmpty(VariableName))
                return null;

            return context.GetVariableValue(VariableName, IsGlobal);
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
