using System;
using System.Collections.Generic;

namespace SunEyeVision.Plugin.SDK.UI.Controls.Region.Models
{
    /// <summary>
    /// 区域定义基类
    /// </summary>
    public abstract class RegionDefinition
    {
        /// <summary>
        /// 区域定义ID
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// 定义模式
        /// </summary>
        public RegionDefinitionMode Mode { get; set; }

        /// <summary>
        /// 获取形状类型（如果适用）
        /// </summary>
        public abstract ShapeType? GetShapeType();

        /// <summary>
        /// 克隆
        /// </summary>
        public abstract RegionDefinition Clone();
    }

    /// <summary>
    /// 形状定义 - 用户绘制的形状
    /// </summary>
    public class ShapeDefinition : RegionDefinition
    {
        /// <summary>
        /// 形状类型
        /// </summary>
        public ShapeType ShapeType { get; set; } = ShapeType.Rectangle;

        #region 通用参数

        /// <summary>
        /// 中心点X坐标
        /// </summary>
        public double CenterX { get; set; }

        /// <summary>
        /// 中心点Y坐标
        /// </summary>
        public double CenterY { get; set; }

        /// <summary>
        /// 宽度（矩形、旋转矩形）
        /// </summary>
        public double Width { get; set; }

        /// <summary>
        /// 高度（矩形、旋转矩形）
        /// </summary>
        public double Height { get; set; }

        /// <summary>
        /// 旋转角度（度数，数学角度系统：[-180°, 180°]，逆时针为正）
        /// </summary>
        public double Angle { get; set; }

        /// <summary>
        /// 半径（圆形、圆环）
        /// </summary>
        public double Radius { get; set; }

        /// <summary>
        /// 外半径（圆环）
        /// </summary>
        public double OuterRadius { get; set; }

        #endregion

        #region 直线参数

        /// <summary>
        /// 起点X坐标（直线）
        /// </summary>
        public double StartX { get; set; }

        /// <summary>
        /// 起点Y坐标（直线）
        /// </summary>
        public double StartY { get; set; }

        /// <summary>
        /// 终点X坐标（直线）
        /// </summary>
        public double EndX { get; set; }

        /// <summary>
        /// 终点Y坐标（直线）
        /// </summary>
        public double EndY { get; set; }

        #endregion

        #region 多边形参数

        /// <summary>
        /// 多边形顶点
        /// </summary>
        public List<Point2D> Points { get; set; } = new();

        #endregion

        #region 弧形参数

        /// <summary>
        /// 起始角度（弧形）
        /// </summary>
        public double StartAngle { get; set; }

        /// <summary>
        /// 结束角度（弧形）
        /// </summary>
        public double EndAngle { get; set; }

        #endregion

        #region 样式属性（参考ROI编辑器）

        /// <summary>
        /// 填充颜色（ARGB）
        /// </summary>
        public uint FillColorArgb { get; set; } = 0x28FF0000; // 默认红色半透明

        /// <summary>
        /// 边框颜色（ARGB）
        /// </summary>
        public uint StrokeColorArgb { get; set; } = 0xFFFF0000; // 默认红色

        /// <summary>
        /// 边框厚度
        /// </summary>
        public double StrokeThickness { get; set; } = 2;

        /// <summary>
        /// 透明度 (0-1)
        /// </summary>
        public double Opacity { get; set; } = 0.3;

        #endregion

        public ShapeDefinition()
        {
            Mode = RegionDefinitionMode.Drawing;
        }

        public override ShapeType? GetShapeType() => ShapeType;

        /// <summary>
        /// 获取中心点
        /// </summary>
        public Point2D GetCenter() => new Point2D(CenterX, CenterY);

        /// <summary>
        /// 设置中心点
        /// </summary>
        public void SetCenter(Point2D center)
        {
            CenterX = center.X;
            CenterY = center.Y;
        }

        /// <summary>
        /// 获取起点（直线）
        /// </summary>
        public Point2D GetStartPoint() => new Point2D(StartX, StartY);

        /// <summary>
        /// 设置起点（直线）
        /// </summary>
        public void SetStartPoint(Point2D point)
        {
            StartX = point.X;
            StartY = point.Y;
        }

        /// <summary>
        /// 获取终点（直线）
        /// </summary>
        public Point2D GetEndPoint() => new Point2D(EndX, EndY);

        /// <summary>
        /// 设置终点（直线）
        /// </summary>
        public void SetEndPoint(Point2D point)
        {
            EndX = point.X;
            EndY = point.Y;
        }

        /// <summary>
        /// 获取直线长度
        /// </summary>
        public double GetLineLength()
        {
            if (ShapeType != ShapeType.Line) return 0;
            var dx = EndX - StartX;
            var dy = EndY - StartY;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>
        /// 获取直线角度（数学角度系统）
        /// </summary>
        public double GetLineAngle()
        {
            if (ShapeType != ShapeType.Line) return 0;
            var dx = EndX - StartX;
            var dy = EndY - StartY;
            // 数学角度：逆时针为正，屏幕坐标系Y轴向下，需要取负
            return -Math.Atan2(dy, dx) * 180 / Math.PI;
        }

        public override RegionDefinition Clone()
        {
            var clone = new ShapeDefinition
            {
                Id = Id,
                Mode = Mode,
                ShapeType = ShapeType,
                CenterX = CenterX,
                CenterY = CenterY,
                Width = Width,
                Height = Height,
                Angle = Angle,
                Radius = Radius,
                OuterRadius = OuterRadius,
                StartX = StartX,
                StartY = StartY,
                EndX = EndX,
                EndY = EndY,
                StartAngle = StartAngle,
                EndAngle = EndAngle,
                FillColorArgb = FillColorArgb,
                StrokeColorArgb = StrokeColorArgb,
                StrokeThickness = StrokeThickness,
                Opacity = Opacity
            };
            foreach (var point in Points)
            {
                clone.Points.Add(point);
            }
            return clone;
        }
    }

    /// <summary>
    /// 固定区域 - 订阅其他节点的区域输出
    /// </summary>
    public class FixedRegion : RegionDefinition
    {
        /// <summary>
        /// 源节点ID
        /// </summary>
        public string SourceNodeId { get; set; } = string.Empty;

        /// <summary>
        /// 输出端口名称
        /// </summary>
        public string OutputName { get; set; } = string.Empty;

        /// <summary>
        /// 区域索引（用于多区域输出）
        /// </summary>
        public int? RegionIndex { get; set; }

        public FixedRegion()
        {
            Mode = RegionDefinitionMode.SubscribeByRegion;
        }

        public FixedRegion(string nodeId, string outputName, int? index = null)
        {
            Mode = RegionDefinitionMode.SubscribeByRegion;
            SourceNodeId = nodeId;
            OutputName = outputName;
            RegionIndex = index;
        }

        public override ShapeType? GetShapeType() => null;

        public override RegionDefinition Clone()
        {
            return new FixedRegion(SourceNodeId, OutputName, RegionIndex)
            {
                Id = Id
            };
        }
    }

    /// <summary>
    /// 计算区域 - 每个参数可单独绑定
    /// </summary>
    public class ComputedRegion : RegionDefinition
    {
        /// <summary>
        /// 目标形状类型
        /// </summary>
        public ShapeType TargetShapeType { get; set; } = ShapeType.Rectangle;

        /// <summary>
        /// 参数绑定字典（参数名 -> 参数源）
        /// </summary>
        public Dictionary<string, ParameterSource> ParameterBindings { get; set; } = new();

        public ComputedRegion()
        {
            Mode = RegionDefinitionMode.SubscribeByParameter;
        }

        public override ShapeType? GetShapeType() => TargetShapeType;

        /// <summary>
        /// 设置参数绑定
        /// </summary>
        public void SetParameterBinding(string parameterName, ParameterSource source)
        {
            ParameterBindings[parameterName] = source;
        }

        /// <summary>
        /// 获取参数绑定
        /// </summary>
        public ParameterSource? GetParameterBinding(string parameterName)
        {
            return ParameterBindings.TryGetValue(parameterName, out var source) ? source : null;
        }

        /// <summary>
        /// 移除参数绑定
        /// </summary>
        public void RemoveParameterBinding(string parameterName)
        {
            ParameterBindings.Remove(parameterName);
        }

        /// <summary>
        /// 解析参数值
        /// </summary>
        public object? ResolveParameter(string parameterName, IParameterContext? context)
        {
            var source = GetParameterBinding(parameterName);
            return source?.GetValue(context);
        }

        public override RegionDefinition Clone()
        {
            var clone = new ComputedRegion
            {
                Id = Id,
                TargetShapeType = TargetShapeType
            };
            foreach (var kvp in ParameterBindings)
            {
                clone.ParameterBindings[kvp.Key] = kvp.Value.Clone();
            }
            return clone;
        }
    }
}
