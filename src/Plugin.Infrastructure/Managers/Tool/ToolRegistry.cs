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
    /// 简化后的设计：
    /// - 直接管理工具类型，不再需要中间的 Plugin 层
    /// - 元数据从 [Tool] 特性自动提取
    /// - 支持按需创建工具实例
    /// </remarks>
    public static class ToolRegistry
    {
        private static readonly Dictionary<string, Type> _toolTypes = new();
        private static readonly Dictionary<string, ToolMetadata> _toolMetadata = new();
        private static readonly object _lock = new();

        /// <summary>
        /// 注册工具类型
        /// </summary>
        /// <param name="toolType">工具类型（必须实现 IToolPlugin）</param>
        /// <param name="metadata">工具元数据</param>
        public static void Register(Type toolType, ToolMetadata metadata)
        {
            if (toolType == null)
                throw new ArgumentNullException(nameof(toolType));
            if (metadata == null)
                throw new ArgumentNullException(nameof(metadata));

            lock (_lock)
            {
                _toolTypes[metadata.Id] = toolType;
                _toolMetadata[metadata.Id] = metadata;
            }
        }

        /// <summary>
        /// 自动注册工具类型（从 [Tool] 特性提取元数据）
        /// </summary>
        /// <param name="toolType">工具类型</param>
        public static void RegisterTool(Type toolType)
        {
            if (toolType == null)
                throw new ArgumentNullException(nameof(toolType));

            var attr = toolType.GetCustomAttribute<ToolAttribute>();
            if (attr == null)
                throw new ArgumentException($"类型 {toolType.Name} 缺少 [Tool] 特性");

            // 从泛型接口获取参数/结果类型
            Type? paramsType = null;
            Type? resultType = null;
            
            var interfaces = toolType.GetInterfaces();
            foreach (var iface in interfaces)
            {
                if (iface.IsGenericType && iface.GetGenericTypeDefinition().Name.StartsWith("IToolPlugin"))
                {
                    var genericArgs = iface.GetGenericArguments();
                    if (genericArgs.Length >= 2)
                    {
                        paramsType = genericArgs[0];
                        resultType = genericArgs[1];
                        break;
                    }
                }
            }

            var metadata = new ToolMetadata
            {
                Id = attr.Id,
                Name = attr.Id,
                DisplayName = attr.DisplayName,
                Description = attr.Description,
                Icon = attr.Icon,
                Category = attr.Category,
                Version = attr.Version,
                Author = attr.Author,
                AlgorithmType = toolType,
                ParamsType = paramsType,
                ResultType = resultType,
                HasDebugInterface = attr.HasDebugWindow
            };

            Register(toolType, metadata);
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
            lock (_lock)
            {
                if (_toolMetadata.TryGetValue(toolId, out var meta) && meta.ParamsType != null)
                {
                    return (ToolParameters)Activator.CreateInstance(meta.ParamsType)!;
                }
                return new GenericToolParameters();
            }
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
        /// 获取工具元数据
        /// </summary>
        /// <param name="toolId">工具ID</param>
        /// <returns>工具元数据，未找到则返回null</returns>
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
        /// 获取指定分类的工具元数据
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
        /// 注销工具
        /// </summary>
        /// <param name="toolId">工具ID</param>
        /// <returns>是否注销成功</returns>
        public static bool UnregisterTool(string toolId)
        {
            lock (_lock)
            {
                _toolTypes.Remove(toolId);
                return _toolMetadata.Remove(toolId);
            }
        }

        /// <summary>
        /// 清除所有已注册的工具
        /// </summary>
        public static void ClearAll()
        {
            lock (_lock)
            {
                _toolTypes.Clear();
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
