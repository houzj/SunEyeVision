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
using SunEyeVision.Tool.ColorConvert.Views;
using System.Text.Json.Serialization;

namespace SunEyeVision.Tool.ColorConvert
{
    #region 参数和结果定义

    /// <summary>
    /// 颜色空间转换参数
    /// </summary>
    /// <remarks>
    /// 多态序列化（rule-010: 方案系统实现规范）：
    /// 使用 [JsonDerivedType] 特性标识参数类型，类型标识符为 "ColorConvert"。
    /// </remarks>
    [JsonDerivedType(typeof(ColorConvertParameters), "ColorConvert")]
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
        [Param(DisplayName = "输出图像", Description = "颜色空间转换后的图像", Category = ParamCategory.Output)]
        public Mat? OutputImage { get; set; }
        public string TargetColorSpaceUsed { get; set; } = "";
        public int OutputChannels { get; set; }
    }

    #endregion

    #region 工具实现

    [Tool("color_convert", "颜色空间转换", Description = "转换图像颜色空间", Icon = "🎨", Category = "图像处理")]
    public class ColorConvertTool : IToolPlugin<ColorConvertParameters, ColorConvertResults>
    {
        public bool HasDebugWindow => true;

        public System.Windows.Window? CreateDebugWindow()
        {
            return new ColorConvertToolDebugWindow();
        }

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
    }

    #endregion
}
