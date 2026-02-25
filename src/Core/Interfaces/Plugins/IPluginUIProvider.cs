namespace SunEyeVision.Core.Interfaces.Plugins
{
    /// <summary>
    /// UI提供模式
    /// </summary>
    public enum UIProviderMode
    {
        /// <summary>
        /// 自动模式：使用框架提供的通用UI，插件只需提供元数据
        /// </summary>
        Auto,

        /// <summary>
        /// 混合模式：使用框架共享UI组件，插件可自定义部分界面
        /// </summary>
        Hybrid,

        /// <summary>
        /// 自定义模式：插件完全自定义UI界面
        /// </summary>
        Custom
    }

    /// <summary>
    /// 插件UI提供者接口（可选）
    /// 如果插件不实现此接口，框架将自动使用通用UI
    /// </summary>
    public interface IPluginUIProvider
    {
        /// <summary>
        /// UI提供模式
        /// </summary>
        UIProviderMode Mode { get; }

        /// <summary>
        /// 获取插件的自定义控件（仅在Custom模式下使用）
        /// 返回null表示使用框架通用UI
        /// </summary>
        object GetCustomControl();

        /// <summary>
        /// 获取插件的自定义面板（在Hybrid模式下使用）
        /// 返回null表示不添加自定义面板
        /// </summary>
        object GetCustomPanel();
    }
}
