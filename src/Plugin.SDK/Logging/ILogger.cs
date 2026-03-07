using System;

namespace SunEyeVision.Plugin.SDK.Logging
{
    /// <summary>
    /// 日志器接口 - 提供简洁的日志记录API
    /// </summary>
    /// <remarks>
    /// 这是面向使用者的主要接口，提供便捷的日志记录方法。
    /// 实现类应保证高性能、非阻塞的日志记录。
    /// 
    /// 方法命名：
    /// - Info: 信息 - 状态变更、过程开始、中间状态
    /// - Success: 成功 - 任务完成、结果达标
    /// - Warning: 警告 - 有问题但能继续
    /// - Error: 错误 - 失败/异常
    /// </remarks>
    public interface ILogger
    {
        /// <summary>
        /// 记录日志（核心方法）
        /// </summary>
        /// <param name="level">日志级别</param>
        /// <param name="message">日志消息</param>
        /// <param name="source">来源标识（节点名称、模块名、组件名等）</param>
        /// <param name="exception">异常信息</param>
        void Log(LogLevel level, string message, string? source = null, Exception? exception = null);

        #region 日志方法

        /// <summary>
        /// 记录信息日志 - 状态变更、过程开始、中间状态
        /// </summary>
        void Info(string message, string? source = null)
            => Log(LogLevel.Info, message, source);

        /// <summary>
        /// 记录信息日志（别名）- 兼容旧代码
        /// </summary>
        void LogInfo(string message, string? source = null)
            => Log(LogLevel.Info, message, source);

        /// <summary>
        /// 记录成功日志 - 任务完成、结果达标
        /// </summary>
        void Success(string message, string? source = null)
            => Log(LogLevel.Success, message, source);

        /// <summary>
        /// 记录成功日志（别名）- 兼容旧代码
        /// </summary>
        void LogSuccess(string message, string? source = null)
            => Log(LogLevel.Success, message, source);

        /// <summary>
        /// 记录警告日志 - 有问题但程序能继续运行
        /// </summary>
        void Warning(string message, string? source = null)
            => Log(LogLevel.Warning, message, source);

        /// <summary>
        /// 记录警告日志（别名）- 兼容旧代码
        /// </summary>
        void LogWarning(string message, string? source = null)
            => Log(LogLevel.Warning, message, source);

        /// <summary>
        /// 记录错误日志 - 失败/异常，需要处理
        /// </summary>
        void Error(string message, string? source = null, Exception? exception = null)
            => Log(LogLevel.Error, message, source, exception);

        /// <summary>
        /// 记录错误日志（带异常）- 便捷方法
        /// </summary>
        void Error(string message, Exception? exception)
            => Log(LogLevel.Error, message, null, exception);

        /// <summary>
        /// 记录错误日志（别名）- 兼容旧代码
        /// </summary>
        void LogError(string message, string? source = null, Exception? exception = null)
            => Log(LogLevel.Error, message, source, exception);

        /// <summary>
        /// 记录错误日志（别名，带异常）- 便捷方法
        /// </summary>
        void LogError(string message, Exception? exception)
            => Log(LogLevel.Error, message, null, exception);

        #endregion

        /// <summary>
        /// 创建带上下文的日志器（用于特定来源）
        /// </summary>
        /// <param name="source">来源标识</param>
        /// <returns>带预设来源的日志器</returns>
        ILogger ForSource(string source);
    }
}
