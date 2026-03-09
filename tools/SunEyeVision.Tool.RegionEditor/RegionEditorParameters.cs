using System.Collections.Generic;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.SDK.UI.Controls.Region.Models;
using SunEyeVision.Plugin.SDK.Validation;

namespace SunEyeVision.Tool.RegionEditor
{
    /// <summary>
    /// 区域编辑器参数
    /// </summary>
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
