namespace SunEyeVision.Core.Interfaces.Plugins
{
    /// <summary>
    /// 所有插件的基础接口，必须实�?
    /// </summary>
    public interface IPlugin
    {
        /// <summary>
        /// 插件唯一标识�?
        /// </summary>
        string PluginId { get; }

        /// <summary>
        /// 插件显示名称
        /// </summary>
        string PluginName { get; }

        /// <summary>
        /// 插件版本
        /// </summary>
        string Version { get; }

        /// <summary>
        /// 插件描述
        /// </summary>
        string Description { get; }

        /// <summary>
        /// 插件作�?
        /// </summary>
        string Author { get; }

        /// <summary>
        /// 初始化插�?
        /// </summary>
        void Initialize();

        /// <summary>
        /// 启动插件
        /// </summary>
        void Start();

        /// <summary>
        /// 停止插件
        /// </summary>
        void Stop();

        /// <summary>
        /// 清理插件资源
        /// </summary>
        void Cleanup();
    }
}
