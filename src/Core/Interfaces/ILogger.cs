using System;

namespace SunEyeVision.Core.Interfaces
{
    /// <summary>
    /// 日志接口
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// 记录调试信息
        /// </summary>
        void LogDebug(string message);

        /// <summary>
        /// 记录信息
        /// </summary>
        void LogInfo(string message);

        /// <summary>
        /// 记录警告
        /// </summary>
        void LogWarning(string message);

        /// <summary>
        /// 记录错误
        /// </summary>
        void LogError(string message, Exception? exception = null);
    }
}
