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
        // 中心手柄（用于拖动）
        Center,
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

        private double _rotation;

        /// <summary>
        /// 旋转角度（度数，仅用于旋转矩形）
        /// 使用数学角度定义：范围[-180°, 180°]，逆时针为正，0°表示矩形宽度方向与X轴平行
        /// </summary>
        public double Rotation
        {
            get => _rotation;
            set
            {
                _rotation = NormalizeAngle(value);
                ModifiedTime = DateTime.Now;
            }
        }

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
                    // 使用精确的旋转矩形包围盒计算
                    return GetRotatedBoundingBox();

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

        #region 数学角度系统支持

        /// <summary>
        /// 将角度规范化到数学角度范围[-180°, 180°]
        /// 数学角度定义：逆时针为正，0°表示矩形宽度方向与X轴平行
        /// </summary>
        /// <param name="angle">任意角度值（度）</param>
        /// <returns>规范化后的角度</returns>
        public static double NormalizeAngle(double angle)
        {
            // 模360得到[0, 360)
            angle = angle % 360;
            if (angle < 0) angle += 360;

            // 转换到[-180°, 180°]
            if (angle > 180)
                angle -= 360;

            return angle;
        }

        /// <summary>
        /// 获取旋转矩形的四个角点（世界坐标）
        /// 角点顺序：TopLeft, TopRight, BottomRight, BottomLeft
        /// 使用数学角度系统：Rotation为正时逆时针旋转
        /// </summary>
        /// <returns>四个角点数组</returns>
        public Point[] GetCorners()
        {
            if (Type != ROIType.RotatedRectangle)
                return Array.Empty<Point>();

            var center = Position;
            var w = Size.Width;
            var h = Size.Height;
            // 数学角度系统：正角度逆时针旋转
            // 在屏幕坐标系（Y轴向下）中，需要使用修正后的旋转矩阵
            var angleRad = Rotation * Math.PI / 180;
            var cos = Math.Cos(angleRad);
            var sin = Math.Sin(angleRad);

            // 本地坐标（未旋转）的四个角点
            var hw = w / 2;
            var hh = h / 2;

            // 应用旋转变换得到世界坐标
            // 在屏幕坐标系中实现数学角度系统（逆时针为正）
            Point Transform(double localX, double localY)
            {
                return new Point(
                    center.X + localX * cos + localY * sin,
                    center.Y - localX * sin + localY * cos
                );
            }

            return new Point[]
            {
                Transform(-hw, -hh),  // TopLeft
                Transform( hw, -hh),  // TopRight
                Transform( hw,  hh),  // BottomRight
                Transform(-hw,  hh)   // BottomLeft
            };
        }

        /// <summary>
        /// 获取方向箭头几何数据
        /// 箭头从中心指向右边中点的方向（表示矩形的"宽度方向"，与数学角度0°一致）
        /// 使用数学角度系统：0°时箭头指向右方，逆时针为正
        /// </summary>
        /// <returns>箭头起点和终点</returns>
        public (Point Start, Point End) GetDirectionArrow()
        {
            if (Type != ROIType.RotatedRectangle)
                return (Position, Position);

            var center = Position;
            var w = Size.Width;

            // 数学角度（逆时针为正）
            var mathAngleRad = Rotation * Math.PI / 180;
            var sin = Math.Sin(mathAngleRad);
            var cos = Math.Cos(mathAngleRad);

            // 右边中点：本地坐标 (w/2, 0)
            // 在屏幕坐标系中应用逆时针旋转变换：
            // x' = x*cos(θ) + y*sin(θ) = (w/2)*cos + 0*sin = (w/2)*cos
            // y' = -x*sin(θ) + y*cos(θ) = -(w/2)*sin + 0 = -(w/2)*sin
            var rightCenterX = center.X + (w / 2) * cos;
            var rightCenterY = center.Y - (w / 2) * sin;

            // 箭头从中心到右边中点
            return (center, new Point(rightCenterX, rightCenterY));
        }

        /// <summary>
        /// 获取旋转矩形精确的轴对齐包围盒
        /// </summary>
        /// <returns>最小外接矩形</returns>
        public Rect GetRotatedBoundingBox()
        {
            if (Type != ROIType.RotatedRectangle)
                return GetBounds();

            var corners = GetCorners();
            if (corners.Length == 0) return GetBounds();

            var minX = double.MaxValue;
            var minY = double.MaxValue;
            var maxX = double.MinValue;
            var maxY = double.MinValue;

            foreach (var corner in corners)
            {
                if (corner.X < minX) minX = corner.X;
                if (corner.Y < minY) minY = corner.Y;
                if (corner.X > maxX) maxX = corner.X;
                if (corner.Y > maxY) maxY = corner.Y;
            }

            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }

        #endregion
    }
}
