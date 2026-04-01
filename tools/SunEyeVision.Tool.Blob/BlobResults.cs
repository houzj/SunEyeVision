using System;
using System.Collections.Generic;
using SunEyeVision.Plugin.SDK.Execution.Results;

namespace SunEyeVision.Tool.Blob
{
    /// <summary>
    /// Blob检测结果
    /// </summary>
    public class BlobResults : ToolResults
    {
        /// <summary>
        /// 检测到的Blob列表
        /// </summary>
        public List<BlobResult> Blobs { get; set; } = new();

        /// <summary>
        /// Blob总数
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// 叠加显示图像（Blob轮廓绘制在原图上）
        /// </summary>
        public OpenCvSharp.Mat? OverlayImage { get; set; }

        /// <summary>
        /// 获取结果项列表
        /// </summary>
        public override IReadOnlyList<ResultItem> GetResultItems()
        {
            var items = new List<ResultItem>();
            items.AddNumeric("TotalCount", TotalCount, "");
            items.AddNumeric("Blobs", Blobs.Count, "");
            items.AddNumeric("ExecutionTimeMs", ExecutionTimeMs, "ms");
            return items;
        }
    }

    /// <summary>
    /// 单个Blob检测结果
    /// </summary>
    public class BlobResult
    {
        /// <summary>
        /// Blob序号
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Blob面积
        /// </summary>
        public double Area { get; set; }

        /// <summary>
        /// 中心点X坐标
        /// </summary>
        public double CenterX { get; set; }

        /// <summary>
        /// 中心点Y坐标
        /// </summary>
        public double CenterY { get; set; }

        /// <summary>
        /// 边界矩形宽度
        /// </summary>
        public double Width { get; set; }

        /// <summary>
        /// 边界矩形高度
        /// </summary>
        public double Height { get; set; }

        /// <summary>
        /// 圆度（0-1）
        /// </summary>
        public double Circularity { get; set; }

        /// <summary>
        /// 凸度（0-1）
        /// </summary>
        public double Convexity { get; set; }

        /// <summary>
        /// 惯性比率
        /// </summary>
        public double InertiaRatio { get; set; }

        /// <summary>
        /// 是否可见（用于UI显示控制）
        /// </summary>
        public bool IsVisible { get; set; } = true;

        /// <summary>
        /// 边界矩形
        /// </summary>
        public OpenCvSharp.Rect BoundingRect { get; set; }

        /// <summary>
        /// 轮廓点集
        /// </summary>
        public List<OpenCvSharp.Point> Contour { get; set; } = new();
    }
}
