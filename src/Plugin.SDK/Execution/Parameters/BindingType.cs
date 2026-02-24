using System;

namespace SunEyeVision.Plugin.SDK.Execution.Parameters
{
    /// <summary>
    /// 参数绑定类型
    /// </summary>
    /// <remarks>
    /// 定义参数值的来源方式：
    /// - Constant: 常量值，直接设置固定值
    /// - DynamicBinding: 动态绑定，从父节点输出获取值
    /// 
    /// 使用示例：
    /// <code>
    /// // 常量绑定
    /// var constantBinding = new ParameterBinding
    /// {
    ///     ParameterName = "Threshold",
    ///     BindingType = BindingType.Constant,
    ///     ConstantValue = 128
    /// };
    /// 
    /// // 动态绑定
    /// var dynamicBinding = new ParameterBinding
    /// {
    ///     ParameterName = "MinRadius",
    ///     BindingType = BindingType.DynamicBinding,
    ///     SourceNodeId = "node_001",
    ///     SourceProperty = "DetectedRadius"
    /// };
    /// </code>
    /// </remarks>
    public enum BindingType
    {
        /// <summary>
        /// 常量值绑定
        /// </summary>
        /// <remarks>
        /// 参数使用固定的常量值，不依赖其他节点的输出。
        /// 适用于需要手动设置参数值的场景。
        /// </remarks>
        Constant = 0,

        /// <summary>
        /// 动态绑定
        /// </summary>
        /// <remarks>
        /// 参数值从父节点的输出属性动态获取。
        /// 支持运行时自动解析，实现节点间的数据传递。
        /// </remarks>
        DynamicBinding = 1,

        /// <summary>
        /// 表达式绑定（预留扩展）
        /// </summary>
        /// <remarks>
        /// 参数值通过表达式计算得出。
        /// 支持简单的数学运算和属性访问。
        /// 示例: "$node1.Radius * 1.5" 或 "$node1.Center.X + 10"
        /// </remarks>
        Expression = 2
    }
}
