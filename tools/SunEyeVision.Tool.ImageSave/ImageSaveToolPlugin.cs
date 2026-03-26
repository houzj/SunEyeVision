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
        public string OutputPath { get; set; } = "output/image.png";
        public string OutputFormat { get; set; } = "png";
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
    }

    [Tool("image_save", "图像保存", Description = "保存图像到文件", Icon = "💾", Category = "输出")]
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
                if (image == null || image.Empty()) { result.SetError("输入图像为空"); return result; }

                var dir = Path.GetDirectoryName(parameters.OutputPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var path = Path.ChangeExtension(parameters.OutputPath, $".{parameters.OutputFormat}");
                Cv2.ImWrite(path, image);
                
                result.SavedPath = path;
                result.FileSize = new FileInfo(path).Length;
                result.SetSuccess(sw.ElapsedMilliseconds);
            }
            catch (Exception ex) { result.SetError($"保存失败: {ex.Message}"); }
            return result;
        }
    }
}
