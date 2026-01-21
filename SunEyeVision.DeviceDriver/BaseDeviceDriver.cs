using System;
using SunEyeVision.Interfaces;
using SunEyeVision.Models;

namespace SunEyeVision.DeviceDriver
{
    /// <summary>
    /// 设备驱动基类
    /// </summary>
    public abstract class BaseDeviceDriver : IDisposable
    {
        /// <summary>
        /// 设备ID
        /// </summary>
        public string DeviceId { get; protected set; }

        /// <summary>
        /// 设备名称
        /// </summary>
        public string DeviceName { get; protected set; }

        /// <summary>
        /// 设备类型
        /// </summary>
        public string DeviceType { get; protected set; }

        /// <summary>
        /// 是否已连�?
        /// </summary>
        public bool IsConnected { get; protected set; }

        /// <summary>
        /// 日志记录�?
        /// </summary>
        protected ILogger Logger { get; private set; }

        /// <summary>
        /// 是否已释�?
        /// </summary>
        private bool _disposed;

        protected BaseDeviceDriver(string deviceId, string deviceName, string deviceType, ILogger logger)
        {
            DeviceId = deviceId;
            DeviceName = deviceName;
            DeviceType = deviceType;
            Logger = logger;
            IsConnected = false;
        }

        /// <summary>
        /// 连接设备
        /// </summary>
        public abstract bool Connect();

        /// <summary>
        /// 断开设备
        /// </summary>
        public abstract bool Disconnect();

        /// <summary>
        /// 获取图像
        /// </summary>
        public abstract Mat CaptureImage();

        /// <summary>
        /// 开始连续采�?
        /// </summary>
        public abstract bool StartContinuousCapture();

        /// <summary>
        /// 停止连续采集
        /// </summary>
        public abstract bool StopContinuousCapture();

        /// <summary>
        /// 获取设备信息
        /// </summary>
        public abstract DeviceInfo GetDeviceInfo();

        /// <summary>
        /// 设置设备参数
        /// </summary>
        public abstract bool SetParameter(string key, object value);

        /// <summary>
        /// 获取设备参数
        /// </summary>
        public abstract T GetParameter<T>(string key);

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // 释放托管资源
                    if (IsConnected)
                    {
                        Disconnect();
                    }
                }

                // 释放非托管资�?
                _disposed = true;
            }
        }

        ~BaseDeviceDriver()
        {
            Dispose(false);
        }
    }
}
