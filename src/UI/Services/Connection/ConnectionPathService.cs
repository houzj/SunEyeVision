using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using SunEyeVision.UI.Models;
using SunEyeVision.UI.Services.Connection;

namespace SunEyeVision.UI.Services.Connection
{
    /// <summary>
    /// 连接路径服务接口
    /// </summary>
    public interface IConnectionPathService
    {
        /// <summary>
        /// 计算两点之间的路�?
        /// </summary>
        string CalculatePath(Point start, Point end);

        /// <summary>
        /// 计算智能路径（带拐点�?
        /// </summary>
        string CalculateSmartPath(Point start, Point end);

        /// <summary>
        /// 更新连接路径
        /// </summary>
        void UpdateConnectionPath(WorkflowConnection connection);

        /// <summary>
        /// 更新所有连接路�?
        /// </summary>
        void UpdateAllConnections(IEnumerable<WorkflowConnection> connections);

        /// <summary>
        /// 标记连接为脏（需要重新计算）
        /// </summary>
        void MarkConnectionDirty(WorkflowConnection connection);

        /// <summary>
        /// 标记节点相关的所有连接为�?
        /// </summary>
        void MarkNodeConnectionsDirty(string nodeId);

        /// <summary>
        /// 清除路径缓存
        /// </summary>
        void ClearCache();

        /// <summary>
        /// 获取缓存统计信息
        /// </summary>
        CacheStatistics GetStatistics();
    }


    /// <summary>
    /// 连接路径服务 - 管理连接线路径的计算和更�?
    /// </summary>
    public class ConnectionPathService : IConnectionPathService
    {
        private readonly ConnectionPathCache _pathCache;
        private readonly ObservableCollection<WorkflowNode> _nodes;

        /// <summary>
        /// 路径计算策略模式
        /// </summary>
        public enum PathStrategy
        {
            /// <summary>
            /// 水平优先
            /// </summary>
            HorizontalFirst,

            /// <summary>
            /// 垂直优先
            /// </summary>
            VerticalFirst,

            /// <summary>
            /// 自适应（根据偏移量自动选择�?
            /// </summary>
            Adaptive
        }

        /// <summary>
        /// 当前路径计算策略
        /// </summary>
        public PathStrategy Strategy { get; set; } = PathStrategy.Adaptive;

        public ConnectionPathService(ConnectionPathCache pathCache, ObservableCollection<WorkflowNode> nodes)
        {
            _pathCache = pathCache ?? throw new ArgumentNullException(nameof(pathCache));
            _nodes = nodes ?? throw new ArgumentNullException(nameof(nodes));
        }

        public string CalculatePath(Point start, Point end)
        {
            return CalculateSmartPath(start, end);
        }

        public string CalculateSmartPath(Point start, Point end)
        {
            var deltaX = end.X - start.X;
            var deltaY = end.Y - start.Y;

            // 根据策略选择路径计算方式
            bool useHorizontal = Strategy switch
            {
                PathStrategy.HorizontalFirst => true,
                PathStrategy.VerticalFirst => false,
                PathStrategy.Adaptive => Math.Abs(deltaX) > Math.Abs(deltaY),
                _ => true
            };

            if (useHorizontal)
            {
                // 水平优先策略
                var midX = start.X + deltaX / 2;
                return $"M {start.X:F1},{start.Y:F1} L {midX:F1},{start.Y:F1} L {midX:F1},{end.Y:F1} L {end.X:F1},{end.Y:F1}";
            }
            else
            {
                // 垂直优先策略
                var midY = start.Y + deltaY / 2;
                return $"M {start.X:F1},{start.Y:F1} L {start.X:F1},{midY:F1} L {end.X:F1},{midY:F1} L {end.X:F1},{end.Y:F1}";
            }
        }

        public void UpdateConnectionPath(WorkflowConnection connection)
        {
            if (connection == null)
            {
                return;
            }

            // 标记为脏，下次访问时重新计算
            _pathCache.MarkDirty(connection);

            // 立即更新（如果需要）
            var pathData = _pathCache.GetPathData(connection);
            if (pathData != null)
            {
                connection.PathData = pathData;
                UpdateArrowPosition(connection);
                UpdateConnectionPoints(connection);
            }
        }

        public void UpdateAllConnections(IEnumerable<WorkflowConnection> connections)
        {
            if (connections == null)
            {
                return;
            }

            foreach (var connection in connections)
            {
                UpdateConnectionPath(connection);
            }
        }

        public void MarkConnectionDirty(WorkflowConnection connection)
        {
            if (connection == null)
            {
                return;
            }

            _pathCache.MarkDirty(connection);
        }

        public void MarkNodeConnectionsDirty(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId))
            {
                return;
            }

            _pathCache.MarkNodeDirty(nodeId);
        }

        public void ClearCache()
        {
            _pathCache.Clear();
        }

        public CacheStatistics GetStatistics()
        {
            return _pathCache.GetStatistics();
        }

        /// <summary>
        /// 更新箭头位置和角�?
        /// </summary>
        private void UpdateArrowPosition(WorkflowConnection connection)
        {
            if (connection == null)
            {
                return;
            }

            var points = connection.PathPoints;
            if (points.Count >= 2)
            {
                var lastPoint = points[points.Count - 1];
                var secondLastPoint = points[points.Count - 2];

                connection.ArrowPosition = lastPoint;
                connection.ArrowAngle = CalculateArrowAngle(secondLastPoint, lastPoint);
            }
        }

        /// <summary>
        /// 更新连接点列�?
        /// </summary>
        private void UpdateConnectionPoints(WorkflowConnection connection)
        {
            if (connection == null || string.IsNullOrEmpty(connection.PathData))
            {
                return;
            }

            // 解析路径数据并提取关键点
            var points = ParsePathData(connection.PathData);
            connection.PathPoints.Clear();
            foreach (var point in points)
            {
                connection.PathPoints.Add(point);
            }
        }

        /// <summary>
        /// 计算箭头角度
        /// </summary>
        private double CalculateArrowAngle(Point from, Point to)
        {
            var deltaX = to.X - from.X;
            var deltaY = to.Y - from.Y;
            var angle = Math.Atan2(deltaY, deltaX) * 180 / Math.PI;
            return angle;
        }

        /// <summary>
        /// 解析路径数据并提取关键点
        /// </summary>
        private List<Point> ParsePathData(string pathData)
        {
            var points = new List<Point>();

            if (string.IsNullOrEmpty(pathData))
            {
                return points;
            }

            try
            {
                // 简单的路径解析（实际应该使用更完善的解析器�?
                var parts = pathData.Split(new[] { 'M', 'L' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var part in parts)
                {
                    var coords = part.Trim().Split(',');
                    if (coords.Length == 2)
                    {
                        if (double.TryParse(coords[0], out var x) && double.TryParse(coords[1], out var y))
                        {
                            points.Add(new Point(x, y));
                        }
                    }
                }
            }
            catch (Exception)
            {
                // 解析失败，返回空列表
            }

            return points;
        }

    }
}
