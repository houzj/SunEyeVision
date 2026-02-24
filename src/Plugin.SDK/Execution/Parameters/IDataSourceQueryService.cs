using System;
using System.Collections.Generic;
using SunEyeVision.Plugin.SDK.Execution.Results;

namespace SunEyeVision.Plugin.SDK.Execution.Parameters
{
    /// <summary>
    /// 数据源查询服务接口
    /// </summary>
    /// <remarks>
    /// 提供查询父节点及其输出属性的能力，用于参数绑定界面。
    /// 
    /// 核心功能：
    /// 1. 查询父节点列表
    /// 2. 查询可绑定数据源
    /// 3. 按类型过滤数据源
    /// 4. 获取节点执行结果缓存
    /// 
    /// 使用示例：
    /// <code>
    /// // 获取数据源查询服务实例
    /// IDataSourceQueryService queryService = ...;
    /// 
    /// // 查询父节点
    /// var parentNodes = queryService.GetParentNodes(currentNodeId);
    /// 
    /// // 查询可绑定数据源（按类型过滤）
    /// var doubleDataSources = queryService.GetAvailableDataSources(currentNodeId, typeof(double));
    /// 
    /// // 获取特定节点的执行结果
    /// var result = queryService.GetNodeResult(nodeId);
    /// </code>
    /// </remarks>
    public interface IDataSourceQueryService
    {
        /// <summary>
        /// 获取父节点列表
        /// </summary>
        /// <param name="nodeId">当前节点ID</param>
        /// <returns>父节点信息列表</returns>
        List<ParentNodeInfo> GetParentNodes(string nodeId);

        /// <summary>
        /// 获取可用数据源
        /// </summary>
        /// <param name="nodeId">当前节点ID</param>
        /// <param name="targetType">目标参数类型（可选，用于类型过滤）</param>
        /// <returns>可用数据源列表</returns>
        List<AvailableDataSource> GetAvailableDataSources(string nodeId, Type? targetType = null);

        /// <summary>
        /// 获取指定父节点的输出属性
        /// </summary>
        /// <param name="parentNodeId">父节点ID</param>
        /// <returns>输出属性列表</returns>
        List<AvailableDataSource> GetNodeOutputProperties(string parentNodeId);

        /// <summary>
        /// 获取节点执行结果
        /// </summary>
        /// <param name="nodeId">节点ID</param>
        /// <returns>执行结果（如果存在）</returns>
        ToolResults? GetNodeResult(string nodeId);

        /// <summary>
        /// 获取属性值
        /// </summary>
        /// <param name="nodeId">节点ID</param>
        /// <param name="propertyName">属性名称</param>
        /// <returns>属性值（如果存在）</returns>
        object? GetPropertyValue(string nodeId, string propertyName);

        /// <summary>
        /// 检查节点是否已执行
        /// </summary>
        /// <param name="nodeId">节点ID</param>
        /// <returns>是否已执行</returns>
        bool HasNodeExecuted(string nodeId);

        /// <summary>
        /// 刷新节点数据（重新从工作流获取）
        /// </summary>
        /// <param name="nodeId">节点ID</param>
        void RefreshNodeData(string nodeId);

        /// <summary>
        /// 刷新所有数据
        /// </summary>
        void RefreshAll();

        /// <summary>
        /// 设置节点结果缓存
        /// </summary>
        /// <param name="nodeId">节点ID</param>
        /// <param name="result">执行结果</param>
        void SetNodeResult(string nodeId, ToolResults result);

        /// <summary>
        /// 清除节点结果缓存
        /// </summary>
        /// <param name="nodeId">节点ID</param>
        void ClearNodeResult(string nodeId);

        /// <summary>
        /// 清除所有结果缓存
        /// </summary>
        void ClearAllResults();
    }

    /// <summary>
    /// 数据源查询服务扩展方法
    /// </summary>
    public static class DataSourceQueryServiceExtensions
    {
        /// <summary>
        /// 获取指定类型的可用数据源
        /// </summary>
        /// <typeparam name="T">目标类型</typeparam>
        /// <param name="service">数据源查询服务</param>
        /// <param name="nodeId">节点ID</param>
        /// <returns>可用数据源列表</returns>
        public static List<AvailableDataSource> GetAvailableDataSources<T>(this IDataSourceQueryService service, string nodeId)
        {
            return service.GetAvailableDataSources(nodeId, typeof(T));
        }

        /// <summary>
        /// 获取属性值并转换为指定类型
        /// </summary>
        /// <typeparam name="T">目标类型</typeparam>
        /// <param name="service">数据源查询服务</param>
        /// <param name="nodeId">节点ID</param>
        /// <param name="propertyName">属性名称</param>
        /// <returns>属性值</returns>
        public static T? GetPropertyValue<T>(this IDataSourceQueryService service, string nodeId, string propertyName)
        {
            var value = service.GetPropertyValue(nodeId, propertyName);
            if (value is T typedValue)
                return typedValue;

            if (value != null)
            {
                try
                {
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    return default;
                }
            }

            return default;
        }

        /// <summary>
        /// 检查绑定是否可用
        /// </summary>
        /// <param name="service">数据源查询服务</param>
        /// <param name="binding">参数绑定</param>
        /// <returns>是否可用</returns>
        public static bool IsBindingAvailable(this IDataSourceQueryService service, ParameterBinding binding)
        {
            if (binding.BindingType != BindingType.DynamicBinding)
                return true;

            if (string.IsNullOrEmpty(binding.SourceNodeId) || string.IsNullOrEmpty(binding.SourceProperty))
                return false;

            return service.HasNodeExecuted(binding.SourceNodeId);
        }

        /// <summary>
        /// 验证绑定配置
        /// </summary>
        /// <param name="service">数据源查询服务</param>
        /// <param name="binding">参数绑定</param>
        /// <returns>验证结果</returns>
        public static BindingValidationResult ValidateBinding(this IDataSourceQueryService service, ParameterBinding binding)
        {
            var result = binding.Validate();

            if (binding.BindingType == BindingType.DynamicBinding &&
                !string.IsNullOrEmpty(binding.SourceNodeId) &&
                !string.IsNullOrEmpty(binding.SourceProperty))
            {
                // 检查源节点是否存在
                if (!service.HasNodeExecuted(binding.SourceNodeId))
                {
                    result.Warnings.Add($"源节点 {binding.SourceNodeId} 尚未执行");
                }

                // 检查属性是否存在
                var properties = service.GetNodeOutputProperties(binding.SourceNodeId);
                var propertyExists = properties.Exists(p => p.PropertyName == binding.SourceProperty);
                if (!propertyExists)
                {
                    result.IsValid = false;
                    result.Errors.Add($"源节点 {binding.SourceNodeId} 不存在属性 {binding.SourceProperty}");
                }

                // 检查类型兼容性
                if (binding.TargetType != null)
                {
                    var prop = properties.Find(p => p.PropertyName == binding.SourceProperty);
                    if (prop != null && !IsTypeCompatible(prop.PropertyType, binding.TargetType))
                    {
                        result.Warnings.Add($"类型可能不兼容: {prop.PropertyType.Name} -> {binding.TargetType.Name}");
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// 检查类型兼容性
        /// </summary>
        private static bool IsTypeCompatible(Type sourceType, Type targetType)
        {
            if (sourceType == targetType)
                return true;

            if (targetType.IsAssignableFrom(sourceType))
                return true;

            // 数值类型互转
            if (IsNumericType(sourceType) && IsNumericType(targetType))
                return true;

            return false;
        }

        /// <summary>
        /// 检查是否为数值类型
        /// </summary>
        private static bool IsNumericType(Type type)
        {
            return type == typeof(int) || type == typeof(long) || type == typeof(short) ||
                   type == typeof(byte) || type == typeof(float) || type == typeof(double) ||
                   type == typeof(decimal);
        }
    }
}
