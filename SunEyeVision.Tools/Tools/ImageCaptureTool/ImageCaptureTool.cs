using System;
using System.Collections.Generic;
using SunEyeVision.Models;
using SunEyeVision.PluginSystem.Base.Interfaces;
using SunEyeVision.PluginSystem.Base.Models;
using SunEyeVision.PluginSystem.Base.Base;

namespace SunEyeVision.Tools.ImageCaptureTool
{
    /// <summary>
    /// 图像采集工具插件示例
    /// </summary>
    [ToolPlugin("image_capture", "ImageCapture")]
    public class ImageCaptureTool : IToolPlugin
    {
        public string Name => "图像采集";
        public string Version => "1.0.0";
        public string Author => "SunEyeVision";
        public string Description => "从相机采集图像";
        public string PluginId => "suneye.image_capture";
        public List<string> Dependencies => new List<string>();
        public string Icon => "📷";

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
                typeof(ImageCaptureAlgorithm)
            };
        }

        public List<ToolMetadata> GetToolMetadata()
        {
            return new List<ToolMetadata>
            {
                new ToolMetadata
                {
                    Id = "image_capture",
                    Name = "ImageCapture",
                    DisplayName = "图像采集",
                    Icon = "📷",
                    Category = "采集",
                    Description = "从相机采集图像",
                    AlgorithmType = typeof(ImageCaptureAlgorithm),
                    Version = "1.0.0",
                    Author = "SunEyeVision",
                    HasDebugInterface = true,
                    NodeType = NodeType.Start,  // 改为Start类型
                    InputParameters = new List<ParameterMetadata>
                    {
                        new ParameterMetadata
                        {
                            Name = "cameraId",
                            DisplayName = "相机ID",
                            Description = "相机的设备ID",
                            Type = ParameterType.Int,
                            DefaultValue = 0,
                            MinValue = 0,
                            MaxValue = 10,
                            Required = true,
                            Category = "基本参数"
                        },
                        new ParameterMetadata
                        {
                            Name = "timeout",
                            DisplayName = "超时时间(ms)",
                            Description = "采集超时时间(毫秒)",
                            Type = ParameterType.Int,
                            DefaultValue = 5000,
                            MinValue = 100,
                            MaxValue = 60000,
                            Required = false,
                            Category = "高级参数"
                        },
                        new ParameterMetadata
                        {
                            Name = "triggerMode",
                            DisplayName = "触发模式",
                            Description = "相机触发模式",
                            Type = ParameterType.Enum,
                            DefaultValue = "Soft",
                            Options = new object[] { "Soft", "Hard", "Continuous" },
                            Required = true,
                            Category = "基本参数"
                        }
                    },
                    OutputParameters = new List<ParameterMetadata>
                    {
                        new ParameterMetadata
                        {
                            Name = "outputImage",
                            DisplayName = "输出图像",
                            Description = "采集到的图像",
                            Type = ParameterType.Image
                        },
                        new ParameterMetadata
                        {
                            Name = "timestamp",
                            DisplayName = "时间戳",
                            Description = "采集时间戳",
                            Type = ParameterType.Double
                        }
                    }
                }
            };
        }

        public SunEyeVision.Interfaces.IImageProcessor CreateToolInstance(string toolId)
        {
            return new ImageCaptureAlgorithm();
        }

        public SunEyeVision.Models.AlgorithmParameters GetDefaultParameters(string toolId)
        {
            var parameters = new SunEyeVision.Models.AlgorithmParameters();
            parameters.Set("cameraId", 0);
            parameters.Set("timeout", 5000);
            parameters.Set("triggerMode", "Soft");
            return parameters;
        }

        public ValidationResult ValidateParameters(string toolId, SunEyeVision.Models.AlgorithmParameters parameters)
        {
            var result = new ValidationResult();

            var cameraId = parameters.Get<int>("cameraId");
            if (cameraId == null || cameraId < 0)
            {
                result.AddError("相机ID必须大于等于0");
            }

            var timeout = parameters.Get<int>("timeout");
            if (timeout != null && timeout < 100)
            {
                result.AddWarning("超时时间过短，可能导致采集失败");
            }

            result.IsValid = result.Errors.Count == 0;
            return result;
        }
    }

    /// <summary>
    /// 图像采集算法实现（简化版，仅用于演示）
    /// </summary>
    public class ImageCaptureAlgorithm : SunEyeVision.Interfaces.IImageProcessor
    {
        public string Name => "图像采集";
        public string Description => "从相机采集图像";

        public object? Process(object image)
        {
            // 简化实现：仅返回时间戳
            return DateTime.Now.Ticks;
        }
    }
}
