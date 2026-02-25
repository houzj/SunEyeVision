using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using SunEyeVision.UI.Models;
using SunEyeVision.UI.Services.PathCalculators;
using SunEyeVision.UI.Services.Connection;
using SunEyeVision.UI.Services.Path;
using SunEyeVision.UI.Services.Node;
using PortDirection = SunEyeVision.UI.Services.Path.PortDirection;

namespace SunEyeVision.UI.Services.Connection
{
    /// <summary>
    /// 杩炴帴绾胯矾寰勭紦瀛?- 閬垮厤閲嶅璁＄畻杩炴帴绾胯矾寰?
    /// </summary>
    public class ConnectionPathCache
    {
        private readonly Dictionary<string, CachedPath> _pathCache;
        private readonly Dictionary<string, bool> _dirtyFlags;
        private readonly ObservableCollection<WorkflowNode> _nodes;
        private readonly object _lockObj;
        private readonly IPathCalculator _pathCalculator;

        // 5C: 鑺傜偣浣嶇疆璺熻釜锛堢敤浜庣粺璁″拰璋冭瘯锛屼笉鍐嶇敤浜庤窛绂婚槇鍊煎垽鏂級
        private readonly Dictionary<string, Point> _lastNodePositions = new Dictionary<string, Point>();
        private readonly Dictionary<string, int> _connectionUsageCount = new Dictionary<string, int>();

        /// <summary>
        /// 缂撳瓨鍛戒腑娆℃暟
        /// </summary>
        public int CacheHits { get; private set; }

        /// <summary>
        /// 缂撳瓨鏈懡涓鏁?
        /// </summary>
        public int CacheMisses { get; private set; }

        /// <summary>
        /// 缂撳瓨澶у皬
        /// </summary>
        public int CacheSize => _pathCache.Count;

        /// <summary>
        /// 缂撳瓨鍛戒腑鐜?
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
            _pathCalculator = pathCalculator ?? PathCalculatorFactory.CreateCalculator();

            SubscribeToNodes();
        }

        /// <summary>
        /// 鑾峰彇杩炴帴绾胯矾寰?
        /// </summary>
        public PathGeometry? GetPath(WorkflowConnection connection)
        {
            lock (_lockObj)
            {
                // 4C: 澧炲姞杩炴帴浣跨敤璁℃暟
                if (_connectionUsageCount.ContainsKey(connection.Id))
                {
                    _connectionUsageCount[connection.Id]++;
                }
                else
                {
                    _connectionUsageCount[connection.Id] = 1;
                }

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
        /// 鑾峰彇杩炴帴绾胯矾寰勬暟鎹紙瀛楃涓诧級
        /// </summary>
        public string? GetPathData(WorkflowConnection connection)
        {
            var geometry = GetPath(connection);
            return geometry?.ToString();
        }

        /// <summary>
        /// 鏍囪杩炴帴涓鸿剰锛堥渶瑕侀噸鏂拌绠楋級
        /// </summary>
        public void MarkDirty(WorkflowConnection connection)
        {
            lock (_lockObj)
            {
                _dirtyFlags[connection.Id] = true;
            }
        }

        /// <summary>
        /// 鏍囪鎵€鏈夎繛鎺ヤ负鑴?
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
        /// 鏍囪鑺傜偣鐩稿叧鐨勬墍鏈夎繛鎺ヤ负鑴?
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
        /// 5C: 鏍囪鑺傜偣涓鸿剰锛堢Щ闄よ窛绂婚槇鍊硷紝浣跨敤鑺傛祦鏈哄埗锛?
        /// 璺緞鏇存柊鐨勮妭娴佺敱ConnectionBatchUpdateManager鎺у埗锛?6ms寤惰繜锛?
        /// </summary>
        public void MarkNodeDirtySmart(string nodeId, Point newPosition)
        {
            lock (_lockObj)
            {
                // 璁板綍鏂颁綅缃?
                _lastNodePositions[nodeId] = newPosition;

                // 鐩存帴鏍囪鐩稿叧杩炴帴涓鸿剰
                // 涓嶄娇鐢ㄨ窛绂婚槇鍊硷紝璁〤onnectionBatchUpdateManager鎺у埗鑺傛祦
                MarkNodeDirty(nodeId);
            }
        }

        /// <summary>
        /// 娓呴櫎缂撳瓨
        /// </summary>
        public void Clear()
        {
            lock (_lockObj)
            {
                _pathCache.Clear();
                _dirtyFlags.Clear();
                _lastNodePositions.Clear();
                _connectionUsageCount.Clear();
                CacheHits = 0;
                CacheMisses = 0;
            }
        }

        /// <summary>
        /// 4C: 鏅鸿兘娓呯悊缂撳瓨锛堝熀浜庝娇鐢ㄩ鐜囧拰LRU绛栫暐锛?
        /// </summary>
        public void CleanupCache(int targetSize = 500)
        {
            lock (_lockObj)
            {
                if (_pathCache.Count <= targetSize)
                    return;

                // 鎸変娇鐢ㄩ鐜囨帓搴忥紝绉婚櫎鏈€灏戜娇鐢ㄧ殑
                var sortedConnections = _connectionUsageCount
                    .OrderBy(kvp => kvp.Value)
                    .Take(_pathCache.Count - targetSize)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var connectionId in sortedConnections)
                {
                    _pathCache.Remove(connectionId);
                    _dirtyFlags.Remove(connectionId);
                    _connectionUsageCount.Remove(connectionId);
                }
            }
        }

        /// <summary>
        /// 绉婚櫎杩炴帴鐨勭紦瀛?
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
        /// 棰勭儹缂撳瓨锛堥鍏堣绠楁墍鏈夎繛鎺ワ級
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
            }
        }

        /// <summary>
        /// 鑾峰彇缂撳瓨缁熻淇℃伅
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

            // 鏍规嵁绔彛鍚嶇О鑾峰彇绔彛鏂瑰悜鍜屼綅缃?
            var sourceDirection = PortDirectionExtensions.FromPortName(connection.SourcePort);
            var targetDirection = PortDirectionExtensions.FromPortName(connection.TargetPort);

            var sourcePos = GetPortPosition(sourceNode, connection.SourcePort);
            var targetPos = GetPortPosition(targetNode, connection.TargetPort);

            // 璁＄畻鑺傜偣杈圭晫鐭╁舰
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

            // 璁＄畻鎵€鏈夎妭鐐硅竟鐣岋紙鐢ㄤ簬纰版挒妫€娴嬶級
            var allNodeRects = _nodes.Select(n => new Rect(
                n.Position.X,
                n.Position.Y,
                n.StyleConfig.NodeWidth,
                n                .StyleConfig.NodeHeight)).ToArray();

            // 鏍规嵁绔彛鏂瑰悜璁＄畻绠ご灏鹃儴浣嶇疆锛堣矾寰勭粓鐐癸級
            var arrowTailPos = CalculateArrowTailPosition(targetPos, targetDirection);



            // 浣跨敤绠ご灏鹃儴浣滀负璺緞缁堢偣锛屼紶閫掓墍鏈夎妭鐐硅竟鐣屼俊鎭敤浜庣鎾炴娴?
            var pathPoints = _pathCalculator.CalculateOrthogonalPath(
                sourcePos,
                arrowTailPos,  // 璺緞缁堢偣 = 绠ご灏鹃儴
                sourceDirection,
                targetDirection,
                sourceNodeRect,  // 婧愯妭鐐硅竟鐣?
                targetNodeRect,  // 鐩爣鑺傜偣杈圭晫
                allNodeRects);   // 鎵€鏈夎妭鐐硅竟鐣岋紙鐢ㄤ簬纰版挒妫€娴嬶級

            // 鍒涘缓璺緞鍑犱綍
            var pathGeometry = _pathCalculator.CreatePathGeometry(pathPoints);

            // 鏇存柊杩炵嚎璺緞鐐归泦鍚堬紙鐢ㄤ簬璋冭瘯鍜屾樉绀猴級
            UpdateConnectionPathPoints(connection, pathPoints);

            // 璁＄畻绠ご浣嶇疆鍜岃搴?
            var (arrowPosition, arrowAngle) = _pathCalculator.CalculateArrow(pathPoints, targetPos, targetDirection);
            connection.ArrowPosition = arrowPosition;
            connection.ArrowAngle = arrowAngle;

            return pathGeometry;
        }

        /// <summary>
        /// 鏍规嵁绔彛鍚嶇О鑾峰彇绔彛浣嶇疆
        /// </summary>
        private static Point GetPortPosition(WorkflowNode node, string portName)
        {
            return portName?.ToLower() switch
            {
                "top" or "topport" => node.TopPortPosition,
                "bottom" or "bottomport" => node.BottomPortPosition,
                "left" or "leftport" => node.LeftPortPosition,
                "right" or "rightport" => node.RightPortPosition,
                _ => node.RightPortPosition // 榛樿涓哄彸渚х鍙?
            };
        }

        /// <summary>
        /// 璁＄畻绠ご灏鹃儴浣嶇疆锛堣矾寰勭粓鐐癸級
        /// 绠ご灏栫鍦ㄧ洰鏍囩鍙ｄ腑蹇冿紝绠ご灏鹃儴鍚戝鍋忕Щ绠ご闀垮害
        /// </summary>
        private static Point CalculateArrowTailPosition(Point arrowTipPosition, PortDirection targetDirection)
        {
            const double arrowLength = 15.0; // 绠ご闀垮害

            // 鏍规嵁绔彛鏂瑰悜锛屽皢绠ご灏栫鍚戝悗鍋忕Щ绠ご闀垮害
            return targetDirection switch
            {
                PortDirection.Top => new Point(arrowTipPosition.X, arrowTipPosition.Y - arrowLength),
                PortDirection.Bottom => new Point(arrowTipPosition.X, arrowTipPosition.Y + arrowLength),
                PortDirection.Left => new Point(arrowTipPosition.X - arrowLength, arrowTipPosition.Y),
                PortDirection.Right => new Point(arrowTipPosition.X + arrowLength, arrowTipPosition.Y),
                _ => arrowTipPosition
            };
        }

        /// <summary>
        /// 鏇存柊杩炵嚎鐨勮矾寰勭偣闆嗗悎
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
    /// 缂撳瓨鐨勮矾寰?
    /// </summary>
    internal class CachedPath
    {
        public PathGeometry Geometry { get; set; } = new PathGeometry();
        public DateTime Timestamp { get; set; }
        public string SourceNodeId { get; set; } = string.Empty;
        public string TargetNodeId { get; set; } = string.Empty;
    }

    /// <summary>
    /// 缂撳瓨缁熻淇℃伅
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
            return $"缂撳瓨澶у皬: {CacheSize}, 鍛戒腑: {CacheHits}, 鏈懡涓? {CacheMisses}, 鍛戒腑鐜? {HitRate:P2}";
        }
    }
}
