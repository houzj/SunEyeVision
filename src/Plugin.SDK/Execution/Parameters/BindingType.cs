using System;

namespace SunEyeVision.Plugin.SDK.Execution.Parameters
{
    /// <summary>
    /// 参数绑定类型
    /// </summary>
    /// <remarks>
    /// 定义参数值的来源方式，只有两种基本绑定类型：
    /// - Constant: 常量绑定，使用固定值
    /// - Binding: 动态绑定，从其他节点输出获取值
    /// 
    /// 注意：Expression（表达式）和 RuntimeInjection（运行时注入）是值获取方式，不是绑定类型。
    /// 表达式是可选的值转换功能，两种绑定模式都可用。
    /// 运行时注入通过执行前直接设置参数值实现，不需要单独的绑定类型。
    /// 
    /// 使用示例：
    /// <code>
    /// // 常量绑定
    /// var constantBinding = ParameterBinding.CreateConstant("Threshold", 128);
    /// 
    /// // 动态绑定
    /// var dynamicBinding = ParameterBinding.CreateBinding("MinRadius", "node_001", "DetectedRadius");
    /// 
    /// // 带转换表达式的绑定
    /// var bindingWithTransform = ParameterBinding.CreateBinding("Radius", "node_001", "Radius", "value * 0.9");
    /// </code>
    /// </remarks>
    public enum BindingType
    {
        /// <summary>
        /// 常量绑定 - 使用固定值
        /// </summary>
        /// <remarks>
        /// 参数使用固定的常量值，不依赖其他节点的输出。
        /// 适用于需要手动设置参数值的场景。
        /// 数据类型由 ParamDataType 枚举或 BindableParameter.DataType 决定。
        /// </remarks>
        Constant = 0,

        /// <summary>
        /// 动态绑定 - 从其他节点输出获取值
        /// </summary>
        /// <remarks>
        /// 参数值从其他节点的输出属性动态获取。
        /// 支持运行时自动解析，实现节点间的数据传递。
        /// 可选地使用 TransformExpression 进行值转换。
        /// </remarks>
        Binding = 1
    }
}
