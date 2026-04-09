using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SunEyeVision.Plugin.Infrastructure.Managers.Tool;
using SunEyeVision.Plugin.SDK.Execution.Results;
using SunEyeVision.Plugin.SDK.Logging;

namespace SunEyeVision.Plugin.SDK.Execution.Parameters
{
    /// <summary>
    /// 鏁版嵁婧愭煡璇㈡湇鍔″疄鐜?
    /// </summary>
    /// <remarks>
    /// 鎻愪緵鏌ヨ鐖惰妭鐐瑰強鍏惰緭鍑哄睘鎬х殑鑳藉姏銆?
    /// 
    /// 鏍稿績鍔熻兘锛?
    /// 1. 鍩轰簬宸ヤ綔娴佽繛鎺ュ叧绯绘煡鎵剧埗鑺傜偣
    /// 2. 浠庢墽琛岀粨鏋滅紦瀛樻彁鍙栬緭鍑哄睘鎬?
    /// 3. 鏀寔绫诲瀷杩囨护
    /// 4. 绾跨▼瀹夊叏鐨勭粨鏋滅紦瀛?
    /// </remarks>
    public class DataSourceQueryService : IDataSourceQueryService
    {
        /// <summary>
        /// 鑺傜偣缁撴灉缂撳瓨
        /// </summary>
        private readonly ConcurrentDictionary<string, ToolResults> _nodeResults = new ConcurrentDictionary<string, ToolResults>();

        /// <summary>
        /// Logger
        /// </summary>
        private readonly ILogger? _logger;


        /// <summary>
        /// 宸ヤ綔娴佽繛鎺ユ彁渚涜€咃紙澶栭儴娉ㄥ叆锛?
        /// </summary>
        private readonly IWorkflowConnectionProvider? _connectionProvider;

        /// <summary>
        /// 鑺傜偣淇℃伅鎻愪緵鑰咃紙澶栭儴娉ㄥ叆锛?
        /// </summary>
        private readonly INodeInfoProvider? _nodeInfoProvider;

        /// <summary>
        /// 杈撳嚭鍙樻洿璁㈤槄瀛楀吀
        /// </summary>
        private readonly ConcurrentDictionary<string, List<Action<object?>>> _outputSubscriptions = new ConcurrentDictionary<string, List<Action<object?>>>();

        /// <summary>
        /// 褰撳墠鑺傜偣ID
        /// </summary>
        private string? _currentNodeId;

        /// <summary>
        /// 褰撳墠鑺傜偣ID
        /// </summary>
        public string? CurrentNodeId
        {
            get => _currentNodeId;
            set => _currentNodeId = value;
        }

        /// <summary>
        /// 鍒涘缓鏁版嵁婧愭煡璇㈡湇鍔?
        /// </summary>
        public DataSourceQueryService()
        {
        }

        /// <summary>
        /// 鍒涘缓鏁版嵁婧愭煡璇㈡湇鍔★紙甯︿緷璧栨敞鍏ワ級
        /// </summary>
        /// <param name="connectionProvider">宸ヤ綔娴佽繛鎺ユ彁渚涜€?/param>
        /// <param name="nodeInfoProvider">鑺傜偣淇℃伅鎻愪緵鑰?/param>
        public DataSourceQueryService(
            IWorkflowConnectionProvider? connectionProvider,
            INodeInfoProvider? nodeInfoProvider = null)
        {
            _connectionProvider = connectionProvider;
            _nodeInfoProvider = nodeInfoProvider;
        }

        /// <summary>
        /// 鍒涘缓鏁版嵁婧愭煡璇㈡湇鍔★紙甯︿緷璧栨敞鍍ュ強 Logger锛?
        /// </summary>
        /// <param name="connectionProvider">宸ヤ綔娴佽繛鎺ユ彁渚涜€?/param>
        /// <param name="nodeInfoProvider">鑺傜偣淇℃伅鎻愪緵鑰?/param>
        /// <param name="logger">Logger</param>
        public DataSourceQueryService(
            IWorkflowConnectionProvider? connectionProvider,
            INodeInfoProvider? nodeInfoProvider,
            ILogger? logger = null)
        {
            _connectionProvider = connectionProvider;
            _nodeInfoProvider = nodeInfoProvider;
            _logger = logger;
        }

        /// <inheritdoc/>
        public List<ParentNodeInfo> GetParentNodes(string nodeId)
        {
            var parentNodes = new List<ParentNodeInfo>();

            _logger?.LogInfo($"========== GetParentNodes 开始 ==========", "DataSourceQueryService");
            _logger?.LogInfo($"查询节点ID: {nodeId}", "DataSourceQueryService");
            _logger?.LogInfo($"_connectionProvider: {(_connectionProvider != null ? "✅ 已注入" : "❌ 为 null")}", "DataSourceQueryService");

            if (_connectionProvider == null)
            {
                // 如果没有连接提供者，返回空列表
                _logger?.LogInfo("❌ _connectionProvider 为 null，返回空列表", "DataSourceQueryService");
                _logger?.LogInfo($"=========================================", "DataSourceQueryService");
                return parentNodes;
            }

            // 获取父节点ID列表
            var parentNodeIds = _connectionProvider.GetParentNodeIds(nodeId);
            _logger?.LogInfo($"找到 {parentNodeIds.Count} 个父节点: [{string.Join(", ", parentNodeIds)}]", "DataSourceQueryService");

            int order = 0;

            foreach (var parentNodeId in parentNodeIds)
            {
                var nodeInfo = CreateParentNodeInfo(parentNodeId, order++);
                parentNodes.Add(nodeInfo);
                _logger?.LogInfo($"  添加父节点: {nodeInfo.NodeName} (ID: {nodeInfo.NodeId})", "DataSourceQueryService");
            }

            _logger?.LogInfo($"返回 {parentNodes.Count} 个父节点", "DataSourceQueryService");
            _logger?.LogInfo($"=========================================", "DataSourceQueryService");
            return parentNodes;
        }

        /// <inheritdoc/>
        public List<AvailableDataSource> GetAvailableDataSources(string nodeId, Type? targetType = null)
        {
            _logger?.LogInfo($"========== GetAvailableDataSources 开始 ==========", "DataSourceQueryService");
            _logger?.LogInfo($"查询节点ID: {nodeId}", "DataSourceQueryService");
            _logger?.LogInfo($"目标类型: {targetType?.Name ?? "Any"}", "DataSourceQueryService");

            var dataSources = new List<AvailableDataSource>();
            var parentNodes = GetParentNodes(nodeId);

            _logger?.LogInfo($"遍历 {parentNodes.Count} 个父节点...", "DataSourceQueryService");

            foreach (var parent in parentNodes)
            {
                var properties = parent.OutputProperties;
                _logger?.LogInfo($"  父节点 [{parent.NodeName}] 有 {properties.Count} 个输出属性", "DataSourceQueryService");

                // 绫诲瀷杩囨护
                if (targetType != null)
                {
                    properties = parent.GetCompatibleProperties(targetType);
                    _logger?.LogInfo($"    过滤后剩余 {properties.Count} 个兼容属性", "DataSourceQueryService");
                }

                dataSources.AddRange(properties);

                foreach (var prop in properties)
                {
                    _logger?.LogInfo($"      - {prop.DisplayName} ({prop.PropertyType.Name})", "DataSourceQueryService");
                }
            }

            _logger?.LogInfo($"总共返回 {dataSources.Count} 个数据源", "DataSourceQueryService");
            _logger?.LogInfo($"==================================================", "DataSourceQueryService");
            return dataSources;
        }

        /// <inheritdoc/>
        public List<AvailableDataSource> GetNodeOutputProperties(string parentNodeId)
        {
            var properties = new List<AvailableDataSource>();

            // 鑾峰彇鑺傜偣缁撴灉
            var result = GetNodeResult(parentNodeId);
            if (result == null)
            {
                // 灏濊瘯浠庤妭鐐逛俊鎭彁渚涜€呰幏鍙?
                if (_nodeInfoProvider != null)
                {
                    // 杩斿洖绌哄睘鎬у垪琛紙鑺傜偣鏈墽琛岋級
                    return properties;
                }

                return properties;
            }

            // 鑾峰彇鑺傜偣鍚嶇О鍜岀被鍨?
            string nodeName = _nodeInfoProvider?.GetNodeName(parentNodeId) ?? parentNodeId;
            string nodeType = _nodeInfoProvider?.GetNodeType(parentNodeId) ?? "Unknown";

            // 浠庣粨鏋滀腑鎻愬彇灞炴€?
            var resultItems = result.GetResultItems();
            foreach (var item in resultItems)
            {
                var dataSource = new AvailableDataSource
                {
                    SourceNodeId = parentNodeId,
                    SourceNodeName = nodeName,
                    SourceNodeType = nodeType,
                    PropertyName = item.Name,
                    DisplayName = item.DisplayName ?? item.Name,
                    PropertyType = item.Value?.GetType() ?? typeof(object),
                    CurrentValue = item.Value,
                    Unit = item.Unit,
                    Description = item.Description,
                    GroupName = nodeName
                };

                properties.Add(dataSource);
            }

            return properties;
        }

        /// <inheritdoc/>
        public ToolResults? GetNodeResult(string nodeId)
        {
            _nodeResults.TryGetValue(nodeId, out var result);
            return result;
        }

        /// <inheritdoc/>
        public object? GetPropertyValue(string nodeId, string propertyName)
        {
            var result = GetNodeResult(nodeId);
            if (result == null)
                return null;

            // 灏濊瘯浠庣粨鏋滈」鑾峰彇
            var resultItems = result.GetResultItems();
            var item = resultItems.FirstOrDefault(i => i.Name == propertyName);
            if (item != null)
                return item.Value;

            // 灏濊瘯閫氳繃鍙嶅皠鑾峰彇灞炴€?
            var property = result.GetType().GetProperty(propertyName);
            if (property != null && property.CanRead)
            {
                try
                {
                    return property.GetValue(result);
                }
                catch
                {
                    return null;
                }
            }

            // 灏濊瘯鑾峰彇宓屽灞炴€э紙濡?Center.X锛?
            if (propertyName.Contains('.'))
            {
                return GetNestedPropertyValue(result, propertyName);
            }

            return null;
        }

        /// <inheritdoc/>
        public bool HasNodeExecuted(string nodeId)
        {
            var result = GetNodeResult(nodeId);
            return result != null && (result.Status == ExecutionStatus.Success || result.Status == ExecutionStatus.PartialSuccess);
        }

        /// <inheritdoc/>
        public void RefreshNodeData(string nodeId)
        {
            // 娓呴櫎缂撳瓨锛屼笅娆℃煡璇㈡椂閲嶆柊鑾峰彇
            _nodeResults.TryRemove(nodeId, out _);
        }

        /// <inheritdoc/>
        public void RefreshAll()
        {
            _nodeResults.Clear();
        }

        /// <inheritdoc/>
        public void SetNodeResult(string nodeId, ToolResults result)
        {
            _nodeResults[nodeId] = result ?? throw new ArgumentNullException(nameof(result));
        }

        /// <inheritdoc/>
        public void ClearNodeResult(string nodeId)
        {
            _nodeResults.TryRemove(nodeId, out _);
        }

        /// <inheritdoc/>
        public void ClearAllResults()
        {
            _nodeResults.Clear();
        }

        /// <inheritdoc/>
        public bool HasNodeOutput(string nodeId)
        {
            var result = GetNodeResult(nodeId);
            return result != null && result.Status == ExecutionStatus.Success;
        }

        /// <inheritdoc/>
        public void UpdateNodeOutput(string nodeId, string portName, object? value)
        {
            var result = _nodeResults.GetOrAdd(nodeId, _ => new GenericToolResults { Status = ExecutionStatus.Success });

            // 修复：类型转换为 GenericToolResults 以访问 AddResultItem 方法
            var genericResult = result as GenericToolResults;
            if (genericResult != null)
            {
                genericResult.AddResultItem(portName, value);
            }
        }


        /// <inheritdoc/>
        public void ClearNodeOutput(string nodeId)
        {
            _nodeResults.TryRemove(nodeId, out _);
        }

        /// <inheritdoc/>
        public object? GetCurrentBindingValue(string nodeId, string portName, string? propertyPath)
        {
            var result = GetNodeResult(nodeId);
            if (result == null)
                return null;

            // 濡傛灉娌℃湁鎸囧畾灞炴€ц矾寰勶紝杩斿洖鏁翠釜杈撳嚭
            if (string.IsNullOrEmpty(propertyPath))
            {
                var resultItems = result.GetResultItems();
                var item = resultItems.FirstOrDefault(i => i.Name == portName);
                return item?.Value;
            }

            // 鏈夊睘鎬ц矾寰勶紝浣跨敤 GetPropertyValue
            return GetPropertyValue(nodeId, propertyPath);
        }

        /// <inheritdoc/>
        public string GetBindingDisplayPath(string nodeId, string outputName, string? propertyPath)
        {
            var nodeName = _nodeInfoProvider?.GetNodeName(nodeId) ?? nodeId;

            if (string.IsNullOrEmpty(propertyPath))
                return $"{nodeName}.{outputName}";

            return $"{nodeName}.{outputName}.{propertyPath}";
        }

        /// <inheritdoc/>
        public IDisposable SubscribeOutputChanged(string nodeId, string outputName, string? propertyPath, Action<object?> onChanged)
        {
            var key = $"{nodeId}:{outputName}:{propertyPath ?? ""}";

            if (!_outputSubscriptions.ContainsKey(key))
            {
                _outputSubscriptions[key] = new List<Action<object?>>();
            }

            _outputSubscriptions[key].Add(onChanged);

            return new SubscriptionToken(() =>
            {
                if (_outputSubscriptions.TryGetValue(key, out var subscriptions))
                {
                    subscriptions.Remove(onChanged);
                    if (subscriptions.Count == 0)
                    {
                        _outputSubscriptions.TryRemove(key, out _);
                    }
                }
            });
        }

        /// <inheritdoc/>
        public void RefreshOutputs()
        {
            // 閫氱煡鎵€鏈夎闃呰€?
            foreach (var kvp in _outputSubscriptions)
            {
                var key = kvp.Key;
                var parts = key.Split(':');
                if (parts.Length >= 2)
                {
                    var nodeId = parts[0];
                    var outputName = parts[1];
                    var propertyPath = parts.Length > 2 ? parts[2] : null;

                    var value = GetCurrentBindingValue(nodeId, outputName, propertyPath);

                    foreach (var callback in kvp.Value)
                    {
                        try
                        {
                            callback(value);
                        }
                        catch
                        {
                            // 蹇界暐鍥炶皟涓殑寮傚父
                        }
                    }
                }
            }
        }

        /// <inheritdoc/>
        public bool IsNodeRegistered(string nodeId)
        {
            return _nodeResults.ContainsKey(nodeId) ||
                   (_nodeInfoProvider != null && _nodeInfoProvider.NodeExists(nodeId));
        }

        /// <summary>
        /// 创建父节点信息
        /// </summary>
        /// <remarks>
        /// 统一的设计时和运行时提取逻辑：
        /// - 从工具元数据获取 ResultType
        /// - 从 ResultType 反射提取输出属性
        /// - 如果有执行结果，填充实际值和执行状态
        /// </remarks>
        private ParentNodeInfo CreateParentNodeInfo(string nodeId, int order)
        {
            string nodeName = _nodeInfoProvider?.GetNodeName(nodeId) ?? nodeId;
            string nodeType = _nodeInfoProvider?.GetNodeType(nodeId) ?? "Unknown";
            string? nodeIcon = _nodeInfoProvider?.GetNodeIcon(nodeId);

            var nodeInfo = new ParentNodeInfo
            {
                NodeId = nodeId,
                NodeName = nodeName,
                NodeType = nodeType,
                NodeIcon = nodeIcon,
                ConnectionOrder = order
            };

            // 鑾峰彇鎵ц缁撴灉
            var result = GetNodeResult(nodeId);
            if (result != null)
            {
                nodeInfo.ExecutionStatus = result.Status;
                nodeInfo.ExecutionTimeMs = result.ExecutionTimeMs;
                nodeInfo.ErrorMessage = result.ErrorMessage;

                // 鎻愬彇杈撳嚭灞炴€?
                nodeInfo.ExtractOutputProperties(result);
            }

            return nodeInfo;
        }

        /// <summary>
        /// 鑾峰彇宓屽灞炴€у€?
        /// </summary>
        private object? GetNestedPropertyValue(object obj, string propertyPath)
        {
            var parts = propertyPath.Split('.');
            object? current = obj;

            foreach (var part in parts)
            {
                if (current == null)
                    return null;

                var type = current.GetType();
                var property = type.GetProperty(part, BindingFlags.Public | BindingFlags.Instance);

                if (property == null)
                    return null;

                try
                {
                    current = property.GetValue(current);
                }
                catch
                {
                    return null;
                }
            }

            return current;
        }

        /// <summary>
        /// 璁㈤槄浠ょ墝
        /// </summary>
        private class SubscriptionToken : IDisposable
        {
            private readonly Action _disposeAction;
            private bool _disposed;

            public SubscriptionToken(Action disposeAction)
            {
                _disposeAction = disposeAction ?? throw new ArgumentNullException(nameof(disposeAction));
            }

            public void Dispose()
            {
                if (!_disposed)
                {
                    _disposeAction();
                    _disposed = true;
                }
            }
        }
    }

    /// <summary>
    /// 宸ヤ綔娴佽繛鎺ユ彁渚涜€呮帴鍙?
    /// </summary>
    /// <remarks>
    /// 鎻愪緵宸ヤ綔娴佽妭鐐硅繛鎺ュ叧绯绘煡璇㈣兘鍔涖€?
    /// 鐢卞伐浣滄祦寮曟搸鎴朥I灞傚疄鐜般€?
    /// </remarks>
    public interface IWorkflowConnectionProvider
    {
        /// <summary>
        /// 鑾峰彇鐖惰妭鐐笽D鍒楄〃
        /// </summary>
        /// <param name="nodeId">褰撳墠鑺傜偣ID</param>
        /// <returns>鐖惰妭鐐笽D鍒楄〃</returns>
        List<string> GetParentNodeIds(string nodeId);

        /// <summary>
        /// 鑾峰彇瀛愯妭鐐笽D鍒楄〃
        /// </summary>
        /// <param name="nodeId">褰撳墠鑺傜偣ID</param>
        /// <returns>瀛愯妭鐐笽D鍒楄〃</returns>
        List<string> GetChildNodeIds(string nodeId);

        /// <summary>
        /// 鑾峰彇鎵€鏈夎妭鐐笽D
        /// </summary>
        /// <returns>鎵€鏈夎妭鐐笽D鍒楄〃</returns>
        List<string> GetAllNodeIds();
    }

    /// <summary>
    /// 鑺傜偣淇℃伅鎻愪緵鑰呮帴鍙?
    /// </summary>
    /// <remarks>
    /// 鎻愪緵鑺傜偣鍩烘湰淇℃伅鏌ヨ鑳藉姏銆?
    /// 鐢卞伐浣滄祦寮曟搸鎴朥I灞傚疄鐜般€?
    /// </remarks>
    public interface INodeInfoProvider
    {
        /// <summary>
        /// 鑾峰彇鑺傜偣鍚嶇О
        /// </summary>
        /// <param name="nodeId">鑺傜偣ID</param>
        /// <returns>鑺傜偣鍚嶇О</returns>
        string GetNodeName(string nodeId);

        /// <summary>
        /// 鑾峰彇鑺傜偣绫诲瀷
        /// </summary>
        /// <param name="nodeId">鑺傜偣ID</param>
        /// <returns>鑺傜偣绫诲瀷</returns>
        string GetNodeType(string nodeId);

        /// <summary>
        /// 鑾峰彇鑺傜偣鍥炬爣
        /// </summary>
        /// <param name="nodeId">鑺傜偣ID</param>
        /// <returns>鑺傜偣鍥炬爣璺緞鎴栧悕绉?/returns>
        string? GetNodeIcon(string nodeId);

        /// <summary>
        /// 妫€鏌ヨ妭鐐规槸鍚﹀瓨鍦?
        /// </summary>
        /// <param name="nodeId">鑺傜偣ID</param>
        /// <returns>鏄惁瀛樺湪</returns>
        bool NodeExists(string nodeId);
    }
}
