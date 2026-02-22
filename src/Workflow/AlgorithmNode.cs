using System;
using System.Reflection;
using SunEyeVision.Core.Interfaces;
using SunEyeVision.Core.Models;
using SunEyeVision.Plugin.Abstractions.Core;

namespace SunEyeVision.Workflow
{
    /// <summary>
    /// 绠楁硶鑺傜偣
    /// </summary>
    public class AlgorithmNode : WorkflowNode
    {
        /// <summary>
        /// 鍥惧儚澶勭悊鍣?
        /// </summary>
        public IImageProcessor Processor { get; set; }

        /// <summary>
        /// 涓婃鎵ц缁撴灉
        /// </summary>
        public AlgorithmResult LastResult { get; private set; }

        public AlgorithmNode(string id, string name, IImageProcessor processor)
            : base(id, name, NodeType.Algorithm)
        {
            Processor = processor;
        }

        /// <summary>
        /// 鎵ц鑺傜偣
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
                // 灏濊瘯浣跨敤鍙嶅皠璋冪敤 Execute 鏂规硶锛堝鏋滃瓨鍦級
                var executeMethod = Processor.GetType().GetMethod("Execute", new[] { typeof(Mat), typeof(AlgorithmParameters) });
                if (executeMethod != null)
                {
                    LastResult = executeMethod.Invoke(Processor, new object[] { inputImage, Parameters }) as AlgorithmResult;
                }
                else
                {
                    // 鍚﹀垯浣跨敤 Process 鏂规硶
                    var resultImage = Processor.Process(inputImage);
                    LastResult = AlgorithmResult.CreateSuccess(resultImage as Mat ?? inputImage, 0);
                }
                OnAfterExecute(LastResult);
                return LastResult;
            }
            catch (Exception ex)
            {
                var result = AlgorithmResult.CreateError($"鑺傜偣 {Name} 鎵ц澶辫触: {ex.Message}");
                OnAfterExecute(result);
                return result;
            }
        }
    }
}
