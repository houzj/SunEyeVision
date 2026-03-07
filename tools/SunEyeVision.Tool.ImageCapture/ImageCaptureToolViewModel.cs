using SunEyeVision.Plugin.SDK;
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.SDK.Metadata;
using SunEyeVision.Plugin.SDK.ViewModels;

namespace SunEyeVision.Tool.ImageCapture
{
    public class ImageCaptureToolViewModel : ToolViewModelBase
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
                if (SetProperty(ref _deviceId, value, "设备ID"))
                    SetParamValue("DeviceId", value);
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

        public double ExposureTime
        {
            get => _exposureTime;
            set
            {
                if (SetProperty(ref _exposureTime, value, "曝光时间"))
                    SetParamValue("ExposureTime", value);
            }
        }

        public double Gain
        {
            get => _gain;
            set
            {
                if (SetProperty(ref _gain, value, "增益"))
                    SetParamValue("Gain", value);
            }
        }

        public string[] CommonResolutions { get; } = { "640x480", "1280x720", "1920x1080", "2560x1440", "3840x2160" };

        public override void Initialize(string toolId, IToolPlugin? toolPlugin, ToolMetadata? toolMetadata)
        {
            // 调用基类初始化（初始化 ToolRunner）
            base.Initialize(toolId, toolPlugin, toolMetadata);
            ToolName = toolMetadata?.DisplayName ?? "图像采集";
        }

        /// <summary>
        /// 获取当前运行参数
        /// </summary>
        protected override ToolParameters GetRunParameters()
        {
            return new ImageCaptureParameters
            {
                CameraId = int.TryParse(DeviceId, out var id) ? id : 0,
                Timeout = 5000
            };
        }
    }
}
