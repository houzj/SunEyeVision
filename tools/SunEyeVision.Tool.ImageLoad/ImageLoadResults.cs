using System;
using System.Collections.Generic;
using OpenCvSharp;
using SunEyeVision.Plugin.SDK.Execution.Results;
using SunEyeVision.Plugin.SDK.Models.Visualization;

namespace SunEyeVision.Tool.ImageLoad
{
    /// <summary>
    /// 图像载入工具执行结果 - 简化版
    /// 只包含图像信息和基本元数据
    /// </summary>
    public class ImageLoadResults : ToolResults
    {
        /// <summary>
        /// 载入的图像
        /// </summary>
        public Mat? OutputImage { get; set; }

        /// <summary>
        /// 图像宽度
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// 图像高度
        /// </summary>
        public int Height { get; set; }

        /// <summary>
        /// 通道数
        /// </summary>
        public int Channels { get; set; }

        /// <summary>
        /// 图像深度
        /// </summary>
        public int Depth { get; set; }

        /// <summary>
        /// 文件大小(字节)
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// 文件格式
        /// </summary>
        public string? FileFormat { get; set; }

        /// <summary>
        /// 文件路径
        /// </summary>
        public string? FilePath { get; set; }

        /// <summary>
        /// 获取结果项列表
        /// </summary>
        public override IReadOnlyList<ResultItem> GetResultItems()
        {
            var items = new List<ResultItem>();

            items.AddText("FilePath", FilePath ?? "");
            items.AddNumeric("Width", Width, "像素");
            items.AddNumeric("Height", Height, "像素");
            items.AddNumeric("Channels", Channels);
            items.AddText("Depth", Depth.ToString());
            items.AddNumeric("FileSize", FileSize, "字节");

            if (!string.IsNullOrEmpty(FileFormat))
                items.AddText("FileFormat", FileFormat);

            items.AddNumeric("ExecutionTimeMs", ExecutionTimeMs, "ms");

            return items;
        }

        /// <summary>
        /// 获取可视化元素
        /// </summary>
        public override IEnumerable<VisualElement> GetVisualElements()
        {
            yield break;
        }

        /// <summary>
        /// 转换为字典
        /// </summary>
        public override Dictionary<string, object> ToDictionary()
        {
            var dict = base.ToDictionary();
            dict["Width"] = Width;
            dict["Height"] = Height;
            dict["Channels"] = Channels;
            dict["Depth"] = Depth;
            dict["FileSize"] = FileSize;
            dict["FileFormat"] = FileFormat ?? "";
            dict["FilePath"] = FilePath ?? "";
            return dict;
        }
    }
}
