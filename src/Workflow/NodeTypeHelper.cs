using System;
using SunEyeVision.Plugin.Infrastructure.Managers.Tool;
using SunEyeVision.Core.Models;

namespace SunEyeVision.Workflow
{
    /// <summary>
    /// 节点类型辅助类 - 从工具类型推断节点类型
    /// </summary>
    /// <remarks>
    /// 推断规则：
    /// - 图像采集/相机相关节点 → NodeType.Start
    /// - 条件/分支相关节点 → NodeType.Condition
    /// - 子程序相关节点 → NodeType.Subroutine
    /// - 其他节点 → NodeType.Algorithm
    /// </remarks>
    public static class NodeTypeHelper
    {
        /// <summary>
        /// 从工具ID推断节点类型
        /// </summary>
        /// <param name="toolId">工具ID</param>
        /// <returns>推断的节点类型</returns>
        public static NodeType InferNodeType(string toolId)
        {
            var metadata = ToolRegistry.GetToolMetadata(toolId);
            if (metadata?.AlgorithmType != null)
            {
                return DetermineNodeType(metadata.AlgorithmType);
            }

            return NodeType.Algorithm;
        }

        /// <summary>
        /// 从工具类型推断节点类型
        /// </summary>
        /// <param name="toolType">工具类型</param>
        /// <returns>推断的节点类型</returns>
        public static NodeType DetermineNodeType(Type toolType)
        {
            string typeName = toolType.Name.ToLowerInvariant();

            if (typeName.Contains("imagecapture") || 
                typeName.Contains("imageacquisition") || 
                typeName.Contains("camera") ||
                typeName.Contains("image_load") ||
                typeName.Contains("imageload"))
            {
                return NodeType.Start;
            }

            if (typeName.Contains("condition") || 
                typeName.Contains("switch") ||
                typeName.Contains("if") ||
                typeName.Contains("branch"))
            {
                return NodeType.Condition;
            }

            if (typeName.Contains("subroutine") || 
                typeName.Contains("workflow") ||
                typeName.Contains("procedure"))
            {
                return NodeType.Subroutine;
            }

            return NodeType.Algorithm;
        }

        /// <summary>
        /// 从算法类型名称推断节点类型
        /// </summary>
        /// <param name="algorithmType">算法类型名称</param>
        /// <returns>推断的节点类型</returns>
        public static NodeType InferNodeTypeFromAlgorithmType(string algorithmType)
        {
            if (string.IsNullOrWhiteSpace(algorithmType))
                return NodeType.Algorithm;

            string typeName = algorithmType.ToLowerInvariant();

            if (typeName.Contains("imagecapture") || 
                typeName.Contains("imageacquisition") || 
                typeName.Contains("camera") ||
                typeName.Contains("image_load") ||
                typeName.Contains("imageload") ||
                typeName.Contains("imagecapturetool") ||
                typeName.Contains("image_load_tool"))
            {
                return NodeType.Start;
            }

            if (typeName.Contains("condition") || 
                typeName.Contains("switch") ||
                typeName.Contains("if") ||
                typeName.Contains("branch"))
            {
                return NodeType.Condition;
            }

            if (typeName.Contains("subroutine") || 
                typeName.Contains("workflow") ||
                typeName.Contains("procedure"))
            {
                return NodeType.Subroutine;
            }

            return NodeType.Algorithm;
        }
    }
}
