namespace SunEyeVision.Plugin.Abstractions.Core
{
    /// <summary>
    /// 图像处理器接口
    /// </summary>
    /// <remarks>
    /// 此接口是插件系统的核心契约，所有图像处理工具都必须实现此接口。
    /// 迁移自 SunEyeVision.Core.Interfaces 以实现依赖反转。
    /// 
    /// 提供两种执行方式：
    /// 1. Process() - 简单处理，仅输入图像
    /// 2. Execute() - 完整处理，支持参数和返回结构化结果
    /// </remarks>
    public interface IImageProcessor
    {
        /// <summary>
        /// 处理器名称
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 处理器描述
        /// </summary>
        string Description { get; }

        /// <summary>
        /// 处理图像（简单模式）
        /// </summary>
        /// <param name="image">输入图像</param>
        /// <returns>处理后的图像</returns>
        object? Process(object image);

        /// <summary>
        /// 执行处理（完整模式）
        /// </summary>
        /// <param name="image">输入图像</param>
        /// <param name="parameters">处理参数</param>
        /// <returns>结构化的算法结果</returns>
        AlgorithmResult Execute(object image, AlgorithmParameters parameters);
    }
}
