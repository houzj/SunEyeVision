using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using OpenCvSharp;
using SunEyeVision.Plugin.SDK;
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.SDK.Execution.Results;
using SunEyeVision.Plugin.SDK.Metadata;
using SunEyeVision.Plugin.SDK.Validation;
using SunEyeVision.Tool.ImageSave.Views;
using System.Text.Json.Serialization;

namespace SunEyeVision.Tool.ImageSave
{
    /// <summary>
    /// 图像保存参数
    /// </summary>
    /// <remarks>
    /// 多态序列化（rule-010: 方案系统实现规范）：
    /// 使用 [JsonDerivedType] 特性标识参数类型，类型标识符为 "ImageSave"。
    /// </remarks>
    [JsonDerivedType(typeof(ImageSaveParameters), "ImageSave")]
    public class ImageSaveParameters : ToolParameters
    {
        private string _outputPath = "output/image.png";
        private string _outputFormat = "png";

        /// <summary>
        /// 输出文件路径
        /// </summary>
        [ParameterDisplay(DisplayName = "输出路径", Description = "图像保存的文件路径", Group = "基本参数", Order = 1)]
        public string OutputPath
        {
            get => _outputPath;
            set => SetProperty(ref _outputPath, value, "输出路径");
        }

        /// <summary>
        /// 输出文件格式
        /// </summary>
        [ParameterDisplay(DisplayName = "输出格式", Description = "图像保存格式（png, jpg, bmp等）", Group = "基本参数", Order = 2)]
        public string OutputFormat
        {
            get => _outputFormat;
            set => SetProperty(ref _outputFormat, value, "输出格式");
        }

        public override ValidationResult Validate()
        {
            var r = new ValidationResult();
            if (string.IsNullOrWhiteSpace(OutputPath)) r.AddError("输出路径不能为空");
            return r;
        }
    }

    public class ImageSaveResults : ToolResults
    {
        public string SavedPath { get; set; } = "";
        public long FileSize { get; set; }
        public string OutputFormatUsed { get; set; } = "";

        /// <summary>
        /// 获取结果项列表
        /// </summary>
        public override IReadOnlyList<ResultItem> GetResultItems()
        {
            var items = new List<ResultItem>();
            items.AddText("SavedPath", SavedPath);
            items.AddNumeric("FileSize", FileSize, "字节");
            items.AddText("OutputFormatUsed", OutputFormatUsed);
            items.AddNumeric("ExecutionTimeMs", ExecutionTimeMs, "ms");
            return items;
        }
    }

    [Tool("image_save", "图像保存", Description = "保存图像到文件", Icon = "💾", Category = "输出", Version = "2.0.0", HasDebugWindow = true)]
    public class ImageSaveTool : IToolPlugin<ImageSaveParameters, ImageSaveResults>
    {
        public bool HasDebugWindow => true;

        public System.Windows.Window? CreateDebugWindow()
        {
            return new ImageSaveToolDebugWindow();
        }

        public ImageSaveResults Run(Mat image, ImageSaveParameters parameters)
        {
            var result = new ImageSaveResults();
            var sw = Stopwatch.StartNew();

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

                parameters.LogInfo($"开始保存图像: 路径={parameters.OutputPath}, 格式={parameters.OutputFormat}");

                var dir = Path.GetDirectoryName(parameters.OutputPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var path = Path.ChangeExtension(parameters.OutputPath, $".{parameters.OutputFormat}");
                Cv2.ImWrite(path, image);

                result.SavedPath = path;
                result.FileSize = new FileInfo(path).Length;
                result.OutputFormatUsed = parameters.OutputFormat;
                result.SetSuccess(sw.ElapsedMilliseconds);

                parameters.LogInfo($"图像保存完成: {result.SavedPath}, 大小={result.FileSize}字节, 耗时{sw.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                sw.Stop();
                parameters.LogError($"图像保存异常: {ex.Message}", ex);
                result.SetError($"保存失败: {ex.Message}");
                result.ExecutionTimeMs = sw.ElapsedMilliseconds;
            }

            return result;
        }
    }
}
