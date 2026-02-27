using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using OpenCvSharp;
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.SDK.Validation;
using SunEyeVision.Tool.Threshold.Views;

namespace SunEyeVision.Tool.Threshold
{
    /// <summary>
    /// 图像阈值化工具 - 实现ITool接口
    /// </summary>
    public class ThresholdTool : ITool<ThresholdParameters, ThresholdResults>
    {
        #region ITool 基本信息

        public string Name => "Threshold";
        public string Description => "将灰度图像转换为二值图像";
        public string Version => "2.0.0";
        public string Category => "图像处理";

        #endregion

        #region ITool<ThresholdParameters, ThresholdResults> 实现

        /// <summary>
        /// 执行工具（同步）
        /// </summary>
        public ThresholdResults Run(Mat image, ThresholdParameters parameters)
        {
            var result = new ThresholdResults
            {
                ToolName = Name,
                ToolId = "threshold",
                Timestamp = DateTime.Now
            };

            var stopwatch = Stopwatch.StartNew();

            try
            {
                // 验证参数
                var validationResult = ValidateParameters(parameters);
                if (!validationResult.IsValid)
                {
                    result.SetError($"参数验证失败: {string.Join(", ", validationResult.Errors)}");
                    return result;
                }

                // 验证输入图像
                if (image == null || image.Empty())
                {
                    result.SetError("输入图像为空");
                    return result;
                }

                // 记录输入尺寸
                result.InputSize = image.Size();

                // 获取参数值
                int threshold = parameters.Threshold;
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
                }

                // 转换OpenCvSharp的阈值类型
                var thresholdType = ConvertThresholdType(type);

                if (type == ThresholdType.Binary || type == ThresholdType.BinaryInv)
                {
                    // 普通阈值化
                    outputImage = new Mat();
                    actualThreshold = Cv2.Threshold(grayImage, outputImage, threshold, maxValue, thresholdType);
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
                }

                // 如果需要反转
                if (invert && outputImage != null)
                {
                    var inverted = new Mat();
                    Cv2.BitwiseNot(outputImage, inverted);
                    outputImage.Dispose();
                    outputImage = inverted;
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

                // 释放临时灰度图
                if (grayImage != image)
                {
                    grayImage.Dispose();
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result.SetError($"处理失败: {ex.Message}", ex);
                result.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;
            }

            return result;
        }

        /// <summary>
        /// 执行工具（异步）
        /// </summary>
        public Task<ThresholdResults> RunAsync(Mat image, ThresholdParameters parameters)
        {
            return Task.Run(() => Run(image, parameters));
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
        /// 获取默认参数
        /// </summary>
        public ThresholdParameters GetDefaultParameters()
        {
            return new ThresholdParameters();
        }

        /// <summary>
        /// 创建调试窗口
        /// </summary>
        public System.Windows.Window CreateDebugWindow()
        {
            return new ThresholdToolDebugWindow();
        }

        #endregion

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
