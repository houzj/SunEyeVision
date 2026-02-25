using SunEyeVision.Plugin.SDK;

using SunEyeVision.Plugin.SDK.ViewModels;

using SunEyeVision.Plugin.SDK.Core;

using System.Collections.Generic;

namespace SunEyeVision.Tool.GaussianBlur
{
    /// <summary>
    /// 高斯模糊工具调试ViewModel
    /// </summary>
    public class GaussianBlurToolViewModel : AutoToolDebugViewModelBase
    {
        private int _kernelSize = 5;
        private double _sigma = 1.5;
        private string _borderType = "Reflect";

        /// <summary>
        /// 核大小（必须为奇数）
        /// </summary>
        public int KernelSize
        {
            get => _kernelSize;
            set
            {
                if (value % 2 == 0)
                    value = value + 1; // 确保为奇数
                if (SetProperty(ref _kernelSize, value))
                {
                    SetParamValue("KernelSize", value);
                }
            }
        }

        /// <summary>
        /// 标准差（Sigma）
        /// </summary>
        public double Sigma
        {
            get => _sigma;
            set
            {
                if (value < 0.1)
                    value = 0.1;
                if (SetProperty(ref _sigma, value))
                {
                    SetParamValue("Sigma", value);
                }
            }
        }

        /// <summary>
        /// 边界类型
        /// </summary>
        public string BorderType
        {
            get => _borderType;
            set
            {
                if (SetProperty(ref _borderType, value))
                {
                    SetParamValue("BorderType", value);
                }
            }
        }

        public string[] BorderTypes { get; } = { "Reflect", "Constant", "Replicate", "Default" };

        /// <summary>
        /// 构建参数字典（供基类 Execute 使用）
        /// </summary>
        protected override Dictionary<string, object> BuildParameterDictionary()
        {
            return new Dictionary<string, object>
            {
                ["kernelSize"] = KernelSize,
                ["sigma"] = Sigma,
                ["borderType"] = BorderType
            };
        }

        /// <summary>
        /// 执行成功回调
        /// </summary>
        protected override void OnExecutionCompleted(AlgorithmResult result)
        {
            if (result.Data != null)
            {
                DebugMessage = $"处理参数: KernelSize={KernelSize}, Sigma={Sigma:F2}, BorderType={BorderType}";
            }
        }

        /// <summary>
        /// 重置参数
        /// </summary>
        public override void ResetParameters()
        {
            KernelSize = 5;
            Sigma = 1.5;
            BorderType = "Reflect";
            base.ResetParameters();
        }
    }
}
