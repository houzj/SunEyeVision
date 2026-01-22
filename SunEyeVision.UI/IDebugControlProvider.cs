using System.Windows.Controls;

namespace SunEyeVision.UI
{
    /// <summary>
    /// 自定义调试控件提供者接口
    /// 工具可以实现此接口来提供完全自定义的调试界面
    /// </summary>
    public interface IDebugControlProvider
    {
        /// <summary>
        /// 创建自定义调试控件
        /// </summary>
        /// <param name="toolId">工具ID</param>
        /// <returns>自定义控件，如果没有则返回null</returns>
        Control? CreateDebugControl(string toolId);

        /// <summary>
        /// 检查是否有自定义调试控件
        /// </summary>
        /// <param name="toolId">工具ID</param>
        /// <returns>是否存在自定义控件</returns>
        bool HasCustomDebugControl(string toolId);
    }
}
