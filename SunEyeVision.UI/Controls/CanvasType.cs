using System.ComponentModel;

namespace SunEyeVision.UI.Controls
{
    /// <summary>
    /// 画布类型枚举
    /// </summary>
    public enum CanvasType
    {
        /// <summary>
        /// 原始的WorkflowCanvasControl
        /// </summary>
        [Description("WorkflowCanvasControl")]
        WorkflowCanvas,

        [Description("AIStudioDiagramControl")]
        /// <summary>
        /// AIStudioDiagramControl
        /// </summary>
        AIStudioDiagram
    }
}
