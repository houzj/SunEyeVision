using System;
using OpenCvSharp;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.SDK.Execution.Results;

namespace SunEyeVision.Plugin.SDK.Core
{
    /// <summary>
    /// 工具执行选项
    /// </summary>
    public sealed class ToolExecuteOptions
    {
        /// <summary>
        /// 是否启用详细日志
        /// </summary>
        public bool VerboseLogging { get; set; }

        /// <summary>
        /// 超时时间（毫秒），0表示无超时
        /// </summary>
        public int TimeoutMs { get; set; }

        /// <summary>
        /// 是否启用结果缓存
        /// </summary>
        public bool EnableCaching { get; set; } = true;

        /// <summary>
        /// 是否返回中间结果
        /// </summary>
        public bool ReturnIntermediateResults { get; set; }

        /// <summary>
        /// 用户自定义数据
        /// </summary>
        public object? Tag { get; set; }

        /// <summary>
        /// 默认选项
        /// </summary>
        public static ToolExecuteOptions Default => new();
    }

    /// <summary>
    /// 工具非泛型接口 - 提供统一的弱类型访问入口
    /// </summary>
    /// <remarks>
    /// 极简工具接口，只定义必要的元数据和执行入口。
    /// 开发者完全自主决定执行流程、参数验证、错误处理等。
    /// </remarks>
    public interface ITool
    {
        /// <summary>
        /// 工具名称
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 工具描述
        /// </summary>
        string Description { get; }

        /// <summary>
        /// 工具版本
        /// </summary>
        string Version { get; }

        /// <summary>
        /// 工具分类
        /// </summary>
        string Category { get; }

        /// <summary>
        /// 参数类型
        /// </summary>
        Type ParamsType { get; }

        /// <summary>
        /// 结果类型
        /// </summary>
        Type ResultType { get; }

        /// <summary>
        /// 执行工具 - 唯一执行入口
        /// </summary>
        /// <param name="image">输入图像</param>
        /// <param name="parameters">参数</param>
        /// <returns>执行结果</returns>
        ToolResults Run(Mat image, ToolParameters parameters);

        /// <summary>
        /// 是否支持调试窗口
        /// </summary>
        /// <remarks>
        /// 无窗口工具（如 ImageLoadTool）应返回 false，避免创建占位窗口。
        /// UI层在双击节点时会检查此属性，决定是否显示调试窗口。
        /// </remarks>
        bool HasDebugWindow { get; }

        /// <summary>
        /// 创建调试窗口 - 工具可选择实现自己的调试窗口
        /// </summary>
        /// <remarks>
        /// 工具可以选择提供自己的调试窗口，继承自 BaseToolDebugWindow。
        /// 调试窗口只负责参数配置，图像显示和ROI编辑由主界面统一管理。
        /// 
        /// 无窗口工具（如 ImageLoadTool）应：
        /// - HasDebugWindow 返回 false
        /// - CreateDebugWindow() 返回 null
        /// 
        /// 使用示例：
        /// <code>
        /// // 有窗口的工具
        /// public bool HasDebugWindow => true;
        /// public Window? CreateDebugWindow() => new ThresholdToolDebugWindow();
        /// 
        /// // 无窗口的工具
        /// public bool HasDebugWindow => false;
        /// public Window? CreateDebugWindow() => null;
        /// </code>
        /// </remarks>
        /// <returns>调试窗口实例，无窗口工具返回 null</returns>
        System.Windows.Window? CreateDebugWindow();
    }

    /// <summary>
    /// 泛型工具接口 - 类型安全
    /// </summary>
    /// <typeparam name="TParams">参数类型</typeparam>
    /// <typeparam name="TResult">结果类型</typeparam>
    /// <remarks>
    /// 极简工具接口，开发者完全自主实现执行逻辑。
    /// 
    /// 使用示例：
    /// <code>
    /// public class ThresholdTool : ITool&lt;ThresholdParams, ThresholdResult&gt;
    /// {
    ///     public string Name => "阈值分割";
    ///     public string Description => "图像二值化处理";
    ///     public string Version => "1.0.0";
    ///     public string Category => "图像处理";
    ///     
    ///     public ThresholdResult Run(Mat image, ThresholdParams parameters)
    ///     {
    ///         // 使用辅助方法（可选）
    ///         if (!ToolHelpers.ValidateInput(image, out var error))
    ///             return ToolHelpers.Error&lt;ThresholdResult&gt;(error);
    ///         
    ///         var result = new ThresholdResult();
    ///         Cv2.Threshold(image, result.OutputImage, parameters.Threshold, 255, ThresholdType.Binary);
    ///         ToolHelpers.Success(result);
    ///         return result;
    ///     }
    /// }
    /// </code>
    /// </remarks>
    public interface ITool<TParams, TResult> : ITool
        where TParams : ToolParameters, new()
        where TResult : ToolResults, new()
    {
        /// <summary>
        /// 执行工具 - 开发者完全自主实现
        /// </summary>
        /// <param name="image">输入图像</param>
        /// <param name="parameters">参数</param>
        /// <returns>执行结果</returns>
        TResult Run(Mat image, TParams parameters);

        #region ITool 显式实现

        Type ITool.ParamsType => typeof(TParams);
        Type ITool.ResultType => typeof(TResult);

        ToolResults ITool.Run(Mat image, ToolParameters parameters)
        {
            if (parameters is TParams typedParams)
                return Run(image, typedParams);
            throw new ArgumentException($"参数类型错误：期望 {typeof(TParams).Name}");
        }

        /// <summary>
        /// 默认实现：支持调试窗口
        /// 子类可重写此属性以禁用调试窗口
        /// </summary>
        bool ITool.HasDebugWindow => true;

        /// <summary>
        /// 默认实现：返回 null，子类应重写此方法以提供调试窗口
        /// </summary>
        System.Windows.Window? ITool.CreateDebugWindow() => null;

        #endregion
    }

    /// <summary>
    /// 支持ROI的工具接口
    /// </summary>
    public interface IRoiTool<TParams, TResult> : ITool<TParams, TResult>
        where TParams : ToolParameters, new()
        where TResult : ToolResults, new()
    {
        /// <summary>
        /// 执行工具（指定ROI）
        /// </summary>
        TResult Run(Mat image, TParams parameters, Models.Roi.IRoi roi);
    }
}
