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
using SunEyeVision.Tool.EdgeDetection.Views;

namespace SunEyeVision.Tool.EdgeDetection
{
    [Tool("edge_detection", "边缘检测", Description = "检测图像中的边缘", Icon = "📐", Category = "图像处理", Version = "2.0.0", HasDebugWindow = true)]
    public class EdgeDetectionTool : IToolPlugin<EdgeDetectionParameters, EdgeDetectionResults>
    {
        public bool HasDebugWindow => true;

        public System.Windows.Window? CreateDebugWindow()
        {
            return new EdgeDetectionToolDebugWindow();
        }

        public EdgeDetectionResults Run(Mat image, EdgeDetectionParameters parameters)
        {
            var result = new EdgeDetectionResults();
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

                parameters.LogInfo($"开始边缘检测: 阈值1={parameters.Threshold1}, 阈值2={parameters.Threshold2}, 孔径={parameters.ApertureSize}");

                // 记录输入尺寸
                result.InputSize = new OpenCvSharp.Size(image.Width, image.Height);

                // 确保输入是灰度图
                Mat grayImage = image;
                if (image.Channels() > 1)
                {
                    grayImage = new Mat();
                    Cv2.CvtColor(image, grayImage, ColorConversionCodes.BGR2GRAY);
                    parameters.LogInfo($"已将图像转换为灰度，原图通道数: {image.Channels()}");
                }

                var outputImage = new Mat();
                Cv2.Canny(grayImage, outputImage, parameters.Threshold1, parameters.Threshold2, parameters.ApertureSize);

                // 计算边缘数量
                Cv2.FindContours(outputImage, out var contours, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

                result.OutputImage = outputImage;
                result.EdgeCount = contours.Length;
                result.Threshold1Used = parameters.Threshold1;
                result.Threshold2Used = parameters.Threshold2;
                result.ApertureSizeUsed = parameters.ApertureSize;

                stopwatch.Stop();
                result.SetSuccess(stopwatch.ElapsedMilliseconds);

                parameters.LogSuccess($"边缘检测完成: 检测到{result.EdgeCount}个边缘, 耗时{stopwatch.ElapsedMilliseconds}ms");

                if (grayImage != image)
                    grayImage.Dispose();
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                parameters.LogError($"边缘检测异常: {ex.Message}", ex);
                result.SetError($"处理失败: {ex.Message}", ex);
                result.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;
            }

            return result;
        }
    }
}
