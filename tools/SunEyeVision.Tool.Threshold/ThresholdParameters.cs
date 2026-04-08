using System.Collections.Generic;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.SDK.Validation;
using System.Text.Json.Serialization;
using SunEyeVision.Tool.Threshold.Models;

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
    /// <remarks>
    /// 参数类继承 ObservableObject，自带属性变化通知，UI 可直接绑定。
    /// 使用 SetProperty 实现属性，自动触发 PropertyChanged 事件和日志记录。
    /// 
    /// 多态序列化（rule-010: 方案系统实现规范）：
    /// 使用 [JsonDerivedType] 特性标识参数类型，类型标识符为 "Threshold"。
    /// System.Text.Json 会自动添加 "$type" 字段并在反序列化时识别。
    /// 
    /// 参数验证流程：
    /// 1. Setter 自动截断：值域范围自动限制（如 0-255）
    /// 2. 关联参数修正：确保 MinValue < MaxValue
    /// 3. Validate() 方法：完整验证所有约束条件
    /// </remarks>
    [JsonDerivedType(typeof(ThresholdParameters), "Threshold")]
    public class ThresholdParameters : ToolParameters
    {
        #region 常量定义

        /// <summary>
        /// 阈值最小值
        /// </summary>
        public const int ThresholdMin = 0;

        /// <summary>
        /// 阈值最大值
        /// </summary>
        public const int ThresholdMax = 255;

        /// <summary>
        /// 块大小最小值
        /// </summary>
        public const int BlockSizeMin = 3;

        /// <summary>
        /// 块大小最大值
        /// </summary>
        public const int BlockSizeMax = 31;

        #endregion

        #region 私有字段

        private int _threshold = 128;
        private int _maxValue = 255;
        private ThresholdType _type = ThresholdType.Binary;
        private bool _invert = false;
        private AdaptiveMethod _adaptiveMethod = AdaptiveMethod.Mean;
        private int _blockSize = 11;

        #endregion

        #region 基本参数

        /// <summary>
        /// 二值化阈值(0-255)
        /// </summary>
        /// <remarks>
        /// 自动截断到有效范围 [0, 255]
        /// </remarks>
        [JsonPropertyName("threshold")]
        public int Threshold
        {
            get => _threshold;
            set
            {
                // 自动截断到有效范围
                var clampedValue = Math.Clamp(value, ThresholdMin, ThresholdMax);
                SetProperty(ref _threshold, clampedValue, "阈值");
            }
        }

        /// <summary>
        /// 超过阈值时使用的最大值(0-255)
        /// </summary>
        /// <remarks>
        /// 自动截断到有效范围 [0, 255]
        /// </remarks>
        [JsonPropertyName("maxValue")]
        public int MaxValue
        {
            get => _maxValue;
            set
            {
                // 自动截断到有效范围
                var clampedValue = Math.Clamp(value, ThresholdMin, ThresholdMax);
                SetProperty(ref _maxValue, clampedValue, "最大值");
            }
        }

        /// <summary>
        /// 二值化方法
        /// </summary>
        [JsonPropertyName("type")]
        public ThresholdType Type
        {
            get => _type;
            set => SetProperty(ref _type, value, "阈值类型");
        }

        /// <summary>
        /// 是否反转二值化结果
        /// </summary>
        [JsonPropertyName("invert")]
        public bool Invert
        {
            get => _invert;
            set => SetProperty(ref _invert, value, "反转结果");
        }

        #endregion

        #region 高级参数

        /// <summary>
        /// 自适应阈值方法
        /// </summary>
        [JsonPropertyName("adaptiveMethod")]
        public AdaptiveMethod AdaptiveMethod
        {
            get => _adaptiveMethod;
            set => SetProperty(ref _adaptiveMethod, value, "自适应方法");
        }

        /// <summary>
        /// 计算阈值的邻域大小(奇数, 3-31)
        /// </summary>
        /// <remarks>
        /// 自动修正：确保为奇数，且在 [3, 31] 范围内
        /// </remarks>
        [JsonPropertyName("blockSize")]
        public int BlockSize
        {
            get => _blockSize;
            set
            {
                // 自动截断到有效范围
                var clampedValue = Math.Clamp(value, BlockSizeMin, BlockSizeMax);
                
                // 确保块大小为奇数
                if (clampedValue % 2 == 0)
                {
                    // 如果是偶数，向上调整为奇数（不超过最大值）
                    if (clampedValue < BlockSizeMax)
                        clampedValue++;
                    else
                        clampedValue--;
                }
                
                SetProperty(ref _blockSize, clampedValue, "块大小");
            }
        }

        #endregion

        #region 结果显示配置

        /// <summary>
        /// 结果判断配置
        /// </summary>
        [JsonPropertyName("resultConfig")]
        public ThresholdResultConfig ResultConfig { get; set; } = new();

        /// <summary>
        /// 图像显示配置
        /// </summary>
        [JsonPropertyName("displayConfig")]
        public ThresholdDisplayConfig DisplayConfig { get; set; } = new();

        /// <summary>
        /// 文本显示配置
        /// </summary>
        [JsonPropertyName("textConfig")]
        public ThresholdTextConfig TextConfig { get; set; } = new();

        #endregion

        /// <summary>
        /// 验证参数
        /// </summary>
        /// <remarks>
        /// 验证规则：
        /// 1. 块大小必须为奇数
        /// 2. Threshold 在 [0, 255] 范围内
        /// 3. MaxValue 在 [0, 255] 范围内
        /// 4. BlockSize 在 [3, 31] 范围内
        /// 5. ResultConfig 各项范围值有效（MinValue &lt; MaxValue）
        /// </remarks>
        public override ValidationResult Validate()
        {
            var result = base.Validate();

            // 块大小必须为奇数
            if (BlockSize % 2 == 0)
            {
                result.AddError($"块大小必须为奇数，当前值: {BlockSize}");
            }

            // 块大小范围检查
            if (BlockSize < BlockSizeMin || BlockSize > BlockSizeMax)
            {
                result.AddError($"块大小必须在 {BlockSizeMin}-{BlockSizeMax} 范围内，当前值: {BlockSize}");
            }

            // 阈值范围检查
            if (Threshold < ThresholdMin || Threshold > ThresholdMax)
            {
                result.AddError($"阈值必须在 {ThresholdMin}-{ThresholdMax} 范围内，当前值: {Threshold}");
            }

            // 最大值范围检查
            if (MaxValue < ThresholdMin || MaxValue > ThresholdMax)
            {
                result.AddError($"最大值必须在 {ThresholdMin}-{ThresholdMax} 范围内，当前值: {MaxValue}");
            }

            // 结果配置验证
            ValidateResultConfig(result);

            return result;
        }

        /// <summary>
        /// 验证结果判断配置
        /// </summary>
        private void ValidateResultConfig(ValidationResult result)
        {
            if (ResultConfig == null)
            {
                result.AddError("结果判断配置不能为空");
                return;
            }

            // 白色像素比例范围验证
            if (ResultConfig.IsWhitePixelRatioCheckEnabled)
            {
                if (ResultConfig.WhitePixelRatioMin >= ResultConfig.WhitePixelRatioMax)
                {
                    result.AddError($"白色像素比例最小值({ResultConfig.WhitePixelRatioMin})必须小于最大值({ResultConfig.WhitePixelRatioMax})");
                }
            }

            // 输出均值范围验证
            if (ResultConfig.IsMeanCheckEnabled)
            {
                if (ResultConfig.MeanMin >= ResultConfig.MeanMax)
                {
                    result.AddError($"输出均值最小值({ResultConfig.MeanMin})必须小于最大值({ResultConfig.MeanMax})");
                }
            }

            // 输出面积范围验证
            if (ResultConfig.IsAreaCheckEnabled)
            {
                if (ResultConfig.AreaMin >= ResultConfig.AreaMax)
                {
                    result.AddError($"输出面积最小值({ResultConfig.AreaMin})必须小于最大值({ResultConfig.AreaMax})");
                }
            }

            // 质心X范围验证
            if (ResultConfig.IsCentroidXCheckEnabled)
            {
                if (ResultConfig.CentroidXMin >= ResultConfig.CentroidXMax)
                {
                    result.AddError($"质心X最小值({ResultConfig.CentroidXMin})必须小于最大值({ResultConfig.CentroidXMax})");
                }
            }

            // 质心Y范围验证
            if (ResultConfig.IsCentroidYCheckEnabled)
            {
                if (ResultConfig.CentroidYMin >= ResultConfig.CentroidYMax)
                {
                    result.AddError($"质心Y最小值({ResultConfig.CentroidYMin})必须小于最大值({ResultConfig.CentroidYMax})");
                }
            }
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
