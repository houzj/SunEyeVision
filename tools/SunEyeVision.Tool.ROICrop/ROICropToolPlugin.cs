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

namespace SunEyeVision.Tool.ROICrop
{
    [ToolPlugin("roi_crop", "ROICrop")]
    public class ROICropToolPlugin : IToolPlugin
    {
        public string Name => "ROI裁剪";
        public string Version => "2.0.0";
        public string Author => "SunEyeVision";
        public string Description => "裁剪指定矩形区域";
        public string PluginId => "suneye.roi_crop";
        public string Icon => "✂️";
        public List<string> Dependencies => new List<string>();
        public bool IsLoaded { get; private set; }

        public void Initialize() => IsLoaded = true;
        public void Unload() => IsLoaded = false;
        public List<Type> GetAlgorithmNodes() => new List<Type>();

        public List<ToolMetadata> GetToolMetadata() => new List<ToolMetadata>
        {
            new ToolMetadata
            {
                Id = "roi_crop",
                Name = "ROICrop",
                DisplayName = "ROI裁剪",
                Icon = "✂️",
                Category = "图像处理",
                Description = "裁剪指定的矩形感兴趣区域",
                Version = Version,
                Author = Author,
                InputParameters = new List<ParameterMetadata>
                {
                    new ParameterMetadata { Name = "X", DisplayName = "X", Type = ParamDataType.Int, DefaultValue = 0 },
                    new ParameterMetadata { Name = "Y", DisplayName = "Y", Type = ParamDataType.Int, DefaultValue = 0 },
                    new ParameterMetadata { Name = "Width", DisplayName = "宽度", Type = ParamDataType.Int, DefaultValue = 100 },
                    new ParameterMetadata { Name = "Height", DisplayName = "高度", Type = ParamDataType.Int, DefaultValue = 100 }
                },
                OutputParameters = new List<ParameterMetadata>
                {
                    new ParameterMetadata { Name = "OutputImage", DisplayName = "输出图像", Type = ParamDataType.Image }
                }
            }
        };

        public ITool? CreateToolInstance(string toolId) => toolId == "roi_crop" ? new ROICropTool() : null;

        public AlgorithmParameters GetDefaultParameters(string toolId)
        {
            var p = new AlgorithmParameters();
            p.Set("X", 0);
            p.Set("Y", 0);
            p.Set("Width", 100);
            p.Set("Height", 100);
            return p;
        }

        public ValidationResult ValidateParameters(string toolId, AlgorithmParameters parameters) => new ValidationResult();
    }

    public class ROICropParameters : ToolParameters
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; } = 100;
        public int Height { get; set; } = 100;
        public override ValidationResult Validate()
        {
            var r = new ValidationResult();
            if (Width <= 0 || Height <= 0) r.AddError("宽高必须大于0");
            return r;
        }
    }

    public class ROICropResults : ToolResults
    {
        public Mat? OutputImage { get; set; }
        public Rect CroppedArea { get; set; }
    }

    public class ROICropTool : ITool<ROICropParameters, ROICropResults>
    {
        public string Name => "ROI裁剪";
        public string Description => "裁剪指定矩形区域";
        public string Version => "2.0.0";
        public string Category => "图像处理";

        public ROICropResults Run(Mat image, ROICropParameters parameters)
        {
            var result = new ROICropResults();
            var sw = Stopwatch.StartNew();
            try
            {
                if (image == null || image.Empty()) { result.SetError("输入图像为空"); return result; }
                
                var roi = new Rect(parameters.X, parameters.Y, parameters.Width, parameters.Height);
                // 限制在图像范围内
                roi = roi.Intersect(new Rect(0, 0, image.Width, image.Height));
                
                result.OutputImage = new Mat(image, roi).Clone();
                result.CroppedArea = roi;
                result.SetSuccess(sw.ElapsedMilliseconds);
            }
            catch (Exception ex) { result.SetError($"处理失败: {ex.Message}"); }
            return result;
        }

        public ValidationResult ValidateParameters(ROICropParameters parameters) => parameters.Validate();
        public ROICropParameters GetDefaultParameters() => new ROICropParameters();
    }
}
