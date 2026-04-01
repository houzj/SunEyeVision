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
    /// 工具调试窗口工厂 - 优先使用工具自定义调试窗口
    /// </summary>
    /// <remarks>
    /// 创建调试窗口的优先级：
    /// 1. toolMetadata.DebugWindowType - 工具元数据指定（推荐）
    /// 2. IToolPlugin.CreateDebugWindow() - 工具自己创建
    /// 3. 返回 null - 不再提供通用调试窗口
    /// </remarks>
    public static class ToolDebugWindowFactory
    {
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
            // ★ 优先级1：使用 toolMetadata.DebugWindowType（新增）
            if (toolMetadata.DebugWindowType != null)
            {
                try
                {
                    var window = Activator.CreateInstance(toolMetadata.DebugWindowType) as Window;
                    if (window != null)
                    {
                        // 初始化调试窗口
                        InitializeDebugWindow(window, toolId, toolPlugin, toolMetadata);

                        // 绑定主窗口的ImageControl（用于区域编辑器）
                        if (window is BaseToolDebugWindow baseDebugWindow && mainImageControl != null)
                        {
                            baseDebugWindow.SetMainImageControl(mainImageControl);
                        }

                        System.Diagnostics.Debug.WriteLine($"[ToolDebugWindowFactory] 使用 toolMetadata.DebugWindowType 创建: {toolId}");
                        return window;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"使用 toolMetadata.DebugWindowType 创建失败: {toolId}, {ex.Message}");
                }
            }

            // ★ 优先级2：通过 ToolRegistry 创建工具实例
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

            // ★ 优先级3：返回 null（不再提供通用调试窗口）
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
            // 运行时检查：使用 ToolRegistry 创建工具实例检查
            var tool = ToolRegistry.CreateToolInstance(toolId);
            if (tool != null)
            {
                return tool.HasDebugWindow;
            }

            return false;
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
