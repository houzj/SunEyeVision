namespace SunEyeVision.Interfaces
{
    /// <summary>
    /// 图像处理器接口
    /// </summary>
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
