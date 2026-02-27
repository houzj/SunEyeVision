using System.Collections.Generic;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.SDK.Validation;

namespace SunEyeVision.Tool.ImageLoad
{
    /// <summary>
    /// 图像载入工具参数 - 简化版，只包含文件路径
    /// 图像处理（灰度转换、缩放等）由下游工具实现
    /// </summary>
    public class ImageLoadParameters : ToolParameters
    {
        /// <summary>
        /// 图像文件路径
        /// </summary>
        [ParameterDisplay(DisplayName = "文件路径", Description = "要载入的图像文件路径", Group = "基本参数", Order = 1)]
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// 验证参数
        /// </summary>
        public override ValidationResult Validate()
        {
            var result = base.Validate();

            if (string.IsNullOrEmpty(FilePath))
            {
                result.AddError("文件路径不能为空");
            }

            return result;
        }

        /// <summary>
        /// 获取支持的图像格式
        /// </summary>
        public static IReadOnlyList<string> GetSupportedFormats()
        {
            return new List<string>
            {
                ".bmp", ".jpg", ".jpeg", ".png", ".tiff", ".tif", ".gif", ".webp"
            };
        }
    }
}
