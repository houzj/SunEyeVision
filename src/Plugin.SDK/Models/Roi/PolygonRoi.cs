using OpenCvSharp;
using SunEyeVision.Plugin.SDK.Extensions;
using System;

namespace SunEyeVision.Plugin.SDK.Models.Roi
{
    /// <summary>
    /// 多边形ROI
    /// </summary>
    public sealed class PolygonRoi : RoiBase
    {
        private readonly Point2d[] _vertices;

        /// <summary>
        /// 从顶点数组创建多边形ROI
        /// </summary>
        public PolygonRoi(Point2d[] vertices)
        {
            _vertices = vertices ?? Array.Empty<Point2d>();
        }

        /// <summary>
        /// 从矩形创建多边形ROI
        /// </summary>
        public static PolygonRoi FromRectangle(Rect2d rect)
        {
            return new PolygonRoi(new[]
            {
                rect.TopLeft(),
                rect.TopRight(),
                rect.BottomRight(),
                rect.BottomLeft()
            });
        }

        /// <summary>
        /// 创建正多边形ROI
        /// </summary>
        public static PolygonRoi CreateRegular(Point2d center, double radius, int sides, double startAngle = 0)
        {
            if (sides < 3)
                throw new ArgumentException("正多边形至少需要3条边", nameof(sides));

            Point2d[] vertices = new Point2d[sides];
            double angleStep = 2 * Math.PI / sides;

            for (int i = 0; i < sides; i++)
            {
                double angle = startAngle + i * angleStep;
                vertices[i] = new Point2d(
                    center.X + radius * Math.Cos(angle),
                    center.Y + radius * Math.Sin(angle)
                );
            }

            return new PolygonRoi(vertices);
        }

        public override RoiType Type => RoiType.Polygon;

        public override bool IsValid => _vertices.Length >= 3;

        public override Rect2d BoundingBox
        {
            get
            {
                if (_vertices.Length == 0)
                    return new Rect2d(0, 0, 0, 0);

                double minX = double.MaxValue, minY = double.MaxValue;
                double maxX = double.MinValue, maxY = double.MinValue;

                foreach (var p in _vertices)
                {
                    minX = Math.Min(minX, p.X);
                    minY = Math.Min(minY, p.Y);
                    maxX = Math.Max(maxX, p.X);
                    maxY = Math.Max(maxY, p.Y);
                }

                return new Rect2d(minX, minY, maxX - minX, maxY - minY);
            }
        }

        public override Point2d Center
        {
            get
            {
                if (_vertices.Length == 0)
                    return new Point2d(0, 0);

                double sumX = 0, sumY = 0;
                foreach (var p in _vertices)
                {
                    sumX += p.X;
                    sumY += p.Y;
                }
                return new Point2d(sumX / _vertices.Length, sumY / _vertices.Length);
            }
        }

        public override double Area
        {
            get
            {
                if (_vertices.Length < 3)
                    return 0;

                // 使用鞋带公式计算面积
                double area = 0;
                int n = _vertices.Length;
                for (int i = 0; i < n; i++)
                {
                    int j = (i + 1) % n;
                    area += _vertices[i].X * _vertices[j].Y;
                    area -= _vertices[j].X * _vertices[i].Y;
                }
                return Math.Abs(area) / 2;
            }
        }

        /// <summary>
        /// 顶点数量
        /// </summary>
        public int VertexCount => _vertices.Length;

        /// <summary>
        /// 顶点数组
        /// </summary>
        public Point2d[] Vertices => _vertices;

        /// <summary>
        /// 周长
        /// </summary>
        public double Perimeter
        {
            get
            {
                if (_vertices.Length < 2)
                    return 0;

                double perimeter = 0;
                int n = _vertices.Length;
                for (int i = 0; i < n; i++)
                {
                    int j = (i + 1) % n;
                    perimeter += _vertices[i].DistanceTo(_vertices[j]);
                }
                return perimeter;
            }
        }

        public override bool Contains(Point2d point)
        {
            if (_vertices.Length < 3)
                return false;

            // 射线法判断点是否在多边形内
            int n = _vertices.Length;
            bool inside = false;

            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                if (((_vertices[i].Y > point.Y) != (_vertices[j].Y > point.Y)) &&
                    (point.X < (_vertices[j].X - _vertices[i].X) * (point.Y - _vertices[i].Y) /
                              (_vertices[j].Y - _vertices[i].Y) + _vertices[i].X))
                {
                    inside = !inside;
                }
            }

            return inside;
        }

        public override Point2d[] GetBoundaryPoints()
        {
            return _vertices;
        }

        public override Mat CreateMask(Size imageSize)
        {
            Mat mask = new Mat(imageSize, MatType.CV_8UC1, Scalar.Black);
            if (_vertices.Length < 3) return mask;

            // 转换为Point数组
            Point[] intPoints = new Point[_vertices.Length];
            for (int i = 0; i < _vertices.Length; i++)
            {
                intPoints[i] = new Point((int)Math.Round(_vertices[i].X), (int)Math.Round(_vertices[i].Y));
            }

            Cv2.FillPoly(mask, new[] { intPoints }, Scalar.White);
            return mask;
        }

        public override IRoi Offset(double dx, double dy)
        {
            Point2d[] newVertices = new Point2d[_vertices.Length];
            for (int i = 0; i < _vertices.Length; i++)
            {
                newVertices[i] = new Point2d(_vertices[i].X + dx, _vertices[i].Y + dy);
            }
            return new PolygonRoi(newVertices);
        }

        public override IRoi Scale(double factor)
        {
            Point2d[] newVertices = new Point2d[_vertices.Length];
            for (int i = 0; i < _vertices.Length; i++)
            {
                newVertices[i] = new Point2d(_vertices[i].X * factor, _vertices[i].Y * factor);
            }
            return new PolygonRoi(newVertices);
        }

        public override IRoi Transform(Mat transformMatrix)
        {
            if (transformMatrix == null || transformMatrix.Empty())
                return this;

            Point2d[] newVertices = new Point2d[_vertices.Length];
            for (int i = 0; i < _vertices.Length; i++)
            {
                double x = _vertices[i].X * transformMatrix.Get<double>(0, 0) +
                          _vertices[i].Y * transformMatrix.Get<double>(0, 1) +
                          transformMatrix.Get<double>(0, 2);
                double y = _vertices[i].X * transformMatrix.Get<double>(1, 0) +
                          _vertices[i].Y * transformMatrix.Get<double>(1, 1) +
                          transformMatrix.Get<double>(1, 2);
                newVertices[i] = new Point2d(x, y);
            }
            return new PolygonRoi(newVertices);
        }

        /// <summary>
        /// 简化多边形（Douglas-Peucker算法）
        /// </summary>
        public PolygonRoi Simplify(double tolerance)
        {
            if (_vertices.Length <= 3)
                return this;

            // 转换为Point2f数组用于OpenCV
            Point2f[] points2f = new Point2f[_vertices.Length];
            for (int i = 0; i < _vertices.Length; i++)
            {
                points2f[i] = new Point2f((float)_vertices[i].X, (float)_vertices[i].Y);
            }

            var simplified = Cv2.ApproxPolyDP(points2f, tolerance, true);

            Point2d[] result = new Point2d[simplified.Length];
            for (int i = 0; i < simplified.Length; i++)
            {
                result[i] = new Point2d(simplified[i].X, simplified[i].Y);
            }

            return new PolygonRoi(result);
        }

        public override string ToString() => $"PolygonROI({VertexCount} vertices)";
    }
}
