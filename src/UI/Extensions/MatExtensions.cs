using System;
using System.IO;
using System.Windows.Media.Imaging;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace SunEyeVision.UI.Extensions
{
    /// <summary>
    /// Mat 图像扩展方法
    /// </summary>
    public static class MatExtensions
    {
        /// <summary>
        /// 将 OpenCvSharp.Mat 转换为 WPF BitmapSource
        /// </summary>
        /// <param name="mat">OpenCV Mat 对象</param>
        /// <returns>WPF BitmapSource 对象</returns>
        public static BitmapSource? ToBitmapSource(this Mat mat)
        {
            if (mat == null || mat.Empty())
                return null;

            try
            {
                // 使用 OpenCvSharp.Extensions.BitmapConverter 的 ToBitmap 方法
                using var bitmap = mat.ToBitmap();
                if (bitmap == null)
                    return null;

                // 将 Bitmap 转换为 BitmapSource
                var hBitmap = bitmap.GetHbitmap();
                try
                {
                    var bitmapSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                        hBitmap,
                        IntPtr.Zero,
                        System.Windows.Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions());
                    bitmapSource.Freeze();
                    return bitmapSource;
                }
                finally
                {
                    // 释放 HBitmap 资源
                    DeleteObject(hBitmap);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MatExtensions] ToBitmapSource 转换失败: {ex.Message}");
                
                // 备用方案：通过内存流转换
                try
                {
                    return ConvertViaMemoryStream(mat);
                }
                catch
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// 通过内存流方式转换 Mat 到 BitmapSource
        /// </summary>
        private static BitmapSource? ConvertViaMemoryStream(Mat mat)
        {
            // 确保图像格式正确
            Mat? convertedMat = null;
            Mat sourceMat = mat;

            // 如果不是标准格式，转换为 BGR
            if (mat.Type() != MatType.CV_8UC3 && mat.Type() != MatType.CV_8UC1 && mat.Type() != MatType.CV_8UC4)
            {
                convertedMat = new Mat();
                Cv2.CvtColor(mat, convertedMat, ColorConversionCodes.GRAY2BGR);
                sourceMat = convertedMat;
            }

            // 编码为 PNG 格式
            Cv2.ImEncode(".png", sourceMat, out byte[] bytes);

            convertedMat?.Dispose();

            // 从字节数组创建 BitmapSource
            using var stream = new MemoryStream(bytes);
            var decoder = new PngBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            var bitmapSource = decoder.Frames[0];
            bitmapSource.Freeze();
            return bitmapSource;
        }

        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);
    }
}
