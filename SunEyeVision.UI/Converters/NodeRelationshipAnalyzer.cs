using System;
using System.Windows;
using SunEyeVision.UI.Models;

namespace SunEyeVision.UI.Converters
{
    /// <summary>
    /// 节点关系分析�?- 分析源节点和目标节点的相对位置关�?
    /// </summary>
    public class NodeRelationshipAnalyzer
    {
        private readonly PathConfiguration _config;

        public NodeRelationshipAnalyzer(PathConfiguration config)
        {
            _config = config ?? new PathConfiguration();
        }

        /// <summary>
        /// 节点相对位置枚举
        /// </summary>
        public enum RelativePosition
        {
            North,       // 目标在源节点正上�?
            South,       // 目标在源节点正下�?
            West,        // 目标在源节点正左�?
            East,        // 目标在源节点正右�?
            NorthWest,   // 目标在源节点左上�?
            NorthEast,   // 目标在源节点右上�?
            SouthWest,   // 目标在源节点左下�?
            SouthEast,   // 目标在源节点右下�?
            Overlapping  // 节点重叠
        }

        /// <summary>
        /// 节点与端口相对关系枚�?
        /// </summary>
        public enum NodePortRelationship
        {
            PortAboveNode,     // 端口在节点上�?
            PortBelowNode,     // 端口在节点下�?
            PortLeftOfNode,    // 端口在节点左�?
            PortRightOfNode,   // 端口在节点右�?
            PortAtNodeCorner,  // 端口在节点角�?
            PortInsideNode     // 端口在节点内�?
        }

        /// <summary>
        /// 分析源节点和目标节点的相对位�?
        /// </summary>
        public RelativePosition AnalyzeNodePosition(WorkflowNode sourceNode, WorkflowNode targetNode)
        {
            // 获取源节点和目标节点的中心点
            Point sourceCenter = GetNodeCenter(sourceNode);
            Point targetCenter = GetNodeCenter(targetNode);

            // 计算水平距离和垂直距�?
            double xDiff = targetCenter.X - sourceCenter.X;
            double yDiff = targetCenter.Y - sourceCenter.Y;

            // 检查是否重�?
            double threshold = 10; // 重叠阈�?
            if (Math.Abs(xDiff) < threshold && Math.Abs(yDiff) < threshold)
            {
                return RelativePosition.Overlapping;
            }

            // 判断主要方向
            double absXDiff = Math.Abs(xDiff);
            double absYDiff = Math.Abs(yDiff);

            // 使用对角线分割线判断象限
            bool isDominantX = absXDiff > absYDiff;

            if (yDiff < 0) // 目标在上�?
            {
                if (isDominantX)
                {
                    return xDiff < 0 ? RelativePosition.NorthWest : RelativePosition.NorthEast;
                }
                return RelativePosition.North;
            }
            else // 目标在下�?
            {
                if (isDominantX)
                {
                    return xDiff < 0 ? RelativePosition.SouthWest : RelativePosition.SouthEast;
                }
                return RelativePosition.South;
            }
        }

        /// <summary>
        /// 分析节点与目标端口的关系
        /// </summary>
        public NodePortRelationship AnalyzeNodePortRelationship(WorkflowNode node, Point portPoint, PortType portType)
        {
            Rect nodeBounds = GetNodeBounds(node);

            // 检查端口是否在节点内部
            if (nodeBounds.Contains(portPoint))
            {
                return NodePortRelationship.PortInsideNode;
            }

            // 计算端口相对于节点的位置
            double nodeCenterX = nodeBounds.Left + nodeBounds.Width / 2;
            double nodeCenterY = nodeBounds.Top + nodeBounds.Height / 2;
            double xDiff = portPoint.X - nodeCenterX;
            double yDiff = portPoint.Y - nodeCenterY;

            // 根据端口类型判断相对关系
            switch (portType)
            {
                case PortType.TopPort:
                    if (portPoint.Y < nodeBounds.Top)
                    {
                        return NodePortRelationship.PortAboveNode;
                    }
                    break;

                case PortType.BottomPort:
                    if (portPoint.Y > nodeBounds.Bottom)
                    {
                        return NodePortRelationship.PortBelowNode;
                    }
                    break;

                case PortType.LeftPort:
                    if (portPoint.X < nodeBounds.Left)
                    {
                        return NodePortRelationship.PortLeftOfNode;
                    }
                    break;

                case PortType.RightPort:
                    if (portPoint.X > nodeBounds.Right)
                    {
                        return NodePortRelationship.PortRightOfNode;
                    }
                    break;
            }

            // 默认情况�?根据位置判断
            if (Math.Abs(xDiff) > Math.Abs(yDiff))
            {
                return xDiff < 0 ? NodePortRelationship.PortLeftOfNode : NodePortRelationship.PortRightOfNode;
            }
            else
            {
                return yDiff < 0 ? NodePortRelationship.PortAboveNode : NodePortRelationship.PortBelowNode;
            }
        }

        /// <summary>
        /// 判断是否需要绕行源节点
        /// </summary>
        public bool ShouldDetourAroundSourceNode(PathContext context)
        {
            // 获取源节点边�?
            Rect sourceBounds = GetNodeBounds(context.SourceNode);

            // 检查起点是否在源节点边界内或附�?
            Point startPoint = context.StartPoint;
            double safeDistance = _config.PathOffset * 1.5;

            // 扩展边界（严格检查是否在边界上）
            double leftBound = sourceBounds.Left - safeDistance;
            double rightBound = sourceBounds.Right + safeDistance;
            double topBound = sourceBounds.Top - safeDistance;
            double bottomBound = sourceBounds.Bottom + safeDistance;

            // 使用严格不等式，避免边界上的误判
            // 只有当起点在节点内部时才需要绕�?
            if (startPoint.X > sourceBounds.Left &&
                startPoint.X < sourceBounds.Right &&
                startPoint.Y > sourceBounds.Top &&
                startPoint.Y < sourceBounds.Bottom)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 判断是否需要绕行目标节�?
        /// </summary>
        public bool ShouldDetourAroundTargetNode(PathContext context)
        {
            // 获取目标节点边界
            Rect targetBounds = GetNodeBounds(context.TargetNode);

            // 检查终点和箭尾点是否在目标节点边界内或附近
            Point endPoint = context.EndPoint;
            Point arrowTailPoint = context.ArrowTailPoint;
            double safeDistance = _config.PathOffset * 1.5;

            // 如果终点或箭尾点在目标节点安全距离内,需要绕�?
            if ((endPoint.X >= targetBounds.Left - safeDistance &&
                 endPoint.X <= targetBounds.Right + safeDistance &&
                 endPoint.Y >= targetBounds.Top - safeDistance &&
                 endPoint.Y <= targetBounds.Bottom + safeDistance) ||
                (arrowTailPoint.X >= targetBounds.Left - safeDistance &&
                 arrowTailPoint.X <= targetBounds.Right + safeDistance &&
                 arrowTailPoint.Y >= targetBounds.Top - safeDistance &&
                 arrowTailPoint.Y <= targetBounds.Bottom + safeDistance))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 确定最优的绕行方向
        /// </summary>
        public DetourDirection DetermineOptimalDetourDirection(PathContext context)
        {
            // 分析节点相对位置
            RelativePosition nodePos = AnalyzeNodePosition(context.SourceNode, context.TargetNode);

            // 分析源节点与起点的关�?
            NodePortRelationship sourcePortRel = AnalyzeNodePortRelationship(
                context.SourceNode,
                context.StartPoint,
                context.SourcePort
            );

            // 分析目标节点与目标端口的关系
            NodePortRelationship targetPortRel = AnalyzeNodePortRelationship(
                context.TargetNode,
                context.ArrowTailPoint,
                context.TargetPort
            );

            // 根据端口类型决定绕行方向
            switch (context.TargetPort)
            {
                case PortType.LeftPort:
                    return DetermineLeftPortDetour(nodePos, sourcePortRel, targetPortRel);

                case PortType.RightPort:
                    return DetermineRightPortDetour(nodePos, sourcePortRel, targetPortRel);

                case PortType.TopPort:
                    return DetermineTopPortDetour(nodePos, sourcePortRel, targetPortRel);

                case PortType.BottomPort:
                    return DetermineBottomPortDetour(nodePos, sourcePortRel, targetPortRel);

                default:
                    return DetourDirection.Auto;
            }
        }

        /// <summary>
        /// 确定左端口的最优绕行方�?
        /// </summary>
        private DetourDirection DetermineLeftPortDetour(
            RelativePosition nodePos,
            NodePortRelationship sourcePortRel,
            NodePortRelationship targetPortRel)
        {
            // 左端�?必须从左侧进�?所以垂直方向绕行最�?
            // 优先从上方绕�?避免从下方进入时穿过源节�?

            if (sourcePortRel == NodePortRelationship.PortBelowNode)
            {
                // 源节点在起点下方,从下方绕行更安全
                return DetourDirection.Bottom;
            }
            else if (sourcePortRel == NodePortRelationship.PortAboveNode)
            {
                // 源节点在起点上方,从上方绕行更安全
                return DetourDirection.Top;
            }
            else
            {
                // 根据目标节点相对位置决定
                if (nodePos == RelativePosition.North || nodePos == RelativePosition.NorthWest || nodePos == RelativePosition.NorthEast)
                {
                    return DetourDirection.Top;
                }
                else
                {
                    return DetourDirection.Bottom;
                }
            }
        }

        /// <summary>
        /// 确定右端口的最优绕行方�?
        /// </summary>
        private DetourDirection DetermineRightPortDetour(
            RelativePosition nodePos,
            NodePortRelationship sourcePortRel,
            NodePortRelationship targetPortRel)
        {
            // 右端�?必须从右侧进�?所以垂直方向绕行最�?

            if (sourcePortRel == NodePortRelationship.PortBelowNode)
            {
                return DetourDirection.Bottom;
            }
            else if (sourcePortRel == NodePortRelationship.PortAboveNode)
            {
                return DetourDirection.Top;
            }
            else
            {
                if (nodePos == RelativePosition.North || nodePos == RelativePosition.NorthWest || nodePos == RelativePosition.NorthEast)
                {
                    return DetourDirection.Top;
                }
                else
                {
                    return DetourDirection.Bottom;
                }
            }
        }

        /// <summary>
        /// 确定上端口的最优绕行方�?
        /// </summary>
        private DetourDirection DetermineTopPortDetour(
            RelativePosition nodePos,
            NodePortRelationship sourcePortRel,
            NodePortRelationship targetPortRel)
        {
            // 上端�?必须从上方进�?所以水平方向绕行最�?

            if (sourcePortRel == NodePortRelationship.PortRightOfNode)
            {
                return DetourDirection.Right;
            }
            else if (sourcePortRel == NodePortRelationship.PortLeftOfNode)
            {
                return DetourDirection.Left;
            }
            else
            {
                if (nodePos == RelativePosition.West || nodePos == RelativePosition.NorthWest || nodePos == RelativePosition.SouthWest)
                {
                    return DetourDirection.Left;
                }
                else
                {
                    return DetourDirection.Right;
                }
            }
        }

        /// <summary>
        /// 确定下端口的最优绕行方�?
        /// </summary>
        private DetourDirection DetermineBottomPortDetour(
            RelativePosition nodePos,
            NodePortRelationship sourcePortRel,
            NodePortRelationship targetPortRel)
        {
            // 下端�?必须从下方进�?所以水平方向绕行最�?

            if (sourcePortRel == NodePortRelationship.PortRightOfNode)
            {
                return DetourDirection.Right;
            }
            else if (sourcePortRel == NodePortRelationship.PortLeftOfNode)
            {
                return DetourDirection.Left;
            }
            else
            {
                if (nodePos == RelativePosition.West || nodePos == RelativePosition.NorthWest || nodePos == RelativePosition.SouthWest)
                {
                    return DetourDirection.Left;
                }
                else
                {
                    return DetourDirection.Right;
                }
            }
        }

        /// <summary>
        /// 计算安全的绕行点(避开源节�?
        /// </summary>
        public Point CalculateSafeDetourPoint(PathContext context, DetourDirection direction)
        {
            Rect sourceBounds = GetNodeBounds(context.SourceNode);
            Point startPoint = context.StartPoint;
            double offset = _config.ControlOffset;

            switch (direction)
            {
                case DetourDirection.Top:
                    return new Point(startPoint.X, Math.Min(startPoint.Y, sourceBounds.Top - offset));

                case DetourDirection.Bottom:
                    return new Point(startPoint.X, Math.Max(startPoint.Y, sourceBounds.Bottom + offset));

                case DetourDirection.Left:
                    return new Point(Math.Min(startPoint.X, sourceBounds.Left - offset), startPoint.Y);

                case DetourDirection.Right:
                    return new Point(Math.Max(startPoint.X, sourceBounds.Right + offset), startPoint.Y);

                default:
                    return startPoint;
            }
        }

        /// <summary>
        /// 获取节点的中心点
        /// </summary>
        private Point GetNodeCenter(WorkflowNode node)
        {
            return new Point(
                node.Position.X + _config.NodeWidth / 2,
                node.Position.Y + _config.NodeHeight / 2
            );
        }

        /// <summary>
        /// 获取节点的边�?
        /// </summary>
        private Rect GetNodeBounds(WorkflowNode node)
        {
            return new Rect(
                node.Position.X,
                node.Position.Y,
                _config.NodeWidth,
                _config.NodeHeight
            );
        }

        /// <summary>
        /// 计算安全距离(节点间的最小间�?
        /// </summary>
        public double CalculateSafeDistance(WorkflowNode sourceNode, WorkflowNode targetNode)
        {
            // 计算节点间的中心距离
            Point sourceCenter = GetNodeCenter(sourceNode);
            Point targetCenter = GetNodeCenter(targetNode);
            double distance = Math.Sqrt(
                Math.Pow(targetCenter.X - sourceCenter.X, 2) +
                Math.Pow(targetCenter.Y - sourceCenter.Y, 2)
            );

            // 安全距离 = 中心距离 - 节点尺寸的一�?
            return distance - Math.Sqrt(
                Math.Pow(_config.NodeWidth / 2, 2) +
                Math.Pow(_config.NodeHeight / 2, 2)
            );
        }
    }

    /// <summary>
    /// 绕行方向枚举
    /// </summary>
    public enum DetourDirection
    {
        Top,       // 向上绕行
        Bottom,    // 向下绕行
        Left,      // 向左绕行
        Right,     // 向右绕行
        Auto       // 自动选择
    }
}
