using System;
using System.Text.Json.Serialization;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.SDK.Validation;

namespace SunEyeVision.Tool.Blob
{
    /// <summary>
    /// Blob检测参数
    /// </summary>
    /// <remarks>
    /// 多态序列化（rule-010: 方案系统实现规范）：
    /// 使用 [JsonDerivedType] 特性标识参数类型，类型标识符为 "Blob"。
    /// </remarks>
    [JsonDerivedType(typeof(BlobParameters), "Blob")]
    public class BlobParameters : ToolParameters
    {
        #region 私有字段

        private int _minArea = 10;
        private int _maxArea = 10000;
        private double _circularity = 0.8;
        private double _convexity = 0.9;
        private double _minInertiaRatio = 0.01;
        private double _maxInertiaRatio = 0.1;
        private int _minDistanceBetweenBlobs = 10;

        #endregion

        #region 基本参数

        /// <summary>
        /// 最小面积
        /// </summary>
        public int MinArea
        {
            get => _minArea;
            set => SetProperty(ref _minArea, value, "最小面积");
        }

        /// <summary>
        /// 最大面积
        /// </summary>
        public int MaxArea
        {
            get => _maxArea;
            set => SetProperty(ref _maxArea, value, "最大面积");
        }

        /// <summary>
        /// 圆度阈值
        /// </summary>
        public double Circularity
        {
            get => _circularity;
            set => SetProperty(ref _circularity, value, "圆度阈值");
        }

        /// <summary>
        /// 凸度阈值
        /// </summary>
        public double Convexity
        {
            get => _convexity;
            set => SetProperty(ref _convexity, value, "凸度阈值");
        }

        #endregion

        #region 高级参数

        /// <summary>
        /// 最小惯性比率
        /// </summary>
        public double MinInertiaRatio
        {
            get => _minInertiaRatio;
            set => SetProperty(ref _minInertiaRatio, value, "最小惯性比率");
        }

        /// <summary>
        /// 最大惯性比率
        /// </summary>
        public double MaxInertiaRatio
        {
            get => _maxInertiaRatio;
            set => SetProperty(ref _maxInertiaRatio, value, "最大惯性比率");
        }

        /// <summary>
        /// Blob之间的最小距离
        /// </summary>
        public int MinDistanceBetweenBlobs
        {
            get => _minDistanceBetweenBlobs;
            set => SetProperty(ref _minDistanceBetweenBlobs, value, "最小Blob间距");
        }

        #endregion

        /// <summary>
        /// 验证参数
        /// </summary>
        public override ValidationResult Validate()
        {
            var result = new ValidationResult();

            if (MinArea < 1)
                result.AddError("最小面积必须大于0");

            if (MaxArea <= MinArea)
                result.AddError("最大面积必须大于最小面积");

            if (Circularity < 0 || Circularity > 1)
                result.AddError("圆度阈值必须在0-1之间");

            if (Convexity < 0 || Convexity > 1)
                result.AddError("凸度阈值必须在0-1之间");

            if (MinInertiaRatio < 0 || MinInertiaRatio > 1)
                result.AddError("最小惯性比率必须在0-1之间");

            if (MaxInertiaRatio < 0 || MaxInertiaRatio > 1)
                result.AddError("最大惯性比率必须在0-1之间");

            if (MinInertiaRatio > MaxInertiaRatio)
                result.AddError("最小惯性比率不能大于最大惯性比率");

            if (MinDistanceBetweenBlobs < 0)
                result.AddError("最小Blob间距必须大于等于0");

            return result;
        }
    }
}
