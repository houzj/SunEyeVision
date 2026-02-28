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

namespace SunEyeVision.Tool.ImageSave
{
    [ToolPlugin("image_save", "ImageSave")]
    public class ImageSaveToolPlugin : IToolPlugin
    {
        public string Name => "图像保存";
        public string Version => "2.0.0";
        public string Author => "SunEyeVision";
        public string Description => "保存图像到文件";
        public string PluginId => "suneye.image_save";
        public string Icon => "💾";
        public List<string> Dependencies => new List<string>();
        public bool IsLoaded { get; private set; }

        public void Initialize() => IsLoaded = true;
        public void Unload() => IsLoaded = false;
        public List<Type> GetAlgorithmNodes() => new List<Type>();

        public List<ToolMetadata> GetToolMetadata() => new List<ToolMetadata>
        {
            new ToolMetadata
            {
                Id = "image_save",
                Name = "ImageSave",
                DisplayName = "图像保存",
                Icon = "💾",
                Category = "输出",
                Description = "保存图像到文件",
                Version = Version,
                Author = Author,
                InputParameters = new List<ParameterMetadata>
                {
                    new ParameterMetadata { Name = "OutputPath", DisplayName = "输出路径", Type = ParamDataType.String, DefaultValue = "output/image.png" },
                    new ParameterMetadata { Name = "OutputFormat", DisplayName = "输出格式", Type = ParamDataType.Enum, DefaultValue = "png", Options = new object[] { "jpg", "png", "bmp" } }
                },
                OutputParameters = new List<ParameterMetadata>
                {
                    new ParameterMetadata { Name = "SavedPath", DisplayName = "保存路径", Type = ParamDataType.String }
                }
            }
        };

        public ITool? CreateToolInstance(string toolId) => toolId == "image_save" ? new ImageSaveTool() : null;

        public AlgorithmParameters GetDefaultParameters(string toolId)
        {
            var p = new AlgorithmParameters();
            p.Set("OutputPath", "output/image.png");
            p.Set("OutputFormat", "png");
            return p;
        }

        public ValidationResult ValidateParameters(string toolId, AlgorithmParameters parameters)
        {
            var r = new ValidationResult();
            var path = parameters.Get<string>("OutputPath");
            if (string.IsNullOrWhiteSpace(path)) r.AddError("输出路径不能为空");
            return r;
        }
    }

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

    public class ImageSaveTool : ITool<ImageSaveParameters, ImageSaveResults>
    {
        public string Name => "图像保存";
        public string Description => "保存图像到文件";
        public string Version => "2.0.0";
        public string Category => "输出";

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

        public ValidationResult ValidateParameters(ImageSaveParameters parameters) => parameters.Validate();
        public ImageSaveParameters GetDefaultParameters() => new ImageSaveParameters();
    }
}
