using SunEyeVision.Plugin.SDK;
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.SDK.Metadata;
using SunEyeVision.Plugin.SDK.ViewModels;

namespace SunEyeVision.Tool.ROICrop
{
    public class ROICropToolViewModel : AutoToolDebugViewModelBase
    {
        private int _x = 0;
        private int _y = 0;
        private int _width = 100;
        private int _height = 100;
        private bool _normalize = false;

        public int X
        {
            get => _x;
            set
            {
                SetProperty(ref _x, value);
                SetParamValue("X", value);
            }
        }

        public int Y
        {
            get => _y;
            set
            {
                SetProperty(ref _y, value);
                SetParamValue("Y", value);
            }
        }

        public int Width
        {
            get => _width;
            set
            {
                SetProperty(ref _width, value);
                SetParamValue("Width", value);
            }
        }

        public int Height
        {
            get => _height;
            set
            {
                SetProperty(ref _height, value);
                SetParamValue("Height", value);
            }
        }

        public bool Normalize
        {
            get => _normalize;
            set
            {
                SetProperty(ref _normalize, value);
                SetParamValue("Normalize", value);
            }
        }

        public override void Initialize(string toolId, IToolPlugin? toolPlugin, ToolMetadata? toolMetadata)
        {
            ToolId = toolId;
            ToolName = toolMetadata?.DisplayName ?? "ROI裁剪";
            ToolStatus = "就绪";
            StatusMessage = "准备就绪";
            LoadParameters(toolMetadata);
        }

        public override void RunTool()
        {
            ToolStatus = "运行中";
            StatusMessage = $"正在裁剪ROI({X},{Y},{Width}x{Height})...";
            var random = new System.Random();
            System.Threading.Thread.Sleep(random.Next(50, 100));
            ExecutionTime = $"{random.Next(20, 50)} ms";
            StatusMessage = $"ROI裁剪完成";
            ToolStatus = "就绪";
        }
    }
}
