using System;
using System.Collections.Generic;
using System.Windows;
using SunEyeVision.Plugin.Infrastructure;
using SunEyeVision.Plugin.Abstractions;
using SunEyeVision.Tools.ImageSaveTool.Views;
using SunEyeVision.Tools.ColorConvertTool.Views;
using SunEyeVision.Tools.EdgeDetectionTool.Views;
using SunEyeVision.Tools.GaussianBlurTool.Views;
using SunEyeVision.Tools.ImageCaptureTool.Views;
using SunEyeVision.Tools.OCRTool.Views;
using SunEyeVision.Tools.ROICropTool.Views;
using SunEyeVision.Tools.TemplateMatchingTool.Views;
using SunEyeVision.Tools.ThresholdTool.Views;

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
                case "image_save":
                    return new ImageSaveToolDebugWindow(toolId, toolPlugin, toolMetadata);
                case "ColorConvertTool":
                case "color_convert":
                    return new ColorConvertToolDebugWindow(toolId, toolPlugin, toolMetadata);
                case "EdgeDetectionTool":
                case "edge_detection":
                    return new EdgeDetectionToolDebugWindow(toolId, toolPlugin, toolMetadata);
                case "GaussianBlurTool":
                case "gaussian_blur":
                    return new GaussianBlurToolDebugWindow(toolId, toolPlugin, toolMetadata);
                case "ImageCaptureTool":
                case "image_capture":
                    return new ImageCaptureToolDebugWindow(toolId, toolPlugin, toolMetadata);
                case "OCRTool":
                case "ocr_recognition":
                    return new OCRToolDebugWindow(toolId, toolPlugin, toolMetadata);
                case "ROICropTool":
                case "roi_crop":
                    return new ROICropToolDebugWindow(toolId, toolPlugin, toolMetadata);
                case "TemplateMatchingTool":
                case "template_matching":
                    return new TemplateMatchingToolDebugWindow(toolId, toolPlugin, toolMetadata);
                case "ThresholdTool":
                case "threshold":
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
                case "image_save":
                case "ColorConvertTool":
                case "color_convert":
                case "EdgeDetectionTool":
                case "edge_detection":
                case "GaussianBlurTool":
                case "gaussian_blur":
                case "ImageCaptureTool":
                case "image_capture":
                case "OCRTool":
                case "ocr_recognition":
                case "ROICropTool":
                case "roi_crop":
                case "TemplateMatchingTool":
                case "template_matching":
                case "ThresholdTool":
                case "threshold":
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
