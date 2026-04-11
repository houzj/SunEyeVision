using System;
using System.Threading.Tasks;
using SunEyeVision.DeviceDriver.Events;
using SunEyeVision.DeviceDriver.Models;

namespace SunEyeVision.DeviceDriver.Cameras
{
    /// <summary>
    /// 相机服务接口
    /// </summary>
    public interface ICameraService
    {
        /// <summary>
        /// 设备ID
        /// </summary>
        string DeviceId { get; }

        /// <summary>
        /// 设备信息
        /// </summary>
        CameraDeviceInfo DeviceInfo { get; }

        /// <summary>
        /// 是否已连接
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// 相机帧接收事件
        /// </summary>
        event EventHandler<CameraFrameReceivedEvent> FrameReceived;

        /// <summary>
        /// 相机异常事件
        /// </summary>
        event EventHandler<CameraExceptionEvent> ExceptionOccurred;

        /// <summary>
        /// 相机连接事件
        /// </summary>
        event EventHandler<CameraConnectionEvent> ConnectionStateChanged;

        /// <summary>
        /// 连接相机
        /// </summary>
        /// <returns>连接是否成功</returns>
        Task<bool> ConnectAsync();

        /// <summary>
        /// 断开相机
        /// </summary>
        /// <returns>断开是否成功</returns>
        Task<bool> DisconnectAsync();

        /// <summary>
        /// 开始采集
        /// </summary>
        /// <returns>是否成功</returns>
        Task<bool> StartCaptureAsync();

        /// <summary>
        /// 停止采集
        /// </summary>
        /// <returns>是否成功</returns>
        Task<bool> StopCaptureAsync();

        /// <summary>
        /// 触发一次采集（软触发）
        /// </summary>
        /// <returns>采集的帧</returns>
        Task<CameraFrameInfo?> TriggerCaptureAsync();

        /// <summary>
        /// 设置触发模式
        /// </summary>
        /// <param name="mode">触发模式</param>
        /// <returns>是否成功</returns>
        Task<bool> SetTriggerModeAsync(CameraTriggerMode mode);

        /// <summary>
        /// 设置采集参数
        /// </summary>
        /// <param name="settings">采集参数</param>
        /// <returns>是否成功</returns>
        Task<bool> SetCaptureSettingsAsync(CameraCaptureSettings settings);

        /// <summary>
        /// 获取当前采集参数
        /// </summary>
        /// <returns>采集参数</returns>
        Task<CameraCaptureSettings?> GetCaptureSettingsAsync();

        /// <summary>
        /// 获取相机属性
        /// </summary>
        /// <returns>相机属性字典</returns>
        Task<System.Collections.Generic.Dictionary<string, object>> GetPropertiesAsync();

        /// <summary>
        /// 设置相机属性
        /// </summary>
        /// <param name="properties">属性字典</param>
        /// <returns>是否成功</returns>
        Task<bool> SetPropertiesAsync(System.Collections.Generic.Dictionary<string, object> properties);

        /// <summary>
        /// 释放资源
        /// </summary>
        void Dispose();
    }
}
