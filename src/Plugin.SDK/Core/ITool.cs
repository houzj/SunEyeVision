using System;
using System.Threading.Tasks;
using OpenCvSharp;
using SunEyeVision.Plugin.SDK.Models.Roi;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.SDK.Execution.Results;
using SunEyeVision.Plugin.SDK.Validation;

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
    /// 工具非泛型基接口 - 提供统一的弱类型访问入口
    /// </summary>
    /// <remarks>
    /// 框架层通过此接口统一访问所有工具，无需处理泛型参数。
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
        /// 执行工具（弱类型）
        /// </summary>
        /// <param name="image">输入图像</param>
        /// <param name="parameters">参数实例</param>
        /// <returns>执行结果</returns>
        ToolResults Execute(Mat image, ToolParameters parameters);

        /// <summary>
        /// 获取默认参数
        /// </summary>
        /// <returns>默认参数实例</returns>
        ToolParameters GetDefaultParameters();

        /// <summary>
        /// 验证参数
        /// </summary>
        /// <param name="parameters">参数实例</param>
        /// <returns>验证结果</returns>
        ValidationResult ValidateParameters(ToolParameters parameters);
    }

    /// <summary>
    /// 泛型工具接口
    /// </summary>
    /// <typeparam name="TParams">参数类型</typeparam>
    /// <typeparam name="TResult">结果类型</typeparam>
    /// <remarks>
    /// 类型安全的工具接口，替代原来的弱类型 IImageProcessor。
    /// 参数和结果都是强类型，提供编译时类型检查。
    /// 
    /// 使用示例：
    /// <code>
    /// public class CircleFindParams : ToolParameters { ... }
    /// public class CircleFindResult : ToolResults { ... }
    /// 
    /// public class CircleFindTool : ITool&lt;CircleFindParams, CircleFindResult&gt;
    /// {
    ///     public string Name => "CircleFind";
    ///     public CircleFindResult Execute(ImageData image, CircleFindParams parameters)
    ///     {
    ///         // 实现...
    ///     }
    /// }
    /// </code>
    /// </remarks>
    public interface ITool<TParams, TResult> : ITool
        where TParams : ToolParameters, new()
        where TResult : ToolResults, new()
    {
        /// <summary>
        /// 执行工具（同步）
        /// </summary>
        /// <param name="image">输入图像</param>
        /// <param name="parameters">输入参数</param>
        /// <returns>执行结果</returns>
        new TResult Execute(Mat image, TParams parameters);

        /// <summary>
        /// 执行工具（异步）
        /// </summary>
        /// <param name="image">输入图像</param>
        /// <param name="parameters">输入参数</param>
        /// <returns>执行结果</returns>
        Task<TResult> ExecuteAsync(Mat image, TParams parameters);

        /// <summary>
        /// 验证参数
        /// </summary>
        /// <param name="parameters">输入参数</param>
        /// <returns>验证结果</returns>
        new ValidationResult ValidateParameters(TParams parameters);

        /// <summary>
        /// 获取默认参数
        /// </summary>
        new TParams GetDefaultParameters();

        #region ITool 显式实现

        Type ITool.ParamsType => typeof(TParams);
        Type ITool.ResultType => typeof(TResult);

        ToolResults ITool.Execute(Mat image, ToolParameters parameters)
        {
            if (parameters is TParams typedParams)
                return Execute(image, typedParams);
            throw new ArgumentException($"参数类型错误：期望 {typeof(TParams).Name}");
        }

        ToolParameters ITool.GetDefaultParameters() => GetDefaultParameters();

        ValidationResult ITool.ValidateParameters(ToolParameters parameters)
        {
            if (parameters is TParams typedParams)
                return ValidateParameters(typedParams);
            return ValidationResult.Failure($"参数类型错误：期望 {typeof(TParams).Name}");
        }

        #endregion
    }

    /// <summary>
    /// 异步工具接口（扩展）
    /// </summary>
    public interface IAsyncTool<TParams, TResult> : ITool<TParams, TResult>
        where TParams : ToolParameters, new()
        where TResult : ToolResults, new()
    {
        /// <summary>
        /// 执行工具（带取消支持和进度报告）
        /// </summary>
        /// <param name="image">输入图像</param>
        /// <param name="parameters">输入参数</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <param name="progress">进度报告器</param>
        /// <returns>执行结果</returns>
        Task<TResult> ExecuteAsync(
            Mat image,
            TParams parameters,
            System.Threading.CancellationToken cancellationToken,
            IProgress<double>? progress = null);
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
        TResult Execute(Mat image, TParams parameters, IRoi roi);

        /// <summary>
        /// 执行工具（指定ROI，异步）
        /// </summary>
        Task<TResult> ExecuteAsync(Mat image, TParams parameters, IRoi roi);
    }
}
