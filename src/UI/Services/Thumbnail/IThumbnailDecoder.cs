using System;
using System.Windows.Media.Imaging;
using SunEyeVision.Core.IO;
using SunEyeVision.UI.Services.Thumbnail;
using SunEyeVision.UI.Services.Thumbnail.Decoders;

namespace SunEyeVision.UI.Services.Thumbnail
{
    /// <summary>
    /// 缩略图解码器接口 - 支持多种解码策略切换
    /// 
    /// 实现类：
    /// - ImageSharpDecoder: CPU软解码，跨平台稳�?
    /// - WicGpuDecoder: GPU硬件加速，性能更高
    /// - AdvancedGpuDecoder: 多策略GPU解码
    /// 
    /// �?文件安全访问�?
    /// - 使用 DecodeThumbnailSafe 方法确保文件访问期间不会被删�?
    /// - 通过 IFileAccessManager 实现引用计数跟踪
    /// </summary>
    public interface IThumbnailDecoder : IDisposable
    {
        /// <summary>
        /// 是否已初始化
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// 是否支持硬件加�?
        /// </summary>
        bool IsHardwareAccelerated { get; }

        /// <summary>
        /// 初始化解码器
        /// </summary>
        /// <returns>初始化是否成�?/returns>
        bool Initialize();

        /// <summary>
        /// 解码缩略图（基础方法�?
        /// </summary>
        /// <param name="filePath">图像文件路径</param>
        /// <param name="size">目标尺寸（宽度）</param>
        /// <param name="prefetchedData">预读取的文件数据（可选）</param>
        /// <param name="verboseLog">是否输出详细日志</param>
        /// <param name="isHighPriority">是否高优先级任务</param>
        /// <returns>解码后的 BitmapImage，失败返�?null</returns>
        BitmapImage? DecodeThumbnail(string filePath, int size, byte[]? prefetchedData = null, bool verboseLog = false, bool isHighPriority = false);

        /// <summary>
        /// �?安全解码缩略图（推荐使用�?
        /// 通过 FileAccessManager 保护文件访问，防止清理器删除正在使用的文�?
        /// </summary>
        /// <param name="fileManager">文件访问管理�?/param>
        /// <param name="filePath">图像文件路径</param>
        /// <param name="size">目标尺寸（宽度）</param>
        /// <param name="prefetchedData">预读取的文件数据（可选）</param>
        /// <param name="verboseLog">是否输出详细日志</param>
        /// <param name="isHighPriority">是否高优先级任务</param>
        /// <returns>解码后的 BitmapImage，失败返�?null</returns>
        /// <remarks>
        /// 使用 RAII 模式确保文件引用正确释放�?
        /// <code>
        /// using var scope = fileManager.CreateAccessScope(filePath, FileAccessIntent.Read);
        /// if (scope.IsGranted) { /* 安全访问文件 */ }
        /// </code>
        /// </remarks>
        BitmapImage? DecodeThumbnailSafe(
            IFileAccessManager? fileManager,
            string filePath,
            int size,
            byte[]? prefetchedData = null,
            bool verboseLog = false,
            bool isHighPriority = false);
    }
}
