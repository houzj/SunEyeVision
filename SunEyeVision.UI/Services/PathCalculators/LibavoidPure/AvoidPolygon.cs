using System;
using System.Collections.Generic;
using System.Linq;

namespace SunEyeVision.UI.Services.PathCalculators.LibavoidPure
{
    /// <summary>
    /// Libavoid的多边形结构
    /// </summary>
    public class AvoidPolygon
    {
        private List<AvoidPoint> _points;

        public AvoidPolygon(int capacity = 4)
        {
            _points = new List<AvoidPoint>(capacity);
        }

        public int Count => _points.Count;

        public AvoidPoint this[int index]
        {
            get => _points[index];
            set => _points[index] = value;
        }

        public AvoidRectangle Bounds { get; private set; }

        public void AddPoint(AvoidPoint point)
        {
            _points.Add(point);
            UpdateBounds();
        }

        private void UpdateBounds()
        {
            if (_points.Count == 0)
            {
                Bounds = new AvoidRectangle(0, 0, 0, 0);
                return;
            }

            double minX = double.MaxValue;
            double minY = double.MaxValue;
            double maxX = double.MinValue;
            double maxY = double.MinValue;

            foreach (var point in _points)
            {
                if (point.X < minX) minX = point.X;
                if (point.X > maxX) maxX = point.X;
                if (point.Y < minY) minY = point.Y;
                if (point.Y > maxY) maxY = point.Y;
            }

            Bounds = new AvoidRectangle(new AvoidPoint(minX, minY), new AvoidPoint(maxX, maxY));
        }

        public bool Contains(AvoidPoint point)
        {
            if (!Bounds.Contains(point))
                return false;

            bool inside = false;
            int j = _points.Count - 1;

            for (int i = 0; i < _points.Count; i++)
            {
                var pi = _points[i];
                var pj = _points[j];

                if (((pi.Y > point.Y) != (pj.Y > point.Y)) &&
                    (point.X < (pj.X - pi.X) * (point.Y - pi.Y) / (pj.Y - pi.Y) + pi.X))
                {
                    inside = !inside;
                }

                j = i;
            }

            return inside;
        }

        public AvoidPoint Center()
        {
            double sumX = 0, sumY = 0;
            foreach (var point in _points)
            {
                sumX += point.X;
                sumY += point.Y;
            }
            return new AvoidPoint(sumX / _points.Count, sumY / _points.Count);
        }

        public static AvoidPolygon FromRectangle(AvoidRectangle rect)
        {
            var polygon = new AvoidPolygon(4);
            polygon.AddPoint(new AvoidPoint(rect.X, rect.Y));
            polygon.AddPoint(new AvoidPoint(rect.Right, rect.Y));
            polygon.AddPoint(new AvoidPoint(rect.Right, rect.Bottom));
            polygon.AddPoint(new AvoidPoint(rect.X, rect.Bottom));
            return polygon;
        }
    }
}
