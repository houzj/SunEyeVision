using System.Windows;

namespace SunEyeVision.Plugin.SDK.UI.Constants
{
    /// <summary>
    /// 项目统一字体常量 - 单一来源
    /// </summary>
    /// <remarks>
    /// 此类是项目所有字体的唯一定义来源：
    /// - C# 代码直接使用常量
    /// - XAML 通过 x:Static 引用常量
    /// - 修改此处，所有地方同步更新
    /// 
    /// 规则：禁止在其他地方定义字体值！
    /// 
    /// 字体体系：
    /// - SizeTitle: 16px（标题）
    /// - SizeHeading: 14px（副标题）
    /// - SizeBody: 13px（正文）
    /// - SizeCaption: 12px（说明文字）
    /// - SizeSmall: 11px（小号文字）
    /// - SizeMicro: 10px（微小文字）
    /// </remarks>
    public static class FontConstants
    {
        #region 字体大小

        /// <summary>
        /// 标题字体大小 - 16px
        /// </summary>
        public const double SizeTitle = 16;

        /// <summary>
        /// 副标题字体大小 - 14px
        /// </summary>
        public const double SizeHeading = 14;

        /// <summary>
        /// 正文字体大小 - 13px
        /// </summary>
        public const double SizeBody = 13;

        /// <summary>
        /// 说明文字字体大小 - 12px
        /// </summary>
        public const double SizeCaption = 12;

        /// <summary>
        /// 小号文字字体大小 - 11px
        /// </summary>
        public const double SizeSmall = 11;

        /// <summary>
        /// 微小文字字体大小 - 10px
        /// </summary>
        public const double SizeMicro = 10;

        /// <summary>
        /// 极小文字字体大小 - 8px
        /// </summary>
        public const double SizeTiny = 8;

        #endregion

        #region 字体族

        /// <summary>
        /// 默认字体族 - 微软雅黑 UI
        /// </summary>
        /// <remarks>
        /// 微软雅黑 UI 是 Windows 系统默认 UI 字体，适合中文界面显示。
        /// 相比微软雅黑，UI 版本的笔画更细，更适合小字号显示。
        /// </remarks>
        public const string FamilyDefault = "Microsoft YaHei UI";

        /// <summary>
        /// 等宽字体族 - Consolas
        /// </summary>
        /// <remarks>
        /// Consolas 是 Windows 系统默认等宽字体，适合代码显示。
        /// 每个字符宽度相同，便于对齐。
        /// </remarks>
        public const string FamilyMono = "Consolas";

        /// <summary>
        /// 数字字体族 - Segoe UI
        /// </summary>
        /// <remarks>
        /// Segoe UI 的数字显示效果更好，适合数据展示。
        /// 数字等宽，便于对齐。
        /// </remarks>
        public const string FamilyNumeric = "Segoe UI";

        #endregion

        #region 字重

        /// <summary>
        /// 正常字重
        /// </summary>
        public static FontWeight WeightNormal => FontWeights.Normal;

        /// <summary>
        /// 中等字重
        /// </summary>
        public static FontWeight WeightMedium => FontWeights.Medium;

        /// <summary>
        /// 粗体字重
        /// </summary>
        public static FontWeight WeightBold => FontWeights.Bold;

        /// <summary>
        /// 轻字重
        /// </summary>
        public static FontWeight WeightLight => FontWeights.Light;

        #endregion
    }
}
