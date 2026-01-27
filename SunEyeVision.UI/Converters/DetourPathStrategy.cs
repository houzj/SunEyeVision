using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using SunEyeVision.UI.Models;

namespace SunEyeVision.UI.Converters
{
    /// <summary>
    /// 绕行路径策略 - 有障碍物时使�?
    /// </summary>
    public class DetourPathStrategy : BasePathStrategy
    {
        private readonly NodeRelationshipAnalyzer _relationshipAnalyzer;
        private readonly PathValidator _pathValidator;

        public DetourPathStrategy(PathConfiguration config, NodeRelationshipAnalyzer analyzer = null, PathValidator validator = null)
            : base(config)
        {
            _relationshipAnalyzer = analyzer ?? new NodeRelationshipAnalyzer(config);
            _pathValidator = validator ?? new PathValidator(config);
        }

        public override bool CanHandle(PathContext context)
        {
            return context.Obstacles != null && context.Obstacles.Count > 0;
        }

        public override List<Point> CalculatePath(PathContext context)
        {

            // 使用节点关系分析器确定最优绕行方�?
            DetourDirection optimalDirection = _relationshipAnalyzer.DetermineOptimalDetourDirection(context);

            // 计算障碍区域
            double minY = context.Obstacles.Min(n => n.Position.Y) - _config.NodeMargin;
            double maxY = context.Obstacles.Max(n => n.Position.Y + _config.NodeHeight) + _config.NodeMargin;
            double minX = context.Obstacles.Min(n => n.Position.X) - _config.NodeMargin;
            double maxX = context.Obstacles.Max(n => n.Position.X + _config.NodeWidth) + _config.NodeMargin;


            // 根据目标端口选择绕行策略
            return context.TargetPort switch
            {
                PortType.LeftPort => CreateLeftPortDetour(context, minX, maxX, minY, maxY, optimalDirection),
                PortType.RightPort => CreateRightPortDetour(context, minX, maxX, minY, maxY, optimalDirection),
                PortType.TopPort => CreateTopPortDetour(context, minX, maxX, minY, maxY, optimalDirection),
                PortType.BottomPort => CreateBottomPortDetour(context, minX, maxX, minY, maxY, optimalDirection),
                _ => new List<Point>()
            };
        }

        /// <summary>
        /// 左端口绕行策�?
        /// </summary>
        private List<Point> CreateLeftPortDetour(PathContext context, double minX, double maxX, double minY, double maxY, DetourDirection direction)
        {
            var segments = new List<Point>();
            Point startPoint = context.StartPoint;
            Point endPoint = context.ArrowTailPoint;

            // 确保起点已经离开源节�?
            Point safeStart = EnsureOutsideSourceNode(startPoint, context.SourceNode);

            // 计算源节点边�?
            double sourceLeft = context.SourceNode.Position.X;
            double sourceRight = context.SourceNode.Position.X + _config.NodeWidth;
            double sourceTop = context.SourceNode.Position.Y;
            double sourceBottom = context.SourceNode.Position.Y + _config.NodeHeight;

            // 策略：从上方或下方绕过障碍物，必须从左侧进入目标节点
            // 使用节点关系分析器推荐的方向
            double detourY;
            if (direction == DetourDirection.Top)
            {
                // 从上方绕�?- 确保有足够距�?
                detourY = Math.Min(safeStart.Y, minY - _config.ControlOffset);
            }
            else if (direction == DetourDirection.Bottom)
            {
                // 从下方绕�?- 确保有足够距�?
                detourY = Math.Max(safeStart.Y, maxY + _config.ControlOffset);
            }
            else if (safeStart.Y < minY)
            {
                // 从上方绕�?- 确保有足够距�?
                detourY = Math.Min(safeStart.Y, minY - _config.ControlOffset);
            }
            else if (safeStart.Y > maxY)
            {
                // 从下方绕�?- 确保有足够距�?
                detourY = Math.Max(safeStart.Y, maxY + _config.ControlOffset);
            }
            else
            {
                // 选择距离更近的方�?- 确保有足够距�?
                double topDist = Math.Abs(safeStart.Y - minY);
                double bottomDist = Math.Abs(safeStart.Y - maxY);
                detourY = topDist < bottomDist
                    ? Math.Min(safeStart.Y, minY - _config.ControlOffset)
                    : Math.Max(safeStart.Y, maxY + _config.ControlOffset);
            }

            // 左端口：必须从左侧进�?
            double entryX = context.TargetNode.Position.X - _config.PathOffset;

            // 如果起点不是安全起点，需要先移动到安全起�?
            if (safeStart != startPoint)
            {
                segments.Add(safeStart);
            }

            // 确保转折点不在源节点附近
            double safeX = safeStart.X;
            if (safeX > sourceLeft - _config.ControlOffset && 
                safeX < sourceRight + _config.ControlOffset)
            {
                // 转折点在源节点水平范围内，需要调�?
                safeX = sourceLeft - _config.ControlOffset;
            }

            segments.Add(new Point(safeX, detourY));
            segments.Add(new Point(entryX, detourY));
            segments.Add(new Point(entryX, endPoint.Y));

            return segments;
        }

        /// <summary>
        /// 右端口绕行策�?
        /// </summary>
        private List<Point> CreateRightPortDetour(PathContext context, double minX, double maxX, double minY, double maxY, DetourDirection direction)
        {
            var segments = new List<Point>();
            Point startPoint = context.StartPoint;
            Point endPoint = context.ArrowTailPoint;

            // 确保起点已经离开源节�?
            Point safeStart = EnsureOutsideSourceNode(startPoint, context.SourceNode);

            // 计算源节点边�?
            double sourceLeft = context.SourceNode.Position.X;
            double sourceRight = context.SourceNode.Position.X + _config.NodeWidth;
            double sourceTop = context.SourceNode.Position.Y;
            double sourceBottom = context.SourceNode.Position.Y + _config.NodeHeight;

            // 策略：从上方或下方绕过障碍物，必须从右侧进入目标节点
            // 使用节点关系分析器推荐的方向
            double detourY;
            if (direction == DetourDirection.Top)
            {
                // 从上方绕�?- 确保有足够距�?
                detourY = Math.Min(safeStart.Y, minY - _config.ControlOffset);
            }
            else if (direction == DetourDirection.Bottom)
            {
                // 从下方绕�?- 确保有足够距�?
                detourY = Math.Max(safeStart.Y, maxY + _config.ControlOffset);
            }
            else if (safeStart.Y < minY)
            {
                // 从上方绕�?- 确保有足够距�?
                detourY = Math.Min(safeStart.Y, minY - _config.ControlOffset);
            }
            else if (safeStart.Y > maxY)
            {
                // 从下方绕�?- 确保有足够距�?
                detourY = Math.Max(safeStart.Y, maxY + _config.ControlOffset);
            }
            else
            {
                double topDist = Math.Abs(safeStart.Y - minY);
                double bottomDist = Math.Abs(safeStart.Y - maxY);
                detourY = topDist < bottomDist
                    ? Math.Min(safeStart.Y, minY - _config.ControlOffset)
                    : Math.Max(safeStart.Y, maxY + _config.ControlOffset);
            }

            // 右端口：必须从右侧进�?
            double entryX = context.TargetNode.Position.X + _config.NodeWidth + _config.PathOffset;

            if (safeStart != startPoint)
            {
                segments.Add(safeStart);
            }

            // 确保转折点不在源节点附近
            double safeX = safeStart.X;
            if (safeX > sourceLeft - _config.ControlOffset && 
                safeX < sourceRight + _config.ControlOffset)
            {
                // 转折点在源节点水平范围内，需要调�?
                safeX = sourceRight + _config.ControlOffset;
            }

            segments.Add(new Point(safeX, detourY));
            segments.Add(new Point(entryX, detourY));
            segments.Add(new Point(entryX, endPoint.Y));

            return segments;
        }

        /// <summary>
        /// 上端口绕行策�?
        /// </summary>
        private List<Point> CreateTopPortDetour(PathContext context, double minX, double maxX, double minY, double maxY, DetourDirection direction)
        {
            var segments = new List<Point>();
            Point startPoint = context.StartPoint;
            Point endPoint = context.ArrowTailPoint;

            // 确保起点已经离开源节�?
            Point safeStart = EnsureOutsideSourceNode(startPoint, context.SourceNode);

            // 计算源节点边�?
            double sourceLeft = context.SourceNode.Position.X;
            double sourceRight = context.SourceNode.Position.X + _config.NodeWidth;
            double sourceTop = context.SourceNode.Position.Y;
            double sourceBottom = context.SourceNode.Position.Y + _config.NodeHeight;

            // 策略：从左侧或右侧绕过障碍物，必须从上方进入目标节点
            // 使用节点关系分析器推荐的方向
            double detourX;
            if (direction == DetourDirection.Left)
            {
                // 从左侧绕�?- 确保有足够距�?
                detourX = Math.Min(safeStart.X, minX - _config.ControlOffset);
            }
            else if (direction == DetourDirection.Right)
            {
                // 从右侧绕�?- 确保有足够距�?
                detourX = Math.Max(safeStart.X, maxX + _config.ControlOffset);
            }
            else if (safeStart.X < minX)
            {
                // 从左侧绕�?- 确保有足够距�?
                detourX = Math.Min(safeStart.X, minX - _config.ControlOffset);
            }
            else if (safeStart.X > maxX)
            {
                // 从右侧绕�?- 确保有足够距�?
                detourX = Math.Max(safeStart.X, maxX + _config.ControlOffset);
            }
            else
            {
                double leftDist = Math.Abs(safeStart.X - minX);
                double rightDist = Math.Abs(safeStart.X - maxX);
                detourX = leftDist < rightDist
                    ? Math.Min(safeStart.X, minX - _config.ControlOffset)
                    : Math.Max(safeStart.X, maxX + _config.ControlOffset);
            }

            // 上端口：必须从上方进�?
            double entryY = context.TargetNode.Position.Y - _config.PathOffset;

            if (safeStart != startPoint)
            {
                segments.Add(safeStart);
            }

            // 确保转折点不在源节点附近
            double safeY = safeStart.Y;
            if (safeY > sourceTop - _config.ControlOffset && 
                safeY < sourceBottom + _config.ControlOffset)
            {
                // 转折点在源节点垂直范围内，需要调�?
                safeY = sourceTop - _config.ControlOffset;
            }

            segments.Add(new Point(detourX, safeY));
            segments.Add(new Point(detourX, entryY));
            segments.Add(new Point(endPoint.X, entryY));

            return segments;
        }

        /// <summary>
        /// 下端口绕行策�?
        /// </summary>
        private List<Point> CreateBottomPortDetour(PathContext context, double minX, double maxX, double minY, double maxY, DetourDirection direction)
        {
            var segments = new List<Point>();
            Point startPoint = context.StartPoint;
            Point endPoint = context.ArrowTailPoint;

            // 确保起点已经离开源节�?
            Point safeStart = EnsureOutsideSourceNode(startPoint, context.SourceNode);

            // 计算源节点边�?
            double sourceLeft = context.SourceNode.Position.X;
            double sourceRight = context.SourceNode.Position.X + _config.NodeWidth;
            double sourceTop = context.SourceNode.Position.Y;
            double sourceBottom = context.SourceNode.Position.Y + _config.NodeHeight;

            // 策略：从左侧或右侧绕过障碍物，必须从下方进入目标节点
            // 使用节点关系分析器推荐的方向
            double detourX;
            if (direction == DetourDirection.Left)
            {
                // 从左侧绕�?- 确保有足够距�?
                detourX = Math.Min(safeStart.X, minX - _config.ControlOffset);
            }
            else if (direction == DetourDirection.Right)
            {
                // 从右侧绕�?- 确保有足够距�?
                detourX = Math.Max(safeStart.X, maxX + _config.ControlOffset);
            }
            else if (safeStart.X < minX)
            {
                // 从左侧绕�?- 确保有足够距�?
                detourX = Math.Min(safeStart.X, minX - _config.ControlOffset);
            }
            else if (safeStart.X > maxX)
            {
                // 从右侧绕�?- 确保有足够距�?
                detourX = Math.Max(safeStart.X, maxX + _config.ControlOffset);
            }
            else
            {
                double leftDist = Math.Abs(safeStart.X - minX);
                double rightDist = Math.Abs(safeStart.X - maxX);
                detourX = leftDist < rightDist
                    ? Math.Min(safeStart.X, minX - _config.ControlOffset)
                    : Math.Max(safeStart.X, maxX + _config.ControlOffset);
            }

            // 下端口：必须从下方进�?
            double entryY = context.TargetNode.Position.Y + _config.NodeHeight + _config.PathOffset;

            if (safeStart != startPoint)
            {
                segments.Add(safeStart);
            }

            // 确保转折点不在源节点附近
            double safeY = safeStart.Y;
            if (safeY > sourceTop - _config.ControlOffset && 
                safeY < sourceBottom + _config.ControlOffset)
            {
                // 转折点在源节点垂直范围内，需要调�?
                safeY = sourceBottom + _config.ControlOffset;
            }

            segments.Add(new Point(detourX, safeY));
            segments.Add(new Point(detourX, entryY));
            segments.Add(new Point(endPoint.X, entryY));

            return segments;
        }

        /// <summary>
        /// 确保起点在源节点外部
        /// </summary>
        private Point EnsureOutsideSourceNode(Point point, WorkflowNode sourceNode)
        {
            double nodeLeft = sourceNode.Position.X;
            double nodeRight = sourceNode.Position.X + _config.NodeWidth;
            double nodeTop = sourceNode.Position.Y;
            double nodeBottom = sourceNode.Position.Y + _config.NodeHeight;

            // 如果点已经在节点外部，直接返�?
            if (point.X <= nodeLeft || point.X >= nodeRight ||
                point.Y <= nodeTop || point.Y >= nodeBottom)
            {
                return point;
            }

            // 点在节点内部，选择最近的边移�?
            double distLeft = point.X - nodeLeft;
            double distRight = nodeRight - point.X;
            double distTop = point.Y - nodeTop;
            double distBottom = nodeBottom - point.Y;

            double minDist = Math.Min(Math.Min(distLeft, distRight), Math.Min(distTop, distBottom));

            // 使用较大的安全距离（2 * PathOffset�?
            double safeDistance = 2 * _config.PathOffset;

            if (minDist == distLeft)
            {
                return new Point(nodeLeft - safeDistance, point.Y);
            }
            else if (minDist == distRight)
            {
                return new Point(nodeRight + safeDistance, point.Y);
            }
            else if (minDist == distTop)
            {
                return new Point(point.X, nodeTop - safeDistance);
            }
            else
            {
                return new Point(point.X, nodeBottom + safeDistance);
            }
        }
    }
}
