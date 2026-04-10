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
    /// 数据源查询服务实现
    /// </summary>
    /// <remarks>
    /// 提供查询父节点及其输出属性的能力。
    /// 
    /// 核心功能：
    /// 1. 基于工作流连接关系查找父节点
    /// 2. 从执行结果缓存提取输出属性
    /// 3. 支持类型过滤
    /// 4. 线程安全的结果缓存
    /// 
    /// 设计优化历史：
    /// - 支持可选注入 IPropertyMetadataProvider（设计时优化）
    /// - SDK 层不依赖 Infrastructure 层（依赖倒置原则）
    /// </remarks>
    public class DataSourceQueryService : IDataSourceQueryService
    {
        /// <summary>
        /// 节点结果缓存
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
        /// 工作流连接提供者（外部注入）
        /// </summary>
        private readonly IWorkflowConnectionProvider? _connectionProvider;

        /// <summary>
        /// 节点信息提供者（外部注入）
        /// </summary>
        private readonly INodeInfoProvider? _nodeInfoProvider;

        /// <summary>
        /// 属性元数据提供者(可选,用于设计时优化)
        /// </summary>
        private readonly IPropertyMetadataProvider? _metadataProvider;

        /// <summary>
        /// 输出变更订阅字典
        /// </summary>
        private readonly ConcurrentDictionary<string, List<Action<object?>>> _outputSubscriptions = new ConcurrentDictionary<string, List<Action<object?>>>();

        /// <summary>
        /// 当前节点ID
        /// </summary>
        private string? _currentNodeId;

        /// <summary>
        /// 当前节点ID
        /// </summary>
        public string? CurrentNodeId
        {
            get => _currentNodeId;
            set => _currentNodeId = value;
        }

        /// <summary>
        /// 创建数据源查询服务
        /// </summary>
        public DataSourceQueryService()
        {
        }

        /// <summary>
        /// 创建数据源查询服务（带依赖注入）
        /// </summary>
        /// <param name="connectionProvider">工作流连接提供者</param>
        /// <param name="nodeInfoProvider">节点信息提供者</param>
        public DataSourceQueryService(
            IWorkflowConnectionProvider? connectionProvider,
            INodeInfoProvider? nodeInfoProvider = null)
        {
            _connectionProvider = connectionProvider;
            _nodeInfoProvider = nodeInfoProvider;
        }

        /// <summary>
        /// 创建数据源查询服务（带依赖注入和 Logger）
        /// </summary>
        /// <param name="connectionProvider">工作流连接提供者</param>
        /// <param name="nodeInfoProvider">节点信息提供者</param>
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

        /// <summary>
        /// 创建数据源查询服务（带依赖注入和 Logger 和属性元数据提供者）
        /// </summary>
        /// <param name="connectionProvider">工作流连接提供者</param>
        /// <param name="nodeInfoProvider">节点信息提供者</param>
        /// <param name="logger">Logger</param>
        /// <param name="metadataProvider">属性元数据提供者(可选)</param>
        public DataSourceQueryService(
            IWorkflowConnectionProvider? connectionProvider,
            INodeInfoProvider? nodeInfoProvider,
            ILogger? logger = null,
            IPropertyMetadataProvider? metadataProvider = null)
        {
            _connectionProvider = connectionProvider;
            _nodeInfoProvider = nodeInfoProvider;
            _logger = logger;
            _metadataProvider = metadataProvider;
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
                // GetParentNodes 已经调用 GetNodeOutputProperties 填充了 OutputProperties
                var properties = parent.OutputProperties;
                _logger?.LogInfo($"  父节点 [{parent.NodeName}] 有 {properties.Count} 个输出属性", "DataSourceQueryService");

                // 类型过滤
                if (targetType != null)
                {
                    properties = parent.GetCompatibleProperties(targetType);
                    _logger?.LogInfo($"    过滤后剩余 {properties.Count} 个兼容属性", "DataSourceQueryService");
                }

                dataSources.AddRange(properties);

                foreach (var prop in properties)
                {
                    var treeNameInfo = string.IsNullOrEmpty(prop.FullTreeName) ? "" : $" [Tree: {prop.FullTreeName}]";
                    _logger?.LogInfo($"      - {prop.DisplayName}{treeNameInfo} ({prop.PropertyType.Name})", "DataSourceQueryService");
                }
            }

            _logger?.LogInfo($"总共返回 {dataSources.Count} 个数据源", "DataSourceQueryService");
            _logger?.LogInfo($"==================================================", "DataSourceQueryService");
            return dataSources;
        }

        /// <inheritdoc/>
        public GroupedDataSources GetAvailableDataSourcesGrouped(string nodeId, Type? targetType = null)
        {
            _logger?.LogInfo($"========== GetAvailableDataSourcesGrouped 开始 ==========", "DataSourceQueryService");
            _logger?.LogInfo($"查询节点ID: {nodeId}", "DataSourceQueryService");
            _logger?.LogInfo($"目标类型: {targetType?.Name ?? "Any"}", "DataSourceQueryService");

            var groupedSources = new GroupedDataSources();

            // 从全局输出类型注册表获取所有上游节点
            // 注意：这里需要根据实际连接关系筛选，而不是所有注册的节点
            var parentNodes = GetParentNodes(nodeId);

            _logger?.LogInfo($"遍历 {parentNodes.Count} 个父节点...", "DataSourceQueryService");

            foreach (var parent in parentNodes)
            {
                // GetParentNodes 已经调用 GetNodeOutputProperties 填充了 OutputProperties
                var properties = parent.OutputProperties;
                _logger?.LogInfo($"  父节点 [{parent.NodeName}] 有 {properties.Count} 个输出属性", "DataSourceQueryService");

                // 类型过滤
                if (targetType != null)
                {
                    properties = parent.GetCompatibleProperties(targetType);
                    _logger?.LogInfo($"    过滤后剩余 {properties.Count} 个兼容属性", "DataSourceQueryService");
                }

                // 按类型分类添加
                foreach (var prop in properties)
                {
                    var category = TypeCategoryMapper.GetCategory(prop.PropertyType);
                    groupedSources.AddDataSource(prop, category);
                    _logger?.LogInfo($"      - {prop.DisplayName} ({prop.PropertyType.Name}) -> {category}", "DataSourceQueryService");
                }
            }

            _logger?.LogInfo($"分组统计: {groupedSources.GetStatistics()}", "DataSourceQueryService");
            _logger?.LogInfo($"==================================================", "DataSourceQueryService");
            return groupedSources;
        }

        /// <inheritdoc/>
        public List<AvailableDataSource> GetNodeOutputProperties(string parentNodeId)
        {
            var properties = new List<AvailableDataSource>();
            var context = GetNodeContext(parentNodeId);

            if (context == null || context.ResultType == null)
            {
                _logger?.LogInfo($"节点上下文为空: {parentNodeId}", "DataSourceQueryService");
                return properties;
            }

            // 从变量池获取属性元数据（不再反射）
            // 注意：需要通过元数据提供者获取，避免直接依赖 Infrastructure 层
            var metadataList = _metadataProvider?.GetAllPropertyMetadata(context.NodeType);

            if (metadataList == null)
            {
                _logger?.LogWarning($"未找到属性元数据: {context.NodeType}", "DataSourceQueryService");
                return properties;
            }

            // 输出关键日志：TreeName 是否正确设置
            var hasTreeName = metadataList.Any(m => !string.IsNullOrEmpty(m.TreeName) && m.TreeName != m.DisplayName);
            _logger?.LogSuccess($"获取到 {metadataList.Count} 个属性元数据: {context.NodeType} (TreeName正确: {hasTreeName})", "DataSourceQueryService");

            foreach (var metadata in metadataList)
            {
                // 直接从元数据创建，不再反射
                object? currentValue = null;
                if (context.Result != null && metadata.PropertyType != null)
                {
                    // 运行时：填充实际值
                    var propInfo = context.ResultType.GetProperty(metadata.PropertyName);
                    currentValue = propInfo?.GetValue(context.Result);
                }

                var dataSource = new AvailableDataSource
                {
                    SourceNodeId = parentNodeId,
                    SourceNodeName = context.NodeName,
                    SourceNodeType = context.NodeType,
                    PropertyName = metadata.PropertyName,
                    DisplayName = metadata.DisplayName,
                    PropertyType = metadata.PropertyType,
                    CurrentValue = currentValue,
                    Unit = null,
                    Description = metadata.Description,
                    GroupName = context.NodeName,
                    FullTreeName = metadata.TreeName
                };

                // 📊 调试日志：输出 FullTreeName 赋值结果
                _logger?.LogInfo($"    [DataSourceQueryService] 属性 {metadata.PropertyName}: TreeName='{metadata.TreeName ?? "null"}', DisplayName='{metadata.DisplayName}'", "DataSourceQueryService");

                properties.Add(dataSource);
            }

            return properties;
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
        /// 从缓存获取节点执行结果
        /// </summary>
        private ToolResults? GetNodeResultFromCache(string nodeId)
        {
            _nodeResults.TryGetValue(nodeId, out var result);
            return result;
        }

        /// <inheritdoc/>
        public object? GetPropertyValue(string nodeId, string propertyName)
        {
            // 从节点输出中获取属性值
            if (_nodeOutputs.TryGetValue(nodeId, out var outputs))
            {
                if (outputs.TryGetValue(propertyName, out var value))
                {
                    return value;
                }
            }

            // 从节点结果中获取属性值
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
        /// 获取嵌套属性值
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
            // TODO: 实现节点数据刷新逻辑
            _logger?.LogInfo($"  RefreshNodeData({nodeId})", "DataSourceQueryService");
        }

        /// <inheritdoc/>
        public void RefreshAll()
        {
            // TODO: 实现所有数据刷新逻辑
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
            // TODO: 实现绑定值获取逻辑
            _logger?.LogInfo($"  GetCurrentBindingValue({nodeId}, {propertyName})", "DataSourceQueryService");
            return null;
        }

        /// <inheritdoc/>
        public string GetBindingDisplayPath(string nodeId, string propertyName, string? bindingPath = null)
        {
            // TODO: 实现绑定显示路径逻辑
            _logger?.LogInfo($"  GetBindingDisplayPath({nodeId}, {propertyName})", "DataSourceQueryService");
            return propertyName;
        }

        /// <inheritdoc/>
        public IDisposable SubscribeOutputChanged(string nodeId, string propertyName, string? bindingPath, Action<object?> callback)
        {
            // TODO: 实现输出变更订阅逻辑
            _logger?.LogInfo($"  SubscribeOutputChanged({nodeId}, {propertyName})", "DataSourceQueryService");

            // 返回一个空的订阅令牌
            return new SubscriptionToken(() => { });
        }

        /// <inheritdoc/>
        public void RefreshOutputs()
        {
            // TODO: 实现输出刷新逻辑
            _logger?.LogInfo("  RefreshOutputs()", "DataSourceQueryService");
        }

        /// <inheritdoc/>
        public bool IsNodeRegistered(string nodeId)
        {
            return _nodeInfoCache.ContainsKey(nodeId);
        }

        #endregion

        /// <summary>
        /// 订阅令牌
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
    /// 工作流连接提供者接口
    /// </summary>
    /// <remarks>
    /// 提供工作流节点连接关系查询能力。
    /// 由工作流引擎或UI层实现。
    /// </remarks>
    public interface IWorkflowConnectionProvider
    {
        /// <summary>
        /// 获取父节点ID列表
        /// </summary>
        /// <param name="nodeId">当前节点ID</param>
        /// <returns>父节点ID列表</returns>
        List<string> GetParentNodeIds(string nodeId);

        /// <summary>
        /// 获取子节点ID列表
        /// </summary>
        /// <param name="nodeId">当前节点ID</param>
        /// <returns>子节点ID列表</returns>
        List<string> GetChildNodeIds(string nodeId);

        /// <summary>
        /// 获取所有节点ID
        /// </summary>
        /// <returns>所有节点ID列表</returns>
        List<string> GetAllNodeIds();
    }

    /// <summary>
    /// 节点信息提供者接口
    /// </summary>
    /// <remarks>
    /// 提供节点基本信息查询能力。
    /// 由工作流引擎或UI层实现。
    /// </remarks>
    public interface INodeInfoProvider
    {
        /// <summary>
        /// 获取节点名称
        /// </summary>
        /// <param name="nodeId">节点ID</param>
        /// <returns>节点名称</returns>
        string GetNodeName(string nodeId);

        /// <summary>
        /// 获取节点类型
        /// </summary>
        /// <param name="nodeId">节点ID</param>
        /// <returns>节点类型</returns>
        string GetNodeType(string nodeId);

        /// <summary>
        /// 获取节点图标
        /// </summary>
        /// <param name="nodeId">节点ID</param>
        /// <returns>节点图标路径或名称</returns>
        string? GetNodeIcon(string nodeId);

        /// <summary>
        /// 检查节点是否存在
        /// </summary>
        /// <param name="nodeId">节点ID</param>
        /// <returns>是否存在</returns>
        bool NodeExists(string nodeId);

        /// <summary>
        /// 获取节点结果类型
        /// </summary>
        /// <remarks>
        /// 用于设计时推断输出属性。UI 层实现时，可以从工具元数据中获取 ResultType。
        /// </remarks>
        /// <param name="nodeId">节点ID</param>
        /// <returns>结果类型，如果无法获取则返回 null</returns>
        Type? GetResultType(string nodeId);
    }
}
