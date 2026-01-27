using System;
using System.Collections.Generic;
using System.Windows;
using SunEyeVision.UI.Models;

namespace SunEyeVision.UI.Converters
{
    /// <summary>
    /// 鍥涘弶鏍戠┖闂寸储寮?- 鐢ㄤ簬楂樻晥鐨勯殰纰嶇墿妫€娴?
    /// 鏌ヨ澶嶆潚搴︿粠 O(n) 浼樺寲鍒?O(log n)
    /// </summary>
    public class QuadTree
    {
        internal const int MaxCapacity = 16;
        internal const int MaxDepth = 6;

        private QuadTreeNode _root;

        public QuadTree(Rect bounds)
        {
            _root = new QuadTreeNode(bounds, 0);
        }

        /// <summary>
        /// 鎻掑叆鑺傜偣
        /// </summary>
        public void Insert(WorkflowNode node)
        {
            if (node == null) return;

            Rect nodeBounds = new Rect(
                node.Position.X,
                node.Position.Y,
                140, // NodeWidth
                90   // NodeHeight
            );

            _root.Insert(node, nodeBounds);
        }

        /// <summary>
        /// </summary>
        public List<WorkflowNode> Query(Rect queryBounds)
        {
            var results = new List<WorkflowNode>();
            _root.Query(queryBounds, results);
            return results;
        }

        /// <summary>
        /// 查询与给定线段相关的所有节点
        /// </summary>
        public List<WorkflowNode> QueryLineSegment(Point start, Point end)
        {
            // 扩大查询范围以包含线段,并增加节点高度的安全边距
            double minX = Math.Min(start.X, end.X);
            double maxX = Math.Max(start.X, end.X);
            double minY = Math.Min(start.Y, end.Y);
            double maxY = Math.Max(start.Y, end.Y);

            // 扩展查询范围以包含可能在线段附近但与线段相交的节点
            // 节点高度为90px,我们添加安全边距
            double margin = 50.0; // 安全边距
            Rect queryBounds = new Rect(
                minX - margin,
                Math.Min(minY, maxY) - margin,
                (maxX - minX) + margin * 2,
                Math.Abs(maxY - minY) + margin * 2
            );

            System.Diagnostics.Debug.WriteLine($"[QuadTree] QueryLineSegment - 查询矩形: X=[{queryBounds.X}, {queryBounds.Right}], Y=[{queryBounds.Y}, {queryBounds.Bottom}], 宽={queryBounds.Width}, 高={queryBounds.Height}");

            var candidates = Query(queryBounds);
            System.Diagnostics.Debug.WriteLine($"[QuadTree] QueryLineSegment - 初步查询到 {candidates.Count} 个候选节点");

            // 精确过滤,只返回真正与线段相交的节点
            var results = new List<WorkflowNode>();
            foreach (var node in candidates)
            {
                Rect nodeBounds = new Rect(
                    node.Position.X,
                    node.Position.Y,
                    140,
                    90
                );

                System.Diagnostics.Debug.WriteLine($"[QuadTree]   候选节点: {node.Name}, 边界: X=[{nodeBounds.X}, {nodeBounds.Right}], Y=[{nodeBounds.Y}, {nodeBounds.Bottom}]");

                if (LineSegmentIntersectsRect(start, end, nodeBounds))
                {
                    results.Add(node);
                    System.Diagnostics.Debug.WriteLine($"[QuadTree]     ✓ 与线段相交");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[QuadTree]     ✗ 不与线段相交");
                }
            }

            System.Diagnostics.Debug.WriteLine($"[QuadTree] QueryLineSegment - 最终返回 {results.Count} 个节点");
            return results;
        }

        /// <summary>
        /// 娓呯┖鍥涘弶鏍?
        /// </summary>
        public void Clear()
        {
            Rect bounds = _root.Bounds;
            _root = new QuadTreeNode(bounds, 0);
        }

        /// <summary>
        /// 妫€鏌ョ嚎娈垫槸鍚︿笌鐭╁舰鐩镐氦
        /// </summary>
        private bool LineSegmentIntersectsRect(Point p1, Point p2, Rect rect)
        {
            // 蹇€熻竟鐣屾鏌?
            if (!rect.Contains(p1) && !rect.Contains(p2))
            {
                // 绠€鍗曠殑杈圭晫妗嗘鏌?
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

            // 妫€鏌ュ洓涓妭鐐?
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

            // 妫€鏌ョ嚎娈电偣鐐规槸鍚﹀湪鐭╁舰鍐?
            if (rect.Contains(p1) || rect.Contains(p2))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 妫€鏌や袱鏉″嚎娈垫槸鍚︾浉浜?
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

    /// <summary>
    /// 鍥涘弶鏍戣妭鐐?
    /// </summary>
    internal class QuadTreeNode
    {
        public Rect Bounds { get; private set; }
        public int Depth { get; private set; }
        public List<WorkflowNode> Nodes { get; private set; }
        public QuadTreeNode[] Children { get; private set; }

        public QuadTreeNode(Rect bounds, int depth)
        {
            Bounds = bounds;
            Depth = depth;
            Nodes = new List<WorkflowNode>();
            Children = null;
        }

        public void Insert(WorkflowNode node, Rect nodeBounds)
        {
            // 濡傛灉鏈夊瓙鑺傜偣锛屾彃鍏ュ埌瀛愯妭鐐逛腑
            if (Children != null)
            {
                for (int i = 0; i < 4; i++)
                {
                    if (Children[i].Bounds.IntersectsWith(nodeBounds))
                    {
                        Children[i].Insert(node, nodeBounds);
                    }
                }
                return;
            }

            // 娣诲姞鍒板綋鍓嶈妭鐐?
            Nodes.Add(node);

            // 妫€鏌ユ槸鍚﹂渶瑕佸垎瑁?
            if (Nodes.Count > QuadTree.MaxCapacity && Depth < QuadTree.MaxDepth)
            {
                Split();
            }
        }

        public void Query(Rect queryBounds, List<WorkflowNode> results)
        {
            // 濡傛灉鏌ヨ鑼冨洿涓嶄笌褰撳墠鑺傜偣鐩镐氦锛岃繑鍥?
            if (!Bounds.IntersectsWith(queryBounds))
            {
                return;
            }

            // 娣诲姞褰撳墠鑺傜偣鐨勬墍鏈夎妭鐐?
            results.AddRange(Nodes);

            // 濡傛灉鏈夊瓙鑺傜偣锛岄€掑綊鏌ヨ
            if (Children != null)
            {
                for (int i = 0; i < 4; i++)
                {
                    Children[i].Query(queryBounds, results);
                }
            }
        }

        private void Split()
        {
            double halfWidth = Bounds.Width / 2;
            double halfHeight = Bounds.Height / 2;

            Children = new QuadTreeNode[4];
            Children[0] = new QuadTreeNode(
                new Rect(Bounds.X, Bounds.Y, halfWidth, halfHeight),
                Depth + 1);
            Children[1] = new QuadTreeNode(
                new Rect(Bounds.X + halfWidth, Bounds.Y, halfWidth, halfHeight),
                Depth + 1);
            Children[2] = new QuadTreeNode(
                new Rect(Bounds.X, Bounds.Y + halfHeight, halfWidth, halfHeight),
                Depth + 1);
            Children[3] = new QuadTreeNode(
                new Rect(Bounds.X + halfWidth, Bounds.Y + halfHeight, halfWidth, halfHeight),
                Depth + 1);

            // 灏嗗綋鍓嶈妭鐐圭殑鎵€鏈夎妭鐐归噸鏂板垎閰嶅埌瀛愯妭鐐?
            foreach (var node in Nodes)
            {
                Rect nodeBounds = new Rect(
                    node.Position.X,
                    node.Position.Y,
                    140,
                    90
                );

                for (int i = 0; i < 4; i++)
                {
                    if (Children[i].Bounds.IntersectsWith(nodeBounds))
                    {
                        Children[i].Insert(node, nodeBounds);
                    }
                }
            }

            Nodes.Clear();
        }
    }
}
