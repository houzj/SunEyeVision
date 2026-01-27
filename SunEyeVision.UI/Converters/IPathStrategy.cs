using System;
using System.Collections.Generic;
using System.Windows;

namespace SunEyeVision.UI.Converters
{
    /// <summary>
    /// 路径策略接口
    /// </summary>
    public interface IPathStrategy
    {
        /// <summary>
        /// 判断是否能处理此场景
        /// </summary>
        bool CanHandle(PathContext context);

        /// <summary>
        /// 计算路径
        /// </summary>
        List<Point> CalculatePath(PathContext context);
    }

    /// <summary>
    /// 基础路径策略�?- 提供公共方法
    /// </summary>
    public abstract class BasePathStrategy : IPathStrategy
    {
        protected readonly PathConfiguration _config;

        protected BasePathStrategy(PathConfiguration config)
        {
            _config = config ?? new PathConfiguration();
        }

        public virtual bool CanHandle(PathContext context) => true;

        public abstract List<Point> CalculatePath(PathContext context);

        /// <summary>
        /// 创建安全点（确保在节点边界外�?
        /// </summary>
        protected Point CreateSafePoint(double x, double y, Rect nodeBounds, double margin)
        {
            return new Point(
                EnsureOutsideBounds(x, nodeBounds.Left, nodeBounds.Right, margin),
                EnsureOutsideBounds(y, nodeBounds.Top, nodeBounds.Bottom, margin)
            );
        }

        /// <summary>
        /// 确保坐标在边界外
        /// </summary>
        protected double EnsureOutsideBounds(double value, double min, double max, double margin)
        {
            if (value > min && value < max)
            {
                // 在边界内，选择较近的一�?
                double distToMin = value - min;
                double distToMax = max - value;
                return distToMin < distToMax ? min - margin : max + margin;
            }
            return value;
        }

        /// <summary>
        /// 检查线段是否与矩形相交
        /// </summary>
        protected bool LineSegmentIntersectsRect(Point p1, Point p2, Rect rect)
        {
            // 快速边界检�?- 使用Rect.Union静态方�?
            Rect segmentBounds = new Rect(p1, p2);
            if (!rect.Contains(p1) && !rect.Contains(p2) &&
                segmentBounds.Right < rect.Left && segmentBounds.Left > rect.Right &&
                segmentBounds.Bottom < rect.Top && segmentBounds.Top > rect.Bottom)
            {
                return false;
            }

            // 检查四个角�?
            Point[] corners = new Point[]
            {
                new Point(rect.Left, rect.Top),
                new Point(rect.Right, rect.Top),
                new Point(rect.Right, rect.Bottom),
                new Point(rect.Left, rect.Bottom)
            };

            for (int i = 0; i < 4; i++)
            {
                if (SegmentsIntersect(p1, p2, corners[i], corners[(i + 1) % 4]))
                {
                    return true;
                }
            }

            // 检查线段端点是否在矩形�?
            if (rect.Contains(p1) || rect.Contains(p2))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 检查两条线段是否相�?
        /// </summary>
        private bool SegmentsIntersect(Point p1, Point p2, Point p3, Point p4)
        {
            double d1 = CrossProduct(p3, p4, p1);
            double d2 = CrossProduct(p3, p4, p2);
            double d3 = CrossProduct(p1, p2, p3);
            double d4 = CrossProduct(p1, p2, p4);

            if (((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)) &&
                ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0)))
            {
                return true;
            }

            return false;
        }

        private double CrossProduct(Point p1, Point p2, Point p3)
        {
            return (p2.X - p1.X) * (p3.Y - p1.Y) - (p3.X - p1.X) * (p2.Y - p1.Y);
        }
    }
}
