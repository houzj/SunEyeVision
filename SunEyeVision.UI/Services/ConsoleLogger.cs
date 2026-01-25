using SunEyeVision.Interfaces;

namespace SunEyeVision.UI.Services
{
    /// <summary>
    /// 控制台日志记录器实现
    /// </summary>
    public class ConsoleLogger : ILogger
    {
        public void LogDebug(string message)
        {
            // 注意：这是独立日志系统，不使用 ViewModel
            System.Diagnostics.Debug.WriteLine($"[DEBUG] {message}");
        }

        public void LogInfo(string message)
        {
            // 注意：这是独立日志系统，不使用 ViewModel
            System.Diagnostics.Debug.WriteLine($"[INFO] {message}");
        }

        public void LogWarning(string message)
        {
            // 注意：这是独立日志系统，不使用 ViewModel
            System.Diagnostics.Debug.WriteLine($"[WARNING] {message}");
        }

        public void LogError(string message, System.Exception exception = null)
        {
            // 注意：这是独立日志系统，不使用 ViewModel
            System.Diagnostics.Debug.WriteLine($"[ERROR] {message}");
            if (exception != null)
            {
                System.Diagnostics.Debug.WriteLine($"Exception: {exception}");
            }
        }

        public void LogFatal(string message, System.Exception exception = null)
        {
            // 注意：这是独立日志系统，不使用 ViewModel
            System.Diagnostics.Debug.WriteLine($"[FATAL] {message}");
            if (exception != null)
            {
                System.Diagnostics.Debug.WriteLine($"Exception: {exception}");
            }
        }
    }
}
