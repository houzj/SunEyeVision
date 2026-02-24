using SunEyeVision.Core.Interfaces;
using SunEyeVision.UI.Diagnostics;

namespace SunEyeVision.UI.Diagnostics
{
    /// <summary>
    /// 控制台日志记录器实现
    /// </summary>
    public class ConsoleLogger : ILogger
    {
        public void LogDebug(string message)
        {
            // 注意：这是独立日志系统，不使�?ViewModel
        }

        public void LogInfo(string message)
        {
            // 注意：这是独立日志系统，不使�?ViewModel
        }

        public void LogWarning(string message)
        {
            // 注意：这是独立日志系统，不使�?ViewModel
        }

        public void LogError(string message, System.Exception exception = null)
        {
            // 注意：这是独立日志系统，不使�?ViewModel
        }

        public void LogFatal(string message, System.Exception exception = null)
        {
            // 注意：这是独立日志系统，不使�?ViewModel
        }
    }
}
