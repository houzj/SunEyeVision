using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using SunEyeVision.UI.Models;
using SunEyeVision.UI.Services.PathCalculators;

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
            _pathCalculator = pathCalculator ?? new OrthogonalPathCalculator();

            SubscribeToNodes();
        }

        /// <summary>
        /// 获取连接线路径
        /// </summary>
        public PathGeometry? GetPath(WorkflowConnection connection)
        {
            lock (_lockObj)
            {
                if (_pathCache.TryGetValue(connection.Id, out var cachedPath))
                {
                    if (_dirtyFlags.TryGetValue(connection.Id, out bool isDirty) && isDirty)
                    {
                        var path = CalculatePath(connection);
                        UpdateCache(connection.Id, path);
                        CacheMisses++;
                        return path;
                    }

                    CacheHits++;
                    return cachedPath.Geometry;
                }

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
                _dirtyFlags[connection.Id] = true;
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
                foreach (var kvp in _pathCache)
                {
                    if (kvp.Value.SourceNodeId == nodeId || kvp.Value.TargetNodeId == nodeId)
                    {
                        _dirtyFlags[kvp.Key] = true;
                    }
                }
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

            // 关键日志：记录路径计算输入和目标节点信息
            System.Diagnostics.Debug.WriteLine($"[PathCache] 计算路径: {connection.Id}");
            System.Diagnostics.Debug.WriteLine($"[PathCache]   源节点:{sourceNode.Name} 位置:({sourceNode.Position.X:F1},{sourceNode.Position.Y:F1}), 端口:{connection.SourcePort}({sourcePos.X:F1},{sourcePos.Y:F1})");
            System.Diagnostics.Debug.WriteLine($"[PathCache]   目标节点:{targetNode.Name} 位置:({targetNode.Position.X:F1},{targetNode.Position.Y:F1}), 端口:{connection.TargetPort}({targetPos.X:F1},{targetPos.Y:F1})");
            System.Diagnostics.Debug.WriteLine($"[PathCache]   目标节点边界: 左{targetNode.Position.X:F1} 右{targetNode.Position.X + targetNode.StyleConfig.NodeWidth:F1}, 上{targetNode.Position.Y:F1} 下{targetNode.Position.Y + targetNode.StyleConfig.NodeHeight:F1}");

            // 使用路径计算器计算正交路径
            var pathPoints = _pathCalculator.CalculateOrthogonalPath(
                sourcePos,
                targetPos,
                sourceDirection,
                targetDirection);

            // 关键日志：记录路径终点位置
            var lastPoint = pathPoints[pathPoints.Length - 1];
            System.Diagnostics.Debug.WriteLine($"[PathCache]   路径终点:({lastPoint.X:F1},{lastPoint.Y:F1}), 距目标端口X:{lastPoint.X - targetPos.X:F1}px, Y:{lastPoint.Y - targetPos.Y:F1}px");

            // 创建路径几何
            var pathGeometry = _pathCalculator.CreatePathGeometry(pathPoints);

            // 更新连线路径点集合（用于调试和显示）
            UpdateConnectionPathPoints(connection, pathPoints);

            // 计算箭头位置和角度
            var (arrowPosition, arrowAngle) = _pathCalculator.CalculateArrow(pathPoints, targetPos, targetDirection);
            connection.ArrowPosition = arrowPosition;
            connection.ArrowAngle = arrowAngle;

            // 关键日志：记录路径计算结果
            System.Diagnostics.Debug.WriteLine($"[PathCache] 路径计算完成: {connection.Id}, 箭头角度{arrowAngle:F1}°, 路径点数{pathPoints.Length}");
            System.Diagnostics.Debug.WriteLine($"[PathCache]   箭头渲染位置:({arrowPosition.X:F1},{arrowPosition.Y:F1}), 距目标端口X:{arrowPosition.X - targetPos.X:F1}px, Y:{arrowPosition.Y - targetPos.Y:F1}px");
            System.Diagnostics.Debug.WriteLine($"[PathCache]   目标端口方向:{targetDirection}, 路径终点{(lastPoint.X >= targetNode.Position.X && lastPoint.X <= targetNode.Position.X + targetNode.StyleConfig.NodeWidth ? "在节点内" : "在节点外")}");

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
