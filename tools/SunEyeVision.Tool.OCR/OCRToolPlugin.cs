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
using SunEyeVision.Tool.OCR.Views;

namespace SunEyeVision.Tool.OCR
{
    public class OCRParameters : ToolParameters
    {
        public string Language { get; set; } = "CN";
        public double ConfThreshold { get; set; } = 80.0;
        public override ValidationResult Validate() => new ValidationResult();
    }

    public class OCRResults : ToolResults
    {
        public string Text { get; set; } = "";
        public double Confidence { get; set; }
    }

    [Tool("ocr_recognition", "OCR识别", Description = "光学字符识别", Icon = "📝", Category = "识别")]
    public class OCRTool : IToolPlugin<OCRParameters, OCRResults>
    {
        public bool HasDebugWindow => true;

        public System.Windows.Window? CreateDebugWindow()
        {
            return new OCRToolDebugWindow();
        }

        public OCRResults Run(Mat image, OCRParameters parameters)
        {
            var result = new OCRResults();
            var sw = Stopwatch.StartNew();
            try
            {
                if (image == null || image.Empty()) { result.SetError("输入图像为空"); return result; }
                // TODO: 实际OCR实现
                result.Text = "OCR功能待实现";
                result.Confidence = 0.0;
                result.SetSuccess(sw.ElapsedMilliseconds);
            }
            catch (Exception ex) { result.SetError($"处理失败: {ex.Message}"); }
            return result;
        }
    }
}
