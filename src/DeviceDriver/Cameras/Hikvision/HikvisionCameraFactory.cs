using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using SunEyeVision.DeviceDriver.Models;
using SunEyeVision.Plugin.SDK.Logging;

namespace SunEyeVision.DeviceDriver.Cameras.Hikvision
{
    /// <summary>
    /// 海康相机工厂
    /// </summary>
    public class HikvisionCameraFactory : ICameraFactory
    {
        /// <summary>
        /// 厂商标识
        /// </summary>
        public string Manufacturer => "Hikvision";

        /// <summary>
        /// 日志记录器
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        public HikvisionCameraFactory(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 检测相机设备
        /// </summary>
        public async Task<List<CameraDeviceInfo>> DiscoverDevicesAsync()
        {
            await Task.CompletedTask;

            var devices = new List<CameraDeviceInfo>();

            try
            {
                _logger.LogInfo("Discovering Hikvision cameras...");

                // 初始化SDK
                int initResult = MvCamera.MV_CC_Initialize();
                if (initResult != MvCamera.MV_OK && initResult != 0x80000000)
                {
                    _logger.LogError($"Failed to initialize MVS SDK, error code: 0x{initResult:X}");
                    return devices;
                }

                // 枚举设备
                MvCamera.MV_CC_DEVICE_INFO_LIST deviceList = new MvCamera.MV_CC_DEVICE_INFO_LIST();
                int enumResult = MvCamera.MV_CC_EnumDevices_NET(ref deviceList, MvCamera.MV_GIGE_DEVICE | MvCamera.MV_USB_DEVICE);

                if (enumResult != MvCamera.MV_OK)
                {
                    _logger.LogError($"Failed to enumerate devices, error code: 0x{enumResult:X}");
                    return devices;
                }

                _logger.LogInfo($"Found {deviceList.nDeviceNum} Hikvision camera(s)");

                // 解析设备信息
                for (int i = 0; i < deviceList.nDeviceNum; i++)
                {
                    try
                    {
                        // 计算每个设备信息结构体的大小
                        int structSize = System.Runtime.InteropServices.Marshal.SizeOf<MvCamera.MV_CC_DEVICE_INFO>();
                        IntPtr deviceInfoPtr = new IntPtr(deviceList.pDeviceInfo.ToInt64() + i * structSize);
                        MvCamera.MV_CC_DEVICE_INFO deviceInfo = Marshal.PtrToStructure<MvCamera.MV_CC_DEVICE_INFO>(deviceInfoPtr);

                        CameraDeviceInfo cameraDevice = ParseDeviceInfo(deviceInfo);
                        devices.Add(cameraDevice);

                        _logger.LogSuccess($"Discovered camera: {cameraDevice.DeviceName} ({cameraDevice.IpAddress})");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error parsing device info for device {i}", ex);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error discovering Hikvision cameras", ex);
            }

            return devices;
        }

        /// <summary>
        /// 创建相机服务
        /// </summary>
        public ICameraService CreateCameraService(CameraDeviceInfo deviceInfo)
        {
            if (deviceInfo == null)
            {
                throw new ArgumentNullException(nameof(deviceInfo));
            }

            if (!IsSupported(deviceInfo))
            {
                throw new ArgumentException("Device is not supported by Hikvision factory", nameof(deviceInfo));
            }

            _logger.LogInfo($"Creating Hikvision camera service for: {deviceInfo.DeviceName}");

            return new HikvisionCameraService(deviceInfo, _logger);
        }

        /// <summary>
        /// 验证设备是否支持
        /// </summary>
        public bool IsSupported(CameraDeviceInfo deviceInfo)
        {
            if (deviceInfo == null)
            {
                return false;
            }

            // 检查制造商
            if (!string.IsNullOrEmpty(deviceInfo.Manufacturer))
            {
                return deviceInfo.Manufacturer.Equals("Hikvision", StringComparison.OrdinalIgnoreCase);
            }

            // 如果没有制造商信息，通过其他方式判断（例如IP地址范围、型号等）
            // 这里简化处理，假设如果是GigE或USB设备都可能是海康相机
            return deviceInfo.ConnectionType == CameraConnectionType.Network ||
                   deviceInfo.ConnectionType == CameraConnectionType.Usb ||
                   deviceInfo.ConnectionType == CameraConnectionType.GigE;
        }

        /// <summary>
        /// 解析设备信息
        /// </summary>
        private CameraDeviceInfo ParseDeviceInfo(MvCamera.MV_CC_DEVICE_INFO deviceInfo)
        {
            CameraDeviceInfo cameraDevice = new CameraDeviceInfo();

            try
            {
                // 根据传输层类型解析
                if (deviceInfo.nTLayerType == MvCamera.MV_GIGE_DEVICE)
                {
                    // GigE设备 - 直接从字节数组解析
                    byte[] specialInfo = deviceInfo.SpecialInfo;

                    if (specialInfo != null && specialInfo.Length >= 64)
                    {
                        // 解析IP地址（前4个字节）
                        cameraDevice.IpAddress = $"{specialInfo[0]}.{specialInfo[1]}.{specialInfo[2]}.{specialInfo[3]}";

                        // 解析MAC地址（接下来的6个字节）
                        cameraDevice.MacAddress = $"{specialInfo[6]:X2}:{specialInfo[7]:X2}:{specialInfo[8]:X2}:{specialInfo[9]:X2}:{specialInfo[10]:X2}:{specialInfo[11]:X2}";

                        // 解析设备信息
                        // 模型名称从偏移48开始，长度16字节
                        cameraDevice.DeviceName = System.Text.Encoding.Default.GetString(specialInfo, 48, 16).TrimEnd('\0');
                        cameraDevice.Model = cameraDevice.DeviceName;
                        cameraDevice.Manufacturer = "Hikvision";

                        // 序列号从偏移32开始，长度32字节
                        cameraDevice.SerialNumber = System.Text.Encoding.Default.GetString(specialInfo, 32, 32).TrimEnd('\0');

                        cameraDevice.ConnectionType = CameraConnectionType.GigE;
                        cameraDevice.Port = 554; // 默认RTSP端口
                    }
                }
                else if (deviceInfo.nTLayerType == MvCamera.MV_USB_DEVICE)
                {
                    // USB设备 - 从字节数组解析
                    byte[] specialInfo = deviceInfo.SpecialInfo;

                    if (specialInfo != null && specialInfo.Length >= 192)
                    {
                        int offset = 0;

                        // 跳过制造商名称
                        offset += 32;

                        // 解析型号名称（16字节）
                        cameraDevice.DeviceName = System.Text.Encoding.Default.GetString(specialInfo, offset, 16).TrimEnd('\0');
                        cameraDevice.Model = cameraDevice.DeviceName;
                        offset += 16;

                        // 跳过系列名称
                        offset += 32;

                        // 跳过设备版本
                        offset += 32;

                        // 解析制造商名称（32字节）
                        cameraDevice.Manufacturer = System.Text.Encoding.Default.GetString(specialInfo, offset, 32).TrimEnd('\0');
                        offset += 32;

                        // 解析序列号（32字节）
                        cameraDevice.SerialNumber = System.Text.Encoding.Default.GetString(specialInfo, offset, 32).TrimEnd('\0');

                        cameraDevice.ConnectionType = CameraConnectionType.Usb;
                    }
                }

                // 设置设备ID
                if (string.IsNullOrEmpty(cameraDevice.SerialNumber))
                {
                    cameraDevice.DeviceId = cameraDevice.IpAddress;
                }
                else
                {
                    cameraDevice.DeviceId = cameraDevice.SerialNumber;
                }

                // 如果设备名称为空，使用ID
                if (string.IsNullOrEmpty(cameraDevice.DeviceName))
                {
                    cameraDevice.DeviceName = $"Hikvision Camera {cameraDevice.DeviceId}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error parsing device info", ex);
            }

            return cameraDevice;
        }
    }
}
