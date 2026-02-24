using SunEyeVision.Core.Interfaces.Plugins;

namespace SunEyeVision.UI.Core.Services
{
    /// <summary>
    /// 插件UI适配�?
    /// 智能选择UI展示方式：自动、混合或自定�?
    /// </summary>
    public class PluginUIAdapter
    {
        private readonly IPlugin _plugin;
        private readonly IPluginUIProvider? _uiProvider;

        public PluginUIAdapter(IPlugin plugin)
        {
            _plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));
            _uiProvider = plugin as IPluginUIProvider;
        }

        /// <summary>
        /// 获取主界面控�?
        /// </summary>
        public object? GetMainControl()
        {
            // Custom模式：返回插件自定义控件
            if (_uiProvider != null && _uiProvider.Mode == UIProviderMode.Custom)
            {
                return _uiProvider.GetCustomControl();
            }

            // Auto和Hybrid模式：返回框架通用控件
            return CreateGenericControl();
        }

        /// <summary>
        /// 获取附加面板
        /// </summary>
        public object? GetAdditionalPanel()
        {
            // Hybrid和Custom模式：可以返回自定义面板
            if (_uiProvider != null &&
                (_uiProvider.Mode == UIProviderMode.Hybrid || _uiProvider.Mode == UIProviderMode.Custom))
            {
                return _uiProvider.GetCustomPanel();
            }

            // Auto模式：无自定义面�?
            return null;
        }

        /// <summary>
        /// 获取UI模式
        /// </summary>
        public UIProviderMode GetMode()
        {
            return _uiProvider?.Mode ?? UIProviderMode.Auto;
        }

        /// <summary>
        /// 是否需要属性面�?
        /// </summary>
        public bool NeedsPropertyPanel()
        {
            return GetMode() != UIProviderMode.Custom;
        }

        /// <summary>
        /// 创建通用控件（框架自动生成）
        /// </summary>
        private object CreateGenericControl()
        {
            // 这里返回框架提供的通用UI组件
            // 实际实现会根据插件类型（节点、算法等）生成不同的通用界面
            return new { PluginId = _plugin.PluginId, PluginName = _plugin.PluginName };
        }
    }
}

