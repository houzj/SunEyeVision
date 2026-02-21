using System;
using System.Collections.Generic;
using SunEyeVision.Core.Models;
using SunEyeVision.Plugin.Abstractions;
using SunEyeVision.Plugin.Abstractions;
using SunEyeVision.Plugin.Infrastructure;

namespace SunEyeVision.Tools.EdgeDetectionTool
{
    /// <summary>
    /// 边缘检测工具插件示例
    /// </summary>
    [ToolPlugin("edge_detection", "EdgeDetection")]
    public class EdgeDetectionTool : IToolPlugin
    {
        public string Name => "边缘检测";
        public string Version => "1.0.0";
        public string Author => "SunEyeVision";
        public string Description => "检测图像中的边缘";
        public string PluginId => "suneye.edge_detection";
        public List<string> Dependencies => new List<string>();
        public string Icon => "📐";

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
                typeof(EdgeDetectionAlgorithm)
            };
        }

        public List<ToolMetadata> GetToolMetadata()
        {
            return new List<ToolMetadata>
            {
                new ToolMetadata
                {
                    Id = "edge_detection",
                    Name = "EdgeDetection",
                    DisplayName = "边缘检测",
                    Icon = "📐",
                    Category = "图像处理",
                    Description = "检测图像中的边缘",
                    AlgorithmType = typeof(EdgeDetectionAlgorithm),
                    Version = "1.0.0",
                    Author = "SunEyeVision",
                    HasDebugInterface = true,
                    InputParameters = new List<ParameterMetadata>
                    {
                        new ParameterMetadata
                        {
                            Name = "method",
                            DisplayName = "检测方法",
                            Description = "边缘检测算法",
                            Type = ParameterType.Enum,
                            DefaultValue = "Canny",
                            Options = new object[] { "Canny", "Sobel", "Laplacian", "Scharr" },
                            Required = true,
                            Category = "基本参数"
                        },
                        new ParameterMetadata
                        {
                            Name = "threshold1",
                            DisplayName = "低阈值",
                            Description = "第一个滞后阈值",
                            Type = ParameterType.Double,
                            DefaultValue = 50.0,
                            MinValue = 0.0,
                            MaxValue = 255.0,
                            Required = true,
                            Category = "基本参数",
                            EditableInDebug = true
                        },
                        new ParameterMetadata
                        {
                            Name = "threshold2",
                            DisplayName = "高阈值",
                            Description = "第二个滞后阈值",
                            Type = ParameterType.Double,
                            DefaultValue = 150.0,
                            MinValue = 0.0,
                            MaxValue = 255.0,
                            Required = true,
                            Category = "基本参数",
                            EditableInDebug = true
                        },
                        new ParameterMetadata
                        {
                            Name = "apertureSize",
                            DisplayName = "孔径大小",
                            Description = "Sobel算子的孔径大小",
                            Type = ParameterType.Int,
                            DefaultValue = 3,
                            MinValue = 1,
                            MaxValue = 7,
                            Required = false,
                            Category = "高级参数"
                        },
                        new ParameterMetadata
                        {
                            Name = "L2gradient",
                            DisplayName = "L2梯度",
                            Description = "是否使用更精确的L2范数计算梯度幅值",
                            Type = ParameterType.Bool,
                            DefaultValue = true,
                            Required = false,
                            Category = "高级参数"
                        },
                        new ParameterMetadata
                        {
                            Name = "kernelSize",
                            DisplayName = "卷积核大小",
                            Description = "Laplacian算子的孔径大小",
                            Type = ParameterType.Int,
                            DefaultValue = 3,
                            MinValue = 1,
                            MaxValue = 5,
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
                            Description = "边缘检测结果图像",
                            Type = ParameterType.Image
                        },
                        new ParameterMetadata
                        {
                            Name = "edgeCount",
                            DisplayName = "边缘数量",
                            Description = "检测到的边缘轮廓数量",
                            Type = ParameterType.Int
                        }
                    }
                }
            };
        }

        public SunEyeVision.Core.Interfaces.IImageProcessor CreateToolInstance(string toolId)
        {
            return new EdgeDetectionAlgorithm();
        }

        public SunEyeVision.Core.Models.AlgorithmParameters GetDefaultParameters(string toolId)
        {
            var parameters = new SunEyeVision.Core.Models.AlgorithmParameters();
            parameters.Set("method", "Canny");
            parameters.Set("threshold1", 50.0);
            parameters.Set("threshold2", 150.0);
            parameters.Set("apertureSize", 3);
            parameters.Set("L2gradient", true);
            parameters.Set("kernelSize", 3);
            return parameters;
        }

        public ValidationResult ValidateParameters(string toolId, SunEyeVision.Core.Models.AlgorithmParameters parameters)
        {
            var result = new ValidationResult();

            var threshold1 = parameters.Get<double>("threshold1");
            var threshold2 = parameters.Get<double>("threshold2");

            if (threshold1 == null || threshold1 < 0 || threshold1 > 255)
            {
                result.AddError("低阈值必须在0-255之间");
            }

            if (threshold2 == null || threshold2 < 0 || threshold2 > 255)
            {
                result.AddError("高阈值必须在0-255之间");
            }

            if (threshold1 != null && threshold2 != null && threshold1 >= threshold2)
            {
                result.AddWarning("通常情况下低阈值应小于高阈值");
            }

            result.IsValid = result.Errors.Count == 0;
            return result;
        }
    }

    /// <summary>
    /// 边缘检测算法实现（简化版，仅用于演示）
    /// </summary>
    public class EdgeDetectionAlgorithm : SunEyeVision.Core.Interfaces.IImageProcessor
    {
        public string Name => "边缘检测";
        public string Description => "检测图像中的边缘";

        public object? Process(object image)
        {
            // 简化实现：仅返回处理时间
            return 12.8;
        }
    }
}
