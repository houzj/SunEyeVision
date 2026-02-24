using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using SunEyeVision.Core.Interfaces;
using SunEyeVision.Plugin.SDK;

namespace SunEyeVision.Plugin.Infrastructure.Managers.Plugin
{
    /// <summary>
    /// 插件加载�?- 从目录加载插件程序集
    /// </summary>
    public class PluginLoader
    {
        private readonly ILogger _logger;
        private readonly Dictionary<string, IToolPlugin> _loadedPlugins = new Dictionary<string, IToolPlugin>();

        public PluginLoader(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 从目录加载所有插�?        /// </summary>
        /// <param name="pluginDirectory">插件目录</param>
        public void LoadPluginsFromDirectory(string pluginDirectory)
        {
            if (!Directory.Exists(pluginDirectory))
            {
                _logger.LogWarning($"插件目录不存�? {pluginDirectory}");
                return;
            }

            var dllFiles = Directory.GetFiles(pluginDirectory, "*.dll", SearchOption.TopDirectoryOnly);

            foreach (var dllFile in dllFiles)
            {
                try
                {
                    LoadPlugin(dllFile);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"加载插件失败: {dllFile}", ex);
                }
            }

            _logger.LogInfo($"已加�?{_loadedPlugins.Count} 个插�?);
        }

        /// <summary>
        /// 加载单个插件
        /// </summary>
        /// <param name="pluginPath">插件DLL路径</param>
        public void LoadPlugin(string pluginPath)
        {
            _logger.LogInfo($"正在加载插件: {pluginPath}");

            var assembly = Assembly.LoadFrom(pluginPath);
            var pluginTypes = assembly.GetTypes()
                .Where(t => typeof(IToolPlugin).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

            foreach (var pluginType in pluginTypes)
            {
                try
                {
                    var plugin = (IToolPlugin)Activator.CreateInstance(pluginType);

                    if (_loadedPlugins.ContainsKey(plugin.PluginId))
                    {
                        _logger.LogWarning($"插件ID {plugin.PluginId} 已存在，跳过");
                        continue;
                    }

                    plugin.Initialize();
                    _loadedPlugins[plugin.PluginId] = plugin;

                    _logger.LogInfo($"插件加载成功: {plugin.Name} v{plugin.Version}");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"实例化插件失�? {pluginType.Name}", ex);
                }
            }
        }

        /// <summary>
        /// 卸载插件
        /// </summary>
        /// <param name="pluginId">插件ID</param>
        public bool UnloadPlugin(string pluginId)
        {
            if (!_loadedPlugins.TryGetValue(pluginId, out var plugin))
            {
                _logger.LogWarning($"插件未找�? {pluginId}");
                return false;
            }

            try
            {
                plugin.Unload();
                _loadedPlugins.Remove(pluginId);
                _logger.LogInfo($"插件卸载成功: {plugin.Name}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"卸载插件失败: {plugin.Name}", ex);
                return false;
            }
        }

        /// <summary>
        /// 获取已加载的插件
        /// </summary>
        /// <param name="pluginId">插件ID</param>
        public IToolPlugin? GetPlugin(string pluginId)
        {
            _loadedPlugins.TryGetValue(pluginId, out var plugin);
            return plugin;
        }

        /// <summary>
        /// 获取所有已加载的插�?        /// </summary>
        public List<IToolPlugin> GetAllPlugins()
        {
            return _loadedPlugins.Values.ToList();
        }

        /// <summary>
        /// 卸载所有插�?        /// </summary>
        public void UnloadAllPlugins()
        {
            var pluginIds = _loadedPlugins.Keys.ToList();
            foreach (var pluginId in pluginIds)
            {
                UnloadPlugin(pluginId);
            }
        }
    }
}
