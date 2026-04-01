using System.Collections.Generic;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.SDK.Validation;
using System.Text.Json.Serialization;

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
    /// </remarks>
    [JsonDerivedType(typeof(ThresholdParameters), "Threshold")]
    public class ThresholdParameters : ToolParameters
    {
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
        public int Threshold
        {
            get => _threshold;
            set=> SetProperty(ref _threshold, value, "阈值");

        }

        /// <summary>
        /// 超过阈值时使用的最大值(0-255)
        /// </summary>
        public int MaxValue
        {
            get => _maxValue;
            set => SetProperty(ref _maxValue, value, "最大值");
        }

        /// <summary>
        /// 二值化方法
        /// </summary>
        public ThresholdType Type
        {
            get => _type;
            set => SetProperty(ref _type, value, "阈值类型");
        }

        /// <summary>
        /// 是否反转二值化结果
        /// </summary>
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
        public AdaptiveMethod AdaptiveMethod
        {
            get => _adaptiveMethod;
            set => SetProperty(ref _adaptiveMethod, value, "自适应方法");
        }

        /// <summary>
        /// 计算阈值的邻域大小(奇数, 3-31)
        /// </summary>
        public int BlockSize
        {
            get => _blockSize;
            set
            {
                // 确保块大小为奇数
                if (value % 2 == 0) value++;
                SetProperty(ref _blockSize, value, "块大小");
            }
        }

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
