using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using SunEyeVision.UI.Models;

namespace SunEyeVision.UI.Converters
{
    /// <summary>
    /// 璺緞楠岃瘉鍣?- 涓ユ牸楠岃瘉璺緞涓嶇┛杩囦换浣曡妭鐐?
    /// </summary>
    public class PathValidator
    {
        private readonly PathConfiguration _config;
        private readonly ObstacleDetector _obstacleDetector;

        public PathValidator(PathConfiguration config)
        {
            _config = config ?? new PathConfiguration();
            _obstacleDetector = new ObstacleDetector(config);
        }

        /// <summary>
        /// 楠岃瘉璺緞鏄惁鏈夋晥(涓嶇┛杩囦换浣曡妭鐐?
        /// </summary>
        public ValidationResult ValidatePath(
            List<Point> pathPoints,
            PathContext context,
            bool includeSourceAndTarget = true)
        {
            var result = new ValidationResult
            {
                IsValid = true,
                Violations = new List<PathViolation>()
            };

            if (pathPoints == null || pathPoints.Count == 0)
            {
                result.IsValid = false;
                result.Violations.Add(new PathViolation
                {
                    ViolationType = ViolationType.EmptyPath,
                    Description = "璺緞涓虹┖",
                    AffectedSegment = -1
                });
                return result;
            }

            // 鏋勫缓瀹屾暣鐨勮矾寰?鍖呮嫭璧风偣鍜岀粓鐐?
            var fullPath = new List<Point>();
            fullPath.Add(context.StartPoint);
            fullPath.AddRange(pathPoints);
            fullPath.Add(context.ArrowTailPoint);

            // 鑾峰彇鎵€鏈夎妫€鏌ョ殑鑺傜偣
            var nodesToCheck = new List<WorkflowNode>();
            if (includeSourceAndTarget)
            {
                nodesToCheck.Add(context.SourceNode);
                nodesToCheck.Add(context.TargetNode);
            }
            if (context.Obstacles != null)
            {
                nodesToCheck.AddRange(context.Obstacles);
            }

            // 妫€鏌ユ瘡涓€娈佃矾寰?
            for (int i = 0; i < fullPath.Count - 1; i++)
            {
                Point p1 = fullPath[i];
                Point p2 = fullPath[i + 1];

                // 检查这一段是否穿过任何节点
                foreach (var node in nodesToCheck)
                {
                    // 对于源节点和目标节点，不使用扩展边界（避免误判）
                    bool isSourceOrTarget = node.Id == context.SourceNode.Id || node.Id == context.TargetNode.Id;
                    var violation = CheckSegmentAgainstNode(p1, p2, node, i, useExpandedBounds: !isSourceOrTarget);
                    if (violation != null)
                    {
                        result.IsValid = false;
                        result.Violations.Add(violation);
                    }
                }
            }

            // 检查路径点是否在节点内部
            // 注意：最后一个点（箭头尾部）是否在目标节点内部已在连接箭头内部处理
            // 注意：起点必须在源节点上，所以跳过源节点和目标节点的检查
            for (int i = 0; i < fullPath.Count - 1; i++)
            {
                Point point = fullPath[i];

                foreach (var node in nodesToCheck)
                {
                    // 跳过源节点和目标节点的检查（起点必须在源节点上）
                    bool isSourceOrTarget = node.Id == context.SourceNode.Id || node.Id == context.TargetNode.Id;
                    if (isSourceOrTarget)
                    {
                        continue;
                    }

                    if (IsPointInsideNode(point, node))
                    {
                        result.IsValid = false;
                        result.Violations.Add(new PathViolation
                        {
                            ViolationType = ViolationType.PointInsideNode,
                            Description = $"路径点{i} 在节点{node.Id} 内部",
                            AffectedNode = node.Id,
                            AffectedPoint = point
                        });
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// 妫€鏌ョ嚎娈垫槸鍚︾┛杩囪妭鐐?
        /// </summary>
        private PathViolation CheckSegmentAgainstNode(Point p1, Point p2, WorkflowNode node, int segmentIndex, bool useExpandedBounds = true)
        {
            Rect nodeBounds = GetNodeBounds(node);
            Rect checkBounds = nodeBounds;

            // 如果需要使用扩展边界（用于障碍节点）
            if (useExpandedBounds)
            {
                // 扩展节点的边界（确保路径有足够的间距）
                double expandedMargin = _config.PathOffset + 10; // 增加扩展量
                checkBounds = new Rect(
                    nodeBounds.Left - expandedMargin,
                    nodeBounds.Top - expandedMargin,
                    nodeBounds.Width + 2 * expandedMargin,
                    nodeBounds.Height + 2 * expandedMargin
                );
            }

            // 检查线段是否与节点边界相交
            if (LineSegmentIntersectsRect(p1, p2, checkBounds))
            {
                // 对于不使用扩展边界的情况（源节点和目标节点），需要特殊处理
                // 如果线段刚好在节点边界上（起点或终点在边界上），不应该被判定为穿过
                if (!useExpandedBounds)
                {
                    // 检查是否起点或终点在节点边界上
                    bool p1OnBorder = IsPointOnNodeBorder(p1, nodeBounds);
                    bool p2OnBorder = IsPointOnNodeBorder(p2, nodeBounds);

                    // 如果一个点在边界上，另一个点在边界外，这是正常的离开/进入节点的线段
                    if ((p1OnBorder && !nodeBounds.Contains(p2)) ||
                        (p2OnBorder && !nodeBounds.Contains(p1)))
                    {
                        return null; // 不算违规
                    }
                }

                return new PathViolation
                {
                    ViolationType = ViolationType.SegmentCrossesNode,
                    Description = $"璺緞娈?{segmentIndex} 绌胯繃鑺傜偣 {node.Id}",
                    AffectedNode = node.Id,
                    AffectedSegment = segmentIndex,
                    SegmentStart = p1,
                    SegmentEnd = p2
                };
            }

            return null;
        }

        /// <summary>
        /// 检查点是否在节点边界上（包括边界上的点）
        /// </summary>
        private bool IsPointOnNodeBorder(Point point, Rect nodeBounds)
        {
            const double epsilon = 0.1; // 允许小的浮点误差
            bool onLeftOrRight = Math.Abs(point.X - nodeBounds.Left) < epsilon || Math.Abs(point.X - nodeBounds.Right) < epsilon;
            bool onTopOrBottom = Math.Abs(point.Y - nodeBounds.Top) < epsilon || Math.Abs(point.Y - nodeBounds.Bottom) < epsilon;

            return onLeftOrRight || onTopOrBottom;
        }

        /// <summary>
        /// 妫€鏌ョ偣鏄惁鍦ㄨ妭鐐瑰唴閮?
        /// </summary>
        private bool IsPointInsideNode(Point point, WorkflowNode node)
        {
            Rect nodeBounds = GetNodeBounds(node);
            return nodeBounds.Contains(point);
        }

        /// <summary>
        /// 妫€鏌ョ嚎娈垫槸鍚︿笌鐭╁舰鐩镐氦
        /// </summary>
        private bool LineSegmentIntersectsRect(Point p1, Point p2, Rect rect)
        {
            // 蹇揃獙鐣屾嵅鏌?
            if (!rect.Contains(p1) && !rect.Contains(p2))
            {
                double minX = Math.Min(p1.X, p2.X);
                double maxX = Math.Max(p1.X, p2.X);
                double minY = Math.Min(p1.Y, p2.Y);
                double maxY = Math.Max(p1.Y, p2.Y);

                if (maxX < rect.Left || minX > rect.Right ||
                    maxY < rect.Top || minY > rect.Bottom)
                {
                    return false;
                }
            }

            // 妫€鏌ュ洓涓繙鐐?
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

            // 妫€鏌ョ嚎娈电鐐规槸鍚﹀湪鐭╁舰鍐?
            if (rect.Contains(p1) || rect.Contains(p2))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 妫€鏌ヤ袱鏉＄嚎娈垫槸鍚︾浉浜?
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

        /// <summary>
        /// 鑾峰彇鑺傜偣鐨勮竟鐣?
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
        /// 蹇揃獙璇佽矾寰?(鍙鏌ュ叧閿偣)
        /// </summary>
        public bool QuickValidate(List<Point> pathPoints, PathContext context)
        {
            var result = ValidatePath(pathPoints, context, includeSourceAndTarget: true);
            
            // 添加调试信息
            if (!result.IsValid)
            {
                System.Diagnostics.Debug.WriteLine("[PathValidator] ========== 验证失败原因 ==========");
                foreach (var violation in result.Violations)
                {
                    System.Diagnostics.Debug.WriteLine($"[PathValidator] - {violation.ViolationType}: {violation.Description}");
                    if (violation.ViolationType == ViolationType.SegmentCrossesNode)
                    {
                        System.Diagnostics.Debug.WriteLine($"[PathValidator]   线段: {violation.SegmentStart} -> {violation.SegmentEnd}");
                        System.Diagnostics.Debug.WriteLine($"[PathValidator]   节点ID: {violation.AffectedNode}");
                    }
                }
                System.Diagnostics.Debug.WriteLine("[PathValidator] ========================================");
            }
            
            return result.IsValid;
        }

        /// <summary>
        /// 鑾峰彇璺緞楠岃瘉鎶ュ憡
        /// </summary>
        public string GetValidationReport(ValidationResult result)
        {
            if (result.IsValid)
            {
                return "璺緞楠岃瘉閫氳繃";
            }

            var report = $"璺緞楠岃瘉澶辫触,鍙戠幇 {result.Violations.Count} 涓繚瑙?\n";
            foreach (var violation in result.Violations)
            {
                report += $"  - [{violation.ViolationType}] {violation.Description}\n";
            }

            return report;
        }
    }

    /// <summary>
    /// 璺緞楠岃瘉缁撴灉
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<PathViolation> Violations { get; set; } = new List<PathViolation>();
    }

    /// <summary>
    /// 璺緞楠岃瘉淇℃伅
    /// </summary>
    public class PathViolation
    {
        public ViolationType ViolationType { get; set; }
        public string Description { get; set; }
        public string AffectedNode { get; set; }
        public int AffectedSegment { get; set; }
        public Point AffectedPoint { get; set; }
        public Point SegmentStart { get; set; }
        public Point SegmentEnd { get; set; }
    }

    /// <summary>
    /// 杩濊鐢绫诲瀷鏋氫妇
    /// </summary>
    public enum ViolationType
    {
        EmptyPath,           // 绌鸿矾寰?
        SegmentCrossesNode,  // 璺緞娈电┛杩囪妭鐐?
        PointInsideNode,     // 璺緞鐐瑰湪鑺傜偣鍐呴儴
        PathTooClose,        // 璺緞澶帴杩戣妭鐐?
        SelfIntersection     // 璺緞鑷浉浜?
    }
}
