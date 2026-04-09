# -*- coding: utf-8 -*-
"""
DataSourceQueryService 修改脚本 - 简化版
目标：修改 CreateParentNodeInfo 方法
"""

import re
import os

# 文件路径
file_path = r"d:\MyWork\SunEyeVision_Dev-tool\src\Plugin.SDK\Execution\Parameters\DataSourceQueryService.cs"

# 读取文件内容（UTF-8 编码）
with open(file_path, 'r', encoding='utf-8') as f:
    content = f.read()

# 使用正则表达式匹配 CreateParentNodeInfo 方法
# 匹配从方法定义到结束大括号
pattern = r'(private ParentNodeInfo CreateParentNodeInfo\(string nodeId, int order\)\s*\{.*?\n\s*\};)'

def replace_method(match):
    """替换为新的方法实现"""
    new_method = """private ParentNodeInfo CreateParentNodeInfo(string nodeId, int order)
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

            // Step 1: Get ResultType from tool metadata
            var toolMetadata = ToolRegistry.GetToolMetadata(nodeType);
            Type? resultType = toolMetadata?.ResultType;

            // Step 2: Get execution result (only for filling actual values)
            var result = GetNodeResult(nodeId);

            if (result != null)
            {
                // Runtime: update execution status
                nodeInfo.ExecutionStatus = result.Status;
                nodeInfo.ExecutionTimeMs = result.ExecutionTimeMs;
                nodeInfo.ErrorMessage = result.ErrorMessage;

                _logger?.LogInfo($"  Runtime extraction: {nodeType} -> ResultType={resultType?.Name}", "DataSourceQueryService");
            }
            else
            {
                // Design time: mark as not executed
                nodeInfo.ExecutionStatus = ExecutionStatus.NotExecuted;
                nodeInfo.ExecutionTimeMs = 0;
                nodeInfo.ErrorMessage = null;

                _logger?.LogInfo($"  Design time inference: {nodeType} -> ResultType={resultType?.Name}", "DataSourceQueryService");
            }

            // Step 3: Unified extraction logic: extract all output properties from ResultType
            // Design time: result = null, extract property definitions only
            // Runtime: result != null, extract property definitions + actual values
            nodeInfo.ExtractOutputPropertiesFromType(resultType, result);

            return nodeInfo;
        }"""
    
    return new_method

# 执行替换
new_content = re.sub(pattern, replace_method, content, flags=re.DOTALL)

# 统计替换次数
if new_content != content:
    print("✅ Successfully replaced CreateParentNodeInfo method")
else:
    print("⚠️  No replacements made, method may already be updated")

# 写入文件（UTF-8 无 BOM 编码）
with open(file_path, 'w', encoding='utf-8', newline='') as f:
    f.write(new_content)

print("✅ DataSourceQueryService.cs modification completed")
