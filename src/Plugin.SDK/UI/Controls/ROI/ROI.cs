using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace SunEyeVision.Plugin.SDK.UI.Controls.ROI
{
    /// <summary>
    /// 编辑手柄类型
    /// </summary>
    public enum HandleType
    {
        None,
        // 角落手柄
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight,
        // 边中点手柄
        Top,
        Bottom,
        Left,
        Right,
        // 旋转手柄
        Rotate,
        // 直线端点手柄
        LineStart,
        LineEnd
    }

    /// <summary>
    /// 编辑手柄信息
    /// </summary>
    public class EditHandle
    {
        public HandleType Type { get; set; }
        public Point Position { get; set; }
        public Rect Bounds { get; set; }
        public Cursor Cursor { get; set; } = Cursors.Arrow;
        public double HandleSize { get; set; } = 8;
    }

    /// <summary>
    /// ROI（感兴趣区域）数据模型
    /// </summary>
    public class ROI : ICloneable
    {
        /// <summary>
        /// 唯一标识符
        /// </summary>
        public Guid ID { get; } = Guid.NewGuid();

        /// <summary>
        /// ROI形状类型
        /// </summary>
        public ROIType Type { get; set; }

        /// <summary>
        /// 位置（中心点或起点）
        /// </summary>
        public Point Position { get; set; }

        /// <summary>
        /// 尺寸
        /// </summary>
        public Size Size { get; set; }

        /// <summary>
        /// 旋转角度（度数，仅用于旋转矩形）
        /// </summary>
        public double Rotation { get; set; }

        /// <summary>
        /// 填充颜色
        /// </summary>
        public Color FillColor { get; set; } = Color.FromArgb(40, 255, 0, 0);

        /// <summary>
        /// 边框颜色
        /// </summary>
        public Color StrokeColor { get; set; } = Colors.Red;

        /// <summary>
        /// 边框厚度
        /// </summary>
        public double StrokeThickness { get; set; } = 2;

        /// <summary>
        /// 透明度 (0-1)
        /// </summary>
        public double Opacity { get; set; } = 0.3;

        /// <summary>
        /// 是否被选中
        /// </summary>
        public bool IsSelected { get; set; }

        /// <summary>
        /// 是否可编辑
        /// </summary>
        public bool IsEditable { get; set; } = true;

        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 标签
        /// </summary>
        public string Tag { get; set; } = string.Empty;

        /// <summary>
        /// 第二个点（用于直线）
        /// </summary>
        public Point EndPoint { get; set; }

        /// <summary>
        /// 半径（用于圆形）
        /// </summary>
        public double Radius { get; set; }

        /// <summary>
        /// 角点集合（用于多边形）
        /// </summary>
        public PointCollection Points { get; set; } = new PointCollection();

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedTime { get; } = DateTime.Now;

        /// <summary>
        /// 最后修改时间
        /// </summary>
        public DateTime ModifiedTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 构造函数
        /// </summary>
        public ROI()
        {
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        public ROI(ROIType type, Point position, Size size)
        {
            Type = type;
            Position = position;
            Size = size;
        }

        /// <summary>
        /// 获取边界矩形
        /// </summary>
        public Rect GetBounds()
        {
            switch (Type)
            {
                case ROIType.Rectangle:
                    return new Rect(
                        Position.X - Size.Width / 2,
                        Position.Y - Size.Height / 2,
                        Size.Width,
                        Size.Height);

                case ROIType.Circle:
                    return new Rect(
                        Position.X - Radius,
                        Position.Y - Radius,
                        Radius * 2,
                        Radius * 2);

                case ROIType.RotatedRectangle:
                    // 简化处理，返回包围盒
                    var diagonal = Math.Sqrt(Size.Width * Size.Width + Size.Height * Size.Height);
                    return new Rect(
                        Position.X - diagonal / 2,
                        Position.Y - diagonal / 2,
                        diagonal,
                        diagonal);

                case ROIType.Line:
                    var minX = Math.Min(Position.X, EndPoint.X);
                    var minY = Math.Min(Position.Y, EndPoint.Y);
                    var maxX = Math.Max(Position.X, EndPoint.X);
                    var maxY = Math.Max(Position.Y, EndPoint.Y);
                    return new Rect(minX, minY, maxX - minX, maxY - minY);

                default:
                    return new Rect(Position, Size);
            }
        }

        /// <summary>
        /// 判断点是否在ROI内
        /// </summary>
        public bool Contains(Point point)
        {
            switch (Type)
            {
                case ROIType.Rectangle:
                    var rect = GetBounds();
                    return rect.Contains(point);

                case ROIType.Circle:
                    var distance = Math.Sqrt(
                        Math.Pow(point.X - Position.X, 2) +
                        Math.Pow(point.Y - Position.Y, 2));
                    return distance <= Radius;

                case ROIType.RotatedRectangle:
                    // 简化处理，使用包围盒检测
                    return GetBounds().Contains(point);

                case ROIType.Line:
                    // 检测点到直线的距离
                    var lineLength = Math.Sqrt(
                        Math.Pow(EndPoint.X - Position.X, 2) +
                        Math.Pow(EndPoint.Y - Position.Y, 2));
                    if (lineLength < 1) return false;

                    var d = Math.Abs(
                        (EndPoint.Y - Position.Y) * point.X -
                        (EndPoint.X - Position.X) * point.Y +
                        EndPoint.X * Position.Y -
                        EndPoint.Y * Position.X) / lineLength;
                    return d < 10; // 10像素容差

                default:
                    return GetBounds().Contains(point);
            }
        }

        /// <summary>
        /// 移动ROI
        /// </summary>
        public void Move(Vector offset)
        {
            Position = Point.Add(Position, offset);
            if (Type == ROIType.Line)
            {
                EndPoint = Point.Add(EndPoint, offset);
            }
            ModifiedTime = DateTime.Now;
        }

        /// <summary>
        /// 缩放ROI
        /// </summary>
        public void Scale(double scaleX, double scaleY)
        {
            Size = new Size(Size.Width * scaleX, Size.Height * scaleY);
            Radius *= Math.Max(scaleX, scaleY);
            ModifiedTime = DateTime.Now;
        }

        /// <summary>
        /// 旋转ROI
        /// </summary>
        public void Rotate(double angle)
        {
            Rotation += angle;
            ModifiedTime = DateTime.Now;
        }

        /// <summary>
        /// 克隆
        /// </summary>
        public object Clone()
        {
            var clone = new ROI
            {
                Type = this.Type,
                Position = this.Position,
                Size = this.Size,
                Rotation = this.Rotation,
                FillColor = this.FillColor,
                StrokeColor = this.StrokeColor,
                StrokeThickness = this.StrokeThickness,
                Opacity = this.Opacity,
                IsSelected = this.IsSelected,
                IsEditable = this.IsEditable,
                Name = this.Name,
                Tag = this.Tag,
                EndPoint = this.EndPoint,
                Radius = this.Radius,
                ModifiedTime = DateTime.Now
            };

            foreach (var point in Points)
            {
                clone.Points.Add(point);
            }

            return clone;
        }

        /// <summary>
        /// 转换为字符串
        /// </summary>
        public override string ToString()
        {
            return $"{Type}: {Name} at {Position}";
        }
    }
}
