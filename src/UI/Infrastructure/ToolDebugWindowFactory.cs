using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using SunEyeVision.Plugin.Infrastructure;
using SunEyeVision.Plugin.SDK;
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.SDK.Execution.Results;
using SunEyeVision.Plugin.SDK.Metadata;
using SunEyeVision.Plugin.SDK.UI;
using SunEyeVision.Plugin.SDK.Validation;
using SunEyeVision.Plugin.SDK.UI.Controls;
using SunEyeVision.Plugin.Infrastructure.Managers.Tool;

namespace SunEyeVision.UI.Factories
{
    /// <summary>
    /// 工具调试窗口工厂 - 优先使用ITool.CreateDebugWindow()
    /// </summary>
    /// <remarks>
    /// 创建调试窗口的优先级：
    /// 1. ITool.CreateDebugWindow() - 工具自己创建（推荐）
    /// 2. 反射加载插件中的调试窗口类型（兼容旧版本）
    /// 3. 抛出异常 - 不再提供UI层自动生成的调试窗口
    /// </remarks>
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
                    // 插件目录在应用程序目录下的 plugins 子目录
                    var pluginsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins");

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
                                    RegisterDebugWindowType(windowType);
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
        /// 注册调试窗口类型 - 生成多种匹配键以支持不同的命名风格
        /// </summary>
        private static void RegisterDebugWindowType(Type windowType)
        {
            var toolName = windowType.Name.Replace("DebugWindow", "");
            
            var toolNameWithoutToolSuffix = toolName;
            if (toolName.EndsWith("Tool"))
            {
                toolNameWithoutToolSuffix = toolName.Substring(0, toolName.Length - 4);
            }

            var toolIdWithToolSuffix = ConvertToSnakeCase(toolName);
            var toolIdWithoutToolSuffix = ConvertToSnakeCase(toolNameWithoutToolSuffix);

            _debugWindowTypes[toolName] = windowType;
            _debugWindowTypes[toolNameWithoutToolSuffix] = windowType;
            _debugWindowTypes[toolIdWithToolSuffix] = windowType;
            _debugWindowTypes[toolIdWithoutToolSuffix] = windowType;
            
            _debugWindowTypes[toolName + "Tool"] = windowType;
            _debugWindowTypes[toolIdWithoutToolSuffix + "_tool"] = windowType;

            System.Diagnostics.Debug.WriteLine($"[ToolDebugWindowFactory] 注册调试窗口: {windowType.Name}");
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
        /// 创建工具调试窗口 - 优先使用IToolPlugin.CreateDebugWindow()
        /// </summary>
        /// <param name="toolId">工具ID</param>
        /// <param name="toolPlugin">工具插件（可选，用于兼容旧版本）</param>
        /// <param name="toolMetadata">工具元数据</param>
        /// <param name="mainImageControl">主窗口的ImageControl（用于区域编辑器绑定）</param>
        /// <returns>调试窗口实例，无窗口工具返回 null</returns>
        public static Window? CreateDebugWindow(string toolId, IToolPlugin? toolPlugin, ToolMetadata toolMetadata, ImageControl? mainImageControl = null)
        {
            Initialize();

            // ★ 优先级1：通过 ToolRegistry 创建工具实例
            var tool = ToolRegistry.CreateToolInstance(toolId);
            if (tool != null)
            {
                // 检查工具是否支持调试窗口
                if (!tool.HasDebugWindow)
                {
                    System.Diagnostics.Debug.WriteLine($"[ToolDebugWindowFactory] 工具 {toolId} 不支持调试窗口 (HasDebugWindow=false)");
                    return null;
                }

                try
                {
                    var window = tool.CreateDebugWindow();
                    if (window != null)
                    {
                        // ★ 初始化调试窗口（传递 toolId, toolPlugin, toolMetadata）
                        InitializeDebugWindow(window, toolId, toolPlugin, toolMetadata);
                        
                        // ★ 绑定主窗口的ImageControl（用于区域编辑器）
                        if (window is BaseToolDebugWindow baseDebugWindow && mainImageControl != null)
                        {
                            baseDebugWindow.SetMainImageControl(mainImageControl);
                        }
                        
                        System.Diagnostics.Debug.WriteLine($"[ToolDebugWindowFactory] 使用 IToolPlugin.CreateDebugWindow() 创建: {toolId}");
                        return window;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"IToolPlugin.CreateDebugWindow() 失败: {toolId}, {ex.Message}");
                }
            }

            // ★ 优先级2：反射加载插件中的调试窗口类型（兼容旧版本）
            if (_debugWindowTypes.TryGetValue(toolId, out var windowType))
            {
                try
                {
                    var constructor = windowType.GetConstructor(new[] { typeof(string), typeof(IToolPlugin), typeof(ToolMetadata) });
                    if (constructor != null)
                    {
                        return (Window)constructor.Invoke(new object?[] { toolId, toolPlugin, toolMetadata });
                    }

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

            // ★ 优先级3：返回 null（不再抛出异常）
            System.Diagnostics.Debug.WriteLine($"[ToolDebugWindowFactory] 工具 {toolId} 无调试窗口");
            return null;
        }

        /// <summary>
        /// 检查工具是否支持调试窗口（运行时检查）
        /// </summary>
        /// <param name="toolId">工具ID</param>
        /// <param name="toolPlugin">工具插件（可选）</param>
        /// <returns>是否支持调试窗口</returns>
        public static bool HasDebugWindow(string toolId, IToolPlugin? toolPlugin)
        {
            Initialize();

            // 运行时检查：使用 ToolRegistry 创建工具实例检查
            var tool = ToolRegistry.CreateToolInstance(toolId);
            if (tool != null)
            {
                return tool.HasDebugWindow;
            }

            // 兼容旧版本：检查是否有注册的调试窗口类型
            return _debugWindowTypes.ContainsKey(toolId);
        }

        /// <summary>
        /// 检查工具是否有专用调试窗口
        /// </summary>
        public static bool HasCustomDebugWindow(string toolId)
        {
            Initialize();
            return _debugWindowTypes.ContainsKey(toolId);
        }

        /// <summary>
        /// 初始化调试窗口及其 ViewModel
        /// </summary>
        private static void InitializeDebugWindow(Window window, string toolId, IToolPlugin? toolPlugin, ToolMetadata? toolMetadata)
        {
            // 尝试调用窗口的 Initialize 方法
            var windowInitMethod = window.GetType().GetMethod("Initialize",
                new[] { typeof(string), typeof(IToolPlugin), typeof(ToolMetadata) });
            if (windowInitMethod != null)
            {
                try
                {
                    windowInitMethod.Invoke(window, new object?[] { toolId, toolPlugin, toolMetadata });
                    System.Diagnostics.Debug.WriteLine($"[ToolDebugWindowFactory] 已初始化窗口: {window.GetType().Name}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[ToolDebugWindowFactory] 初始化窗口失败: {ex.Message}");
                }
            }

            // 尝试初始化 DataContext (ViewModel)
            var dataContext = window.DataContext;
            if (dataContext != null)
            {
                var vmInitMethod = dataContext.GetType().GetMethod("Initialize",
                    new[] { typeof(string), typeof(IToolPlugin), typeof(ToolMetadata) });
                if (vmInitMethod != null)
                {
                    try
                    {
                        vmInitMethod.Invoke(dataContext, new object?[] { toolId, toolPlugin, toolMetadata });
                        System.Diagnostics.Debug.WriteLine($"[ToolDebugWindowFactory] 已初始化 ViewModel: {dataContext.GetType().Name}");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[ToolDebugWindowFactory] 初始化 ViewModel 失败: {ex.Message}");
                    }
                }
            }
        }
    }

    /// <summary>
    /// 默认工具插件 - 用于工具调试窗口工厂的兼容性
    /// </summary>
    internal class DefaultToolPlugin : IToolPlugin
    {
        public Type ParamsType => typeof(GenericToolParameters);
        public Type ResultType => typeof(GenericToolResults);

        public ToolResults Run(OpenCvSharp.Mat image, ToolParameters parameters)
        {
            return new GenericToolResults();
        }

        public bool HasDebugWindow => false;
        public System.Windows.Window? CreateDebugWindow() => null;
        public ToolParameters GetDefaultParameters() => new GenericToolParameters();
    }
}
