using System.Collections.Generic;
using OpenCvSharp;
using SunEyeVision.Plugin.SDK.Execution.Results;
using SunEyeVision.Plugin.SDK.Metadata;
using SunEyeVision.Plugin.SDK.UI.Controls.Region.Logic;
using SunEyeVision.Plugin.SDK.UI.Controls.Region.Models;

namespace SunEyeVision.Tool.RegionEditor
{
    /// <summary>
    /// 区域编辑器结果
    /// </summary>
    public class RegionEditorResults : ToolResults
    {
        /// <summary>
        /// 编辑后的区域列表
        /// </summary>
        public List<RegionData> Regions { get; set; } = new();

        /// <summary>
        /// 区域数量
        /// </summary>
        public int RegionCount { get; set; }

        /// <summary>
        /// 解析后的区域列表（用于后续处理）
        /// </summary>
        public List<ResolvedRegion> ResolvedRegions { get; set; } = new();

        /// <summary>
        /// 输出图像（带区域标记）
        /// </summary>
        [Param(DisplayName = "输出图像", Description = "带区域标记的图像", Category = ParamCategory.Output)]
        public Mat? OutputImage { get; set; }

        public override IReadOnlyList<ResultItem> GetResultItems()
        {
            var items = new List<ResultItem>();

            items.Add(new ResultItem
            {
                Name = "RegionCount",
                DisplayName = "区域数量",
                Value = RegionCount,
                Type = ResultItemType.Numeric,
                Unit = "个"
            });

            for (int i = 0; i < Regions.Count; i++)
            {
                var region = Regions[i];
                var shapeType = region.GetShapeType()?.ToString() ?? "Unknown";
                var mode = region.GetMode();

                items.Add(new ResultItem
                {
                    Name = $"Region_{i}",
                    DisplayName = $"区域 {i + 1}",
                    Value = $"{region.Name}: {shapeType} ({mode})",
                    Type = ResultItemType.Text
                });

                // 如果是绘制模式，显示几何参数
                if (region.Parameters is ShapeParameters shapeDef)
                {
                    items.Add(new ResultItem
                    {
                        Name = $"Region_{i}_Center",
                        DisplayName = $"  中心点",
                        Value = $"({shapeDef.CenterX:F1}, {shapeDef.CenterY:F1})",
                        Type = ResultItemType.Text
                    });

                    if (shapeDef.ShapeType == ShapeType.Rectangle || 
                        shapeDef.ShapeType == ShapeType.RotatedRectangle)
                    {
                        items.Add(new ResultItem
                        {
                            Name = $"Region_{i}_Size",
                            DisplayName = $"  尺寸",
                            Value = $"{shapeDef.Width:F1} x {shapeDef.Height:F1}",
                            Type = ResultItemType.Text
                        });

                        if (shapeDef.ShapeType == ShapeType.RotatedRectangle)
                        {
                            items.Add(new ResultItem
                            {
                                Name = $"Region_{i}_Angle",
                                DisplayName = $"  角度",
                                Value = shapeDef.Angle,
                                Type = ResultItemType.Numeric,
                                Unit = "°"
                            });
                        }
                    }
                    else if (shapeDef.ShapeType == ShapeType.Circle)
                    {
                        items.Add(new ResultItem
                        {
                            Name = $"Region_{i}_Radius",
                            DisplayName = $"  半径",
                            Value = shapeDef.Radius,
                            Type = ResultItemType.Numeric
                        });
                    }
                    else if (shapeDef.ShapeType == ShapeType.Line)
                    {
                        items.Add(new ResultItem
                        {
                            Name = $"Region_{i}_Start",
                            DisplayName = $"  起点",
                            Value = $"({shapeDef.StartX:F1}, {shapeDef.StartY:F1})",
                            Type = ResultItemType.Text
                        });
                        items.Add(new ResultItem
                        {
                            Name = $"Region_{i}_End",
                            DisplayName = $"  终点",
                            Value = $"({shapeDef.EndX:F1}, {shapeDef.EndY:F1})",
                            Type = ResultItemType.Text
                        });
                    }
                }
            }

            return items;
        }
    }
}
