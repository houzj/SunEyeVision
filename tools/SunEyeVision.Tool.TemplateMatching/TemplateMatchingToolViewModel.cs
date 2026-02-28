using System.Collections.Generic;
using System.Collections.ObjectModel;
using SunEyeVision.Plugin.SDK;
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.SDK.Metadata;
using SunEyeVision.Plugin.SDK.UI.Controls;
using SunEyeVision.Plugin.SDK.ViewModels;

namespace SunEyeVision.Tool.TemplateMatching
{
    public class TemplateMatchingToolViewModel : ToolViewModelBase
    {
        private string _method = "TM_CCOEFF_NORMED";
        private double _threshold = 0.8;
        private int _maxMatches = 1;
        private bool _multiScale = false;

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

        private ParameterBindingMode _thresholdBindingMode = ParameterBindingMode.Constant;
        private string _thresholdBindingSource = string.Empty;

        /// <summary>
        /// 可用绑定源列表（用于参数绑定）
        /// </summary>
        public List<string> AvailableBindings { get; } = new List<string>();

        /// <summary>
        /// 阈值绑定模式
        /// </summary>
        public ParameterBindingMode ThresholdBindingMode
        {
            get => _thresholdBindingMode;
            set => SetProperty(ref _thresholdBindingMode, value);
        }

        /// <summary>
        /// 阈值绑定源
        /// </summary>
        public string ThresholdBindingSource
        {
            get => _thresholdBindingSource;
            set => SetProperty(ref _thresholdBindingSource, value);
        }

        #endregion

        public string Method
        {
            get => _method;
            set
            {
                SetProperty(ref _method, value);
                SetParamValue("Method", value);
            }
        }

        public double Threshold
        {
            get => _threshold;
            set
            {
                SetProperty(ref _threshold, value);
                SetParamValue("Threshold", value);
            }
        }

        public int MaxMatches
        {
            get => _maxMatches;
            set
            {
                SetProperty(ref _maxMatches, value);
                SetParamValue("MaxMatches", value);
            }
        }

        public bool MultiScale
        {
            get => _multiScale;
            set
            {
                SetProperty(ref _multiScale, value);
                SetParamValue("MultiScale", value);
            }
        }

        public string[] Methods { get; } = {
            "TM_CCOEFF_NORMED", "TM_CCOEFF",
            "TM_SQDIFF", "TM_SQDIFF_NORMED",
            "TM_CCORR", "TM_CCORR_NORMED"
        };

        public override void Initialize(string toolId, IToolPlugin? toolPlugin, ToolMetadata? toolMetadata)
        {
            // 调用基类初始化（初始化 ToolRunner）
            base.Initialize(toolId, toolPlugin, toolMetadata);
            ToolName = toolMetadata?.DisplayName ?? "模板匹配";
        }

        /// <summary>
        /// 获取当前运行参数
        /// </summary>
        protected override ToolParameters GetRunParameters()
        {
            return new TemplateMatchingParameters
            {
                Threshold = this.Threshold
            };
        }
    }
}
