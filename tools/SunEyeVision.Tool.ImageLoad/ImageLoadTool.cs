using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using OpenCvSharp;
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.SDK.Validation;

namespace SunEyeVision.Tool.ImageLoad
{
    /// <summary>
    /// 图像载入工具 - 只负责从文件读取图像，不进行任何处理
    /// 图像处理（灰度转换、缩放等）由下游工具实现
    /// </summary>
    public class ImageLoadTool : ITool<ImageLoadParameters, ImageLoadResults>
    {
        #region ITool 基本信息

        public string Name => "ImageLoad";
        public string Description => "从文件载入图像";
        public string Version => "2.0.0";
        public string Category => "采集";

        /// <summary>
        /// 图像载入工具不需要调试窗口
        /// </summary>
        public bool HasDebugWindow => false;

        /// <summary>
        /// 无调试窗口，返回 null
        /// </summary>
        System.Windows.Window? ITool.CreateDebugWindow() => null;

        #endregion

        #region ITool<ImageLoadParameters, ImageLoadResults> 实现

        /// <summary>
        /// 执行工具（同步）
        /// </summary>
        public ImageLoadResults Run(Mat image, ImageLoadParameters parameters)
        {
            var result = new ImageLoadResults
            {
                ToolName = Name,
                ToolId = "image_load",
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

                // 验证文件路径
                if (string.IsNullOrEmpty(parameters.FilePath))
                {
                    result.SetError("文件路径为空");
                    return result;
                }

                if (!File.Exists(parameters.FilePath))
                {
                    result.SetError($"文件不存在: {parameters.FilePath}");
                    return result;
                }

                // 获取文件信息
                var fileInfo = new FileInfo(parameters.FilePath);
                result.FilePath = parameters.FilePath;
                result.FileSize = fileInfo.Length;
                result.FileFormat = fileInfo.Extension.ToUpperInvariant();

                // 使用 OpenCvSharp 加载图像（保持原样，不转换）
                var loadedImage = Cv2.ImRead(parameters.FilePath, ImreadModes.Color);

                if (loadedImage == null || loadedImage.Empty())
                {
                    result.SetError($"无法加载图像: {parameters.FilePath}");
                    return result;
                }

                // 设置结果 - 只记录原始信息，不进行任何处理
                result.OutputImage = loadedImage;
                result.Width = loadedImage.Width;
                result.Height = loadedImage.Height;
                result.Channels = loadedImage.Channels();
                result.Depth = (int)loadedImage.Depth();

                stopwatch.Stop();
                result.SetSuccess(stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result.SetError($"加载图像失败: {ex.Message}", ex);
                result.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;
            }

            return result;
        }

        /// <summary>
        /// 执行工具（异步）
        /// </summary>
        public Task<ImageLoadResults> RunAsync(Mat image, ImageLoadParameters parameters)
        {
            return Task.Run(() => Run(image, parameters));
        }

        /// <summary>
        /// 验证参数
        /// </summary>
        public static ValidationResult ValidateParameters(ImageLoadParameters parameters)
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
        public static ImageLoadParameters GetDefaultParameters()
        {
            return new ImageLoadParameters();
        }

        #endregion
    }
}
