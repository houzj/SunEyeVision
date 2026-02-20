using System;
using System.Windows.Media.Imaging;

namespace SunEyeVision.UI.Controls.Rendering
{
    /// <summary>
    /// 缩略图解码器接口 - 支持多种解码策略切换
    /// 
    /// 实现类：
    /// - ImageSharpDecoder: CPU软解码，跨平台稳定
    /// - WicGpuDecoder: GPU硬件加速，性能更高
    /// - AdvancedGpuDecoder: 多策略GPU解码
    /// </summary>
    public interface IThumbnailDecoder : IDisposable
    {
        /// <summary>
        /// 是否已初始化
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// 是否支持硬件加速
        /// </summary>
        bool IsHardwareAccelerated { get; }

        /// <summary>
        /// 初始化解码器
        /// </summary>
        /// <returns>初始化是否成功</returns>
        bool Initialize();

        /// <summary>
        /// 解码缩略图
        /// </summary>
        /// <param name="filePath">图像文件路径</param>
        /// <param name="size">目标尺寸（宽度）</param>
        /// <param name="prefetchedData">预读取的文件数据（可选）</param>
        /// <param name="verboseLog">是否输出详细日志</param>
        /// <param name="isHighPriority">是否高优先级任务</param>
        /// <returns>解码后的 BitmapImage，失败返回 null</returns>
        BitmapImage? DecodeThumbnail(string filePath, int size, byte[]? prefetchedData = null, bool verboseLog = false, bool isHighPriority = false);
    }
}
