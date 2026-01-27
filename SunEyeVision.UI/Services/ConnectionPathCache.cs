using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using SunEyeVision.UI.Models;

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

        public ConnectionPathCache(ObservableCollection<WorkflowNode> nodes)
        {
            _pathCache = new Dictionary<string, CachedPath>();
            _dirtyFlags = new Dictionary<string, bool>();
            _nodes = nodes;
            _lockObj = new object();

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
                foreach (var connection in connections)
                {
                    if (!_pathCache.ContainsKey(connection.Id))
                    {
                        var path = CalculatePath(connection);
                        UpdateCache(connection.Id, path);
                    }
                }
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

            var sourcePos = new Point(
                sourceNode.Position.X + CanvasConfig.NodeWidth / 2,
                sourceNode.Position.Y + CanvasConfig.NodeHeight / 2
            );
            var targetPos = new Point(
                targetNode.Position.X + CanvasConfig.NodeWidth / 2,
                targetNode.Position.Y + CanvasConfig.NodeHeight / 2
            );

            var pathGeometry = new PathGeometry();
            var pathFigure = new PathFigure
            {
                StartPoint = sourcePos,
                IsClosed = false
            };

            var midPoint1 = new Point(sourcePos.X, sourcePos.Y + (targetPos.Y - sourcePos.Y) / 2);
            var midPoint2 = new Point(targetPos.X, sourcePos.Y + (targetPos.Y - sourcePos.Y) / 2);

            pathFigure.Segments.Add(new BezierSegment(midPoint1, midPoint2, targetPos, true));
            pathGeometry.Figures.Add(pathFigure);

            return pathGeometry;
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

        private string ExtractSourceNodeId(string connectionId)
        {
            return connectionId.Split('_')[0];
        }

        private string ExtractTargetNodeId(string connectionId)
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
