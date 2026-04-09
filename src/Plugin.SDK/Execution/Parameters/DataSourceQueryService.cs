using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
        /// 节点信息缓存
        /// </summary>
        private readonly ConcurrentDictionary<string, ParentNodeInfo> _nodeInfoCache = new ConcurrentDictionary<string, ParentNodeInfo>();

        /// <summary>
        /// 节点输出缓存（节点ID -> 属性名 -> 值）
        /// </summary>
        private readonly ConcurrentDictionary<string, Dictionary<string, object?>> _nodeOutputs = new ConcurrentDictionary<string, Dictionary<string, object?>>();


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
                // 直接创建父节点信息
                var nodeInfo = new ParentNodeInfo
                {
                    NodeId = parentNodeId,
                    NodeName = _nodeInfoProvider?.GetNodeName(parentNodeId) ?? parentNodeId,
                    NodeType = _nodeInfoProvider?.GetNodeType(parentNodeId) ?? "Unknown",
                    NodeIcon = _nodeInfoProvider?.GetNodeIcon(parentNodeId)
                };

                // 获取该节点的输出属性
                var outputProperties = GetNodeOutputProperties(parentNodeId);
                nodeInfo.OutputProperties = outputProperties;

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

            // 鑾峰彇鑺傜偣鎵ц涓婁笅鏂?
            var context = GetNodeContext(parentNodeId);
            if (context == null)
            {
                return properties;
            }

            // 鍒涘缓鐖惰妭鐐逛俊鎭紙鐢ㄤ簬鎵╁睍鏂规硶锛?
            var nodeInfo = new ParentNodeInfo
            {
                NodeId = parentNodeId,
                NodeName = context.NodeName,
                NodeType = context.NodeType,
                NodeIcon = context.NodeIcon,
                ExecutionStatus = context.ExecutionStatus
            };

            // 缁熶竴鐨勬彁鍙栭€昏緫锛氳璁℃椂鍜岃繍琛屾椂
            // - 璁捐鏃讹細浠?ResultType 鍙嶅皠鎻愬彇灞炴€у畾涔?
            // - 杩愯鏃讹細浠?ToolResults 鎻愬彇灞炴€у畾涔?+ 瀹為檯鍊?
            nodeInfo.ExtractOutputPropertiesFromType(context.ResultType, context.Result);

            return nodeInfo.OutputProperties;
        }

        /// <inheritdoc/>
        public NodeExecutionContext? GetNodeContext(string nodeId)
        {
            if (_nodeInfoProvider == null)
            {
                return null;
            }

            return new NodeExecutionContext
            {
                NodeId = nodeId,
                NodeName = _nodeInfoProvider.GetNodeName(nodeId) ?? nodeId,
                NodeType = _nodeInfoProvider.GetNodeType(nodeId) ?? "Unknown",
                NodeIcon = _nodeInfoProvider.GetNodeIcon(nodeId),
                Result = GetNodeResultFromCache(nodeId),
                ResultType = _nodeInfoProvider.GetResultType(nodeId)
            };
        }

        /// <summary>
        /// 浠庣紦瀛樿幏鍙栬妭鐐规墽琛岀粨鏋?
        /// </summary>
        private ToolResults? GetNodeResultFromCache(string nodeId)
        {
            _nodeResults.TryGetValue(nodeId, out var result);
            return result;
        }
        /// <inheritdoc/>
        public object? GetPropertyValue(string nodeId, string propertyName)
        {
            // 浠庤妭鐐硅緭鍑轰腑鑾峰彇灞炴€у€?
            if (_nodeOutputs.TryGetValue(nodeId, out var outputs))
            {
                if (outputs.TryGetValue(propertyName, out var value))
                {
                    return value;
                }
            }

            // 浠庤妭鐐圭粨鏋滀腑鑾峰彇灞炴€у€?
            var result = GetNodeResultFromCache(nodeId);
            if (result != null)
            {
                var resultItems = result.GetResultItems();
                var item = resultItems.FirstOrDefault(i => i.Name == propertyName);
                return item?.Value;
            }

            return null;
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
        #region IDataSourceQueryService Runtime Methods

        /// <inheritdoc/>
        public bool HasNodeExecuted(string nodeId)
        {
            return _nodeResults.ContainsKey(nodeId);
        }

        /// <inheritdoc/>
        public void RefreshNodeData(string nodeId)
        {
            // TODO: 瀹炵幇鑺傜偣鏁版嵁鍒锋柊閫昏緫
            _logger?.LogInfo($"  RefreshNodeData({nodeId})", "DataSourceQueryService");
        }

        /// <inheritdoc/>
        public void RefreshAll()
        {
            // TODO: 瀹炵幇鎵€鏈夋暟鎹埛鏂伴€昏緫
            _logger?.LogInfo("  RefreshAll()", "DataSourceQueryService");
        }

        /// <inheritdoc/>
        public void SetNodeResult(string nodeId, ToolResults result)
        {
            _nodeResults[nodeId] = result;
            _logger?.LogInfo($"  SetNodeResult({nodeId})", "DataSourceQueryService");
        }

        /// <inheritdoc/>
        public void ClearNodeResult(string nodeId)
        {
            _nodeResults.TryRemove(nodeId, out _);
            _logger?.LogInfo($"  ClearNodeResult({nodeId})", "DataSourceQueryService");
        }

        /// <inheritdoc/>
        public void ClearAllResults()
        {
            _nodeResults.Clear();
            _logger?.LogInfo("  ClearAllResults()", "DataSourceQueryService");
        }

        /// <inheritdoc/>
        public bool HasNodeOutput(string nodeId)
        {
            return _nodeOutputs.ContainsKey(nodeId);
        }

        /// <inheritdoc/>
        public void UpdateNodeOutput(string nodeId, string propertyName, object? value)
        {
            if (!_nodeOutputs.ContainsKey(nodeId))
            {
                _nodeOutputs[nodeId] = new Dictionary<string, object?>();
            }
            _nodeOutputs[nodeId][propertyName] = value;
            _logger?.LogInfo($"  UpdateNodeOutput({nodeId}, {propertyName})", "DataSourceQueryService");
        }

        /// <inheritdoc/>
        public void ClearNodeOutput(string nodeId)
        {
            _nodeOutputs.TryRemove(nodeId, out _);
            _logger?.LogInfo($"  ClearNodeOutput({nodeId})", "DataSourceQueryService");
        }

        /// <inheritdoc/>
        public object? GetCurrentBindingValue(string nodeId, string propertyName, string? bindingPath = null)
        {
            // TODO: 瀹炵幇缁戝畾鍊艰幏鍙栭€昏緫
            _logger?.LogInfo($"  GetCurrentBindingValue({nodeId}, {propertyName})", "DataSourceQueryService");
            return null;
        }

        /// <inheritdoc/>
        public string GetBindingDisplayPath(string nodeId, string propertyName, string? bindingPath = null)
        {
            // TODO: 瀹炵幇缁戝畾鏄剧ず璺緞閫昏緫
            _logger?.LogInfo($"  GetBindingDisplayPath({nodeId}, {propertyName})", "DataSourceQueryService");
            return propertyName;
        }

        /// <inheritdoc/>
        public IDisposable SubscribeOutputChanged(string nodeId, string propertyName, string? bindingPath, Action<object?> callback)
        {
            // TODO: 瀹炵幇杈撳嚭鍙樻洿璁㈤槄閫昏緫
            _logger?.LogInfo($"  SubscribeOutputChanged({nodeId}, {propertyName})", "DataSourceQueryService");

            // 返回一个空的订阅令牌
            return new SubscriptionToken(() => { });
        }

        /// <inheritdoc/>
        public void RefreshOutputs()
        {
            // TODO: 瀹炵幇杈撳嚭鍒锋柊閫昏緫
            _logger?.LogInfo("  RefreshOutputs()", "DataSourceQueryService");
        }

        /// <inheritdoc/>
        public bool IsNodeRegistered(string nodeId)
        {
            return _nodeInfoCache.ContainsKey(nodeId);
        }

        #endregion

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

        /// <summary>
        /// 鑾峰彇鑺傜偣缁撴灉绫诲瀷
        /// </summary>
        /// <remarks>
        /// 鐢ㄤ簬璁捐鏃舵帹鏂緭鍑哄睘鎬с€俇I 灞傚疄鐜版椂锛屽彲浠ヤ粠宸ュ叿鍏冩暟鎹腑鑾峰彇 ResultType銆?
        /// </remarks>
        /// <param name="nodeId">鑺傜偣ID</param>
        /// <returns>缁撴灉绫诲瀷锛屽鏋滄棤娉曡幏鍙栧垯杩斿洖 null</returns>
        Type? GetResultType(string nodeId);
    }
}
