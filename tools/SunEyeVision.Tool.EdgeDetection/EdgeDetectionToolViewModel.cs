using System.Collections.Generic;
using System.Collections.ObjectModel;
using SunEyeVision.Plugin.SDK;
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.SDK.Metadata;
using SunEyeVision.Plugin.SDK.UI.Controls;
using SunEyeVision.Plugin.SDK.ViewModels;

namespace SunEyeVision.Tool.EdgeDetection
{
    public class EdgeDetectionToolViewModel : ToolViewModelBase
    {
        private string _algorithm = "Canny";
        private int _threshold1 = 50;
        private int _threshold2 = 150;
        private int _apertureSize = 3;

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

        private ParameterBindingMode _threshold1BindingMode = ParameterBindingMode.Constant;
        private string _threshold1BindingSource = string.Empty;
        private ParameterBindingMode _threshold2BindingMode = ParameterBindingMode.Constant;
        private string _threshold2BindingSource = string.Empty;

        /// <summary>
        /// 可用绑定源列表（用于参数绑定）
        /// </summary>
        public List<string> AvailableBindings { get; } = new List<string>();

        /// <summary>
        /// 阈值1绑定模式
        /// </summary>
        public ParameterBindingMode Threshold1BindingMode
        {
            get => _threshold1BindingMode;
            set => SetProperty(ref _threshold1BindingMode, value);
        }

        /// <summary>
        /// 阈值1绑定源
        /// </summary>
        public string Threshold1BindingSource
        {
            get => _threshold1BindingSource;
            set => SetProperty(ref _threshold1BindingSource, value);
        }

        /// <summary>
        /// 阈值2绑定模式
        /// </summary>
        public ParameterBindingMode Threshold2BindingMode
        {
            get => _threshold2BindingMode;
            set => SetProperty(ref _threshold2BindingMode, value);
        }

        /// <summary>
        /// 阈值2绑定源
        /// </summary>
        public string Threshold2BindingSource
        {
            get => _threshold2BindingSource;
            set => SetProperty(ref _threshold2BindingSource, value);
        }

        #endregion

        public string Algorithm
        {
            get => _algorithm;
            set
            {
                SetProperty(ref _algorithm, value);
                SetParamValue("Algorithm", value);
            }
        }

        public int Threshold1
        {
            get => _threshold1;
            set
            {
                SetProperty(ref _threshold1, value);
                SetParamValue("Threshold1", value);
            }
        }

        public int Threshold2
        {
            get => _threshold2;
            set
            {
                SetProperty(ref _threshold2, value);
                SetParamValue("Threshold2", value);
            }
        }

        public int ApertureSize
        {
            get => _apertureSize;
            set
            {
                SetProperty(ref _apertureSize, value);
                SetParamValue("ApertureSize", value);
            }
        }

        public string[] Algorithms { get; } = { "Canny", "Sobel", "Laplacian", "Scharr", "Prewitt" };

        public override void Initialize(string toolId, IToolPlugin? toolPlugin, ToolMetadata? toolMetadata)
        {
            // 调用基类初始化（初始化 ToolRunner）
            base.Initialize(toolId, toolPlugin, toolMetadata);
            ToolName = toolMetadata?.DisplayName ?? "边缘检测";
        }

        /// <summary>
        /// 获取当前运行参数
        /// </summary>
        protected override ToolParameters GetRunParameters()
        {
            return new EdgeDetectionParameters
            {
                Threshold1 = this.Threshold1,
                Threshold2 = this.Threshold2,
                ApertureSize = this.ApertureSize
            };
        }
    }
}
