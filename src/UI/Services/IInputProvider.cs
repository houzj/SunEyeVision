using System.Threading.Tasks;

namespace SunEyeVision.UI.Services
{
    /// <summary>
    /// 图像输入提供者接口
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
