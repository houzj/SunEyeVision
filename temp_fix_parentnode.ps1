# ParentNodeInfo 修改脚本
# 目标：将 ExtractOutputProperties 方法改为 ExtractOutputPropertiesFromType，支持统一的设计时和运行时逻辑

$filePath = "d:\MyWork\SunEyeVision_Dev-tool\src\Plugin.SDK\Execution\Parameters\ParentNodeInfo.cs"

# 读取文件内容
$content = Get-Content $filePath -Raw -Encoding UTF8

# 旧方法：从执行结果提取输出属性
$oldMethod = @"
        /// <summary>
        /// 从执行结果提取输出属性
        /// </summary>
        public static void ExtractOutputProperties(this ParentNodeInfo nodeInfo, ToolResults? result)
        {
            nodeInfo.LastResult = result;

            if (result == null)
                return;

            nodeInfo.ExecutionStatus = result.Status;
            nodeInfo.ExecutionTimeMs = result.ExecutionTimeMs;
            nodeInfo.ErrorMessage = result.ErrorMessage;

            // 提取结果项
            var resultItems = result.GetResultItems();
            foreach (var item in resultItems)
            {
                var dataSource = new AvailableDataSource
                {
                    SourceNodeId = nodeInfo.NodeId,
                    SourceNodeName = nodeInfo.NodeName,
                    SourceNodeType = nodeInfo.NodeType,
                    PropertyName = item.Name,
                    DisplayName = item.DisplayName ?? item.Name,
                    PropertyType = item.Value?.GetType() ?? typeof(object),
                    CurrentValue = item.Value,
                    Unit = item.Unit,
                    Description = item.Description,
                    GroupName = nodeInfo.NodeName
                };

                nodeInfo.OutputProperties.Add(dataSource);
            }
        }"@

# 新方法：从 ResultType 反射提取输出属性
$newMethod = @"
        /// <summary>
        /// 从 ResultType 反射提取输出属性
        /// </summary>
        /// <remarks>
        /// 统一的设计时和运行时提取逻辑：
        /// - 设计时: result = null, 只提取属性定义
        /// - 运行时: result != null, 提取属性定义 + 实际值
        /// </remarks>
        public static void ExtractOutputPropertiesFromType(
            this ParentNodeInfo nodeInfo, 
            Type? resultType,
            ToolResults? result)
        {
            if (resultType == null)
            {
                return;
            }

            // 只处理 ToolResults 的派生类
            if (!typeof(ToolResults).IsAssignableFrom(resultType))
            {
                return;
            }

            // 通过反射分析 ResultType 的公共属性
            var properties = resultType.GetProperties(
                BindingFlags.Public | 
                BindingFlags.Instance | 
                BindingFlags.DeclaredOnly);

            foreach (var prop in properties)
            {
                // 跳过索引属性
                if (prop.GetIndexParameters().Length > 0)
                {
                    continue;
                }

                // 跳过继承自 ToolResults 的属性
                if (prop.DeclaringType != resultType)
                {
                    continue;
                }

                // 跳过特殊的属性
                if (prop.Name == "Status" || 
                    prop.Name == "ErrorMessage" || 
                    prop.Name == "ExecutionTimeMs" ||
                    prop.Name == "Timestamp" ||
                    prop.Name == "ToolName" ||
                    prop.Name == "ToolId")
                {
                    continue;
                }

                var dataSource = new AvailableDataSource
                {
                    SourceNodeId = nodeInfo.NodeId,
                    SourceNodeName = nodeInfo.NodeName,
                    SourceNodeType = nodeInfo.NodeType,
                    PropertyName = prop.Name,
                    DisplayName = GetDisplayName(prop.Name),
                    PropertyType = prop.PropertyType,
                    // 关键: 运行时填充实际值，设计时为 null
                    CurrentValue = result != null ? prop.GetValue(result) : null,
                    Unit = null,
                    Description = GetPropertyDescription(prop),
                    GroupName = nodeInfo.NodeName
                };

                nodeInfo.OutputProperties.Add(dataSource);
            }
        }"@

# 执行替换
$newContent = $content.Replace($oldMethod, $newMethod)

# 写入文件
[System.IO.File]::WriteAllText($filePath, $newContent, [System.Text.UTF8Encoding]::new($false))

Write-Host "✅ ParentNodeInfo.cs 修改完成" -ForegroundColor Green
