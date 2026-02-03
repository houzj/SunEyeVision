using System;

namespace SunEyeVision.UI.Services.PathCalculators.LibavoidPure
{
    /// <summary>
    /// Libavoid的点结构
    /// </summary>
    public struct AvoidPoint : IEquatable<AvoidPoint>
    {
        public double X { get; set; }
        public double Y { get; set; }

        public AvoidPoint(double x, double y)
        {
            X = x;
            Y = y;
        }

        public static AvoidPoint operator +(AvoidPoint p1, AvoidPoint p2)
        {
            return new AvoidPoint(p1.X + p2.X, p1.Y + p2.Y);
        }

        public static AvoidPoint operator -(AvoidPoint p1, AvoidPoint p2)
        {
            return new AvoidPoint(p1.X - p2.X, p1.Y - p2.Y);
        }

        public static AvoidPoint operator *(AvoidPoint p, double scalar)
        {
            return new AvoidPoint(p.X * scalar, p.Y * scalar);
        }

        public double DistanceTo(AvoidPoint other)
        {
            double dx = X - other.X;
            double dy = Y - other.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        public double DistanceSquaredTo(AvoidPoint other)
        {
            double dx = X - other.X;
            double dy = Y - other.Y;
            return dx * dx + dy * dy;
        }

        public bool Equals(AvoidPoint other)
        {
            return Math.Abs(X - other.X) < 0.001 && Math.Abs(Y - other.Y) < 0.001;
        }

        public override bool Equals(object obj)
        {
            if (obj is AvoidPoint other)
            {
                return Equals(other);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y);
        }

        public override string ToString()
        {
            return $"({X:F2}, {Y:F2})";
        }
    }
}
