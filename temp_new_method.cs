// 新的 CreateParentNodeInfo 方法，用于替换 DataSourceQueryService.cs 中的旧方法
// 直接复制下面的内容到文件中

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
        }
