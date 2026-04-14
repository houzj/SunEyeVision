using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using OpenCvSharp;
using SunEyeVision.Plugin.SDK.Logging;
using SunEyeVision.Core.Models;
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.SDK.Execution.Results;

namespace SunEyeVision.Workflow
{
    /// <summary>
    /// 算法节点
    /// </summary>
    public class AlgorithmNode : WorkflowNodeBase
    {
        /// <summary>
        /// 工具实例
        /// </summary>
        public IToolPlugin? Tool { get; set; }

        /// <summary>
        /// 上次执行结果
        /// </summary>
        public AlgorithmResult LastResult { get; private set; }

        /// <summary>
        /// 上次工具执行结果（保留原始完整结果）
        /// </summary>
        public ToolResults? LastToolResult { get; private set; }

        public AlgorithmNode(string id, string name, string dispName, IToolPlugin tool)
            : base(id, name, dispName, "Algorithm")
        {
            Tool = tool;
        }

        /// <summary>
        /// 执行节点算法
        /// </summary>
        public AlgorithmResult Execute(Mat inputImage)
        {
            return Execute(inputImage, null);
        }

        /// <summary>
        /// 执行节点算法（带绑定解析）
        /// </summary>
        /// <param name="inputImage">输入图像</param>
        /// <param name="nodeResults">其他节点的执行结果缓存（用于动态绑定解析）</param>
        public AlgorithmResult Execute(Mat inputImage, IDictionary<string, ToolResults>? nodeResults)
        {
            if (!IsEnabled)
            {
                return AlgorithmResult.CreateSuccess(inputImage, 0);
            }

            OnBeforeExecute();

            try
            {
                // 优先使用新的 ITool 接口
                if (Tool != null)
                {
                    var typedParams = GetTypedParameters();
                    
                    // 应用参数绑定解析
                    if (ParamSettings?.Count > 0 && nodeResults != null)
                    {
                        var resolver = new ParameterResolver();
                        var applyResult = resolver.ApplyToParameters(typedParams, ParamSettings, nodeResults);
                        
                        if (!applyResult.IsSuccess && applyResult.Errors.Count > 0)
                        {
                            var errors = string.Join("; ", applyResult.Errors);
                            var result = AlgorithmResult.CreateError($"参数绑定解析失败: {errors}");
                            OnAfterExecute(result);
                            return result;
                        }
                    }
                    
                    var toolResult = Tool.Run(inputImage, typedParams);
                    LastToolResult = toolResult;  // 保存原始工具结果
                    LastResult = ConvertToAlgorithmResult(toolResult);
                }
                else
                {
                    LastResult = AlgorithmResult.CreateError("节点未配置工具实例");
                }

                OnAfterExecute(LastResult);
                return LastResult;
            }
            catch (Exception ex)
            {
                var result = AlgorithmResult.CreateError($"节点 {Name} 执行失败: {ex.Message}");
                OnAfterExecute(result);
                return result;
            }
        }

        /// <summary>
        /// 获取强类型参数
        /// </summary>
        /// <remarks>
        /// 直接返回节点的 Parameters 属性，避免反射转换开销。
        /// 参数已由工作流引擎在创建节点时设置正确的类型。
        /// </remarks>
        private ToolParameters GetTypedParameters()
        {
            // 如果参数已经是正确的类型，直接返回
            if (Tool != null && Parameters.GetType() == Tool.ParamsType)
                return Parameters;

            // ✅ 如果参数类型不匹配，直接抛出异常
            throw new InvalidOperationException(
                $"参数类型不匹配。工具 {ToolType} 期望 {Tool.ParamsType?.Name ?? "null"}, 实际为 {Parameters.GetType().Name}");
        }

        /// <summary>
        /// 将 ToolResults 转换为 AlgorithmResult
        /// </summary>
        private AlgorithmResult ConvertToAlgorithmResult(ToolResults result)
        {
            if (!result.IsSuccess)
            {
                return AlgorithmResult.CreateError(result.ErrorMessage ?? "处理失败");
            }

            // 1. 首先尝试通过反射获取 OutputImage 属性（最直接的方式）
            Mat? outputImage = null;
            var outputImageProp = result.GetType().GetProperty("OutputImage");
            if (outputImageProp != null)
            {
                outputImage = outputImageProp.GetValue(result) as Mat;
            }

            // 如果没有找到输出图像，返回错误
            if (outputImage == null || outputImage.Empty())
            {
                return AlgorithmResult.CreateError("工具执行成功但没有输出图像");
            }

            var algorithmResult = AlgorithmResult.CreateSuccess(outputImage, result.ExecutionTimeMs);

            // 保留原始工具结果引用（移除 ResultItems，不再使用 GetResultItems()）
            algorithmResult.ToolResults = result;

            return algorithmResult;
        }
    }
}
