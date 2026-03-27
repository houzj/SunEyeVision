using SunEyeVision.UI.Services.Path;

namespace SunEyeVision.UI.Services.PathCalculators
{
    /// <summary>
    /// 路径计算器工厂 - 创建贝塞尔曲线路径计算器
    /// </summary>
    public static class PathCalculatorFactory
    {
        /// <summary>
        /// 创建路径计算器实例
        /// </summary>
        /// <returns>贝塞尔曲线路径计算器实例</returns>
        public static IPathCalculator CreateCalculator()
        {
            return new BezierPathCalculator();
        }
    }
}
