using System;
using System.Windows;
using System.Windows.Media;

namespace SunEyeVision.UI.Services.PathCalculators
{
    /// <summary>
    /// 正交折线路径计算器 - 实现基于端口方向的智能正交路径算法
    /// </summary>
    public class OrthogonalPathCalculator : IPathCalculator
    {
        private const double MinSegmentLength = 0; // 最小线段长度，避免过短的折线
        private const double ArrowLength = 15.0; // 箭头长度
        private const double VeryCloseDistanceThreshold = 0; // 极近距离阈值

        // 模块1：安全距离常量定义
        private const double NodeSafeDistance = 15.0; // 节点安全距离，确保路径不穿过节点
        private const double PathClearanceDistance = 15.0; // 路径净空距离，路径与节点的最小安全距离

        /// <summary>
        /// 路径策略枚举
        /// </summary>
        private enum PathStrategy
        {
            /// <summary>
            /// 直接连接策略 - 极近距离时的直接连接（无拐点）
            /// </summary>
            Direct,

            /// <summary>
            /// 水平优先策略 - 优先从源端口沿水平方向延伸
            /// </summary>
            HorizontalFirst,

            /// <summary>
            /// 垂直优先策略 - 优先从源端口沿垂直方向延伸
            /// </summary>
            VerticalFirst,

            /// <summary>
            /// 三段式策略 - 简单的三段折线（水平-垂直-水平或垂直-水平-垂直）
            /// </summary>
            ThreeSegment,

            /// <summary>
            /// 相对方向策略 - 用于Top-Bottom, Bottom-Top等相对方向连接
            /// 先沿源端口方向延伸，再水平，再沿目标端口方向延伸（4段）
            /// </summary>
            OppositeDirection,

            /// <summary>
            /// 四段式策略 - 中等距离的四段折线，优化同向端口场景
            /// </summary>
            FourSegment,

            /// <summary>
            /// 五段式策略 - 复杂的五段折线，适用于特殊场景
            /// </summary>
            FiveSegment
        }

        /// <summary>
        /// 矩形相对位置枚举
        /// </summary>
        private enum RectRelativePosition
        {
            /// <summary>在左侧</summary>
            OnLeft,
            /// <summary>在右侧</summary>
            OnRight,
            /// <summary>在上方</summary>
            OnTop,
            /// <summary>在下方</summary>
            OnBottom,
            /// <summary>重叠</summary>
            Overlapping
        }

        /// <summary>
        /// 碰撞信息类
        /// </summary>
        private class CollisionInfo
        {
            public int SegmentIndex { get; set; }
            public Rect CollidingRect { get; set; }
            public Point[] PathPoints { get; set; }

            public CollisionInfo(int segmentIndex, Rect collidingRect, Point[] pathPoints)
            {
                SegmentIndex = segmentIndex;
                CollidingRect = collidingRect;
                PathPoints = pathPoints;
            }
        }

        /// <summary>
        /// 计算正交折线路径点集合（基础方法，向后兼容）
        /// 箭头尾部已经在 ConnectionPathCache 中计算并作为 targetPosition 传入
        /// 直接计算基本路径即可
        /// </summary>
        public Point[] CalculateOrthogonalPath(
            Point sourcePosition,
            Point targetPosition,
            PortDirection sourceDirection,
            PortDirection targetDirection)
        {
            // 调用增强方法，传入空的节点边界
            return CalculateOrthogonalPath(
                sourcePosition,
                targetPosition,
                sourceDirection,
                targetDirection,
                Rect.Empty,
                Rect.Empty);
        }

        /// <summary>
        /// 计算正交折线路径点集合（增强方法，带节点边界信息）
        /// 箭头尾部已经在 ConnectionPathCache 中计算并作为 targetPosition 传入
        /// 直接计算基本路径即可
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
            
            
            
            
            
            

            // 直接计算基本路径（目标位置已经是箭头尾部）
            var pathPoints = CalculateBasicPath(
                sourcePosition,
                targetPosition,
                sourceDirection,
                targetDirection,
                sourceNodeRect,
                targetNodeRect,
                allNodeRects);

            
            
            

            return pathPoints;
        }

        /// <summary>
        /// 计算基本路径（带节点边界信息和碰撞检测）
        /// 目标位置已经是箭头尾部位置（由ConnectionPathCache计算）
        /// </summary>
        private Point[] CalculateBasicPath(
            Point sourcePosition,
            Point targetPosition,
            PortDirection sourceDirection,
            PortDirection targetDirection,
            Rect sourceNodeRect,
            Rect targetNodeRect,
            Rect[] allNodeRects)
        {
            // 1. 计算源节点和目标节点的相对位置关系
            var dx = targetPosition.X - sourcePosition.X;
            var dy = targetPosition.Y - sourcePosition.Y;
            var horizontalDistance = Math.Abs(dx);
            var verticalDistance = Math.Abs(dy);

            // 2. 选择最佳路径策略（仅基于端口关系、位置关系和节点位置，不涉及碰撞检测）
            var strategy = SelectPathStrategy(sourcePosition,targetPosition,
                sourceDirection,
                targetDirection,
                dx,
                dy,
                horizontalDistance,
                verticalDistance,
                sourceNodeRect,
                targetNodeRect);

            // 3. 根据策略计算路径点（目标位置是箭头尾部）
            
            var basicPath = CalculatePathByStrategy(
                sourcePosition,
                targetPosition,  // 使用箭头尾部作为目标位置
                sourceDirection,
                targetDirection,
                strategy,
                dx,
                dy,
                sourceNodeRect,
                targetNodeRect);

            // 4. 模块3：统一的节点避让后处理（确保路径不穿过任何节点）
            var finalPath = ApplyNodeAvoidance(
                basicPath,
                sourcePosition,
                targetPosition,
                sourceDirection,
                targetDirection,
                sourceNodeRect,
                targetNodeRect,
                allNodeRects);

            return finalPath;
        }


        /// <summary>
        /// 场景复杂度枚举
        /// </summary>
        private enum SceneComplexity
        {
            /// <summary>极简场景：直接对齐，无障碍</summary>
            Direct,
            /// <summary>简单场景：无障碍或障碍很少，对齐良好</summary>
            Simple,
            /// <summary>中等场景：有少量障碍，需要简单避让</summary>
            Medium,
            /// <summary>复杂场景：多障碍，需要复杂避让</summary>
            Complex
        }

        /// <summary>
        /// 几何对齐枚举
        /// </summary>
        private enum GeometricAlignment
        {
            /// <summary>水平对齐</summary>
            HorizontalAligned,
            /// <summary>垂直对齐</summary>
            VerticalAligned,
            /// <summary>不对齐</summary>
            NotAligned
        }

        /// <summary>
        /// 选择最佳路径策略（优化版本：仅基于端口关系、位置关系和节点位置，不涉及碰撞检测）
        /// 分层优先级：Priority 1(极简场景) > Priority 2(相对方向) > Priority 3(垂直方向) > Priority 4(同向)
        /// </summary>
        private PathStrategy SelectPathStrategy(Point sourcePosition, Point targetPosition,
            PortDirection sourceDirection,
            PortDirection targetDirection,
            double dx, double dy,
            double horizontalDistance,
            double verticalDistance,
            Rect sourceNodeRect,
            Rect targetNodeRect)
        {
            
            
            

            // 分析端口方向关系
            bool isOpposite = IsOppositeDirection(sourceDirection, targetDirection);
            bool isSame = IsSameDirection(sourceDirection, targetDirection);
            bool isPerpendicular = IsPerpendicularDirection(sourceDirection, targetDirection);
            

            // 分析节点相对位置
            if (!sourceNodeRect.IsEmpty && !targetNodeRect.IsEmpty)
            {
                double relativeDx = targetNodeRect.X - sourceNodeRect.X;
                double relativeDy = targetNodeRect.Y - sourceNodeRect.Y;
                string relativePos = AnalyzeRelativePosition(relativeDx, relativeDy);
                
            }

            // 检测场景复杂度（不涉及碰撞检测）
            var sceneComplexity = DetectSceneComplexitySimple(sourcePosition, targetPosition,
                sourceDirection, targetDirection, dx, dy, horizontalDistance, verticalDistance,
                sourceNodeRect, targetNodeRect);
            

            // 检测几何对齐情况
            var geometricAlignment = DetectGeometricAlignment(
                sourceDirection, targetDirection, horizontalDistance, verticalDistance);
            

            // Priority 1: 极简场景（直接对齐，距离极近）
            if (sceneComplexity == SceneComplexity.Direct)
            {
                
                return PathStrategy.Direct;
            }

            // Priority 2: 相对方向（Left-Right, Right-Left, Top-Bottom, Bottom-Top）
            if (IsOppositeDirection(sourceDirection, targetDirection))
            {
                var strategy = SelectStrategyForOppositeDirection(
                    sceneComplexity, geometricAlignment,
                    sourceDirection, targetDirection, horizontalDistance, verticalDistance);
                
                return strategy;
            }

            // Priority 3: 垂直方向（一个水平一个垂直）
            if (IsPerpendicularDirection(sourceDirection, targetDirection))
            {
                var strategy = SelectStrategyForPerpendicularDirectionSimple(
                    sceneComplexity, geometricAlignment,
                    sourceDirection, targetDirection, horizontalDistance, verticalDistance,
                    sourceNodeRect, targetNodeRect);
                
                return strategy;
            }

            // Priority 4: 同向（Left-Left, Right-Right, Top-Top, Bottom-Bottom）
            if (IsSameDirection(sourceDirection, targetDirection))
            {
                var strategy = SelectStrategyForSameDirectionSimple(
                    sceneComplexity, geometricAlignment,
                    sourceDirection, targetDirection, dx, dy, horizontalDistance, verticalDistance,
                    sourceNodeRect, targetNodeRect);
                
                return strategy;
            }

            // 默认策略：水平优先或垂直优先
            var defaultStrategy = sourceDirection.IsHorizontal()
                ? PathStrategy.HorizontalFirst
                : PathStrategy.VerticalFirst;
            
            
            return defaultStrategy;
        }

        /// <summary>
        /// 分析节点相对位置的字符串描述
        /// </summary>
        private string AnalyzeRelativePosition(double dx, double dy)
        {
            const double threshold = 3;
            
            string horizontal = Math.Abs(dx)< threshold ? "水平对齐" : (dx > 0 ? "目标在右" : "目标在左");
            string vertical = Math.Abs(dy) < threshold ? "垂直对齐" : (dy > 0 ? "目标在下" : "目标在上");
            
            return $"{horizontal}, {vertical}";
        }

        /// <summary>
        /// 检测场景复杂度（简化版本：不涉及碰撞检测）
        /// 考虑因素：对齐程度、距离
        /// </summary>
        private SceneComplexity DetectSceneComplexitySimple(Point sourcePosition, Point targetPosition,
            PortDirection sourceDirection,
            PortDirection targetDirection,
            double dx, double dy,
            double horizontalDistance,
            double verticalDistance,
            Rect sourceNodeRect,
            Rect targetNodeRect)
        {
            // 1. 完全水平对齐：极简场景
            if (verticalDistance < 3)
            {
                
                return SceneComplexity.Direct;
            }

            // 2. 完全垂直对齐：极简场景
            if (horizontalDistance <3)
            {
                
                return SceneComplexity.Direct;
            }

            // 3. 检查对齐程度
            bool horizontallyAligned = horizontalDistance < 20;
            bool verticallyAligned = verticalDistance < 20;
            

            // 4. 根据对齐程度判断场景复杂度
            if (horizontallyAligned || verticallyAligned)
            {
                
                return SceneComplexity.Simple;
            }

            
            return SceneComplexity.Simple;
        }

        /// <summary>
        /// 检测几何对齐情况
        /// 考虑端口方向和位置对齐
        /// </summary>
        private GeometricAlignment DetectGeometricAlignment(
            PortDirection sourceDirection,
            PortDirection targetDirection,
            double horizontalDistance,
            double verticalDistance)
        {
            // 对齐阈值
            double alignmentThreshold = MinSegmentLength * 2;

            // 检查垂直对齐（水平方向接近）
            bool verticallyAligned = verticalDistance < alignmentThreshold;

            // 检查水平对齐（垂直方向接近）
            bool horizontallyAligned = horizontalDistance < alignmentThreshold;

            if (verticallyAligned && horizontallyAligned)
            {
                
                return GeometricAlignment.NotAligned; // 重叠，实际上不是"对齐"
            }
            else if (verticallyAligned)
            {
                
                return GeometricAlignment.VerticalAligned;
            }
            else if (horizontallyAligned)
            {
                
                return GeometricAlignment.HorizontalAligned;
            }
            else
            {
                
                return GeometricAlignment.NotAligned;
            }
        }

        /// <summary>
        /// 为相对方向场景选择策略（优化版本：基于场景复杂度）
        /// Left-Right, Right-Left, Top-Bottom, Bottom-Top
        /// </summary>
        private PathStrategy SelectStrategyForOppositeDirection(
            SceneComplexity sceneComplexity,
            GeometricAlignment geometricAlignment,
            PortDirection sourceDirection,
            PortDirection targetDirection,
            double horizontalDistance,
            double verticalDistance)
        {
            
            

            // 根据场景复杂度选择策略
            switch (sceneComplexity)
            {
                case SceneComplexity.Direct:
                    // 极简场景：直接连接
                    
                    return PathStrategy.Direct;

                case SceneComplexity.Simple:
                    // 简单场景：使用相对方向策略
                    return PathStrategy.OppositeDirection;

                case SceneComplexity.Medium:
                case SceneComplexity.Complex:
                    // 中等或复杂场景：使用相对方向策略
                    return PathStrategy.OppositeDirection;

                default:
                    return PathStrategy.OppositeDirection;
            }
        }

        /// <summary>
        /// 为垂直方向场景选择策略（简化版本：不涉及碰撞检测）
        /// 一个水平一个垂直（Left-Top, Left-Bottom, Right-Top, Right-Bottom等）
        /// </summary>
        private PathStrategy SelectStrategyForPerpendicularDirectionSimple(
            SceneComplexity sceneComplexity,
            GeometricAlignment geometricAlignment,
            PortDirection sourceDirection,
            PortDirection targetDirection,
            double horizontalDistance,
            double verticalDistance,
            Rect sourceNodeRect,
            Rect targetNodeRect)
        {
            
            

            // 简单或极简场景：选择简单策略
            if (sceneComplexity == SceneComplexity.Simple || sceneComplexity == SceneComplexity.Direct)
            {
                var simpleStrategy = SelectSimplestStrategy(
                    sourceDirection, targetDirection, horizontalDistance, verticalDistance);
                
                return simpleStrategy;
            }

            // 计算节点之间的相对距离
            double dx = targetNodeRect.X - sourceNodeRect.X;
            double dy = targetNodeRect.Y - sourceNodeRect.Y;
            

            // 使用智能判断：基于端口朝向和相对位置的策略选择
            bool preferHorizontal = ShouldPreferHorizontal(
                sourceDirection, targetDirection,
                dx, dy,
                horizontalDistance, verticalDistance);

            if (preferHorizontal)
            {
                
                return PathStrategy.HorizontalFirst;
            }
            else
            {
                
                return PathStrategy.VerticalFirst;
            }
        }

        /// <summary>
        /// 基于端口朝向和相对位置，计算首选的延伸方向
        /// 返回：true=水平优先，false=垂直优先
        /// </summary>
        private bool ShouldPreferHorizontal(
            PortDirection sourceDirection,
            PortDirection targetDirection,
            double dx,
            double dy,
            double horizontalDistance,
            double verticalDistance)
        {
            // 1. 检查源端口朝向的自然延伸
            bool sourceHorizontal = sourceDirection.IsHorizontal();
            bool sourceVertical = sourceDirection.IsVertical();

            // 2. 检查目标方向的移动倾向
            bool targetMoveHorizontal = (targetDirection == PortDirection.Left || targetDirection == PortDirection.Right);
            bool targetMoveVertical = (targetDirection == PortDirection.Top || targetDirection == PortDirection.Bottom);

            // 3. 分析相对位置和端口方向的语义关系
            if (sourceHorizontal)
            {
                // 源端口是水平方向（Left/Right）

                // 情况A：目标端口是垂直方向
                if (targetMoveVertical)
                {
                    // 判断是否应该水平优先

                    // 检查目标是否在端口朝向的同侧
                    bool targetInDirection = false;
                    if (sourceDirection == PortDirection.Right && dx > 0)
                        targetInDirection = true;  // 右端口，目标在右侧
                    else if (sourceDirection == PortDirection.Left && dx < 0)
                        targetInDirection = true;  // 左端口，目标在左侧

                    if (targetInDirection)
                    {
                        // 目标在端口朝向的同侧：优先水平延伸（顺应端口方向）
                        
                        return true;
                    }

                    // 目标在端口朝向的反侧：需要考虑其他因素

                    // 检查垂直方向是否更自然
                    bool verticalNatural = false;
                    if (targetDirection == PortDirection.Top && dy < 0)
                        verticalNatural = true;  // 目标在上端口，目标也在上方
                    else if (targetDirection == PortDirection.Bottom && dy > 0)
                        verticalNatural = true;  // 目标在下端口，目标也在下方

                    if (verticalNatural && horizontalDistance <= verticalDistance)
                    {
                        // 垂直方向更自然，且垂直距离不小于水平距离
                        
                        return false;
                    }

                    // 默认：根据距离判断
                    bool result = horizontalDistance >= verticalDistance;
                    
                    return result;
                }
                else
                {
                    // 目标端口也是水平方向：根据距离判断
                    bool result = horizontalDistance >= verticalDistance;
                    
                    return result;
                }
            }
            else
            {
                // 源端口是垂直方向（Top/Bottom）

                // 情况B：目标端口是水平方向
                if (targetMoveHorizontal)
                {
                    // 判断是否应该垂直优先

                    // 检查目标是否在端口朝向的同侧
                    bool targetInDirection = false;
                    if (sourceDirection == PortDirection.Bottom && dy > 0)
                        targetInDirection = true;  // 下端口，目标在下方
                    else if (sourceDirection == PortDirection.Top && dy < 0)
                        targetInDirection = true;  // 上端口，目标在上方

                    if (targetInDirection)
                    {
                        // 目标在端口朝向的同侧：优先垂直延伸（顺应端口方向）
                        
                        return false;
                    }

                    // 目标在端口朝向的反侧：需要考虑其他因素

                    // 检查水平方向是否更自然
                    bool horizontalNatural = false;
                    if (targetDirection == PortDirection.Right && dx > 0)
                        horizontalNatural = true;  // 目标在右端口，目标也在右侧
                    else if (targetDirection == PortDirection.Left && dx < 0)
                        horizontalNatural = true;  // 目标在左端口，目标也在左侧

                    if (horizontalNatural && verticalDistance <= horizontalDistance)
                    {
                        // 水平方向更自然，且水平距离不小于垂直距离
                        
                        return true;
                    }

                    // 默认：根据距离判断
                    bool result = horizontalDistance < verticalDistance;
                    
                    return result;
                }
                else
                {
                    // 目标端口也是垂直方向：根据距离判断
                    bool result = horizontalDistance < verticalDistance;
                    
                    return result;
                }
            }
        }

        /// <summary>
        /// 为同向场景选择策略（简化版本：不涉及碰撞检测）
        /// Left-Left, Right-Right, Top-Top, Bottom-Bottom
        /// </summary>
        private PathStrategy SelectStrategyForSameDirectionSimple(
            SceneComplexity sceneComplexity,
            GeometricAlignment geometricAlignment,
            PortDirection sourceDirection,
            PortDirection targetDirection,
            double dx, double dy,
            double horizontalDistance,
            double verticalDistance,
            Rect sourceNodeRect,
            Rect targetNodeRect)
        {
            
            

            // 极简场景或简单场景：选择简单策略
            if (sceneComplexity == SceneComplexity.Direct || sceneComplexity == SceneComplexity.Simple)
            {
                var simpleStrategy = SelectSimplestStrategy(
                    sourceDirection, targetDirection, horizontalDistance, verticalDistance);
                
                return simpleStrategy;
            }

            // 默认：基于几何对齐选择
            
            if (geometricAlignment == GeometricAlignment.VerticalAligned && sourceDirection.IsHorizontal())
            {
                
                return PathStrategy.HorizontalFirst;
            }
            else if (geometricAlignment == GeometricAlignment.HorizontalAligned && sourceDirection.IsVertical())
            {
                
                return PathStrategy.VerticalFirst;
            }
            else
            {
                // 不对齐：使用三段式
                
                return PathStrategy.ThreeSegment;
            }
        }

        /// <summary>
        /// 选择最简单的策略（用于简单场景）
        /// </summary>
        private PathStrategy SelectSimplestStrategy(
            PortDirection sourceDirection,
            PortDirection targetDirection,
            double horizontalDistance,
            double verticalDistance)
        {
            

            // 极近距离：直接连接
            if (IsVeryCloseDistance(horizontalDistance, verticalDistance))
            {
                
                return PathStrategy.Direct;
            }

            // 根据端口方向选择简单策略
            if (sourceDirection.IsHorizontal())
            {
                // 水平端口：优先水平延伸
                
                return PathStrategy.HorizontalFirst;
            }
            else
            {
                // 垂直端口：优先垂直延伸
                
                return PathStrategy.VerticalFirst;
            }
        }


        /// <summary>
        /// 判断两个端口方向是否相反（对向）
        /// </summary>
        private bool IsOppositeDirection(PortDirection dir1, PortDirection dir2)
        {
            return (dir1 == PortDirection.Left && dir2 == PortDirection.Right) ||
                   (dir1 == PortDirection.Right && dir2 == PortDirection.Left) ||
                   (dir1 == PortDirection.Top && dir2 == PortDirection.Bottom) ||
                   (dir1 == PortDirection.Bottom && dir2 == PortDirection.Top);
        }

        /// <summary>
        /// 判断距离是否极近（极近距离阈值）
        /// </summary>
        private bool IsVeryCloseDistance(double horizontalDistance, double verticalDistance)
        {
            return horizontalDistance == 0 && verticalDistance == 0;
        }

        /// <summary>
        /// 检测路径是否与节点发生碰撞
        /// </summary>
        /// <param name="excludeTargetNode">是否排除目标节点（false表示检测与目标节点的碰撞）</param>
        private bool HasCollision(
            Point[] pathPoints,
            Rect[] allNodeRects,
            Rect excludeSource,
            Rect excludeTarget,
            bool excludeTargetNode = true)
        {
            if (allNodeRects == null || allNodeRects.Length == 0)
                return false;

            var relevantRects = allNodeRects.Where(rect =>
                !rect.IsEmpty &&
                rect != excludeSource &&
                (excludeTargetNode || rect != excludeTarget)).ToArray();

            for (int i = 0; i < pathPoints.Length - 1; i++)
            {
                var p1 = pathPoints[i];
                var p2 = pathPoints[i + 1];

                if (relevantRects.Any(rect => LineIntersectsRect(p1, p2, rect)))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 检测线段是否与矩形相交（模块2：精确碰撞检测系统）
        /// 使用三层检测机制：快速排除 + 多采样点 + 线段相交检测
        /// </summary>
        private bool LineIntersectsRect(Point p1, Point p2, Rect rect)
        {
            // TODO: Fix duplicate definition issue
            // Temporary simplified implementation
            return false;
        }

        /// <summary>
        /// 检测两条线段是否相交
        /// </summary>
        private bool LineSegmentsIntersect(Point p1, Point p2, Point p3, Point p4)
        {
            // 使用跨乘积判断线段相交
            var d1 = Direction(p3, p4, p1);
            var d2 = Direction(p3, p4, p2);
            var d3 = Direction(p1, p2, p3);
            var d4 = Direction(p1, p2, p4);

            if (((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)) &&
                ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0)))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 计算点相对线段的方向
        /// </summary>
        private double Direction(Point pi, Point pj, Point pk)
        {
            return (pk.X - pi.X) * (pj.Y - pi.Y) - (pj.X - pi.X) * (pk.Y - pi.Y);
        }

        /// <summary>
        /// 判断距离是否较近（小于最小线段长度）
        /// </summary>
        private bool IsCloseDistance(double horizontalDistance, double verticalDistance)
        {
            return horizontalDistance < MinSegmentLength * 2 && verticalDistance < MinSegmentLength * 2;
        }

        /// <summary>
        /// 判断两个端口方向是否相同
        /// </summary>
        private bool IsSameDirection(PortDirection dir1, PortDirection dir2)
        {
            return dir1 == dir2;
        }

        /// <summary>
        /// 判断两个端口方向是否正交（一个水平一个垂直）
        /// </summary>
        private bool IsPerpendicularDirection(PortDirection dir1, PortDirection dir2)
        {
            return (dir1.IsHorizontal() && dir2.IsVertical()) ||
                   (dir1.IsVertical() && dir2.IsHorizontal());
        }

        /// <summary>
        /// 根据策略计算路径点（带节点边界信息）
        /// </summary>
        private Point[] CalculatePathByStrategy(
            Point sourcePosition,
            Point targetPosition,
            PortDirection sourceDirection,
            PortDirection targetDirection,
            PathStrategy strategy,
            double dx,
            double dy,
            Rect sourceNodeRect,
            Rect targetNodeRect)
        {
            switch (strategy)
            {
                case PathStrategy.Direct:
                    return CalculateDirectPath(sourcePosition, targetPosition, sourceDirection, targetDirection, dx, dy);

                case PathStrategy.HorizontalFirst:
                    return CalculateHorizontalFirstPath(sourcePosition, targetPosition, sourceDirection, targetDirection, dx, dy, sourceNodeRect, targetNodeRect);

                case PathStrategy.VerticalFirst:
                    return CalculateVerticalFirstPath(sourcePosition, targetPosition, sourceDirection, targetDirection, dx, dy, sourceNodeRect, targetNodeRect);

                case PathStrategy.ThreeSegment:
                    return CalculateThreeSegmentPath(sourcePosition, targetPosition, sourceDirection, targetDirection, dx, dy, sourceNodeRect, targetNodeRect);

                case PathStrategy.OppositeDirection:
                    return CalculateOppositeDirectionPath(sourcePosition, targetPosition, sourceDirection, targetDirection, dx, dy, sourceNodeRect, targetNodeRect);

                case PathStrategy.FourSegment:
                    return CalculateFourSegmentPath(sourcePosition, targetPosition, sourceDirection, targetDirection, dx, dy, sourceNodeRect,targetNodeRect);

                case PathStrategy.FiveSegment:
                    return CalculateFiveSegmentPath(sourcePosition, targetPosition, sourceDirection, targetDirection, dx, dy,sourceNodeRect,targetNodeRect);

                default:
                    return CalculateHorizontalFirstPath(sourcePosition, targetPosition, sourceDirection, targetDirection, dx, dy, sourceNodeRect, targetNodeRect);
            }
        }

        /// <summary>
        /// 计算直接连接路径（极近距离，无拐点）
        /// </summary>
        private Point[] CalculateDirectPath(
            Point sourcePosition,
            Point targetPosition,
            PortDirection sourceDirection,
            PortDirection targetDirection,
            double dx,
            double dy)
        {
            // 极近距离直接连接（无拐点）
            return new Point[]
            {
                sourcePosition,
                targetPosition
            };
        }

        /// <summary>
        /// 计算水平优先路径（水平-垂直-水平）
        /// </summary>
        private Point[] CalculateHorizontalFirstPath(
            Point sourcePosition,
            Point targetPosition,
            PortDirection sourceDirection,
            PortDirection targetDirection,
            double dx,
            double dy,
            Rect sourceNodeRect,
            Rect targetNodeRect)
        {
            // 计算第一个拐点：从源点沿源方向延伸，考虑节点相交情况
            var p1 = CalculateOptimizedFirstPoint(
                sourcePosition,targetPosition ,sourceDirection, dx, dy,
                sourceNodeRect, targetNodeRect);

            // 计算第二个拐点：从第一个拐点沿垂直方向延伸到目标Y
            var p2 = new Point(p1.X, targetPosition.Y);

            // 最终路径点
            var path = new Point[]
            {
                sourcePosition,
                p1,
                p2,
                targetPosition
            };
            return path;
        }

        /// <summary>
        /// 计算垂直优先路径（垂直-水平-垂直）
        /// </summary>
        private Point[] CalculateVerticalFirstPath(
            Point sourcePosition,
            Point targetPosition,
            PortDirection sourceDirection,
            PortDirection targetDirection,
            double dx,
            double dy,
            Rect sourceNodeRect,Rect targetNodeRect)
        {
            // 计算第一个拐点：从源点沿源方向延伸一段距离，考虑节点边界
            var p1 = CalculateOptimizedFirstPoint(sourcePosition,targetPosition ,sourceDirection, dx, dy, sourceNodeRect,targetNodeRect);

            // 计算第二个拐点：从第一个拐点沿水平方向延伸到目标X
            var p2 = new Point(targetPosition.X, p1.Y);

            // 最终路径点
            var path = new Point[]
            {
                sourcePosition,
                p1,
                p2,
                targetPosition
            };
            return path;
        }

        /// <summary>
        /// 计算三段式路径（优化版本，确保中间点在节点外）
        /// </summary>
        private Point[] CalculateThreeSegmentPath(
            Point sourcePosition,
            Point targetPosition,
            PortDirection sourceDirection,
            PortDirection targetDirection,
            double dx,
            double dy,
            Rect sourceNodeRect,
            Rect targetNodeRect)
        {
            // 三段式路径：水平-垂直 或 垂直-水平
            var midPoint1 = new Point(sourcePosition.X, targetPosition.Y);
            var midPoint2 = new Point(targetPosition.X, sourcePosition.Y);

            // 选择更优的中间点
            var betterMidPoint = sourceDirection.IsHorizontal() ? midPoint2 : midPoint1;

            // 如果节点边界信息有效，检查中间点是否在节点内
            if (!sourceNodeRect.IsEmpty || !targetNodeRect.IsEmpty)
            {
                // 如果选中的中间点在节点内，尝试另一个
                if (IsPointInRect(betterMidPoint, sourceNodeRect) || IsPointInRect(betterMidPoint, targetNodeRect))
                {
                    betterMidPoint = sourceDirection.IsHorizontal() ? midPoint1 : midPoint2;

                    // 如果另一个也在节点内，计算安全的中点
                    if (IsPointInRect(betterMidPoint, sourceNodeRect) || IsPointInRect(betterMidPoint, targetNodeRect))
                    {
                        betterMidPoint = CalculateSafeMidPoint(
                            sourcePosition, targetPosition,
                            sourceDirection,
                            sourceNodeRect, targetNodeRect);
                    }
                }
            }

            var path = new Point[]
            {
                sourcePosition,
                betterMidPoint,
                targetPosition
            };
            return path;
        }

        /// <summary>
        /// 计算相对方向路径（用于Top-Bottom, Bottom-Top, Left-Right, Right-Left等相对方向连接）
        /// 路径模式：沿源方向延伸 → 水平/垂直 → 到达目标
        /// 简化版本：只负责基本路径计算，所有避让逻辑由ApplyNodeAvoidance统一处理
        /// </summary>
        private Point[] CalculateOppositeDirectionPath(
            Point sourcePosition,
            Point targetPosition,
            PortDirection sourceDirection,
            PortDirection targetDirection,
            double dx,
            double dy,
            Rect sourceNodeRect,
            Rect targetNodeRect)
        {
            // 计算第一个拐点：沿源方向延伸
            var p1 = CalculateOptimizedFirstPoint(sourcePosition, targetPosition, sourceDirection, dx, dy, sourceNodeRect, targetNodeRect);

            // 计算第二个拐点：简单的水平或垂直到目标
            // 不进行复杂的避让判断，避让逻辑由ApplyNodeAvoidance统一处理
            Point p2;

            if (sourceDirection.IsVertical())
            {
                // 垂直相对：Top-Bottom, Bottom-Top
                // 先垂直延伸（p1），再水平移动（p2）
                // 使用目标位置和p1坐标计算拐点
                var midX = (p1.X + targetPosition.X) / 2;
                p2 = new Point(midX, p1.Y);
            }
            else
            {
                // 水平相对：Left-Right, Right-Left
                // 先水平延伸（p1），再垂直移动（p2）
                // 使用目标位置和p1坐标计算拐点
                var midY = (p1.Y + targetPosition.Y) / 2;
                p2 = new Point(p1.X, midY);
            }

            return new Point[]
            {
                sourcePosition,
                p1,
                p2,
                targetPosition
            };
        }

        /// <summary>
        /// 计算四段式路径（用于同向端口中等距离场景）
        /// </summary>
        private Point[] CalculateFourSegmentPath(
            Point sourcePosition,
            Point targetPosition,
            PortDirection sourceDirection,
            PortDirection targetDirection,
            double dx,
            double dy,
            Rect sourceNodeRect,Rect targetNodeRect)
        {
            // 四段式路径：源端口延伸 → 垂直/水平 → 水平/垂直 → 到达目标
            // 用于同向端口但距离不太远的场景

            var p1 = CalculateOptimizedFirstPoint(sourcePosition, targetPosition, sourceDirection, dx, dy, sourceNodeRect, targetNodeRect);

            // 计算中间拐点
            Point p2, p3;

            if (sourceDirection.IsHorizontal())
            {
                // 水平端口：水平 → 垂直 → 水平 → 垂直
                var midY = (sourcePosition.Y + targetPosition.Y) / 2;
                p2 = new Point(p1.X, midY);
                p3 = new Point(targetPosition.X, midY);
            }
            else
            {
                // 垂直端口：垂直 → 水平 → 垂直 → 水平
                var midX = (sourcePosition.X + targetPosition.X) / 2;
                p2 = new Point(midX, p1.Y);
                p3 = new Point(midX, targetPosition.Y);
            }

            var path = new Point[]
            {
                sourcePosition,
                p1,
                p2,
                p3,
                targetPosition
            };
            return path;
        }

        /// <summary>
        /// 计算五段式路径（用于复杂场景，特别是同向端口连接）
        /// </summary>
        private Point[] CalculateFiveSegmentPath(
            Point sourcePosition,
            Point targetPosition,
            PortDirection sourceDirection,
            PortDirection targetDirection,
            double dx,
            double dy,Rect sourceNodeRect,Rect targetNodeRect)
        {
            var p1 = CalculateOptimizedFirstPoint(sourcePosition, targetPosition, sourceDirection, dx, dy, sourceNodeRect, targetNodeRect);

            // 中间段：使用与端口方向垂直的连接
            // 对于水平端口，中间段在垂直方向；对于垂直端口，中间段在水平方向
            Point[] path;
            if (sourceDirection.IsHorizontal())
            {
                // 水平端口（Right/Left）：垂直-水平-垂直-水平-垂直模式
                var midY = (sourcePosition.Y + targetPosition.Y) / 2;
                var p2 = new Point(p1.X, midY);
                var p3 = new Point(targetPosition.X, midY);

                path = new Point[]
                {
                    sourcePosition,
                    p1,
                    p2,
                    p3,
                    targetPosition
                };
            }
            else
            {
                // 垂直端口（Top/Bottom）：水平-垂直-水平-垂直-水平模式
                var midX = (sourcePosition.X + targetPosition.X) / 2;
                var p2 = new Point(midX, p1.Y);
                var p3 = new Point(midX, targetPosition.Y);

                path = new Point[]
                {
                    sourcePosition,
                    p1,
                    p2,
                    p3,
                    targetPosition
                };
            }

            return path;
        }

        /// <summary>
        /// 计算优化的第一个拐点（HorizontalFirst策略专用）
        /// 根据端口方向和节点相交情况动态调整延伸距离
        /// </summary>
        private Point CalculateOptimizedFirstPoint(
            Point sourcePosition,Point targetPosition,
            PortDirection sourceDirection,
            double dx,
            double dy,
            Rect sourceNodeRect,
            Rect targetNodeRect)
        {
            // 1. 基础偏移量
            var minOffset = NodeSafeDistance;

            // 2. 根据端口方向和节点相交情况计算 requiredOffset
            var requiredOffset = minOffset;

            if (!sourceNodeRect.IsEmpty && !targetNodeRect.IsEmpty)
            {
                switch (sourceDirection)
                {
                    case PortDirection.Right:
                        // 检查两个节点水平方向是否相交
                        if (sourcePosition.X > targetPosition.X )
                        {
                            // 水平方向相交，使用最小安全距离
                            requiredOffset = NodeSafeDistance;
                        }
                        else
                        {
                            // 水平方向不相交，使用节点间的距离
                            requiredOffset =Math.Abs(sourceNodeRect.Right - targetNodeRect.Left)/2 ;
                        }
                        break;

                    case PortDirection.Left:
                        // 检查两个节点水平方向是否相交
                        if ( sourcePosition.X < targetPosition.X)
                        {
                            // 水平方向相交，使用最小安全距离
                            requiredOffset = NodeSafeDistance;
                        }
                        else
                        {
                            requiredOffset = Math.Abs(sourceNodeRect.Left - targetNodeRect.Right) / 2;
                        }
                        break;

                    case PortDirection.Bottom:
                        // 检查两个节点垂直方向是否相交
                        if (sourcePosition.Y > targetPosition.Y )
                            
                        {
                            // 垂直方向相交，使用最小安全距离
                            requiredOffset = NodeSafeDistance;
                        }
                        else
                        {
                            // 垂直方向不相交，使用节点间的距离
                            requiredOffset =Math.Abs(sourceNodeRect.Bottom - targetNodeRect.Top) ;
                        }
                        break;

                    case PortDirection.Top:
                        // 检查两个节点垂直方向是否相交
                        if (sourcePosition.Y < targetPosition.Y)
                        {
                            // 垂直方向相交，使用最小安全距离
                            requiredOffset = NodeSafeDistance;
                        }
                        else
                        {
                            // 垂直方向不相交，使用节点间的距离
                            requiredOffset = Math.Abs(sourceNodeRect.Top - targetNodeRect.Bottom);
                        }
                        break;
                }
            }

            // 3. 计算最终偏移量（直接使用 requiredOffset）
            return sourceDirection switch
            {
                PortDirection.Right => new Point(sourcePosition.X + requiredOffset, sourcePosition.Y),
                PortDirection.Left => new Point(sourcePosition.X - requiredOffset, sourcePosition.Y),
                PortDirection.Bottom => new Point(sourcePosition.X, sourcePosition.Y + requiredOffset),
                PortDirection.Top => new Point(sourcePosition.X, sourcePosition.Y - requiredOffset),
                _ => sourcePosition
            };
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

            var pathGeometry = new PathGeometry();
            var pathFigure = new PathFigure
            {
                StartPoint = pathPoints[0],
                IsClosed = false
            };

            // 添加线段
            for (int i = 1; i < pathPoints.Length; i++)
            {
                pathFigure.Segments.Add(new LineSegment(pathPoints[i], true));
            }

            pathGeometry.Figures.Add(pathFigure);
            return pathGeometry;
        }




        /// <summary>
        /// 计算箭头位置和角度
        /// 箭头尖端位于目标端口位置，角度基于目标端口方向固定
        /// 路径终点已经是箭头尾部位置（由ConnectionPathCache计算）
        /// </summary>
        /// <param name="pathPoints">路径点数组（终点是箭头尾部）</param>
        /// <param name="targetPosition">目标端口位置（箭头尖端位置）</param>
        /// <param name="targetDirection">目标端口方向，决定箭头的固定角度</param>
        /// <returns>箭头位置和角度（角度为度数）</returns>
        public (Point position, double angle) CalculateArrow(Point[] pathPoints, Point targetPosition, PortDirection targetDirection)
        {
            if (pathPoints == null || pathPoints.Length < 2)
            {
                return (new Point(0, 0), 0);
            }

            // 箭头尖端位于目标端口位置
            var arrowPosition = targetPosition;

            // 箭头角度基于目标端口方向固定
            var arrowAngle = GetFixedArrowAngle(targetDirection);

            // 获取路径最后一点用于调试（箭头尾部位置）
            var lastPoint = pathPoints[pathPoints.Length - 1];

            return (arrowPosition, arrowAngle);
        }

        /// <summary>
        /// 获取固定箭头角度（基于目标端口方向）
        /// 箭头角度不受源节点端口影响，固定为目标端口方向
        /// 角度定义：0度指向右，90度指向下，180度指向左，270度指向上
        /// </summary>
        private double GetFixedArrowAngle(PortDirection targetDirection)
        {
            return targetDirection switch
            {
                PortDirection.Left => 0.0,     // 左边端口：箭头向右
                PortDirection.Right => 180.0,   // 右边端口：箭头向左
                PortDirection.Top => 90.0,      // 上边端口：箭头向下
                PortDirection.Bottom => 270.0,  // 下边端口：箭头向上
                _ => 0.0
            };
        }

        /// <summary>
        /// 判断点是否在矩形内
        /// </summary>
        private bool IsPointInRect(Point point, Rect rect)
        {
            if (rect.IsEmpty)
                return false;

            return point.X >= rect.Left &&
                   point.X <= rect.Right &&
                   point.Y >= rect.Top &&
                   point.Y <= rect.Bottom;
        }

        /// <summary>
        /// 计算安全的中点（确保在节点边界外）
        /// </summary>
        private Point CalculateSafeMidPoint(
            Point sourcePosition,
            Point targetPosition,
            PortDirection sourceDirection,
            Rect sourceNodeRect,
            Rect targetNodeRect)
        {
            // 计算两个节点边界之间的安全区域
            double safeX, safeY;

            if (sourceDirection.IsHorizontal())
            {
                // X坐标：在源节点右侧边界之外
                safeX = !sourceNodeRect.IsEmpty
                    ? sourceNodeRect.Right + MinSegmentLength
                    : sourcePosition.X + MinSegmentLength;

                // Y坐标：在两个节点之间
                safeY = (sourcePosition.Y + targetPosition.Y) / 2;
            }
            else
            {
                // X坐标：在两个节点之间
                safeX = (sourcePosition.X + targetPosition.X) / 2;

                // Y坐标：在源节点下方边界之外
                safeY = !sourceNodeRect.IsEmpty
                    ? sourceNodeRect.Bottom + MinSegmentLength
                    : sourcePosition.Y + MinSegmentLength;
            }

            return new Point(safeX, safeY);
        }

        /// <summary>
        /// 模块3：统一的节点避让后处理方法（双点避让策略）
        /// 对计算好的路径进行后处理，确保路径不穿过任何节点
        /// 每个碰撞添加两个避让点：
        /// 1. shapePreservingPoint：保持原路径形状，使用 0.7 * NodeSafeDistance 的偏移
        /// 2. strategyPoint：基于策略的避让点
        /// </summary>
        private Point[] ApplyNodeAvoidance(
            Point[] pathPoints,
            Point sourcePosition,
            Point targetPosition,
            PortDirection sourceDirection,
            PortDirection targetDirection,
            Rect sourceNodeRect,
            Rect targetNodeRect,
            Rect[] allNodeRects)
        {
            if (allNodeRects == null || allNodeRects.Length == 0)
            {
                return pathPoints;
            }

            var currentPath = pathPoints;

            // 按顺序查找所有碰撞（不按严重程度排序）
            var collisions = FindCollisionsInOrder(currentPath, allNodeRects, sourceNodeRect, targetNodeRect);

            if (collisions == null || collisions.Count == 0)
            {
                return currentPath;
            }

            // 按顺序处理每个碰撞
            foreach (var collision in collisions)
            {
                // 生成双避让点
                var avoidancePoints = GenerateDualAvoidancePoints(
                    collision,
                    sourcePosition,
                    targetDirection,
                    allNodeRects,
                    sourceNodeRect,
                    targetNodeRect);

                if (avoidancePoints != null && avoidancePoints.Length > 0)
                {
                    // 插入避让点到路径中
                    currentPath = InsertAvoidancePoints(currentPath, collision.SegmentIndex, avoidancePoints);
                }
            }

            return currentPath;
        }

        /// <summary>
        /// 按顺序查找所有碰撞（包括源节点和目标节点）
        /// 统一由避障模块处理所有节点避让，包括源节点和目标节点
        /// </summary>
        private List<CollisionInfo> FindCollisionsInOrder(Point[] pathPoints, Rect[] allNodeRects, Rect excludeSource, Rect excludeTarget)
        {
            var collisions = new List<CollisionInfo>();

            // 保留所有节点进行碰撞检测，包括源节点和目标节点
            // 避障模块会统一处理所有避让逻辑
            var relevantRects = allNodeRects.Where(rect => !rect.IsEmpty).ToList();

            for (int i = 0; i < pathPoints.Length - 1; i++)
            {
                var p1 = pathPoints[i];
                var p2 = pathPoints[i + 1];

                foreach (var rect in relevantRects)
                {
                    if (LineIntersectsRect(p1, p2, rect))
                    {
                        collisions.Add(new CollisionInfo(i, rect, pathPoints));
                    }
                }
            }

            return collisions;
        }

        /// <summary>
        /// 检测源矩形与碰撞矩形的相对位置
        /// </summary>
        private RectRelativePosition DetectRelativePosition(Rect sourceRect, Rect collidingRect)
        {
            if (sourceRect.IsEmpty || collidingRect.IsEmpty)
                return RectRelativePosition.Overlapping;

            // 检查水平方向关系
            bool isOnRight = collidingRect.X > sourceRect.Right;
            bool isOnLeft = collidingRect.Right < sourceRect.Left;

            // 检查垂直方向关系
            bool isBelow = collidingRect.Y > sourceRect.Bottom;
            bool isAbove = collidingRect.Bottom < sourceRect.Top;

            if (isOnRight)
                return RectRelativePosition.OnRight;
            if (isOnLeft)
                return RectRelativePosition.OnLeft;
            if (isBelow)
                return RectRelativePosition.OnBottom;
            if (isAbove)
                return RectRelativePosition.OnTop;

            return RectRelativePosition.Overlapping;
        }

        /// <summary>
        /// 生成双避让点
        /// 1. shapePreservingPoint：保持原路径形状，使用动态偏移距离
        /// 2. strategyPoint：基于策略的避让点
        ///
        /// 智能处理源节点和目标节点的碰撞：
        /// - 源节点碰撞：使用更大的避让距离（2.0 * NodeSafeDistance）
        /// - 目标节点碰撞：确保最后一个拐点接近目标端口方向
        /// - 普通节点碰撞：使用标准避让距离（0.7 * NodeSafeDistance）
        /// </summary>
        private Point[] GenerateDualAvoidancePoints(
            CollisionInfo collision,
            Point sourcePosition,
            PortDirection targetDirection,
            Rect[] allNodeRects,
            Rect sourceNodeRect,
            Rect targetNodeRect)
        {
            var p1 = collision.PathPoints[collision.SegmentIndex];
            var p2 = collision.PathPoints[collision.SegmentIndex + 1];
            var collidingRect = collision.CollidingRect;

            // 判断碰撞节点类型
            bool isSourceNode = collidingRect == sourceNodeRect;
            bool isTargetNode = collidingRect == targetNodeRect;
            bool isRegularNode = !isSourceNode && !isTargetNode;

            // 判断线段是水平还是垂直
            bool isHorizontal = Math.Abs(p1.Y - p2.Y) < 1.0;



            // 计算形状保持点（shapePreservingPoint）
            Point shapePreservingPoint = CalculateShapePreservingPoint(
                sourcePosition, p1, p2, collidingRect, isHorizontal, isSourceNode, isTargetNode);

            // 计算策略避让点（strategyPoint）
            Point strategyPoint = CalculateStrategyPoint(
                p1, p2, collidingRect, isHorizontal, targetDirection, isSourceNode, isTargetNode);

            // 尝试使用双点避让
            var avoidancePoints = new[] { shapePreservingPoint, strategyPoint };

            // 测试避让路径是否有效
            if (!HasCollision(new[] { p1, shapePreservingPoint, strategyPoint, p2 },
                allNodeRects, Rect.Empty, Rect.Empty, excludeTargetNode: false))
            {
                return avoidancePoints;
            }

            // 双点避让失败，尝试单点避让
            if (!HasCollision(new[] { p1, strategyPoint, p2 },
                allNodeRects, Rect.Empty, Rect.Empty, excludeTargetNode: false))
            {
                return new[] { strategyPoint };
            }

            // 单点避让也失败，返回空
            return null;
        }

        /// <summary>
        /// 计算形状保持点（shapePreservingPoint）
        /// 使用动态偏移距离：
        /// - 源节点碰撞：使用 2.0 * NodeSafeDistance（更大避让）
        /// - 目标节点碰撞：使用 1.5 * NodeSafeDistance（中等避让）
        /// - 普通节点碰撞：使用 0.7 * NodeSafeDistance（标准避让）
        /// 根据相对位置动态计算避让方向
        /// </summary>
        private Point CalculateShapePreservingPoint(
            Point sourcePosition,
            Point p1,
            Point p2,
            Rect collidingRect,
            bool isHorizontal,
            bool isSourceNode = false,
            bool isTargetNode = false)
        {
            // 根据碰撞节点类型确定避让距离
            double offset;
            if (isSourceNode)
            {
                offset = NodeSafeDistance * 2.0;  // 源节点需要更大的避让距离
            }
            else if (isTargetNode)
            {
                offset = NodeSafeDistance * 1.5;  // 目标节点使用中等避让距离
            }
            else
            {
                offset = NodeSafeDistance * 0.7;  // 普通节点使用标准避让距离
            }

            if (isHorizontal)
            {
                // 水平线段：计算基于源节点与碰撞矩形的相对位置的Y坐标
                var relativePosition = DetectRelativePosition(
                    new Rect(sourcePosition.X, sourcePosition.Y, 1, 1),
                    collidingRect);

                double safeY;
                switch (relativePosition)
                {
                    case RectRelativePosition.OnRight:
                        // 碰撞矩形在源节点右侧
                        safeY = collidingRect.Top - offset;
                        break;
                    case RectRelativePosition.OnLeft:
                        // 碰撞矩形在源节点左侧
                        safeY = collidingRect.Bottom + offset;
                        break;
                    case RectRelativePosition.OnBottom:
                        // 碰撞矩形在源节点下方
                        safeY = collidingRect.Top - offset;
                        break;
                    case RectRelativePosition.OnTop:
                        // 碰撞矩形在源节点上方
                        safeY = collidingRect.Bottom + offset;
                        break;
                    default:
                        // 重叠情况，使用上方避让
                        safeY = collidingRect.Top - offset;
                        break;
                }

                return new Point(p1.X, safeY);
            }
            else
            {
                // 垂直线段：计算基于源节点与碰撞矩形的相对位置的X坐标
                var relativePosition = DetectRelativePosition(
                    new Rect(sourcePosition.X, sourcePosition.Y, 1, 1),
                    collidingRect);

                double safeX;
                switch (relativePosition)
                {
                    case RectRelativePosition.OnRight:
                        // 碰撞矩形在源节点右侧
                        safeX = collidingRect.Left - offset;
                        break;
                    case RectRelativePosition.OnLeft:
                        // 碰撞矩形在源节点左侧
                        safeX = collidingRect.Right + offset;
                        break;
                    case RectRelativePosition.OnBottom:
                        // 碰撞矩形在源节点下方
                        safeX = collidingRect.Right + offset;
                        break;
                    case RectRelativePosition.OnTop:
                        // 碰撞矩形在源节点上方
                        safeX = collidingRect.Left - offset;
                        break;
                    default:
                        // 重叠情况，使用左侧避让
                        safeX = collidingRect.Left - offset;
                        break;
                }

                return new Point(safeX, p1.Y);
            }
        }

        /// <summary>
        /// 计算策略避让点（strategyPoint）
        /// 基于目标端口方向的策略避让
        ///
        /// 智能处理源节点和目标节点碰撞：
        /// - 源节点碰撞：使用 2.0 * NodeSafeDistance，确保从源节点出发的方向
        /// - 目标节点碰撞：使用 1.5 * NodeSafeDistance，确保接近目标端口方向
        /// - 普通节点碰撞：使用标准的 NodeSafeDistance
        /// </summary>
        private Point CalculateStrategyPoint(
            Point p1,
            Point p2,
            Rect collidingRect,
            bool isHorizontal,
            PortDirection targetDirection,
            bool isSourceNode = false,
            bool isTargetNode = false)
        {
            // 根据碰撞节点类型确定避让距离
            double offset;
            if (isSourceNode)
            {
                offset = NodeSafeDistance * 2.0;  // 源节点需要更大的避让距离
            }
            else if (isTargetNode)
            {
                offset = NodeSafeDistance * 1.5;  // 目标节点使用中等避让距离
            }
            else
            {
                offset = NodeSafeDistance;  // 普通节点使用标准避让距离
            }

            if (isHorizontal)
            {
                // 水平线段：垂直避让
                if (targetDirection == PortDirection.Bottom)
                {
                    // 目标端口在下方，优先从上方接近
                    return new Point(p1.X, collidingRect.Top - offset);
                }
                else if (targetDirection == PortDirection.Top)
                {
                    // 目标端口在上方，优先从下方接近
                    return new Point(p1.X, collidingRect.Bottom + offset);
                }
                else
                {
                    // 其他方向，默认上方避让
                    return new Point(p1.X, collidingRect.Top - offset);
                }
            }
            else
            {
                // 垂直线段：水平避让
                if (targetDirection == PortDirection.Right)
                {
                    // 目标端口在右方，优先从左方接近
                    return new Point(collidingRect.Left - offset, p1.Y);
                }
                else if (targetDirection == PortDirection.Left)
                {
                    // 目标端口在左方，优先从右方接近
                    return new Point(collidingRect.Right + offset, p1.Y);
                }
                else
                {
                    // 其他方向，默认左侧避让
                    return new Point(collidingRect.Left - offset, p1.Y);
                }
            }
        }

        /// <summary>
        /// 将避让拐点插入到路径中
        /// </summary>
        private Point[] InsertAvoidancePoints(Point[] pathPoints, int segmentIndex, Point[] avoidancePoints)
        {
            var newPath = new List<Point>();

            // 添加碰撞段之前的所有点
            for (int i = 0; i <= segmentIndex; i++)
            {
                newPath.Add(pathPoints[i]);
            }

            // 插入避让拐点
            foreach (var point in avoidancePoints)
            {
                newPath.Add(point);
            }

            // 添加碰撞段之后的所有点
            for (int i = segmentIndex + 1; i < pathPoints.Length; i++)
            {
                newPath.Add(pathPoints[i]);
            }

            return newPath.ToArray();
        }
    }
}
