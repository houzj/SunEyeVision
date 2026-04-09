namespace SunEyeVision.DeviceDriver.Models
{
    /// <summary>
    /// 相机触发模式
    /// </summary>
    public enum CameraTriggerMode
    {
        /// <summary>
        /// 连续采集模式
        /// </summary>
        Continuous,

        /// <summary>
        /// 软件触发模式
        /// </summary>
        Software,

        /// <summary>
        /// 硬件触发模式
        /// </summary>
        Hardware
    }

    /// <summary>
    /// 相机设备信息
    /// </summary>
    public class CameraDeviceInfo
    {
        /// <summary>
        /// 设备ID（唯一标识）
        /// </summary>
        public string DeviceId { get; set; } = string.Empty;

        /// <summary>
        /// 设备名称
        /// </summary>
        public string DeviceName { get; set; } = string.Empty;

        /// <summary>
        /// 制造商
        /// </summary>
        public string Manufacturer { get; set; } = string.Empty;

        /// <summary>
        /// 型号
        /// </summary>
        public string Model { get; set; } = string.Empty;

        /// <summary>
        /// 序列号
        /// </summary>
        public string SerialNumber { get; set; } = string.Empty;

        /// <summary>
        /// IP地址
        /// </summary>
        public string IpAddress { get; set; } = string.Empty;

        /// <summary>
        /// 端口号
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// MAC地址
        /// </summary>
        public string MacAddress { get; set; } = string.Empty;

        /// <summary>
        /// 连接类型
        /// </summary>
        public CameraConnectionType ConnectionType { get; set; }

        /// <summary>
        /// 固件版本
        /// </summary>
        public string FirmwareVersion { get; set; } = string.Empty;

        /// <summary>
        /// 驱动版本
        /// </summary>
        public string DriverVersion { get; set; } = string.Empty;

        /// <summary>
        /// 厂家自定义字段
        /// </summary>
        public System.Collections.Generic.Dictionary<string, object> CustomProperties { get; set; } = new();
    }

    /// <summary>
    /// 相机连接类型
    /// </summary>
    public enum CameraConnectionType
    {
        /// <summary>
        /// 网络连接
        /// </summary>
        Network,

        /// <summary>
        /// USB连接
        /// </summary>
        Usb,

        /// <summary>
        /// GigE连接
        /// </summary>
        GigE,

        /// <summary>
        /// CameraLink连接
        /// </summary>
        CameraLink
    }
}
