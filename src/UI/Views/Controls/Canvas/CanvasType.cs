using System.ComponentModel;

namespace SunEyeVision.UI.Views.Controls.Canvas
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

        /// <summary>
        /// 原生AIStudio.Wpf.DiagramDesigner控件（使用贝塞尔曲线连接）。
        /// </summary>
        [Description("NativeDiagramControl")]
        NativeDiagram
    }
}
