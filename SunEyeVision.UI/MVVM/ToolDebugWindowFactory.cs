using System;
using System.Collections.Generic;
using System.Windows;
using SunEyeVision.PluginSystem;
using SunEyeVision.PluginSystem.Tools.ImageSaveTool.UI;
using SunEyeVision.PluginSystem.UI.Tools;

namespace SunEyeVision.UI
{
    /// <summary>
    /// 工具调试窗口工厂 - 根据工具ID创建对应的调试窗口
    /// </summary>
    public static class ToolDebugWindowFactory
    {
        /// <summary>
        /// 创建工具调试窗口
        /// </summary>
        /// <param name="toolId">工具ID</param>
        /// <param name="toolPlugin">工具插件</param>
        /// <param name="toolMetadata">工具元数据</param>
        /// <returns>调试窗口实例</returns>
        public static Window CreateDebugWindow(string toolId, IToolPlugin? toolPlugin, ToolMetadata toolMetadata)
        {
            // 根据工具ID或工具类型创建对应的调试窗口
            switch (toolId)
            {
                case "ImageSaveTool":
                    return new ImageSaveToolDebugWindow(toolId, toolPlugin, toolMetadata);
                case "ColorConvertTool":
                    return new ColorConvertToolDebugWindow(toolId, toolPlugin, toolMetadata);
                case "EdgeDetectionTool":
                    return new EdgeDetectionToolDebugWindow(toolId, toolPlugin, toolMetadata);
                case "GaussianBlurTool":
                    return new GaussianBlurToolDebugWindow(toolId, toolPlugin, toolMetadata);
                case "ImageCaptureTool":
                    return new ImageCaptureToolDebugWindow(toolId, toolPlugin, toolMetadata);
                case "OCRTool":
                    return new OCRToolDebugWindow(toolId, toolPlugin, toolMetadata);
                case "ROICropTool":
                    return new ROICropToolDebugWindow(toolId, toolPlugin, toolMetadata);
                case "TemplateMatchingTool":
                    return new TemplateMatchingToolDebugWindow(toolId, toolPlugin, toolMetadata);
                case "ThresholdTool":
                    return new ThresholdToolDebugWindow(toolId, toolPlugin, toolMetadata);

                default:
                    // 默认使用通用调试窗口
                    return new DebugWindow(toolId, toolPlugin ?? new DefaultToolPlugin(), toolMetadata);
            }
        }

        /// <summary>
        /// 检查工具是否有专用调试窗口
        /// </summary>
        /// <param name="toolId">工具ID</param>
        /// <returns>是否有专用调试窗口</returns>
        public static bool HasCustomDebugWindow(string toolId)
        {
            switch (toolId)
            {
                case "ImageSaveTool":
                case "ColorConvertTool":
                case "EdgeDetectionTool":
                case "GaussianBlurTool":
                case "ImageCaptureTool":
                case "OCRTool":
                case "ROICropTool":
                case "TemplateMatchingTool":
                case "ThresholdTool":
                    return true;

                default:
                    return false;
            }
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

        public SunEyeVision.Interfaces.IImageProcessor CreateToolInstance(string toolId)
        {
            throw new NotImplementedException();
        }

        public SunEyeVision.Models.AlgorithmParameters GetDefaultParameters(string toolId)
        {
            return new SunEyeVision.Models.AlgorithmParameters();
        }

        public ValidationResult ValidateParameters(string toolId, SunEyeVision.Models.AlgorithmParameters parameters)
        {
            return ValidationResult.Success();
        }
    }
}
