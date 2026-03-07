namespace SunEyeVision.Plugin.SDK.Logging
{
    /// <summary>
    /// 日志级别 - 专业视觉软件精简版
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// 信息 - 状态变更、过程开始、中间状态
        /// </summary>
        Info = 0,

        /// <summary>
        /// 成功 - 任务完成、结果达标、操作完成
        /// </summary>
        Success = 1,

        /// <summary>
        /// 警告 - 有问题但程序能继续运行
        /// </summary>
        Warning = 2,

        /// <summary>
        /// 错误 - 失败/异常，需要处理
        /// </summary>
        Error = 3
    }
}
