using System;
using System.Collections.Generic;
using SunEyeVision.Models;

namespace SunEyeVision.PluginSystem.SampleTools
{
    /// <summary>
    /// 颜色空间转换工具插件示例
    /// </summary>
    [ToolPlugin("color_convert", "ColorConvert")]
    public class ColorConvertTool : IToolPlugin
    {
        public string Name => "颜色空间转换";
        public string Version => "1.0.0";
        public string Author => "SunEyeVision";
        public string Description => "转换图像颜色空间";
        public string PluginId => "suneye.color_convert";
        public List<string> Dependencies => new List<string>();
        public string Icon => "🎨";

        private bool _isLoaded;

        public bool IsLoaded => _isLoaded;

        public void Initialize()
        {
            _isLoaded = true;
        }

        public void Unload()
        {
            _isLoaded = false;
        }

        public List<Type> GetAlgorithmNodes()
        {
            return new List<Type>
            {
                typeof(ColorConvertAlgorithm)
            };
        }

        public List<ToolMetadata> GetToolMetadata()
        {
            return new List<ToolMetadata>
            {
                new ToolMetadata
                {
                    Id = "color_convert",
                    Name = "ColorConvert",
                    DisplayName = "颜色空间转换",
                    Icon = "🎨",
                    Category = "图像处理",
                    Description = "转换图像颜色空间",
                    AlgorithmType = typeof(ColorConvertAlgorithm),
                    Version = "1.0.0",
                    Author = "SunEyeVision",
                    HasDebugInterface = true,
                    InputParameters = new List<ParameterMetadata>
                    {
                        new ParameterMetadata
                        {
                            Name = "targetColorSpace",
                            DisplayName = "目标颜色空间",
                            Description = "要转换到的颜色空间",
                            Type = ParameterType.Enum,
                            DefaultValue = "GRAY",
                            Options = new object[] { "GRAY", "RGB", "HSV", "Lab", "XYZ", "YCrCb" },
                            Required = true,
                            Category = "基本参数"
                        },
                        new ParameterMetadata
                        {
                            Name = "sourceColorSpace",
                            DisplayName = "源颜色空间",
                            Description = "源图像的颜色空间",
                            Type = ParameterType.Enum,
                            DefaultValue = "BGR",
                            Options = new object[] { "BGR", "RGB", "GRAY", "HSV", "Lab" },
                            Required = false,
                            Category = "高级参数"
                        },
                        new ParameterMetadata
                        {
                            Name = "channels",
                            DisplayName = "输出通道数",
                            Description = "保留的通道数(仅对某些转换有效)",
                            Type = ParameterType.Int,
                            DefaultValue = 0,
                            MinValue = 0,
                            MaxValue = 4,
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
                            Description = "转换后的图像",
                            Type = ParameterType.Image
                        },
                        new ParameterMetadata
                        {
                            Name = "channelCount",
                            DisplayName = "通道数",
                            Description = "输出图像的通道数",
                            Type = ParameterType.Int
                        }
                    }
                }
            };
        }

        public SunEyeVision.Interfaces.IImageProcessor CreateToolInstance(string toolId)
        {
            return new ColorConvertAlgorithm();
        }

        public SunEyeVision.Models.AlgorithmParameters GetDefaultParameters(string toolId)
        {
            var parameters = new SunEyeVision.Models.AlgorithmParameters();
            parameters.Set("targetColorSpace", "GRAY");
            parameters.Set("sourceColorSpace", "BGR");
            parameters.Set("channels", 0);
            return parameters;
        }

        public ValidationResult ValidateParameters(string toolId, SunEyeVision.Models.AlgorithmParameters parameters)
        {
            var result = new ValidationResult();

            var channels = parameters.Get<int>("channels");
            if (channels != null && channels > 4)
            {
                result.AddError("通道数不能超过4");
            }

            result.IsValid = result.Errors.Count == 0;
            return result;
        }
    }

    /// <summary>
    /// 颜色空间转换算法实现（简化版，仅用于演示）
    /// </summary>
    public class ColorConvertAlgorithm : SunEyeVision.Interfaces.IImageProcessor
    {
        public string Name => "颜色空间转换";
        public string Description => "转换图像颜色空间";

        public object? Process(object image)
        {
            // 简化实现：仅返回处理时间
            return 2.3;
        }
    }
}
