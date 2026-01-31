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
            System.Diagnostics.Debug.WriteLine($"[OrthogonalPath] ========== 开始路径计算 ==========");
            System.Diagnostics.Debug.WriteLine($"[OrthogonalPath] 源位置:({sourcePosition.X:F1},{sourcePosition.Y:F1}), 目标位置（箭头尾部）:({targetPosition.X:F1},{targetPosition.Y:F1})");
            System.Diagnostics.Debug.WriteLine($"[OrthogonalPath] 源方向:{sourceDirection}, 目标方向:{targetDirection}");
            System.Diagnostics.Debug.WriteLine($"[OrthogonalPath] 源节点边界:{(sourceNodeRect.IsEmpty ? "无" : $"({sourceNodeRect.X:F1},{sourceNodeRect.Y:F1},{sourceNodeRect.Width:F1}x{sourceNodeRect.Height:F1})")}");
            System.Diagnostics.Debug.WriteLine($"[OrthogonalPath] 目标节点边界:{(targetNodeRect.IsEmpty ? "无" : $"({targetNodeRect.X:F1},{targetNodeRect.Y:F1},{targetNodeRect.Width:F1}x{targetNodeRect.Height:F1})")}");
            System.Diagnostics.Debug.WriteLine($"[OrthogonalPath] 碰撞检测节点数:{(allNodeRects?.Length ?? 0)}");

            // 直接计算基本路径（目标位置已经是箭头尾部）
            var pathPoints = CalculateBasicPath(
                sourcePosition,
                targetPosition,
                sourceDirection,
                targetDirection,
                sourceNodeRect,
                targetNodeRect,
                allNodeRects);

            System.Diagnostics.Debug.WriteLine($"[OrthogonalPath] 路径计算完成: 路径点数={pathPoints.Length}");
            for (int i = 0; i < pathPoints.Length; i++)
            {
                System.Diagnostics.Debug.WriteLine($"[OrthogonalPath]   路径点[{i}]:({pathPoints[i].X:F1},{pathPoints[i].Y:F1})");
            }
            System.Diagnostics.Debug.WriteLine($"[OrthogonalPath] ========== 路径计算完成 ==========");

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

            // 2. 选择最佳路径策略（考虑碰撞检测）
            var strategy = SelectPathStrategy(sourcePosition,targetPosition,
                sourceDirection,
                targetDirection,
                dx,
                dy,
                horizontalDistance,
                verticalDistance,
                sourceNodeRect,
                targetNodeRect,
                allNodeRects);

            // 3. 根据策略计算路径点（目标位置是箭头尾部）
            System.Diagnostics.Debug.WriteLine($"[OrthogonalPath] 最终选择的策略: {strategy}");
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
        /// 选择最佳路径策略（优化版本：基于场景复杂度而非距离）
        /// 分层优先级：Priority 1(极简场景) > Priority 2(相对方向) > Priority 3(垂直方向) > Priority 4(同向)
        /// </summary>
        private PathStrategy SelectPathStrategy(Point sourcePosition, Point targetPosition,
            PortDirection sourceDirection,
            PortDirection targetDirection,
            double dx, double dy,
            double horizontalDistance,
            double verticalDistance,
            Rect sourceNodeRect,
            Rect targetNodeRect,
            Rect[] allNodeRects)
        {
            System.Diagnostics.Debug.WriteLine($"[OrthogonalPath] ========== 开始优化分层决策 ==========");
            System.Diagnostics.Debug.WriteLine($"[OrthogonalPath] 源方向:{sourceDirection}, 目标方向:{targetDirection}");
            System.Diagnostics.Debug.WriteLine($"[OrthogonalPath] 水平距离:{horizontalDistance:F0}, 垂直距离:{verticalDistance:F0}");

            // 分析端口方向关系
            bool isOpposite = IsOppositeDirection(sourceDirection, targetDirection);
            bool isSame = IsSameDirection(sourceDirection, targetDirection);
            bool isPerpendicular = IsPerpendicularDirection(sourceDirection, targetDirection);
            System.Diagnostics.Debug.WriteLine($"[OrthogonalPath] 端口方向关系: {(isOpposite ? "相对(Opposite)" : isSame ? "同向(Same)" : isPerpendicular ? "垂直(Perpendicular)" : "未知")}");

            // 分析节点相对位置
            if (!sourceNodeRect.IsEmpty && !targetNodeRect.IsEmpty)
            {
                double relativeDx = targetNodeRect.X - sourceNodeRect.X;
                double relativeDy = targetNodeRect.Y - sourceNodeRect.Y;
                string relativePos = AnalyzeRelativePosition(relativeDx, relativeDy);
                System.Diagnostics.Debug.WriteLine($"[OrthogonalPath] 节点相对位置: {relativePos} (dx={relativeDx:F0}, dy={relativeDy:F0})");
            }

            // 检测场景复杂度
            var sceneComplexity = DetectSceneComplexity(sourcePosition, targetPosition,
                sourceDirection, targetDirection, dx, dy, horizontalDistance, verticalDistance,
                sourceNodeRect, targetNodeRect, allNodeRects);
            System.Diagnostics.Debug.WriteLine($"[OrthogonalPath] 场景复杂度: {sceneComplexity}");

            // 检测几何对齐情况
            var geometricAlignment = DetectGeometricAlignment(
                sourceDirection, targetDirection, horizontalDistance, verticalDistance);
            System.Diagnostics.Debug.WriteLine($"[OrthogonalPath] 几何对齐: {geometricAlignment}");

            // Priority 1: 极简场景（直接对齐，无障碍，距离极近）
            if (sceneComplexity == SceneComplexity.Direct)
            {
                System.Diagnostics.Debug.WriteLine($"[OrthogonalPath] ========== Priority 1 命中（极简场景） ==========");
                return PathStrategy.Direct;
            }

            // Priority 2: 相对方向（Left-Right, Right-Left, Top-Bottom, Bottom-Top）
            if (IsOppositeDirection(sourceDirection, targetDirection))
            {
                var strategy = SelectStrategyForOppositeDirection(
                    sceneComplexity, geometricAlignment,
                    sourceDirection, targetDirection, horizontalDistance, verticalDistance);
                System.Diagnostics.Debug.WriteLine($"[OrthogonalPath] ========== Priority 2 命中（相对方向） ==========");
                return strategy;
            }

            // Priority 3: 垂直方向（一个水平一个垂直）
            if (IsPerpendicularDirection(sourceDirection, targetDirection))
            {
                var strategy = SelectStrategyForPerpendicularDirection(
                    sceneComplexity, geometricAlignment,
                    sourceDirection, targetDirection, horizontalDistance, verticalDistance,
                    sourceNodeRect, targetNodeRect, allNodeRects);
                System.Diagnostics.Debug.WriteLine($"[OrthogonalPath] ========== Priority 3 命中（垂直方向） ==========");
                return strategy;
            }

            // Priority 4: 同向（Left-Left, Right-Right, Top-Top, Bottom-Bottom）
            if (IsSameDirection(sourceDirection, targetDirection))
            {
                var strategy = SelectStrategyForSameDirection(
                    sceneComplexity, geometricAlignment,
                    sourceDirection, targetDirection, dx, dy, horizontalDistance, verticalDistance,
                    sourceNodeRect, targetNodeRect, allNodeRects);
                System.Diagnostics.Debug.WriteLine($"[OrthogonalPath] ========== Priority 4 命中（同向） ==========");
                return strategy;
            }

            // 默认策略：水平优先或垂直优先
            var defaultStrategy = sourceDirection.IsHorizontal()
                ? PathStrategy.HorizontalFirst
                : PathStrategy.VerticalFirst;
            System.Diagnostics.Debug.WriteLine($"[OrthogonalPath] ========== 默认策略 ==========");
            System.Diagnostics.Debug.WriteLine($"[OrthogonalPath] 选择策略: {defaultStrategy}");
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
        /// 检测场景复杂度
        /// 考虑因素：障碍数量、碰撞场景、对齐程度
        /// </summary>
        private SceneComplexity DetectSceneComplexity(Point sourcePosition, Point targetPosition,
            PortDirection sourceDirection,
            PortDirection targetDirection,
            double dx, double dy,
            double horizontalDistance,
            double verticalDistance,
            Rect sourceNodeRect,
            Rect targetNodeRect,
            Rect[] allNodeRects)
        {
            // 1. 完全水平对齐：极简场景
            if (verticalDistance < 3)
            {
                System.Diagnostics.Debug.WriteLine($"[DetectComplexity] 完全垂直对齐 -> Direct (vDist={verticalDistance:F0})");
                return SceneComplexity.Direct;
            }

            // 2. 完全垂直对齐：极简场景
            if (horizontalDistance <3)
            {
                System.Diagnostics.Debug.WriteLine($"[DetectComplexity] 完全水平对齐 -> Direct (hDist={horizontalDistance:F0})");
                return SceneComplexity.Direct;
            }

            // 2. 计算障碍节点数量（排除源和目标节点）
            int obstacleCount = 0;
            if (allNodeRects != null && allNodeRects.Length > 0)
            {
                obstacleCount = allNodeRects.Count(rect =>
                    !rect.IsEmpty &&
                    rect != sourceNodeRect &&
                    rect != targetNodeRect);
            }
            System.Diagnostics.Debug.WriteLine($"[DetectComplexity] 障碍节点数量: {obstacleCount}");

            // 3. 检查基本路径是否会发生碰撞
            bool hasPotentialCollision = false;
            if (obstacleCount > 0)
            {
                // 尝试计算简单路径，检查是否碰撞
                var simplePath = sourceDirection.IsHorizontal()?
                    CalculateHorizontalFirstPath(sourcePosition, targetPosition, sourceDirection, targetDirection, dx, dy, sourceNodeRect, targetNodeRect):
                    CalculateVerticalFirstPath(sourcePosition, targetPosition, sourceDirection, targetDirection, dx, dy, sourceNodeRect, targetNodeRect);

                if (simplePath != null)
                {
                    hasPotentialCollision = HasCollision(
                        simplePath, allNodeRects, sourceNodeRect, targetNodeRect);
                }
                System.Diagnostics.Debug.WriteLine($"[DetectComplexity] 潜在碰撞检测: {(hasPotentialCollision ? "是" : "否")}");
            }

            // 4. 检查对齐程度
            bool horizontallyAligned = horizontalDistance < 3;
            bool verticallyAligned = verticalDistance < 3;
            System.Diagnostics.Debug.WriteLine($"[DetectComplexity] 对齐状态: 水平{(horizontallyAligned ? "对齐" : "不对齐")}, 垂直{(verticallyAligned ? "对齐" : "不对齐")}");

            // 5. 根据综合条件判断场景复杂度
            if (obstacleCount == 0)
            {
                // 无障碍：简单场景
                if (horizontallyAligned || verticallyAligned)
                {
                    System.Diagnostics.Debug.WriteLine($"[DetectComplexity] 无障碍且对齐 -> Direct");
                    return SceneComplexity.Direct;
                }
                System.Diagnostics.Debug.WriteLine($"[DetectComplexity] 无障碍且不对齐 -> Simple");
                return SceneComplexity.Simple;
            }
            else if (obstacleCount == 1 && !hasPotentialCollision)
            {
                // 有一个障碍但不会碰撞：简单场景
                System.Diagnostics.Debug.WriteLine($"[DetectComplexity] 1个障碍且无碰撞 -> Simple");
                return SceneComplexity.Simple;
            }
            else if (obstacleCount <= 2 && !hasPotentialCollision)
            {
                // 少量障碍但不会碰撞：中等场景
                System.Diagnostics.Debug.WriteLine($"[DetectComplexity] {obstacleCount}个障碍且无碰撞 -> Medium");
                return SceneComplexity.Medium;
            }
            else
            {
                // 多障碍或有碰撞风险：复杂场景
                System.Diagnostics.Debug.WriteLine($"[DetectComplexity] {obstacleCount}个障碍或有碰撞 -> Complex");
                return SceneComplexity.Complex;
            }
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
                System.Diagnostics.Debug.WriteLine($"[DetectAlignment] 完全对齐 -> NotAligned (vDist={verticalDistance:F0}, hDist={horizontalDistance:F0}, threshold={alignmentThreshold:F0})");
                return GeometricAlignment.NotAligned; // 重叠，实际上不是"对齐"
            }
            else if (verticallyAligned)
            {
                System.Diagnostics.Debug.WriteLine($"[DetectAlignment] 垂直对齐 -> VerticalAligned (vDist={verticalDistance:F0} < threshold)");
                return GeometricAlignment.VerticalAligned;
            }
            else if (horizontallyAligned)
            {
                System.Diagnostics.Debug.WriteLine($"[DetectAlignment] 水平对齐 -> HorizontalAligned (hDist={horizontalDistance:F0} < threshold)");
                return GeometricAlignment.HorizontalAligned;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[DetectAlignment] 不对齐 -> NotAligned (vDist={verticalDistance:F0}, hDist={horizontalDistance:F0})");
                return GeometricAlignment.NotAligned;
            }
        }

        /// <summary>
        /// 计算简单测试路径（用于碰撞检测）
        /// </summary>
        private Point[] CalculateSimpleTestPath(
            PortDirection sourceDirection,
            PortDirection targetDirection,
            double dx, double dy,
            double horizontalDistance,
            double verticalDistance)
        {
            // 根据端口方向选择最简单的路径策略
            if (sourceDirection.IsHorizontal())
            {
                // 源端口水平：优先水平延伸
                var midPoint = new Point(sourcePositionPlaceholder.X + dx * 0.5, sourcePositionPlaceholder.Y + dy);
                return new Point[]
                {
                    new Point(0, 0),
                    new Point(dx * 0.25, 0),
                    new Point(dx * 0.25, dy),
                    new Point(dx, dy)
                };
            }
            else
            {
                // 源端口垂直：优先垂直延伸
                return new Point[]
                {
                    new Point(0, 0),
                    new Point(0, dy * 0.25),
                    new Point(dx, dy * 0.25),
                    new Point(dx, dy)
                };
            }
        }

        // 占位符，用于简单测试路径计算
        private static Point sourcePositionPlaceholder = new Point(0, 0);

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
            System.Diagnostics.Debug.WriteLine($"[SelectOpposite] 相对方向策略选择 - 源方向:{sourceDirection}, 目标方向:{targetDirection}");
            System.Diagnostics.Debug.WriteLine($"[SelectOpposite] 场景复杂度:{sceneComplexity}, 几何对齐:{geometricAlignment}");

            // 根据场景复杂度选择策略
            switch (sceneComplexity)
            {
                case SceneComplexity.Direct:
                    // 极简场景：直接连接
                    System.Diagnostics.Debug.WriteLine($"[SelectOpposite] 极简场景，使用Direct策略");
                    return PathStrategy.Direct;

                case SceneComplexity.Simple:
                    // 简单场景：使用相对方向策略
                    System.Diagnostics.Debug.WriteLine($"[SelectOpposite] 简单场景，使用OppositeDirection策略");
                    return PathStrategy.OppositeDirection;

                case SceneComplexity.Medium:
                case SceneComplexity.Complex:
                    // 中等或复杂场景：使用相对方向策略
                    System.Diagnostics.Debug.WriteLine($"[SelectOpposite] 中等/复杂场景，使用OppositeDirection策略");
                    return PathStrategy.OppositeDirection;

                default:
                    System.Diagnostics.Debug.WriteLine($"[SelectOpposite] 默认使用OppositeDirection策略");
                    return PathStrategy.OppositeDirection;
            }
        }

        /// <summary>
        /// 为垂直方向场景选择策略（优化版本：基于场景复杂度）
        /// 一个水平一个垂直（Left-Top, Left-Bottom, Right-Top, Right-Bottom等）
        /// </summary>
        private PathStrategy SelectStrategyForPerpendicularDirection(
            SceneComplexity sceneComplexity,
            GeometricAlignment geometricAlignment,
            PortDirection sourceDirection,
            PortDirection targetDirection,
            double horizontalDistance,
            double verticalDistance,
            Rect sourceNodeRect,
            Rect targetNodeRect,
            Rect[] allNodeRects)
        {
            System.Diagnostics.Debug.WriteLine($"[SelectPerpendicular] 垂直方向策略选择 - 源方向:{sourceDirection}, 目标方向:{targetDirection}");
            System.Diagnostics.Debug.WriteLine($"[SelectPerpendicular] 场景复杂度:{sceneComplexity}, 几何对齐:{geometricAlignment}");

            // 简单或极简场景：选择简单策略
            if (sceneComplexity == SceneComplexity.Simple || sceneComplexity == SceneComplexity.Direct)
            {
                var simpleStrategy = SelectSimplestStrategy(
                    sourceDirection, targetDirection, horizontalDistance, verticalDistance);
                System.Diagnostics.Debug.WriteLine($"[SelectPerpendicular] 简单场景，使用{simpleStrategy}策略");
                return simpleStrategy;
            }

            // 中等或复杂场景：尝试碰撞检测优化
            if (sceneComplexity == SceneComplexity.Medium || sceneComplexity == SceneComplexity.Complex)
            {
                System.Diagnostics.Debug.WriteLine($"[SelectPerpendicular] 中等/复杂场景，尝试碰撞检测优化");
                if (allNodeRects != null && allNodeRects.Length > 0)
                {
                    var bestStrategy = FindBestStrategyWithoutCollision(
                        sourceDirection, targetDirection,
                        horizontalDistance > 0 ? horizontalDistance : -horizontalDistance,
                        verticalDistance > 0 ? verticalDistance : -verticalDistance,
                        horizontalDistance, verticalDistance,
                        sourceNodeRect, targetNodeRect,
                        allNodeRects);

                    if (bestStrategy.HasValue)
                    {
                        System.Diagnostics.Debug.WriteLine($"[SelectPerpendicular] 碰撞检测优化，使用{bestStrategy.Value}策略");
                        return bestStrategy.Value;
                    }
                    System.Diagnostics.Debug.WriteLine($"[SelectPerpendicular] 碰撞检测优化失败，进入智能判断");
                }
            }

            // 计算节点之间的相对距离
            double dx = targetNodeRect.X - sourceNodeRect.X;
            double dy = targetNodeRect.Y - sourceNodeRect.Y;
            System.Diagnostics.Debug.WriteLine($"[SelectPerpendicular] 节点相对距离: dx={dx:F0}, dy={dy:F0}");

            // 使用智能判断：基于端口朝向和相对位置的策略选择
            bool preferHorizontal = ShouldPreferHorizontal(
                sourceDirection, targetDirection,
                dx, dy,
                horizontalDistance, verticalDistance);

            if (preferHorizontal)
            {
                System.Diagnostics.Debug.WriteLine($"[SelectPerpendicular] 智能判断：水平优先");
                return PathStrategy.HorizontalFirst;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[SelectPerpendicular] 智能判断：垂直优先");
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
            System.Diagnostics.Debug.WriteLine($"[ShouldPreferHorizontal] 源方向:{sourceDirection}, 目标方向:{targetDirection}");
            System.Diagnostics.Debug.WriteLine($"[ShouldPreferHorizontal] dx:{dx:F1}, dy:{dy:F1}");
            System.Diagnostics.Debug.WriteLine($"[ShouldPreferHorizontal] 水平距离:{horizontalDistance:F1}, 垂直距离:{verticalDistance:F1}");

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
                        System.Diagnostics.Debug.WriteLine($"[ShouldPreferHorizontal] 目标在端口朝向同侧，优先水平");
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
                        System.Diagnostics.Debug.WriteLine($"[ShouldPreferHorizontal] 垂直方向更自然，优先垂直");
                        return false;
                    }

                    // 默认：根据距离判断
                    bool result = horizontalDistance >= verticalDistance;
                    System.Diagnostics.Debug.WriteLine($"[ShouldPreferHorizontal] 根据距离判断: {result}");
                    return result;
                }
                else
                {
                    // 目标端口也是水平方向：根据距离判断
                    bool result = horizontalDistance >= verticalDistance;
                    System.Diagnostics.Debug.WriteLine($"[ShouldPreferHorizontal] 两个都是水平端口，根据距离判断: {result}");
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
                        System.Diagnostics.Debug.WriteLine($"[ShouldPreferHorizontal] 目标在端口朝向同侧，优先垂直");
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
                        System.Diagnostics.Debug.WriteLine($"[ShouldPreferHorizontal] 水平方向更自然，优先水平");
                        return true;
                    }

                    // 默认：根据距离判断
                    bool result = horizontalDistance < verticalDistance;
                    System.Diagnostics.Debug.WriteLine($"[ShouldPreferHorizontal] 根据距离判断: {result}");
                    return result;
                }
                else
                {
                    // 目标端口也是垂直方向：根据距离判断
                    bool result = horizontalDistance < verticalDistance;
                    System.Diagnostics.Debug.WriteLine($"[ShouldPreferHorizontal] 两个都是垂直端口，根据距离判断: {result}");
                    return result;
                }
            }
        }

        /// <summary>
        /// 为同向场景选择策略（优化版本：基于场景复杂度）
        /// Left-Left, Right-Right, Top-Top, Bottom-Bottom
        /// </summary>
        private PathStrategy SelectStrategyForSameDirection(
            SceneComplexity sceneComplexity,
            GeometricAlignment geometricAlignment,
            PortDirection sourceDirection,
            PortDirection targetDirection,
            double dx, double dy,
            double horizontalDistance,
            double verticalDistance,
            Rect sourceNodeRect,
            Rect targetNodeRect,
            Rect[] allNodeRects)
        {
            System.Diagnostics.Debug.WriteLine($"[SelectSame] 同向策略选择 - 源方向:{sourceDirection}, 目标方向:{targetDirection}");
            System.Diagnostics.Debug.WriteLine($"[SelectSame] 场景复杂度:{sceneComplexity}, 几何对齐:{geometricAlignment}");

            // 极简场景或简单场景：选择简单策略
            if (sceneComplexity == SceneComplexity.Direct || sceneComplexity == SceneComplexity.Simple)
            {
                var simpleStrategy = SelectSimplestStrategy(
                    sourceDirection, targetDirection, horizontalDistance, verticalDistance);
                System.Diagnostics.Debug.WriteLine($"[SelectSame] 简单/极简场景，使用{simpleStrategy}策略");
                return simpleStrategy;
            }

            // 中等场景：检查是否可以使用三段式策略
            if (sceneComplexity == SceneComplexity.Medium)
            {
                System.Diagnostics.Debug.WriteLine($"[SelectSame] 中等场景，检查ThreeSegment可用性");
                if (CanUseThreeSegmentWithAvoidance(
                    sourceDirection, targetDirection, dx, dy,
                    sourceNodeRect, targetNodeRect, allNodeRects))
                {
                    System.Diagnostics.Debug.WriteLine($"[SelectSame] 中等场景，可以使用ThreeSegment策略");
                    return PathStrategy.ThreeSegment;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[SelectSame] 中等场景，ThreeSegment不足，使用FourSegment策略");
                    return PathStrategy.FourSegment;
                }
            }

            // 复杂场景：使用更复杂的策略
            if (sceneComplexity == SceneComplexity.Complex)
            {
                System.Diagnostics.Debug.WriteLine($"[SelectSame] 复杂场景，估计避让点数量");
                // 估计需要的避让点数量
                int requiredAvoidancePoints = CountRequiredAvoidancePoints(
                    sourceDirection, targetDirection, dx, dy,
                    sourceNodeRect, targetNodeRect, allNodeRects);
                System.Diagnostics.Debug.WriteLine($"[SelectSame] 需要的避让点数量: {requiredAvoidancePoints}");

                if (requiredAvoidancePoints <= 1)
                {
                    System.Diagnostics.Debug.WriteLine($"[SelectSame] 复杂场景，避让点少，使用FourSegment策略");
                    return PathStrategy.FourSegment;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[SelectSame] 复杂场景，避让点多，使用FiveSegment策略");
                    return PathStrategy.FiveSegment;
                }
            }

            // 默认：基于几何对齐选择
            System.Diagnostics.Debug.WriteLine($"[SelectSame] 默认策略选择，基于几何对齐");
            if (geometricAlignment == GeometricAlignment.VerticalAligned && sourceDirection.IsHorizontal())
            {
                System.Diagnostics.Debug.WriteLine($"[SelectSame] 垂直对齐+水平端口，使用HorizontalFirst策略");
                return PathStrategy.HorizontalFirst;
            }
            else if (geometricAlignment == GeometricAlignment.HorizontalAligned && sourceDirection.IsVertical())
            {
                System.Diagnostics.Debug.WriteLine($"[SelectSame] 水平对齐+垂直端口，使用VerticalFirst策略");
                return PathStrategy.VerticalFirst;
            }
            else
            {
                // 不对齐：使用三段式
                System.Diagnostics.Debug.WriteLine($"[SelectSame] 不对齐，使用ThreeSegment策略");
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
            System.Diagnostics.Debug.WriteLine($"[SelectSimplest] 选择最简单策略 - hDist={horizontalDistance:F0}, vDist={verticalDistance:F0}");

            // 极近距离：直接连接
            if (IsVeryCloseDistance(horizontalDistance, verticalDistance))
            {
                System.Diagnostics.Debug.WriteLine($"[SelectSimplest] 极近距离，选择Direct策略");
                return PathStrategy.Direct;
            }

            // 根据端口方向选择简单策略
            if (sourceDirection.IsHorizontal())
            {
                // 水平端口：优先水平延伸
                System.Diagnostics.Debug.WriteLine($"[SelectSimplest] 水平端口方向，选择HorizontalFirst策略");
                return PathStrategy.HorizontalFirst;
            }
            else
            {
                // 垂直端口：优先垂直延伸
                System.Diagnostics.Debug.WriteLine($"[SelectSimplest] 垂直端口方向，选择VerticalFirst策略");
                return PathStrategy.VerticalFirst;
            }
        }

        /// <summary>
        /// 检查是否可以使用三段式策略进行避让
        /// </summary>
        private bool CanUseThreeSegmentWithAvoidance(
            PortDirection sourceDirection,
            PortDirection targetDirection,
            double dx, double dy,
            Rect sourceNodeRect,
            Rect targetNodeRect,
            Rect[] allNodeRects)
        {
            // 如果没有节点信息，假设可以使用
            if (allNodeRects == null || allNodeRects.Length == 0)
            {
                return true;
            }

            // 计算三段式路径的两个可能中间点
            var midPoint1 = new Point(dx * 0.5, 0); // 水平优先
            var midPoint2 = new Point(0, dy * 0.5); // 垂直优先

            // 测试两个中间点是否会碰撞
            Point[] testPath1 = { new Point(0, 0), midPoint1, new Point(dx, dy) };
            Point[] testPath2 = { new Point(0, 0), midPoint2, new Point(dx, dy) };

            bool path1HasCollision = HasCollision(testPath1, allNodeRects, sourceNodeRect, targetNodeRect);
            bool path2HasCollision = HasCollision(testPath2, allNodeRects, sourceNodeRect, targetNodeRect);

            // 如果至少有一个路径不会碰撞，可以使用三段式
            return !path1HasCollision || !path2HasCollision;
        }

        /// <summary>
        /// 估计需要的避让点数量
        /// </summary>
        private int CountRequiredAvoidancePoints(
            PortDirection sourceDirection,
            PortDirection targetDirection,
            double dx, double dy,
            Rect sourceNodeRect,
            Rect targetNodeRect,
            Rect[] allNodeRects)
        {
            if (allNodeRects == null || allNodeRects.Length == 0)
            {
                return 0;
            }

            // 计算基本路径
            var basicPath = CalculateSimpleTestPath(
                sourceDirection, targetDirection, dx, dy,
                Math.Abs(dx), Math.Abs(dy));

            if (basicPath == null)
            {
                return 0;
            }

            // 统计碰撞数量
            int collisionCount = 0;
            for (int i = 0; i < basicPath.Length - 1; i++)
            {
                var p1 = basicPath[i];
                var p2 = basicPath[i + 1];

                foreach (var rect in allNodeRects)
                {
                    if (!rect.IsEmpty && rect != sourceNodeRect && rect != targetNodeRect)
                    {
                        if (LineIntersectsRect(p1, p2, rect))
                        {
                            collisionCount++;
                        }
                    }
                }
            }

            // 每个碰撞可能需要1-2个避让点
            return (int)Math.Ceiling(collisionCount * 1.5);
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
        /// 查找无碰撞的最佳策略
        /// </summary>
        private PathStrategy? FindBestStrategyWithoutCollision(
            PortDirection sourceDirection,
            PortDirection targetDirection,
            double dx, double dy,
            double hDist, double vDist,
            Rect sourceNodeRect,
            Rect targetNodeRect,
            Rect[] allNodeRects)
        {
            // 如果没有节点信息，返回null
            if (allNodeRects == null || allNodeRects.Length == 0)
                return null;

            // 尝试两种策略，选择无碰撞的
            var strategies = new[] { PathStrategy.HorizontalFirst, PathStrategy.VerticalFirst };

            foreach (var strategy in strategies)
            {
                var pathPoints = CalculatePathByStrategy(
                    new Point(0, 0),  // 源位置（临时）
                    new Point(dx, dy), // 目标位置（临时）
                    sourceDirection,
                    targetDirection,
                    strategy,
                    dx, dy,
                    sourceNodeRect,
                    targetNodeRect);

                if (!HasCollision(pathPoints, allNodeRects, sourceNodeRect, targetNodeRect))
                {
                    return strategy;
                }
            }

            return null;
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
            if (rect.IsEmpty)
                return false;

            // 第一层：快速排除 - 检查线段边界框和矩形边界框是否相交
            var lineMinX = Math.Min(p1.X, p2.X);
            var lineMaxX = Math.Max(p1.X, p2.X);
            var lineMinY = Math.Min(p1.Y, p2.Y);
            var lineMaxY = Math.Max(p1.Y, p2.Y);

            // 如果线段完全在矩形外，快速返回false
            if (lineMaxX < rect.Left - PathClearanceDistance ||
                lineMinX > rect.Right + PathClearanceDistance ||
                lineMaxY < rect.Top - PathClearanceDistance ||
                lineMinY > rect.Bottom + PathClearanceDistance)
            {
                return false;
            }

            // 第二层：多采样点检测 - 检测线段上的多个采样点
            // 增加采样点密度：10%、25%、40%、60%、75%、90%
            double[] sampleRatios = { 0.10, 0.25, 0.40, 0.60, 0.75, 0.90 };
            // 扩展矩形：从原矩形边界向内/外扩展安全距离
            var expandedRect = new Rect(
                rect.Left - PathClearanceDistance,
                rect.Top - PathClearanceDistance,
                rect.Width + PathClearanceDistance * 2,
                rect.Height + PathClearanceDistance * 2);

            foreach (var ratio in sampleRatios)
            {
                var sampleX = p1.X + (p2.X - p1.X) * ratio;
                var sampleY = p1.Y + (p2.Y - p1.Y) * ratio;
                var samplePoint = new Point(sampleX, sampleY);

                if (IsPointInRect(samplePoint, expandedRect))
                {
                    return true;
                }
            }

            // 额外检查：如果线段是水平的，检测线段Y坐标是否在矩形Y范围内
            if (Math.Abs(p1.Y - p2.Y) < 1.0)
            {
                double lineY = p1.Y;
                if (lineY >= expandedRect.Top && lineY <= expandedRect.Bottom)
                {
                    // 重用之前计算的边界值
                    if (lineMaxX >= expandedRect.Left && lineMinX <= expandedRect.Right)
                    {
                        return true;
                    }
                }
            }
            // 额外检查：如果线段是垂直的，检测线段X坐标是否在矩形X范围内
            else if (Math.Abs(p1.X - p2.X) < 1.0)
            {
                double lineX = p1.X;
                if (lineX >= expandedRect.Left && lineX <= expandedRect.Right)
                {
                    // 重用之前计算的边界值
                    if (lineMaxY >= expandedRect.Top && lineMinY <= expandedRect.Bottom)
                    {
                        return true;
                    }
                }
            }

            // 第三层：线段相交检测 - 完整的线段与矩形四条边的相交检测
            var rectCorners = new[]
            {
                new Point(rect.Left, rect.Top),
                new Point(rect.Right, rect.Top),
                new Point(rect.Right, rect.Bottom),
                new Point(rect.Left, rect.Bottom)
            };

            // 检查线段与矩形四条边是否相交
            for (int i = 0; i < 4; i++)
            {
                var corner1 = rectCorners[i];
                var corner2 = rectCorners[(i + 1) % 4];
                if (LineSegmentsIntersect(p1, p2, corner1, corner2))
                {
                    return true;
                }
            }

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
            System.Diagnostics.Debug.WriteLine($"[CalculatePathByStrategy] 计算路径 - 策略:{strategy}");

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
                    System.Diagnostics.Debug.WriteLine($"[CalculatePathByStrategy] 默认策略: HorizontalFirst");
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
            System.Diagnostics.Debug.WriteLine($"[CalculateDirect] 极近距离直接连接 - 源点:({sourcePosition.X:F1},{sourcePosition.Y:F1}), 目标点:({targetPosition.X:F1},{targetPosition.Y:F1})");
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
            System.Diagnostics.Debug.WriteLine($"[CalculateHorizontalFirst] 3段路径 - 拐点1:({p1.X:F1},{p1.Y:F1}), 拐点2:({p2.X:F1},{p2.Y:F1})");
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
            System.Diagnostics.Debug.WriteLine($"[CalculateVerticalFirst] 3段路径 - 拐点1:({p1.X:F1},{p1.Y:F1}), 拐点2:({p2.X:F1},{p2.Y:F1})");
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
            System.Diagnostics.Debug.WriteLine($"[CalculateThreeSegment] 2段路径 - 中间点:({betterMidPoint.X:F1},{betterMidPoint.Y:F1})");
            return path;
        }

        /// <summary>
        /// 计算相对方向路径（用于Top-Bottom, Bottom-Top, Left-Right, Right-Left等相对方向连接）
        /// 路径模式：沿源方向延伸 → 水平/垂直 → 到达目标
        /// 注意：避让逻辑已由统一的ApplyNodeAvoidance方法处理，此方法只负责基本路径计算
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
            // 计算第一个拐点：沿源方向延伸（确保不穿过源节点）
            var p1 = CalculateOptimizedFirstPoint(sourcePosition,targetPosition ,sourceDirection, dx, dy, sourceNodeRect, targetNodeRect);

            // 计算第二个拐点：水平或垂直到目标，确保不穿过目标节点
            Point p2;

            if (sourceDirection.IsVertical())
            {
                // 垂直相对：Top-Bottom, Bottom-Top
                // 先垂直延伸（p1），再水平移动（p2）
                // 智能避让策略：选择最佳的避让方向
                var targetNodeLeft = targetNodeRect.Left;
                var targetNodeRight = targetNodeRect.Right;
                var targetNodeTop = targetNodeRect.Top;
                var targetNodeBottom = targetNodeRect.Bottom;
                var targetNodeCenterX = (targetNodeLeft + targetNodeRight) / 2;
                
                // 确保安全距离，根据节点大小动态调整
                var safeDistance = NodeSafeDistance * 2 + targetNodeRect.Width / 2;
                
                // 智能选择避让方向
                if (targetPosition.X > sourcePosition.X)
                {
                    // 目标在右侧
                    if (targetPosition.X > targetNodeCenterX)
                    {
                        // 目标偏右：从左侧避让（更自然）
                        var safeX = targetNodeLeft - safeDistance;
                        p2 = new Point(Math.Min(targetPosition.X, safeX), p1.Y);
                    }
                    else
                    {
                        // 目标在中间：从右侧避让（避免穿过）
                        var safeX = targetNodeRight + safeDistance;
                        p2 = new Point(Math.Max(targetPosition.X, safeX), p1.Y);
                    }
                }
                else
                {
                    // 目标在左侧
                    if (targetPosition.X < targetNodeCenterX)
                    {
                        // 目标偏左：从右侧避让（更自然）
                        var safeX = targetNodeRight + safeDistance;
                        p2 = new Point(Math.Max(targetPosition.X, safeX), p1.Y);
                    }
                    else
                    {
                        // 目标在中间：从左侧避让（避免穿过）
                        var safeX = targetNodeLeft - safeDistance;
                        p2 = new Point(Math.Min(targetPosition.X, safeX), p1.Y);
                    }
                }
            }
            else
            {
                // 水平相对：Left-Right, Right-Left
                // 先水平延伸（p1），再垂直移动（p2）
                // 智能避让策略：选择最佳的避让方向
                var targetNodeTop = targetNodeRect.Top;
                var targetNodeBottom = targetNodeRect.Bottom;
                var targetNodeLeft = targetNodeRect.Left;
                var targetNodeRight = targetNodeRect.Right;
                var targetNodeCenterY = (targetNodeTop + targetNodeBottom) / 2;
                
                // 确保安全距离，根据节点大小动态调整
                var safeDistance = NodeSafeDistance * 2 + targetNodeRect.Height / 2;
                
                // 智能选择避让方向
                if (targetPosition.Y > sourcePosition.Y)
                {
                    // 目标在下侧
                    if (targetPosition.Y > targetNodeCenterY)
                    {
                        // 目标偏下：从上侧避让（更自然）
                        var safeY = targetNodeTop - safeDistance;
                        p2 = new Point(p1.X, Math.Min(targetPosition.Y, safeY));
                    }
                    else
                    {
                        // 目标在中间：从下侧避让（避免穿过）
                        var safeY = targetNodeBottom + safeDistance;
                        p2 = new Point(p1.X, Math.Max(targetPosition.Y, safeY));
                    }
                }
                else
                {
                    // 目标在上侧
                    if (targetPosition.Y < targetNodeCenterY)
                    {
                        // 目标偏上：从下侧避让（更自然）
                        var safeY = targetNodeBottom + safeDistance;
                        p2 = new Point(p1.X, Math.Max(targetPosition.Y, safeY));
                    }
                    else
                    {
                        // 目标在中间：从上侧避让（避免穿过）
                        var safeY = targetNodeTop - safeDistance;
                        p2 = new Point(p1.X, Math.Min(targetPosition.Y, safeY));
                    }
                }
            }

            System.Diagnostics.Debug.WriteLine($"[OppositeDirection] 基本路径计算完成（避让由统一后处理处理）");
            System.Diagnostics.Debug.WriteLine($"[OppositeDirection] 路径点:");
            System.Diagnostics.Debug.WriteLine($"[OppositeDirection]   源位置:{sourcePosition}");
            System.Diagnostics.Debug.WriteLine($"[OppositeDirection]   拐点1(沿源方向延伸):{p1}");
            System.Diagnostics.Debug.WriteLine($"[OppositeDirection]   拐点2(水平/垂直):{p2}");
            System.Diagnostics.Debug.WriteLine($"[OppositeDirection]   目标位置:{targetPosition}");

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
            System.Diagnostics.Debug.WriteLine($"[CalculateFourSegment] 4段路径 - 拐点1:({p1.X:F1},{p1.Y:F1}), 拐点2:({p2.X:F1},{p2.Y:F1}), 拐点3:({p3.X:F1},{p3.Y:F1})");
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
                System.Diagnostics.Debug.WriteLine($"[CalculateFiveSegment] 水平端口5段路径 - 拐点1:({p1.X:F1},{p1.Y:F1}), 拐点2:({p2.X:F1},{p2.Y:F1}), 拐点3:({p3.X:F1},{p3.Y:F1})");
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
                System.Diagnostics.Debug.WriteLine($"[CalculateFiveSegment] 垂直端口5段路径 - 拐点1:({p1.X:F1},{p1.Y:F1}), 拐点2:({p2.X:F1},{p2.Y:F1}), 拐点3:({p3.X:F1},{p3.Y:F1})");
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

            System.Diagnostics.Debug.WriteLine($"[CalculateOptimizedFirstPoint] 源方向:{sourceDirection}, 计算偏移量:{requiredOffset:F1}");

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

            // 关键日志：记录箭头计算结果
            System.Diagnostics.Debug.WriteLine($"[ArrowCalc] ========== 箭头计算结果 ==========");
            System.Diagnostics.Debug.WriteLine($"[ArrowCalc] 箭头尖端位置（目标端口）:({arrowPosition.X:F1},{arrowPosition.Y:F1})");
            System.Diagnostics.Debug.WriteLine($"[ArrowCalc] 目标端口方向:{targetDirection}, 固定箭头角度:{arrowAngle:F1}°");
            System.Diagnostics.Debug.WriteLine($"[ArrowCalc] 箭头尾部位置（路径终点）:({lastPoint.X:F1},{lastPoint.Y:F1})");

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
        /// 检查是否应该跳过避让处理（方案A：智能避让触发机制）
        /// 避免简单场景下过度曲折
        /// </summary>
        private bool ShouldSkipAvoidance(
            Point[] pathPoints,
            Point sourcePosition,
            Point targetPosition,
            PortDirection sourceDirection,
            PortDirection targetDirection,
            Rect targetNodeRect,
            Rect[] allNodeRects)
        {
            // 1. 计算路径总长度
            double totalDistance = 0;
            for (int i = 0; i < pathPoints.Length - 1; i++)
            {
                var dx = pathPoints[i + 1].X - pathPoints[i].X;
                var dy = pathPoints[i + 1].Y - pathPoints[i].Y;
                totalDistance += Math.Sqrt(dx * dx + dy * dy);
            }

            // 2. 直线距离（欧几里得距离）
            var directDx = targetPosition.X - sourcePosition.X;
            var directDy = targetPosition.Y - sourcePosition.Y;
            var directDistance = Math.Sqrt(directDx * directDx + directDy * directDy);

            // 3. 如果路径长度接近直线距离（< 1.5倍），说明路径已经很合理
            if (totalDistance < directDistance * 1.5)
            {
                System.Diagnostics.Debug.WriteLine($"[ShouldSkipAvoidance] 路径合理（路径长度={totalDistance:F0}, 直线距离={directDistance:F0}），优先保持");
                return true;
            }

            // 4. 如果路径点数 <= 4 且 路径长度合理，跳过避让
            if (pathPoints.Length <= 4 && totalDistance < directDistance * 2.0)
            {
                System.Diagnostics.Debug.WriteLine($"[ShouldSkipAvoidance] 简单路径（点数={pathPoints.Length}, 长度比={totalDistance/directDistance:F2}），跳过避让");
                return true;
            }

            // 5. 如果节点数量少（<= 2），且路径点数少，检查是否需要避让
            int obstacleCount = (allNodeRects?.Length ?? 0);
            if (obstacleCount <= 2 && pathPoints.Length <= 4)
            {
                // 检查路径是否会穿过目标节点
                bool willCrossTarget = WillPathCrossTarget(pathPoints, targetNodeRect);
                if (willCrossTarget)
                {
                    System.Diagnostics.Debug.WriteLine($"[ShouldSkipAvoidance] 节点少({obstacleCount})但路径会穿过目标，需要避让");
                    return false;
                }
                System.Diagnostics.Debug.WriteLine($"[ShouldSkipAvoidance] 节点少({obstacleCount})且路径简单，跳过避让");
                return true;
            }

            // 6. 如果路径点数很少（2-4个），说明已经是简单路径
            if (pathPoints.Length <= 4 && totalDistance < directDistance * 2.5)
            {
                System.Diagnostics.Debug.WriteLine($"[ShouldSkipAvoidance] 路径点数少({pathPoints.Length})，优先保持");
                return true;
            }

            return false;
        }

        /// <summary>
        /// 检查路径是否会穿过目标节点
        /// </summary>
        private bool WillPathCrossTarget(Point[] pathPoints, Rect targetNodeRect)
        {
            if (pathPoints == null || pathPoints.Length < 2 || targetNodeRect.IsEmpty)
            {
                return false;
            }

            // 检查每一段路径是否会穿过目标节点
            for (int i = 0; i < pathPoints.Length - 1; i++)
            {
                var p1 = pathPoints[i];
                var p2 = pathPoints[i + 1];

                // 检查这一段路径是否会穿过目标节点
                if (LineIntersectsRect(p1, p2, targetNodeRect))
                {
                    System.Diagnostics.Debug.WriteLine($"[WillPathCrossTarget] 路径段[{i}]穿过目标节点: ({p1.X:F1},{p1.Y:F1}) -> ({p2.X:F1},{p2.Y:F1})");
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 检查两条线段是否相交
        /// </summary>
        private bool LineIntersectsLine(Point p1, Point p2, Point p3, Point p4)
        {
            // 使用叉积方法判断两线段是否相交
            double d1 = CrossProduct(p3, p4, p1);
            double d2 = CrossProduct(p3, p4, p2);
            double d3 = CrossProduct(p1, p2, p3);
            double d4 = CrossProduct(p1, p2, p4);

            // 如果叉积符号不同，说明线段相交
            if (((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)) &&
                ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0)))
            {
                return true;
            }

            // 端点在另一条线段上的情况
            return false;
        }

        /// <summary>
        /// 计算叉积
        /// </summary>
        private double CrossProduct(Point p1, Point p2, Point p)
        {
            return (p2.X - p1.X) * (p.Y - p1.Y) - (p.Y - p1.Y) * (p2.X - p1.X);
        }

        /// <summary>
        /// 模块3：统一的节点避让后处理方法（优化版本：智能触发机制）
        /// 对计算好的路径进行后处理，确保路径不穿过任何节点
        /// 优先级：与目标端口方向一致 > 垂直/水平延伸 > 其他方向
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
            System.Diagnostics.Debug.WriteLine($"[ApplyNodeAvoidance] ========== 开始节点避让后处理 ==========");

            // 新增：快速检查 - 如果场景简单，直接返回
            if (ShouldSkipAvoidance(pathPoints, sourcePosition, targetPosition,
                sourceDirection, targetDirection, targetNodeRect, allNodeRects))
            {
                System.Diagnostics.Debug.WriteLine($"[ApplyNodeAvoidance] 简单场景，跳过避让处理");
                return pathPoints;
            }

            if (allNodeRects == null || allNodeRects.Length == 0)
            {
                System.Diagnostics.Debug.WriteLine($"[ApplyNodeAvoidance] 无节点信息，跳过避让处理");
                return pathPoints;
            }

            // 迭代检测和调整（最多5次）
            var currentPath = pathPoints;
            int maxIterations = 5;

            for (int iteration = 0; iteration < maxIterations; iteration++)
            {
                System.Diagnostics.Debug.WriteLine($"[ApplyNodeAvoidance] 迭代 {iteration + 1}/{maxIterations}");

                // 检查路径是否与任何节点发生碰撞
                var collisionResult = FindCollisionSegment(
                    currentPath,
                    allNodeRects,
                    sourceNodeRect,
                    targetNodeRect);

                if (!collisionResult.HasValue)
                {
                    System.Diagnostics.Debug.WriteLine($"[ApplyNodeAvoidance] 无碰撞，避让处理完成");
                    break;
                }

                var (segmentIndex, collidingRect) = collisionResult.Value;
                System.Diagnostics.Debug.WriteLine($"[ApplyNodeAvoidance] 发现碰撞: 段{segmentIndex}与节点({collidingRect.X:F1},{collidingRect.Y:F1},{collidingRect.Width:F1}x{collidingRect.Height:F1})");

                // 计算避让拐点
                var avoidancePoints = CalculateAvoidancePoints(
                    currentPath,
                    segmentIndex,
                    collidingRect,
                    sourceDirection,
                    targetDirection,
                    allNodeRects,
                    targetNodeRect);

                if (avoidancePoints != null && avoidancePoints.Length > 0)
                {
                    // 插入避让拐点到路径中
                    currentPath = InsertAvoidancePoints(currentPath, segmentIndex, avoidancePoints);
                    System.Diagnostics.Debug.WriteLine($"[ApplyNodeAvoidance] 插入{avoidancePoints.Length}个避让拐点，新路径点数:{currentPath.Length}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[ApplyNodeAvoidance] 警告: 无法找到有效的避让拐点");
                    break;
                }
            }

            System.Diagnostics.Debug.WriteLine($"[ApplyNodeAvoidance] ========== 避让处理完成 ==========");
            return currentPath;
        }

        /// <summary>
        /// 查找路径中与节点发生碰撞的线段（优化版本：方案B - 排除目标节点）
        /// 注意：排除目标节点，避免与目标节点边界"碰撞"触发不必要的避让
        /// </summary>
        private (int segmentIndex, Rect collidingRect)? FindCollisionSegment(
            Point[] pathPoints,
            Rect[] allNodeRects,
            Rect excludeSource,
            Rect excludeTarget)
        {
            var relevantRects = allNodeRects.Where(rect =>
                !rect.IsEmpty &&
                rect != excludeSource &&
                rect != excludeTarget).ToList(); // 方案B：排除目标节点，避免误判碰撞

            for (int i = 0; i < pathPoints.Length - 1; i++)
            {
                var p1 = pathPoints[i];
                var p2 = pathPoints[i + 1];

                foreach (var rect in relevantRects)
                {
                    if (LineIntersectsRect(p1, p2, rect))
                    {
                        System.Diagnostics.Debug.WriteLine($"[FindCollisionSegment] 发现碰撞: 段{i} ({p1.X:F1},{p1.Y:F1})->({p2.X:F1},{p2.Y:F1}) 与节点({rect.X:F1},{rect.Y:F1},{rect.Width:F1}x{rect.Height:F1})");
                        return (i, rect);
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// 计算避让拐点（核心算法）
        /// 根据碰撞情况和目标端口方向，计算最优的避让拐点
        /// </summary>
        private Point[] CalculateAvoidancePoints(
            Point[] pathPoints,
            int collisionSegmentIndex,
            Rect collidingRect,
            PortDirection sourceDirection,
            PortDirection targetDirection,
            Rect[] allNodeRects,
            Rect targetNodeRect)
        {
            var p1 = pathPoints[collisionSegmentIndex];
            var p2 = pathPoints[collisionSegmentIndex + 1];

            // 判断线段是水平还是垂直
            bool isHorizontal = Math.Abs(p1.Y - p2.Y) < 1.0;

            System.Diagnostics.Debug.WriteLine($"[CalculateAvoidancePoints] 碰撞线段: ({p1.X:F1},{p1.Y:F1}) -> ({p2.X:F1},{p2.Y:F1})");
            System.Diagnostics.Debug.WriteLine($"[CalculateAvoidancePoints] 碰撞节点: ({collidingRect.X:F1},{collidingRect.Y:F1},{collidingRect.Width:F1}x{collidingRect.Height:F1})");
            System.Diagnostics.Debug.WriteLine($"[CalculateAvoidancePoints] 线段类型: {(isHorizontal ? "水平" : "垂直")}");

            // 计算两个候选避让拐点（一个绕上/左，一个绕下/右）
            Point[] candidatePoints;

            if (isHorizontal)
            {
                // 水平线段：垂直避让（绕上方或绕下方）
                candidatePoints = new Point[]
                {
                    new Point(p1.X, collidingRect.Top - NodeSafeDistance),  // 上方避让
                    new Point(p1.X, collidingRect.Bottom + NodeSafeDistance) // 下方避让
                };
            }
            else
            {
                // 垂直线段：水平避让（绕左侧或绕右侧）
                candidatePoints = new Point[]
                {
                    new Point(collidingRect.Left - NodeSafeDistance, p1.Y),  // 左侧避让
                    new Point(collidingRect.Right + NodeSafeDistance, p1.Y) // 右侧避让
                };
            }

            // 按优先级选择避让点：第一优先级是与目标端口方向一致
            Point[] prioritizedPoints;

            if (isHorizontal)
            {
                // 水平线段，目标端口在Top或Bottom方向
                if (targetDirection == PortDirection.Bottom)
                {
                    // 目标端口在下方，优先从上方接近（最后一段向下）
                    prioritizedPoints = new Point[] { candidatePoints[0], candidatePoints[1] };
                }
                else if (targetDirection == PortDirection.Top)
                {
                    // 目标端口在上方，优先从下方接近（最后一段向上）
                    prioritizedPoints = new Point[] { candidatePoints[1], candidatePoints[0] };
                }
                else
                {
                    // 其他方向，使用默认顺序
                    prioritizedPoints = candidatePoints;
                }
            }
            else
            {
                // 垂直线段，目标端口在Left或Right方向
                if (targetDirection == PortDirection.Right)
                {
                    // 目标端口在右方，优先从左方接近（最后一段向右）
                    prioritizedPoints = new Point[] { candidatePoints[0], candidatePoints[1] };
                }
                else if (targetDirection == PortDirection.Left)
                {
                    // 目标端口在左方，优先从右方接近（最后一段向左）
                    prioritizedPoints = new Point[] { candidatePoints[1], candidatePoints[0] };
                }
                else
                {
                    // 其他方向，使用默认顺序
                    prioritizedPoints = candidatePoints;
                }
            }

            // 选择第一个无碰撞的候选点
            foreach (var candidate in prioritizedPoints)
            {
                // 测试从p1到candidate到p2的路径是否与任何节点碰撞
                // 注意：excludeTargetNode=false，确保检测与目标节点的碰撞
                if (!HasCollision(new[] { p1, candidate, p2 }, allNodeRects, Rect.Empty, targetNodeRect, excludeTargetNode: false))
                {
                    System.Diagnostics.Debug.WriteLine($"[CalculateAvoidancePoints] 选择避让点:({candidate.X:F1},{candidate.Y:F1})");
                    return new Point[] { candidate };
                }
            }

            // 如果两个单点都无法满足，尝试双点避让（更复杂的避让）
            System.Diagnostics.Debug.WriteLine($"[CalculateAvoidancePoints] 单点避让失败，尝试双点避让");
            var twoPointResult = CalculateTwoPointAvoidance(
                p1, p2, collidingRect, isHorizontal,
                sourceDirection, targetDirection,
                allNodeRects, targetNodeRect);

            if (twoPointResult != null)
            {
                return twoPointResult;
            }

            // 如果双点避让也失败，尝试三段式避让（更灵活的绕行）
            System.Diagnostics.Debug.WriteLine($"[CalculateAvoidancePoints] 双点避让失败，尝试三段式避让");
            return CalculateThreePointAvoidance(
                p1, p2, collidingRect, isHorizontal,
                sourceDirection, targetDirection,
                allNodeRects, targetNodeRect);
        }

        /// <summary>
        /// 计算三段式避让（最复杂的避让策略）
        /// </summary>
        private Point[] CalculateThreePointAvoidance(
            Point p1, Point p2, Rect collidingRect, bool isHorizontal,
            PortDirection sourceDirection, PortDirection targetDirection,
            Rect[] allNodeRects, Rect targetNodeRect)
        {
            System.Diagnostics.Debug.WriteLine($"[CalculateThreePointAvoidance] 开始三段式避让");

            if (isHorizontal)
            {
                // 水平线段：绕上或绕下，使用三个拐点
                // 方案1：向上绕行：p1 -> (p1.X, safeY) -> (p2.X, safeY) -> p2
                double safeY1 = collidingRect.Top - NodeSafeDistance * 1.5;
                double safeY2 = collidingRect.Bottom + NodeSafeDistance * 1.5;

                // 尝试向上绕行
                var upPoints = new Point[]
                {
                    new Point(p1.X, safeY1),
                    new Point(p2.X, safeY1)
                };

                if (!HasCollision(new[] { p1, upPoints[0], upPoints[1], p2 },
                    allNodeRects, Rect.Empty, targetNodeRect, excludeTargetNode: false))
                {
                    System.Diagnostics.Debug.WriteLine($"[CalculateThreePointAvoidance] ✓ 向上绕行成功");
                    return upPoints;
                }

                // 尝试向下绕行
                var downPoints = new Point[]
                {
                    new Point(p1.X, safeY2),
                    new Point(p2.X, safeY2)
                };

                if (!HasCollision(new[] { p1, downPoints[0], downPoints[1], p2 },
                    allNodeRects, Rect.Empty, targetNodeRect, excludeTargetNode: false))
                {
                    System.Diagnostics.Debug.WriteLine($"[CalculateThreePointAvoidance] ✓ 向下绕行成功");
                    return downPoints;
                }
            }
            else
            {
                // 垂直线段：绕左或绕右，使用三个拐点
                // 方案1：向左绕行：p1 -> (safeX, p1.Y) -> (safeX, p2.Y) -> p2
                double safeX1 = collidingRect.Left - NodeSafeDistance * 1.5;
                double safeX2 = collidingRect.Right + NodeSafeDistance * 1.5;

                // 尝试向左绕行
                var leftPoints = new Point[]
                {
                    new Point(safeX1, p1.Y),
                    new Point(safeX1, p2.Y)
                };

                if (!HasCollision(new[] { p1, leftPoints[0], leftPoints[1], p2 },
                    allNodeRects, Rect.Empty, targetNodeRect, excludeTargetNode: false))
                {
                    System.Diagnostics.Debug.WriteLine($"[CalculateThreePointAvoidance] ✓ 向左绕行成功");
                    return leftPoints;
                }

                // 尝试向右绕行
                var rightPoints = new Point[]
                {
                    new Point(safeX2, p1.Y),
                    new Point(safeX2, p2.Y)
                };

                if (!HasCollision(new[] { p1, rightPoints[0], rightPoints[1], p2 },
                    allNodeRects, Rect.Empty, targetNodeRect, excludeTargetNode: false))
                {
                    System.Diagnostics.Debug.WriteLine($"[CalculateThreePointAvoidance] ✓ 向右绕行成功");
                    return rightPoints;
                }
            }

            System.Diagnostics.Debug.WriteLine($"[CalculateThreePointAvoidance] 警告: 三段式避让也失败，尝试直接绕过");
            return CalculateDirectBypass(p1, p2, collidingRect, isHorizontal, allNodeRects, targetNodeRect);
        }

        /// <summary>
        /// 计算直接绕过（绕过四个角落）
        /// </summary>
        private Point[] CalculateDirectBypass(
            Point p1, Point p2, Rect rect, bool isHorizontal,
            Rect[] allNodeRects, Rect targetNodeRect)
        {
            System.Diagnostics.Debug.WriteLine($"[CalculateDirectBypass] 尝试直接绕过四个角落");

            // 尝试绕过四个角落
            var cornerPoints = new Point[]
            {
                new Point(rect.Left - NodeSafeDistance, rect.Top - NodeSafeDistance),    // 左上
                new Point(rect.Right + NodeSafeDistance, rect.Top - NodeSafeDistance),   // 右上
                new Point(rect.Right + NodeSafeDistance, rect.Bottom + NodeSafeDistance), // 右下
                new Point(rect.Left - NodeSafeDistance, rect.Bottom + NodeSafeDistance)   // 左下
            };

            foreach (var corner in cornerPoints)
            {
                // 使用两个拐点绕过角落
                Point[] bypassPoints;
                if (isHorizontal)
                {
                    bypassPoints = new Point[]
                    {
                        new Point(p1.X, corner.Y),
                        new Point(p2.X, corner.Y)
                    };
                }
                else
                {
                    bypassPoints = new Point[]
                    {
                        new Point(corner.X, p1.Y),
                        new Point(corner.X, p2.Y)
                    };
                }

                if (!HasCollision(new[] { p1, bypassPoints[0], bypassPoints[1], p2 },
                    allNodeRects, Rect.Empty, targetNodeRect, excludeTargetNode: false))
                {
                    System.Diagnostics.Debug.WriteLine($"[CalculateDirectBypass] ✓ 绕过角落成功:({corner.X:F1},{corner.Y:F1})");
                    return bypassPoints;
                }
            }

            System.Diagnostics.Debug.WriteLine($"[CalculateDirectBypass] 警告: 所有避让方案都失败");
            return null;
        }

        /// <summary>
        /// 计算双点避让（用于复杂场景）
        /// </summary>
        private Point[] CalculateTwoPointAvoidance(
            Point p1, Point p2, Rect collidingRect, bool isHorizontal,
            PortDirection sourceDirection, PortDirection targetDirection,
            Rect[] allNodeRects, Rect targetNodeRect)
        {
            System.Diagnostics.Debug.WriteLine($"[CalculateTwoPointAvoidance] 开始双点避让: 线段({p1.X:F1},{p1.Y:F1})->({p2.X:F1},{p2.Y:F1}), 类型:{(isHorizontal?"水平":"垂直")}");

            Point[] avoidancePoints;

            if (isHorizontal)
            {
                // 水平线段：先避让到安全距离，再水平通过
                double safeY = targetDirection == PortDirection.Bottom
                    ? collidingRect.Top - NodeSafeDistance  // 目标在下方，从上方接近
                    : collidingRect.Bottom + NodeSafeDistance; // 目标在上方，从下方接近

                avoidancePoints = new Point[]
                {
                    new Point(p1.X, safeY),      // 第一个拐点
                    new Point(p2.X, safeY)       // 第二个拐点
                };
                System.Diagnostics.Debug.WriteLine($"[CalculateTwoPointAvoidance] 候选方案1(优先): ({avoidancePoints[0].X:F1},{avoidancePoints[0].Y:F1}) -> ({avoidancePoints[1].X:F1},{avoidancePoints[1].Y:F1})");
            }
            else
            {
                // 垂直线段：先避让到安全距离，再垂直通过
                double safeX = targetDirection == PortDirection.Right
                    ? collidingRect.Left - NodeSafeDistance  // 目标在右方，从左方接近
                    : collidingRect.Right + NodeSafeDistance; // 目标在左方，从右方接近

                avoidancePoints = new Point[]
                {
                    new Point(safeX, p1.Y),      // 第一个拐点
                    new Point(safeX, p2.Y)       // 第二个拐点
                };
                System.Diagnostics.Debug.WriteLine($"[CalculateTwoPointAvoidance] 候选方案1(优先): ({avoidancePoints[0].X:F1},{avoidancePoints[0].Y:F1}) -> ({avoidancePoints[1].X:F1},{avoidancePoints[1].Y:F1})");
            }

            // 测试避让路径是否有效
            if (!HasCollision(new[] { p1, avoidancePoints[0], avoidancePoints[1], p2 },
                allNodeRects, Rect.Empty, targetNodeRect, excludeTargetNode: false))
            {
                System.Diagnostics.Debug.WriteLine($"[CalculateTwoPointAvoidance] ✓ 方案1成功");
                return avoidancePoints;
            }

            // 双点避让失败，尝试另一个方向
            System.Diagnostics.Debug.WriteLine($"[CalculateTwoPointAvoidance] 方案1失败，尝试相反方向");

            if (isHorizontal)
            {
                double safeY = targetDirection == PortDirection.Bottom
                    ? collidingRect.Bottom + NodeSafeDistance
                    : collidingRect.Top - NodeSafeDistance;

                avoidancePoints = new Point[]
                {
                    new Point(p1.X, safeY),
                    new Point(p2.X, safeY)
                };
                System.Diagnostics.Debug.WriteLine($"[CalculateTwoPointAvoidance] 候选方案2: ({avoidancePoints[0].X:F1},{avoidancePoints[0].Y:F1}) -> ({avoidancePoints[1].X:F1},{avoidancePoints[1].Y:F1})");
            }
            else
            {
                double safeX = targetDirection == PortDirection.Right
                    ? collidingRect.Right + NodeSafeDistance
                    : collidingRect.Left - NodeSafeDistance;

                avoidancePoints = new Point[]
                {
                    new Point(safeX, p1.Y),
                    new Point(safeX, p2.Y)
                };
                System.Diagnostics.Debug.WriteLine($"[CalculateTwoPointAvoidance] 候选方案2: ({avoidancePoints[0].X:F1},{avoidancePoints[0].Y:F1}) -> ({avoidancePoints[1].X:F1},{avoidancePoints[1].Y:F1})");
            }

            if (!HasCollision(new[] { p1, avoidancePoints[0], avoidancePoints[1], p2 },
                allNodeRects, Rect.Empty, targetNodeRect, excludeTargetNode: false))
            {
                System.Diagnostics.Debug.WriteLine($"[CalculateTwoPointAvoidance] ✓ 方案2成功");
                return avoidancePoints;
            }

            System.Diagnostics.Debug.WriteLine($"[CalculateTwoPointAvoidance] 警告: 双点避让也失败");
            return null;
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
