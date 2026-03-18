using System;

namespace SunEyeVision.Workflow;

/// <summary>
/// 解决方案元数据事件参数
/// </summary>
public class SolutionMetadataEventArgs : EventArgs
{
    /// <summary>
    /// 元数据对象（完整）
    /// </summary>
    public SolutionMetadata? Metadata { get; set; }

    /// <summary>
    /// 解决方案ID
    /// </summary>
    public string? SolutionId { get; set; }

    /// <summary>
    /// 旧名称（用于重命名事件）
    /// </summary>
    public string? OldName { get; set; }

    /// <summary>
    /// 新名称（用于重命名事件）
    /// </summary>
    public string? NewName { get; set; }
}
