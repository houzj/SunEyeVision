using System.Collections.Generic;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.SDK.Validation;

namespace SunEyeVision.Tool.EdgeDetection
{
    /// <summary>
    /// 边缘检测工具参数
    /// </summary>
    public class EdgeDetectionParameters : ToolParameters
    {
        #region 基本参数

        /// <summary>
        /// 第一个滞后阈值(0-255)
        /// </summary>
        [ParameterRange(0, 255, Step = 1, Unit = "灰度值")]
        [ParameterDisplay(DisplayName = "低阈值", Description = "Canny边缘检测的第一个滞后阈值", Group = "基本参数", Order = 1)]
        public double Threshold1 { get; set; } = 50.0;

        /// <summary>
        /// 第二个滞后阈值(0-255)
        /// </summary>
        [ParameterRange(0, 255, Step = 1, Unit = "灰度值")]
        [ParameterDisplay(DisplayName = "高阈值", Description = "Canny边缘检测的第二个滞后阈值", Group = "基本参数", Order = 2)]
        public double Threshold2 { get; set; } = 150.0;

        #endregion

        #region 高级参数

        /// <summary>
        /// Sobel算子的孔径大小(3、5、7)
        /// </summary>
        [ParameterRange(3, 7, Step = 2)]
        [ParameterDisplay(DisplayName = "孔径大小", Description = "Sobel算子的孔径大小(3、5、7)", Group = "高级参数", Order = 3, IsAdvanced = true)]
        public int ApertureSize { get; set; } = 3;

        #endregion

        /// <summary>
        /// 验证参数
        /// </summary>
        public override ValidationResult Validate()
        {
            var result = base.Validate();

            if (Threshold1 < 0 || Threshold1 > 255)
                result.AddError("低阈值必须在0-255之间");

            if (Threshold2 < 0 || Threshold2 > 255)
                result.AddError("高阈值必须在0-255之间");

            if (ApertureSize != 3 && ApertureSize != 5 && ApertureSize != 7)
                result.AddError("孔径大小必须是3、5或7");

            if (Threshold1 > Threshold2)
                result.AddWarning("低阈值大于高阈值，可能影响检测效果");

            return result;
        }

        /// <summary>
        /// 获取孔径大小选项
        /// </summary>
        public static IReadOnlyList<int> GetApertureSizeOptions()
        {
            return new List<int> { 3, 5, 7 };
        }
    }
}
