using System;
using System.Collections.Generic;
using OpenCvSharp;
using SunEyeVision.Plugin.SDK.Execution.Results;
using SunEyeVision.Plugin.SDK.Models.Visualization;

namespace SunEyeVision.Tool.EdgeDetection
{
    /// <summary>
    /// 边缘检测工具执行结果
    /// </summary>
    public class EdgeDetectionResults : ToolResults
    {
        /// <summary>
        /// 输出图像（边缘检测结果）
        /// </summary>
        public Mat? OutputImage { get; set; }

        /// <summary>
        /// 检测到的边缘轮廓数量
        /// </summary>
        public int EdgeCount { get; set; }

        /// <summary>
        /// 实际使用的低阈值
        /// </summary>
        public double Threshold1Used { get; set; }

        /// <summary>
        /// 实际使用的高阈值
        /// </summary>
        public double Threshold2Used { get; set; }

        /// <summary>
        /// 实际使用的孔径大小
        /// </summary>
        public int ApertureSizeUsed { get; set; }

        /// <summary>
        /// 输入图像尺寸
        /// </summary>
        public OpenCvSharp.Size InputSize { get; set; }

        /// <summary>
        /// 处理时间戳
        /// </summary>
        public DateTime ProcessedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// 获取结果项列表
        /// </summary>
        public override IReadOnlyList<ResultItem> GetResultItems()
        {
            var items = new List<ResultItem>();

            items.AddNumeric("EdgeCount", EdgeCount, "个");
            items.AddNumeric("Threshold1Used", Threshold1Used, "灰度值");
            items.AddNumeric("Threshold2Used", Threshold2Used, "灰度值");
            items.AddNumeric("ApertureSizeUsed", ApertureSizeUsed, "");
            items.AddNumeric("InputWidth", InputSize.Width, "像素");
            items.AddNumeric("InputHeight", InputSize.Height, "像素");
            items.AddNumeric("ExecutionTimeMs", ExecutionTimeMs, "ms");

            return items;
        }

        /// <summary>
        /// 获取可视化元素
        /// </summary>
        public override IEnumerable<VisualElement> GetVisualElements()
        {
            // 可以添加边缘轮廓的可视化标注
            yield break;
        }

        /// <summary>
        /// 转换为字典
        /// </summary>
        public override Dictionary<string, object> ToDictionary()
        {
            var dict = base.ToDictionary();
            dict["EdgeCount"] = EdgeCount;
            dict["Threshold1Used"] = Threshold1Used;
            dict["Threshold2Used"] = Threshold2Used;
            dict["ApertureSizeUsed"] = ApertureSizeUsed;
            dict["InputWidth"] = InputSize.Width;
            dict["InputHeight"] = InputSize.Height;
            dict["ProcessedAt"] = ProcessedAt;
            return dict;
        }
    }
}
