using System.Collections.Generic;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.SDK.Validation;

namespace SunEyeVision.Tool.Threshold
{
    /// <summary>
    /// 阈值类型枚举
    /// </summary>
    public enum ThresholdType
    {
        Binary = 0,
        BinaryInv = 1,
        Trunc = 2,
        ToZero = 3,
        ToZeroInv = 4
    }

    /// <summary>
    /// 自适应方法枚举
    /// </summary>
    public enum AdaptiveMethod
    {
        Mean = 0,
        Gaussian = 1
    }

    /// <summary>
    /// 图像阈值化工具参数
    /// </summary>
    public class ThresholdParameters : ToolParameters
    {
        #region 基本参数

        /// <summary>
        /// 二值化阈值(0-255)
        /// </summary>
        [ParameterRange(0, 255, Step = 1)]
        [ParameterDisplay(DisplayName = "阈值", Description = "二值化的阈值", Group = "基本参数", Order = 1)]
        public int Threshold { get; set; } = 128;

        /// <summary>
        /// 超过阈值时使用的最大值(0-255)
        /// </summary>
        [ParameterRange(0, 255, Step = 1)]
        [ParameterDisplay(DisplayName = "最大值", Description = "超过阈值时使用的最大值", Group = "基本参数", Order = 2)]
        public int MaxValue { get; set; } = 255;

        /// <summary>
        /// 二值化方法
        /// </summary>
        [ParameterDisplay(DisplayName = "阈值类型", Description = "二值化方法", Group = "基本参数", Order = 3)]
        public ThresholdType Type { get; set; } = ThresholdType.Binary;

        /// <summary>
        /// 是否反转二值化结果
        /// </summary>
        [ParameterDisplay(DisplayName = "反转结果", Description = "是否反转二值化结果", Group = "基本参数", Order = 6)]
        public bool Invert { get; set; } = false;

        #endregion

        #region 高级参数

        /// <summary>
        /// 自适应阈值方法
        /// </summary>
        [ParameterDisplay(DisplayName = "自适应方法", Description = "自适应阈值方法", Group = "高级参数", Order = 4, IsAdvanced = true)]
        public AdaptiveMethod AdaptiveMethod { get; set; } = AdaptiveMethod.Mean;

        /// <summary>
        /// 计算阈值的邻域大小(奇数, 3-31)
        /// </summary>
        [ParameterRange(3, 31, Step = 2)]
        [ParameterDisplay(DisplayName = "块大小", Description = "计算阈值的邻域大小(奇数)", Group = "高级参数", Order = 5, IsAdvanced = true)]
        public int BlockSize { get; set; } = 11;

        #endregion

        /// <summary>
        /// 验证参数
        /// </summary>
        public override ValidationResult Validate()
        {
            var result = base.Validate();

            // 块大小必须为奇数
            if (BlockSize % 2 == 0)
            {
                result.AddError("块大小必须为奇数");
            }

            return result;
        }

        /// <summary>
        /// 获取阈值类型选项
        /// </summary>
        public static IReadOnlyList<string> GetThresholdTypeOptions()
        {
            return new List<string> { "Binary", "BinaryInv", "Trunc", "ToZero", "ToZeroInv" };
        }

        /// <summary>
        /// 获取自适应方法选项
        /// </summary>
        public static IReadOnlyList<string> GetAdaptiveMethodOptions()
        {
            return new List<string> { "Mean", "Gaussian" };
        }
    }
}
