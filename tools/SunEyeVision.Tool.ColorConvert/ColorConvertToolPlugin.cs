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
        private string _targetColorSpace = "GRAY";

        /// <summary>
        /// 目标颜色空间
        /// </summary>
        public string TargetColorSpace
        {
            get => _targetColorSpace;
            set => SetProperty(ref _targetColorSpace, value, "目标颜色空间");
        }

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
        public int InputChannels { get; set; }

        /// <summary>
        /// 获取结果项列表
        /// </summary>
        public override IReadOnlyList<ResultItem> GetResultItems()
        {
            var items = new List<ResultItem>();
            items.AddText("TargetColorSpaceUsed", TargetColorSpaceUsed);
            items.AddNumeric("InputChannels", InputChannels, "通道");
            items.AddNumeric("OutputChannels", OutputChannels, "通道");
            items.AddNumeric("ExecutionTimeMs", ExecutionTimeMs, "ms");
            return items;
        }
    }

    #endregion

    #region 工具实现

    [Tool("color_convert", "颜色空间转换", Description = "转换图像颜色空间", Icon = "🎨", Category = "图像处理", Version = "2.0.0", HasDebugWindow = true)]
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
                // 验证参数
                var validationResult = parameters.Validate();
                if (!validationResult.IsValid)
                {
                    parameters.LogError($"参数验证失败: {string.Join(", ", validationResult.Errors)}");
                    result.SetError($"参数验证失败: {string.Join(", ", validationResult.Errors)}");
                    return result;
                }

                // 验证输入图像
                if (image == null || image.Empty())
                {
                    parameters.LogWarning("输入图像为空");
                    result.SetError("输入图像为空");
                    return result;
                }

                parameters.LogInfo($"开始颜色空间转换: {image.Channels()}通道 → {parameters.TargetColorSpace}");

                var outputImage = new Mat();
                var colorCode = GetColorConversionCode(parameters.TargetColorSpace, image.Channels());
                Cv2.CvtColor(image, outputImage, colorCode);

                result.OutputImage = outputImage;
                result.TargetColorSpaceUsed = parameters.TargetColorSpace;
                result.InputChannels = image.Channels();
                result.OutputChannels = outputImage.Channels();
                result.SetSuccess(stopwatch.ElapsedMilliseconds);

                parameters.LogInfo($"颜色空间转换完成: 输出{result.OutputChannels}通道, 耗时{stopwatch.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                parameters.LogError($"颜色空间转换异常: {ex.Message}", ex);
                result.SetError($"处理失败: {ex.Message}");
                result.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;
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
