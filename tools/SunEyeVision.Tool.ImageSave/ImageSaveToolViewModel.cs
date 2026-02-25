using SunEyeVision.Plugin.SDK;
using SunEyeVision.Plugin.SDK.ViewModels;

namespace SunEyeVision.Tool.ImageSave
{
    /// <summary>
    /// ImageSaveTool ViewModel - 图像保存工具的视图模型
    /// </summary>
    public class ImageSaveToolViewModel : ToolDebugViewModelBase
    {
        private string _filePath = "";
        private string _imageFormat = "PNG";
        private int _imageQuality = 95;
        private bool _overwriteExisting = false;

        #region 属性

        /// <summary>
        /// 文件路径
        /// </summary>
        public string FilePath
        {
            get => _filePath;
            set
            {
                if (SetProperty(ref _filePath, value))
                {
                    StatusMessage = $"文件路径已更新: {value}";
                }
            }
        }

        /// <summary>
        /// 图像格式
        /// </summary>
        public string ImageFormat
        {
            get => _imageFormat;
            set => SetProperty(ref _imageFormat, value);
        }

        /// <summary>
        /// 图像质量
        /// </summary>
        public int ImageQuality
        {
            get => _imageQuality;
            set
            {
                if (SetProperty(ref _imageQuality, value))
                {
                    StatusMessage = $"图像质量已更新: {value}";
                }
            }
        }

        /// <summary>
        /// 是否覆盖已有文件
        /// </summary>
        public bool OverwriteExisting
        {
            get => _overwriteExisting;
            set => SetProperty(ref _overwriteExisting, value);
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
            ToolId = toolId;
            ToolName = toolMetadata?.DisplayName ?? "图像保存";
            ToolStatus = "就绪";
            StatusMessage = "准备就绪";
            LoadParameters(toolMetadata);
        }

        /// <summary>
        /// 加载参数
        /// </summary>
        public override void LoadParameters(ToolMetadata? toolMetadata)
        {
            if (toolMetadata?.InputParameters == null)
                return;

            foreach (var param in toolMetadata.InputParameters)
            {
                switch (param.Name)
                {
                    case "FilePath":
                        FilePath = param.DefaultValue?.ToString() ?? string.Empty;
                        break;
                    case "ImageFormat":
                        ImageFormat = param.DefaultValue?.ToString() ?? "PNG";
                        break;
                    case "ImageQuality":
                        if (int.TryParse(param.DefaultValue?.ToString(), out int quality))
                        {
                            ImageQuality = quality;
                        }
                        break;
                    case "OverwriteExisting":
                        if (bool.TryParse(param.DefaultValue?.ToString(), out bool overwrite))
                        {
                            OverwriteExisting = overwrite;
                        }
                        break;
                }
            }
            StatusMessage = "参数加载完成";
        }

        /// <summary>
        /// 保存参数
        /// </summary>
        public override System.Collections.Generic.Dictionary<string, object> SaveParameters()
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

        /// <summary>
        /// 运行工具
        /// </summary>
        public override void RunTool()
        {
            if (string.IsNullOrWhiteSpace(FilePath))
            {
                StatusMessage = "请先设置文件路径";
                return;
            }

            ToolStatus = "运行中";
            StatusMessage = $"正在保存图像到: {FilePath}";
            var random = new System.Random();
            System.Threading.Thread.Sleep(random.Next(100, 500));
            ExecutionTime = $"{random.Next(50, 200)} ms";
            StatusMessage = $"图像保存成功: {System.IO.Path.GetFileName(FilePath)}";
            ToolStatus = "就绪";
        }

        #endregion
    }
}
