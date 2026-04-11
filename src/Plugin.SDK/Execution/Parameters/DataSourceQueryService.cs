using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SunEyeVision.Plugin.SDK.Execution.Results;
using SunEyeVision.Plugin.Infrastructure.Managers.Tool;
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
    /// </remarks>
    public class DataSourceQueryService : IDataSourceQueryService
    {
        /// <summary>
        /// 节点结果缓存
        /// </summary>
        private readonly ConcurrentDictionary<string, ToolResults> _nodeResults = new ConcurrentDictionary<string, ToolResults>();

        /// <summary>
        /// 工作流连接提供者（外部注入）
        /// </summary>
        private readonly IWorkflowConnectionProvider? _connectionProvider;

        /// <summary>
        /// 节点信息提供者（外部注入）
        /// </summary>
        private readonly INodeInfoProvider? _nodeInfoProvider;

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

        /// <inheritdoc/>
        public List<ParentNodeInfo> GetParentNodes(string nodeId)
        {
            var parentNodes = new List<ParentNodeInfo>();

            if (_connectionProvider == null)
            {
                // 如果没有连接提供者，返回空列表
                return parentNodes;
            }

            // 获取父节点ID列表
            var parentNodeIds = _connectionProvider.GetParentNodeIds(nodeId);
            int order = 0;

            foreach (var parentNodeId in parentNodeIds)
            {
                var nodeInfo = CreateParentNodeInfo(parentNodeId, order++);
                parentNodes.Add(nodeInfo);
            }

            return parentNodes;
        }

        /// <inheritdoc/>
        public List<AvailableDataSource> GetAvailableDataSources(string nodeId, Type? targetType = null)
        {
            var dataSources = new List<AvailableDataSource>();
            var parentNodes = GetParentNodes(nodeId);

            foreach (var parent in parentNodes)
            {
                var properties = parent.OutputProperties;

                // 类型过滤
                if (targetType != null)
                {
                    properties = parent.GetCompatibleProperties(targetType);
                }

                dataSources.AddRange(properties);
            }

            return dataSources;
        }

        /// <inheritdoc/>
        public List<AvailableDataSource> GetNodeOutputProperties(string parentNodeId)
        {
            var properties = new List<AvailableDataSource>();

            // 获取节点结果
            var result = GetNodeResult(parentNodeId);
            if (result != null)
            {
                // 从执行结果中提取属性
                return ExtractPropertiesFromResult(result, parentNodeId);
            }

            // 节点未执行，尝试从工具元数据提取
            if (_nodeInfoProvider != null)
            {
                return ExtractPropertiesFromMetadata(parentNodeId);
            }

            // 返回空属性列表
            return properties;
        }

        /// <summary>
        /// 从执行结果中提取输出属性
        /// </summary>
        private List<AvailableDataSource> ExtractPropertiesFromResult(ToolResults result, string nodeId)
        {
            var properties = new List<AvailableDataSource>();

            string nodeName = _nodeInfoProvider?.GetNodeName(nodeId) ?? nodeId;
            string nodeType = _nodeInfoProvider?.GetNodeType(nodeId) ?? "Unknown";

            var resultItems = result.GetResultItems();
            foreach (var item in resultItems)
            {
                var dataSource = new AvailableDataSource
                {
                    SourceNodeId = nodeId,
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

        /// <summary>
        /// 从工具元数据中提取输出属性定义（节点未执行时）
        /// </summary>
        private List<AvailableDataSource> ExtractPropertiesFromMetadata(string nodeId)
        {
            var properties = new List<AvailableDataSource>();

            string nodeName = _nodeInfoProvider?.GetNodeName(nodeId) ?? nodeId;
            string nodeType = _nodeInfoProvider?.GetNodeType(nodeId) ?? "Unknown";
            string nodeIcon = _nodeInfoProvider?.GetNodeIcon(nodeId);

            // 从工具注册表获取输出属性定义
            var toolProperties = ToolRegistry.GetToolOutputProperties(nodeType, nodeId);
            properties.AddRange(toolProperties);

            // 更新节点名称
            foreach (var prop in properties)
            {
                prop.SourceNodeName = nodeName;
                prop.GroupName = nodeName;
            }

            VisionLogger.Instance.Log(LogLevel.Info,
                $"[DataSourceQueryService.ExtractPropertiesFromMetadata] 从元数据提取输出属性 - nodeId={nodeId}, nodeType={nodeType}, 属性数量={properties.Count}",
                "DataSourceQueryService");

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

            // 尝试从结果项获取
            var resultItems = result.GetResultItems();
            var item = resultItems.FirstOrDefault(i => i.Name == propertyName);
            if (item != null)
                return item.Value;

            // 尝试通过反射获取属性
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

            // 尝试获取嵌套属性（如 Center.X）
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
            // 清除缓存，下次查询时重新获取
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

        /// <summary>
        /// 创建父节点信息
        /// </summary>
        /// <remarks>
        /// 统一调用 ExtractOutputProperties 方法，该方法内部会根据节点是否执行自动选择提取路径：
        /// - 已执行：从执行结果提取（包含实际值）
        /// - 未执行：从工具元数据提取（仅属性定义）
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

            // 获取执行结果（可能为null）
            var result = GetNodeResult(nodeId);

            // 统一调用 ExtractOutputProperties，内部会根据 result 是否为null自动选择提取路径
            nodeInfo.ExtractOutputProperties(result);

            return nodeInfo;
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
    }
}
