using System;
using System.Runtime.InteropServices;

namespace SunEyeVision.Models
{
    /// <summary>
    /// 图像数据模型（OpenCvSharp封装）
    /// </summary>
    public class Mat : IDisposable
    {
        private IntPtr _nativePtr;
        private bool _disposed;

        /// <summary>
        /// 图像宽度
        /// </summary>
        public int Width { get; private set; }

        /// <summary>
        /// 图像高度
        /// </summary>
        public int Height { get; private set; }

        /// <summary>
        /// 图像通道数
        /// </summary>
        public int Channels { get; private set; }

        /// <summary>
        /// 图像数据
        /// </summary>
        public byte[] Data { get; private set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        public Mat(int width, int height, int channels)
        {
            Width = width;
            Height = height;
            Channels = channels;
            Data = new byte[width * height * channels];
        }

        /// <summary>
        /// 从字节数组创建
        /// </summary>
        public Mat(byte[] data, int width, int height, int channels)
        {
            Data = data;
            Width = width;
            Height = height;
            Channels = channels;
        }

        /// <summary>
        /// 克隆图像
        /// </summary>
        public Mat Clone()
        {
            byte[] newData = new byte[Data.Length];
            Array.Copy(Data, newData, Data.Length);
            return new Mat(newData, Width, Height, Channels);
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                Data = null;
                _disposed = true;
            }
        }

        /// <summary>
        /// 深度克隆
        /// </summary>
        public Mat DeepClone()
        {
            return Clone();
        }
    }
}
