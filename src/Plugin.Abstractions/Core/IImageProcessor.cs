namespace SunEyeVision.Plugin.Abstractions.Core
{
    /// <summary>
    /// 图像处理器接口
    /// </summary>
    /// <remarks>
    /// 此接口是插件系统的核心契约，所有图像处理工具都必须实现此接口。
    /// 迁移自 SunEyeVision.Core.Interfaces 以实现依赖反转。
    /// </remarks>
    public interface IImageProcessor
    {
        /// <summary>
        /// 处理图像
        /// </summary>
        /// <param name="image">输入图像</param>
        /// <returns>处理后的图像</returns>
        object? Process(object image);
    }
}
