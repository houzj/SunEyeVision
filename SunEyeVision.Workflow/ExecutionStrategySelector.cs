using System;
using System.Collections.Generic;
using System.Linq;
using SunEyeVision.Models;
using SunEyeVision.PluginSystem;
using SunEyeVision.PluginSystem.Base.Services;

namespace SunEyeVision.Workflow
{
    /// <summary>
    /// 执行策略枚举
    /// </summary>
    public enum ExecutionStrategy
    {
        /// <summary>
        /// 顺序执行 - 逐个节点依次执行
        /// </summary>
        Sequential,

        /// <summary>
        /// 并行执行 - 所有可并行节点同时执行
        /// </summary>
        Parallel,

        /// <summary>
        /// 混合执行 - 根据工作流特点智能选择
        /// </summary>
        Hybrid,

        /// <summary>
        /// 性能优化执行 - 基于历史执行数据优化
        /// </summary>
        PerformanceOptimized
    }

    /// <summary>
    /// 执行策略选择器 - 基于工作流特征智能选择执行策略
    /// </summary>
    public static class ExecutionStrategySelector
    {
        /// <summary>
        /// 选择最优执行策略
        /// </summary>
        /// <param name="workflow">工作流</param>
        /// <returns>选择的执行策略</returns>
        public static ExecutionStrategy SelectStrategy(Workflow workflow)
        {
            if (workflow == null)
            {
                return ExecutionStrategy.Sequential;
            }

            // 分析工作流特征
            var analysis = AnalyzeWorkflow(workflow);

            // 基于特征选择策略
            return SelectBasedOnCharacteristics(analysis);
        }

        /// <summary>
        /// 分析工作流特征
        /// </summary>
        private static WorkflowAnalysis AnalyzeWorkflow(Workflow workflow)
        {
            var analysis = new WorkflowAnalysis
            {
                TotalNodes = workflow.Nodes.Count,
                StartNodes = workflow.Nodes.Count(n => n.Type == NodeType.Start),
                HasStartNodes = workflow.Nodes.Any(n => n.Type == NodeType.Start)
            };

            // 计算节点深度
            analysis.MaxDepth = CalculateMaxDepth(workflow);

            // 计算并行度
            analysis.ParallelDegree = CalculateParallelDegree(workflow);

            // 获取执行链数量
            var chains = workflow.GetStartDrivenExecutionChains();
            analysis.ExecutionChainCount = chains.Count;

            // 分析节点特性
            var algorithmNodes = workflow.Nodes.OfType<AlgorithmNode>().ToList();
            analysis.PureFunctionNodes = algorithmNodes.Count(n => IsPureFunctionNode(n));
            analysis.LongRunningNodes = algorithmNodes.Count(n => IsLongRunningNode(n));
            analysis.SupportParallelNodes = algorithmNodes.Count(n => SupportsParallel(n));

            // 计算估计执行时间
            analysis.EstimatedExecutionTime = EstimateExecutionTime(workflow);

            return analysis;
        }

        /// <summary>
        /// 基于特征选择策略
        /// </summary>
        private static ExecutionStrategy SelectBasedOnCharacteristics(WorkflowAnalysis analysis)
        {
            // 策略1: 如果有Start节点,使用混合执行
            if (analysis.HasStartNodes && analysis.ExecutionChainCount > 1)
            {
                Console.WriteLine($"[ExecutionStrategySelector] 选择混合执行策略 (执行链数: {analysis.ExecutionChainCount})");
                return ExecutionStrategy.Hybrid;
            }

            // 策略2: 如果并行度高且支持并行的节点多,使用并行执行
            if (analysis.ParallelDegree > 2 && analysis.SupportParallelNodes > analysis.TotalNodes / 2)
            {
                Console.WriteLine($"[ExecutionStrategySelector] 选择并行执行策略 (并行度: {analysis.ParallelDegree})");
                return ExecutionStrategy.Parallel;
            }

            // 策略3: 如果节点都是纯函数,可以考虑并行
            if (analysis.PureFunctionNodes == analysis.TotalNodes && analysis.TotalNodes > 3)
            {
                Console.WriteLine($"[ExecutionStrategySelector] 选择并行执行策略 (纯函数节点: {analysis.PureFunctionNodes})");
                return ExecutionStrategy.Parallel;
            }

            // 策略4: 如果有长时间运行的节点且支持并行,使用性能优化策略
            if (analysis.LongRunningNodes > 0 && analysis.SupportParallelNodes > 0)
            {
                Console.WriteLine($"[ExecutionStrategySelector] 选择性能优化策略 (长运行节点: {analysis.LongRunningNodes})");
                return ExecutionStrategy.PerformanceOptimized;
            }

            // 默认: 顺序执行
            Console.WriteLine($"[ExecutionStrategySelector] 选择顺序执行策略 (节点数: {analysis.TotalNodes})");
            return ExecutionStrategy.Sequential;
        }

        /// <summary>
        /// 计算最大深度
        /// </summary>
        private static int CalculateMaxDepth(Workflow workflow)
        {
            var maxDepth = 0;

            foreach (var node in workflow.Nodes)
            {
                var depth = GetNodeDepth(workflow, node.Id, new HashSet<string>());
                if (depth > maxDepth)
                {
                    maxDepth = depth;
                }
            }

            return maxDepth;
        }

        /// <summary>
        /// 获取节点深度
        /// </summary>
        private static int GetNodeDepth(Workflow workflow, string nodeId, HashSet<string> visited)
        {
            if (visited.Contains(nodeId))
            {
                return 0; // 避免循环
            }

            visited.Add(nodeId);

            var parentIds = workflow.Connections
                .Where(kvp => kvp.Value.Contains(nodeId))
                .Select(kvp => kvp.Key)
                .ToList();

            if (parentIds.Count == 0)
            {
                return 1;
            }

            var maxParentDepth = 0;
            foreach (var parentId in parentIds)
            {
                var depth = GetNodeDepth(workflow, parentId, visited);
                if (depth > maxParentDepth)
                {
                    maxParentDepth = depth;
                }
            }

            return maxParentDepth + 1;
        }

        /// <summary>
        /// 计算并行度
        /// </summary>
        private static int CalculateParallelDegree(Workflow workflow)
        {
            var degree = new Dictionary<string, int>();

            // 计算每个节点的入度
            foreach (var node in workflow.Nodes)
            {
                var inputCount = workflow.Connections
                    .Count(kvp => kvp.Value.Contains(node.Id));
                degree[node.Id] = inputCount;
            }

            // 找出同时可以执行的节点数(入度为0的节点数)
            return degree.Values.Count(d => d == 0);
        }

        /// <summary>
        /// 判断是否为纯函数节点
        /// </summary>
        private static bool IsPureFunctionNode(AlgorithmNode node)
        {
            // 获取工具元数据
            var metadata = ToolRegistry.GetToolMetadata(node.AlgorithmType);
            return metadata != null && metadata.IsPureFunction;
        }

        /// <summary>
        /// 判断是否为长时间运行节点
        /// </summary>
        private static bool IsLongRunningNode(AlgorithmNode node)
        {
            // 获取工具元数据
            var metadata = ToolRegistry.GetToolMetadata(node.AlgorithmType);
            return metadata != null && metadata.EstimatedExecutionTimeMs > 1000;
        }

        /// <summary>
        /// 判断是否支持并行
        /// </summary>
        private static bool SupportsParallel(AlgorithmNode node)
        {
            // 获取工具元数据
            var metadata = ToolRegistry.GetToolMetadata(node.AlgorithmType);
            return metadata != null && metadata.SupportParallel;
        }

        /// <summary>
        /// 估计执行时间
        /// </summary>
        private static TimeSpan EstimateExecutionTime(Workflow workflow)
        {
            var totalTimeMs = 0;

            foreach (var node in workflow.Nodes)
            {
                if (node is AlgorithmNode algorithmNode)
                {
                    var metadata = ToolRegistry.GetToolMetadata(algorithmNode.AlgorithmType);
                    if (metadata != null)
                    {
                        totalTimeMs += metadata.EstimatedExecutionTimeMs;
                    }
                }
            }

            return TimeSpan.FromMilliseconds(totalTimeMs);
        }

        /// <summary>
        /// 工作流分析结果
        /// </summary>
        private class WorkflowAnalysis
        {
            public int TotalNodes { get; set; }
            public int StartNodes { get; set; }
            public bool HasStartNodes { get; set; }
            public int MaxDepth { get; set; }
            public int ParallelDegree { get; set; }
            public int ExecutionChainCount { get; set; }
            public int PureFunctionNodes { get; set; }
            public int LongRunningNodes { get; set; }
            public int SupportParallelNodes { get; set; }
            public TimeSpan EstimatedExecutionTime { get; set; }
        }
    }

    /// <summary>
    /// 混合执行计划 - 阶段化执行
    /// </summary>
    public class HybridExecutionPlan
    {
        /// <summary>
        /// 执行阶段
        /// </summary>
        public List<ExecutionPhase> Phases { get; set; } = new List<ExecutionPhase>();

        /// <summary>
        /// 总估计执行时间
        /// </summary>
        public TimeSpan EstimatedTotalTime { get; set; }

        /// <summary>
        /// 添加阶段
        /// </summary>
        public void AddPhase(ExecutionPhase phase)
        {
            Phases.Add(phase);
        }

        /// <summary>
        /// 获取并行阶段
        /// </summary>
        public List<ExecutionPhase> GetParallelPhases()
        {
            return Phases.Where(p => p.IsParallel).ToList();
        }

        /// <summary>
        /// 获取顺序阶段
        /// </summary>
        public List<ExecutionPhase> GetSequentialPhases()
        {
            return Phases.Where(p => !p.IsParallel).ToList();
        }
    }

    /// <summary>
    /// 执行阶段
    /// </summary>
    public class ExecutionPhase
    {
        /// <summary>
        /// 阶段ID
        /// </summary>
        public string PhaseId { get; set; } = string.Empty;

        /// <summary>
        /// 阶段名称
        /// </summary>
        public string PhaseName { get; set; } = string.Empty;

        /// <summary>
        /// 节点ID列表
        /// </summary>
        public List<string> NodeIds { get; set; } = new List<string>();

        /// <summary>
        /// 是否并行执行
        /// </summary>
        public bool IsParallel { get; set; }

        /// <summary>
        /// 估计执行时间
        /// </summary>
        public TimeSpan EstimatedTime { get; set; }

        /// <summary>
        /// 依赖的阶段ID列表
        /// </summary>
        public List<string> Dependencies { get; set; } = new List<string>();
    }
}
