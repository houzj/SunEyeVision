using System;

namespace SunEyeVision.UI.Services.PathCalculators.LibavoidPure
{
    /// <summary>
    /// Libavoid的矩形结构
    /// </summary>
    public struct AvoidRectangle
    {
        public AvoidPoint Min { get; set; }
        public AvoidPoint Max { get; set; }

        public double X => Min.X;
        public double Y => Min.Y;
        public double Width => Max.X - Min.X;
        public double Height => Max.Y - Min.Y;

        public double Left => Min.X;
        public double Right => Max.X;
        public double Top => Min.Y;
        public double Bottom => Max.Y;

        public AvoidRectangle(AvoidPoint min, AvoidPoint max)
        {
            Min = min;
            Max = max;
        }

        public AvoidRectangle(double x, double y, double width, double height)
        {
            Min = new AvoidPoint(x, y);
            Max = new AvoidPoint(x + width, y + height);
        }

        public AvoidPoint Center => new AvoidPoint((Min.X + Max.X) / 2, (Min.Y + Max.Y) / 2);

        public bool Contains(AvoidPoint point)
        {
            return point.X >= Min.X && point.X <= Max.X &&
                   point.Y >= Min.Y && point.Y <= Max.Y;
        }

        public bool Intersects(AvoidRectangle other)
        {
            return !(Right < other.Left || Left > other.Right ||
                     Bottom < other.Top || Top > other.Bottom);
        }

        public AvoidRectangle Expand(double amount)
        {
            return new AvoidRectangle(
                Min.X - amount,
                Min.Y - amount,
                Width + amount * 2,
                Height + amount * 2
            );
        }

        public override string ToString()
        {
            return $"({X:F2}, {Y:F2}, {Width:F2}x{Height:F2})";
        }
    }
}
