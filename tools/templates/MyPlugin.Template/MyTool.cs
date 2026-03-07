using System;
using System.Diagnostics;
using OpenCvSharp;
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.SDK.Execution.Results;

namespace MyPlugin
{
    /// <summary>
    /// 阈值化工具插件示例 - 展示完整的插件开发流程
    /// </summary>
    /// <remarks>
    /// 这是 SunEyeVision 插件开发的完整示例，展示：
    /// 1. 使用 [Tool] 特性定义元数据（单一数据源）
    /// 2. 实现 IToolPlugin&lt;TParams, TResult&gt; 强类型接口
    /// 3. 参数定义、验证和处理逻辑
    /// 
    /// 开发步骤：
    /// 1. 复制此文件并修改命名空间
    /// 2. 修改 [Tool] 特性的 Id 和 DisplayName
    /// 3. 定义参数类和结果类
    /// 4. 实现 Run 方法（业务逻辑）
    /// </remarks>
    [Tool("myplugin-threshold", "阈值化处理",
        Description = "将灰度图像转换为二值图像",
        Icon = "🔲",
        Category = "图像处理",
        Version = "1.0.0")]
    public class ThresholdTool : IToolPlugin<ThresholdToolParameters, ThresholdToolResults>
    {
        /// <summary>
        /// 执行工具
        /// </summary>
        public ThresholdToolResults Run(Mat image, ThresholdToolParameters parameters)
        {
            var result = new ThresholdToolResults();
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // 验证输入
                if (image == null || image.Empty())
                {
                    parameters.LogWarning("输入图像为空");
                    result.SetError("输入图像为空");
                    return result;
                }

                parameters.LogInfo($"开始二值化处理: 图像尺寸 {image.Width}x{image.Height}");

                // 执行二值化处理
                var outputImage = new Mat();
                Cv2.Threshold(image, outputImage, parameters.ThresholdValue, parameters.MaxValue, ThresholdTypes.Binary);

                result.OutputImage = outputImage;
                result.ProcessedPixels = image.Rows * image.Cols;
                stopwatch.Stop();
                result.SetSuccess(stopwatch.ElapsedMilliseconds);

                parameters.LogSuccess($"二值化处理完成，阈值: {parameters.ThresholdValue}, 耗时: {stopwatch.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                parameters.LogError($"二值化处理异常: {ex.Message}", ex);
                result.SetError($"处理失败: {ex.Message}");
            }

            return result;
        }
    }

    #region 参数和结果定义

    /// <summary>
    /// 阈值化工具参数
    /// </summary>
    public class ThresholdToolParameters : ToolParameters
    {
        /// <summary>
        /// 阈值 (0-255)
        /// </summary>
        [ParameterRange(0, 255, Step = 1)]
        [ParameterDisplay(DisplayName = "阈值", Description = "二值化的阈值(0-255)", Group = "基本参数", Order = 1)]
        public int ThresholdValue { get; set; } = 128;

        /// <summary>
        /// 最大值 (0-255)
        /// </summary>
        [ParameterRange(0, 255, Step = 1)]
        [ParameterDisplay(DisplayName = "最大值", Description = "超过阈值时设置的值", Group = "基本参数", Order = 2)]
        public int MaxValue { get; set; } = 255;
    }

    /// <summary>
    /// 阈值化工具结果
    /// </summary>
    public class ThresholdToolResults : ToolResults
    {
        /// <summary>
        /// 输出图像
        /// </summary>
        public Mat? OutputImage { get; set; }

        /// <summary>
        /// 处理的像素数
        /// </summary>
        public int ProcessedPixels { get; set; }
    }

    #endregion
}
