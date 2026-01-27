using System;
using System.Collections.Generic;
using System.Linq;
using SunEyeVision.Interfaces;

namespace SunEyeVision.Workflow
{
    /// <summary>
    /// 工作流依赖分析器 - 负责分析节点依赖关系、检测循环、计算执行顺序
    /// </summary>
    public class DependencyAnalyzer
    {
        private ILogger Logger { get; set; }

        public DependencyAnalyzer(ILogger logger)
        {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 拓扑排序 - 返回按照依赖关系排序的节点ID列表
        /// </summary>
        public List<string> TopologicalSort(Workflow workflow)
        {
            var result = new List<string>();
            var inDegree = new Dictionary<string, int>();
            var graph = BuildGraph(workflow);

            // 计算入度
            foreach (var node in workflow.Nodes)
            {
                inDegree[node.Id] = 0;
            }

            foreach (var edge in graph)
            {
                if (inDegree.ContainsKey(edge.Value))
                {
                    inDegree[edge.Value]++;
                }
            }

            // 使用队列处理入度为0的节点
            var queue = new Queue<string>(inDegree.Where(kvp => kvp.Value == 0).Select(kvp => kvp.Key));

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                result.Add(current);

                // 减少邻接节点的入度
                foreach (var neighbor in graph.Where(kvp => kvp.Key == current).SelectMany(kvp => graph.Where(g => g.Key == kvp.Key).SelectMany(g => graph.Where(x => x.Value == g.Key).Select(x => x.Value))))
                {
                    inDegree[neighbor]--;
                    if (inDegree[neighbor] == 0)
                    {
                        queue.Enqueue(neighbor);
                    }
                }
            }

            // 检查是否有循环
            if (result.Count != workflow.Nodes.Count)
            {
                Logger.LogWarning("工作流存在循环依赖,无法完成拓扑排序");
            }

            return result;
        }

        /// <summary>
        /// 获取并行执行分组 - 返回可以并行执行的节点组
        /// </summary>
        public List<List<string>> GetParallelExecutionGroups(Workflow workflow)
        {
            var groups = new List<List<string>>();
            var executedNodes = new HashSet<string>();
            var graph = BuildGraph(workflow);

            while (executedNodes.Count < workflow.Nodes.Count)
            {
                var currentGroup = new List<string>();

                // 找出所有依赖已满足的节点
                foreach (var node in workflow.Nodes)
                {
                    if (executedNodes.Contains(node.Id))
                        continue;

                    // 检查所有依赖是否已执行
                    var dependencies = GetDependencies(node.Id, graph);
                    if (dependencies.All(dep => executedNodes.Contains(dep)))
                    {
                        currentGroup.Add(node.Id);
                    }
                }

                if (currentGroup.Count == 0)
                {
                    Logger.LogWarning("无法找到可执行的节点,可能存在循环依赖");
                    break;
                }

                groups.Add(currentGroup);
                foreach (var nodeId in currentGroup)
                {
                    executedNodes.Add(nodeId);
                }
            }

            Logger.LogInfo($"生成了 {groups.Count} 个并行执行组");
            for (int i = 0; i < groups.Count; i++)
            {
                Logger.LogInfo($"  组 {i + 1}: {string.Join(", ", groups[i])}");
            }

            return groups;
        }

        /// <summary>
        /// 检测循环 - 返回所有循环中的节点ID
        /// </summary>
        public List<string> DetectCycles(Workflow workflow)
        {
            var visited = new HashSet<string>();
            var recursionStack = new HashSet<string>();
            var cycles = new List<string>();
            var graph = BuildGraph(workflow);

            foreach (var node in workflow.Nodes)
            {
                if (!visited.Contains(node.Id))
                {
                    if (DetectCyclesDFS(node.Id, visited, recursionStack, graph, cycles))
                    {
                        break; // 找到至少一个循环就停止
                    }
                }
            }

            return cycles;
        }

        /// <summary>
        /// 获取节点优先级 - 用于决定执行顺序,值越小优先级越高
        /// </summary>
        public int GetNodePriority(Workflow workflow, string nodeId)
        {
            var graph = BuildGraph(workflow);
            return GetNodeDepth(nodeId, graph, new HashSet<string>());
        }

        /// <summary>
        /// 获取节点的最长依赖链深度
        /// </summary>
        private int GetNodeDepth(string nodeId, Dictionary<string, string> graph, HashSet<string> visited)
        {
            if (visited.Contains(nodeId))
                return 0;

            visited.Add(nodeId);

            var dependencies = GetDependencies(nodeId, graph);
            if (dependencies.Count == 0)
                return 0;

            int maxDepth = 0;
            foreach (var dep in dependencies)
            {
                maxDepth = Math.Max(maxDepth, GetNodeDepth(dep, graph, visited));
            }

            return maxDepth + 1;
        }

        /// <summary>
        /// 构建图结构
        /// </summary>
        private Dictionary<string, string> BuildGraph(Workflow workflow)
        {
            var graph = new Dictionary<string, string>();

            foreach (var connection in workflow.Connections)
            {
                foreach (var targetId in connection.Value)
                {
                    graph[connection.Key] = targetId;
                }
            }

            return graph;
        }

        /// <summary>
        /// 获取节点的所有依赖
        /// </summary>
        private List<string> GetDependencies(string nodeId, Dictionary<string, string> graph)
        {
            return graph.Where(kvp => kvp.Value == nodeId)
                        .Select(kvp => kvp.Key)
                        .ToList();
        }

        /// <summary>
        /// DFS 检测循环
        /// </summary>
        private bool DetectCyclesDFS(string nodeId, HashSet<string> visited, HashSet<string> recursionStack,
            Dictionary<string, string> graph, List<string> cycles)
        {
            visited.Add(nodeId);
            recursionStack.Add(nodeId);

            var dependencies = GetDependencies(nodeId, graph);
            foreach (var dep in dependencies)
            {
                if (!visited.Contains(dep))
                {
                    if (DetectCyclesDFS(dep, visited, recursionStack, graph, cycles))
                    {
                        cycles.Add(nodeId);
                        return true;
                    }
                }
                else if (recursionStack.Contains(dep))
                {
                    cycles.Add(nodeId);
                    cycles.Add(dep);
                    Logger.LogWarning($"检测到循环依赖: {dep} -> {nodeId}");
                    return true;
                }
            }

            recursionStack.Remove(nodeId);
            return false;
        }
    }
}
