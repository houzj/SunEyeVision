using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using OpenCvSharp;
using SunEyeVision.Core.Interfaces;
using SunEyeVision.Core.Models;
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.SDK.Execution.Results;

namespace SunEyeVision.Workflow
{
    /// <summary>
    /// 算法节点
    /// </summary>
    public class AlgorithmNode : WorkflowNode
    {
        /// <summary>
        /// 工具实例
        /// </summary>
        public ITool? Tool { get; set; }

        /// <summary>
        /// 上次执行结果
        /// </summary>
        public AlgorithmResult LastResult { get; private set; }

        /// <summary>
        /// 上次工具执行结果（保留原始完整结果）
        /// </summary>
        public ToolResults? LastToolResult { get; private set; }

        public AlgorithmNode(string id, string name, ITool tool)
            : base(id, name, NodeType.Algorithm)
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
                    if (ParameterBindings?.Count > 0 && nodeResults != null)
                    {
                        var resolver = new ParameterResolver();
                        var applyResult = resolver.ApplyToParameters(typedParams, ParameterBindings, nodeResults);
                        
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
        private ToolParameters GetTypedParameters()
        {
            if (Tool == null)
                return new GenericToolParameters();

            // 使用反射创建默认参数实例
            var defaultParams = (ToolParameters?)Activator.CreateInstance(Tool.ParamsType) 
                ?? new GenericToolParameters();
            
            // 如果 Parameters 为空，返回默认参数
            if (Parameters == null || Parameters.Values.Count == 0)
                return defaultParams;

            // 从 AlgorithmParameters 复制值到强类型参数
            var props = defaultParams.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var prop in props)
            {
                if (!prop.CanWrite) continue;
                if (prop.Name == "Version") continue;

                // 尝试从 Parameters 获取值
                var getMethod = typeof(AlgorithmParameters).GetMethod("TryGet")?.MakeGenericMethod(prop.PropertyType);
                if (getMethod != null)
                {
                    var parameters = new object[] { prop.Name, null };
                    var found = (bool?)getMethod.Invoke(Parameters, parameters);
                    if (found == true && parameters[1] != null)
                    {
                        try
                        {
                            prop.SetValue(defaultParams, parameters[1]);
                        }
                        catch
                        {
                            // 转换失败时使用默认值
                        }
                    }
                }
            }

            return defaultParams;
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

            // 尝试获取输出图像
            Mat? outputImage = null;
            var resultItems = result.GetResultItems();
            foreach (var item in resultItems)
            {
                if (item.Value is Mat mat)
                {
                    outputImage = mat;
                    break;
                }
            }

            var algorithmResult = AlgorithmResult.CreateSuccess(
                outputImage ?? new Mat(),
                result.ExecutionTimeMs);
            
            // 保留结果项和原始工具结果引用
            algorithmResult.ResultItems = resultItems;
            algorithmResult.ToolResults = result;
            
            return algorithmResult;
        }
    }
}
