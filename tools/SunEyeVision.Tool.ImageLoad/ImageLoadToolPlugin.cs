using System;
using System.Collections.Generic;
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.SDK.Validation;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.SDK.Execution.Results;
using SunEyeVision.Plugin.SDK.Metadata;
using OpenCvSharp;
using System.Diagnostics;
using System.IO;
using System.Text.Json.Serialization;

namespace SunEyeVision.Tool.ImageLoad
{
    /// <summary>
    /// 图像载入参数
    /// </summary>
    /// <remarks>
    /// 多态序列化（rule-010: 方案系统实现规范）：
    /// 使用 [JsonDerivedType] 特性标识参数类型，类型标识符为 "ImageLoad"。
    /// </remarks>
    [JsonDerivedType(typeof(ImageLoadParameters), "ImageLoad")]
    public class ImageLoadParameters : ToolParameters
    {
        private string _filePath = string.Empty;

        /// <summary>
        /// 图像文件路径
        /// </summary>
        [ParameterDisplay(DisplayName = "文件路径", Description = "要载入的图像文件路径", Group = "基本参数", Order = 1)]
        public string FilePath
        {
            get => _filePath;
            set => SetProperty(ref _filePath, value, "文件路径");
        }

        public override ValidationResult Validate()
        {
            var result = base.Validate();
            if (string.IsNullOrEmpty(FilePath))
            {
                result.AddError("文件路径不能为空");
            }
            return result;
        }
    }

    /// <summary>
    /// 图像载入结果
    /// </summary>
    public class ImageLoadResults : ToolResults
    {
        [Param(DisplayName = "输出图像", Description = "载入的图像", Category = ParamCategory.Output)]
        public Mat? OutputImage { get; set; }

        public int Width { get; set; }
        public int Height { get; set; }
        public int Channels { get; set; }
        public int Depth { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string FileFormat { get; set; } = string.Empty;

        /// <summary>
        /// 获取结果项列表
        /// </summary>
        public override IReadOnlyList<ResultItem> GetResultItems()
        {
            var items = new List<ResultItem>();
            items.AddNumeric("Width", Width, "像素");
            items.AddNumeric("Height", Height, "像素");
            items.AddNumeric("Channels", Channels, "通道");
            items.AddNumeric("Depth", Depth, "位");
            items.AddText("FilePath", FilePath);
            items.AddNumeric("FileSize", FileSize, "字节");
            items.AddText("FileFormat", FileFormat);
            items.AddNumeric("ExecutionTimeMs", ExecutionTimeMs, "ms");
            return items;
        }
    }

    /// <summary>
    /// 图像载入工具 - 从文件读取图像
    /// </summary>
    [Tool("ImageLoad", "图像载入", Description = "从文件载入图像，作为工作流的图像源", Icon = "📁", Category = "采集", Version = "2.0.0", HasDebugWindow = false)]
    public class ImageLoadTool : IToolPlugin<ImageLoadParameters, ImageLoadResults>
    {
        public bool HasDebugWindow => false;

        public System.Windows.Window? CreateDebugWindow() => null;

        public ImageLoadResults Run(Mat image, ImageLoadParameters parameters)
        {
            var result = new ImageLoadResults
            {
                ToolName = "ImageLoad",
                ToolId = "image_load",
                Timestamp = DateTime.Now
            };

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

                // 验证文件路径
                if (string.IsNullOrEmpty(parameters.FilePath))
                {
                    parameters.LogWarning("文件路径为空");
                    result.SetError("文件路径为空");
                    return result;
                }

                if (!File.Exists(parameters.FilePath))
                {
                    parameters.LogError($"文件不存在: {parameters.FilePath}");
                    result.SetError($"文件不存在: {parameters.FilePath}");
                    return result;
                }

                parameters.LogInfo($"开始加载图像: {parameters.FilePath}");

                // 获取文件信息
                var fileInfo = new FileInfo(parameters.FilePath);
                result.FilePath = parameters.FilePath;
                result.FileSize = fileInfo.Length;
                result.FileFormat = fileInfo.Extension.ToUpperInvariant();

                // 使用 OpenCvSharp 加载图像
                var loadedImage = Cv2.ImRead(parameters.FilePath, ImreadModes.Color);

                if (loadedImage == null || loadedImage.Empty())
                {
                    parameters.LogError($"无法加载图像: {parameters.FilePath}");
                    result.SetError($"无法加载图像: {parameters.FilePath}");
                    return result;
                }

                // 设置结果
                result.OutputImage = loadedImage;
                result.Width = loadedImage.Width;
                result.Height = loadedImage.Height;
                result.Channels = loadedImage.Channels();
                result.Depth = (int)loadedImage.Depth();

                stopwatch.Stop();
                result.SetSuccess(stopwatch.ElapsedMilliseconds);

                parameters.LogSuccess($"图像加载成功: {result.Width}x{result.Height}, {result.Channels}通道, {result.FileSize}字节, 耗时{stopwatch.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                parameters.LogError($"加载图像异常: {ex.Message}", ex);
                result.SetError($"加载图像失败: {ex.Message}", ex);
                result.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;
            }

            return result;
        }
    }
}
