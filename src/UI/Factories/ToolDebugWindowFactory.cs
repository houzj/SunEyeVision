using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using SunEyeVision.Plugin.Infrastructure;
using SunEyeVision.Plugin.Abstractions;
using SunEyeVision.UI.Factories;
using SunEyeVision.UI.Views.Windows;

namespace SunEyeVision.UI.Factories
{
    /// <summary>
    /// 工具调试窗口工厂 - 动态从插件加载调试窗口
    /// </summary>
    public static class ToolDebugWindowFactory
    {
        private static readonly Dictionary<string, Type> _debugWindowTypes = new Dictionary<string, Type>();
        private static bool _isInitialized = false;
        private static readonly object _lock = new object();

        /// <summary>
        /// 初始化 - 扫描插件目录加载调试窗口类型
        /// </summary>
        public static void Initialize()
        {
            if (_isInitialized) return;

            lock (_lock)
            {
                if (_isInitialized) return;

                try
                {
                    var pluginsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "output", "plugins");
                    if (!Directory.Exists(pluginsPath))
                    {
                        pluginsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins");
                    }

                    if (Directory.Exists(pluginsPath))
                    {
                        var dllFiles = Directory.GetFiles(pluginsPath, "SunEyeVision.Tool.*.dll");
                        foreach (var dllFile in dllFiles)
                        {
                            try
                            {
                                var assembly = Assembly.LoadFrom(dllFile);
                                var windowTypes = assembly.GetTypes()
                                    .Where(t => typeof(Window).IsAssignableFrom(t) &&
                                                t.Name.EndsWith("DebugWindow") &&
                                                !t.IsAbstract);

                                foreach (var windowType in windowTypes)
                                {
                                    // 从类型名称提取工具名称
                                    var toolName = windowType.Name.Replace("DebugWindow", "");
                                    var toolIdLower = ConvertToSnakeCase(toolName);

                                    _debugWindowTypes[toolName] = windowType;
                                    _debugWindowTypes[toolIdLower] = windowType;
                                    _debugWindowTypes[toolName + "Tool"] = windowType;
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"加载插件调试窗口失败: {dllFile}, {ex.Message}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"初始化调试窗口工厂失败: {ex.Message}");
                }

                _isInitialized = true;
            }
        }

        /// <summary>
        /// 将驼峰命名转换为蛇形命名
        /// </summary>
        private static string ConvertToSnakeCase(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            var result = new System.Text.StringBuilder();
            for (int i = 0; i < input.Length; i++)
            {
                var c = input[i];
                if (char.IsUpper(c))
                {
                    if (i > 0) result.Append('_');
                    result.Append(char.ToLower(c));
                }
                else
                {
                    result.Append(c);
                }
            }
            return result.ToString();
        }

        /// <summary>
        /// 创建工具调试窗口
        /// </summary>
        /// <param name="toolId">工具ID</param>
        /// <param name="toolPlugin">工具插件</param>
        /// <param name="toolMetadata">工具元数据</param>
        /// <returns>调试窗口实例</returns>
        public static Window CreateDebugWindow(string toolId, IToolPlugin? toolPlugin, ToolMetadata toolMetadata)
        {
            Initialize();

            // 尝试从缓存中查找调试窗口类型
            if (_debugWindowTypes.TryGetValue(toolId, out var windowType))
            {
                try
                {
                    var constructor = windowType.GetConstructor(new[] { typeof(string), typeof(IToolPlugin), typeof(ToolMetadata) });
                    if (constructor != null)
                    {
                        return (Window)constructor.Invoke(new object?[] { toolId, toolPlugin, toolMetadata });
                    }

                    // 尝试无参构造函数
                    constructor = windowType.GetConstructor(Type.EmptyTypes);
                    if (constructor != null)
                    {
                        return (Window)constructor.Invoke(null);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"创建调试窗口失败: {toolId}, {ex.Message}");
                }
            }

            // 默认使用通用调试窗口
            return new DebugWindow(toolId, toolPlugin ?? new DefaultToolPlugin(), toolMetadata);
        }

        /// <summary>
        /// 检查工具是否有专用调试窗口
        /// </summary>
        /// <param name="toolId">工具ID</param>
        /// <returns>是否有专用调试窗口</returns>
        public static bool HasCustomDebugWindow(string toolId)
        {
            Initialize();
            return _debugWindowTypes.ContainsKey(toolId);
        }
    }

    /// <summary>
    /// 默认工具插件 - 用于工具调试窗口工厂的兼容性
    /// </summary>
    internal class DefaultToolPlugin : IToolPlugin
    {
        public string Name => "Default Tool";
        public string Version => "1.0.0";
        public string Author => "SunEyeVision";
        public string Description => "Default tool plugin";
        public string PluginId => "default.tool";
        public List<string> Dependencies => new List<string>();
        public string Icon => "🔧";

        private bool _isLoaded = true;
        public bool IsLoaded => _isLoaded;

        public void Initialize() { }
        public void Unload() { }

        public List<Type> GetAlgorithmNodes() => new List<Type>();

        public List<ToolMetadata> GetToolMetadata() => new List<ToolMetadata>();

        public SunEyeVision.Core.Interfaces.IImageProcessor CreateToolInstance(string toolId)
        {
            throw new NotImplementedException();
        }

        public SunEyeVision.Core.Models.AlgorithmParameters GetDefaultParameters(string toolId)
        {
            return new SunEyeVision.Core.Models.AlgorithmParameters();
        }

        public ValidationResult ValidateParameters(string toolId, SunEyeVision.Core.Models.AlgorithmParameters parameters)
        {
            return ValidationResult.Success();
        }
    }
}
