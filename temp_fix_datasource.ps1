# DataSourceQueryService 修改脚本
# 目标：修改 CreateParentNodeInfo 方法，使用统一的提取逻辑

$filePath = "d:\MyWork\SunEyeVision_Dev-tool\src\Plugin.SDK\Execution\Parameters\DataSourceQueryService.cs"

# 读取文件内容
$content = Get-Content $filePath -Raw -Encoding UTF8

# 检查是否已经添加了 using 语句
if ($content -notmatch "using SunEyeVision.Plugin.Infrastructure.Managers.Tool;")
{
    # 添加 using 语句（在现有的 using 后面）
    $content = $content -replace "(using SunEyeVision.Plugin.SDK.Logging;)", "`$1`r`nusing SunEyeVision.Plugin.Infrastructure.Managers.Tool;"
    Write-Host "✅ 添加 using 语句" -ForegroundColor Green
}

# 查找并替换 CreateParentNodeInfo 方法
$oldMethodPattern = @'
        private ParentNodeInfo CreateParentNodeInfo\(string nodeId, int order\)
        \{
            string nodeName = _nodeInfoProvider\?\.GetNodeName\(nodeId\) \?\? nodeId;
            string nodeType = _nodeInfoProvider\?\.GetNodeType\(nodeId\) \?\? "Unknown";
            string\? nodeIcon = _nodeInfoProvider\?\.GetNodeIcon\(nodeId\);

            var nodeInfo = new ParentNodeInfo
            \{
                NodeId = nodeId,
                NodeName = nodeName,
                NodeType = nodeType,
                NodeIcon = nodeIcon,
                ConnectionOrder = order
            \};

            .*?// 鑾峰彇鎵ц繙鍙粨鏋滅紦瀛樻彁鍙栬緭鍑哄睘鎬\?
            var result = GetNodeResult\(nodeId\);
            if \(result != null\)
            \{
                nodeInfo\.ExecutionStatus = result\.Status;
                nodeInfo\.ExecutionTimeMs = result\.ExecutionTimeMs;
                nodeInfo\.ErrorMessage = result\.ErrorMessage;

                .*?// 鎻愬彇杈撳嚭灞炴€\?
                nodeInfo\.ExtractOutputProperties\(result\);
            \}

            return nodeInfo;
        \}
'@

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
        }"@

# 使用正则表达式替换
if ($content -match $oldMethodPattern)
{
    $content = $content -replace $oldMethodPattern, $newMethod
    Write-Host "✅ 替换 CreateParentNodeInfo 方法" -ForegroundColor Green
}
else
{
    Write-Host "⚠️  未找到 CreateParentNodeInfo 方法，可能已经被修改" -ForegroundColor Yellow
}

# 写入文件
[System.IO.File]::WriteAllText($filePath, $content, [System.Text.UTF8Encoding]::new($false))

Write-Host "✅ DataSourceQueryService.cs 修改完成" -ForegroundColor Green
