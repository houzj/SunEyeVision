using System;

namespace SunEyeVision.Plugin.SDK.Metadata
{
    /// <summary>
    /// 忽略保存特性 - 标记参数不随项目文件保存
    /// </summary>
    /// <remarks>
    /// 用于标记那些不需要保存到项目文件的参数。
    /// 这类参数通常需要本地持久化（如ROI位置），但不随项目文件迁移。
    ///
    /// 使用示例：
    /// <code>
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
    ///
    /// 使用示例：
    /// <code>
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
    /// [IgnoreDisplay]  // 不在UI显示
    /// public int InternalState { get; set; }
    /// </code>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class IgnoreDisplayAttribute : Attribute
    {
    }
}
