using System;
using System.Reflection;
using SunEyeVision.Core.Interfaces;
using SunEyeVision.Core.Models;
using SunEyeVision.Plugin.Abstractions.Core;

namespace SunEyeVision.Workflow
{
    /// <summary>
    /// 算法节点
    /// </summary>
    public class AlgorithmNode : WorkflowNode
    {
        /// <summary>
        /// 图像处理器
        /// </summary>
        public IImageProcessor Processor { get; set; }

        /// <summary>
        /// 上次执行结果
        /// </summary>
        public AlgorithmResult LastResult { get; private set; }

        public AlgorithmNode(string id, string name, IImageProcessor processor)
            : base(id, name, NodeType.Algorithm)
        {
            Processor = processor;
        }

        /// <summary>
        /// 执行节点
        /// </summary>
        public AlgorithmResult Execute(Mat inputImage)
        {
            if (!IsEnabled)
            {
                return AlgorithmResult.CreateSuccess(inputImage, 0);
            }

            OnBeforeExecute();

            try
            {
                // 尝试使用反射调用 Execute 方法（如果存在）
                var executeMethod = Processor.GetType().GetMethod("Execute", new[] { typeof(Mat), typeof(AlgorithmParameters) });
                if (executeMethod != null)
                {
                    LastResult = executeMethod.Invoke(Processor, new object[] { inputImage, Parameters }) as AlgorithmResult;
                }
                else
                {
                    // 否则使用 Process 方法
                    var resultImage = Processor.Process(inputImage);
                    LastResult = AlgorithmResult.CreateSuccess(resultImage as Mat ?? inputImage, 0);
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
    }
}
