using SunEyeVision.Plugin.SDK.Models;

namespace SunEyeVision.UI.ViewModels
{
    /// <summary>
    /// UI层 ViewModel 基类 - 继承 SDK 的 ObservableObject
    /// </summary>
    /// <remarks>
    /// 所有属性变化通知功能继承自 Plugin.SDK.Models.ObservableObject。
    /// 使用 SetProperty 方法设置属性值，支持自动日志记录：
    /// 
    /// <code>
    /// // 不记录日志
    /// public string Name
    /// {
    ///     get => _name;
    ///     set => SetProperty(ref _name, value);
    /// }
    /// 
    /// // 自动记录日志
    /// public int Threshold
    /// {
    ///     get => _threshold;
    ///     set => SetProperty(ref _threshold, value, "阈值");
    /// }
    /// </code>
    /// </remarks>
    public abstract class ViewModelBase : ObservableObject
    {
    }
}
