using System;
using System.Collections.Generic;
using System.Linq;
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.SDK.Metadata;

namespace SunEyeVision.Plugin.Infrastructure.Managers.Tool
{
    /// <summary>
    /// ?- ѵĹ߲?    /// </summary>
    public static class ToolRegistry
    {
        private static readonly Dictionary<string, IToolPlugin> _toolPlugins = new Dictionary<string, IToolPlugin>();
        private static readonly Dictionary<string, ToolMetadata> _toolMetadata = new Dictionary<string, ToolMetadata>();
        private static readonly object _lock = new object();

        /// <summary>
        /// ߲
        /// </summary>
        /// <param name="toolPlugin">工具插件实例</param>
        public static void RegisterTool(IToolPlugin toolPlugin)
        {
            if (toolPlugin == null)
                throw new ArgumentNullException(nameof(toolPlugin));

            lock (_lock)
            {
                var metadataList = toolPlugin.GetToolMetadata();
                if (metadataList == null || metadataList.Count == 0)
                    return;

                _toolPlugins[toolPlugin.PluginId] = toolPlugin;

                foreach (var metadata in metadataList)
                {
                    _toolMetadata[metadata.Id] = metadata;
                }
            }
        }

        /// <summary>
        /// ߲
        /// </summary>
        /// <param name="toolId">工具ID</param>
        /// <returns>߲ʵ򷵻null</returns>
        public static IToolPlugin? GetToolPlugin(string toolId)
        {
            lock (_lock)
            {
                if (_toolMetadata.TryGetValue(toolId, out var metadata))
                {
                    var pluginId = metadata.AlgorithmType?.Assembly.GetName().Name ?? string.Empty;
                    if (string.IsNullOrEmpty(pluginId))
                    {
                        foreach (var plugin in _toolPlugins.Values)
                        {
                            var metadatas = plugin.GetToolMetadata();
                            if (metadatas.Any(m => m.Id == toolId))
                            {
                                return plugin;
                            }
                        }
                    }
                    return _toolPlugins.Values.FirstOrDefault(p => p.GetToolMetadata().Any(m => m.Id == toolId));
                }
                return null;
            }
        }

        /// <summary>
        /// ȡ工具元数?        /// </summary>
        /// <param name="toolId">工具ID</param>
        /// <returns>Ԫ򷵻null</returns>
        public static ToolMetadata? GetToolMetadata(string toolId)
        {
            lock (_lock)
            {
                return _toolMetadata.TryGetValue(toolId, out var metadata) ? metadata : null;
            }
        }

        /// <summary>
        /// ȡ有工具元
        /// </summary>
        /// <returns>йԪб</returns>
        public static List<ToolMetadata> GetAllToolMetadata()
        {
            lock (_lock)
            {
                return _toolMetadata.Values.ToList();
            }
        }

        /// <summary>
        /// ȡԪ
        /// </summary>
        /// <param name="category">分类名称</param>
        /// <returns>÷µĹԪб</returns>
        public static List<ToolMetadata> GetToolsByCategory(string category)
        {
            lock (_lock)
            {
                return _toolMetadata.Values.Where(m => m.Category == category).ToList();
            }
        }

        /// <summary>
        /// 查工具是否存?        /// </summary>
        /// <param name="toolId">工具ID</param>
        /// <returns>昐存在</returns>
        public static bool ToolExists(string toolId)
        {
            lock (_lock)
            {
                return _toolMetadata.ContainsKey(toolId);
            }
        }

        /// <summary>
        /// ע
        /// </summary>
        /// <param name="toolId">工具ID</param>
        /// <returns>昐ɹ</returns>
        public static bool UnregisterTool(string toolId)
        {
            lock (_lock)
            {
                if (_toolMetadata.Remove(toolId))
                {
                    var plugin = _toolPlugins.Values.FirstOrDefault(p =>
                        p.GetToolMetadata().Any(m => m.Id == toolId));

                    if (plugin != null)
                    {
                        var remainingTools = plugin.GetToolMetadata().Count(m =>
                            _toolMetadata.ContainsKey(m.Id));

                        if (remainingTools == 0)
                        {
                            _toolPlugins.Remove(plugin.PluginId);
                        }
                    }
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// עĹ
        /// </summary>
        public static void ClearAll()
        {
            lock (_lock)
            {
                _toolPlugins.Clear();
                _toolMetadata.Clear();
            }
        }

        /// <summary>
        /// עĹ
        /// </summary>
        /// <returns>工具数量</returns>
        public static int GetToolCount()
        {
            lock (_lock)
            {
                return _toolMetadata.Count;
            }
        }

        /// <summary>
        /// ȡ有分?        /// </summary>
        /// <returns>分类列表</returns>
        public static List<string> GetAllCategories()
        {
            lock (_lock)
            {
                return _toolMetadata.Values
                    .Select(m => m.Category)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToList();
            }
        }
    }
}
