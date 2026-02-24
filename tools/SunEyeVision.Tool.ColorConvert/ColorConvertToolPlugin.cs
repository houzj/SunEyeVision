using System;
using System.Collections.Generic;
using SunEyeVision.Plugin.Abstractions;
using SunEyeVision.Plugin.Abstractions.Core;

namespace SunEyeVision.Tool.ColorConvert
{
    /// <summary>
    /// 颜色空间转换工具插件 - 独立插件项目
    /// </summary>
    [ToolPlugin("color_convert", "ColorConvert")]
    public class ColorConvertToolPlugin : IToolPlugin
    {
        #region 插件基本信息
        public string Name => "颜色空间转换";
        public string Version => "1.0.0";
        public string Author => "SunEyeVision";
        public string Description => "转换图像颜色空间";
        public string PluginId => "suneye.color_convert";
        public string Icon => "🎨";
        public List<string> Dependencies => new List<string>();
        public bool IsLoaded { get; private set; }
        #endregion

        #region 生命周期管理
        public void Initialize() => IsLoaded = true;
        public void Unload() => IsLoaded = false;
        #endregion

        #region 工具管理
        public List<Type> GetAlgorithmNodes() => new List<Type> { typeof(ColorConvertAlgorithm) };

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

        public IImageProcessor CreateToolInstance(string toolId) => new ColorConvertAlgorithm();

        public AlgorithmParameters GetDefaultParameters(string toolId)
        {
            var parameters = new AlgorithmParameters();
            parameters.Set("targetColorSpace", "GRAY");
            parameters.Set("sourceColorSpace", "BGR");
            parameters.Set("channels", 0);
            return parameters;
        }

        public ValidationResult ValidateParameters(string toolId, AlgorithmParameters parameters)
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
        #endregion
    }

    /// <summary>
    /// 颜色空间转换算法实现
    /// </summary>
    public class ColorConvertAlgorithm : ImageProcessorBase
    {
        public override string Name => "颜色空间转换";
        public override string Description => "转换图像颜色空间";

        protected override ImageProcessResult ProcessImage(object image, AlgorithmParameters parameters)
        {
            // 获取参数
            var targetColorSpace = GetParameter(parameters, "targetColorSpace", "GRAY");
            var sourceColorSpace = GetParameter(parameters, "sourceColorSpace", "BGR");
            var channels = GetParameter(parameters, "channels", 0);

            // TODO: 实际图像处理逻辑
            // 这里应使用 OpenCV 或其他图像处理库进行实际处理
            // 示例：Cv2.CvtColor(mat, output, GetColorConversionCode(sourceColorSpace, targetColorSpace))

            // 返回处理结果（简化示例）
            return ImageProcessResult.FromData(new
            {
                TargetColorSpace = targetColorSpace,
                SourceColorSpace = sourceColorSpace,
                Channels = channels,
                ProcessedAt = System.DateTime.Now
            });
        }

        protected override ValidationResult ValidateParameters(AlgorithmParameters parameters)
        {
            var result = new ValidationResult();
            
            var channels = GetParameter<int?>(parameters, "channels", null);
            if (channels.HasValue && channels.Value > 4)
                result.AddError("通道数不能超过4");

            return result;
        }
    }
}
