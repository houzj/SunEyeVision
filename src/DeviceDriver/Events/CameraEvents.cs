using System;

namespace SunEyeVision.DeviceDriver.Events
{
    /// <summary>
    /// 相机帧接收事件
    /// </summary>
    public class CameraFrameReceivedEvent : EventArgs
    {
        /// <summary>
        /// 设备ID
        /// </summary>
        public string DeviceId { get; set; } = string.Empty;

        /// <summary>
        /// 帧信息
        /// </summary>
        public Models.CameraFrameInfo FrameInfo { get; set; } = null!;
    }

    /// <summary>
    /// 相机异常事件
    /// </summary>
    public class CameraExceptionEvent : EventArgs
    {
        /// <summary>
        /// 设备ID
        /// </summary>
        public string DeviceId { get; set; } = string.Empty;

        /// <summary>
        /// 异常信息
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// 异常类型
        /// </summary>
        public CameraExceptionType ExceptionType { get; set; }

        /// <summary>
        /// 异常对象
        /// </summary>
        public Exception? Exception { get; set; }

        /// <summary>
        /// 时间戳
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 相机异常类型
    /// </summary>
    public enum CameraExceptionType
    {
        /// <summary>
        /// 连接失败
        /// </summary>
        ConnectionFailed,

        /// <summary>
        /// 断开连接
        /// </summary>
        Disconnected,

        /// <summary>
        /// 采集失败
        /// </summary>
        CaptureFailed,

        /// <summary>
        /// 参数设置失败
        /// </summary>
        SettingFailed,

        /// <summary>
        /// 超时
        /// </summary>
        Timeout,

        /// <summary>
        /// 未知错误
        /// </summary>
        Unknown
    }

    /// <summary>
    /// 相机连接事件
    /// </summary>
    public class CameraConnectionEvent : EventArgs
    {
        /// <summary>
        /// 设备ID
        /// </summary>
        public string DeviceId { get; set; } = string.Empty;

        /// <summary>
        /// 连接状态
        /// </summary>
        public CameraConnectionState ConnectionState { get; set; }

        /// <summary>
        /// 时间戳
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;

        /// <summary>
        /// 附加信息
        /// </summary>
        public string? Message { get; set; }
    }

    /// <summary>
    /// 相机连接状态
    /// </summary>
    public enum CameraConnectionState
    {
        /// <summary>
        /// 已连接
        /// </summary>
        Connected,

        /// <summary>
        /// 已断开
        /// </summary>
        Disconnected,

        /// <summary>
        /// 连接中
        /// </summary>
        Connecting,

        /// <summary>
        /// 断开中
        /// </summary>
        Disconnecting,

        /// <summary>
        /// 错误
        /// </summary>
        Error
    }
}
