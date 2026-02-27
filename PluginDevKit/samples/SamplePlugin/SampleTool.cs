using System;
using System.Threading.Tasks;
using OpenCvSharp;
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.SDK.Execution.Results;
using SunEyeVision.Plugin.SDK.Validation;

namespace SamplePlugin;

/// <summary>
/// 示例工具参数
/// </summary>
public class SampleToolParameters : ToolParameters
{
    /// <summary>
    /// 阈值
    /// </summary>
    [ParameterRange(0, 255, Step = 1)]
    [ParameterDisplay(DisplayName = "阈值", Description = "二值化阈值", Group = "基本参数", Order = 1)]
    public double Threshold { get; set; } = 128;

    /// <summary>
    /// 是否启用模糊
    /// </summary>
    [ParameterDisplay(DisplayName = "启用模糊", Description = "是否在阈值化前应用高斯模糊", Group = "高级参数", Order = 2)]
    public bool EnableBlur { get; set; } = true;

    /// <summary>
    /// 模糊核大小
    /// </summary>
    [ParameterRange(1, 31, Step = 2)]
    [ParameterDisplay(DisplayName = "模糊核大小", Description = "高斯模糊核大小", Group = "高级参数", Order = 3)]
    public int BlurKernelSize { get; set; } = 5;

    /// <summary>
    /// 验证参数
    /// </summary>
    public override ValidationResult Validate()
    {
        var result = base.Validate();

        if (EnableBlur && BlurKernelSize % 2 == 0)
        {
            result.AddError("模糊核大小必须为奇数");
        }

        return result;
    }
}

/// <summary>
/// 示例工具结果
/// </summary>
public class SampleToolResult : ToolResults
{
    /// <summary>
    /// 处理后的图像
    /// </summary>
    public Mat? ProcessedImage { get; set; }

    /// <summary>
    /// 处理的像素数量
    /// </summary>
    public int ProcessedPixels { get; set; }

    /// <summary>
    /// 白色像素数量
    /// </summary>
    public int WhitePixelCount { get; set; }
}

/// <summary>
/// 示例工具实现
/// </summary>
public class SampleTool : ITool<SampleToolParameters, SampleToolResult>
{
    public string Name => "SampleTool";
    public string Description => "一个简单的示例工具，执行二值化处理";
    public string Version => "1.0.0";
    public string Category => "示例";

    public SampleToolResult Execute(Mat image, SampleToolParameters parameters)
    {
        var result = new SampleToolResult
        {
            Success = true,
            ProcessedPixels = image.Width * image.Height
        };

        try
        {
            using var processed = image.Clone();

            // 如果启用模糊，先进行高斯模糊
            if (parameters.EnableBlur && parameters.BlurKernelSize > 0)
            {
                var kernelSize = parameters.BlurKernelSize % 2 == 0 
                    ? parameters.BlurKernelSize + 1 
                    : parameters.BlurKernelSize;
                Cv2.GaussianBlur(processed, processed, new Size(kernelSize, kernelSize), 0);
            }

            // 执行二值化
            Cv2.Threshold(processed, processed, parameters.Threshold, 255, ThresholdTypes.Binary);

            // 计算白色像素数量
            result.WhitePixelCount = Cv2.CountNonZero(processed);
            result.ProcessedImage = processed.Clone();

            result.Message = $"处理完成，白色像素: {result.WhitePixelCount}";
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"处理失败: {ex.Message}";
            result.ErrorMessages.Add(ex.Message);
        }

        return result;
    }

    public Task<SampleToolResult> ExecuteAsync(Mat image, SampleToolParameters parameters)
    {
        return Task.Run(() => Execute(image, parameters));
    }

    public ValidationResult ValidateParameters(SampleToolParameters parameters)
    {
        if (parameters == null)
        {
            return ValidationResult.Failure("参数不能为空");
        }
        return parameters.Validate();
    }

    public SampleToolParameters GetDefaultParameters()
    {
        return new SampleToolParameters();
    }
}
