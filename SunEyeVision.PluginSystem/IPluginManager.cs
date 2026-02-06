using System;
using System.Collections.Generic;
using System.Linq;

namespace SunEyeVision.PluginSystem
{
    /// <summary>
    /// 插件管理器接口
    /// </summary>
    public interface IPluginManager
    {
        /// <summary>
        /// 加载所有插件
        /// </summary>
        void LoadPlugins();

        /// <summary>
        /// 卸载所有插件
        /// </summary>
        void UnloadPlugins();

        /// <summary>
        /// 获取所有插件
        /// </summary>
        /// <typeparam name="T">插件类型</typeparam>
        /// <returns>插件列表</returns>
        List<T> GetPlugins<T>() where T : class;

        /// <summary>
        /// 注册插件
        /// </summary>
        /// <param name="plugin">插件实例</param>
        void RegisterPlugin(object plugin);

        /// <summary>
        /// 注销插件
        /// </summary>
        /// <param name="plugin">插件实例</param>
        void UnregisterPlugin(object plugin);

        /// <summary>
        /// 检查插件是否已加载
        /// </summary>
        /// <typeparam name="T">插件类型</typeparam>
        /// <returns>是否已加载</returns>
        bool IsPluginLoaded<T>() where T : class;
    }

    /// <summary>
    /// 插件管理器实现
    /// </summary>
    public class PluginManager : IPluginManager
    {
        /// <summary>
        /// 已加载的插件列表
        /// </summary>
        private readonly List<object> _loadedPlugins;

        /// <summary>
        /// 插件加载器
        /// </summary>
        private readonly PluginLoader _pluginLoader;

        public PluginManager()
        {
            _loadedPlugins = new List<object>();
            _pluginLoader = new PluginLoader();
        }

        /// <summary>
        /// 加载所有插件
        /// </summary>
        public void LoadPlugins()
        {
            var plugins = _pluginLoader.LoadAllPlugins();
            _loadedPlugins.AddRange(plugins);
        }

        /// <summary>
        /// 卸载所有插件
        /// </summary>
        public void UnloadPlugins()
        {
            _loadedPlugins.Clear();
        }

        /// <summary>
        /// 获取所有插件
        /// </summary>
        public List<T> GetPlugins<T>() where T : class
        {
            return _loadedPlugins.OfType<T>().ToList();
        }

        /// <summary>
        /// 注册插件
        /// </summary>
        public void RegisterPlugin(object plugin)
        {
            if (!_loadedPlugins.Contains(plugin))
            {
                _loadedPlugins.Add(plugin);
            }
        }

        /// <summary>
        /// 注销插件
        /// </summary>
        public void UnregisterPlugin(object plugin)
        {
            _loadedPlugins.Remove(plugin);
        }

        /// <summary>
        /// 检查插件是否已加载
        /// </summary>
        public bool IsPluginLoaded<T>() where T : class
        {
            return _loadedPlugins.OfType<T>().Any();
        }
    }
}
