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
using SunEyeVision.Tool.ROICrop.Views;
using System.Text.Json.Serialization;

namespace SunEyeVision.Tool.ROICrop
{
    /// <summary>
    /// ROI裁剪参数
    /// </summary>
    /// <remarks>
    /// 多态序列化（rule-010: 方案系统实现规范）：
    /// 使用 [JsonDerivedType] 特性标识参数类型，类型标识符为 "ROICrop"。
    /// </remarks>
    [JsonDerivedType(typeof(ROICropParameters), "ROICrop")]
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

    [Tool("roi_crop", "ROI裁剪", Description = "裁剪指定的矩形感兴趣区域", Icon = "✂️", Category = "图像处理")]
    public class ROICropTool : IToolPlugin<ROICropParameters, ROICropResults>
    {
        public bool HasDebugWindow => true;

        public FrameworkElement? CreateDebugControl()
        {
            return new ROICropToolDebugWindow();
        }

        [Obsolete("使用 CreateDebugControl 替代")]
        public System.Windows.Window? CreateDebugWindow()
        {
            return new ROICropToolDebugWindow();
        }

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
    }
}
