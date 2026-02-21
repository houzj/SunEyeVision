using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using SunEyeVision.Core.Interfaces;
using SunEyeVision.Plugin.Abstractions;

namespace SunEyeVision.Plugin.Infrastructure
{
    /// <summary>
    /// 插件管理器实现
    /// </summary>
    public class PluginManager : IPluginManager
    {
        private readonly ILogger _logger;
        private readonly Dictionary<Type, List<object>> _plugins = new Dictionary<Type, List<object>>();

        public PluginManager(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 无参构造函数 - 使用空日志器
        /// </summary>
        public PluginManager()
        {
            _logger = new NullLogger();
        }

        /// <summary>
        /// 加载所有插件
        /// </summary>
        public void LoadPlugins()
        {
            _logger.LogInfo("开始加载插件...");
            // 插件加载逻辑由 PluginLoader 处理
            _logger.LogInfo("插件加载完成");
        }

        /// <summary>
        /// 从指定目录加载所有插件
        /// </summary>
        /// <param name="pluginDirectory">插件目录路径</param>
        public void LoadPlugins(string pluginDirectory)
        {
            _logger.LogInfo($"开始从目录加载插件: {pluginDirectory}");
            
            if (!Directory.Exists(pluginDirectory))
            {
                _logger.LogInfo($"插件目录不存在: {pluginDirectory}");
                return;
            }

            var dllFiles = Directory.GetFiles(pluginDirectory, "*.dll", SearchOption.AllDirectories);
            _logger.LogInfo($"找到 {dllFiles.Length} 个DLL文件");

            foreach (var dllFile in dllFiles)
            {
                LoadPlugin(dllFile);
            }

            _logger.LogInfo($"插件加载完成，共加载 {ToolRegistry.GetToolCount()} 个工具");
        }

        /// <summary>
        /// 加载单个插件DLL
        /// </summary>
        private void LoadPlugin(string dllPath)
        {
            try
            {
                var assembly = Assembly.LoadFrom(dllPath);
                
                // 加载 IToolPlugin 类型插件并注册到 ToolRegistry
                var toolPluginTypes = assembly.GetTypes()
                    .Where(t => typeof(IToolPlugin).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

                foreach (var toolPluginType in toolPluginTypes)
                {
                    try
                    {
                        var toolPlugin = (IToolPlugin?)Activator.CreateInstance(toolPluginType);
                        if (toolPlugin != null)
                        {
                            toolPlugin.Initialize();
                            ToolRegistry.RegisterTool(toolPlugin);
                            RegisterPlugin(toolPlugin);
                            _logger.LogInfo($"已加载工具插件: {toolPlugin.Name}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"加载工具插件失败: {toolPluginType.Name} - {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"加载插件DLL失败: {dllPath} - {ex.Message}");
            }
        }

        /// <summary>
        /// 卸载所有插件
        /// </summary>
        public void UnloadPlugins()
        {
            _logger.LogInfo("开始卸载插件...");
            _plugins.Clear();
            ToolRegistry.ClearAll();
            _logger.LogInfo("插件卸载完成");
        }

        /// <summary>
        /// 获取所有插件
        /// </summary>
        /// <typeparam name="T">插件类型</typeparam>
        /// <returns>插件列表</returns>
        public List<T> GetPlugins<T>() where T : class
        {
            var pluginType = typeof(T);
            if (_plugins.TryGetValue(pluginType, out var pluginList))
            {
                return pluginList.OfType<T>().ToList();
            }
            return new List<T>();
        }

        /// <summary>
        /// 注册插件
        /// </summary>
        /// <param name="plugin">插件实例</param>
        public void RegisterPlugin(object plugin)
        {
            if (plugin == null)
            {
                throw new ArgumentNullException(nameof(plugin));
            }

            var pluginType = plugin.GetType();
            var interfaces = pluginType.GetInterfaces();

            foreach (var iface in interfaces)
            {
                if (!_plugins.ContainsKey(iface))
                {
                    _plugins[iface] = new List<object>();
                }

                _plugins[iface].Add(plugin);
                _logger.LogInfo($"已注册插件: {plugin.GetType().Name} 实现接口: {iface.Name}");
            }
        }

        /// <summary>
        /// 注销插件
        /// </summary>
        /// <param name="plugin">插件实例</param>
        public void UnregisterPlugin(object plugin)
        {
            if (plugin == null)
            {
                throw new ArgumentNullException(nameof(plugin));
            }

            var pluginType = plugin.GetType();
            var interfaces = pluginType.GetInterfaces();

            foreach (var iface in interfaces)
            {
                if (_plugins.TryGetValue(iface, out var pluginList))
                {
                    pluginList.Remove(plugin);
                    _logger.LogInfo($"已注销插件: {plugin.GetType().Name}");
                }
            }
        }

        /// <summary>
        /// 检查插件是否已加载
        /// </summary>
        /// <typeparam name="T">插件类型</typeparam>
        /// <returns>是否已加载</returns>
        public bool IsPluginLoaded<T>() where T : class
        {
            var pluginType = typeof(T);
            return _plugins.ContainsKey(pluginType) && _plugins[pluginType].Count > 0;
        }
    }

    /// <summary>
    /// 空日志器 - 不输出任何日志
    /// </summary>
    internal class NullLogger : ILogger
    {
        public void LogDebug(string message) { }
        public void LogInfo(string message) { }
        public void LogWarning(string message) { }
        public void LogError(string message, Exception? exception = null) { }
    }
}
