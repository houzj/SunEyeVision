# -*- coding: utf-8 -*-
"""
DataSourceQueryService 修改脚本
目标：修改 CreateParentNodeInfo 方法，使用统一的提取逻辑
"""

import os

# 文件路径
file_path = r"d:\MyWork\SunEyeVision_Dev-tool\src\Plugin.SDK\Execution\Parameters\DataSourceQueryService.cs"

# 读取文件内容（UTF-8 编码）
with open(file_path, 'r', encoding='utf-8') as f:
    content = f.read()

# 旧方法（使用更精确的匹配）
old_method = """        private ParentNodeInfo CreateParentNodeInfo(string nodeId, int order)
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

            // 鑾峰彇鎵ц繙鍙粨鏋滅紦瀛樻彁鍙栬緭鍑哄睘鎬?
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
        }"""

# 新方法
new_method = """        /// <summary>
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

            // 步骤1：从工具元数据获取 ResultType
            var toolMetadata = ToolRegistry.GetToolMetadata(nodeType);
            Type? resultType = toolMetadata?.ResultType;

            // 步骤2：获取执行结果（仅用于填充实际值）
            var result = GetNodeResult(nodeId);

            if (result != null)
            {
                // 运行时：更新执行状态
                nodeInfo.ExecutionStatus = result.Status;
                nodeInfo.ExecutionTimeMs = result.ExecutionTimeMs;
                nodeInfo.ErrorMessage = result.ErrorMessage;

                _logger?.LogInfo($"  运行时提取: {nodeType} -> ResultType={resultType?.Name}", "DataSourceQueryService");
            }
            else
            {
                // 设计时：标记为未执行
                nodeInfo.ExecutionStatus = ExecutionStatus.NotExecuted;
                nodeInfo.ExecutionTimeMs = 0;
                nodeInfo.ErrorMessage = null;

                _logger?.LogInfo($"  设计时推断: {nodeType} -> ResultType={resultType?.Name}", "DataSourceQueryService");
            }

            // 步骤3：统一提取逻辑：从 ResultType 反射获取所有输出属性
            // 设计时：result = null, 提取属性定义
            // 运行时：result != null, 提取属性定义 + 实际值
            nodeInfo.ExtractOutputPropertiesFromType(resultType, result);

            return nodeInfo;
        }"""

# 检查是否找到旧方法
if old_method in content:
    # 执行替换
    content = content.replace(old_method, new_method)
    print("✅ 成功替换 CreateParentNodeInfo 方法")
else:
    print("⚠️  未找到 CreateParentNodeInfo 方法，可能已经被修改")

# 写入文件（UTF-8 无 BOM 编码）
with open(file_path, 'w', encoding='utf-8', newline='') as f:
    f.write(content)

print("✅ DataSourceQueryService.cs 修改完成")
