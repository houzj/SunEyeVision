using SunEyeVision.Plugin.SDK.Models;

namespace SunEyeVision.Core.Services.CameraDiscovery
{
    /// <summary>
    /// 发现的相机模型
    /// </summary>
    public class DiscoveredCamera : ObservableObject
    {
        /// <summary>
        /// 相机名称
        /// </summary>
        private string _name = string.Empty;
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value, "相机名称");
        }

        /// <summary>
        /// 制造商（从标准协议获取）
        /// </summary>
        private string _manufacturer = string.Empty;
        public string Manufacturer
        {
            get => _manufacturer;
            set => SetProperty(ref _manufacturer, value, "制造商");
        }

        /// <summary>
        /// 型号（从标准协议获取）
        /// </summary>
        private string _model = string.Empty;
        public string Model
        {
            get => _model;
            set => SetProperty(ref _model, value, "型号");
        }

        /// <summary>
        /// IP地址
        /// </summary>
        private string _ipAddress = string.Empty;
        public string IpAddress
        {
            get => _ipAddress;
            set => SetProperty(ref _ipAddress, value, "IP地址");
        }

        /// <summary>
        /// 序列号（从标准协议获取）
        /// </summary>
        private string _serialNumber = string.Empty;
        public string SerialNumber
        {
            get => _serialNumber;
            set => SetProperty(ref _serialNumber, value, "序列号");
        }

        /// <summary>
        /// 相机类型（GigE/USB/IP）
        /// </summary>
        private CameraType _cameraType = CameraType.GigE;
        public CameraType CameraType
        {
            get => _cameraType;
            set => SetProperty(ref _cameraType, value, "相机类型");
        }

        /// <summary>
        /// 端口
        /// </summary>
        private int _port;
        public int Port
        {
            get => _port;
            set => SetProperty(ref _port, value, "端口");
        }

        /// <summary>
        /// 是否选中
        /// </summary>
        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        /// <summary>
        /// 状态
        /// </summary>
        private CameraStatus _status = CameraStatus.Available;
        public CameraStatus Status
        {
            get => _status;
            set => SetProperty(ref _status, value, "状态");
        }

        /// <summary>
        /// MAC地址（仅 GigE）
        /// </summary>
        private string? _macAddress;
        public string? MacAddress
        {
            get => _macAddress;
            set => SetProperty(ref _macAddress, value);
        }

        /// <summary>
        /// 设备ID（仅 USB）
        /// </summary>
        private string? _deviceId;
        public string? DeviceId
        {
            get => _deviceId;
            set => SetProperty(ref _deviceId, value);
        }
    }

    /// <summary>
    /// 相机类型
    /// </summary>
    public enum CameraType
    {
        GigE,
        USB,
        IP
    }

    /// <summary>
    /// 相机状态
    /// </summary>
    public enum CameraStatus
    {
        Available,      // 可用
        NeedAuth,       // 需要认证
        Unavailable     // 不可用
    }
}
