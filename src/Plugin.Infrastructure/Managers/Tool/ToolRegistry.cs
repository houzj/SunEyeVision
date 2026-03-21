using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.SDK.Metadata;

namespace SunEyeVision.Plugin.Infrastructure.Managers.Tool
{
    /// <summary>
    /// 工具注册中心 - 管理所有已注册的工具
    /// </summary>
    /// <remarks>
    /// 优化后的设计：
    /// - 只管理工具类型字典（简化为1个字典）
    /// - 元数据按需创建（延迟创建，减少内存占用）
    /// - 从 [Tool] 特性自动提取基本信息
    /// </remarks>
    public static class ToolRegistry
    {
        private static readonly Dictionary<string, Type> _toolTypes = new();
        private static readonly Dictionary<string, ToolMetadata> _metadataCache = new();
        private static readonly object _lock = new();

        /// <summary>
        /// 注册工具类型（从 [Tool] 特性提取元数据）
        /// </summary>
        /// <param name="toolType">工具类型</param>
        public static void RegisterTool(Type toolType)
        {
            if (toolType == null)
                throw new ArgumentNullException(nameof(toolType));

            var attr = toolType.GetCustomAttribute<ToolAttribute>();
            if (attr == null)
                throw new ArgumentException($"类型 {toolType.Name} 缺少 [Tool] 特性");

            lock (_lock)
            {
                _toolTypes[attr.Id] = toolType;
            }
        }

        /// <summary>
        /// 获取工具元数据（带缓存）
        /// </summary>
        /// <param name="toolId">工具ID</param>
        /// <returns>工具元数据，未找到则返回null</returns>
        public static ToolMetadata? GetToolMetadata(string toolId)
        {
            lock (_lock)
            {
                if (!_toolTypes.TryGetValue(toolId, out var toolType))
                    return null;

                var attr = toolType.GetCustomAttribute<ToolAttribute>();
                if (attr == null)
                    return null;

                if (_metadataCache.TryGetValue(toolId, out var cachedMetadata))
                    return cachedMetadata;

                var metadata = ToolMetadata.FromToolType(
                    toolType,
                    attr.Id,
                    attr.DisplayName,
                    attr.Description,
                    attr.Icon,
                    attr.Category
                );

                _metadataCache[toolId] = metadata;
                return metadata;
            }
        }

        /// <summary>
        /// 获取所有工具元数据（带缓存）
        /// </summary>
        /// <returns>所有工具元数据列表</returns>
        public static List<ToolMetadata> GetAllToolMetadata()
        {
            lock (_lock)
            {
                return _toolTypes.Values.Select(toolType =>
                {
                    var attr = toolType.GetCustomAttribute<ToolAttribute>();
                    if (attr == null)
                        return null;

                    var toolId = attr.Id;
                    if (_metadataCache.TryGetValue(toolId, out var cachedMetadata))
                        return cachedMetadata;

                    var metadata = ToolMetadata.FromToolType(
                        toolType,
                        attr.Id,
                        attr.DisplayName,
                        attr.Description,
                        attr.Icon,
                        attr.Category
                    );

                    _metadataCache[toolId] = metadata;
                    return metadata;
                }).Where(m => m != null).ToList()!;
            }
        }

        /// <summary>
        /// 获取指定分类的工具元数据
        /// </summary>
        /// <param name="category">分类名称</param>
        /// <returns>该分类下的工具元数据列表</returns>
        public static List<ToolMetadata> GetToolsByCategory(string category)
        {
            return GetAllToolMetadata().Where(m => m.Category == category).ToList();
        }

        /// <summary>
        /// 创建工具实例
        /// </summary>
        /// <param name="toolId">工具ID</param>
        /// <returns>工具实例，未找到则返回null</returns>
        public static IToolPlugin? CreateToolInstance(string toolId)
        {
            lock (_lock)
            {
                if (_toolTypes.TryGetValue(toolId, out var type))
                {
                    return Activator.CreateInstance(type) as IToolPlugin;
                }
                return null;
            }
        }

        /// <summary>
        /// 创建参数实例
        /// </summary>
        /// <param name="toolId">工具ID</param>
        /// <returns>参数实例</returns>
        public static ToolParameters CreateParameters(string toolId)
        {
            var tool = CreateToolInstance(toolId);
            if (tool != null)
            {
                return (ToolParameters)Activator.CreateInstance(tool.ParamsType)!;
            }
            return new GenericToolParameters();
        }

        /// <summary>
        /// 创建调试窗口
        /// </summary>
        /// <param name="toolId">工具ID</param>
        /// <returns>调试窗口实例</returns>
        public static System.Windows.Window? CreateDebugWindow(string toolId)
        {
            var tool = CreateToolInstance(toolId);
            return tool?.HasDebugWindow == true ? tool.CreateDebugWindow() : null;
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
                return _toolTypes.ContainsKey(toolId);
            }
        }

        /// <summary>
        /// 注销工具
        /// </summary>
        /// <param name="toolId">工具ID</param>
        /// <returns>是否注销成功</returns>
        public static bool UnregisterTool(string toolId)
        {
            lock (_lock)
            {
                var removed = _toolTypes.Remove(toolId);
                _metadataCache.Remove(toolId);
                return removed;
            }
        }

        /// <summary>
        /// 清除所有已注册的工具和缓存
        /// </summary>
        public static void ClearAll()
        {
            lock (_lock)
            {
                _toolTypes.Clear();
                _metadataCache.Clear();
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
                return _toolTypes.Count;
            }
        }

        /// <summary>
        /// 获取所有分类
        /// </summary>
        /// <returns>分类列表</returns>
        public static List<string> GetAllCategories()
        {
            return GetAllToolMetadata()
                .Select(m => m.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToList();
        }
    }
}
