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
        /// 获取节点的执行顺序（拓扑排序）
        /// </summary>
        public List<string> GetExecutionOrder()
        {
            var order = new List<string>();
            var visited = new HashSet<string>();
            var tempVisited = new HashSet<string>();

            void Visit(string nodeId)
            {
                if (tempVisited.Contains(nodeId))
                {
                    return;
                }

                if (visited.Contains(nodeId))
                {
                    return;
                }

                tempVisited.Add(nodeId);

                if (Connections.ContainsKey(nodeId))
                {
                    foreach (var dependentId in Connections[nodeId])
                    {
                        Visit(dependentId);
                    }
                }

                tempVisited.Remove(nodeId);
                visited.Add(nodeId);
                order.Add(nodeId);
            }

            foreach (var node in Nodes)
            {
                Visit(node.Id);
            }

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
