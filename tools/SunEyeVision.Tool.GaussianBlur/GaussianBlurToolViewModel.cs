using System.Collections.Generic;
using System.Collections.ObjectModel;
using SunEyeVision.Plugin.SDK;
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.SDK.Metadata;
using SunEyeVision.Plugin.SDK.UI.Controls;
using SunEyeVision.Plugin.SDK.ViewModels;

namespace SunEyeVision.Tool.GaussianBlur
{
    /// <summary>
    /// 高斯模糊工具调试ViewModel
    /// </summary>
    public class GaussianBlurToolViewModel : ToolViewModelBase
    {
        private int _kernelSize = 5;
        private double _sigma = 1.5;
        private string _borderType = "Reflect";

        #region 图像源选择

        private ImageSourceInfo? _selectedImageSource;

        /// <summary>
        /// 当前选中的图像源
        /// </summary>
        public ImageSourceInfo? SelectedImageSource
        {
            get => _selectedImageSource;
            set => SetProperty(ref _selectedImageSource, value);
        }

        /// <summary>
        /// 可用图像源列表（由工作流上下文提供）
        /// </summary>
        public ObservableCollection<ImageSourceInfo> AvailableImageSources { get; }
            = new ObservableCollection<ImageSourceInfo>();

        #endregion

        #region 参数绑定支持

        private ParameterBindingMode _sigmaBindingMode = ParameterBindingMode.Constant;
        private string _sigmaBindingSource = string.Empty;

        /// <summary>
        /// 可用绑定源列表（用于参数绑定）
        /// </summary>
        public List<string> AvailableBindings { get; } = new List<string>();

        /// <summary>
        /// Sigma绑定模式
        /// </summary>
        public ParameterBindingMode SigmaBindingMode
        {
            get => _sigmaBindingMode;
            set => SetProperty(ref _sigmaBindingMode, value);
        }

        /// <summary>
        /// Sigma绑定源
        /// </summary>
        public string SigmaBindingSource
        {
            get => _sigmaBindingSource;
            set => SetProperty(ref _sigmaBindingSource, value);
        }

        #endregion

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
        /// 获取当前运行参数
        /// </summary>
        protected override ToolParameters GetRunParameters()
        {
            return new GaussianBlurParameters
            {
                KernelSize = this.KernelSize,
                Sigma = this.Sigma
            };
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
