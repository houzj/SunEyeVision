using System;
using System.Collections.Generic;
using System.Linq;
using OpenCvSharp;
using SunEyeVision.Core.Interfaces;
using SunEyeVision.Plugin.SDK.Core;

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
        /// 源端口 (如 "output", "left", "right", "top", "bottom")
        /// </summary>
        public string SourcePort { get; set; }

        /// <summary>
        /// 目标节点ID
        /// </summary>
        public string TargetNodeId { get; set; }

        /// <summary>
        /// 目标端口 (如 "input", "left", "right", "top", "bottom")
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
        /// 添加节点到工作流
        /// </summary>
        /// <param name="node">要添加的工作流节点</param>
        public void AddNode(WorkflowNode node)
        {
            Nodes.Add(node);
            Logger.LogInfo($"Workflow {Name} added node: {node.Name}");
        }

        /// <summary>
        /// 从工作流中移除节点
        /// </summary>
        /// <param name="nodeId">要移除的节点ID</param>
        /// <remarks>
        /// 移除操作包括：
        /// 1. 从节点列表中移除
        /// 2. 移除该节点的所有输出连接
        /// 3. 移除其他节点到该节点的输入连接
        /// 4. 移除该节点的所有端口连接
        /// </remarks>
        public void RemoveNode(string nodeId)
        {
            var node = Nodes.FirstOrDefault(n => n.Id == nodeId);
            if (node != null)
            {
                Nodes.Remove(node);
                Connections.Remove(nodeId);

                // 移除其他节点到该节点的连接
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
        /// 连接两个节点（创建从源节点到目标节点的连接）
        /// </summary>
        /// <param name="sourceNodeId">源节点ID</param>
        /// <param name="targetNodeId">目标节点ID</param>
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
        /// 获取节点的并行执行分组（基于拓扑排序的分层）
        /// </summary>
        /// <returns>并行执行组列表，每个组包含可并行执行的节点ID</returns>
        /// <remarks>
        /// 算法说明：
        /// 1. 初始化：所有节点未处理，已处理集合为空
        /// 2. 迭代：在每轮中，找出所有依赖已满足的节点
        /// 3. 分组：将满足条件的节点加入当前分组
        /// 4. 更新：标记当前组为已处理，继续下一轮
        /// 5. 终止：所有节点处理完毕或无法继续（循环依赖）
        /// </remarks>
        public List<List<string>> GetParallelExecutionGroups()
        {
            var groups = new List<List<string>>();
            var remaining = new HashSet<string>(Nodes.Select(n => n.Id));
            var processed = new HashSet<string>();

            while (remaining.Count > 0)
            {
                var currentGroup = new List<string>();

                // 找出当前所有依赖已满足的节点
                foreach (var nodeId in remaining.ToList())
                {
                    var dependencies = Connections
                        .Where(kvp => kvp.Value.Contains(nodeId))
                        .Select(kvp => kvp.Key)
                        .ToList();

                    if (dependencies.All(dep => processed.Contains(dep)))
                    {
                        currentGroup.Add(nodeId);
                    }
                }

                // 如果无法找到可执行的节点，说明存在循环依赖
                if (currentGroup.Count == 0)
                {
                    Logger?.LogWarning("无法继续分组，可能存在循环依赖");
                    break;
                }

                groups.Add(currentGroup);

                // 标记当前组为已处理
                foreach (var nodeId in currentGroup)
                {
                    processed.Add(nodeId);
                    remaining.Remove(nodeId);
                }
            }

            return groups;
        }

        /// <summary>
        /// 获取节点的执行顺序（使用Kahn算法进行拓扑排序）
        /// </summary>
        /// <returns>按拓扑排序的节点ID列表</returns>
        /// <remarks>
        /// 拓扑排序算法步骤：
        /// 1. 计算每个节点的入度（指向该节点的连接数量）
        /// 2. 将所有入度为0的节点加入队列
        /// 3. 从队列中取出节点并加入结果列表
        /// 4. 减少该节点所有后续节点的入度，如果入度变为0则加入队列
        /// 5. 重复步骤3-4直到队列为空
        /// 6. 如果处理的节点数不等于总节点数，说明存在循环依赖
        /// </remarks>
        public List<string> GetExecutionOrder()
        {
            var order = new List<string>();
            var inDegree = CalculateNodeInDegrees();

            // 将入度为0的节点加入队列
            var queue = new Queue<string>();
            foreach (var node in Nodes)
            {
                if (inDegree[node.Id] == 0)
                {
                    queue.Enqueue(node.Id);
                }
            }

            // 拓扑排序处理
            var processedCount = 0;
            while (queue.Count > 0)
            {
                var nodeId = queue.Dequeue();
                order.Add(nodeId);
                processedCount++;

                // 减少后续节点的入度
                ProcessSuccessors(nodeId, inDegree, queue);
            }

            // 检测循环依赖
            if (processedCount != Nodes.Count)
            {
                Logger?.LogWarning($"拓扑排序检测到循环依赖，已处理 {processedCount}/{Nodes.Count} 个节点");
            }

            Logger?.LogInfo($"拓扑排序完成，执行顺序: [{string.Join(" -> ", order)}]");
            return order;
        }

        /// <summary>
        /// 处理指定节点的所有后续节点，减少它们的入度
        /// </summary>
        /// <param name="nodeId">源节点ID</param>
        /// <param name="inDegree">节点入度字典</param>
        /// <param name="queue">待处理节点队列</param>
        private void ProcessSuccessors(string nodeId, Dictionary<string, int> inDegree, Queue<string> queue)
        {
            if (!Connections.ContainsKey(nodeId))
            {
                return;
            }

            foreach (var dependentId in Connections[nodeId])
            {
                inDegree[dependentId]--;
                if (inDegree[dependentId] == 0)
                {
                    queue.Enqueue(dependentId);
                }
            }
        }

        /// <summary>
        /// 检测工作流中的循环依赖
        /// </summary>
        /// <returns>检测到的循环依赖路径列表</returns>
        /// <remarks>
        /// 使用DFS深度优先搜索算法检测循环：
        /// 1. 访问节点时加入已访问集合和递归栈
        /// 2. 递归访问所有后续节点
        /// 3. 如果在递归栈中再次遇到某个节点，说明存在循环
        /// 4. 记录循环路径并返回
        /// </remarks>
        public List<string> DetectCycles()
        {
            var cycles = new List<string>();
            var visited = new HashSet<string>();
            var recursionStack = new HashSet<string>();

            foreach (var node in Nodes)
            {
                if (!visited.Contains(node.Id))
                {
                    DetectCycleDFS(node.Id, node.Id, cycles, visited, recursionStack);
                }
            }

            return cycles;
        }

        /// <summary>
        /// 使用深度优先搜索（DFS）检测循环依赖
        /// </summary>
        /// <param name="nodeId">当前节点ID</param>
        /// <param name="path">当前路径</param>
        /// <param name="cycles">循环列表（用于记录发现的循环）</param>
        /// <param name="visited">已访问节点集合</param>
        /// <param name="recursionStack">递归栈（用于检测回边）</param>
        /// <returns>是否发现循环</returns>
        private bool DetectCycleDFS(
            string nodeId,
            string path,
            List<string> cycles,
            HashSet<string> visited,
            HashSet<string> recursionStack)
        {
            visited.Add(nodeId);
            recursionStack.Add(nodeId);

            if (Connections.ContainsKey(nodeId))
            {
                foreach (var dependentId in Connections[nodeId])
                {
                    if (!visited.Contains(dependentId))
                    {
                        if (DetectCycleDFS(dependentId, path + " -> " + dependentId, cycles, visited, recursionStack))
                        {
                            return true;
                        }
                    }
                    else if (recursionStack.Contains(dependentId))
                    {
                        // 发现回边，记录循环路径
                        cycles.Add(path + " -> " + dependentId);
                        return true;
                    }
                }
            }

            recursionStack.Remove(nodeId);
            return false;
        }

        /// <summary>
        /// 获取节点优先级（基于从该节点出发的最长路径长度）
        /// </summary>
        /// <param name="nodeId">节点ID</param>
        /// <returns>节点优先级值（值越大表示该节点离出口越远）</returns>
        /// <remarks>
        /// 优先级计算说明：
        /// 1. 出口节点（无后续节点）的优先级为0
        /// 2. 其他节点的优先级 = 1 + max(所有后续节点的优先级)
        /// 3. 优先级越高的节点应该越早执行
        /// </remarks>
        public int GetNodePriority(string nodeId)
        {
            var visited = new HashSet<string>();
            return CalculateNodePriority(nodeId, visited);
        }

        /// <summary>
        /// 递归计算节点优先级
        /// </summary>
        /// <param name="nodeId">节点ID</param>
        /// <param name="visited">已访问节点集合（用于避免循环导致的无限递归）</param>
        /// <returns>节点优先级</returns>
        private int CalculateNodePriority(string nodeId, HashSet<string> visited)
        {
            // 避免循环导致的无限递归
            if (visited.Contains(nodeId))
            {
                return 0;
            }

            visited.Add(nodeId);

            // 出口节点优先级为0
            if (!Connections.ContainsKey(nodeId))
            {
                return 0;
            }

            // 计算所有后续节点的最大优先级
            var maxPriority = 0;
            foreach (var dependentId in Connections[nodeId])
            {
                var priority = CalculateNodePriority(dependentId, visited);
                if (priority > maxPriority)
                {
                    maxPriority = priority;
                }
            }

            return maxPriority + 1;
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
        /// 执行工作流
        /// </summary>
        /// <param name="inputImage">输入图像数据</param>
        /// <returns>所有节点的执行结果列表</returns>
        /// <remarks>
        /// 执行流程：
        /// 1. 创建执行计划（并行分组）
        /// 2. 获取拓扑排序的执行顺序
        /// 3. 按顺序依次执行每个节点
        /// 4. 节点的输出作为后续节点的输入
        /// 5. 记录所有节点的执行结果和执行时间
        /// </remarks>
        public List<AlgorithmResult> Execute(Mat inputImage)
        {
            var results = new List<AlgorithmResult>();
            var nodeResults = new Dictionary<string, Mat>();
            var executedNodes = new HashSet<string>();

            Logger.LogInfo($"Starting workflow execution: {Name}");

            // 创建执行计划
            var executionPlan = CreateExecutionPlan();
            executionPlan.Start();

            // 获取拓扑排序的执行顺序
            var executionOrder = GetExecutionOrder();
            if (executionOrder.Count == 0)
            {
                Logger.LogWarning("无法确定执行顺序,可能存在循环依赖");
                return results;
            }

            // 按执行顺序执行节点
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
        /// 创建执行计划（基于并行执行分组）
        /// </summary>
        /// <returns>执行计划对象</returns>
        /// <remarks>
        /// 执行计划包含：
        /// 1. 多个执行组，每个组包含可并行执行的节点
        /// 2. 执行组的序号和状态
        /// 3. 执行开始和结束时间
        /// </remarks>
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

        /// <summary>
        /// 执行单个工作流节点
        /// </summary>
        /// <param name="node">要执行的节点</param>
        /// <param name="inputImage">输入图像</param>
        /// <param name="nodeResults">节点执行结果映射表（用于存储和获取中间结果）</param>
        /// <param name="executedNodes">已执行节点集合（用于避免重复执行）</param>
        /// <param name="results">算法结果列表（用于收集所有节点的执行结果）</param>
        private void ExecuteNode(
            WorkflowNode node,
            Mat inputImage,
            Dictionary<string, Mat> nodeResults,
            HashSet<string> executedNodes,
            List<AlgorithmResult> results)
        {
            // 跳过已执行或未启用的节点
            if (executedNodes.Contains(node.Id) || !node.IsEnabled)
            {
                return;
            }

            // 准备输入数据
            var input = PrepareNodeInput(node, inputImage, nodeResults);

            try
            {
                Logger.LogInfo($"Executing node: {node.Name} (ID: {node.Id})");

                // 创建算法实例并执行
                var algorithm = node.CreateInstance();
                var resultImage = algorithm.Process(input) as Mat;

                // 保存节点执行结果
                nodeResults[node.Id] = resultImage ?? input;

                // 记录执行结果
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
        /// 获取工作流信息摘要
        /// </summary>
        /// <returns>工作流信息的格式化字符串</returns>
        /// <remarks>
        /// 信息包含：
        /// - 工作流名称和ID
        /// - 工作流描述
        /// - 节点总数
        /// - 所有节点的详细列表（名称、ID、算法类型、启用状态）
        /// </remarks>
        public string GetInfo()
        {
            var info = $"Workflow: {Name} (ID: {Id})\n";
            info += $"Description: {Description}\n";
            info += $"Node count: {Nodes.Count}\n";
            info += "Node list:\n";

            foreach (var node in Nodes)
            {
                var status = node.IsEnabled ? "Enabled" : "Disabled";
                info += $"  - {node.Name} (ID: {node.Id}, {node.AlgorithmType}) - {status}\n";
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
            var chainCounter = 0;

            // 步骤1：计算每个节点的入度
            var inDegree = CalculateNodeInDegrees();

            // 步骤2：识别所有入口节点（入度=0且已启用）
            var entryNodes = Nodes
                .Where(n => inDegree[n.Id] == 0 && n.IsEnabled)
                .ToList();

            Logger?.LogInfo($"[AutoDetect] 找到 {entryNodes.Count} 个入口节点");

            // 步骤3：入口节点合并 - 将共享下游节点的入口节点合并到同一执行链
            var entryGroups = GroupEntryNodesByDownstream(entryNodes);

            // 步骤4：为每个入口组构建执行链
            foreach (var entryGroup in entryGroups)
            {
                var chain = CreateExecutionChain(entryGroup, chainCounter, allVisitedNodes, chains);
                chains.Add(chain);
                chainCounter++;

                var entryNames = string.Join(", ", entryGroup.Select(n => n.Name));
                Logger?.LogInfo($"[AutoDetect] 创建执行链[{chainCounter}]: [{entryNames}] (包含{chain.NodeIds.Count}个节点)");
            }

            // 步骤5：处理未访问的节点（孤岛节点）
            CreateIsolatedChains(allVisitedNodes, ref chainCounter, chains);

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
        /// 递归收集执行链中的所有节点（从指定节点开始）
        /// </summary>
        /// <param name="nodeId">起始节点ID</param>
        /// <param name="chainNodes">执行链节点列表（用于收集节点）</param>
        /// <param name="allVisitedNodes">全局已访问节点集合（用于避免跨链重复访问）</param>
        /// <remarks>
        /// 算法说明：
        /// 1. 检查节点是否已访问（避免重复和循环）
        /// 2. 将节点加入执行链和全局访问集合
        /// 3. 递归收集所有下游节点
        /// </remarks>
        private void CollectExecutionChain(
            string nodeId,
            List<string> chainNodes,
            HashSet<string> allVisitedNodes)
        {
            // 跳过已访问的节点
            if (allVisitedNodes.Contains(nodeId) || chainNodes.Contains(nodeId))
            {
                return;
            }

            // 添加节点到执行链和全局访问集合
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
        /// 分析并记录执行链的跨链依赖关系
        /// </summary>
        /// <param name="chain">目标执行链</param>
        /// <param name="allVisitedNodes">全局已访问节点集合</param>
        /// <param name="existingChains">已存在的执行链列表</param>
        /// <remarks>
        /// 跨链依赖定义：
        /// - 当执行链中的某个节点依赖于其他执行链中的节点时
        /// - 需要记录源链ID、源节点ID和目标节点ID
        /// - 用于执行时的同步和顺序控制
        /// </remarks>
        private void AnalyzeChainDependencies(
            ExecutionChain chain,
            HashSet<string> allVisitedNodes,
            List<ExecutionChain> existingChains)
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
                        // 查找父节点所在的源链
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
        /// </summary>
        /// <returns>并行执行组列表，每个组包含可并行执行的节点ID</returns>
        /// <remarks>
        /// 算法特点：
        /// 1. 识别所有执行链（独立的执行单元）
        /// 2. 为每个执行链生成层级分组（BFS层级感知）
        /// 3. 将不同执行链中同一层级的节点合并到同一执行组
        /// 4. 确保不同执行链的节点不会被错误地混合到同一组
        /// 
        /// 优势：
        /// - 最大化并行度：同一层级的所有节点可以并行执行
        /// - 避免依赖冲突：通过层级分组保证执行顺序正确
        /// - 支持跨链同步：不同执行链的同一层级节点可以同步执行
        /// </remarks>
        public List<List<string>> GetParallelExecutionGroupsByChains()
        {
            var groups = new List<List<string>>();

            // 步骤1：识别所有执行链
            var chains = GetAutoDetectExecutionChains();
            Logger?.LogInfo($"[ParallelGroups] 识别到 {chains.Count} 条执行链");

            // 步骤2：为每个执行链生成层级分组（而非线性排序）
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

            // 步骤3：跨链并行合并 - 将不同执行链中同一层级的节点合并
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
        /// 获取指定节点集合的执行顺序（使用Kahn算法进行拓扑排序）
        /// </summary>
        /// <param name="nodeIds">节点ID集合</param>
        /// <returns>按拓扑排序的节点ID列表</returns>
        /// <remarks>
        /// 与GetExecutionOrder的区别：
        /// - 仅考虑指定集合内的节点和连接
        /// - 忽略集合外节点的依赖关系
        /// - 用于子图或部分节点的执行排序
        /// </remarks>
        private List<string> GetExecutionOrderForNodes(HashSet<string> nodeIds)
        {
            var order = new List<string>();
            var inDegree = CalculateInDegreesForNodes(nodeIds);

            // 将入度为0的节点加入队列
            var queue = new Queue<string>();
            foreach (var nodeId in nodeIds)
            {
                if (inDegree[nodeId] == 0)
                {
                    queue.Enqueue(nodeId);
                }
            }

            // 拓扑排序处理
            var processedCount = 0;
            while (queue.Count > 0)
            {
                var nodeId = queue.Dequeue();
                order.Add(nodeId);
                processedCount++;

                // 减少后续节点的入度（仅考虑集合内的节点）
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
        /// </summary>
        /// <param name="nodeIds">节点ID集合</param>
        /// <returns>层级列表，每个层级包含可并行执行的节点ID</returns>
        /// <remarks>
        /// 算法说明：
        /// 1. 计算节点集合内的入度（仅考虑集合内的连接）
        /// 2. BFS层级遍历，将同一层级的节点分组
        /// 3. 每层包含入度为0且未处理的节点
        /// 4. 处理后减少后续节点的入度，进入下一层
        /// 
        /// 优势：
        /// - 最大化并行度：同一层级的所有节点可以并行执行
        /// - 保证顺序正确：通过层级分组确保依赖关系
        /// - 支持部分执行：仅处理指定集合内的节点
        /// </remarks>
        private List<List<string>> GetExecutionLevelsForNodes(HashSet<string> nodeIds)
        {
            var levels = new List<List<string>>();
            var inDegree = CalculateInDegreesForNodes(nodeIds);
            var remaining = new HashSet<string>(nodeIds);

            // BFS层级遍历，将同一层级的节点分组
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

                // 没有可执行的节点，可能是循环依赖
                if (currentLevel.Count == 0)
                {
                    Logger?.LogWarning($"无法处理剩余节点，可能存在循环依赖 [{FormatNodeListDisplay(remaining)}]");
                    break;
                }

                levels.Add(currentLevel);

                // 从剩余节点中移除已处理的节点
                foreach (var nodeId in currentLevel)
                {
                    remaining.Remove(nodeId);
                }

                // 减少这些节点的后续节点的入度
                ProcessLevelSuccessors(currentLevel, nodeIds, inDegree);
            }

            Logger?.LogInfo($"层级分组完成，共{levels.Count}个层级");
            for (int i = 0; i < levels.Count; i++)
            {
                Logger?.LogInfo($"  层级{i + 1}: [{FormatNodeListDisplay(levels[i])}]");
            }

            return levels;
        }

        /// <summary>
        /// 计算指定节点集合中每个节点的入度
        /// </summary>
        /// <param name="nodeIds">节点ID集合</param>
        /// <returns>节点入度字典</returns>
        private Dictionary<string, int> CalculateInDegreesForNodes(HashSet<string> nodeIds)
        {
            var inDegree = new Dictionary<string, int>();

            // 初始化所有节点的入度为0
            foreach (var nodeId in nodeIds)
            {
                inDegree[nodeId] = 0;
            }

            // 计算集合内连接的入度
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

            return inDegree;
        }

        /// <summary>
        /// 处理当前层级的后续节点，减少它们的入度
        /// </summary>
        /// <param name="currentLevel">当前层级的节点列表</param>
        /// <param name="nodeIds">节点ID集合（用于过滤）</param>
        /// <param name="inDegree">节点入度字典</param>
        private void ProcessLevelSuccessors(
            List<string> currentLevel,
            HashSet<string> nodeIds,
            Dictionary<string, int> inDegree)
        {
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
        /// 为节点准备输入数据
        /// 如果节点有父节点，则使用父节点的输出作为输入；否则使用原始输入
        /// </summary>
        /// <param name="node">目标节点</param>
        /// <param name="defaultInput">默认输入数据</param>
        /// <param name="nodeResults">节点执行结果映射表</param>
        /// <returns>节点的输入数据</returns>
        private Mat PrepareNodeInput(
            WorkflowNode node,
            Mat defaultInput,
            Dictionary<string, Mat> nodeResults)
        {
            var input = defaultInput;

            if (Connections.Any(kvp => kvp.Value.Contains(node.Id)))
            {
                // 获取父节点的输出作为输入
                var parentIds = Connections
                    .Where(kvp => kvp.Value.Contains(node.Id))
                    .Select(kvp => kvp.Key);

                var parentResults = parentIds
                    .Where(id => nodeResults.ContainsKey(id))
                    .Select(id => nodeResults[id])
                    .ToList();

                if (parentResults.Any())
                {
                    input = parentResults.First();
                }
            }

            return input;
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

        /// <summary>
        /// 计算所有节点的入度
        /// </summary>
        /// <returns>节点ID到入度的映射字典</returns>
        private Dictionary<string, int> CalculateNodeInDegrees()
        {
            var inDegree = new Dictionary<string, int>();

            // 初始化所有节点的入度为0
            foreach (var node in Nodes)
            {
                inDegree[node.Id] = 0;
            }

            // 遍历所有连接，计算每个目标节点的入度
            foreach (var connection in Connections)
            {
                foreach (var targetId in connection.Value)
                {
                    inDegree[targetId]++;
                }
            }

            return inDegree;
        }

        /// <summary>
        /// 计算指定节点组的所有下游节点
        /// </summary>
        /// <param name="nodes">节点列表</param>
        /// <returns>下游节点ID集合</returns>
        private HashSet<string> CalculateDownstreamNodes(List<WorkflowNode> nodes)
        {
            var downstream = new HashSet<string>();

            foreach (var node in nodes)
            {
                if (Connections.ContainsKey(node.Id))
                {
                    downstream.UnionWith(Connections[node.Id]);
                }
            }

            return downstream;
        }

        /// <summary>
        /// 根据下游节点重叠情况对入口节点进行分组
        /// 将共享下游节点的入口节点合并到同一组，以便构建单个执行链
        /// </summary>
        /// <param name="entryNodes">入口节点列表</param>
        /// <returns>分组后的入口节点组列表</returns>
        private List<List<WorkflowNode>> GroupEntryNodesByDownstream(List<WorkflowNode> entryNodes)
        {
            var entryGroups = new List<List<WorkflowNode>>();

            foreach (var entryNode in entryNodes)
            {
                var merged = false;
                var currentDownstream = CalculateDownstreamNodes(new List<WorkflowNode> { entryNode });

                for (int groupIndex = 0; groupIndex < entryGroups.Count; groupIndex++)
                {
                    var group = entryGroups[groupIndex];
                    var groupDownstream = CalculateDownstreamNodes(group);

                    // 如果下游节点有交集，则合并到同一组
                    if (groupDownstream.Overlaps(currentDownstream))
                    {
                        group.Add(entryNode);
                        merged = true;
                        Logger?.LogInfo($"[AutoDetect] 合并入口节点: {entryNode.Name} -> 组[{groupIndex}]");
                        break;
                    }
                }

                // 如果没有找到重叠的组，则创建新组
                if (!merged)
                {
                    entryGroups.Add(new List<WorkflowNode> { entryNode });
                }
            }

            Logger?.LogInfo($"[AutoDetect] 将 {entryNodes.Count} 个入口节点合并为 {entryGroups.Count} 组");
            return entryGroups;
        }

        /// <summary>
        /// 为指定的入口节点组创建执行链
        /// </summary>
        /// <param name="entryGroup">入口节点组</param>
        /// <param name="chainIndex">执行链索引</param>
        /// <param name="allVisitedNodes">已访问的节点集合（用于标记已处理的节点）</param>
        /// <param name="existingChains">已存在的执行链列表（用于分析跨链依赖）</param>
        /// <returns>创建的执行链</returns>
        private ExecutionChain CreateExecutionChain(
            List<WorkflowNode> entryGroup,
            int chainIndex,
            HashSet<string> allVisitedNodes,
            List<ExecutionChain> existingChains)
        {
            var chain = new ExecutionChain
            {
                ChainId = $"chain_{chainIndex}",
                StartNodeId = entryGroup.First().Id,
                NodeIds = new List<string>(),
                Dependencies = new List<ChainDependency>()
            };

            // 递归收集该组所有入口节点下游的所有节点
            foreach (var entryNode in entryGroup)
            {
                CollectExecutionChain(entryNode.Id, chain.NodeIds, allVisitedNodes);
            }

            // 分析跨链依赖关系
            AnalyzeChainDependencies(chain, allVisitedNodes, existingChains);

            return chain;
        }

        /// <summary>
        /// 为未访问的节点（孤岛节点）创建独立的执行链
        /// </summary>
        /// <param name="allVisitedNodes">已访问的节点集合</param>
        /// <param name="chainCounter">执行链计数器（引用类型，用于递增索引）</param>
        /// <param name="chains">执行链列表（用于添加新的孤岛链）</param>
        private void CreateIsolatedChains(
            HashSet<string> allVisitedNodes,
            ref int chainCounter,
            List<ExecutionChain> chains)
        {
            var unvisitedNodes = Nodes
                .Where(n => !allVisitedNodes.Contains(n.Id) && n.IsEnabled)
                .ToList();

            foreach (var isolatedNode in unvisitedNodes)
            {
                var chain = new ExecutionChain
                {
                    ChainId = $"chain_{chainCounter}",
                    StartNodeId = isolatedNode.Id,
                    NodeIds = new List<string> { isolatedNode.Id },
                    Dependencies = new List<ChainDependency>()
                };

                chains.Add(chain);
                chainCounter++;

                Logger?.LogInfo($"[AutoDetect] 创建孤岛链[{chainCounter}]: {isolatedNode.Name}");
            }
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
