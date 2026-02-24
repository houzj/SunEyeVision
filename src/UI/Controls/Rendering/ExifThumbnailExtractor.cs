using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Media.Imaging;

namespace SunEyeVision.UI.Controls.Rendering
{
    /// <summary>
    /// EXIF嵌入式缩略图提取�?
    /// 从相机拍摄的照片中快速提取嵌入的缩略�?
    /// 
    /// 性能特点�?
    /// - 有EXIF缩略图时�?-20ms（比完整解码�?0-100倍）
    /// - 无EXIF缩略图时�?-5ms（快速检测并返回�?
    /// 
    /// 适用场景�?
    /// 1. 数码相机拍摄的照片（通常包含160x120或更大的缩略图）
    /// 2. 智能手机拍摄的照�?
    /// 3. 经过后期的照片（可能保留原始缩略图）
    /// 
    /// 注意事项�?
    /// - EXIF缩略图通常分辨率较低（160x120左右�?
    /// - 对于大尺寸预览需要回退到完整解�?
    /// </summary>
    public static class ExifThumbnailExtractor
    {
        /// <summary>
        /// 最小可接受的缩略图尺寸比例
        /// 如果EXIF缩略图小于目标尺寸的一半，则认为质量不�?
        /// </summary>
        private const double MinSizeRatio = 0.5;

        /// <summary>
        /// 尝试提取嵌入式缩略图
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="targetSize">目标尺寸</param>
        /// <returns>BitmapSource或null（无缩略图或质量不足�?/returns>
        public static BitmapSource? TryExtractThumbnail(string filePath, int targetSize)
        {
            if (!File.Exists(filePath))
                return null;

            // 仅处理常见的照片格式
            var ext = Path.GetExtension(filePath).ToLowerInvariant();
            if (ext != ".jpg" && ext != ".jpeg" && ext != ".tiff" && ext != ".tif")
                return null;

            var sw = Stopwatch.StartNew();

            try
            {
                // 使用FileStream而非File.Open，指定最优参�?
                using var stream = new FileStream(
                    filePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    bufferSize: 4096,
                    FileOptions.SequentialScan);

                // 使用BitmapDecoder的Thumbnail属�?
                // 这会尝试读取EXIF中的缩略图，而不是解码整个图�?
                var decoder = BitmapDecoder.Create(
                    stream,
                    BitmapCreateOptions.IgnoreColorProfile | BitmapCreateOptions.DelayCreation,
                    BitmapCacheOption.None);

                // 检查是否有嵌入式缩略图
                if (decoder.Thumbnail == null)
                {
                    sw.Stop();
                    // Debug.WriteLine($"[EXIF] 无缩略图: {Path.GetFileName(filePath)}");
                    return null;
                }

                var thumbnail = decoder.Thumbnail;

                // 检查缩略图质量
                if (thumbnail.PixelWidth < targetSize * MinSizeRatio)
                {
                    sw.Stop();
                    Debug.WriteLine($"[EXIF] 缩略图太�?{thumbnail.PixelWidth}x{thumbnail.PixelHeight} < {targetSize}: {Path.GetFileName(filePath)}");
                    return null;
                }

                // 创建可冻结的BitmapSource
                BitmapSource result;

                if (thumbnail.PixelWidth <= targetSize && thumbnail.PixelHeight <= targetSize)
                {
                    // 缩略图尺寸符合要求，直接使用
                    result = BitmapFrame.Create(thumbnail);
                }
                else
                {
                    // 需要缩放到目标尺寸
                    result = ResizeThumbnail(thumbnail, targetSize);
                }

                // 冻结以便跨线程使�?
                if (result.CanFreeze)
                    result.Freeze();

                sw.Stop();
                Debug.WriteLine($"[EXIF] 成功提取 - {sw.ElapsedMilliseconds}ms - {thumbnail.PixelWidth}x{thumbnail.PixelHeight} - {Path.GetFileName(filePath)}");

                return result;
            }
            catch (Exception ex)
            {
                sw.Stop();
                // 不是所有图片都有EXIF缩略图，静默失败
                Debug.WriteLine($"[EXIF] 提取失败: {ex.Message} - {Path.GetFileName(filePath)}");
                return null;
            }
        }

        /// <summary>
        /// 检查文件是否可能包含EXIF缩略图（快速检测，不完全解码）
        /// </summary>
        public static bool MightHaveThumbnail(string filePath)
        {
            var ext = Path.GetExtension(filePath).ToLowerInvariant();
            if (ext != ".jpg" && ext != ".jpeg" && ext != ".tiff" && ext != ".tif")
                return false;

            try
            {
                using var stream = new FileStream(
                    filePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    bufferSize: 4096,
                    FileOptions.SequentialScan);

                // 只读取前64KB来判断是否有EXIF
                var buffer = new byte[Math.Min(stream.Length, 65536)];
                stream.Read(buffer, 0, buffer.Length);

                // 检查JPEG SOI标记和EXIF标记
                // JPEG文件以FF D8开�?
                if (buffer.Length < 4 || buffer[0] != 0xFF || buffer[1] != 0xD8)
                    return false;

                // 简单检查是否有EXIF标记 (FF E1)
                for (int i = 2; i < buffer.Length - 4; i++)
                {
                    if (buffer[i] == 0xFF && buffer[i + 1] == 0xE1)
                    {
                        // 找到APP1标记，很可能有EXIF
                        return true;
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 调整缩略图尺�?
        /// </summary>
        private static BitmapSource ResizeThumbnail(BitmapSource source, int targetSize)
        {
            // 计算目标尺寸（保持宽高比�?
            double ratio = Math.Min(
                (double)targetSize / source.PixelWidth,
                (double)targetSize / source.PixelHeight);

            int newWidth = (int)(source.PixelWidth * ratio);
            int newHeight = (int)(source.PixelHeight * ratio);

            // 使用高质量的缩放
            var transformedBitmap = new TransformedBitmap(
                source,
                new System.Windows.Media.ScaleTransform(
                    (double)newWidth / source.PixelWidth,
                    (double)newHeight / source.PixelHeight));

            return transformedBitmap;
        }

        /// <summary>
        /// 快速提取并转换为BitmapImage（用于显示）
        /// </summary>
        public static BitmapImage? ExtractAsBitmapImage(string filePath, int targetSize)
        {
            var thumbnail = TryExtractThumbnail(filePath, targetSize);
            if (thumbnail == null)
                return null;

            // 转换为BitmapImage以便UI显示
            var result = new BitmapImage();
            using var memory = new MemoryStream();

            // 编码为PNG以保持质�?
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(thumbnail));
            encoder.Save(memory);
            memory.Position = 0;

            result.BeginInit();
            result.CacheOption = BitmapCacheOption.OnLoad;
            result.DecodePixelWidth = targetSize;
            result.StreamSource = memory;
            result.EndInit();
            result.Freeze();

            return result;
        }
    }
}
