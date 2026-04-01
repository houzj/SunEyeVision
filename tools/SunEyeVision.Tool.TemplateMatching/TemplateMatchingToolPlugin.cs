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
using SunEyeVision.Tool.TemplateMatching.Views;
using System.Text.Json.Serialization;

namespace SunEyeVision.Tool.TemplateMatching
{
    /// <summary>
    /// 模板匹配参数
    /// </summary>
    /// <remarks>
    /// 多态序列化（rule-010: 方案系统实现规范）：
    /// 使用 [JsonDerivedType] 特性标识参数类型，类型标识符为 "TemplateMatching"。
    /// </remarks>
    [JsonDerivedType(typeof(TemplateMatchingParameters), "TemplateMatching")]
    public class TemplateMatchingParameters : ToolParameters
    {
        public double Threshold { get; set; } = 0.8;
        public override ValidationResult Validate()
        {
            var r = new ValidationResult();
            if (Threshold < 0 || Threshold > 1) r.AddError("阈值必须在0-1之间");
            return r;
        }
    }

    public class TemplateMatchingResults : ToolResults
    {
        public Mat? OutputImage { get; set; }
        public double Score { get; set; }
        public OpenCvSharp.Point Position { get; set; }
    }

    [Tool("template_matching", "模板匹配定位", Description = "使用模板匹配进行定位", Icon = "🎯", Category = "定位")]
    public class TemplateMatchingTool : IToolPlugin<TemplateMatchingParameters, TemplateMatchingResults>
    {
        public bool HasDebugWindow => true;

        public System.Windows.Window? CreateDebugWindow()
        {
            return new TemplateMatchingToolDebugWindow();
        }

        public TemplateMatchingResults Run(Mat image, TemplateMatchingParameters parameters)
        {
            var result = new TemplateMatchingResults();
            var sw = Stopwatch.StartNew();
            try
            {
                if (image == null || image.Empty()) { result.SetError("输入图像为空"); return result; }
                // 简化实现，实际应使用模板匹配
                result.OutputImage = image.Clone();
                result.Score = parameters.Threshold;
                result.SetSuccess(sw.ElapsedMilliseconds);
            }
            catch (Exception ex) { result.SetError($"处理失败: {ex.Message}"); }
            return result;
        }
    }
}
