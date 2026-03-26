using System.Collections.Generic;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.SDK.UI.Controls.Region.Models;
using SunEyeVision.Plugin.SDK.Validation;
using System.Text.Json.Serialization;

namespace SunEyeVision.Tool.RegionEditor
{
    /// <summary>
    /// 区域编辑器参数
    /// </summary>
    /// <remarks>
    /// 多态序列化（rule-010: 方案系统实现规范）：
    /// 使用 [JsonDerivedType] 特性标识参数类型，类型标识符为 "RegionEditor"。
    /// </remarks>
    [JsonDerivedType(typeof(RegionEditorParameters), "RegionEditor")]
    public class RegionEditorParameters : ToolParameters
    {
        /// <summary>
        /// 区域数据列表
        /// </summary>
        public List<RegionData> Regions { get; set; } = new();

        /// <summary>
        /// 是否启用实时预览
        /// </summary>
        public bool EnableRealtimePreview { get; set; } = true;

        /// <summary>
        /// 默认显示颜色（ARGB格式）
        /// </summary>
        public uint DefaultDisplayColor { get; set; } = 0xFFFF0000;

        /// <summary>
        /// 默认透明度
        /// </summary>
        public double DefaultOpacity { get; set; } = 0.3;

        public override ValidationResult Validate()
        {
            var result = new ValidationResult();
            
            // 验证区域数据
            foreach (var region in Regions)
            {
                if (string.IsNullOrEmpty(region.Name))
                {
                    result.AddError($"区域 {region.Id} 名称为空");
                }

                if (region.Parameters == null)
                {
                    result.AddError($"区域 {region.Name} 参数为空");
                }
            }

            return result;
        }
    }
}
