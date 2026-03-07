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
    [Tool("edge_detection", "边缘检测", Description = "检测图像中的边缘", Icon = "📐", Category = "图像处理")]
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
                if (image == null || image.Empty())
                {
                    result.SetError("输入图像为空");
                    return result;
                }

                // 确保输入是灰度图
                Mat grayImage = image;
                if (image.Channels() > 1)
                {
                    grayImage = new Mat();
                    Cv2.CvtColor(image, grayImage, ColorConversionCodes.BGR2GRAY);
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
                result.InputSize = new OpenCvSharp.Size(image.Width, image.Height);
                result.SetSuccess(stopwatch.ElapsedMilliseconds);

                if (grayImage != image)
                    grayImage.Dispose();
            }
            catch (Exception ex)
            {
                result.SetError($"处理失败: {ex.Message}");
            }

            return result;
        }
    }
}
