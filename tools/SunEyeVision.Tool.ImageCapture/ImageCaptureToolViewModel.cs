using SunEyeVision.Plugin.SDK;
using SunEyeVision.Plugin.SDK.ViewModels;
namespace SunEyeVision.Tool.ImageCapture
{
    public class ImageCaptureToolViewModel : AutoToolDebugViewModelBase
    {
        private string _deviceId = "0";
        private int _width = 1920;
        private int _height = 1080;
        private double _exposureTime = 10.0;
        private double _gain = 1.0;

        public string DeviceId
        {
            get => _deviceId;
            set
            {
                SetProperty(ref _deviceId, value);
                SetParamValue("DeviceId", value);
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

        public double ExposureTime
        {
            get => _exposureTime;
            set
            {
                SetProperty(ref _exposureTime, value);
                SetParamValue("ExposureTime", value);
            }
        }

        public double Gain
        {
            get => _gain;
            set
            {
                SetProperty(ref _gain, value);
                SetParamValue("Gain", value);
            }
        }

        public string[] CommonResolutions { get; } = { "640x480", "1280x720", "1920x1080", "2560x1440", "3840x2160" };

        public override void Initialize(string toolId, IToolPlugin? toolPlugin, ToolMetadata? toolMetadata)
        {
            ToolId = toolId;
            ToolName = toolMetadata?.DisplayName ?? "图像采集";
            ToolStatus = "就绪";
            StatusMessage = "准备就绪";
            LoadParameters(toolMetadata);
        }

        public override void RunTool()
        {
            ToolStatus = "运行�?;
            StatusMessage = $"正在从设�?{_deviceId} 采集图像 ({Width}x{Height})...";
            
            var random = new System.Random();
            System.Threading.Thread.Sleep(random.Next(200, 500));
            
            ExecutionTime = $"{random.Next(150, 300)} ms";
            StatusMessage = $"图像采集完成 - {Width}x{Height}";
            ToolStatus = "就绪";
        }
    }
}
