using System.Collections.Generic;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.SDK.Validation;
using System.Text.Json.Serialization;

namespace SunEyeVision.Tool.ROIEditor
{
    /// <summary>
    /// ROI编辑器参数
    /// </summary>
    /// <remarks>
    /// 多态序列化（rule-010: 方案系统实现规范）：
    /// 使用 [JsonDerivedType] 特性标识参数类型，类型标识符为 "ROIEditor"。
    /// System.Text.Json 会自动添加 "$type" 字段并在反序列化时识别。
    /// </remarks>
    [JsonDerivedType(typeof(ROIEditorParameters), "ROIEditor")]
    public class ROIEditorParameters : ToolParameters
    {
        /// <summary>
        /// ROI数据列表
        /// </summary>
        public List<ROIData> ROIs { get; set; } = [];

        /// <summary>
        /// 编辑模式：Edit 或 Inherit
        /// </summary>
        public string Mode { get; set; } = "Edit";

        /// <summary>
        /// 是否显示网格
        /// </summary>
        public bool ShowGrid { get; set; } = false;

        /// <summary>
        /// 是否启用吸附
        /// </summary>
        public bool EnableSnap { get; set; } = true;

        /// <summary>
        /// 网格大小
        /// </summary>
        public int GridSize { get; set; } = 10;

        public override ValidationResult Validate()
        {
            return new ValidationResult();
        }
    }

    /// <summary>
    /// ROI数据结构
    /// </summary>
    public class ROIData
    {
        public string Type { get; set; } = "Rectangle";
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public double Radius { get; set; }
        public double Rotation { get; set; }
        public double EndX { get; set; }
        public double EndY { get; set; }
        public string Name { get; set; } = "";
        public string Tag { get; set; } = "";
    }
}
