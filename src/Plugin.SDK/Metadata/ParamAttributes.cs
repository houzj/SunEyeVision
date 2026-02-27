using System;

namespace SunEyeVision.Plugin.SDK.Metadata
{
    /// <summary>
    /// 参数分类枚举
    /// </summary>
    /// <remarks>
    /// 用于区分参数在工具执行中的角色：
    /// - Input: 输入参数，从上游节点传递，支持数据绑定
    /// - Output: 输出参数，传递给下游节点，可被绑定
    /// - Config: 配置参数，工具内部配置，随项目保存
    /// - Runtime: 运行时状态，仅运行时使用，不保存
    /// </remarks>
    public enum ParamCategory
    {
        /// <summary>
        /// 输入参数 - 节点间传递，可绑定
        /// </summary>
        Input,

        /// <summary>
        /// 输出参数 - 节点间传递，可被绑定
        /// </summary>
        Output,

        /// <summary>
        /// 配置参数 - 工具内部配置
        /// </summary>
        Config,

        /// <summary>
        /// 运行时状态 - 仅运行时使用
        /// </summary>
        Runtime
    }

    /// <summary>
    /// 参数特性 - 用于标注工具参数的元数据
    /// </summary>
    /// <remarks>
    /// 核心特性，用于定义参数的分类、显示信息和行为约束。
    /// 
    /// 使用示例：
    /// <code>
    /// public class ThresholdParams : VisionToolParameters
    /// {
    ///     [Param(Category = ParamCategory.Input, DisplayName = "输入图像")]
    ///     public ImageData? InputImage { get; set; }
    ///     
    ///     [Param(Category = ParamCategory.Config, DisplayName = "阈值", Order = 1)]
    ///     [ParameterRange(0, 255)]
    ///     public int Threshold { get; set; } = 128;
    ///     
    ///     [Param(Category = ParamCategory.Config, DisplayName = "ROI", Order = 2)]
    ///     [IgnoreSave]  // ROI 仅本地保存，不随项目保存
    ///     public RectangleRoi? Roi { get; set; }
    /// }
    /// </code>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class ParamAttribute : Attribute
    {
        /// <summary>
        /// 显示名称（UI显示）
        /// </summary>
        public string? DisplayName { get; set; }

        /// <summary>
        /// 参数分类
        /// </summary>
        public ParamCategory Category { get; set; } = ParamCategory.Input;

        /// <summary>
        /// 是否必填（默认true）
        /// </summary>
        public bool Required { get; set; } = true;

        /// <summary>
        /// 显示顺序（越小越靠前）
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// 分组名称
        /// </summary>
        public string? Group { get; set; }

        /// <summary>
        /// 参数描述
        /// </summary>
        public string? Description { get; set; }
    }

    /// <summary>
    /// 忽略保存特性 - 标记参数不随项目文件保存
    /// </summary>
    /// <remarks>
    /// 用于标记那些不需要保存到项目文件的参数。
    /// 这类参数通常需要本地持久化（如ROI位置），但不随项目文件迁移。
    /// 
    /// 使用示例：
    /// <code>
    /// [Param(Category = ParamCategory.Config, DisplayName = "ROI")]
    /// [IgnoreSave]  // 不保存到项目文件，但可能保存到本地状态
    /// public RectangleRoi? Roi { get; set; }
    /// </code>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class IgnoreSaveAttribute : Attribute
    {
    }

    /// <summary>
    /// 忽略绑定特性 - 标记参数不支持数据绑定
    /// </summary>
    /// <remarks>
    /// 用于标记那些不支持从上游节点绑定的参数。
    /// 即使是Input类型参数，也可能因为特殊原因不支持绑定。
    /// 
    /// 使用示例：
    /// <code>
    /// [Param(Category = ParamCategory.Input, DisplayName = "参考图像")]
    /// [IgnoreBind]  // 不支持从上游绑定，必须手动设置
    /// public ImageData? ReferenceImage { get; set; }
    /// </code>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class IgnoreBindAttribute : Attribute
    {
    }

    /// <summary>
    /// 忽略显示特性 - 标记参数不在UI中显示
    /// </summary>
    /// <remarks>
    /// 用于标记那些不应该在参数面板中显示的参数。
    /// 通常用于内部状态或调试参数。
    /// 
    /// 使用示例：
    /// <code>
    /// [Param(Category = ParamCategory.Runtime)]
    /// [IgnoreDisplay]  // 不在UI显示
    /// public int InternalState { get; set; }
    /// </code>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class IgnoreDisplayAttribute : Attribute
    {
    }
}
