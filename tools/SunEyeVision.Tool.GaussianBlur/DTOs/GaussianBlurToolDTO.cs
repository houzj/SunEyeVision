using SunEyeVision.Plugin.SDK;

namespace SunEyeVision.Tool.GaussianBlur.DTOs
{
    /// <summary>
    /// 高斯模糊工具数据传输对象
    /// </summary>
    public class GaussianBlurToolDTO
    {
        /// <summary>
        /// 核大小（必须为奇数）
        /// </summary>
        public int KernelSize { get; set; } = 5;

        /// <summary>
        /// 标准差（Sigma�?
        /// </summary>
        public double Sigma { get; set; } = 1.5;

        /// <summary>
        /// 边界类型
        /// </summary>
        public string BorderType { get; set; } = "Reflect";
    }
}
