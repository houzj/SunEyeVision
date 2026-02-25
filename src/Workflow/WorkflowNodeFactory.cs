using System;
using System.Collections.Generic;
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.Infrastructure.Managers.Tool;
using SunEyeVision.Plugin.SDK.Metadata;

namespace SunEyeVision.Workflow
{
    /// <summary>
    /// 工作流节点工厂 - 从ToolRegistry创建工作流节点
    /// </summary>
    public static class WorkflowNodeFactory
    {
        /// <summary>
        /// 从工具元数据创建算法节点
        /// </summary>
        /// <param name="toolId">工具ID(统一标识符)</param>
        /// <param name="nodeId">节点ID</param>
        /// <param name="nodeName">节点名称</param>
        /// <param name="parameters">算法参数</param>
        /// <param name="enableCaching">是否启用缓存</param>
        /// <param name="enableRetry">是否启用重试</param>
        /// <returns>创建的AlgorithmNode,如果工具不存在则返回null</returns>
        public static AlgorithmNode? CreateNode(
            string toolId,
            string nodeId,
            string nodeName,
            AlgorithmParameters? parameters = null,
            bool enableCaching = true,
            bool enableRetry = false)
        {
            return CreateAlgorithmNode(toolId, nodeId, nodeName, parameters, enableCaching, enableRetry);
        }

        /// <summary>
        /// 从工具ID创建AlgorithmNode
        /// </summary>
        /// <param name="toolId">工具ID(统一标识符)</param>
        /// <param name="nodeId">节点ID</param>
        /// <param name="nodeName">节点名称</param>
        /// <param name="parameters">算法参数</param>
        /// <param name="enableCaching">是否启用缓存</param>
        /// <param name="enableRetry">是否启用重试</param>
        /// <returns>创建的AlgorithmNode,如果工具不存在则返回null</returns>
        public static AlgorithmNode? CreateAlgorithmNode(
            string toolId,
            string nodeId,
            string nodeName,
            AlgorithmParameters? parameters = null,
            bool enableCaching = true,
            bool enableRetry = false)
        {
            try
            {
                // 从ToolRegistry获取工具元数据
                var metadata = ToolRegistry.GetToolMetadata(toolId);
                if (metadata == null)
                {
                    Console.WriteLine($"[WorkflowNodeFactory] 工具不存在: {toolId}");
                    return null;
                }

                // 获取工具插件
                var toolPlugin = ToolRegistry.GetToolPlugin(toolId);
                if (toolPlugin == null)
                {
                    Console.WriteLine($"[WorkflowNodeFactory] 无法获取工具插件: {toolId}");
                    return null;
                }

                // 使用原始工具插件
                var decoratedPlugin = toolPlugin;

                // 创建处理器实例
                var processor = decoratedPlugin.CreateToolInstance(toolId);

                // 设置参数
                if (parameters == null)
                {
                    parameters = toolPlugin.GetDefaultParameters(toolId);
                }

                // 创建AlgorithmNode
                var algorithmNode = new AlgorithmNode(nodeId, nodeName, processor)
                {
                    Parameters = parameters
                };

                Console.WriteLine($"[WorkflowNodeFactory] 创建AlgorithmNode: {nodeName} (ToolId: {toolId}, 缓存: {enableCaching}, 重试: {enableRetry})");

                return algorithmNode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WorkflowNodeFactory] 创建AlgorithmNode失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 从工具ID批量创建AlgorithmNode
        /// </summary>
        /// <param name="toolConfig">工具配置列表</param>
        /// <returns>创建的AlgorithmNode列表</returns>
        public static List<AlgorithmNode> CreateAlgorithmNodes(IEnumerable<ToolNodeConfig> toolConfig)
        {
            var nodes = new List<AlgorithmNode>();

            foreach (var config in toolConfig)
            {
                var node = CreateAlgorithmNode(
                    config.ToolId,
                    config.NodeId,
                    config.NodeName,
                    config.Parameters,
                    config.EnableCaching,
                    config.EnableRetry);

                if (node != null)
                {
                    nodes.Add(node);
                }
            }

            return nodes;
        }

        // Start 节点已废弃，不再需要特殊节点类型
        // 执行顺序由连线关系决定，基于入度自动识别入口节点

        /// <summary>
        /// 创建子程序节点
        /// </summary>
        public static SubroutineNode CreateSubroutineNode(
            string nodeId,
            string nodeName,
            AlgorithmParameters? parameters = null)
        {
            var node = new SubroutineNode(nodeId, nodeName);
            if (parameters != null)
            {
                node.Parameters = parameters;
            }
            return node;
        }

        /// <summary>
        /// 创建条件节点
        /// </summary>
        public static ConditionNode CreateConditionNode(
            string nodeId,
            string nodeName,
            string conditionExpression,
            AlgorithmParameters? parameters = null)
        {
            var node = new ConditionNode(nodeId, nodeName);
            node.SetExpressionCondition(conditionExpression);

            if (parameters != null)
            {
                node.Parameters = parameters;
            }
            return node;
        }

        /// <summary>
        /// 验证工具是否可用
        /// </summary>
        public static bool ValidateTool(string toolId)
        {
            var metadata = ToolRegistry.GetToolMetadata(toolId);
            return metadata != null && metadata.IsEnabled;
        }

        /// <summary>
        /// 获取工具元数据
        /// </summary>
        public static ToolMetadata? GetToolMetadata(string toolId)
        {
            return ToolRegistry.GetToolMetadata(toolId);
        }
    }

    /// <summary>
    /// 工具节点配置
    /// </summary>
    public class ToolNodeConfig
    {
        /// <summary>
        /// 工具ID(统一标识符)
        /// </summary>
        public string ToolId { get; set; } = string.Empty;

        /// <summary>
        /// 节点ID
        /// </summary>
        public string NodeId { get; set; } = string.Empty;

        /// <summary>
        /// 节点名称
        /// </summary>
        public string NodeName { get; set; } = string.Empty;

        /// <summary>
        /// 算法参数
        /// </summary>
        public AlgorithmParameters? Parameters { get; set; }

        /// <summary>
        /// 是否启用缓存
        /// </summary>
        public bool EnableCaching { get; set; } = true;

        /// <summary>
        /// 是否启用重试
        /// </summary>
        public bool EnableRetry { get; set; } = false;
    }
}
