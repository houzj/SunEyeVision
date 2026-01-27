using System.Windows;
using SunEyeVision.UI.Models;

namespace SunEyeVision.UI
{
    /// <summary>
    /// 画布辅助类 - 提供常用的辅助方法
    /// </summary>
    public static class CanvasHelper
    {
        #region 端口位置计算

        /// <summary>
        /// 获取指定端口的位置
        /// </summary>
        /// <param name="node">工作流节点</param>
        /// <param name="portName">端口名称</param>
        /// <returns>端口位置</returns>
        public static Point GetPortPosition(WorkflowNode node, string portName)
        {
            return portName switch
            {
                "TopPort" => node.TopPortPosition,
                "BottomPort" => node.BottomPortPosition,
                "LeftPort" => node.LeftPortPosition,
                "RightPort" => node.RightPortPosition,
                _ => node.RightPortPosition
            };
        }

        /// <summary>
        /// 获取所有端口的位置
        /// </summary>
        /// <param name="node">工作流节点</param>
        /// <returns>端口名称到位置的字典</returns>
        public static System.Collections.Generic.Dictionary<string, Point> GetAllPortPositions(WorkflowNode node)
        {
            return new System.Collections.Generic.Dictionary<string, Point>
            {
                { "TopPort", node.TopPortPosition },
                { "BottomPort", node.BottomPortPosition },
                { "LeftPort", node.LeftPortPosition },
                { "RightPort", node.RightPortPosition }
            };
        }

        /// <summary>
        /// 检查点是否在端口范围内
        /// </summary>
        /// <param name="point">测试点</param>
        /// <param name="portPosition">端口位置</param>
        /// <param name="hitDistance">命中距离</param>
        /// <returns>是否命中</returns>
        public static bool IsPointInPort(Point point, Point portPosition, double hitDistance = CanvasConfig.PortHitTestDistance)
        {
            double dx = point.X - portPosition.X;
            double dy = point.Y - portPosition.Y;
            double distance = Math.Sqrt(dx * dx + dy * dy);
            return distance <= hitDistance;
        }

        #endregion

        #region 连接验证

        /// <summary>
        /// 验证是否可以创建连接
        /// </summary>
        /// <param name="sourceNode">源节点</param>
        /// <param name="targetNode">目标节点</param>
        /// <param name="existingConnections">现有连接列表</param>
        /// <returns>验证结果和错误消息</returns>
        public static (bool IsValid, string ErrorMessage) ValidateConnection(
            WorkflowNode sourceNode,
            WorkflowNode targetNode,
            System.Collections.Generic.IList<WorkflowConnection> existingConnections)
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
        /// 检查是否存在循环依赖
        /// </summary>
        /// <param name="sourceNodeId">源节点ID</param>
        /// <param name="targetNodeId">目标节点ID</param>
        /// <param name="connections">所有连接</param>
        /// <returns>是否存在循环</returns>
        public static bool HasCycle(
            string sourceNodeId,
            string targetNodeId,
            System.Collections.Generic.IList<WorkflowConnection> connections)
        {
            if (sourceNodeId == targetNodeId)
            {
                return true;
            }

            var visited = new System.Collections.Generic.HashSet<string>();
            return HasCycleRecursive(targetNodeId, sourceNodeId, connections, visited);
        }

        private static bool HasCycleRecursive(
            string currentId,
            string targetId,
            System.Collections.Generic.IList<WorkflowConnection> connections,
            System.Collections.Generic.HashSet<string> visited)
        {
            if (currentId == targetId)
            {
                return true;
            }

            if (visited.Contains(currentId))
            {
                return false;
            }

            visited.Add(currentId);

            var outgoingConnections = connections.Where(c => c.SourceNodeId == currentId);
            foreach (var connection in outgoingConnections)
            {
                if (HasCycleRecursive(connection.TargetNodeId, targetId, connections, visited))
                {
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region 节点操作

        /// <summary>
        /// 获取选中的节点
        /// </summary>
        /// <param name="nodes">节点集合</param>
        /// <returns>选中的节点列表</returns>
        public static System.Collections.Generic.List<WorkflowNode> GetSelectedNodes(
            System.Collections.Generic.IList<WorkflowNode> nodes)
        {
            return nodes.Where(n => n.IsSelected).ToList();
        }

        /// <summary>
        /// 清除所有节点的选中状态
        /// </summary>
        /// <param name="nodes">节点集合</param>
        public static void ClearSelection(System.Collections.Generic.IList<WorkflowNode> nodes)
        {
            foreach (var node in nodes)
            {
                node.IsSelected = false;
            }
        }

        /// <summary>
        /// 选择多个节点
        /// </summary>
        /// <param name="nodes">节点集合</param>
        /// <param name="nodeIds">要选中的节点ID列表</param>
        public static void SelectNodes(
            System.Collections.Generic.IList<WorkflowNode> nodes,
            System.Collections.Generic.IList<string> nodeIds)
        {
            foreach (var node in nodes)
            {
                node.IsSelected = nodeIds.Contains(node.Id);
            }
        }

        /// <summary>
        /// 获取节点的边界矩形
        /// </summary>
        /// <param name="node">工作流节点</param>
        /// <returns>边界矩形</returns>
        public static Rect GetNodeBounds(WorkflowNode node)
        {
            return new Rect(
                node.Position.X,
                node.Position.Y,
                CanvasConfig.NodeWidth,
                CanvasConfig.NodeHeight
            );
        }

        /// <summary>
        /// 获取多个节点的边界矩形
        /// </summary>
        /// <param name="nodes">节点列表</param>
        /// <returns>边界矩形</returns>
        public static Rect GetNodesBounds(System.Collections.Generic.IList<WorkflowNode> nodes)
        {
            if (nodes == null || nodes.Count == 0)
            {
                return Rect.Empty;
            }

            var firstBounds = GetNodeBounds(nodes[0]);
            double minX = firstBounds.Left;
            double minY = firstBounds.Top;
            double maxX = firstBounds.Right;
            double maxY = firstBounds.Bottom;

            foreach (var node in nodes)
            {
                var bounds = GetNodeBounds(node);
                minX = Math.Min(minX, bounds.Left);
                minY = Math.Min(minY, bounds.Top);
                maxX = Math.Max(maxX, bounds.Right);
                maxY = Math.Max(maxY, bounds.Bottom);
            }

            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }

        #endregion

        #region 对齐和吸附

        /// <summary>
        /// 将点吸附到网格
        /// </summary>
        /// <param name="point">原始点</param>
        /// <param name="gridSize">网格大小</param>
        /// <returns>吸附后的点</returns>
        public static Point SnapToGrid(Point point, double gridSize = CanvasConfig.GridSize)
        {
            double x = Math.Round(point.X / gridSize) * gridSize;
            double y = Math.Round(point.Y / gridSize) * gridSize;
            return new Point(x, y);
        }

        /// <summary>
        /// 将点吸附到最近的节点位置
        /// </summary>
        /// <param name="point">原始点</param>
        /// <param name="nodes">节点列表</param>
        /// <param name="snapDistance">吸附距离</param>
        /// <returns>吸附后的点</returns>
        public static Point SnapToNodes(
            Point point,
            System.Collections.Generic.IList<WorkflowNode> nodes,
            double snapDistance = CanvasConfig.SnapDistance)
        {
            Point result = point;

            foreach (var node in nodes)
            {
                var center = CanvasHelper.GetNodeCenter(node.Position);

                if (Math.Abs(point.X - node.Position.X) < snapDistance)
                {
                    result = new Point(node.Position.X, result.Y);
                }

                if (Math.Abs(point.Y - node.Position.Y) < snapDistance)
                {
                    result = new Point(result.X, node.Position.Y);
                }

                if (Math.Abs(point.X - center.X) < snapDistance)
                {
                    result = new Point(center.X, result.Y);
                }

                if (Math.Abs(point.Y - center.Y) < snapDistance)
                {
                    result = new Point(result.X, center.Y);
                }
            }

            return result;
        }

        /// <summary>
        /// 对齐选中的节点到网格
        /// </summary>
        /// <param name="nodes">节点列表</param>
        public static void AlignNodesToGrid(System.Collections.Generic.IList<WorkflowNode> nodes)
        {
            foreach (var node in nodes)
            {
                if (node.IsSelected)
                {
                    node.Position = SnapToGrid(node.Position);
                }
            }
        }

        /// <summary>
        /// 水平对齐选中的节点
        /// </summary>
        /// <param name="nodes">节点列表</param>
        public static void AlignNodesHorizontally(System.Collections.Generic.IList<WorkflowNode> nodes)
        {
            var selectedNodes = GetSelectedNodes(nodes);
            if (selectedNodes.Count < 2)
            {
                return;
            }

            var firstNode = selectedNodes[0];
            var targetY = firstNode.Position.Y;

            foreach (var node in selectedNodes.Skip(1))
            {
                node.Position = new Point(node.Position.X, targetY);
            }
        }

        /// <summary>
        /// 垂直对齐选中的节点
        /// </summary>
        /// <param name="nodes">节点列表</param>
        public static void AlignNodesVertically(System.Collections.Generic.IList<WorkflowNode> nodes)
        {
            var selectedNodes = GetSelectedNodes(nodes);
            if (selectedNodes.Count < 2)
            {
                return;
            }

            var firstNode = selectedNodes[0];
            var targetX = firstNode.Position.X;

            foreach (var node in selectedNodes.Skip(1))
            {
                node.Position = new Point(targetX, node.Position.Y);
            }
        }

        #endregion

        #region 几何计算

        /// <summary>
        /// 计算两点之间的距离
        /// </summary>
        /// <param name="p1">点1</param>
        /// <param name="p2">点2</param>
        /// <returns>距离</returns>
        public static double GetDistance(Point p1, Point p2)
        {
            double dx = p1.X - p2.X;
            double dy = p1.Y - p2.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>
        /// 计算两点之间的中点
        /// </summary>
        /// <param name="p1">点1</param>
        /// <param name="p2">点2</param>
        /// <returns>中点</returns>
        public static Point GetMidPoint(Point p1, Point p2)
        {
            return new Point(
                (p1.X + p2.X) / 2,
                (p1.Y + p2.Y) / 2
            );
        }

        /// <summary>
        /// 获取节点中心点
        /// </summary>
        /// <param name="nodePosition">节点位置</param>
        /// <returns>中心点</returns>
        public static Point GetNodeCenter(Point nodePosition)
        {
            return new Point(
                nodePosition.X + CanvasConfig.NodeWidth / 2,
                nodePosition.Y + CanvasConfig.NodeHeight / 2
            );
        }

        /// <summary>
        /// 计算向量的长度
        /// </summary>
        /// <param name="vector">向量</param>
        /// <returns>长度</returns>
        public static double GetVectorLength(Vector vector)
        {
            return Math.Sqrt(vector.X * vector.X + vector.Y * vector.Y);
        }

        /// <summary>
        /// 归一化向量
        /// </summary>
        /// <param name="vector">向量</param>
        /// <returns>归一化后的向量</returns>
        public static Vector NormalizeVector(Vector vector)
        {
            double length = GetVectorLength(vector);
            if (length == 0)
            {
                return new Vector(0, 0);
            }

            return new Vector(vector.X / length, vector.Y / length);
        }

        #endregion

        #region 命中测试

        /// <summary>
        /// 检查点是否在矩形内
        /// </summary>
        /// <param name="point">测试点</param>
        /// <param name="rect">矩形</param>
        /// <returns>是否在矩形内</returns>
        public static bool IsPointInRect(Point point, Rect rect)
        {
            return point.X >= rect.Left &&
                   point.X <= rect.Right &&
                   point.Y >= rect.Top &&
                   point.Y <= rect.Bottom;
        }

        /// <summary>
        /// 检查点是否在节点内
        /// </summary>
        /// <param name="point">测试点</param>
        /// <param name="node">节点</param>
        /// <returns>是否在节点内</returns>
        public static bool IsPointInNode(Point point, WorkflowNode node)
        {
            return IsPointInRect(point, GetNodeBounds(node));
        }

        /// <summary>
        /// 查找包含指定点的节点
        /// </summary>
        /// <param name="point">测试点</param>
        /// <param name="nodes">节点列表</param>
        /// <returns>命中的节点，如果没有则返回null</returns>
        public static WorkflowNode FindNodeAtPoint(
            Point point,
            System.Collections.Generic.IList<WorkflowNode> nodes)
        {
            return nodes.FirstOrDefault(n => IsPointInNode(point, n));
        }

        /// <summary>
        /// 查找包含指定点的所有节点
        /// </summary>
        /// <param name="point">测试点</param>
        /// <param name="nodes">节点列表</param>
        /// <returns>命中的节点列表</returns>
        public static System.Collections.Generic.List<WorkflowNode> FindNodesAtPoint(
            Point point,
            System.Collections.Generic.IList<WorkflowNode> nodes)
        {
            return nodes.Where(n => IsPointInNode(point, n)).ToList();
        }

        #endregion
    }
}
