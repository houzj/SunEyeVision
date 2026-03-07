using System.Collections.ObjectModel;
using SunEyeVision.Plugin.SDK;
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.SDK.Metadata;
using SunEyeVision.Plugin.SDK.UI.Controls;
using SunEyeVision.Plugin.SDK.ViewModels;

namespace SunEyeVision.Tool.OCR
{
    public class OCRToolViewModel : ToolViewModelBase
    {
        private string _language = "chi_sim+eng";
        private string _dataPath = "";
        private string _charWhitelist = "";
        private int _psm = 3;

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

        public string Language
        {
            get => _language;
            set
            {
                if (SetProperty(ref _language, value, "语言"))
                    SetParamValue("Language", value);
            }
        }

        public string DataPath
        {
            get => _dataPath;
            set
            {
                if (SetProperty(ref _dataPath, value, "数据路径"))
                    SetParamValue("DataPath", value);
            }
        }

        public string CharWhitelist
        {
            get => _charWhitelist;
            set
            {
                if (SetProperty(ref _charWhitelist, value, "字符白名单"))
                    SetParamValue("CharWhitelist", value);
            }
        }

        public int Psm
        {
            get => _psm;
            set
            {
                if (SetProperty(ref _psm, value, "页面分割模式"))
                    SetParamValue("Psm", value);
            }
        }

        public string[] Languages { get; } = {
            "chi_sim", "chi_tra", "eng", "chi_sim+eng",
            "jpn", "kor", "fra", "deu", "spa"
        };

        public override void Initialize(string toolId, IToolPlugin? toolPlugin, ToolMetadata? toolMetadata)
        {
            // 调用基类初始化（初始化 ToolRunner）
            base.Initialize(toolId, toolPlugin, toolMetadata);
            ToolName = toolMetadata?.DisplayName ?? "OCR识别";
        }

        /// <summary>
        /// 获取当前运行参数
        /// </summary>
        protected override ToolParameters GetRunParameters()
        {
            return new OCRParameters
            {
                Language = this.Language,
                ConfThreshold = 80.0
            };
        }
    }
}
