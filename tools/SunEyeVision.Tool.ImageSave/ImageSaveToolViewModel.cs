using System.Collections.ObjectModel;
using SunEyeVision.Plugin.SDK;
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.SDK.Metadata;
using SunEyeVision.Plugin.SDK.UI.Controls;
using SunEyeVision.Plugin.SDK.ViewModels;

namespace SunEyeVision.Tool.ImageSave
{
    /// <summary>
    /// ImageSaveTool ViewModel - 图像保存工具的视图模型
    /// </summary>
    public class ImageSaveToolViewModel : ToolViewModelBase
    {
        private string _filePath = "";
        private string _imageFormat = "PNG";
        private int _imageQuality = 95;
        private bool _overwriteExisting = false;

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

        #region 属性

        /// <summary>
        /// 文件路径
        /// </summary>
        public string FilePath
        {
            get => _filePath;
            set
            {
                if (SetProperty(ref _filePath, value, "文件路径"))
                    SetParamValue("FilePath", value);
            }
        }

        /// <summary>
        /// 图像格式
        /// </summary>
        public string ImageFormat
        {
            get => _imageFormat;
            set
            {
                if (SetProperty(ref _imageFormat, value, "图像格式"))
                    SetParamValue("ImageFormat", value);
            }
        }

        /// <summary>
        /// 图像质量
        /// </summary>
        public int ImageQuality
        {
            get => _imageQuality;
            set
            {
                if (SetProperty(ref _imageQuality, value, "图像质量"))
                    SetParamValue("ImageQuality", value);
            }
        }

        /// <summary>
        /// 是否覆盖已有文件
        /// </summary>
        public bool OverwriteExisting
        {
            get => _overwriteExisting;
            set
            {
                if (SetProperty(ref _overwriteExisting, value, "覆盖已有文件"))
                    SetParamValue("OverwriteExisting", value);
            }
        }

        /// <summary>
        /// 图像格式选项
        /// </summary>
        public System.Collections.Generic.List<string> ImageFormats { get; } = new System.Collections.Generic.List<string>
        {
            "PNG", "JPEG", "BMP", "TIFF"
        };

        #endregion

        #region 实现抽象方法

        /// <summary>
        /// 初始化调试界面
        /// </summary>
        public override void Initialize(string toolId, IToolPlugin? toolPlugin, ToolMetadata? toolMetadata)
        {
            // 调用基类初始化（初始化 ToolRunner）
            base.Initialize(toolId, toolPlugin, toolMetadata);
            ToolName = toolMetadata?.DisplayName ?? "图像保存";
        }

        /// <summary>
        /// 获取当前运行参数
        /// </summary>
        protected override ToolParameters GetRunParameters()
        {
            return new ImageSaveParameters
            {
                OutputPath = this.FilePath,
                OutputFormat = this.ImageFormat.ToLower()
            };
        }

        /// <summary>
        /// 加载参数 - 使用默认值
        /// </summary>
        protected override void LoadParameters(ToolMetadata? toolMetadata)
        {
            // 使用 ImageSaveParameters 的默认值
            var defaultParams = new ImageSaveParameters();
            FilePath = defaultParams.OutputPath;
            ImageFormat = defaultParams.OutputFormat.ToUpper();
            ImageQuality = 90;
            OverwriteExisting = true;
            StatusMessage = "参数加载完成";
        }

        /// <summary>
        /// 保存参数
        /// </summary>
        public System.Collections.Generic.Dictionary<string, object> SaveParameters()
        {
            return new System.Collections.Generic.Dictionary<string, object>
            {
                { "FilePath", FilePath },
                { "ImageFormat", ImageFormat },
                { "ImageQuality", ImageQuality },
                { "OverwriteExisting", OverwriteExisting }
            };
        }

        #endregion

        #region 重写虚方法

        /// <summary>
        /// 重置参数
        /// </summary>
        public override void ResetParameters()
        {
            FilePath = string.Empty;
            ImageFormat = "PNG";
            ImageQuality = 95;
            OverwriteExisting = false;
            StatusMessage = "参数已重置为默认值";
        }

        #endregion
    }
}
