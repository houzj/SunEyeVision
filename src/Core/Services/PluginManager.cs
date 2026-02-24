using SunEyeVision.Core.Interfaces.Plugins;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace SunEyeVision.Core.Services
{
    /// <summary>
    /// 插件管理�?
    /// 负责插件的加载、注册、生命周期管�?
    /// </summary>
    public class PluginManager
    {
        private readonly Dictionary<string, IPlugin> _plugins = new Dictionary<string, IPlugin>();
        private readonly Dictionary<string, PluginMetadata> _pluginMetadata = new Dictionary<string, PluginMetadata>();

        /// <summary>
        /// 从指定目录加载所有插�?
        /// </summary>
        /// <param name="pluginDirectory">插件目录</param>
        public void LoadPlugins(string pluginDirectory)
        {
            if (!Directory.Exists(pluginDirectory))
            {
                return;
            }

            var dllFiles = Directory.GetFiles(pluginDirectory, "*.dll", SearchOption.AllDirectories);

            foreach (var dllFile in dllFiles)
            {
                LoadPlugin(dllFile);
            }
        }

        /// <summary>
        /// 加载单个插件
        /// </summary>
        /// <param name="dllPath">插件DLL路径</param>
        public void LoadPlugin(string dllPath)
        {
            try
            {
                var assembly = Assembly.LoadFrom(dllPath);
                var pluginTypes = assembly.GetTypes()
                    .Where(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

                foreach (var pluginType in pluginTypes)
                {
                    var plugin = (IPlugin?)Activator.CreateInstance(pluginType);
                    if (plugin != null)
                    {
                        RegisterPlugin(plugin, Path.GetDirectoryName(dllPath));
                    }
                }
            }
            catch (Exception ex)
            {
                // 记录加载失败日志
            }
        }

        /// <summary>
        /// 注册插件
        /// </summary>
        /// <param name="plugin">插件实例</param>
        /// <param name="pluginPath">插件路径</param>
        private void RegisterPlugin(IPlugin plugin, string? pluginPath)
        {
            if (_plugins.ContainsKey(plugin.PluginId))
            {
                return;
            }

            _plugins[plugin.PluginId] = plugin;

            // 加载插件元数�?
            var metadata = LoadPluginMetadata(pluginPath);
            _pluginMetadata[plugin.PluginId] = metadata;

            plugin.Initialize();
        }

        /// <summary>
        /// 加载插件元数�?
        /// </summary>
        /// <param name="pluginPath">插件路径</param>
        /// <returns>插件元数�?/returns>
        private PluginMetadata LoadPluginMetadata(string? pluginPath)
        {
            if (pluginPath == null)
            {
                return new PluginMetadata();
            }

            var metadataFile = Path.Combine(pluginPath, "plugin.json");
            if (File.Exists(metadataFile))
            {
                try
                {
                    var json = File.ReadAllText(metadataFile);
                    return JsonSerializer.Deserialize<PluginMetadata>(json) ?? new PluginMetadata();
                }
                catch
                {
                    // 如果加载失败，返回默认元数据
                }
            }

            return new PluginMetadata();
        }

        /// <summary>
        /// 获取插件
        /// </summary>
        /// <param name="pluginId">插件ID</param>
        /// <returns>插件实例</returns>
        public IPlugin? GetPlugin(string pluginId)
        {
            _plugins.TryGetValue(pluginId, out var plugin);
            return plugin;
        }

        /// <summary>
        /// 获取所有插�?
        /// </summary>
        /// <returns>插件列表</returns>
        public IEnumerable<IPlugin> GetAllPlugins()
        {
            return _plugins.Values;
        }

        /// <summary>
        /// 获取插件元数�?
        /// </summary>
        /// <param name="pluginId">插件ID</param>
        /// <returns>插件元数�?/returns>
        public PluginMetadata? GetPluginMetadata(string pluginId)
        {
            _pluginMetadata.TryGetValue(pluginId, out var metadata);
            return metadata;
        }

        /// <summary>
        /// 启动所有插�?
        /// </summary>
        public void StartAllPlugins()
        {
            foreach (var plugin in _plugins.Values)
            {
                try
                {
                    plugin.Start();
                }
                catch (Exception ex)
                {
                }
            }
        }

        /// <summary>
        /// 停止所有插�?
        /// </summary>
        public void StopAllPlugins()
        {
            foreach (var plugin in _plugins.Values)
            {
                try
                {
                    plugin.Stop();
                }
                catch (Exception ex)
                {
                }
            }
        }

        /// <summary>
        /// 清理所有插�?
        /// </summary>
        public void CleanupAllPlugins()
        {
            foreach (var plugin in _plugins.Values)
            {
                try
                {
                    plugin.Stop();
                    plugin.Cleanup();
                }
                catch (Exception ex)
                {
                }
            }
            _plugins.Clear();
            _pluginMetadata.Clear();
        }
    }

    /// <summary>
    /// 插件元数�?
    /// </summary>
    public class PluginMetadata
    {
        /// <summary>
        /// 依赖�?
        /// </summary>
        public List<string> Dependencies { get; set; } = new List<string>();

        /// <summary>
        /// 权限要求
        /// </summary>
        public List<string> Permissions { get; set; } = new List<string>();

        /// <summary>
        /// 最小框架版�?
        /// </summary>
        public string MinFrameworkVersion { get; set; } = "1.0.0";

        /// <summary>
        /// 自定义数�?
        /// </summary>
        public Dictionary<string, object> CustomData { get; set; } = new Dictionary<string, object>();
    }
}
