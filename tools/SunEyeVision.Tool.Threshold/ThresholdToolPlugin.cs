using System;
using System.Collections.Generic;
using SunEyeVision.Plugin.SDK;
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.SDK.Metadata;
using SunEyeVision.Plugin.SDK.Validation;

namespace SunEyeVision.Tool.Threshold
{
    /// <summary>
    /// 图像阈值化工具插件
    /// </summary>
    [ToolPlugin("threshold", "Threshold")]
    public class ThresholdToolPlugin : IToolPlugin
    {
        #region 插件基本信息
        public string Name => "图像阈值化";
        public string Version => "1.0.0";
        public string Author => "SunEyeVision";
        public string Description => "将灰度图像转换为二值图像";
        public string PluginId => "suneye.threshold";
        public string Icon => "📷";
        public List<string> Dependencies => new List<string>();
        public bool IsLoaded { get; private set; }
        #endregion

        #region 生命周期管理
        public void Initialize() => IsLoaded = true;
        public void Unload() => IsLoaded = false;
        #endregion

        #region 工具管理
        public List<Type> GetAlgorithmNodes() => new List<Type> { typeof(ThresholdAlgorithm) };

        public List<ToolMetadata> GetToolMetadata()
        {
            return new List<ToolMetadata>
            {
                new ToolMetadata
                {
                    Id = "threshold",
                    Name = "Threshold",
                    DisplayName = "图像阈值化",
                    Icon = "📷",
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

        public IImageProcessor CreateToolInstance(string toolId) => new ThresholdAlgorithm();

        public AlgorithmParameters GetDefaultParameters(string toolId)
        {
            var parameters = new AlgorithmParameters();
            parameters.Set("threshold", 128);
            parameters.Set("maxValue", 255);
            parameters.Set("type", "Binary");
            parameters.Set("adaptiveMethod", "Mean");
            parameters.Set("blockSize", 11);
            parameters.Set("invert", false);
            return parameters;
        }

        public ValidationResult ValidateParameters(string toolId, AlgorithmParameters parameters)
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
        #endregion
    }

    /// <summary>
    /// 阈值化算法实现
    /// </summary>
    public class ThresholdAlgorithm : ImageProcessorBase
    {
        public override string Name => "图像阈值化";
        public override string Description => "将灰度图像转换为二值图像";

        protected override ImageProcessResult ProcessImage(object image, AlgorithmParameters parameters)
        {
            var threshold = GetParameter(parameters, "threshold", 128);
            var maxValue = GetParameter(parameters, "maxValue", 255);
            var type = GetParameter(parameters, "type", "Binary");
            var adaptiveMethod = GetParameter(parameters, "adaptiveMethod", "Mean");
            var blockSize = GetParameter(parameters, "blockSize", 11);
            var invert = GetParameter(parameters, "invert", false);

            // TODO: 实际图像处理逻辑

            return ImageProcessResult.FromData(new
            {
                ThresholdUsed = threshold,
                MaxValue = maxValue,
                Type = type,
                AdaptiveMethod = adaptiveMethod,
                BlockSize = blockSize,
                Invert = invert,
                ProcessedAt = System.DateTime.Now
            });
        }

        protected override ValidationResult ValidateParameters(AlgorithmParameters parameters)
        {
            var result = new ValidationResult();
            var threshold = GetParameter<int?>(parameters, "threshold", null);
            var maxValue = GetParameter<int?>(parameters, "maxValue", null);
            var blockSize = GetParameter<int?>(parameters, "blockSize", null);

            if (threshold.HasValue && (threshold.Value < 0 || threshold.Value > 255))
                result.AddError("阈值必须在0-255之间");
            if (maxValue.HasValue && (maxValue.Value < 0 || maxValue.Value > 255))
                result.AddError("最大值必须在0-255之间");
            if (blockSize.HasValue && (blockSize.Value < 3 || blockSize.Value > 31 || blockSize.Value % 2 == 0))
                result.AddError("块大小必须在3-31之间且为奇数");

            return result;
        }
    }
}
