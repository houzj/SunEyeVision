using System;
using System.Collections.Generic;
using System.Diagnostics;
using OpenCvSharp;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.SDK.Execution.Results;

namespace SunEyeVision.Plugin.SDK.Core
{
    /// <summary>
    /// 工具执行器 - 统一所有工具执行的唯一入口
    /// </summary>
    /// <remarks>
    /// 核心执行器，确保工具调试和工作流执行使用相同的执行路径。
    /// 
    /// 主要功能：
    /// 1. 参数绑定解析
    /// 2. 执行上下文管理
    /// 3. 结果缓存
    /// 4. 错误处理
    /// 
    /// 使用示例：
    /// <code>
    /// var executor = new ToolExecutor(new ParameterResolver());
    /// 
    /// // 执行工具
    /// var result = executor.Run&lt;CircleFindResult&gt;(
    ///     tool, 
    ///     parameters, 
    ///     inputImage, 
    ///     bindings, 
    ///     context);
    /// </code>
    /// </remarks>
    public class ToolExecutor
    {
        private readonly IParameterResolver _parameterResolver;

        /// <summary>
        /// 创建工具执行器
        /// </summary>
        /// <param name="parameterResolver">参数解析器</param>
        public ToolExecutor(IParameterResolver parameterResolver)
        {
            _parameterResolver = parameterResolver ?? throw new ArgumentNullException(nameof(parameterResolver));
        }

        /// <summary>
        /// 执行工具（泛型版本）
        /// </summary>
        /// <typeparam name="TResult">结果类型</typeparam>
        /// <param name="tool">工具实例</param>
        /// <param name="parameters">参数</param>
        /// <param name="inputImage">输入图像</param>
        /// <param name="bindings">参数绑定列表（可选）</param>
        /// <param name="context">执行上下文（可选）</param>
        /// <returns>执行结果</returns>
        public TResult Run<TResult>(
            ITool tool,
            ToolParameters parameters,
            Mat inputImage,
            IList<ParameterBinding>? bindings = null,
            IDictionary<string, ToolResults>? context = null)
            where TResult : ToolResults, new()
        {
            if (tool == null)
                throw new ArgumentNullException(nameof(tool));

            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            var stopwatch = Stopwatch.StartNew();

            try
            {
                // 1. 应用参数绑定
                if (bindings != null && bindings.Count > 0 && context != null)
                {
                    var applyResult = ApplyBindings(parameters, bindings, context);
                    if (!applyResult.IsSuccess)
                    {
                        return ToolResults.CreateError<TResult>(
                            $"参数绑定失败: {string.Join("; ", applyResult.Errors)}");
                    }
                }

                // 2. 执行工具
                var result = tool.Run(inputImage, parameters);

                stopwatch.Stop();
                result.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;

                return (TResult)result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return ToolResults.CreateError<TResult>(ex.Message, ex);
            }
        }

        /// <summary>
        /// 执行工具（非泛型版本）
        /// </summary>
        /// <param name="tool">工具实例</param>
        /// <param name="parameters">参数</param>
        /// <param name="inputImage">输入图像</param>
        /// <param name="bindings">参数绑定列表（可选）</param>
        /// <param name="context">执行上下文（可选）</param>
        /// <returns>执行结果</returns>
        public ToolResults Run(
            ITool tool,
            ToolParameters parameters,
            Mat inputImage,
            IList<ParameterBinding>? bindings = null,
            IDictionary<string, ToolResults>? context = null)
        {
            if (tool == null)
                throw new ArgumentNullException(nameof(tool));

            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            var stopwatch = Stopwatch.StartNew();

            try
            {
                // 1. 应用参数绑定
                if (bindings != null && bindings.Count > 0 && context != null)
                {
                    var applyResult = ApplyBindings(parameters, bindings, context);
                    if (!applyResult.IsSuccess)
                    {
                        return ToolResults.CreateError<GenericToolResults>(
                            $"参数绑定失败: {string.Join("; ", applyResult.Errors)}");
                    }
                }

                // 2. 执行工具
                var result = tool.Run(inputImage, parameters);

                stopwatch.Stop();
                result.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return ToolResults.CreateError<GenericToolResults>(ex.Message, ex);
            }
        }

        /// <summary>
        /// 执行工具（强类型版本）
        /// </summary>
        /// <typeparam name="TParams">参数类型</typeparam>
        /// <typeparam name="TResult">结果类型</typeparam>
        /// <param name="tool">工具实例</param>
        /// <param name="parameters">参数</param>
        /// <param name="inputImage">输入图像</param>
        /// <param name="bindings">参数绑定列表（可选）</param>
        /// <param name="context">执行上下文（可选）</param>
        /// <returns>执行结果</returns>
        public TResult Run<TParams, TResult>(
            ITool<TParams, TResult> tool,
            TParams parameters,
            Mat inputImage,
            IList<ParameterBinding>? bindings = null,
            IDictionary<string, ToolResults>? context = null)
            where TParams : ToolParameters, new()
            where TResult : ToolResults, new()
        {
            if (tool == null)
                throw new ArgumentNullException(nameof(tool));

            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            var stopwatch = Stopwatch.StartNew();

            try
            {
                // 1. 应用参数绑定
                if (bindings != null && bindings.Count > 0 && context != null)
                {
                    var applyResult = ApplyBindings(parameters, bindings, context);
                    if (!applyResult.IsSuccess)
                    {
                        return ToolResults.CreateError<TResult>(
                            $"参数绑定失败: {string.Join("; ", applyResult.Errors)}");
                    }
                }

                // 2. 执行工具
                var result = tool.Run(inputImage, parameters);

                stopwatch.Stop();
                result.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return ToolResults.CreateError<TResult>(ex.Message, ex);
            }
        }

        /// <summary>
        /// 应用参数绑定
        /// </summary>
        private ParameterApplyResult ApplyBindings(
            ToolParameters parameters,
            IList<ParameterBinding> bindings,
            IDictionary<string, ToolResults> context)
        {
            var container = new ParameterBindingContainer();
            foreach (var binding in bindings)
            {
                container.SetBinding(binding);
            }
            return _parameterResolver.ApplyToParameters(parameters, container, context);
        }
    }
}
