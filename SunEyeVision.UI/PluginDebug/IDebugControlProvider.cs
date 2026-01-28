namespace SunEyeVision.UI.PluginDebug
{
    /// <summary>
    /// 调试控制提供者接口
    /// 插件可实现此接口提供自定义调试控制
    /// </summary>
    public interface IDebugControlProvider
    {
        /// <summary>
        /// 获取调试控制面板
        /// </summary>
        /// <returns>调试控制面板</returns>
        object GetDebugPanel();

        /// <summary>
        /// 开始调试
        /// </summary>
        void StartDebug();

        /// <summary>
        /// 停止调试
        /// </summary>
        void StopDebug();

        /// <summary>
        /// 单步执行
        /// </summary>
        void Step();

        /// <summary>
        /// 重置调试
        /// </summary>
        void Reset();
    }
}
