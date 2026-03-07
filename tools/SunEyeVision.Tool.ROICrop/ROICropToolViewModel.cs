using System.Collections.ObjectModel;
using SunEyeVision.Plugin.SDK;
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.SDK.Metadata;
using SunEyeVision.Plugin.SDK.UI.Controls;
using SunEyeVision.Plugin.SDK.ViewModels;

namespace SunEyeVision.Tool.ROICrop
{
    public class ROICropToolViewModel : ToolViewModelBase
    {
        private int _x = 0;
        private int _y = 0;
        private int _width = 100;
        private int _height = 100;
        private bool _normalize = false;

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

        public int X
        {
            get => _x;
            set
            {
                if (SetProperty(ref _x, value, "X坐标"))
                    SetParamValue("X", value);
            }
        }

        public int Y
        {
            get => _y;
            set
            {
                if (SetProperty(ref _y, value, "Y坐标"))
                    SetParamValue("Y", value);
            }
        }

        public int Width
        {
            get => _width;
            set
            {
                if (SetProperty(ref _width, value, "宽度"))
                    SetParamValue("Width", value);
            }
        }

        public int Height
        {
            get => _height;
            set
            {
                if (SetProperty(ref _height, value, "高度"))
                    SetParamValue("Height", value);
            }
        }

        public bool Normalize
        {
            get => _normalize;
            set
            {
                if (SetProperty(ref _normalize, value, "归一化"))
                    SetParamValue("Normalize", value);
            }
        }

        public override void Initialize(string toolId, IToolPlugin? toolPlugin, ToolMetadata? toolMetadata)
        {
            // 调用基类初始化（初始化 ToolRunner）
            base.Initialize(toolId, toolPlugin, toolMetadata);
            ToolName = toolMetadata?.DisplayName ?? "ROI裁剪";
        }

        /// <summary>
        /// 获取当前运行参数
        /// </summary>
        protected override ToolParameters GetRunParameters()
        {
            return new ROICropParameters
            {
                X = this.X,
                Y = this.Y,
                Width = this.Width,
                Height = this.Height
            };
        }
    }
}
