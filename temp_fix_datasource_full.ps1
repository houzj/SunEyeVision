# DataSourceQueryService 完整修改脚本
# 目标：修改 CreateParentNodeInfo 方法，使用统一的提取逻辑

$filePath = "d:\MyWork\SunEyeVision_Dev-tool\src\Plugin.SDK\Execution\Parameters\DataSourceQueryService.cs"

# 读取文件所有行
$lines = Get-Content $filePath -Encoding UTF8

# 查找 CreateParentNodeInfo 方法的开始和结束行号
$startLine = -1
$endLine = -1

for ($i = 0; $i -lt $lines.Count; $i++)
{
    if ($lines[$i] -match "private ParentNodeInfo CreateParentNodeInfo\(string nodeId, int order\)" -and $startLine -eq -1)
    {
        $startLine = $i
    }
    if ($startLine -ne -1 -and $lines[$i] -match "^        \}$" -and $i -gt $startLine + 10)
    {
        $endLine = $i
        break
    }
}

Write-Host "找到 CreateParentNodeInfo 方法：第 $($startLine+1) 行到第 $($endLine+1) 行" -ForegroundColor Cyan

# 新方法代码（替换掉）
$newMethod = @"
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

                _logger?.LogInfo(`"  运行时提取: {nodeType} → ResultType={resultType?.Name}`", `"DataSourceQueryService`");
            }
            else
            {
                // 设计时：标记为未执行
                nodeInfo.ExecutionStatus = ExecutionStatus.NotExecuted;
                nodeInfo.ExecutionTimeMs = 0;
                nodeInfo.ErrorMessage = null;

                _logger?.LogInfo(`"  设计时推断: {nodeType} → ResultType={resultType?.Name}`", `"DataSourceQueryService`");
            }

            // 步骤3：统一提取逻辑：从 ResultType 反射获取所有输出属性
            // 设计时：result = null, 提取属性定义
            // 运行时：result != null, 提取属性定义 + 实际值
            nodeInfo.ExtractOutputPropertiesFromType(resultType, result);

            return nodeInfo;
        }
"@.Trim().Split("`n")

# 构建新文件内容
$newLines = New-Object System.Collections.Generic.List[string]
for ($i = 0; $i -lt $lines.Count; $i++)
{
    if ($i -lt $startLine -or $i -gt $endLine)
    {
        # 保留不在替换范围内的行
        $newLines.Add($lines[$i])
    }
    elseif ($i -eq $startLine)
    {
        # 在方法开始位置插入新方法
        $newLines.AddRange($newMethod)
    }
}

# 写入文件
[System.IO.File]::WriteAllLines($filePath, $newLines, [System.Text.UTF8Encoding]::new($false))

Write-Host "✅ DataSourceQueryService.cs 修改完成" -ForegroundColor Green
