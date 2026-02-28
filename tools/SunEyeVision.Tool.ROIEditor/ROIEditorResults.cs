using System.Collections.Generic;
using OpenCvSharp;
using SunEyeVision.Plugin.SDK.Execution.Results;
using SunEyeVision.Plugin.SDK.Validation;

namespace SunEyeVision.Tool.ROIEditor
{
    /// <summary>
    /// ROI编辑器结果
    /// </summary>
    public class ROIEditorResults : ToolResults
    {
        /// <summary>
        /// 编辑后的ROI列表
        /// </summary>
        public List<ROIData> ROIs { get; set; } = [];

        /// <summary>
        /// ROI数量
        /// </summary>
        public int ROICount { get; set; }

        /// <summary>
        /// 输出图像（带ROI标记）
        /// </summary>
        public Mat? OutputImage { get; set; }

        public override IReadOnlyList<ResultItem> GetResultItems()
        {
            var items = new List<ResultItem>();

            items.Add(new ResultItem
            {
                Name = "ROICount",
                DisplayName = "ROI数量",
                Value = ROICount,
                Type = ResultItemType.Numeric,
                Unit = "个"
            });

            for (int i = 0; i < ROIs.Count; i++)
            {
                var roi = ROIs[i];
                items.Add(new ResultItem
                {
                    Name = $"ROI_{i}",
                    DisplayName = $"ROI {i + 1}",
                    Value = $"{roi.Type}: ({roi.X:F1}, {roi.Y:F1})",
                    Type = ResultItemType.Text
                });
            }

            return items;
        }
    }
}
