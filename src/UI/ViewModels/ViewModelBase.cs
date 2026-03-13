using SunEyeVision.Plugin.SDK.Models;
using SunEyeVision.Plugin.SDK.Logging;

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
    ///
    /// 提供便捷的日志方法（LogInfo、LogSuccess、LogWarning、LogError）
    /// </remarks>
    public abstract class ViewModelBase : ObservableObject
    {
        /// <summary>
        /// 记录信息日志
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <param name="source">来源标识</param>
        protected void LogInfo(string message, string? source = null)
        {
            var logger = VisionLogger.Instance;
            logger.Info(message, source ?? GetLogSource());
        }

        /// <summary>
        /// 记录成功日志
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <param name="source">来源标识</param>
        protected void LogSuccess(string message, string? source = null)
        {
            var logger = VisionLogger.Instance;
            logger.Success(message, source ?? GetLogSource());
        }

        /// <summary>
        /// 记录警告日志
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <param name="source">来源标识</param>
        protected void LogWarning(string message, string? source = null)
        {
            var logger = VisionLogger.Instance;
            logger.Warning(message, source ?? GetLogSource());
        }

        /// <summary>
        /// 记录错误日志
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <param name="source">来源标识</param>
        /// <param name="exception">异常信息</param>
        protected void LogError(string message, string? source = null, Exception? exception = null)
        {
            var logger = VisionLogger.Instance;
            logger.Error(message, source ?? GetLogSource(), exception);
        }
    }
}
