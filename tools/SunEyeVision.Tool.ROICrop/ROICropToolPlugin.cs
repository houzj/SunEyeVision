using System;
using System.Collections.Generic;
using SunEyeVision.Core.Interfaces;
using SunEyeVision.Core.Models;
using SunEyeVision.Plugin.Abstractions;

namespace SunEyeVision.Tool.ROICrop
{
    /// <summary>
    /// ROI裁剪工具插件
    /// </summary>
    [ToolPlugin("roi_crop", "ROICrop")]
    public class ROICropToolPlugin : IToolPlugin
    {
        #region 插件基本信息
        public string Name => "ROI裁剪";
        public string Version => "1.0.0";
        public string Author => "SunEyeVision";
        public string Description => "裁剪指定矩形区域";
        public string PluginId => "suneye.roi_crop";
        public string Icon => "✂️";
        public List<string> Dependencies => new List<string>();
        public bool IsLoaded { get; private set; }
        #endregion

        #region 生命周期管理
        public void Initialize() => IsLoaded = true;
        public void Unload() => IsLoaded = false;
        #endregion

        #region 工具管理
        public List<Type> GetAlgorithmNodes() => new List<Type> { typeof(ROICropAlgorithm) };

        public List<ToolMetadata> GetToolMetadata()
        {
            return new List<ToolMetadata>
            {
                new ToolMetadata
                {
                    Id = "roi_crop",
                    Name = "ROICrop",
                    DisplayName = "ROI裁剪",
                    Icon = "✂️",
                    Category = "图像处理",
                    Description = "裁剪指定的矩形感兴趣区域",
                    AlgorithmType = typeof(ROICropAlgorithm),
                    Version = "1.0.0",
                    Author = "SunEyeVision",
                    HasDebugInterface = true,
                    InputParameters = new List<ParameterMetadata>
                    {
                        new ParameterMetadata
                        {
                            Name = "roi",
                            DisplayName = "ROI区域",
                            Description = "要裁剪的矩形区域",
                            Type = ParameterType.Rect,
                            Required = true,
                            Category = "基本参数",
                            EditableInDebug = true
                        },
                        new ParameterMetadata
                        {
                            Name = "padding",
                            DisplayName = "边距填充",
                            Description = "在ROI周围添加的边距",
                            Type = ParameterType.Int,
                            DefaultValue = 0,
                            MinValue = 0,
                            MaxValue = 100,
                            Required = false,
                            Category = "基本参数",
                            EditableInDebug = true
                        },
                        new ParameterMetadata
                        {
                            Name = "clipToImage",
                            DisplayName = "限制在图像内",
                            Description = "是否将ROI限制在图像范围内",
                            Type = ParameterType.Bool,
                            DefaultValue = true,
                            Required = false,
                            Category = "基本参数"
                        },
                        new ParameterMetadata
                        {
                            Name = "outputWidth",
                            DisplayName = "输出宽度",
                            Description = "输出图像宽度(0表示保持原尺寸)",
                            Type = ParameterType.Int,
                            DefaultValue = 0,
                            MinValue = 0,
                            MaxValue = 4096,
                            Required = false,
                            Category = "高级参数"
                        },
                        new ParameterMetadata
                        {
                            Name = "outputHeight",
                            DisplayName = "输出高度",
                            Description = "输出图像高度(0表示保持原尺寸)",
                            Type = ParameterType.Int,
                            DefaultValue = 0,
                            MinValue = 0,
                            MaxValue = 4096,
                            Required = false,
                            Category = "高级参数"
                        }
                    },
                    OutputParameters = new List<ParameterMetadata>
                    {
                        new ParameterMetadata
                        {
                            Name = "outputImage",
                            DisplayName = "输出图像",
                            Description = "裁剪后的图像",
                            Type = ParameterType.Image
                        },
                        new ParameterMetadata
                        {
                            Name = "croppedArea",
                            DisplayName = "实际裁剪区域",
                            Description = "实际裁剪的矩形区域",
                            Type = ParameterType.Rect
                        }
                    }
                }
            };
        }

        public IImageProcessor CreateToolInstance(string toolId) => new ROICropAlgorithm();

        public AlgorithmParameters GetDefaultParameters(string toolId)
        {
            var parameters = new AlgorithmParameters();
            parameters.Set("roi", "0,0,100,100");
            parameters.Set("padding", 0);
            parameters.Set("clipToImage", true);
            parameters.Set("outputWidth", 0);
            parameters.Set("outputHeight", 0);
            return parameters;
        }

        public ValidationResult ValidateParameters(string toolId, AlgorithmParameters parameters)
        {
            var result = new ValidationResult();

            var padding = parameters.Get<int>("padding");
            if (padding != null && padding < 0)
            {
                result.AddError("边距填充不能为负数");
            }

            result.IsValid = result.Errors.Count == 0;
            return result;
        }
        #endregion
    }

    /// <summary>
    /// ROI裁剪算法实现（简化版）
    /// </summary>
    public class ROICropAlgorithm : IImageProcessor
    {
        public string Name => "ROI裁剪";
        public string Description => "裁剪指定的矩形感兴趣区域";

        public object? Process(object image)
        {
            // 简化实现：仅返回处理时间
            return 3.5;
        }
    }
}
