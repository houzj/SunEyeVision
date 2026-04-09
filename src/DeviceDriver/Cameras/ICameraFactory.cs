using System.Threading.Tasks;
using SunEyeVision.DeviceDriver.Models;

namespace SunEyeVision.DeviceDriver.Cameras
{
    /// <summary>
    /// 相机工厂接口
    /// </summary>
    public interface ICameraFactory
    {
        /// <summary>
        /// 厂商标识
        /// </summary>
        string Manufacturer { get; }

        /// <summary>
        /// 检测相机设备
        /// </summary>
        /// <returns>检测到的设备列表</returns>
        Task<System.Collections.Generic.List<CameraDeviceInfo>> DiscoverDevicesAsync();

        /// <summary>
        /// 创建相机服务
        /// </summary>
        /// <param name="deviceInfo">设备信息</param>
        /// <returns>相机服务实例</returns>
        ICameraService CreateCameraService(CameraDeviceInfo deviceInfo);

        /// <summary>
        /// 验证设备是否支持
        /// </summary>
        /// <param name="deviceInfo">设备信息</param>
        /// <returns>是否支持</returns>
        bool IsSupported(CameraDeviceInfo deviceInfo);
    }
}
