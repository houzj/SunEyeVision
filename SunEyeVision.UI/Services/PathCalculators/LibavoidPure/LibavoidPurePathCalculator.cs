using System;
using System.Windows;
using System.Windows.Media;
using System.Collections.Generic;
using System.Linq;

namespace SunEyeVision.UI.Services.PathCalculators.LibavoidPure
{
    /// <summary>
    /// 真正的Libavoid算法纯C#实现路径计算器
    /// 实现了正交路径路由和障碍物避让
    /// </summary>
    public class LibavoidPurePathCalculator : IPathCalculator
    {
        private readonly AvoidRouter _router;
        private readonly Dictionary<string, ShapeRef> _shapeMap;
        private uint _requestId = 0;

        public LibavoidPurePathCalculator()
        {
            var config = new AvoidRouterConfiguration
            {
                IdealSegmentLength = 50.0,
                UseOrthogonalRouting = true,
                RoutingTimeLimit = 5000
            };

            _router = new AvoidRouter(config);
            _shapeMap = new Dictionary<string, ShapeRef>();

            System.Diagnostics.Debug.WriteLine("[LibavoidPure] ╔═════════════════════════════════════════════════════╗");
            System.Diagnostics.Debug.WriteLine("[LibavoidPure] ║      LibavoidPurePathCalculator 初始化完成           ║");
            System.Diagnostics.Debug.WriteLine("[LibavoidPure] ╚═════════════════════════════════════════════════════╝");
        }

        /// <summary>
        /// 清除所有缓存
        /// </summary>
        public void ClearCache()
        {
            _shapeMap.Clear();
            System.Diagnostics.Debug.WriteLine("[LibavoidPure] 缓存已清除");
        }

        /// <summary>
        /// 计算正交折线路径点集合（基础方法）
        /// </summary>
        public Point[] CalculateOrthogonalPath(
            Point sourcePosition,
            Point targetPosition,
            PortDirection sourceDirection,
            PortDirection targetDirection)
        {
            return CalculateOrthogonalPath(
                sourcePosition,
                targetPosition,
                sourceDirection,
                targetDirection,
                Rect.Empty,
                Rect.Empty);
        }

        /// <summary>
        /// 计算正交折线路径点集合（增强方法，带节点信息和障碍物检测）
        /// </summary>
        public Point[] CalculateOrthogonalPath(
            Point sourcePosition,
            Point targetPosition,
            PortDirection sourceDirection,
            PortDirection targetDirection,
            Rect sourceNodeRect,
            Rect targetNodeRect,
            params Rect[] allNodeRects)
        {
            System.Diagnostics.Debug.WriteLine("[LibavoidPure] ========== 开始路径计算 ==========");
            System.Diagnostics.Debug.WriteLine($"[LibavoidPure] 源位置:({sourcePosition.X:F1},{sourcePosition.Y:F1}), 目标位置:({targetPosition.X:F1},{targetPosition.Y:F1})");
            System.Diagnostics.Debug.WriteLine($"[LibavoidPure] 源方向:{sourceDirection}, 目标方向:{targetDirection}");

            // 清空所有形状（重新创建）
            _shapeMap.Clear();

            // 创建所有障碍物形状（排除源和目标节点）
            if (allNodeRects != null && allNodeRects.Length > 0)
            {
                for (int i = 0; i < allNodeRects.Length; i++)
                {
                    var nodeRect = allNodeRects[i];

                    // 跳过源节点和目标节点
                    if (!sourceNodeRect.IsEmpty && RectsEqual(nodeRect, sourceNodeRect))
                        continue;
                    if (!targetNodeRect.IsEmpty && RectsEqual(nodeRect, targetNodeRect))
                        continue;

                    var key = $"node_{i}";
                    var avoidRect = new AvoidRectangle(
                        nodeRect.X,
                        nodeRect.Y,
                        nodeRect.Width,
                        nodeRect.Height);

                    var shape = _router.CreateRectangleShape(avoidRect);
                    _shapeMap[key] = shape;

                    System.Diagnostics.Debug.WriteLine($"[LibavoidPure] 添加障碍物: {avoidRect}");
                }
            }

            // 转换为AvoidPoint
            var avoidSource = new AvoidPoint(sourcePosition.X, sourcePosition.Y);
            var avoidTarget = new AvoidPoint(targetPosition.X, targetPosition.Y);

            // 使用Libavoid路由器计算路径
            var avoidPath = _router.RoutePath(avoidSource, avoidTarget);

            // 转换为Point数组
            var path = avoidPath.Select(p => new Point(p.X, p.Y)).ToArray();

            System.Diagnostics.Debug.WriteLine($"[LibavoidPure] ✅ 路径计算完成，路径点数: {path.Length}");
            for (int i = 0; i < path.Length; i++)
            {
                System.Diagnostics.Debug.WriteLine($"[LibavoidPure]   路径点[{i}]:({path[i].X:F1},{path[i].Y:F1})");
            }
            System.Diagnostics.Debug.WriteLine("[LibavoidPure] ========== 路径计算完成 ==========");

            return path;
        }

        /// <summary>
        /// 根据路径点创建路径几何
        /// </summary>
        public PathGeometry CreatePathGeometry(Point[] pathPoints)
        {
            if (pathPoints == null || pathPoints.Length < 2)
            {
                return new PathGeometry();
            }

            var geometry = new PathGeometry();
            var figure = new PathFigure { StartPoint = pathPoints[0] };

            for (int i = 1; i < pathPoints.Length; i++)
            {
                figure.Segments.Add(new LineSegment(pathPoints[i], true));
            }

            geometry.Figures.Add(figure);
            return geometry;
        }

        /// <summary>
        /// 计算箭头位置和角度
        /// </summary>
        public (Point position, double angle) CalculateArrow(Point[] pathPoints, Point targetPosition, PortDirection targetDirection)
        {
            if (pathPoints == null || pathPoints.Length < 2)
            {
                return (new Point(0, 0), 0);
            }

            var arrowPosition = targetPosition;
            var arrowAngle = GetFixedArrowAngle(targetDirection);

            return (arrowPosition, arrowAngle);
        }

        /// <summary>
        /// 获取固定箭头角度
        /// </summary>
        private double GetFixedArrowAngle(PortDirection targetDirection)
        {
            return targetDirection switch
            {
                PortDirection.Left => 0.0,
                PortDirection.Right => 180.0,
                PortDirection.Top => 90.0,
                PortDirection.Bottom => 270.0,
                _ => 0.0
            };
        }

        /// <summary>
        /// 判断两个矩形是否相等（带容差）
        /// </summary>
        private bool RectsEqual(Rect rect1, Rect rect2)
        {
            const double tolerance = 0.001;
            return Math.Abs(rect1.X - rect2.X) < tolerance &&
                   Math.Abs(rect1.Y - rect2.Y) < tolerance &&
                   Math.Abs(rect1.Width - rect2.Width) < tolerance &&
                   Math.Abs(rect1.Height - rect2.Height) < tolerance;
        }
    }
}
