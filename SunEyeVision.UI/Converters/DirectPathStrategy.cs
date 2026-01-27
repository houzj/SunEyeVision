using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using SunEyeVision.UI.Models;

namespace SunEyeVision.UI.Converters
{
    /// <summary>
    /// 端口位置类型
    /// </summary>
    public enum PortPositionType
    {
        Left,       // 左侧端口
        Right,      // 右侧端口
        Top,        // 顶部端口
        Bottom      // 底部端口
    }

    /// <summary>
    /// 端口组合类型（16种组合的简化分类）
    /// </summary>
    public enum PortCombinationType
    {
        // 相对方向的组合
        LeftToRight,      // 源左侧 -> 目标右侧
        RightToLeft,      // 源右侧 -> 目标左侧
        TopToBottom,      // 源顶部 -> 目标底部
        BottomToTop,      // 源底部 -> 目标顶部
        
        // 同向的组合
        LeftToLeft,       // 源左侧 -> 目标左侧
        RightToRight,     // 源右侧 -> 目标右侧
        TopToTop,         // 源顶部 -> 目标顶部
        BottomToBottom,   // 源底部 -> 目标底部
        
        // 垂直-水平的组合
        LeftToBottom,     // 源左侧 -> 目标底部
        LeftToTop,        // 源左侧 -> 目标顶部
        RightToBottom,    // 源右侧 -> 目标底部
        RightToTop,       // 源右侧 -> 目标顶部
        
        // 水平-垂直的组合
        TopToLeft,        // 源顶部 -> 目标左侧
        TopToRight,       // 源顶部 -> 目标右侧
        BottomToLeft,     // 源底部 -> 目标左侧
        BottomToRight     // 源底部 -> 目标右侧
    }

    /// <summary>
    /// 节点相对位置类型
    /// </summary>
    public enum NodeRelativePosition
    {
        LeftOfTarget,          // 源节点在目标节点左侧
        RightOfTarget,         // 源节点在目标节点右侧
        AboveTarget,           // 源节点在目标节点上方
        BelowTarget,           // 源节点在目标节点下方
        HorizontallyOverlapping,  // 水平方向重叠
        VerticallyOverlapping     // 垂直方向重叠
    }

    /// <summary>
    /// 障碍节点信息
    /// </summary>
    public class ObstacleNode
    {
        public WorkflowNode? Node { get; set; }
        public double Left { get; set; }
        public double Right { get; set; }
        public double Top { get; set; }
        public double Bottom { get; set; }
        public Point Center { get; set; }
    }

    /// <summary>
    /// 直接路径策略 - 智能路径生成
    /// </summary>
    public class DirectPathStrategy : BasePathStrategy
    {
        private readonly NodeRelationshipAnalyzer _relationshipAnalyzer;
        private readonly PathValidator _pathValidator;
        
        // 基础配置
        private const double MinGapThreshold = 5.0;  // 最小间距阈值
        private const double BaseSafeDistance = 20.0;  // 基础安全距离
        private const double MaxSafeDistance = 50.0;   // 最大安全距离
        
        // 过渡点距离因子
        private const double MinTransitionFactor = 0.03;  // 最小过渡因子
        private const double MaxTransitionFactor = 0.08;  // 最大过渡因子

        public DirectPathStrategy(PathConfiguration config, NodeRelationshipAnalyzer? analyzer = null, PathValidator? validator = null)
            : base(config)
        {
            _relationshipAnalyzer = analyzer ?? new NodeRelationshipAnalyzer(config);
            _pathValidator = validator ?? new PathValidator(config);
        }

        public override bool CanHandle(PathContext context)
        {
            // 始终可以处理，内部决定使用智能路径还是通用路径
            return true;
        }

        public override List<Point> CalculatePath(PathContext context)
        {
            var segments = new List<Point>();
            Point startPoint = context.StartPoint;
            Point endPoint = context.ArrowTailPoint;

            // 步骤1: 分析端口组合类型
            PortCombinationType portCombo = DeterminePortCombination(context.SourcePort, context.TargetPort);

            // 步骤2: 分析节点相对位置
            NodeRelativePosition relativePos = AnalyzeNodeRelativePosition(context);

            // 步骤3: 计算源节点和目标节点的最大外接矩形
            var boundingRect = CalculateBoundingRectangle(context);

            // 步骤4: 过滤边界矩形内的障碍节点
            var obstaclesInRect = FilterObstaclesInBoundingRect(context, boundingRect);

            // 步骤5: 计算过渡点
            Point transitionPoint = CalculateTransitionPoint(context, portCombo, relativePos);

            // 步骤6: 计算最小间距（障碍节点之间）
            double minGap = CalculateMinGapInBoundingRect(obstaclesInRect);

            // 步骤7: 计算安全距离
            double safeDistance = CalculateSafeDistance(obstaclesInRect.Count);

            // 步骤8: 生成路径
            // 当有障碍物时，总是生成绕行路径
            if (obstaclesInRect.Count > 0)
            {
                // 有障碍物，使用绕行路径（垂直或水平绕行）
                segments = GenerateSmartPath(context, portCombo, relativePos, transitionPoint, minGap, safeDistance);
            }
            else if (minGap >= MinGapThreshold)
            {
                // 无障碍物且有足够间距，使用最小间距方案
                segments = GenerateSmartPath(context, portCombo, relativePos, transitionPoint, minGap, safeDistance);
            }
            else
            {
                // 无障碍物且间距正常，使用通用直线路径
                segments = GenerateUniversalPathFromTransition_Optimized(context, portCombo, relativePos, transitionPoint);
            }

            return segments;
        }

        /// <summary>
        /// 分析节点位置和相对关系
        /// </summary>
        private void AnalyzeNodePositions(PathContext context)
        {
            // 源节点信息
            double sourceLeft = context.SourceNode.Position.X;
            double sourceRight = context.SourceNode.Position.X + _config.NodeWidth;
            double sourceTop = context.SourceNode.Position.Y;
            double sourceBottom = context.SourceNode.Position.Y + _config.NodeHeight;
            double sourceCenterX = (sourceLeft + sourceRight) / 2;
            double sourceCenterY = (sourceTop + sourceBottom) / 2;

            // 目标节点信息
            double targetLeft = context.TargetNode.Position.X;
            double targetRight = context.TargetNode.Position.X + _config.NodeWidth;
            double targetTop = context.TargetNode.Position.Y;
            double targetBottom = context.TargetNode.Position.Y + _config.NodeHeight;
            double targetCenterX = (targetLeft + targetRight) / 2;
            double targetCenterY = (targetTop + targetBottom) / 2;
        }

        /// <summary>
        /// 确定端口组合类型
        /// </summary>
        private PortCombinationType DeterminePortCombination(PortType sourcePort, PortType targetPort)
        {
            return (sourcePort, targetPort) switch
            {
                (PortType.LeftPort, PortType.RightPort) => PortCombinationType.LeftToRight,
                (PortType.RightPort, PortType.LeftPort) => PortCombinationType.RightToLeft,
                (PortType.TopPort, PortType.BottomPort) => PortCombinationType.TopToBottom,
                (PortType.BottomPort, PortType.TopPort) => PortCombinationType.BottomToTop,
                (PortType.LeftPort, PortType.LeftPort) => PortCombinationType.LeftToLeft,
                (PortType.RightPort, PortType.RightPort) => PortCombinationType.RightToRight,
                (PortType.TopPort, PortType.TopPort) => PortCombinationType.TopToTop,
                (PortType.BottomPort, PortType.BottomPort) => PortCombinationType.BottomToBottom,
                (PortType.LeftPort, PortType.BottomPort) => PortCombinationType.LeftToBottom,
                (PortType.LeftPort, PortType.TopPort) => PortCombinationType.LeftToTop,
                (PortType.RightPort, PortType.BottomPort) => PortCombinationType.RightToBottom,
                (PortType.RightPort, PortType.TopPort) => PortCombinationType.RightToTop,
                (PortType.TopPort, PortType.LeftPort) => PortCombinationType.TopToLeft,
                (PortType.TopPort, PortType.RightPort) => PortCombinationType.TopToRight,
                (PortType.BottomPort, PortType.LeftPort) => PortCombinationType.BottomToLeft,
                (PortType.BottomPort, PortType.RightPort) => PortCombinationType.BottomToRight,
                _ => PortCombinationType.LeftToRight  // 默认值
            };
        }

        /// <summary>
        /// 分析节点相对位置
        /// </summary>
        private NodeRelativePosition AnalyzeNodeRelativePosition(PathContext context)
        {
            double sourceLeft = context.SourceNode.Position.X;
            double sourceRight = context.SourceNode.Position.X + _config.NodeWidth;
            double sourceTop = context.SourceNode.Position.Y;
            double sourceBottom = context.SourceNode.Position.Y + _config.NodeHeight;

            double targetLeft = context.TargetNode.Position.X;
            double targetRight = context.TargetNode.Position.X + _config.NodeWidth;
            double targetTop = context.TargetNode.Position.Y;
            double targetBottom = context.TargetNode.Position.Y + _config.NodeHeight;

            // 检查水平方向关系
            if (sourceRight < targetLeft)
            {
                return NodeRelativePosition.LeftOfTarget;
            }
            else if (sourceLeft > targetRight)
            {
                return NodeRelativePosition.RightOfTarget;
            }

            // 检查垂直方向关系
            if (sourceBottom < targetTop)
            {
                return NodeRelativePosition.AboveTarget;
            }
            else if (sourceTop > targetBottom)
            {
                return NodeRelativePosition.BelowTarget;
            }

            // 检查重叠关系
            double horizontalOverlap = Math.Min(sourceRight, targetRight) - Math.Max(sourceLeft, targetLeft);
            double verticalOverlap = Math.Min(sourceBottom, targetBottom) - Math.Max(sourceTop, targetTop);

            if (horizontalOverlap > 0 && horizontalOverlap > verticalOverlap)
            {
                return NodeRelativePosition.HorizontallyOverlapping;
            }
            else if (verticalOverlap > 0)
            {
                return NodeRelativePosition.VerticallyOverlapping;
            }

            // 默认返回水平重叠
            return NodeRelativePosition.HorizontallyOverlapping;
        }

        /// <summary>
        /// 计算源节点和目标节点的最大外接矩形
        /// </summary>
        private Rect CalculateBoundingRectangle(PathContext context)
        {
            double sourceLeft = context.SourceNode.Position.X;
            double sourceRight = context.SourceNode.Position.X + _config.NodeWidth;
            double sourceTop = context.SourceNode.Position.Y;
            double sourceBottom = context.SourceNode.Position.Y + _config.NodeHeight;

            double targetLeft = context.TargetNode.Position.X;
            double targetRight = context.TargetNode.Position.X + _config.NodeWidth;
            double targetTop = context.TargetNode.Position.Y;
            double targetBottom = context.TargetNode.Position.Y + _config.NodeHeight;

            // 计算包围两个节点的最大矩形
            double minX = Math.Min(sourceLeft, targetLeft);
            double maxX = Math.Max(sourceRight, targetRight);
            double minY = Math.Min(sourceTop, targetTop);
            double maxY = Math.Max(sourceBottom, targetBottom);

            // 计算矩形的宽度和高度
            double rectWidth = maxX - minX;
            double rectHeight = maxY - minY;

            // 使用最大边长作为正方形的边长，增加搜索范围
            double maxSide = Math.Max(rectWidth, rectHeight);

            // 以源节点和目标节点的中心点为基准，构建正方形搜索区域
            double centerX = (minX + maxX) / 2;
            double centerY = (minY + maxY) / 2;

            // 创建基于最大边的正方形
            var boundingRect = new Rect(
                centerX - maxSide / 2,
                centerY - maxSide / 2,
                maxSide,
                maxSide
            );

            return boundingRect;
        }

        /// <summary>
        /// 过滤边界矩形内的障碍节点（O(N)复杂度）
        /// </summary>
        private List<ObstacleNode> FilterObstaclesInBoundingRect(PathContext context, Rect boundingRect)
        {
            var obstacles = new List<ObstacleNode>();

            if (context.Obstacles == null || context.Obstacles.Count == 0)
            {
                return obstacles;
            }

            foreach (var obstacle in context.Obstacles)
            {
                // 排除源节点和目标节点
                if (obstacle.Id == context.SourceNode.Id || obstacle.Id == context.TargetNode.Id)
                {
                    continue;
                }

                double obsLeft = obstacle.Position.X;
                double obsRight = obstacle.Position.X + _config.NodeWidth;
                double obsTop = obstacle.Position.Y;
                double obsBottom = obstacle.Position.Y + _config.NodeHeight;

                // 检查是否在边界矩形内或与边界矩形相交
                bool horizontallyOverlaps = obsRight >= boundingRect.X && obsLeft <= boundingRect.Right;
                bool verticallyOverlaps = obsBottom >= boundingRect.Y && obsTop <= boundingRect.Bottom;
                bool isInBoundingRect = horizontallyOverlaps && verticallyOverlaps;

                if (isInBoundingRect)
                {
                    obstacles.Add(new ObstacleNode
                    {
                        Node = obstacle,
                        Left = obsLeft,
                        Right = obsRight,
                        Top = obsTop,
                        Bottom = obsBottom,
                        Center = new Point((obsLeft + obsRight) / 2, (obsTop + obsBottom) / 2)
                    });
                }
            }

            return obstacles;
        }

        /// <summary>
        /// 计算过渡点（基于相对位置和端口方向）
        /// </summary>
        private Point CalculateTransitionPoint(PathContext context, PortCombinationType portCombo, NodeRelativePosition relativePos)
        {
            Point startPoint = context.StartPoint;

            // 获取源节点边界
            double sourceLeft = context.SourceNode.Position.X;
            double sourceRight = context.SourceNode.Position.X + _config.NodeWidth;
            double sourceTop = context.SourceNode.Position.Y;
            double sourceBottom = context.SourceNode.Position.Y + _config.NodeHeight;

            // 根据端口方向确定过渡距离
            double transitionDistance = CalculateTransitionDistance(context, relativePos);

            // 计算初始过渡点
            Point initialTransition = portCombo switch
            {
                // 相对方向的组合：直接向目标方向延伸
                PortCombinationType.LeftToRight when relativePos == NodeRelativePosition.LeftOfTarget
                    => new Point(startPoint.X + transitionDistance, startPoint.Y),
                PortCombinationType.RightToLeft when relativePos == NodeRelativePosition.RightOfTarget
                    => new Point(startPoint.X - transitionDistance, startPoint.Y),
                PortCombinationType.TopToBottom when relativePos == NodeRelativePosition.AboveTarget
                    => new Point(startPoint.X, startPoint.Y + transitionDistance),
                PortCombinationType.BottomToTop when relativePos == NodeRelativePosition.BelowTarget
                    => new Point(startPoint.X, startPoint.Y - transitionDistance),

                // 同向的组合：先沿端口方向离开源节点，然后根据目标位置调整
                PortCombinationType.LeftToLeft when relativePos == NodeRelativePosition.LeftOfTarget
                    => CalculateTransitionPointLeftToLeft(context, startPoint, transitionDistance),
                PortCombinationType.LeftToLeft when relativePos == NodeRelativePosition.RightOfTarget
                    => new Point(startPoint.X - transitionDistance, startPoint.Y),
                PortCombinationType.RightToRight when relativePos == NodeRelativePosition.RightOfTarget
                    => CalculateTransitionPointRightToRight(context, startPoint, transitionDistance),
                PortCombinationType.RightToRight when relativePos == NodeRelativePosition.LeftOfTarget
                    => new Point(startPoint.X + transitionDistance, startPoint.Y),
                PortCombinationType.TopToTop when relativePos == NodeRelativePosition.AboveTarget
                    => CalculateTransitionPointTopToTop(context, startPoint, transitionDistance),
                PortCombinationType.TopToTop when relativePos == NodeRelativePosition.BelowTarget
                    => new Point(startPoint.X, startPoint.Y + transitionDistance),
                PortCombinationType.BottomToBottom when relativePos == NodeRelativePosition.BelowTarget
                    => CalculateTransitionPointBottomToBottom(context, startPoint, transitionDistance),
                PortCombinationType.BottomToBottom when relativePos == NodeRelativePosition.AboveTarget
                    => new Point(startPoint.X, startPoint.Y - transitionDistance),

                // 垂直-水平组合：基于相对位置选择
                PortCombinationType.LeftToBottom when relativePos == NodeRelativePosition.LeftOfTarget
                    => new Point(startPoint.X + transitionDistance, startPoint.Y),
                PortCombinationType.LeftToTop when relativePos == NodeRelativePosition.LeftOfTarget
                    => new Point(startPoint.X + transitionDistance, startPoint.Y),
                PortCombinationType.RightToBottom when relativePos == NodeRelativePosition.RightOfTarget
                    => new Point(startPoint.X - transitionDistance, startPoint.Y),
                PortCombinationType.RightToTop when relativePos == NodeRelativePosition.RightOfTarget
                    => new Point(startPoint.X - transitionDistance, startPoint.Y),

                // 水平-垂直组合：基于相对位置选择
                PortCombinationType.TopToLeft when relativePos == NodeRelativePosition.AboveTarget
                    => new Point(startPoint.X, startPoint.Y + transitionDistance),
                PortCombinationType.TopToRight when relativePos == NodeRelativePosition.AboveTarget
                    => new Point(startPoint.X, startPoint.Y + transitionDistance),
                PortCombinationType.BottomToLeft when relativePos == NodeRelativePosition.BelowTarget
                    => new Point(startPoint.X, startPoint.Y - transitionDistance),
                PortCombinationType.BottomToRight when relativePos == NodeRelativePosition.BelowTarget
                    => new Point(startPoint.X, startPoint.Y - transitionDistance),

                // 默认情况：基于端口方向
                _ when context.SourcePort == PortType.LeftPort => new Point(startPoint.X - transitionDistance, startPoint.Y),
                _ when context.SourcePort == PortType.RightPort => new Point(startPoint.X + transitionDistance, startPoint.Y),
                _ when context.SourcePort == PortType.TopPort => new Point(startPoint.X, startPoint.Y - transitionDistance),
                _ when context.SourcePort == PortType.BottomPort => new Point(startPoint.X, startPoint.Y + transitionDistance),
                _ => startPoint  // 默认不添加过渡点
            };

            // 确保过渡点在源节点外部
            // 确定过渡点的主轴方向（基于初始过渡点相对于起点的移动方向）
            bool isHorizontalTransition = Math.Abs(initialTransition.X - startPoint.X) > 0.01;
            bool isVerticalTransition = Math.Abs(initialTransition.Y - startPoint.Y) > 0.01;

            // 检查X坐标是否在源节点范围内
            if (initialTransition.X >= sourceLeft && initialTransition.X <= sourceRight)
            {
                // 如果在X范围内，需要调整X坐标到范围外
                // 注意：只在X轴为主要移动方向时才调整
                if (startPoint.X <= sourceLeft)
                {
                    // 起点在左侧，向左移出
                    initialTransition = new Point(sourceLeft - 10, initialTransition.Y);
                }
                else
                {
                    // 起点在右侧，向右移出
                    initialTransition = new Point(sourceRight + 10, initialTransition.Y);
                }
            }

            // 检查Y坐标是否在源节点范围内
            // 只在Y轴为主要移动方向时才调整Y坐标
            if (isVerticalTransition && initialTransition.Y >= sourceTop && initialTransition.Y <= sourceBottom)
            {
                // 如果在Y范围内，需要调整Y坐标到范围外
                if (startPoint.Y <= sourceTop)
                {
                    // 起点在上方，向上移出
                    initialTransition = new Point(initialTransition.X, sourceTop - 10);
                }
                else
                {
                    // 起点在下方，向下移出
                    initialTransition = new Point(initialTransition.X, sourceBottom + 10);
                }
            }

            return initialTransition;
        }

        /// <summary>
        /// 计算过渡距离（基于节点间距和相对位置）
        /// </summary>
        private double CalculateTransitionDistance(PathContext context, NodeRelativePosition relativePos)
        {
            Point sourcePos = context.SourceNode.Position;
            Point targetPos = context.TargetNode.Position;
            
            // 计算节点间距
            double distance;
            switch (relativePos)
            {
                case NodeRelativePosition.LeftOfTarget:
                case NodeRelativePosition.RightOfTarget:
                    distance = Math.Abs(targetPos.X - sourcePos.X);
                    break;
                case NodeRelativePosition.AboveTarget:
                case NodeRelativePosition.BelowTarget:
                    distance = Math.Abs(targetPos.Y - sourcePos.Y);
                    break;
                default:
                    // 重叠情况，使用最大维度
                    distance = Math.Max(
                        Math.Abs(targetPos.X - sourcePos.X),
                        Math.Abs(targetPos.Y - sourcePos.Y)
                    );
                    break;
            }
            
            // 使用动态因子（0.03-0.08）
            double factor = Math.Clamp(distance / 1000.0, MinTransitionFactor, MaxTransitionFactor);
            double transitionDistance = distance * factor;
            
            // 确保最小值
            return Math.Max(transitionDistance, 10.0);
        }

        /// <summary>
        /// 计算最小间距（障碍节点之间，O(N)复杂度）
        /// </summary>
        private double CalculateMinGapInBoundingRect(List<ObstacleNode> obstacles)
        {
            if (obstacles.Count == 0)
            {
                return double.MaxValue;
            }

            if (obstacles.Count == 1)
            {
                return double.MaxValue;
            }

            // 投影优化：分别计算X方向和Y方向的最小间距
            double minGap = double.MaxValue;

            // X方向投影
            obstacles.Sort((a, b) => a.Left.CompareTo(b.Left));
            for (int i = 0; i < obstacles.Count - 1; i++)
            {
                double gap = obstacles[i + 1].Left - obstacles[i].Right;
                if (gap < minGap)
                {
                    minGap = gap;
                }
            }

            // Y方向投影
            obstacles.Sort((a, b) => a.Top.CompareTo(b.Top));
            for (int i = 0; i < obstacles.Count - 1; i++)
            {
                double gap = obstacles[i + 1].Top - obstacles[i].Bottom;
                if (gap < minGap)
                {
                    minGap = gap;
                }
            }

            return minGap;
        }

        /// <summary>
        /// 计算安全距离（基于障碍数量）
        /// </summary>
        private double CalculateSafeDistance(int obstacleCount)
        {
            // 基础距离 × (1.0 + 障碍数量 × 0.1)
            double factor = 1.0 + obstacleCount * 0.1;
            double distance = BaseSafeDistance * factor;

            // 如果有障碍物，增加额外的安全距离
            if (obstacleCount > 0)
            {
                distance += 10.0; // 额外的避障空间
            }

            // 限制在15-60px之间（提高了上限）
            return Math.Clamp(distance, 15.0, MaxSafeDistance + 10.0);
        }

        /// <summary>
        /// 生成智能路径（最小间距方案）- 简化版本
        /// </summary>
        private List<Point> GenerateSmartPath(PathContext context, PortCombinationType portCombo, 
            NodeRelativePosition relativePos, Point transitionPoint, double minGap, double safeDistance)
        {
            // 使用优化后的统一路径生成
            return GenerateUniversalPathFromTransition_Optimized(context, portCombo, relativePos, transitionPoint);
        }

        /// <summary>
        /// 生成通用路径（允许穿过障碍）
        /// </summary>
        private List<Point> GenerateUniversalPathFromTransition_Optimized(PathContext context, PortCombinationType portCombo, NodeRelativePosition relativePos, Point transitionPoint)
        {
            var path = new List<Point>();
            Point startPoint = context.StartPoint;
            Point endPoint = context.ArrowTailPoint;

            // 如果起点和过渡点相同，直接从起点开始
            Point actualStart = IsSamePoint(startPoint, transitionPoint) ? startPoint : transitionPoint;

            // 生成简单的正交路径（最多2个转折点）
            switch (portCombo)
            {
                case PortCombinationType.LeftToRight:
                case PortCombinationType.RightToLeft:
                    // 水平连接
                    path.Add(new Point(endPoint.X, actualStart.Y));
                    path.Add(endPoint);
                    break;

                case PortCombinationType.TopToBottom:
                case PortCombinationType.BottomToTop:
                    // 垂直连接
                    path.Add(new Point(actualStart.X, endPoint.Y));
                    path.Add(endPoint);
                    break;

                case PortCombinationType.LeftToBottom:
                case PortCombinationType.LeftToTop:
                case PortCombinationType.RightToBottom:
                case PortCombinationType.RightToTop:
                    // 垂直-水平组合
                    path.Add(new Point(actualStart.X, endPoint.Y));
                    path.Add(endPoint);
                    break;

                case PortCombinationType.TopToLeft:
                case PortCombinationType.TopToRight:
                case PortCombinationType.BottomToLeft:
                case PortCombinationType.BottomToRight:
                    // 水平-垂直组合
                    path.Add(new Point(endPoint.X, actualStart.Y));
                    path.Add(endPoint);
                    break;

                case PortCombinationType.LeftToLeft:
                case PortCombinationType.RightToRight:
                    // 同向水平组合
                    path.Add(new Point(actualStart.X, endPoint.Y));
                    path.Add(endPoint);
                    break;

                case PortCombinationType.TopToTop:
                case PortCombinationType.BottomToBottom:
                    // 同向垂直组合
                    path.Add(new Point(endPoint.X, actualStart.Y));
                    path.Add(endPoint);
                    break;

                default:
                    // 默认：使用简单的L型路径
                    if (relativePos == NodeRelativePosition.LeftOfTarget || 
                        relativePos == NodeRelativePosition.RightOfTarget)
                    {
                        // 水平优先
                        path.Add(new Point(endPoint.X, actualStart.Y));
                        path.Add(endPoint);
                    }
                    else
                    {
                        // 垂直优先
                        path.Add(new Point(actualStart.X, endPoint.Y));
                        path.Add(endPoint);
                    }
                    break;
            }

            return path;
        }

        /// <summary>
        /// 检查两点是否相同
        /// </summary>
        private bool IsSamePoint(Point p1, Point p2)
        {
            return Math.Abs(p1.X - p2.X) < 0.01 && Math.Abs(p1.Y - p2.Y) < 0.01;
        }

        /// <summary>
        /// 计算LeftToLeft的过渡点
        /// </summary>
        private Point CalculateTransitionPointLeftToLeft(PathContext context, Point startPoint, double transitionDistance)
        {
            // 简化实现
            return new Point(startPoint.X - transitionDistance, startPoint.Y);
        }

        /// <summary>
        /// 计算RightToRight的过渡点
        /// </summary>
        private Point CalculateTransitionPointRightToRight(PathContext context, Point startPoint, double transitionDistance)
        {
            // 简化实现
            return new Point(startPoint.X + transitionDistance, startPoint.Y);
        }

        /// <summary>
        /// 计算TopToTop的过渡点
        /// </summary>
        private Point CalculateTransitionPointTopToTop(PathContext context, Point startPoint, double transitionDistance)
        {
            // 简化实现
            return new Point(startPoint.X, startPoint.Y - transitionDistance);
        }

        /// <summary>
        /// 计算BottomToBottom的过渡点
        /// </summary>
        private Point CalculateTransitionPointBottomToBottom(PathContext context, Point startPoint, double transitionDistance)
        {
            // 简化实现
            return new Point(startPoint.X, startPoint.Y + transitionDistance);
        }
    }
}