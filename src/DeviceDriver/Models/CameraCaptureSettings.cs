namespace SunEyeVision.DeviceDriver.Models
{
    /// <summary>
    /// 相机采集参数
    /// </summary>
    public class CameraCaptureSettings
    {
        /// <summary>
        /// 曝光时间（微秒）
        /// </summary>
        public double ExposureTime { get; set; } = 10000.0;

        /// <summary>
        /// 增益
        /// </summary>
        public double Gain { get; set; } = 0.0;

        /// <summary>
        /// 帧率
        /// </summary>
        public double FrameRate { get; set; } = 30.0;

        /// <summary>
        /// 图像宽度
        /// </summary>
        public int Width { get; set; } = 0;

        /// <summary>
        /// 图像高度
        /// </summary>
        public int Height { get; set; } = 0;

        /// <summary>
        /// 偏移X
        /// </summary>
        public int OffsetX { get; set; } = 0;

        /// <summary>
        /// 偏移Y
        /// </summary>
        public int OffsetY { get; set; } = 0;

        /// <summary>
        /// 像素格式
        /// </summary>
        public string PixelFormat { get; set; } = "RGB8";

        /// <summary>
        /// 是否使用硬件触发
        /// </summary>
        public bool HardwareTrigger { get; set; } = false;

        /// <summary>
        /// 触发源
        /// </summary>
        public string TriggerSource { get; set; } = "Software";

        /// <summary>
        /// 触发激活方式
        /// </summary>
        public string TriggerActivation { get; set; } = "RisingEdge";

        /// <summary>
        /// 是否启用自动曝光
        /// </summary>
        public bool AutoExposure { get; set; } = false;

        /// <summary>
        /// 是否启用自动增益
        /// </summary>
        public bool AutoGain { get; set; } = false;

        /// <summary>
        /// 自动曝光目标值
        /// </summary>
        public double AutoExposureTarget { get; set; } = 50.0;

        /// <summary>
        /// 自动增益目标值
        /// </summary>
        public double AutoGainTarget { get; set; } = 50.0;

        /// <summary>
        /// 是否启用伽马校正
        /// </summary>
        public bool GammaEnabled { get; set; } = false;

        /// <summary>
        /// 伽马值
        /// </summary>
        public double Gamma { get; set; } = 1.0;
    }
}
