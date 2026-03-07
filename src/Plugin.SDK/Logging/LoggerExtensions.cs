using System;

namespace SunEyeVision.Plugin.SDK.Logging
{
    /// <summary>
    /// 日志器扩展方法 - 提供语义化的日志记录API
    /// </summary>
    /// <remarks>
    /// 使用扩展方法可以更清晰地表达日志来源和上下文。
    /// 示例：
    /// <code>
    /// logger.NodeInfo("图像采集1", "开始采集图像");
    /// logger.NodeSuccess("图像采集1", "采集完成");
    /// logger.EngineInfo("工作流执行开始");
    /// logger.DeviceError("相机1", "连接失败", ex);
    /// </code>
    /// </remarks>
    public static class LoggerExtensions
    {
        #region 节点日志扩展

        /// <summary>
        /// 记录节点信息日志
        /// </summary>
        /// <param name="logger">日志器</param>
        /// <param name="nodeName">节点名称</param>
        /// <param name="message">日志消息</param>
        public static void NodeInfo(this ILogger logger, string nodeName, string message)
            => logger.Info(message, nodeName);

        /// <summary>
        /// 记录节点成功日志
        /// </summary>
        public static void NodeSuccess(this ILogger logger, string nodeName, string message)
            => logger.Success(message, nodeName);

        /// <summary>
        /// 记录节点警告日志
        /// </summary>
        public static void NodeWarning(this ILogger logger, string nodeName, string message)
            => logger.Warning(message, nodeName);

        /// <summary>
        /// 记录节点错误日志
        /// </summary>
        public static void NodeError(this ILogger logger, string nodeName, string message, Exception? ex = null)
            => logger.Error(message, nodeName, ex);

        #endregion

        #region 工作流引擎日志扩展

        /// <summary>
        /// 记录工作流引擎信息日志
        /// </summary>
        public static void EngineInfo(this ILogger logger, string message)
            => logger.Info(message, LogSources.WorkflowEngine);

        /// <summary>
        /// 记录工作流引擎成功日志
        /// </summary>
        public static void EngineSuccess(this ILogger logger, string message)
            => logger.Success(message, LogSources.WorkflowEngine);

        /// <summary>
        /// 记录工作流引擎警告日志
        /// </summary>
        public static void EngineWarning(this ILogger logger, string message)
            => logger.Warning(message, LogSources.WorkflowEngine);

        /// <summary>
        /// 记录工作流引擎错误日志
        /// </summary>
        public static void EngineError(this ILogger logger, string message, Exception? ex = null)
            => logger.Error(message, LogSources.WorkflowEngine, ex);

        #endregion

        #region 设备日志扩展

        /// <summary>
        /// 记录设备信息日志
        /// </summary>
        public static void DeviceInfo(this ILogger logger, string deviceName, string message)
            => logger.Info(message, deviceName);

        /// <summary>
        /// 记录设备成功日志
        /// </summary>
        public static void DeviceSuccess(this ILogger logger, string deviceName, string message)
            => logger.Success(message, deviceName);

        /// <summary>
        /// 记录设备警告日志
        /// </summary>
        public static void DeviceWarning(this ILogger logger, string deviceName, string message)
            => logger.Warning(message, deviceName);

        /// <summary>
        /// 记录设备错误日志
        /// </summary>
        public static void DeviceError(this ILogger logger, string deviceName, string message, Exception? ex = null)
            => logger.Error(message, deviceName, ex);

        #endregion

        #region 系统日志扩展

        /// <summary>
        /// 记录系统信息日志
        /// </summary>
        public static void SystemInfo(this ILogger logger, string message)
            => logger.Info(message, LogSources.System);

        /// <summary>
        /// 记录系统成功日志
        /// </summary>
        public static void SystemSuccess(this ILogger logger, string message)
            => logger.Success(message, LogSources.System);

        /// <summary>
        /// 记录系统警告日志
        /// </summary>
        public static void SystemWarning(this ILogger logger, string message)
            => logger.Warning(message, LogSources.System);

        /// <summary>
        /// 记录系统错误日志
        /// </summary>
        public static void SystemError(this ILogger logger, string message, Exception? ex = null)
            => logger.Error(message, LogSources.System, ex);

        #endregion

        #region UI组件日志扩展

        /// <summary>
        /// 记录UI组件信息日志
        /// </summary>
        public static void UIInfo(this ILogger logger, string componentName, string message)
            => logger.Info(message, componentName);

        /// <summary>
        /// 记录UI组件成功日志
        /// </summary>
        public static void UISuccess(this ILogger logger, string componentName, string message)
            => logger.Success(message, componentName);

        /// <summary>
        /// 记录UI组件警告日志
        /// </summary>
        public static void UIWarning(this ILogger logger, string componentName, string message)
            => logger.Warning(message, componentName);

        /// <summary>
        /// 记录UI组件错误日志
        /// </summary>
        public static void UIError(this ILogger logger, string componentName, string message, Exception? ex = null)
            => logger.Error(message, componentName, ex);

        #endregion

        #region 插件日志扩展

        /// <summary>
        /// 记录插件信息日志
        /// </summary>
        public static void PluginInfo(this ILogger logger, string pluginName, string message)
            => logger.Info(message, pluginName);

        /// <summary>
        /// 记录插件成功日志
        /// </summary>
        public static void PluginSuccess(this ILogger logger, string pluginName, string message)
            => logger.Success(message, pluginName);

        /// <summary>
        /// 记录插件警告日志
        /// </summary>
        public static void PluginWarning(this ILogger logger, string pluginName, string message)
            => logger.Warning(message, pluginName);

        /// <summary>
        /// 记录插件错误日志
        /// </summary>
        public static void PluginError(this ILogger logger, string pluginName, string message, Exception? ex = null)
            => logger.Error(message, pluginName, ex);

        #endregion
    }
}
