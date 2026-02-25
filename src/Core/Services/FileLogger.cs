using System;
using System.IO;

namespace SunEyeVision.Core.Services
{
    /// <summary>
    /// 文件日志记录器实例。
    /// </summary>
    public class FileLogger : SunEyeVision.Core.Interfaces.ILogger
    {
        private readonly string _logDirectory;
        private readonly object _lockObject = new object();
        private string _logFilePath;

        public FileLogger(string logDirectory = "logs")
        {
            _logDirectory = logDirectory;
            EnsureLogDirectory();
            InitializeLogFile();
        }

        private void EnsureLogDirectory()
        {
            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }
        }

        private void InitializeLogFile()
        {
            var dateStr = DateTime.Now.ToString("yyyyMMdd");
            _logFilePath = Path.Combine(_logDirectory, $"SunEyeVision_{dateStr}.log");
        }

        private void WriteLog(string level, string message)
        {
            lock (_lockObject)
            {
                try
                {
                    var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    var logMessage = $"[{timestamp}] [{level}] {message}{Environment.NewLine}";
                    File.AppendAllText(_logFilePath, logMessage);
                }
                catch
                {
                    // 防止日志写入失败导致程序崩溃
                }
            }
        }

        public void LogDebug(string message)
        {
            WriteLog("DEBUG", message);
        }

        public void LogInfo(string message)
        {
            WriteLog("INFO", message);
        }

        public void LogWarning(string message)
        {
            WriteLog("WARNING", message);
        }

        public void LogError(string message, Exception? exception = null)
        {
            var fullMessage = exception != null
                ? $"{message}{Environment.NewLine}Exception: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}"
                : message;
            WriteLog("ERROR", fullMessage);
        }
    }
}
