using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using OpenCvSharp;
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.SDK.Validation;
using SunEyeVision.Tool.Threshold.Views;

namespace SunEyeVision.Tool.Threshold
{
    /// <summary>
    /// 图像阈值化工具 - 使用 [Tool] 特性作为元数据单一数据源
    /// </summary>
    [Tool(
        id: "threshold",
        displayName: "图像阈值化",
        Description = "将灰度图像转换为二值图像",
        Icon = "📷",
        Category = "图像处理",
        Version = "2.0.0",
        HasDebugWindow = true
    )]
    public class ThresholdTool : IToolPlugin<ThresholdParameters, ThresholdResults>
    {
        private readonly ToolAttribute? _attribute;

        public ThresholdTool()
        {
            _attribute = GetType().GetCustomAttribute<ToolAttribute>();
        }

        /// <summary>
        /// 执行工具（同步）
        /// </summary>
        public ThresholdResults Run(Mat image, ThresholdParameters parameters)
        {
            var result = new ThresholdResults
            {
                ToolName = _attribute?.DisplayName ?? "Threshold",
                ToolId = _attribute?.Id ?? "threshold",
                Timestamp = DateTime.Now
            };

            var stopwatch = Stopwatch.StartNew();

            try
            {
                // 验证参数
                var validationResult = ValidateParameters(parameters);
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

                parameters.LogInfo($"开始阈值化处理: 图像尺寸 {image.Width}x{image.Height}");

                // 记录输入尺寸
                result.InputSize = image.Size();

                // 获取参数值
                double threshold = parameters.Threshold;
                int maxValue = parameters.MaxValue;
                ThresholdType type = parameters.Type;
                AdaptiveMethod adaptiveMethod = parameters.AdaptiveMethod;
                int blockSize = parameters.BlockSize;
                bool invert = parameters.Invert;

                // 执行阈值化处理
                Mat outputImage;
                double actualThreshold;

                // 确保输入是灰度图
                Mat grayImage = image;
                if (image.Channels() > 1)
                {
                    grayImage = new Mat();
                    Cv2.CvtColor(image, grayImage, ColorConversionCodes.BGR2GRAY);
                    parameters.LogInfo($"已将图像转换为灰度，原图通道数: {image.Channels()}");
                }

                // 转换OpenCvSharp的阈值类型
                var thresholdType = ConvertThresholdType(type);

                if (type == ThresholdType.Binary || type == ThresholdType.BinaryInv)
                {
                    // 普通阈值化
                    outputImage = new Mat();
                    actualThreshold = Cv2.Threshold(grayImage, outputImage, threshold, maxValue, thresholdType);
                    parameters.LogInfo($"使用固定阈值: {actualThreshold}");
                }
                else
                {
                    // 自适应阈值化
                    outputImage = new Mat();
                    var adaptiveType = adaptiveMethod == AdaptiveMethod.Mean
                        ? AdaptiveThresholdTypes.MeanC
                        : AdaptiveThresholdTypes.GaussianC;

                    Cv2.AdaptiveThreshold(grayImage, outputImage, maxValue, adaptiveType, thresholdType, blockSize, 5);
                    actualThreshold = threshold; // 自适应阈值无法直接获取
                    parameters.LogInfo($"使用自适应阈值，块大小: {blockSize}");
                }

                // 如果需要反转
                if (invert && outputImage != null)
                {
                    var inverted = new Mat();
                    Cv2.BitwiseNot(outputImage, inverted);
                    outputImage.Dispose();
                    outputImage = inverted;
                    parameters.LogInfo("已反转输出图像");
                }

                // 设置结果
                result.OutputImage = outputImage;
                result.ThresholdUsed = actualThreshold;
                result.MaxValueUsed = maxValue;
                result.TypeUsed = type;
                result.AdaptiveMethodUsed = adaptiveMethod;
                result.BlockSizeUsed = blockSize;
                result.InvertUsed = invert;
                result.ProcessedAt = DateTime.Now;

                stopwatch.Stop();
                result.SetSuccess(stopwatch.ElapsedMilliseconds);

                parameters.LogInfo($"阈值化处理完成: 使用阈值 {actualThreshold}, " +
                                  $"类型 {type}, 耗时 {stopwatch.ElapsedMilliseconds}ms");

                // 释放临时灰度图
                if (grayImage != image)
                {
                    grayImage.Dispose();
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                parameters.LogError($"阈值化处理异常: {ex.Message}", ex);
                result.SetError($"处理失败: {ex.Message}", ex);
                result.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;
            }

            return result;
        }

        /// <summary>
        /// 验证参数
        /// </summary>
        public ValidationResult ValidateParameters(ThresholdParameters parameters)
        {
            if (parameters == null)
            {
                return ValidationResult.Failure("参数不能为空");
            }
            return parameters.Validate();
        }

        /// <summary>
        /// 创建调试控件
        /// </summary>
        public FrameworkElement? CreateDebugControl()
        {
            return new ThresholdToolDebugControl();
        }

        #region 私有方法

        /// <summary>
        /// 转换阈值类型到OpenCvSharp类型
        /// </summary>
        private ThresholdTypes ConvertThresholdType(ThresholdType type)
        {
            return type switch
            {
                ThresholdType.Binary => ThresholdTypes.Binary,
                ThresholdType.BinaryInv => ThresholdTypes.BinaryInv,
                ThresholdType.Trunc => ThresholdTypes.Trunc,
                ThresholdType.ToZero => ThresholdTypes.Tozero,
                ThresholdType.ToZeroInv => ThresholdTypes.TozeroInv,
                _ => ThresholdTypes.Binary
            };
        }

        #endregion
    }
}
