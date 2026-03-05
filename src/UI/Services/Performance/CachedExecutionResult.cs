using System;
using System.Windows.Media.Imaging;

namespace SunEyeVision.UI.Services.Performance
{
    /// <summary>
    /// 缓存的执行结果
    /// 用于图像快速切换场景，避免重复执行
    /// </summary>
    public class CachedExecutionResult
    {
        /// <summary>
        /// 节点ID
        /// </summary>
        public string NodeId { get; set; } = string.Empty;

        /// <summary>
        /// 图像源ID（用于标识不同的图像源）
        /// </summary>
        public string ImageSourceId { get; set; } = string.Empty;

        /// <summary>
        /// 处理后的图像（WPF BitmapSource，可直接用于UI绑定）
        /// </summary>
        public BitmapSource? ProcessedImage { get; set; }

        /// <summary>
        /// 原始图像（可选，用于对比显示）
        /// </summary>
        public BitmapSource? OriginalImage { get; set; }

        /// <summary>
        /// 执行时间（毫秒）
        /// </summary>
        public long ExecutionTimeMs { get; set; }

        /// <summary>
        /// 缓存创建时间
        /// </summary>
        public DateTime CreatedTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 最后访问时间（用于LRU淘汰）
        /// </summary>
        public DateTime LastAccessTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 缓存大小估算（字节）
        /// </summary>
        public long EstimatedSize { get; set; }

        /// <summary>
        /// 缓存键（用于字典查找）
        /// </summary>
        public string CacheKey => $"{NodeId}_{ImageSourceId}";

        /// <summary>
        /// 更新访问时间
        /// </summary>
        public void Touch()
        {
            LastAccessTime = DateTime.Now;
        }

        /// <summary>
        /// 计算图像大小估算
        /// </summary>
        public void CalculateSize()
        {
            EstimatedSize = 0;

            if (ProcessedImage != null)
            {
                EstimatedSize += ProcessedImage.PixelWidth * ProcessedImage.PixelHeight *
                                (ProcessedImage.Format.BitsPerPixel / 8);
            }

            if (OriginalImage != null)
            {
                EstimatedSize += OriginalImage.PixelWidth * OriginalImage.PixelHeight *
                                (OriginalImage.Format.BitsPerPixel / 8);
            }

            // 预留一些空间用于其他数据
            EstimatedSize += 1024 * 10; // 10KB
        }

        /// <summary>
        /// 是否过期
        /// </summary>
        /// <param name="expirationSeconds">过期时间（秒）</param>
        /// <returns>是否过期</returns>
        public bool IsExpired(int expirationSeconds = 300)
        {
            return (DateTime.Now - LastAccessTime).TotalSeconds > expirationSeconds;
        }
    }
}
