using System.Collections.Generic;
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.SDK.Metadata;

namespace SunEyeVision.Plugin.SDK.ViewModels
{
    /// <summary>
    /// 工具调试 ViewModel 基类（别名）
    /// </summary>
    /// <remarks>
    /// 这是 AutoToolDebugViewModelBase 的别名，用于向后兼容。
    /// </remarks>
    public abstract class ToolDebugViewModelBase : AutoToolDebugViewModelBase
    {
        /// <summary>
        /// 获取参数字典
        /// </summary>
        public new Dictionary<string, object> GetParameters()
        {
            return base.GetParameters();
        }
    }
}
