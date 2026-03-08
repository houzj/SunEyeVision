using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SunEyeVision.Plugin.SDK.Logging;

namespace SunEyeVision.Plugin.SDK.UI.Controls.Region.Models
{
    /// <summary>
    /// 工作流数据源提供者 - 从工作流引擎获取父节点输出
    /// </summary>
    public class WorkflowDataSourceProvider : IRegionDataSourceProvider
    {
        private readonly Dictionary<string, object?> _nodeOutputs = new();
        private readonly Dictionary<string, List<Action<object?>>> _subscriptions = new();
        private readonly Dictionary<string, NodeOutputInfo> _outputCache = new();
        private readonly ILogger _logger;

        // 前驱节点注册信息（支持显示未执行的节点）
        private readonly Dictionary<string, string> _parentNodeNames = new();
        private readonly Dictionary<string, string> _parentNodeTypes = new();

        /// <summary>
        /// 当前节点ID（用于过滤，不包含自己的输出）
        /// </summary>
        public string? CurrentNodeId { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        public WorkflowDataSourceProvider(ILogger? logger = null)
        {
            _logger = logger ?? new PluginLogger();
        }

        /// <summary>
        /// 是否在输出列表中包含当前节点
        /// 默认为 false（工具输入选择器场景：仅显示父节点）
        /// 设置为 true（图像显示选择器场景：显示当前节点 + 父节点）
        /// </summary>
        public bool IncludeCurrentNode { get; set; } = false;

        /// <summary>
        /// 工作流上下文（可选，用于更深入的集成）
        /// </summary>
        public object? WorkflowContext { get; set; }

        /// <summary>
        /// 获取父节点输出列表
        /// </summary>
        public IEnumerable<NodeOutputInfo> GetParentNodeOutputs(string? targetDataType = null)
        {
            var result = new List<NodeOutputInfo>();

            // 首先添加已注册的前驱节点（无论是否有输出数据）
            foreach (var kvp in _parentNodeNames)
            {
                var nodeId = kvp.Key;
                var nodeName = kvp.Value;

                // 根据 IncludeCurrentNode 决定是否跳过当前节点
                if (!IncludeCurrentNode && nodeId == CurrentNodeId)
                    continue;

                // 检查是否已有输出数据
                var hasOutput = _nodeOutputs.TryGetValue(nodeId, out var output);
                var nodeType = _parentNodeTypes.TryGetValue(nodeId, out var type) ? type : "Mat";

                var nodeInfo = new NodeOutputInfo
                {
                    NodeId = nodeId,
                    NodeName = nodeName,
                    OutputName = "Output",
                    DataType = nodeType,
                    CurrentValue = hasOutput ? output : null,
                    IsTypeMatched = IsTypeCompatible(nodeType, targetDataType),
                    Depth = 0,
                    HasExecuted = hasOutput && output != null
                };

                // 如果有输出且是复合对象，展开其属性
                if (hasOutput && output != null && IsComplexType(output))
                {
                    var properties = GetProperties(output);
                    foreach (var prop in properties)
                    {
                        var childInfo = BuildPropertyOutputInfo(nodeId, output, prop, targetDataType, 1);
                        if (childInfo != null)
                        {
                            nodeInfo.Children.Add(childInfo);
                        }
                    }
                }

                result.Add(nodeInfo);
            }

            // 添加有输出数据但未注册的节点（兼容旧逻辑）
            foreach (var kvp in _nodeOutputs)
            {
                var nodeId = kvp.Key;
                var output = kvp.Value;

                // 根据 IncludeCurrentNode 决定是否跳过当前节点
                if (!IncludeCurrentNode && nodeId == CurrentNodeId)
                    continue;

                // 如果已经通过注册添加过，跳过
                if (_parentNodeNames.ContainsKey(nodeId))
                    continue;

                // 构建节点信息
                var nodeInfo = BuildNodeOutputInfo(nodeId, output, targetDataType);
                if (nodeInfo != null)
                {
                    result.Add(nodeInfo);
                }
            }

            return result;
        }

        /// <summary>
        /// 构建节点输出信息
        /// </summary>
        private NodeOutputInfo? BuildNodeOutputInfo(string nodeId, object? output, string? targetDataType)
        {
            if (output == null)
                return null;

            var nodeInfo = new NodeOutputInfo
            {
                NodeId = nodeId,
                NodeName = GetNodeDisplayName(nodeId),
                OutputName = "Output",
                DataType = GetDataTypeName(output),
                CurrentValue = output,
                IsTypeMatched = IsTypeCompatible(GetDataTypeName(output), targetDataType),
                Depth = 0
            };

            // 如果是复合对象，展开其属性
            if (IsComplexType(output))
            {
                var properties = GetProperties(output);
                foreach (var prop in properties)
                {
                    var childInfo = BuildPropertyOutputInfo(nodeId, output, prop, targetDataType, 1);
                    if (childInfo != null)
                    {
                        nodeInfo.Children.Add(childInfo);
                    }
                }
            }

            return nodeInfo;
        }

        /// <summary>
        /// 构建属性输出信息
        /// </summary>
        private NodeOutputInfo? BuildPropertyOutputInfo(string nodeId, object parent, PropertyInfo property, string? targetDataType, int depth)
        {
            try
            {
                var value = property.GetValue(parent);
                var propertyPath = property.Name;

                var info = new NodeOutputInfo
                {
                    NodeId = nodeId,
                    NodeName = GetNodeDisplayName(nodeId),
                    OutputName = "Output",
                    PropertyPath = propertyPath,
                    DataType = GetDataTypeName(value),
                    CurrentValue = value,
                    IsTypeMatched = IsTypeCompatible(GetDataTypeName(value), targetDataType),
                    Depth = depth
                };

                // 如果是复合对象且深度不深，继续展开
                if (depth < 3 && value != null && IsComplexType(value))
                {
                    var properties = GetProperties(value);
                    foreach (var prop in properties)
                    {
                        var childInfo = BuildPropertyOutputInfo(nodeId, value, prop, targetDataType, depth + 1);
                        if (childInfo != null)
                        {
                            childInfo.PropertyPath = $"{propertyPath}.{prop.Name}";
                            info.Children.Add(childInfo);
                        }
                    }
                }

                return info;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 订阅输出值变更
        /// </summary>
        public IDisposable SubscribeOutputChanged(string nodeId, string outputName, string? propertyPath, Action<object?> onChanged)
        {
            var key = $"{nodeId}.{outputName}.{propertyPath ?? ""}";

            if (!_subscriptions.ContainsKey(key))
            {
                _subscriptions[key] = new List<Action<object?>>();
            }

            _subscriptions[key].Add(onChanged);

            // 立即触发一次当前值
            var currentValue = GetCurrentBindingValue(nodeId, outputName, propertyPath);
            onChanged(currentValue);

            return new SubscriptionToken(() =>
            {
                if (_subscriptions.ContainsKey(key))
                {
                    _subscriptions[key].Remove(onChanged);
                }
            });
        }

        /// <summary>
        /// 获取绑定显示路径
        /// </summary>
        public string GetBindingDisplayPath(string nodeId, string outputName, string? propertyPath)
        {
            var nodeName = GetNodeDisplayName(nodeId);
            return string.IsNullOrEmpty(propertyPath)
                ? $"{nodeName}.{outputName}"
                : $"{nodeName}.{outputName}.{propertyPath}";
        }

        /// <summary>
        /// 获取当前绑定值
        /// </summary>
        public object? GetCurrentBindingValue(string nodeId, string outputName, string? propertyPath)
        {
            if (!_nodeOutputs.TryGetValue(nodeId, out var output))
            {
                _logger.LogWarning($"获取绑定值：节点 {nodeId} 没有输出数据，返回null", "WorkflowDataSource");
                return null;
            }

            _logger.LogInfo($"获取绑定值：节点={nodeId}，输出={outputName}，属性路径={propertyPath ?? "(无)"}", "WorkflowDataSource");

            if (string.IsNullOrEmpty(propertyPath))
            {
                _logger.LogSuccess($"获取绑定值成功：节点={nodeId}，值类型={output?.GetType().Name ?? "null"}", "WorkflowDataSource");
                return output;
            }

            // 沿着属性路径获取值
            var propertyValue = GetPropertyValue(output, propertyPath);
            _logger.LogSuccess($"获取绑定值成功：节点={nodeId}，属性路径={propertyPath}，值类型={propertyValue?.GetType().Name ?? "null"}", "WorkflowDataSource");

            return propertyValue;
        }

        /// <summary>
        /// 刷新所有输出信息
        /// </summary>
        public void RefreshOutputs()
        {
            // 通知所有订阅者
            foreach (var kvp in _subscriptions)
            {
                var parts = kvp.Key.Split('.');
                if (parts.Length >= 2)
                {
                    var nodeId = parts[0];
                    var outputName = parts[1];
                    var propertyPath = parts.Length > 2 ? string.Join(".", parts.Skip(2)) : null;

                    var value = GetCurrentBindingValue(nodeId, outputName, propertyPath);

                    foreach (var callback in kvp.Value)
                    {
                        callback(value);
                    }
                }
            }
        }

        /// <summary>
        /// 更新节点输出（由工作流引擎调用）
        /// </summary>
        public void UpdateNodeOutput(string nodeId, object? output)
        {
            var outputType = output?.GetType().Name ?? "null";
            _logger.LogInfo($"节点输出更新：节点ID={nodeId}，输出类型={outputType}", "WorkflowDataSource");

            _nodeOutputs[nodeId] = output;

            // 通知订阅者
            var key = $"{nodeId}.Output.";
            if (_subscriptions.ContainsKey(key))
            {
                _logger.LogInfo($"节点输出更新：通知 {key} 的 {_subscriptions[key].Count} 个订阅者", "WorkflowDataSource");
                foreach (var callback in _subscriptions[key])
                {
                    callback(output);
                }
            }

            // 通知属性订阅者
            foreach (var kvp in _subscriptions)
            {
                if (kvp.Key.StartsWith($"{nodeId}.Output."))
                {
                    var propertyPath = kvp.Key.Substring($"{nodeId}.Output.".Length);
                    var value = GetPropertyValue(output, propertyPath);

                    _logger.LogSuccess($"节点输出更新：属性订阅 {nodeId}.Output.{propertyPath} 值={value}", "WorkflowDataSource");

                    foreach (var callback in kvp.Value)
                    {
                        callback(value);
                    }
                }
            }
        }

        /// <summary>
        /// 清除节点输出
        /// </summary>
        public void ClearNodeOutput(string nodeId)
        {
            _nodeOutputs.Remove(nodeId);
        }

        /// <summary>
        /// 清除所有输出
        /// </summary>
        public void ClearAllOutputs()
        {
            _nodeOutputs.Clear();
        }

        /// <summary>
        /// 注册前驱节点信息（不依赖执行结果，用于显示节点列表）
        /// </summary>
        /// <param name="nodeId">节点ID</param>
        /// <param name="nodeName">节点名称</param>
        /// <param name="nodeType">节点输出类型（默认为"Mat"）</param>
        public void RegisterParentNode(string nodeId, string nodeName, string nodeType = "Mat")
        {
            if (string.IsNullOrEmpty(nodeId))
                return;

            _parentNodeNames[nodeId] = nodeName ?? nodeId;
            _parentNodeTypes[nodeId] = nodeType ?? "Mat";
            _logger.LogInfo($"节点注册：节点ID={nodeId}，节点名称={nodeName}，节点类型={nodeType}", "WorkflowDataSource");
        }

        /// <summary>
        /// 检查节点是否有输出数据
        /// </summary>
        /// <param name="nodeId">节点ID</param>
        /// <returns>如果节点有输出数据返回true</returns>
        public bool HasNodeOutput(string nodeId)
        {
            return !string.IsNullOrEmpty(nodeId) && 
                   _nodeOutputs.ContainsKey(nodeId) && 
                   _nodeOutputs[nodeId] != null;
        }

        /// <summary>
        /// 检查节点是否已注册（存在连接关系）
        /// </summary>
        /// <param name="nodeId">节点ID</param>
        /// <returns>如果节点已注册返回true</returns>
        public bool IsNodeRegistered(string nodeId)
        {
            return !string.IsNullOrEmpty(nodeId) && _parentNodeNames.ContainsKey(nodeId);
        }

        #region 辅助方法

        /// <summary>
        /// 获取节点显示名称
        /// </summary>
        private string GetNodeDisplayName(string nodeId)
        {
            // 优先使用注册的前驱节点名称
            if (_parentNodeNames.TryGetValue(nodeId, out var registeredName))
            {
                return registeredName;
            }

            // 简化实现，实际应从工作流获取
            return nodeId.StartsWith("Node_") ? nodeId.Substring(5) : nodeId;
        }

        /// <summary>
        /// 获取数据类型名称
        /// </summary>
        private string GetDataTypeName(object? value)
        {
            if (value == null)
                return "null";

            var type = value.GetType();
            return type.Name;
        }

        /// <summary>
        /// 判断类型是否兼容
        /// </summary>
        private bool IsTypeCompatible(string sourceType, string? targetType)
        {
            if (string.IsNullOrEmpty(targetType))
                return true;

            // 完全匹配
            if (sourceType == targetType)
                return true;

            // 数值类型兼容
            var numericTypes = new[] { "double", "float", "int", "long", "short", "byte" };
            if (numericTypes.Contains(sourceType.ToLower()) && numericTypes.Contains(targetType.ToLower()))
                return true;

            return false;
        }

        /// <summary>
        /// 判断是否为复合类型
        /// </summary>
        private bool IsComplexType(object? value)
        {
            if (value == null)
                return false;

            var type = value.GetType();

            // 基本类型不算复合类型
            if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal))
                return false;

            // 数组、列表等集合类型
            if (type.IsArray || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>)))
                return false;

            return true;
        }

        /// <summary>
        /// 获取属性列表
        /// </summary>
        private IEnumerable<PropertyInfo> GetProperties(object obj)
        {
            return obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead);
        }

        /// <summary>
        /// 根据属性路径获取值
        /// </summary>
        private object? GetPropertyValue(object? obj, string propertyPath)
        {
            if (obj == null || string.IsNullOrEmpty(propertyPath))
                return obj;

            var properties = propertyPath.Split('.');
            var current = obj;

            foreach (var prop in properties)
            {
                if (current == null)
                    return null;

                var property = current.GetType().GetProperty(prop);
                if (property == null)
                    return null;

                current = property.GetValue(current);
            }

            return current;
        }

        #endregion

        /// <summary>
        /// 订阅令牌
        /// </summary>
        private class SubscriptionToken : IDisposable
        {
            private readonly Action _dispose;
            private bool _disposed;

            public SubscriptionToken(Action dispose)
            {
                _dispose = dispose;
            }

            public void Dispose()
            {
                if (!_disposed)
                {
                    _disposed = true;
                    _dispose();
                }
            }
        }
    }
}
