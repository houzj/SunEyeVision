using System;
using System.Collections.Generic;
using SunEyeVision.Core.Models;
using SunEyeVision.Plugin.Abstractions;
using SunEyeVision.Plugin.Abstractions;
using SunEyeVision.Plugin.Infrastructure;

namespace SunEyeVision.Tools.ThresholdTool
{
    /// <summary>
    /// 图像阈值化工具插件示例
    /// </summary>
    [ToolPlugin("threshold", "Threshold")]
    public class ThresholdTool : IToolPlugin
    {
        public string Name => "图像阈值化";
        public string Version => "1.0.0";
        public string Author => "SunEyeVision";
        public string Description => "将灰度图像转换为二值图像";
        public string PluginId => "suneye.threshold";
        public List<string> Dependencies => new List<string>();
        public string Icon => "🎚️";

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
                typeof(ThresholdAlgorithm)
            };
        }

        public List<ToolMetadata> GetToolMetadata()
        {
            return new List<ToolMetadata>
            {
                new ToolMetadata
                {
                    Id = "threshold",
                    Name = "Threshold",
                    DisplayName = "图像阈值化",
                    Icon = "🎚️",
                    Category = "图像处理",
                    Description = "将灰度图像转换为二值图像",
                    AlgorithmType = typeof(ThresholdAlgorithm),
                    Version = "1.0.0",
                    Author = "SunEyeVision",
                    HasDebugInterface = true,
                    InputParameters = new List<ParameterMetadata>
                    {
                        new ParameterMetadata
                        {
                            Name = "threshold",
                            DisplayName = "阈值",
                            Description = "二值化的阈值(0-255)",
                            Type = ParameterType.Int,
                            DefaultValue = 128,
                            MinValue = 0,
                            MaxValue = 255,
                            Required = true,
                            Category = "基本参数",
                            EditableInDebug = true
                        },
                        new ParameterMetadata
                        {
                            Name = "maxValue",
                            DisplayName = "最大值",
                            Description = "超过阈值时使用的最大值(0-255)",
                            Type = ParameterType.Int,
                            DefaultValue = 255,
                            MinValue = 0,
                            MaxValue = 255,
                            Required = true,
                            Category = "基本参数",
                            EditableInDebug = true
                        },
                        new ParameterMetadata
                        {
                            Name = "type",
                            DisplayName = "阈值类型",
                            Description = "二值化方法",
                            Type = ParameterType.Enum,
                            DefaultValue = "Binary",
                            Options = new object[] { "Binary", "BinaryInv", "Trunc", "ToZero", "ToZeroInv" },
                            Required = true,
                            Category = "基本参数"
                        },
                        new ParameterMetadata
                        {
                            Name = "adaptiveMethod",
                            DisplayName = "自适应方法",
                            Description = "自适应阈值方法",
                            Type = ParameterType.Enum,
                            DefaultValue = "Mean",
                            Options = new object[] { "Mean", "Gaussian" },
                            Required = false,
                            Category = "高级参数"
                        },
                        new ParameterMetadata
                        {
                            Name = "blockSize",
                            DisplayName = "块大小",
                            Description = "计算阈值的邻域大小(奇数)",
                            Type = ParameterType.Int,
                            DefaultValue = 11,
                            MinValue = 3,
                            MaxValue = 31,
                            Required = false,
                            Category = "高级参数"
                        },
                        new ParameterMetadata
                        {
                            Name = "invert",
                            DisplayName = "反转结果",
                            Description = "是否反转二值化结果",
                            Type = ParameterType.Bool,
                            DefaultValue = false,
                            Required = false,
                            Category = "基本参数",
                            EditableInDebug = true
                        }
                    },
                    OutputParameters = new List<ParameterMetadata>
                    {
                        new ParameterMetadata
                        {
                            Name = "outputImage",
                            DisplayName = "输出图像",
                            Description = "二值化后的图像",
                            Type = ParameterType.Image
                        },
                        new ParameterMetadata
                        {
                            Name = "thresholdUsed",
                            DisplayName = "实际阈值",
                            Description = "实际使用的阈值",
                            Type = ParameterType.Double
                        }
                    }
                }
            };
        }

        public SunEyeVision.Core.Interfaces.IImageProcessor CreateToolInstance(string toolId)
        {
            return new ThresholdAlgorithm();
        }

        public SunEyeVision.Core.Models.AlgorithmParameters GetDefaultParameters(string toolId)
        {
            var parameters = new SunEyeVision.Core.Models.AlgorithmParameters();
            parameters.Set("threshold", 128);
            parameters.Set("maxValue", 255);
            parameters.Set("type", "Binary");
            parameters.Set("adaptiveMethod", "Mean");
            parameters.Set("blockSize", 11);
            parameters.Set("invert", false);
            return parameters;
        }

        public ValidationResult ValidateParameters(string toolId, SunEyeVision.Core.Models.AlgorithmParameters parameters)
        {
            var result = new ValidationResult();

            var threshold = parameters.Get<int>("threshold");
            if (threshold == null || threshold < 0 || threshold > 255)
            {
                result.AddError("阈值必须在0-255之间");
            }

            var maxValue = parameters.Get<int>("maxValue");
            if (maxValue == null || maxValue < 0 || maxValue > 255)
            {
                result.AddError("最大值必须在0-255之间");
            }

            var blockSize = parameters.Get<int>("blockSize");
            if (blockSize != null && (blockSize < 3 || blockSize > 31 || blockSize % 2 == 0))
            {
                result.AddError("块大小必须在3-31之间且为奇数");
            }

            result.IsValid = result.Errors.Count == 0;
            return result;
        }
    }

    /// <summary>
    /// 阈值化算法实现（简化版，仅用于演示）
    /// </summary>
    public class ThresholdAlgorithm : SunEyeVision.Core.Interfaces.IImageProcessor
    {
        public string Name => "图像阈值化";
        public string Description => "将灰度图像转换为二值图像";

        public object? Process(object image)
        {
            // 简化实现：仅返回处理时间
            return 5.2;
        }
    }
}
