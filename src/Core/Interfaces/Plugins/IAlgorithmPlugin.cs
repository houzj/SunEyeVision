namespace SunEyeVision.Core.Interfaces.Plugins
{
    /// <summary>
    /// 算法插件接口
    /// 定义图像处理算法的标准行�?
    /// </summary>
    public interface IAlgorithmPlugin : IPlugin
    {
        /// <summary>
        /// 算法类型
        /// </summary>
        string AlgorithmType { get; }

        /// <summary>
        /// 算法图标
        /// </summary>
        string Icon { get; }

        /// <summary>
        /// 算法分类
        /// </summary>
        string Category { get; }

        /// <summary>
        /// 算法参数元数�?
        /// </summary>
        ParameterMetadata[] GetParameters();

        /// <summary>
        /// 执行算法
        /// </summary>
        /// <param name="inputImage">输入图像</param>
        /// <param name="parameters">参数字典</param>
        /// <returns>处理后的图像</returns>
        object Execute(object inputImage, System.Collections.Generic.Dictionary<string, object> parameters);

        /// <summary>
        /// 验证参数
        /// </summary>
        /// <param name="parameters">参数字典</param>
        /// <returns>验证结果</returns>
        bool ValidateParameters(System.Collections.Generic.Dictionary<string, object> parameters);
    }
}
