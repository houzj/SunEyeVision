using System;
using System.Collections.Generic;
using System.Diagnostics;
using OpenCvSharp;
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.SDK.Execution.Results;
using SunEyeVision.Plugin.SDK.Validation;
using SunEyeVision.Tool.GaussianBlur.Views;
using System.Text.Json.Serialization;

namespace SunEyeVision.Tool.GaussianBlur
{
    #region 参数和结果定义

    /// <summary>
    /// 高斯模糊参数
    /// </summary>
    /// <remarks>
    /// 多态序列化（rule-010: 方案系统实现规范）：
    /// 使用 [JsonDerivedType] 特性标识参数类型，类型标识符为 "GaussianBlur"。
    /// </remarks>
    [JsonDerivedType(typeof(GaussianBlurParameters), "GaussianBlur")]
    public class GaussianBlurParameters : ToolParameters
    {
        private int _kernelSize = 5;
        private double _sigma = 1.5;

        /// <summary>
        /// 高斯核大小，必须为奇数
        /// </summary>
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
        public Mat? OutputImage { get; set; }
        public int KernelSizeUsed { get; set; }
        public double SigmaUsed { get; set; }
        public int InputWidth { get; set; }
        public int InputHeight { get; set; }

        /// <summary>
        /// 获取结果项列表
        /// </summary>
        public override IReadOnlyList<ResultItem> GetResultItems()
        {
            var items = new List<ResultItem>();
            items.AddNumeric("KernelSizeUsed", KernelSizeUsed, "像素");
            items.AddNumeric("SigmaUsed", SigmaUsed, "标准差");
            items.AddNumeric("InputWidth", InputWidth, "像素");
            items.AddNumeric("InputHeight", InputHeight, "像素");
            items.AddNumeric("ExecutionTimeMs", ExecutionTimeMs, "ms");
            return items;
        }
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

                parameters.LogInfo($"开始高斯模糊处理: 核大小={parameters.KernelSize}, 标准差={parameters.Sigma}");

                var outputImage = new Mat();
                Cv2.GaussianBlur(image, outputImage, new Size(parameters.KernelSize, parameters.KernelSize), parameters.Sigma);

                result.OutputImage = outputImage;
                result.KernelSizeUsed = parameters.KernelSize;
                result.SigmaUsed = parameters.Sigma;
                result.InputWidth = image.Width;
                result.InputHeight = image.Height;

                stopwatch.Stop();
                result.SetSuccess(stopwatch.ElapsedMilliseconds);

                parameters.LogSuccess($"高斯模糊处理完成: {result.InputWidth}x{result.InputHeight}, 耗时{stopwatch.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                parameters.LogError($"高斯模糊异常: {ex.Message}", ex);
                result.SetError($"处理失败: {ex.Message}", ex);
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
