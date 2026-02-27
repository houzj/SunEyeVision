using System;
using System.Collections.Generic;
using OpenCvSharp;
using SunEyeVision.Plugin.SDK.Execution.Results;
using SunEyeVision.Plugin.SDK.Models.Visualization;

namespace SunEyeVision.Tool.Threshold
{
    /// <summary>
    /// 图像阈值化工具执行结果
    /// </summary>
    public class ThresholdResults : ToolResults
    {
        /// <summary>
        /// 输出图像（二值化后的图像）
        /// </summary>
        public Mat? OutputImage { get; set; }

        /// <summary>
        /// 实际使用的阈值
        /// </summary>
        public double ThresholdUsed { get; set; }

        /// <summary>
        /// 最大值
        /// </summary>
        public int MaxValueUsed { get; set; }

        /// <summary>
        /// 使用的阈值类型
        /// </summary>
        public ThresholdType TypeUsed { get; set; }

        /// <summary>
        /// 使用的自适应方法
        /// </summary>
        public AdaptiveMethod AdaptiveMethodUsed { get; set; }

        /// <summary>
        /// 使用的块大小
        /// </summary>
        public int BlockSizeUsed { get; set; }

        /// <summary>
        /// 是否反转
        /// </summary>
        public bool InvertUsed { get; set; }

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

            items.AddNumeric("ThresholdUsed", ThresholdUsed, "灰度值");
            items.AddNumeric("MaxValueUsed", MaxValueUsed, "灰度值");
            items.AddText("ThresholdType", TypeUsed.ToString());
            items.AddNumeric("InputWidth", InputSize.Width, "像素");
            items.AddNumeric("InputHeight", InputSize.Height, "像素");

            if (AdaptiveMethodUsed != AdaptiveMethod.Mean || BlockSizeUsed != 11)
            {
                items.AddText("AdaptiveMethod", AdaptiveMethodUsed.ToString());
                items.AddNumeric("BlockSize", BlockSizeUsed, "像素");
            }

            items.AddBoolean("Inverted", InvertUsed);
            items.AddNumeric("ExecutionTimeMs", ExecutionTimeMs, "ms");

            return items;
        }

        /// <summary>
        /// 获取可视化元素
        /// </summary>
        public override IEnumerable<VisualElement> GetVisualElements()
        {
            // 可以添加阈值信息的可视化标注
            yield break;
        }

        /// <summary>
        /// 转换为字典
        /// </summary>
        public override Dictionary<string, object> ToDictionary()
        {
            var dict = base.ToDictionary();
            dict["ThresholdUsed"] = ThresholdUsed;
            dict["MaxValueUsed"] = MaxValueUsed;
            dict["TypeUsed"] = TypeUsed.ToString();
            dict["AdaptiveMethodUsed"] = AdaptiveMethodUsed.ToString();
            dict["BlockSizeUsed"] = BlockSizeUsed;
            dict["InvertUsed"] = InvertUsed;
            dict["ProcessedAt"] = ProcessedAt;
            return dict;
        }
    }
}
