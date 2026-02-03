using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using SunEyeVision.UI.Models;
using SunEyeVision.UI.Services.PathCalculators;
using SunEyeVision.UI.Services.PathCalculators.LibavoidPure;

namespace SunEyeVision.UI.Services
{
    /// <summary>
    /// 连接线路径缓存 - 避免重复计算连接线路径
    /// </summary>
    public class ConnectionPathCache
    {
        private readonly Dictionary<string, CachedPath> _pathCache;
        private readonly Dictionary<string, bool> _dirtyFlags;
        private readonly ObservableCollection<WorkflowNode> _nodes;
        private readonly object _lockObj;
        private readonly IPathCalculator _pathCalculator;

        /// <summary>
        /// 缓存命中次数
        /// </summary>
        public int CacheHits { get; private set; }

        /// <summary>
        /// 缓存未命中次数
        /// </summary>
        public int CacheMisses { get; private set; }

        /// <summary>
        /// 缓存大小
        /// </summary>
        public int CacheSize => _pathCache.Count;

        /// <summary>
        /// 缓存命中率
        /// </summary>
        public double HitRate => CacheHits + CacheMisses > 0
            ? (double)CacheHits / (CacheHits + CacheMisses)
            : 0;

        public ConnectionPathCache(ObservableCollection<WorkflowNode> nodes, IPathCalculator? pathCalculator = null)
        {
            _pathCache = [];
            _dirtyFlags = [];
            _nodes = nodes;
            _lockObj = new object();
            _pathCalculator = pathCalculator ?? new LibavoidPurePathCalculator();

            SubscribeToNodes();
        }

        /// <summary>
        /// 获取连接线路径
        /// </summary>
        public PathGeometry? GetPath(WorkflowConnection connection)
        {
            lock (_lockObj)
            {
                // 🔥 减少日志输出以提高性能
                // System.Diagnostics.Debug.WriteLine($"[PathCache] GetPath called for connection: {connection.Id}");
                // System.Diagnostics.Debug.WriteLine($"[PathCache]   Cache size: {_pathCache.Count}, Dirty flags: {_dirtyFlags.Count}");

                if (_pathCache.TryGetValue(connection.Id, out var cachedPath))
                {
                    // System.Diagnostics.Debug.WriteLine($"[PathCache]   Found in cache. Checking dirty flag...");
                    if (_dirtyFlags.TryGetValue(connection.Id, out bool isDirty) && isDirty)
                    {
                        // System.Diagnostics.Debug.WriteLine($"[PathCache]   Cache is DIRTY, recalculating...");
                        var path = CalculatePath(connection);
                        UpdateCache(connection.Id, path);
                        CacheMisses++;
                        return path;
                    }
                    // else
                    // {
                    //     System.Diagnostics.Debug.WriteLine($"[PathCache]   Cache is CLEAN, returning cached. isDirty={isDirty}");
                    // }

                    CacheHits++;
                    return cachedPath.Geometry;
                }

                // System.Diagnostics.Debug.WriteLine($"[PathCache]   Not in cache, calculating...");
                var newPath = CalculatePath(connection);
                UpdateCache(connection.Id, newPath);
                CacheMisses++;
                return newPath;
            }
        }

        /// <summary>
        /// 获取连接线路径数据（字符串）
        /// </summary>
        public string? GetPathData(WorkflowConnection connection)
        {
            var geometry = GetPath(connection);
            return geometry?.ToString();
        }

        /// <summary>
        /// 标记连接为脏（需要重新计算）
        /// </summary>
        public void MarkDirty(WorkflowConnection connection)
        {
            lock (_lockObj)
            {
                // 🔥 减少日志输出以提高性能
                // System.Diagnostics.Debug.WriteLine($"[PathCache] MarkDirty called for connection: {connection.Id}");
                _dirtyFlags[connection.Id] = true;
                // System.Diagnostics.Debug.WriteLine($"[PathCache]   Dirty flag set. Total dirty flags: {_dirtyFlags.Count}");
            }
        }

        /// <summary>
        /// 标记所有连接为脏
        /// </summary>
        public void MarkAllDirty()
        {
            lock (_lockObj)
            {
                foreach (var key in _dirtyFlags.Keys)
                {
                    _dirtyFlags[key] = true;
                }
            }
        }

        /// <summary>
        /// 标记节点相关的所有连接为脏
        /// </summary>
        public void MarkNodeDirty(string nodeId)
        {
            lock (_lockObj)
            {
                // 🔥 减少日志输出以提高性能
                // System.Diagnostics.Debug.WriteLine($"[PathCache] MarkNodeDirty called for node: {nodeId}");
                // int markedCount = 0;
                foreach (var kvp in _pathCache)
                {
                    if (kvp.Value.SourceNodeId == nodeId || kvp.Value.TargetNodeId == nodeId)
                    {
                        _dirtyFlags[kvp.Key] = true;
                        // markedCount++;
                        // System.Diagnostics.Debug.WriteLine($"[PathCache]   Marked connection {kvp.Key} as dirty");
                    }
                }
                // System.Diagnostics.Debug.WriteLine($"[PathCache]   Marked {markedCount} connections as dirty");
            }
        }

        /// <summary>
        /// 清除缓存
        /// </summary>
        public void Clear()
        {
            lock (_lockObj)
            {
                _pathCache.Clear();
                _dirtyFlags.Clear();
                CacheHits = 0;
                CacheMisses = 0;
            }
        }

        /// <summary>
        /// 移除连接的缓存
        /// </summary>
        public void Remove(string connectionId)
        {
            lock (_lockObj)
            {
                _pathCache.Remove(connectionId);
                _dirtyFlags.Remove(connectionId);
            }
        }

        /// <summary>
        /// 预热缓存（预先计算所有连接）
        /// </summary>
        public void WarmUp(IEnumerable<WorkflowConnection> connections)
        {
            lock (_lockObj)
            {
                int warmedCount = 0;
                foreach (var connection in connections)
                {
                    if (!_pathCache.ContainsKey(connection.Id))
                    {
                        var path = CalculatePath(connection);
                        UpdateCache(connection.Id, path);
                        warmedCount++;
                    }
                }
                System.Diagnostics.Debug.WriteLine($"[PathCache] 缓存预热完成: 预热{warmedCount}个连接");
            }
        }

        /// <summary>
        /// 获取缓存统计信息
        /// </summary>
        public CacheStatistics GetStatistics()
        {
            lock (_lockObj)
            {
                return new CacheStatistics
                {
                    CacheSize = _pathCache.Count,
                    CacheHits = CacheHits,
                    CacheMisses = CacheMisses,
                    HitRate = HitRate
                };
            }
        }

        private PathGeometry CalculatePath(WorkflowConnection connection)
        {
            var sourceNode = _nodes.FirstOrDefault(n => n.Id == connection.SourceNodeId);
            var targetNode = _nodes.FirstOrDefault(n => n.Id == connection.TargetNodeId);

            if (sourceNode == null || targetNode == null)
                return new PathGeometry();

            // 根据端口名称获取端口方向和位置
            var sourceDirection = PortDirectionExtensions.FromPortName(connection.SourcePort);
            var targetDirection = PortDirectionExtensions.FromPortName(connection.TargetPort);

            var sourcePos = GetPortPosition(sourceNode, connection.SourcePort);
            var targetPos = GetPortPosition(targetNode, connection.TargetPort);

            // 计算节点边界矩形
            var sourceNodeRect = new Rect(
                sourceNode.Position.X,
                sourceNode.Position.Y,
                sourceNode.StyleConfig.NodeWidth,
                sourceNode.StyleConfig.NodeHeight);

            var targetNodeRect = new Rect(
                targetNode.Position.X,
                targetNode.Position.Y,
                targetNode.StyleConfig.NodeWidth,
                targetNode.StyleConfig.NodeHeight);

            // 计算所有节点边界（用于碰撞检测）
            var allNodeRects = _nodes.Select(n => new Rect(
                n.Position.X,
                n.Position.Y,
                n.StyleConfig.NodeWidth,
                n.StyleConfig.NodeHeight)).ToArray();

            // 🔥 减少日志输出以提高性能
            /*
            // 关键日志：记录路径计算输入和目标节点信息
            System.Diagnostics.Debug.WriteLine($"[PathCache] 计算路径: {connection.Id}");
            System.Diagnostics.Debug.WriteLine($"[PathCache]   源节点:{sourceNode.Name} 位置:({sourceNode.Position.X:F1},{sourceNode.Position.Y:F1}), 端口:{connection.SourcePort}({sourcePos.X:F1},{sourcePos.Y:F1})");
            System.Diagnostics.Debug.WriteLine($"[PathCache]   目标节点:{targetNode.Name} 位置:({targetNode.Position.X:F1},{targetNode.Position.Y:F1}), 端口:{connection.TargetPort}({targetPos.X:F1},{targetPos.Y:F1})");
            System.Diagnostics.Debug.WriteLine($"[PathCache]   源节点边界:({sourceNodeRect.X:F1},{sourceNodeRect.Y:F1},{sourceNodeRect.Width:F1}x{sourceNodeRect.Height:F1})");
            System.Diagnostics.Debug.WriteLine($"[PathCache]   目标节点边界:({targetNodeRect.X:F1},{targetNodeRect.Y:F1},{targetNodeRect.Width:F1}x{targetNodeRect.Height:F1})");
            */

            // 根据端口方向计算箭头尾部位置（路径终点）
            var arrowTailPos = CalculateArrowTailPosition(targetPos, targetDirection);

            /*
            System.Diagnostics.Debug.WriteLine($"[PathCache]   目标端口位置（箭头尖端）:({targetPos.X:F1},{targetPos.Y:F1})");
            System.Diagnostics.Debug.WriteLine($"[PathCache]   箭头尾部位置（路径终点）:({arrowTailPos.X:F1},{arrowTailPos.Y:F1})");
            */

            // 使用箭头尾部作为路径终点，传递所有节点边界信息用于碰撞检测
            var pathPoints = _pathCalculator.CalculateOrthogonalPath(
                sourcePos,
                arrowTailPos,  // 路径终点 = 箭头尾部
                sourceDirection,
                targetDirection,
                sourceNodeRect,  // 源节点边界
                targetNodeRect,  // 目标节点边界
                allNodeRects);   // 所有节点边界（用于碰撞检测）

            // 🔥 减少日志输出以提高性能
            // 关键日志：记录路径终点位置
            // var lastPoint = pathPoints[pathPoints.Length - 1];
            // System.Diagnostics.Debug.WriteLine($"[PathCache]   路径终点:({lastPoint.X:F1},{lastPoint.Y:F1}), 距目标端口X:{lastPoint.X - targetPos.X:F1}px, Y:{lastPoint.Y - targetPos.Y:F1}px");

            // 创建路径几何
            var pathGeometry = _pathCalculator.CreatePathGeometry(pathPoints);

            // 更新连线路径点集合（用于调试和显示）
            UpdateConnectionPathPoints(connection, pathPoints);

            // 计算箭头位置和角度
            var (arrowPosition, arrowAngle) = _pathCalculator.CalculateArrow(pathPoints, targetPos, targetDirection);
            connection.ArrowPosition = arrowPosition;
            connection.ArrowAngle = arrowAngle;

            /*
            // 关键日志：记录路径计算结果
            var lastPoint = pathPoints[pathPoints.Length - 1];
            System.Diagnostics.Debug.WriteLine($"[PathCache] 路径计算完成: {connection.Id}, 箭头角度{arrowAngle:F1}°, 路径点数{pathPoints.Length}");
            System.Diagnostics.Debug.WriteLine($"[PathCache]   箭头渲染位置:({arrowPosition.X:F1},{arrowPosition.Y:F1}), 距目标端口X:{arrowPosition.X - targetPos.X:F1}px, Y:{arrowPosition.Y - targetPos.Y:F1}px");
            System.Diagnostics.Debug.WriteLine($"[PathCache]   目标端口方向:{targetDirection}, 路径终点{(lastPoint.X >= targetNode.Position.X && lastPoint.X <= targetNode.Position.X + targetNode.StyleConfig.NodeWidth && lastPoint.Y >= targetNode.Position.Y && lastPoint.Y <= targetNode.Position.Y + targetNode.StyleConfig.NodeHeight ? "在节点内" : "在节点外")}");
            */

            return pathGeometry;
        }

        /// <summary>
        /// 根据端口名称获取端口位置
        /// </summary>
        private static Point GetPortPosition(WorkflowNode node, string portName)
        {
            return portName?.ToLower() switch
            {
                "top" or "topport" => node.TopPortPosition,
                "bottom" or "bottomport" => node.BottomPortPosition,
                "left" or "leftport" => node.LeftPortPosition,
                "right" or "rightport" => node.RightPortPosition,
                _ => node.RightPortPosition // 默认为右侧端口
            };
        }

        /// <summary>
        /// 计算箭头尾部位置（路径终点）
        /// 箭头尖端在目标端口中心，箭头尾部向外偏移箭头长度
        /// </summary>
        private static Point CalculateArrowTailPosition(Point arrowTipPosition, PathCalculators.PortDirection targetDirection)
        {
            const double arrowLength = 15.0; // 箭头长度

            // 根据端口方向，将箭头尖端向后偏移箭头长度
            return targetDirection switch
            {
                PathCalculators.PortDirection.Top => new Point(arrowTipPosition.X, arrowTipPosition.Y - arrowLength),
                PathCalculators.PortDirection.Bottom => new Point(arrowTipPosition.X, arrowTipPosition.Y + arrowLength),
                PathCalculators.PortDirection.Left => new Point(arrowTipPosition.X - arrowLength, arrowTipPosition.Y),
                PathCalculators.PortDirection.Right => new Point(arrowTipPosition.X + arrowLength, arrowTipPosition.Y),
                _ => arrowTipPosition
            };
        }

        /// <summary>
        /// 更新连线的路径点集合
        /// </summary>
        private static void UpdateConnectionPathPoints(WorkflowConnection connection, Point[] pathPoints)
        {
            connection.PathPoints.Clear();
            foreach (var point in pathPoints)
            {
                connection.PathPoints.Add(point);
            }
        }

        private void UpdateCache(string connectionId, PathGeometry path)
        {
            _pathCache[connectionId] = new CachedPath
            {
                Geometry = path,
                Timestamp = DateTime.Now,
                SourceNodeId = ExtractSourceNodeId(connectionId),
                TargetNodeId = ExtractTargetNodeId(connectionId)
            };
            _dirtyFlags[connectionId] = false;
        }

        private static string ExtractSourceNodeId(string connectionId)
        {
            return connectionId.Split('_')[0];
        }

        private static string ExtractTargetNodeId(string connectionId)
        {
            return connectionId.Split('_')[1];
        }

        private void SubscribeToNodes()
        {
            _nodes.CollectionChanged += (s, e) =>
            {
                if (e.OldItems != null)
                {
                    foreach (WorkflowNode node in e.OldItems)
                    {
                        MarkNodeDirty(node.Id);
                    }
                }

                if (e.NewItems != null)
                {
                    foreach (WorkflowNode node in e.NewItems)
                    {
                        MarkNodeDirty(node.Id);
                    }
                }
            };
        }
    }

    /// <summary>
    /// 缓存的路径
    /// </summary>
    internal class CachedPath
    {
        public PathGeometry Geometry { get; set; } = new PathGeometry();
        public DateTime Timestamp { get; set; }
        public string SourceNodeId { get; set; } = string.Empty;
        public string TargetNodeId { get; set; } = string.Empty;
    }

    /// <summary>
    /// 缓存统计信息
    /// </summary>
    public class CacheStatistics
    {
        public int CacheSize { get; set; }
        public int CacheHits { get; set; }
        public int CacheMisses { get; set; }
        public double HitRate { get; set; }
        public int TotalRequests => CacheHits + CacheMisses;

        public override string ToString()
        {
            return $"缓存大小: {CacheSize}, 命中: {CacheHits}, 未命中: {CacheMisses}, 命中率: {HitRate:P2}";
        }
    }
}
