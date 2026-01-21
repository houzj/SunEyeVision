using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using SunEyeVision.Interfaces;

namespace SunEyeVision.PluginSystem
{
    /// <summary>
    /// Plugin loader
    /// </summary>
    public class PluginLoader
    {
        private readonly ILogger _logger;
        private readonly Dictionary<string, IVisionPlugin> _loadedPlugins;

        public PluginLoader(ILogger logger)
        {
            _logger = logger;
            _loadedPlugins = new Dictionary<string, IVisionPlugin>();
        }

        /// <summary>
        /// Load all plugins from directory
        /// </summary>
        /// <param name="pluginDirectory">Plugin directory</param>
        public void LoadPluginsFromDirectory(string pluginDirectory)
        {
            if (!Directory.Exists(pluginDirectory))
            {
                _logger.LogWarning($"Plugin directory does not exist: {pluginDirectory}");
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
                    _logger.LogError($"Failed to load plugin: {dllFile}", ex);
                }
            }

            _logger.LogInfo($"Loaded {_loadedPlugins.Count} plugins");
        }

        /// <summary>
        /// Load single plugin
        /// </summary>
        /// <param name="pluginPath">Plugin DLL path</param>
        public void LoadPlugin(string pluginPath)
        {
            _logger.LogInfo($"Loading plugin: {pluginPath}");

            var assembly = Assembly.LoadFrom(pluginPath);
            var pluginTypes = assembly.GetTypes()
                .Where(t => typeof(IVisionPlugin).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

            foreach (var pluginType in pluginTypes)
            {
                try
                {
                    var plugin = (IVisionPlugin)Activator.CreateInstance(pluginType);

                    // Check if plugin ID already exists
                    if (_loadedPlugins.ContainsKey(plugin.PluginId))
                    {
                        _logger.LogWarning($"Plugin ID {plugin.PluginId} already exists, skipping");
                        continue;
                    }

                    // Initialize plugin
                    plugin.Initialize();
                    _loadedPlugins[plugin.PluginId] = plugin;

                    _logger.LogInfo($"Plugin loaded successfully: {plugin.Name} v{plugin.Version}");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to instantiate plugin: {pluginType.Name}", ex);
                }
            }
        }

        /// <summary>
        /// Unload plugin
        /// </summary>
        /// <param name="pluginId">Plugin ID</param>
        public bool UnloadPlugin(string pluginId)
        {
            if (!_loadedPlugins.TryGetValue(pluginId, out var plugin))
            {
                _logger.LogWarning($"Plugin not found: {pluginId}");
                return false;
            }

            try
            {
                plugin.Unload();
                _loadedPlugins.Remove(pluginId);
                _logger.LogInfo($"Plugin unloaded successfully: {plugin.Name}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to unload plugin: {plugin.Name}", ex);
                return false;
            }
        }

        /// <summary>
        /// Get loaded plugin
        /// </summary>
        /// <param name="pluginId">Plugin ID</param>
        public IVisionPlugin GetPlugin(string pluginId)
        {
            _loadedPlugins.TryGetValue(pluginId, out var plugin);
            return plugin;
        }

        /// <summary>
        /// Get all loaded plugins
        /// </summary>
        public List<IVisionPlugin> GetAllPlugins()
        {
            return _loadedPlugins.Values.ToList();
        }

        /// <summary>
        /// Get all algorithm node types from plugins
        /// </summary>
        public List<Type> GetAllAlgorithmNodes()
        {
            var allNodes = new List<Type>();

            foreach (var plugin in _loadedPlugins.Values)
            {
                try
                {
                    var nodes = plugin.GetAlgorithmNodes();
                    if (nodes != null)
                    {
                        allNodes.AddRange(nodes);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to get plugin algorithm nodes: {plugin.Name}", ex);
                }
            }

            return allNodes;
        }

        /// <summary>
        /// Unload all plugins
        /// </summary>
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
