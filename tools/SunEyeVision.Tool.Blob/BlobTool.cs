using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using OpenCvSharp;
using SunEyeVision.Plugin.SDK;
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.SDK.Execution.Results;
using SunEyeVision.Plugin.SDK.Metadata;
using SunEyeVision.Plugin.SDK.Validation;
using SunEyeVision.Tool.Blob.Views;
using System.Text.Json.Serialization;

namespace SunEyeVision.Tool.Blob
{
    /// <summary>
    /// Blob检测工具
    /// </summary>
    [Tool(
        id: "blob_detection",
        displayName: "Blob检测",
        Description = "检测图像中的连通区域",
        Icon = "🔵",
        Category = "分析",
        Version = "1.0.0",
        HasDebugWindow = true
    )]
    public class BlobTool : IToolPlugin<BlobParameters, BlobResults>
    {
        /// <summary>
        /// 执行Blob检测
        /// </summary>
        public BlobResults Run(Mat image, BlobParameters parameters)
        {
            var result = new BlobResults();
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

                parameters.LogInfo($"开始Blob检测: 最小面积={parameters.MinArea}, 最大面积={parameters.MaxArea}, 圆度阈值={parameters.Circularity}");

                // 转换为灰度图
                var grayImage = new Mat();
                if (image.Channels() > 1)
                {
                    Cv2.CvtColor(image, grayImage, ColorConversionCodes.BGR2GRAY);
                }
                else
                {
                    grayImage = image.Clone();
                }

                // 二值化
                var binaryImage = new Mat();
                Cv2.Threshold(grayImage, binaryImage, 128, 255, ThresholdTypes.Binary);

                    // 查找轮廓
                Point[][] contours;
                HierarchyIndex[] hierarchy;
                Cv2.FindContours(binaryImage, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

                // 过滤和处理轮廓
                var blobs = new List<BlobResult>();
                var overlayImage = image.Clone();
                int index = 0;
                int contourIndex = 0;

                foreach (var contour in contours)
                {
                    // 计算轮廓面积
                    double area = Cv2.ContourArea(contour);

                    // 面积过滤
                    if (area < parameters.MinArea || area > parameters.MaxArea)
                        continue;

                    // 计算圆度和凸度
                    double circularity = CalculateCircularity(contour, area);
                    double convexity = CalculateConvexity(contour, area);

                    // 形状过滤
                    if (circularity < parameters.Circularity || convexity < parameters.Convexity)
                        continue;

                    // 计算边界矩形和中心点
                    var boundingRect = Cv2.BoundingRect(contour);
                    var contourMoments = Cv2.Moments(contour);
                    double centerX = contourMoments.M10 / contourMoments.M00;
                    double centerY = contourMoments.M01 / contourMoments.M00;

                    // 计算惯性比率
                    double[] huMoments = contourMoments.HuMoments();
                    double inertiaRatio = Math.Sqrt(Math.Abs(huMoments[2] / Math.Pow(Math.Sqrt(huMoments[0]), 4)));

                    // 距离过滤
                    if (blobs.Count > 0 && ShouldFilterByDistance(centerX, centerY, blobs, parameters.MinDistanceBetweenBlobs))
                        continue;

                    // 创建Blob结果
                    var blob = new BlobResult
                    {
                        Index = index++,
                        Area = area,
                        CenterX = centerX,
                        CenterY = centerY,
                        Width = boundingRect.Width,
                        Height = boundingRect.Height,
                        Circularity = circularity,
                        Convexity = convexity,
                        InertiaRatio = inertiaRatio,
                        BoundingRect = boundingRect,
                        Contour = contour.ToList(),
                        IsVisible = true
                    };

                    blobs.Add(blob);

                    // 绘制轮廓到叠加图像
                    Cv2.DrawContours(overlayImage, new Point[][] { contour }, contourIndex, Scalar.Red, 2);
                    Cv2.DrawMarker(overlayImage, new Point(centerX, centerY), Scalar.Green, MarkerTypes.Cross, 10, 1);
                    contourIndex++;
                }

                // 设置结果
                result.Blobs = blobs;
                result.TotalCount = blobs.Count;
                result.OverlayImage = overlayImage;
                result.SetSuccess(stopwatch.ElapsedMilliseconds);

                parameters.LogSuccess($"Blob检测完成: 检测到{result.TotalCount}个Blob, 耗时{stopwatch.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                parameters.LogError($"Blob检测异常: {ex.Message}", ex);
                result.SetError($"处理失败: {ex.Message}", ex);
                result.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;
            }

            return result;
        }

        /// <summary>
        /// 创建调试控件
        /// </summary>
        public FrameworkElement? CreateDebugControl()
        {
            return new BlobToolDebugWindow();
        }

        /// <summary>
        /// 创建调试窗口（保留向后兼容）
        /// </summary>
        [Obsolete("使用 CreateDebugControl 替代")]
        public System.Windows.Window? CreateDebugWindow()
        {
            return new BlobToolDebugWindow();
        }

        /// <summary>
        /// 计算圆度
        /// </summary>
        private double CalculateCircularity(Point[] contour, double area)
        {
            if (area <= 0)
                return 0;

            double perimeter = Cv2.ArcLength(contour, true);
            return 4 * Math.PI * area / (perimeter * perimeter);
        }

        /// <summary>
        /// 计算凸度
        /// </summary>
        private double CalculateConvexity(Point[] contour, double area)
        {
            try
            {
                var hull = Cv2.ConvexHull(contour);
                double hullArea = Cv2.ContourArea(hull);

                if (hullArea <= 0)
                    return 0;

                return area / hullArea;
            }
            catch
            {
                return 1;
            }
        }

        /// <summary>
        /// 判断是否应该根据距离过滤
        /// </summary>
        private bool ShouldFilterByDistance(double x, double y, List<BlobResult> existingBlobs, int minDistance)
        {
            if (minDistance <= 0)
                return false;

            foreach (var blob in existingBlobs)
            {
                double dx = blob.CenterX - x;
                double dy = blob.CenterY - y;
                double distance = Math.Sqrt(dx * dx + dy * dy);
                
                if (distance < minDistance)
                    return true;
            }

            return false;
        }
    }
}
