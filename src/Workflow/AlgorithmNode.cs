using System;
using System.Reflection;
using OpenCvSharp;
using SunEyeVision.Core.Interfaces;
using SunEyeVision.Core.Models;
using SunEyeVision.Plugin.SDK.Core;

namespace SunEyeVision.Workflow
{
    /// <summary>
    /// 算法ڵ
    /// </summary>
    public class AlgorithmNode : WorkflowNode
    {
        /// <summary>
        /// 图像?        /// </summary>
        public IImageProcessor Processor { get; set; }

        /// <summary>
        /// 上ִн
        /// </summary>
        public AlgorithmResult LastResult { get; private set; }

        public AlgorithmNode(string id, string name, IImageProcessor processor)
            : base(id, name, NodeType.Algorithm)
        {
            Processor = processor;
        }

        /// <summary>
        /// ִнڵ
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
                // ʹ÷ Execute [
                var executeMethod = Processor.GetType().GetMethod("Execute", new[] { typeof(Mat), typeof(AlgorithmParameters) });
                if (executeMethod != null)
                {
                    LastResult = executeMethod.Invoke(Processor, new object[] { inputImage, Parameters }) as AlgorithmResult;
                }
                else
                {
                    // 否则使用 Process 
                    var resultImage = Processor.Process(inputImage);
                    LastResult = AlgorithmResult.CreateSuccess(resultImage as Mat ?? inputImage, 0);
                }
                OnAfterExecute(LastResult);
                return LastResult;
            }
            catch (Exception ex)
            {
                var result = AlgorithmResult.CreateError($"ڵ {Name} ִʧ: {ex.Message}");
                OnAfterExecute(result);
                return result;
            }
        }
    }
}
