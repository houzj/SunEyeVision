using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using OpenCvSharp;

namespace SunEyeVision.UI.Services.Performance
{
    /// <summary>
    /// 图像转换器池 - 优化OpenCV Mat到WPF BitmapSource的转换
    /// 使用WriteableBitmap直接内存写入，避免中间转换
    /// </summary>
    public class ImageConverterPool : IDisposable
    {
        private readonly ConcurrentDictionary<string, WriteableBitmap> _bitmapCache;
        private readonly int _maxCacheSize;
        private bool _disposed;

        /// <summary>
        /// 创建图像转换器池
        /// </summary>
        /// <param name="maxCacheSize">最大缓存大小（默认10）</param>
        public ImageConverterPool(int maxCacheSize = 10)
        {
            _bitmapCache = new ConcurrentDictionary<string, WriteableBitmap>();
            _maxCacheSize = maxCacheSize;
        }

        /// <summary>
        /// 将OpenCV Mat转换为WPF BitmapSource（优化版）
        /// </summary>
        /// <param name="mat">OpenCV Mat图像</param>
        /// <returns>WPF BitmapSource</returns>
        public BitmapSource ConvertToBitmapSource(Mat mat)
        {
            if (mat == null || mat.Empty())
                throw new ArgumentNullException(nameof(mat));

            // 确定像素格式
            PixelFormat pixelFormat = GetPixelFormat(mat.Type());

            // 生成缓存键
            string cacheKey = $"{mat.Width}_{mat.Height}_{pixelFormat}";

            // 尝试从缓存获取WriteableBitmap
            if (!_bitmapCache.TryGetValue(cacheKey, out var writeableBitmap) ||
                writeableBitmap.PixelWidth != mat.Width ||
                writeableBitmap.PixelHeight != mat.Height)
            {
                // 创建新的WriteableBitmap
                writeableBitmap = new WriteableBitmap(mat.Width, mat.Height, 96, 96, pixelFormat, null);

                // 缓存（如果未达上限）
                if (_bitmapCache.Count < _maxCacheSize)
                {
                    _bitmapCache.TryAdd(cacheKey, writeableBitmap);
                }
            }

            // 锁定并写入数据
            writeableBitmap.Lock();

            try
            {
                // 根据通道数进行转换
                if (mat.Channels() == 1)
                {
                    // 灰度图：直接复制
                    CopyGrayToBitmap(mat, writeableBitmap);
                }
                else if (mat.Channels() == 3)
                {
                    // BGR图：转换为BGR32（或BGRA32）
                    CopyBgrToBitmap(mat, writeableBitmap);
                }
                else if (mat.Channels() == 4)
                {
                    // BGRA图：直接复制
                    CopyBgraToBitmap(mat, writeableBitmap);
                }
                else
                {
                    throw new NotSupportedException($"不支持的通道数: {mat.Channels()}");
                }

                // 标记脏区域
                writeableBitmap.AddDirtyRect(new Int32Rect(0, 0, mat.Width, mat.Height));
            }
            finally
            {
                writeableBitmap.Unlock();
            }

            // 冻结以提高性能（允许跨线程访问）
            // writeableBitmap.Freeze(); // 注意：WriteableBitmap不能冻结

            return writeableBitmap;
        }

        /// <summary>
        /// 获取像素格式
        /// </summary>
        private PixelFormat GetPixelFormat(MatType matType)
        {
            // 简化处理：统一使用Bgr32格式
            return PixelFormats.Bgr32;
        }

        /// <summary>
        /// 复制灰度图到WriteableBitmap
        /// </summary>
        private void CopyGrayToBitmap(Mat mat, WriteableBitmap bitmap)
        {
            int width = mat.Width;
            int height = mat.Height;
            int stride = bitmap.BackBufferStride;
            IntPtr srcPtr = mat.Data;
            IntPtr dstPtr = bitmap.BackBuffer;

            // 使用安全代码方式
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    byte gray = Marshal.ReadByte(srcPtr, y * width + x);
                    
                    // BGR32格式：B, G, R, A
                    int dstOffset = y * stride + x * 4;
                    Marshal.WriteByte(dstPtr, dstOffset, gray);     // B
                    Marshal.WriteByte(dstPtr, dstOffset + 1, gray); // G
                    Marshal.WriteByte(dstPtr, dstOffset + 2, gray); // R
                    Marshal.WriteByte(dstPtr, dstOffset + 3, 255);  // A
                }
            }
        }

        /// <summary>
        /// 复制BGR图到WriteableBitmap
        /// </summary>
        private void CopyBgrToBitmap(Mat mat, WriteableBitmap bitmap)
        {
            int width = mat.Width;
            int height = mat.Height;
            int stride = bitmap.BackBufferStride;
            IntPtr srcPtr = mat.Data;
            IntPtr dstPtr = bitmap.BackBuffer;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int srcOffset = (y * width + x) * 3;
                    int dstOffset = y * stride + x * 4;
                    
                    // BGR -> BGR32
                    Marshal.WriteByte(dstPtr, dstOffset, Marshal.ReadByte(srcPtr, srcOffset));       // B
                    Marshal.WriteByte(dstPtr, dstOffset + 1, Marshal.ReadByte(srcPtr, srcOffset + 1)); // G
                    Marshal.WriteByte(dstPtr, dstOffset + 2, Marshal.ReadByte(srcPtr, srcOffset + 2)); // R
                    Marshal.WriteByte(dstPtr, dstOffset + 3, 255); // A
                }
            }
        }

        /// <summary>
        /// 复制BGRA图到WriteableBitmap
        /// </summary>
        private void CopyBgraToBitmap(Mat mat, WriteableBitmap bitmap)
        {
            int width = mat.Width;
            int height = mat.Height;
            int stride = bitmap.BackBufferStride;
            IntPtr srcPtr = mat.Data;
            IntPtr dstPtr = bitmap.BackBuffer;

            for (int y = 0; y < height; y++)
            {
                // 复制整行（4字节对齐）
                int srcOffset = y * width * 4;
                int dstOffset = y * stride;
                
                // 使用Marshal.Copy复制整行
                byte[] rowData = new byte[width * 4];
                Marshal.Copy(srcPtr + srcOffset, rowData, 0, width * 4);
                Marshal.Copy(rowData, 0, dstPtr + dstOffset, width * 4);
            }
        }

        /// <summary>
        /// 清空缓存
        /// </summary>
        public void Clear()
        {
            _bitmapCache.Clear();
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            Clear();
        }
    }
}
