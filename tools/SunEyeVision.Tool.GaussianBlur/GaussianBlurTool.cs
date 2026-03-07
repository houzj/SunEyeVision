using System;
using System.Collections.Generic;
using System.Diagnostics;
using OpenCvSharp;
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.SDK.Execution.Results;
using SunEyeVision.Plugin.SDK.Validation;
using SunEyeVision.Tool.GaussianBlur.Views;

namespace SunEyeVision.Tool.GaussianBlur
{
    #region 参数和结果定义

    /// <summary>
    /// 高斯模糊参数
    /// </summary>
    public class GaussianBlurParameters : ToolParameters
    {
        private int _kernelSize = 5;
        private double _sigma = 1.5;

        /// <summary>
        /// 高斯核大小，必须为奇数
        /// </summary>
        [ParameterRange(3, 99, Step = 2)]
        [ParameterDisplay(DisplayName = "核大小", Description = "高斯核大小，必须为奇数", Group = "基本参数", Order = 1)]
        public int KernelSize
        {
            get => _kernelSize;
            set
            {
                // 确保为奇数
                if (value % 2 == 0) value++;
                SetProperty(ref _kernelSize, value, "核大小");
            }
        }

        /// <summary>
        /// 高斯核的标准差
        /// </summary>
        [ParameterRange(0.1, 10.0, Step = 0.1)]
        [ParameterDisplay(DisplayName = "标准差", Description = "高斯核的标准差", Group = "基本参数", Order = 2)]
        public double Sigma
        {
            get => _sigma;
            set => SetProperty(ref _sigma, value, "标准差");
        }

        public override ValidationResult Validate()
        {
            var result = new ValidationResult();
            if (KernelSize < 3 || KernelSize > 99)
                result.AddError("核大小必须在3-99之间");
            else if (KernelSize % 2 == 0)
                result.AddError("核大小必须为奇数");
            if (Sigma <= 0)
                result.AddWarning("标准差应大于0");
            return result;
        }
    }

    /// <summary>
    /// 高斯模糊结果
    /// </summary>
    public class GaussianBlurResults : ToolResults
    {
        [SunEyeVision.Plugin.SDK.Metadata.Param(DisplayName = "输出图像", Description = "模糊处理后的图像", Category = SunEyeVision.Plugin.SDK.Metadata.ParamCategory.Output)]
        public Mat? OutputImage { get; set; }
        public int KernelSizeUsed { get; set; }
        public double SigmaUsed { get; set; }
    }

    #endregion

    #region 工具实现

    /// <summary>
    /// 高斯模糊工具
    /// </summary>
    [Tool(
        id: "gaussian_blur",
        displayName: "高斯模糊",
        Description = "应用高斯模糊滤波",
        Icon = "🖼️",
        Category = "图像处理",
        Version = "2.0.0",
        HasDebugWindow = true
    )]
    public class GaussianBlurTool : IToolPlugin<GaussianBlurParameters, GaussianBlurResults>
    {
        /// <summary>
        /// 执行工具
        /// </summary>
        public GaussianBlurResults Run(Mat image, GaussianBlurParameters parameters)
        {
            var result = new GaussianBlurResults();
            var stopwatch = Stopwatch.StartNew();

            try
            {
                if (image == null || image.Empty())
                {
                    result.SetError("输入图像为空");
                    return result;
                }

                parameters.LogInfo($"开始高斯模糊处理: 核大小={parameters.KernelSize}, 标准差={parameters.Sigma}");

                var outputImage = new Mat();
                Cv2.GaussianBlur(image, outputImage, new Size(parameters.KernelSize, parameters.KernelSize), parameters.Sigma);

                result.OutputImage = outputImage;
                result.KernelSizeUsed = parameters.KernelSize;
                result.SigmaUsed = parameters.Sigma;
                
                stopwatch.Stop();
                result.SetSuccess(stopwatch.ElapsedMilliseconds);

                parameters.LogInfo($"高斯模糊处理完成，耗时 {stopwatch.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                parameters.LogError($"处理失败: {ex.Message}", ex);
                result.SetError($"处理失败: {ex.Message}");
                result.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;
            }

            return result;
        }

        /// <summary>
        /// 创建调试窗口
        /// </summary>
        public System.Windows.Window? CreateDebugWindow()
        {
            return new GaussianBlurToolDebugWindow();
        }
    }

    #endregion
}
