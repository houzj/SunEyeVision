using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using OpenCvSharp;
using SunEyeVision.Plugin.SDK;
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.SDK.Execution.Results;
using SunEyeVision.Plugin.SDK.Metadata;
using SunEyeVision.Plugin.SDK.Validation;

namespace SunEyeVision.Tool.ColorConvert
{
    /// <summary>
    /// 颜色空间转换工具插件
    /// </summary>
    [ToolPlugin("color_convert", "ColorConvert")]
    public class ColorConvertToolPlugin : IToolPlugin
    {
        #region 插件基本信息
        public string Name => "颜色空间转换";
        public string Version => "2.0.0";
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
        public List<Type> GetAlgorithmNodes() => new List<Type>();

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
                    Version = Version,
                    Author = Author,
                    InputParameters = new List<ParameterMetadata>
                    {
                        new ParameterMetadata
                        {
                            Name = "TargetColorSpace",
                            DisplayName = "目标颜色空间",
                            Description = "要转换到的颜色空间",
                            Type = ParamDataType.Enum,
                            DefaultValue = "GRAY",
                            Options = new object[] { "GRAY", "RGB", "HSV", "Lab", "XYZ", "YCrCb" },
                            Required = true,
                            Category = "基本参数"
                        }
                    },
                    OutputParameters = new List<ParameterMetadata>
                    {
                        new ParameterMetadata
                        {
                            Name = "OutputImage",
                            DisplayName = "输出图像",
                            Description = "转换后的图像",
                            Type = ParamDataType.Image
                        }
                    }
                }
            };
        }

        public ITool? CreateToolInstance(string toolId) => 
            toolId == "color_convert" ? new ColorConvertTool() : null;

        public AlgorithmParameters GetDefaultParameters(string toolId)
        {
            var parameters = new AlgorithmParameters();
            parameters.Set("TargetColorSpace", "GRAY");
            return parameters;
        }

        public ValidationResult ValidateParameters(string toolId, AlgorithmParameters parameters) => new ValidationResult();
        #endregion
    }

    #region 参数和结果定义

    public class ColorConvertParameters : ToolParameters
    {
        public string TargetColorSpace { get; set; } = "GRAY";

        public override ValidationResult Validate()
        {
            var result = new ValidationResult();
            var validSpaces = new[] { "GRAY", "RGB", "HSV", "Lab", "XYZ", "YCrCb" };
            if (Array.IndexOf(validSpaces, TargetColorSpace) < 0)
                result.AddError($"不支持的颜色空间: {TargetColorSpace}");
            return result;
        }
    }

    public class ColorConvertResults : ToolResults
    {
        public Mat? OutputImage { get; set; }
        public string TargetColorSpaceUsed { get; set; } = "";
        public int OutputChannels { get; set; }
    }

    #endregion

    #region 工具实现

    public class ColorConvertTool : ITool<ColorConvertParameters, ColorConvertResults>
    {
        public string Name => "颜色空间转换";
        public string Description => "转换图像颜色空间";
        public string Version => "2.0.0";
        public string Category => "图像处理";

        public ColorConvertResults Run(Mat image, ColorConvertParameters parameters)
        {
            var result = new ColorConvertResults();
            var stopwatch = Stopwatch.StartNew();

            try
            {
                if (image == null || image.Empty())
                {
                    result.SetError("输入图像为空");
                    return result;
                }

                var outputImage = new Mat();
                var colorCode = GetColorConversionCode(parameters.TargetColorSpace, image.Channels());
                Cv2.CvtColor(image, outputImage, colorCode);

                result.OutputImage = outputImage;
                result.TargetColorSpaceUsed = parameters.TargetColorSpace;
                result.OutputChannels = outputImage.Channels();
                result.SetSuccess(stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                result.SetError($"处理失败: {ex.Message}");
            }

            return result;
        }

        private ColorConversionCodes GetColorConversionCode(string targetSpace, int inputChannels)
        {
            return targetSpace.ToUpper() switch
            {
                "GRAY" => inputChannels == 4 ? ColorConversionCodes.BGRA2GRAY : ColorConversionCodes.BGR2GRAY,
                "RGB" => inputChannels == 4 ? ColorConversionCodes.BGRA2RGB : ColorConversionCodes.BGR2RGB,
                "HSV" => ColorConversionCodes.BGR2HSV,
                "LAB" => ColorConversionCodes.BGR2Lab,
                "XYZ" => ColorConversionCodes.BGR2XYZ,
                "YCRCB" => ColorConversionCodes.BGR2YCrCb,
                _ => ColorConversionCodes.BGR2GRAY
            };
        }

        public ValidationResult ValidateParameters(ColorConvertParameters parameters) => parameters.Validate();
        public ColorConvertParameters GetDefaultParameters() => new ColorConvertParameters();
    }

    #endregion
}
