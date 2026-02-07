using System;
using System.Collections.Generic;
using System.Linq;
using SunEyeVision.Interfaces;
using SunEyeVision.Models;

namespace SunEyeVision.Workflow
{
    /// <summary>
    /// 端口连接 - 记录源节点端口到目标节点端口的连接
    /// </summary>
    public class PortConnection
    {
        /// <summary>
        /// 源节点ID
        /// </summary>
        public string SourceNodeId { get; set; }

        /// <summary>
        /// 源端口 (如: "output", "left", "right", "top", "bottom")
        /// </summary>
        public string SourcePort { get; set; }

        /// <summary>
        /// 目标节点ID
        /// </summary>
        public string TargetNodeId { get; set; }

        /// <summary>
        /// 目标端口 (如: "input", "left", "right", "top", "bottom")
        /// </summary>
        public string TargetPort { get; set; }
    }

    /// <summary>
    /// 执行链 - 由Start节点驱动的独立执行单元
    /// </summary>
    public class ExecutionChain
    {
        /// <summary>
        /// 执行链唯一标识
        /// </summary>
        public string ChainId { get; set; }

        /// <summary>
        /// 起始节点ID
        /// </summary>
        public string StartNodeId { get; set; }

        /// <summary>
        /// 执行链中的节点ID列表（按拓扑顺序）
        /// </summary>
        public List<string> NodeIds { get; set; }

        /// <summary>
        /// 执行链中的所有依赖（用于跨链同步）
        /// </summary>
        public List<ChainDependency> Dependencies { get; set; }

        public ExecutionChain()
        {
            NodeIds = new List<string>();
            Dependencies = new List<ChainDependency>();
        }
    }

    /// <summary>
    /// 执行链依赖关系
    /// </summary>
    public class ChainDependency
    {
        /// <summary>
        /// 依赖的源链ID
        /// </summary>
        public string SourceChainId { get; set; }

        /// <summary>
        /// 源节点ID
        /// </summary>
        public string SourceNodeId { get; set; }

        /// <summary>
        /// 本链中依赖该源的节点ID
        /// </summary>
        public string TargetNodeId { get; set; }
    }

    /// <summary>
    /// Workflow
    /// </summary>
    public class Workflow
    {
        /// <summary>
        /// Workflow ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Workflow name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Workflow description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Node list
        /// </summary>
        public List<WorkflowNode> Nodes { get; set; }

        /// <summary>
        /// Node connections (source node ID -> target node ID list)
        /// </summary>
        public Dictionary<string, List<string>> Connections { get; set; }

        /// <summary>
        /// 端口级连接列表 - 精确记录哪个端口连接到哪个端口
        /// </summary>
        public List<PortConnection> PortConnections { get; set; }

        /// <summary>
        /// Logger
        /// </summary>
        private ILogger Logger { get; set; }

        /// <summary>
        /// Set logger (internal use for deserialization)
        /// </summary>
        internal void SetLogger(ILogger logger)
        {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Workflow(string id, string name, ILogger logger)
        {
            Id = id;
            Name = name;
            Logger = logger;
            Nodes = new List<WorkflowNode>();
            Connections = new Dictionary<string, List<string>>();
            PortConnections = new List<PortConnection>();
        }

        /// <summary>
        /// Add node
        /// </summary>
        public void AddNode(WorkflowNode node)
        {
            Nodes.Add(node);
            Logger.LogInfo($"Workflow {Name} added node: {node.Name}");
        }

        /// <summary>
        /// Remove node
        /// </summary>
        public void RemoveNode(string nodeId)
        {
            var node = Nodes.FirstOrDefault(n => n.Id == nodeId);
            if (node != null)
            {
                Nodes.Remove(node);
                Connections.Remove(nodeId);

                // Remove connections from other nodes to this node
                foreach (var kvp in Connections)
                {
                    kvp.Value.Remove(nodeId);
                }

                // 移除该节点的端口连接
                PortConnections.RemoveAll(pc => pc.SourceNodeId == nodeId || pc.TargetNodeId == nodeId);

                Logger.LogInfo($"Workflow {Name} removed node: {node.Name}");
            }
        }

        /// <summary>
        /// Connect nodes
        /// </summary>
        public void ConnectNodes(string sourceNodeId, string targetNodeId)
        {
            if (!Connections.ContainsKey(sourceNodeId))
            {
                Connections[sourceNodeId] = new List<string>();
            }

            if (!Connections[sourceNodeId].Contains(targetNodeId))
            {
                Connections[sourceNodeId].Add(targetNodeId);
                Logger.LogInfo($"Workflow {Name} connected nodes: {sourceNodeId} -> {targetNodeId}");
            }
        }

        /// <summary>
        /// Connect nodes by port - 精确指定源端口和目标端口
        /// </summary>
        public void ConnectNodesByPort(string sourceNodeId, string sourcePort, string targetNodeId, string targetPort)
        {
            // 添加节点级连接
            ConnectNodes(sourceNodeId, targetNodeId);

            // 添加端口级连接
            var existingConnection = PortConnections.FirstOrDefault(pc =>
                pc.SourceNodeId == sourceNodeId && pc.SourcePort == sourcePort &&
                pc.TargetNodeId == targetNodeId && pc.TargetPort == targetPort);

            if (existingConnection == null)
            {
                PortConnections.Add(new PortConnection
                {
                    SourceNodeId = sourceNodeId,
                    SourcePort = sourcePort,
                    TargetNodeId = targetNodeId,
                    TargetPort = targetPort
                });

                Logger.LogInfo($"Workflow {Name} connected ports: {sourceNodeId}.{sourcePort} -> {targetNodeId}.{targetPort}");
            }
        }

        /// <summary>
        /// Disconnect nodes by port
        /// </summary>
        public void DisconnectNodesByPort(string sourceNodeId, string sourcePort, string targetNodeId, string targetPort)
        {
            // 移除节点级连接
            DisconnectNodes(sourceNodeId, targetNodeId);

            // 移除端口级连接
            var connection = PortConnections.FirstOrDefault(pc =>
                pc.SourceNodeId == sourceNodeId && pc.SourcePort == sourcePort &&
                pc.TargetNodeId == targetNodeId && pc.TargetPort == targetPort);

            if (connection != null)
            {
                PortConnections.Remove(connection);
                Logger.LogInfo($"Workflow {Name} disconnected ports: {sourceNodeId}.{sourcePort} -> {targetNodeId}.{targetPort}");
            }
        }

        /// <summary>
        /// 获取节点的并行执行分组
        /// </summary>
        public List<List<string>> GetParallelExecutionGroups()
        {
            var groups = new List<List<string>>();
            var remaining = new HashSet<string>(Nodes.Select(n => n.Id));
            var processed = new HashSet<string>();

            while (remaining.Count > 0)
            {
                var currentGroup = new List<string>();

                foreach (var nodeId in remaining.ToList())
                {
                    var dependencies = Connections.Where(kvp => kvp.Value.Contains(nodeId)).Select(kvp => kvp.Key).ToList();
                    if (dependencies.All(dep => processed.Contains(dep)))
                    {
                        currentGroup.Add(nodeId);
                    }
                }

                if (currentGroup.Count == 0)
                {
                    break;
                }

                groups.Add(currentGroup);
                foreach (var nodeId in currentGroup)
                {
                    processed.Add(nodeId);
                    remaining.Remove(nodeId);
                }
            }

            return groups;
        }

        /// <summary>
        /// 获取节点的执行顺序（真正的拓扑排序 - Kahn算法）
        /// </summary>
        public List<string> GetExecutionOrder()
        {
            var order = new List<string>();

            // 1. 计算每个节点的入度
            var inDegree = new Dictionary<string, int>();
            foreach (var node in Nodes)
            {
                inDegree[node.Id] = 0;
            }

            foreach (var connection in Connections)
            {
                foreach (var targetId in connection.Value)
                {
                    inDegree[targetId]++;
                }
            }

            // 2. 将入度为0的节点加入队列
            var queue = new Queue<string>();
            foreach (var node in Nodes)
            {
                if (inDegree[node.Id] == 0)
                {
                    queue.Enqueue(node.Id);
                }
            }

            // 3. 拓扑排序
            var processedCount = 0;
            while (queue.Count > 0)
            {
                var nodeId = queue.Dequeue();
                order.Add(nodeId);
                processedCount++;

                // 4. 减少后继节点的入度
                if (Connections.ContainsKey(nodeId))
                {
                    foreach (var dependentId in Connections[nodeId])
                    {
                        inDegree[dependentId]--;
                        if (inDegree[dependentId] == 0)
                        {
                            queue.Enqueue(dependentId);
                        }
                    }
                }
            }

            // 5. 检测循环依赖
            if (processedCount != Nodes.Count)
            {
                Logger?.LogWarning($"拓扑排序检测到循环依赖，已处理 {processedCount}/{Nodes.Count} 个节点");
            }

            Logger?.LogInfo($"拓扑排序完成，执行顺序: [{string.Join(" -> ", order)}]");
            return order;
        }

        /// <summary>
        /// 检测工作流中的循环依赖
        /// </summary>
        public List<string> DetectCycles()
        {
            var cycles = new List<string>();
            var visited = new HashSet<string>();
            var recursionStack = new HashSet<string>();

            bool DetectCycle(string nodeId, string path)
            {
                visited.Add(nodeId);
                recursionStack.Add(nodeId);

                if (Connections.ContainsKey(nodeId))
                {
                    foreach (var dependentId in Connections[nodeId])
                    {
                        if (!visited.Contains(dependentId))
                        {
                            if (DetectCycle(dependentId, path + " -> " + dependentId))
                            {
                                return true;
                            }
                        }
                        else if (recursionStack.Contains(dependentId))
                        {
                            cycles.Add(path + " -> " + dependentId);
                            return true;
                        }
                    }
                }

                recursionStack.Remove(nodeId);
                return false;
            }

            foreach (var node in Nodes)
            {
                if (!visited.Contains(node.Id))
                {
                    DetectCycle(node.Id, node.Id);
                }
            }

            return cycles;
        }

        /// <summary>
        /// 获取节点优先级
        /// </summary>
        public int GetNodePriority(string nodeId)
        {
            var visited = new HashSet<string>();

            int CalculatePriority(string id)
            {
                if (visited.Contains(id))
                {
                    return 0;
                }

                visited.Add(id);

                if (!Connections.ContainsKey(id))
                {
                    return 0;
                }

                var maxPriority = 0;
                foreach (var dependentId in Connections[id])
                {
                    var priority = CalculatePriority(dependentId);
                    if (priority > maxPriority)
                    {
                        maxPriority = priority;
                    }
                }

                return maxPriority + 1;
            }

            return CalculatePriority(nodeId);
        }

        /// <summary>
        /// Disconnect nodes
        /// </summary>
        public void DisconnectNodes(string sourceNodeId, string targetNodeId)
        {
            if (Connections.ContainsKey(sourceNodeId))
            {
                Connections[sourceNodeId].Remove(targetNodeId);
                Logger.LogInfo($"Workflow {Name} disconnected: {sourceNodeId} -> {targetNodeId}");
            }
        }

        /// <summary>
        /// Execute workflow
        /// </summary>
        public List<AlgorithmResult> Execute(Mat inputImage)
        {
            var results = new List<AlgorithmResult>();
            var nodeResults = new Dictionary<string, Mat>();
            var executedNodes = new HashSet<string>();

            Logger.LogInfo($"Starting workflow execution: {Name}");

            // 创建执行计划
            var executionPlan = CreateExecutionPlan();
            executionPlan.Start();

            // 按执行顺序执行节点
            var executionOrder = GetExecutionOrder();
            if (executionOrder.Count == 0)
            {
                Logger.LogWarning("无法确定执行顺序,可能存在循环依赖");
                return results;
            }

            foreach (var nodeId in executionOrder)
            {
                var node = Nodes.FirstOrDefault(n => n.Id == nodeId);
                if (node != null)
                {
                    ExecuteNode(node, inputImage, nodeResults, executedNodes, results);
                }
            }

            executionPlan.Complete();
            Logger.LogInfo(executionPlan.GetReport());

            return results;
        }

        /// <summary>
        /// 创建执行计划
        /// </summary>
        public ExecutionPlan CreateExecutionPlan()
        {
            var plan = new ExecutionPlan();
            var groups = GetParallelExecutionGroups();

            for (int i = 0; i < groups.Count; i++)
            {
                plan.Groups.Add(new ExecutionGroup
                {
                    GroupNumber = i + 1,
                    NodeIds = groups[i],
                    Status = ExecutionGroupStatus.Pending
                });
            }

            return plan;
        }

        private void ExecuteNode(WorkflowNode node, Mat inputImage,
            Dictionary<string, Mat> nodeResults, HashSet<string> executedNodes,
            List<AlgorithmResult> results)
        {
            if (executedNodes.Contains(node.Id) || !node.IsEnabled)
            {
                return;
            }

            // Execute subsequent nodes
            var input = inputImage;

            if (Connections.Any(kvp => kvp.Value.Contains(node.Id)))
            {
                // Get input from parent nodes
                var parentIds = Connections.Where(kvp => kvp.Value.Contains(node.Id)).Select(kvp => kvp.Key);
                var parentResults = parentIds.Where(id => nodeResults.ContainsKey(id)).Select(id => nodeResults[id]).ToList();

                if (parentResults.Any())
                {
                    input = parentResults.First();
                }
            }

            try
            {
                Logger.LogInfo($"Executing node: {node.Name} (ID: {node.Id})");

                var algorithm = node.CreateInstance();
                var resultImage = algorithm.Process(input) as Mat;

                nodeResults[node.Id] = resultImage ?? input;

                var result = new AlgorithmResult
                {
                    AlgorithmName = node.AlgorithmType,
                    Success = true,
                    ResultImage = resultImage,
                    ExecutionTime = DateTime.Now
                };

                results.Add(result);
                executedNodes.Add(node.Id);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to execute node {node.Name}: {ex.Message}", ex);

                results.Add(new AlgorithmResult
                {
                    AlgorithmName = node.AlgorithmType,
                    Success = false,
                    ErrorMessage = ex.Message,
                    ExecutionTime = DateTime.Now
                });
            }
        }

        /// <summary>
        /// Get workflow information
        /// </summary>
        public string GetInfo()
        {
            var info = $"Workflow: {Name} (ID: {Id})\n";
            info += $"Description: {Description}\n";
            info += $"Node count: {Nodes.Count}\n";
            info += "Node list:\n";

            foreach (var node in Nodes)
            {
                info += $"  - {node.Name} (ID: {node.Id}, {node.AlgorithmType}) - {(node.IsEnabled ? "Enabled" : "Disabled")}\n";
            }

            return info;
        }

        /// <summary>
        /// 自动识别执行链（基于节点连接关系和入度）
        /// </summary>
        /// <returns>识别到的执行链列表</returns>
        public List<ExecutionChain> GetAutoDetectExecutionChains()
        {
            var chains = new List<ExecutionChain>();
            var allVisitedNodes = new HashSet<string>();
            var chainIndex = 0;

            // ==================== 步骤1：计算每个节点的入度 ====================
            var inDegree = new Dictionary<string, int>();
            foreach (var node in Nodes)
            {
                inDegree[node.Id] = 0;  // 初始化入度为0
            }

            // 遍历所有连接，计算每个目标节点的入度
            foreach (var connection in Connections)
            {
                foreach (var targetId in connection.Value)
                {
                    inDegree[targetId]++;  // 每有一个输入连接，入度+1
                }
            }

            // ==================== 步骤2：识别所有入口节点（入度=0且已启用） ====================
            var entryNodes = Nodes
                .Where(n => inDegree[n.Id] == 0 && n.IsEnabled)
                .ToList();

            Logger?.LogInfo($"[AutoDetect] 找到 {entryNodes.Count} 个入口节点");

            // ==================== 步骤3：为每个入口节点构建独立的执行链 ====================
            foreach (var entryNode in entryNodes)
            {
                var chain = new ExecutionChain
                {
                    ChainId = $"chain_{chainIndex}",
                    StartNodeId = entryNode.Id,
                    NodeIds = new List<string>(),
                    Dependencies = new List<ChainDependency>()
                };

                // 递归收集该入口节点下游的所有节点
                CollectExecutionChain(entryNode.Id, chain.NodeIds, allVisitedNodes);

                // 分析跨链依赖关系
                AnalyzeChainDependencies(chain, allVisitedNodes, chains);

                chains.Add(chain);
                chainIndex++;

                Logger?.LogInfo($"[AutoDetect] 创建执行链[{chainIndex}]: {entryNode.Name} (包含{chain.NodeIds.Count}个节点)");
            }

            // ==================== 步骤4：处理未访问的节点（孤岛节点） ====================
            var unvisitedNodes = Nodes.Where(n => !allVisitedNodes.Contains(n.Id) && n.IsEnabled).ToList();
            foreach (var isolatedNode in unvisitedNodes)
            {
                var chain = new ExecutionChain
                {
                    ChainId = $"chain_{chainIndex}",
                    StartNodeId = isolatedNode.Id,
                    NodeIds = new List<string> { isolatedNode.Id },
                    Dependencies = new List<ChainDependency>()
                };

                chains.Add(chain);
                chainIndex++;

                Logger?.LogInfo($"[AutoDetect] 创建孤岛链[{chainIndex}]: {isolatedNode.Name}");
            }

            Logger?.LogInfo($"[AutoDetect] 共识别 {chains.Count} 条执行链");
            return chains;
        }

        /// <summary>
        /// 获取执行链（基于连接关系自动识别入口节点）
        /// </summary>
        /// <returns>执行链列表</returns>
        public List<ExecutionChain> GetExecutionChains()
        {
            return GetAutoDetectExecutionChains();
        }

        /// <summary>
        /// 收集执行链中的节点
        /// </summary>
        private void CollectExecutionChain(
            string nodeId,
            List<string> chainNodes,
            HashSet<string> allVisitedNodes)
        {
            if (allVisitedNodes.Contains(nodeId) || chainNodes.Contains(nodeId))
            {
                return;
            }

            chainNodes.Add(nodeId);
            allVisitedNodes.Add(nodeId);

            // 递归收集下游节点
            if (Connections.ContainsKey(nodeId))
            {
                foreach (var childId in Connections[nodeId])
                {
                    CollectExecutionChain(childId, chainNodes, allVisitedNodes);
                }
            }
        }

        /// <summary>
        /// 分析跨链依赖关系
        /// </summary>
        private void AnalyzeChainDependencies(ExecutionChain chain, HashSet<string> allVisitedNodes, List<ExecutionChain> existingChains)
        {
            foreach (var nodeId in chain.NodeIds)
            {
                var parentIds = Connections
                    .Where(kvp => kvp.Value.Contains(nodeId))
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var parentId in parentIds)
                {
                    // 如果父节点不在本链中，则创建跨链依赖
                    if (!chain.NodeIds.Contains(parentId))
                    {
                        // 查找源链
                        var sourceChain = existingChains.FirstOrDefault(c => c.NodeIds.Contains(parentId));
                        var sourceChainId = sourceChain?.ChainId ?? "unknown";

                        chain.Dependencies.Add(new ChainDependency
                        {
                            SourceChainId = sourceChainId,
                            SourceNodeId = parentId,
                            TargetNodeId = nodeId
                        });
                    }
                }
            }
        }

        /// <summary>
        /// 基于执行链的并行执行分组（改进版 - 使用层级分组最大化并行度）
        /// 确保不同执行链的节点不会被错误地混合到同一组
        /// 使用BFS层级感知分组，将同一层级的节点分到同一执行组
        /// </summary>
        /// <returns>并行执行组列表，每个组包含可并行执行的节点ID</returns>
        public List<List<string>> GetParallelExecutionGroupsByChains()
        {
            var groups = new List<List<string>>();

            // 1. 识别所有执行链
            var chains = GetAutoDetectExecutionChains();
            Logger?.LogInfo($"[ParallelGroups] 识别到 {chains.Count} 条执行链");

            // 2. 为每个执行链生成层级分组（而非线性排序）
            var chainExecutionLevels = new Dictionary<string, List<List<string>>>();

            foreach (var chain in chains)
            {
                var chainNodes = new HashSet<string>(chain.NodeIds);
                var levels = GetExecutionLevelsForNodes(chainNodes);
                chainExecutionLevels[chain.ChainId] = levels;

                Logger?.LogInfo($"[ParallelGroups] 执行链 {chain.ChainId} ({GetNodeDisplayName(chain.StartNodeId)}):");
                for (int i = 0; i < levels.Count; i++)
                {
                    Logger?.LogInfo($"    层级{i + 1}: [{FormatNodeListDisplay(levels[i])}]");
                }
            }

            // 3. 跨链并行合并：将不同执行链中同一层级的节点合并
            int maxLevel = chainExecutionLevels.Values.Max(l => l.Count);

            for (int level = 0; level < maxLevel; level++)
            {
                var currentGroup = new List<string>();

                foreach (var chainId in chainExecutionLevels.Keys)
                {
                    var levels = chainExecutionLevels[chainId];
                    if (level < levels.Count)
                    {
                        // 将该链在当前层级的所有节点加入执行组
                        currentGroup.AddRange(levels[level]);
                    }
                }

                if (currentGroup.Count > 0)
                {
                    groups.Add(currentGroup);
                    Logger?.LogInfo($"[ParallelGroups] 执行组{level + 1}: [{FormatNodeListDisplay(currentGroup)}]");
                }
            }

            Logger?.LogInfo($"[ParallelGroups] 共生成 {groups.Count} 个并行执行组（优化后）");
            return groups;
        }

        /// <summary>
        /// 获取指定节点集合的执行顺序（拓扑排序）
        /// </summary>
        /// <param name="nodeIds">节点ID集合</param>
        /// <returns>按拓扑排序的节点ID列表</returns>
        private List<string> GetExecutionOrderForNodes(HashSet<string> nodeIds)
        {
            var order = new List<string>();
            var inDegree = new Dictionary<string, int>();

            // 1. 计算指定节点的入度
            foreach (var nodeId in nodeIds)
            {
                inDegree[nodeId] = 0;
            }

            foreach (var connection in Connections)
            {
                foreach (var targetId in connection.Value)
                {
                    if (nodeIds.Contains(targetId) && nodeIds.Contains(connection.Key))
                    {
                        inDegree[targetId]++;
                    }
                }
            }

            // 2. 将入度为0的节点加入队列
            var queue = new Queue<string>();
            foreach (var nodeId in nodeIds)
            {
                if (inDegree[nodeId] == 0)
                {
                    queue.Enqueue(nodeId);
                }
            }

            // 3. 拓扑排序
            var processedCount = 0;
            while (queue.Count > 0)
            {
                var nodeId = queue.Dequeue();
                order.Add(nodeId);
                processedCount++;

                // 4. 减少后继节点的入度
                if (Connections.ContainsKey(nodeId))
                {
                    foreach (var dependentId in Connections[nodeId])
                    {
                        if (nodeIds.Contains(dependentId))
                        {
                            inDegree[dependentId]--;
                            if (inDegree[dependentId] == 0)
                            {
                                queue.Enqueue(dependentId);
                            }
                        }
                    }
                }
            }

            if (processedCount != nodeIds.Count)
            {
                Logger?.LogWarning($"拓扑排序检测到循环依赖，已处理 {processedCount}/{nodeIds.Count} 个节点");
            }

            return order;
        }

        /// <summary>
        /// 获取指定节点集合的执行层级（BFS层级感知分组）
        /// 将具有相同依赖深度的节点分到同一层级，以最大化并行度
        /// </summary>
        /// <param name="nodeIds">节点ID集合</param>
        /// <returns>层级列表，每个层级包含可并行执行的节点ID</returns>
        private List<List<string>> GetExecutionLevelsForNodes(HashSet<string> nodeIds)
        {
            var levels = new List<List<string>>();
            var inDegree = new Dictionary<string, int>();
            var remaining = new HashSet<string>(nodeIds);

            // 1. 计算指定节点的入度（仅考虑集合内的连接）
            foreach (var nodeId in nodeIds)
            {
                inDegree[nodeId] = 0;
            }

            foreach (var connection in Connections)
            {
                foreach (var targetId in connection.Value)
                {
                    if (nodeIds.Contains(targetId) && nodeIds.Contains(connection.Key))
                    {
                        inDegree[targetId]++;
                    }
                }
            }

            // 2. BFS层级遍历，将同一层级的节点分组
            while (remaining.Count > 0)
            {
                var currentLevel = new List<string>();

                // 找出当前所有入度为0的节点（可并行执行）
                foreach (var nodeId in remaining.ToList())
                {
                    if (inDegree[nodeId] == 0)
                    {
                        currentLevel.Add(nodeId);
                    }
                }

                if (currentLevel.Count == 0)
                {
                    // 没有可执行的节点，可能是循环依赖
                    Logger?.LogWarning($"无法处理剩余节点，可能存在循环依赖: [{FormatNodeListDisplay(remaining)}]");
                    break;
                }

                levels.Add(currentLevel);

                // 从剩余节点中移除已处理的节点
                foreach (var nodeId in currentLevel)
                {
                    remaining.Remove(nodeId);
                }

                // 减少这些节点的后继节点的入度
                foreach (var nodeId in currentLevel)
                {
                    if (Connections.ContainsKey(nodeId))
                    {
                        foreach (var dependentId in Connections[nodeId])
                        {
                            if (nodeIds.Contains(dependentId) && inDegree.ContainsKey(dependentId))
                            {
                                inDegree[dependentId]--;
                            }
                        }
                    }
                }
            }

            Logger?.LogInfo($"层级分组完成，共{levels.Count}个层级");
            for (int i = 0; i < levels.Count; i++)
            {
                Logger?.LogInfo($"  层级{i + 1}: [{FormatNodeListDisplay(levels[i])}]");
            }

            return levels;
        }

        /// <summary>
        /// 获取节点的完整显示名称（格式：节点名称(ID: xxx)）
        /// </summary>
        /// <param name="nodeId">节点ID</param>
        /// <returns>节点的完整显示名称，如果节点不存在则返回ID本身</returns>
        private string GetNodeDisplayName(string nodeId)
        {
            var node = Nodes.FirstOrDefault(n => n.Id == nodeId);
            if (node != null)
            {
                return $"{node.Name}(ID: {nodeId})";
            }
            return nodeId;
        }

        /// <summary>
        /// 格式化节点ID列表为显示名称列表
        /// </summary>
        /// <param name="nodeIds">节点ID列表</param>
        /// <returns>格式化后的节点显示名称列表字符串</returns>
        private string FormatNodeListDisplay(IEnumerable<string> nodeIds)
        {
            return string.Join(", ", nodeIds.Select(id => GetNodeDisplayName(id)));
        }
    }

    /// <summary>
    /// 执行组状态
    /// </summary>
    public enum ExecutionGroupStatus
    {
        Pending,
        Running,
        Completed,
        Failed
    }

    /// <summary>
    /// 执行组
    /// </summary>
    public class ExecutionGroup
    {
        public int GroupNumber { get; set; }
        public List<string> NodeIds { get; set; } = new List<string>();
        public ExecutionGroupStatus Status { get; set; }
    }

    /// <summary>
    /// 执行计划
    /// </summary>
    public class ExecutionPlan
    {
        public List<ExecutionGroup> Groups { get; set; } = new List<ExecutionGroup>();
        private DateTime _startTime;
        private DateTime _endTime;

        public void Start()
        {
            _startTime = DateTime.Now;
        }

        public void Complete()
        {
            _endTime = DateTime.Now;
        }

        public string GetReport()
        {
            var duration = (_endTime - _startTime).TotalMilliseconds;
            var report = $"Execution completed in {duration:F2}ms\n";
            report += $"Groups: {Groups.Count}\n";

            foreach (var group in Groups)
            {
                report += $"  Group {group.GroupNumber}: {group.NodeIds.Count} nodes - {group.Status}\n";
            }

            return report;
        }
    }
}
