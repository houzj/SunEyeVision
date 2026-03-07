using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using SunEyeVision.Plugin.SDK.Logging;
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.Infrastructure.Managers.Tool;

namespace SunEyeVision.Plugin.Infrastructure.Managers.Plugin
{
    /// <summary>
    /// 插件加载器 - 从目录加载工具并注册到 ToolRegistry
    /// </summary>
    /// <remarks>
    /// 简化后的设计：
    /// - 扫描带有 [Tool] 特性的类
    /// - 直接注册到 ToolRegistry
    /// - 不再需要中间的 Plugin 包装类
    /// </remarks>
    public class PluginLoader
    {
        private readonly ILogger _logger;
        private readonly HashSet<string> _loadedAssemblies = new();

        public PluginLoader(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 从目录加载所有插件
        /// </summary>
        /// <param name="pluginDirectory">插件目录</param>
        public void LoadPluginsFromDirectory(string pluginDirectory)
        {
            if (!Directory.Exists(pluginDirectory))
            {
                _logger.LogWarning($"插件目录不存在: {pluginDirectory}");
                return;
            }

            var dllFiles = Directory.GetFiles(pluginDirectory, "*.dll", SearchOption.TopDirectoryOnly);
            var loadedCount = 0;

            foreach (var dllFile in dllFiles)
            {
                try
                {
                    if (LoadPlugin(dllFile))
                    {
                        loadedCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"加载插件失败: {dllFile}", ex);
                }
            }

            _logger.LogInfo($"插件加载完成，共加载 {ToolRegistry.GetToolCount()} 个工具");
        }

        /// <summary>
        /// 加载单个插件
        /// </summary>
        /// <param name="pluginPath">插件DLL路径</param>
        /// <returns>是否成功加载</returns>
        public bool LoadPlugin(string pluginPath)
        {
            if (_loadedAssemblies.Contains(pluginPath))
            {
                _logger.LogWarning($"插件已加载: {pluginPath}");
                return false;
            }

            _logger.LogInfo($"正在加载插件: {pluginPath}");

            var assembly = Assembly.LoadFrom(pluginPath);
            
            // 扫描带有 [Tool] 特性的类
            var toolTypes = assembly.GetTypes()
                .Where(t => t.GetCustomAttribute<ToolAttribute>() != null && !t.IsAbstract);

            var loaded = false;
            foreach (var toolType in toolTypes)
            {
                try
                {
                    // 确保实现了 IToolPlugin 接口
                    if (!typeof(IToolPlugin).IsAssignableFrom(toolType))
                    {
                        _logger.LogWarning($"类型 {toolType.Name} 有 [Tool] 特性但未实现 IToolPlugin 接口");
                        continue;
                    }

                    ToolRegistry.RegisterTool(toolType);
                    _logger.LogInfo($"已注册工具: {toolType.Name}");
                    loaded = true;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"注册工具失败: {toolType.Name}", ex);
                }
            }

            if (loaded)
            {
                _loadedAssemblies.Add(pluginPath);
            }

            return loaded;
        }

        /// <summary>
        /// 获取已加载的工具数量
        /// </summary>
        public int GetToolCount() => ToolRegistry.GetToolCount();

        /// <summary>
        /// 获取所有工具元数据
        /// </summary>
        public List<SunEyeVision.Plugin.SDK.Metadata.ToolMetadata> GetAllToolMetadata()
        {
            return ToolRegistry.GetAllToolMetadata();
        }
    }
}
