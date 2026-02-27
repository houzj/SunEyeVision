using System;
using System.Collections.Generic;
using OpenCvSharp;
using SunEyeVision.Plugin.SDK;
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.SDK.Execution.Results;
using SunEyeVision.Plugin.SDK.Metadata;
using SunEyeVision.Plugin.SDK.Validation;

namespace MyPlugin
{
    /// <summary>
    /// 阈值化工具插件示例 - 展示完整的插件开发流程
    /// </summary>
    /// <remarks>
    /// 这是 SunEyeVision 插件开发的完整示例，包含：
    /// 1. IToolPlugin 接口的完整实现
    /// 2. ITool&lt;TParams, TResult&gt; 强类型工具的实现
    /// 3. 参数定义、验证和处理逻辑
    /// </remarks>
    [ToolPlugin("myplugin-threshold", "Threshold", Version = "1.0.0", Category = "图像处理")]
    public class ThresholdToolPlugin : IToolPlugin
    {
        #region 插件基本信息

        public string Name => "Threshold";
        public string Version => "1.0.0";
        public string PluginId => "myplugin-threshold";
        public string Description => "图像二值化处理 - 将灰度图像转换为二值图像";
        public string Icon => "🔲";
        public string Author => "SunEyeVision Team";
        public List<string> Dependencies => new List<string>();
        public bool IsLoaded { get; private set; }

        #endregion

        #region 生命周期管理

        public void Initialize() => IsLoaded = true;
        public void Unload() => IsLoaded = false;

        #endregion

        #region 工具管理

        public List<ToolMetadata> GetToolMetadata()
        {
            return new List<ToolMetadata>
            {
                new ToolMetadata
                {
                    Id = PluginId,
                    Name = Name,
                    DisplayName = "阈值化处理",
                    Description = Description,
                    Icon = Icon,
                    Category = "图像处理",
                    Version = Version,
                    Author = Author,
                    InputParameters = new List<ParameterMetadata>
                    {
                        new ParameterMetadata
                        {
                            Name = "ThresholdValue",
                            DisplayName = "阈值",
                            Type = ParamDataType.Int,
                            DefaultValue = 128,
                            MinValue = 0,
                            MaxValue = 255
                        },
                        new ParameterMetadata
                        {
                            Name = "MaxValue",
                            DisplayName = "最大值",
                            Type = ParamDataType.Int,
                            DefaultValue = 255,
                            MinValue = 0,
                            MaxValue = 255
                        }
                    },
                    OutputParameters = new List<ParameterMetadata>
                    {
                        new ParameterMetadata
                        {
                            Name = "OutputImage",
                            DisplayName = "输出图像",
                            Type = ParamDataType.Image
                        }
                    }
                }
            };
        }

        public ITool? CreateToolInstance(string toolId)
        {
            return toolId == PluginId ? new ThresholdTool() : null;
        }

        public AlgorithmParameters GetDefaultParameters(string toolId)
        {
            if (toolId != PluginId) return new AlgorithmParameters();
            var parameters = new AlgorithmParameters();
            parameters.Set("ThresholdValue", 128);
            parameters.Set("MaxValue", 255);
            return parameters;
        }

        #endregion
    }

    #region 参数和结果定义

    public class ThresholdToolParameters : ToolParameters
    {
        public int ThresholdValue { get; set; } = 128;
        public int MaxValue { get; set; } = 255;

        public override ValidationResult Validate()
        {
            var result = new ValidationResult();
            if (ThresholdValue < 0 || ThresholdValue > 255)
                result.AddError($"阈值必须在 0-255 范围内，当前值: {ThresholdValue}");
            if (MaxValue < 0 || MaxValue > 255)
                result.AddError($"最大值必须在 0-255 范围内，当前值: {MaxValue}");
            return result;
        }
    }

    public class ThresholdToolResults : ToolResults
    {
        public Mat? OutputImage { get; set; }
        public int ProcessedPixels { get; set; }
    }

    #endregion

    #region 工具实现

    public class ThresholdTool : ITool<ThresholdToolParameters, ThresholdToolResults>
    {
        public string Name => "阈值化处理";
        public string Description => "将灰度图像转换为二值图像";
        public string Version => "1.0.0";
        public string Category => "图像处理";

        public ThresholdToolResults Execute(Mat image, ThresholdToolParameters parameters)
        {
            var result = new ThresholdToolResults();
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                var outputImage = new Mat();
                Cv2.Threshold(image, outputImage, parameters.ThresholdValue, parameters.MaxValue, ThresholdTypes.Binary);
                result.OutputImage = outputImage;
                result.ProcessedPixels = image.Rows * image.Cols;
                result.SetSuccess(stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                result.SetError($"处理失败: {ex.Message}");
            }

            return result;
        }

        public ValidationResult ValidateParameters(ThresholdToolParameters parameters) => parameters.Validate();
        public ThresholdToolParameters GetDefaultParameters() => new ThresholdToolParameters();
    }

    #endregion
}
