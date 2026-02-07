using System;
using System.Collections.Generic;
using System.Linq;
using SunEyeVision.PluginSystem.Base.Interfaces;
using SunEyeVision.PluginSystem.Base.Models;

namespace SunEyeVision.PluginSystem.Base.Services
{
    /// <summary>
    /// 工具注册表 - 管理所有已注册的工具插件
    /// </summary>
    public static class ToolRegistry
    {
        private static readonly Dictionary<string, IToolPlugin> _toolPlugins = new Dictionary<string, IToolPlugin>();
        private static readonly Dictionary<string, ToolMetadata> _toolMetadata = new Dictionary<string, ToolMetadata>();
        private static readonly object _lock = new object();

        /// <summary>
        /// 注册工具插件
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
        /// 获取工具插件
        /// </summary>
        /// <param name="toolId">工具ID</param>
        /// <returns>工具插件实例，如果不存在则返回null</returns>
        public static IToolPlugin? GetToolPlugin(string toolId)
        {
            lock (_lock)
            {
                if (_toolMetadata.TryGetValue(toolId, out var metadata))
                {
                    var pluginId = metadata.AlgorithmType?.Assembly.GetName().Name ?? string.Empty;
                    if (string.IsNullOrEmpty(pluginId))
                    {
                        // 尝试从所有插件中查找
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
        /// 获取工具元数据
        /// </summary>
        /// <param name="toolId">工具ID</param>
        /// <returns>工具元数据，如果不存在则返回null</returns>
        public static ToolMetadata? GetToolMetadata(string toolId)
        {
            lock (_lock)
            {
                return _toolMetadata.TryGetValue(toolId, out var metadata) ? metadata : null;
            }
        }

        /// <summary>
        /// 获取所有工具元数据
        /// </summary>
        /// <returns>所有工具元数据列表</returns>
        public static List<ToolMetadata> GetAllToolMetadata()
        {
            lock (_lock)
            {
                return _toolMetadata.Values.ToList();
            }
        }

        /// <summary>
        /// 按分类获取工具元数据
        /// </summary>
        /// <param name="category">分类名称</param>
        /// <returns>该分类下的工具元数据列表</returns>
        public static List<ToolMetadata> GetToolsByCategory(string category)
        {
            lock (_lock)
            {
                return _toolMetadata.Values.Where(m => m.Category == category).ToList();
            }
        }

        /// <summary>
        /// 检查工具是否存在
        /// </summary>
        /// <param name="toolId">工具ID</param>
        /// <returns>是否存在</returns>
        public static bool ToolExists(string toolId)
        {
            lock (_lock)
            {
                return _toolMetadata.ContainsKey(toolId);
            }
        }

        /// <summary>
        /// 注销工具插件
        /// </summary>
        /// <param name="toolId">工具ID</param>
        /// <returns>是否成功</returns>
        public static bool UnregisterTool(string toolId)
        {
            lock (_lock)
            {
                if (_toolMetadata.Remove(toolId))
                {
                    // 检查是否还有其他工具属于同一个插件
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
        /// 清空所有注册的工具
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
        /// 获取已注册的工具数量
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
        /// 获取所有分类
        /// </summary>
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
