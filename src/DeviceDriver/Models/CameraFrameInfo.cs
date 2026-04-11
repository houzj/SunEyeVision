using System;

namespace SunEyeVision.DeviceDriver.Models
{
    /// <summary>
    /// 相机帧信息
    /// </summary>
    public class CameraFrameInfo : IDisposable
    {
        /// <summary>
        /// 设备ID
        /// </summary>
        public string DeviceId { get; set; } = string.Empty;

        /// <summary>
        /// 帧编号
        /// </summary>
        public long FrameNumber { get; set; }

        /// <summary>
        /// 时间戳
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// 图像宽度
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// 图像高度
        /// </summary>
        public int Height { get; set; }

        /// <summary>
        /// 像素格式
        /// </summary>
        public string PixelFormat { get; set; } = string.Empty;

        /// <summary>
        /// 图像数据（字节数组）
        /// </summary>
        public byte[] ImageData { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// 数据大小（字节）
        /// </summary>
        public int DataSize { get; set; }

        /// <summary>
        /// 曝光时间（微秒）
        /// </summary>
        public double ExposureTime { get; set; }

        /// <summary>
        /// 增益
        /// </summary>
        public double Gain { get; set; }

        /// <summary>
        /// 帧率
        /// </summary>
        public double FrameRate { get; set; }

        /// <summary>
        /// 自定义属性
        /// </summary>
        public System.Collections.Generic.Dictionary<string, object> Properties { get; set; } = new();

        private bool _disposed = false;

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                ImageData = Array.Empty<byte>();
                _disposed = true;
            }
        }
    }
}
