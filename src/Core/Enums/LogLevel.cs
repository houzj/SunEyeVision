namespace SunEyeVision.Core
{
    /// <summary>
    /// 日志级别
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// 调试信息 - 最详细的日志，用于开发调试
        /// </summary>
        Debug = 0,

        /// <summary>
        /// 信息 - 一般信息
        /// </summary>
        Info = 1,

        /// <summary>
        /// 警告 - 警告信息
        /// </summary>
        Warning = 2,

        /// <summary>
        /// 错误 - 错误信息
        /// </summary>
        Error = 3,

        /// <summary>
        /// 致命错误 - 致命错误信息
        /// </summary>
        Fatal = 4
    }
}
