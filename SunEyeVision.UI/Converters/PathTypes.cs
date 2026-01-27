using System;
using System.Windows;
using SunEyeVision.UI.Models;

namespace SunEyeVision.UI.Converters
{
    /// <summary>
    /// 端口类型枚举
    /// </summary>
    public enum PortType
    {
        TopPort,
        BottomPort,
        LeftPort,
        RightPort,
        Unknown
    }

    /// <summary>
    /// 路径计算上下�?
    /// </summary>
    public class PathContext
    {
        public Point StartPoint { get; set; }
        public Point EndPoint { get; set; }
        public Point ArrowTailPoint { get; set; }
        public PortType SourcePort { get; set; }
        public PortType TargetPort { get; set; }
        public WorkflowNode SourceNode { get; set; }
        public WorkflowNode TargetNode { get; set; }
        public System.Collections.Generic.List<WorkflowNode> Obstacles { get; set; } = new System.Collections.Generic.List<WorkflowNode>();
        public PathConfiguration Config { get; set; } = new PathConfiguration();
    }

    /// <summary>
    /// 路径配置
    /// </summary>
    public class PathConfiguration
    {
        public double ControlOffset { get; set; } = 60;
        public double GridSize { get; set; } = 20;
        public double NodeMargin { get; set; } = 30;
        public double ArrowSize { get; set; } = 10;
        public double PathOffset { get; set; } = 20;
        public double NodeWidth { get; set; } = 140;
        public double NodeHeight { get; set; } = 90;
        public bool EnableDebugLog { get; set; } = false;
    }
}
