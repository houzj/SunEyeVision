using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SunEyeVision.UI.Models;

namespace SunEyeVision.UI.Services.Canvas
{
    /// <summary>
    /// 画布辅助?- 提供画布相关的通用辅助方法
    /// </summary>
    public static class CanvasHelper
    {
        /// <summary>
        /// 判断点是否在端口?        /// </summary>
        /// <param name="point">要检测的?/param>
        /// <param name="portPosition">端口位置</param>
        /// <param name="hitDistance">命中距离</param>
        /// <returns>是否命中</returns>
        public static bool IsPointInPort(Point point, Point portPosition, double hitDistance = 15)
        {
            double dx = point.X - portPosition.X;
            double dy = point.Y - portPosition.Y;
            return dx * dx + dy * dy <= hitDistance * hitDistance;
        }

        /// <summary>
        /// 验证连接是否有效
        /// </summary>
        public static (bool isValid, string message) ValidateConnection(
            WorkflowNode sourceNode,
            WorkflowNode targetNode,
            IList<WorkflowConnection> existingConnections)
        {
            if (sourceNode == null)
            {
                return (false, "源节点不能为空");
            }

            if (targetNode == null)
            {
                return (false, "目标节点不能为空");
            }

            if (sourceNode.Id == targetNode.Id)
            {
                return (false, "不允许自连接");
            }

            if (existingConnections.Any(c => c.SourceNodeId == sourceNode.Id && c.TargetNodeId == targetNode.Id))
            {
                return (false, "连接已存在");
            }

            if (existingConnections.Any(c => c.TargetNodeId == sourceNode.Id && c.SourceNodeId == targetNode.Id))
            {
                return (false, "反向连接已存在");
            }

            return (true, string.Empty);
        }

        /// <summary>
        /// 将点吸附到网?        /// </summary>
        /// <param name="point">原始?/param>
        /// <param name="gridSize">网格大小</param>
        /// <returns>吸附后的?/returns>
        public static Point SnapToGrid(Point point, double gridSize = 10)
        {
            double x = Math.Round(point.X / gridSize) * gridSize;
            double y = Math.Round(point.Y / gridSize) * gridSize;
            return new Point(x, y);
        }

        /// <summary>
        /// 计算两个矩形是否相交
        /// </summary>
        public static bool RectanglesIntersect(Rect rect1, Rect rect2)
        {
            return rect1.IntersectsWith(rect2);
        }

        /// <summary>
        /// 计算节点矩形
        /// </summary>
        public static Rect GetNodeRect(WorkflowNode node)
        {
            return new Rect(node.Position.X, node.Position.Y, node.StyleConfig.NodeWidth, node.StyleConfig.NodeHeight);
        }

        /// <summary>
        /// 查找指定区域内的节点
        /// </summary>
        public static List<WorkflowNode> FindNodesInRegion(
            Point startPoint,
            Point endPoint,
            IList<WorkflowNode> nodes)
        {
            var rect = new Rect(
                Math.Min(startPoint.X, endPoint.X),
                Math.Min(startPoint.Y, endPoint.Y),
                Math.Abs(endPoint.X - startPoint.X),
                Math.Abs(endPoint.Y - startPoint.Y));

            return nodes.Where(n => rect.Contains(new Point(n.Position.X + n.StyleConfig.NodeWidth / 2, n.Position.Y + n.StyleConfig.NodeHeight / 2))).ToList();
        }

        /// <summary>
        /// 计算吸附?        /// </summary>
        public static Point FindSnapPoint(
            Point point,
            IList<WorkflowNode> nodes,
            double snapDistance = 10)
        {
            Point result = point;
            double minDistance = double.MaxValue;

            foreach (var node in nodes)
            {
                // 检查四个角
                var corners = new[]
                {
                    new Point(node.Position.X, node.Position.Y),
                    new Point(node.Position.X + node.StyleConfig.NodeWidth, node.Position.Y),
                    new Point(node.Position.X, node.Position.Y + node.StyleConfig.NodeHeight),
                    new Point(node.Position.X + node.StyleConfig.NodeWidth, node.Position.Y + node.StyleConfig.NodeHeight)
                };

                foreach (var corner in corners)
                {
                    double distance = Point.Subtract(point, corner).Length;
                    if (distance < snapDistance && distance < minDistance)
                    {
                        minDistance = distance;
                        result = corner;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// 清除所有节点的选中状?        /// </summary>
        public static void ClearSelection(IEnumerable<WorkflowNode> nodes)
        {
            if (nodes == null) return;
            foreach (var node in nodes)
            {
                node.IsSelected = false;
            }
        }

        /// <summary>
        /// 获取节点边界矩形
        /// </summary>
        public static Rect GetNodeBounds(WorkflowNode node)
        {
            if (node == null) return Rect.Empty;
            return new Rect(node.Position.X, node.Position.Y, node.StyleConfig.NodeWidth, node.StyleConfig.NodeHeight);
        }

        /// <summary>
        /// 获取端口位置
        /// </summary>
        public static Point GetPortPosition(WorkflowNode node, string portName)
        {
            if (node == null || string.IsNullOrEmpty(portName)) return new Point(0, 0);

            // 根据端口名称计算位置
            return portName switch
            {
                "LeftPort" => new Point(node.Position.X, node.Position.Y + node.StyleConfig.NodeHeight / 2),
                "RightPort" => new Point(node.Position.X + node.StyleConfig.NodeWidth, node.Position.Y + node.StyleConfig.NodeHeight / 2),
                "TopPort" => new Point(node.Position.X + node.StyleConfig.NodeWidth / 2, node.Position.Y),
                "BottomPort" => new Point(node.Position.X + node.StyleConfig.NodeWidth / 2, node.Position.Y + node.StyleConfig.NodeHeight),
                _ => new Point(node.Position.X + node.StyleConfig.NodeWidth / 2, node.Position.Y + node.StyleConfig.NodeHeight / 2)
            };
        }

        /// <summary>
        /// 获取节点所有端口位?        /// </summary>
        public static Dictionary<string, Point> GetAllPortPositions(WorkflowNode node)
        {
            if (node == null) return new Dictionary<string, Point>();

            return new Dictionary<string, Point>
            {
                { "LeftPort", new Point(node.Position.X, node.Position.Y + node.StyleConfig.NodeHeight / 2) },
                { "RightPort", new Point(node.Position.X + node.StyleConfig.NodeWidth, node.Position.Y + node.StyleConfig.NodeHeight / 2) },
                { "TopPort", new Point(node.Position.X + node.StyleConfig.NodeWidth / 2, node.Position.Y) },
                { "BottomPort", new Point(node.Position.X + node.StyleConfig.NodeWidth / 2, node.Position.Y + node.StyleConfig.NodeHeight) }
            };
        }

        /// <summary>
        /// 计算两点之间的距?        /// </summary>
        public static double GetDistance(Point p1, Point p2)
        {
            double dx = p1.X - p2.X;
            double dy = p1.Y - p2.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>
        /// 获取节点中心?        /// </summary>
        public static Point GetNodeCenter(WorkflowNode node)
        {
            if (node == null) return new Point(0, 0);
            return new Point(
                node.Position.X + node.StyleConfig.NodeWidth / 2,
                node.Position.Y + node.StyleConfig.NodeHeight / 2
            );
        }
    }
}
