using System;

namespace SunEyeVision.Services
{
    /// <summary>
    /// 控制台日志记录器实现
    /// </summary>
    public class ConsoleLogger : SunEyeVision.Interfaces.ILogger
    {
        private readonly object _lockObject = new object();

        private void WriteLog(string level, string message, ConsoleColor color)
        {
            lock (_lockObject)
            {
                var originalColor = Console.ForegroundColor;
                try
                {
                    Console.ForegroundColor = color;
                    var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    Console.WriteLine($"[{timestamp}] [{level}] {message}");
                }
                finally
                {
                    Console.ForegroundColor = originalColor;
                }
            }
        }

        public void LogDebug(string message)
        {
            WriteLog("DEBUG", message, ConsoleColor.Gray);
        }

        public void LogInfo(string message)
        {
            WriteLog("INFO", message, ConsoleColor.White);
        }

        public void LogWarning(string message)
        {
            WriteLog("WARNING", message, ConsoleColor.Yellow);
        }

        public void LogError(string message, Exception? exception = null)
        {
            var fullMessage = exception != null
                ? $"{message}{Environment.NewLine}Exception: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}"
                : message;
            WriteLog("ERROR", fullMessage, ConsoleColor.Red);
        }
    }
}
