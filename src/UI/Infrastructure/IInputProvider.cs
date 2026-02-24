using System.Threading.Tasks;
using SunEyeVision.UI.Infrastructure;

namespace SunEyeVision.UI.Infrastructure
{
    /// <summary>
    /// 图像输入提供者接�?
    /// </summary>
    public interface IInputProvider
    {
        /// <summary>
        /// 异步获取输入图像
        /// </summary>
        /// <returns>图像数据,如果没有图像则返回null</returns>
        Task<object?> GetInputImageAsync();
    }
}
