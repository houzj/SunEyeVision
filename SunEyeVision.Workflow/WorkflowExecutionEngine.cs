using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SunEyeVision.Interfaces;
using SunEyeVision.Models;
using SunEyeVision.PluginSystem;
using SunEyeVision.PluginSystem.Base.Interfaces;
using SunEyeVision.PluginSystem.Base.Services;

namespace SunEyeVision.Workflow
{
    /// <summary>
    /// 工作流执行状态
    /// </summary>
    public enum WorkflowExecutionState
    {
        Idle,           // 空闲
        Running,        // 运行中
        Paused,         // 已暂停
        Stopped,        // 已停止
        Completed,      // 已完成
        Error           // 错误
    }

    /// <summary>
    /// 工作流执行引擎 - 扩展WorkflowEngine以支持工作流控制节点
    /// </summary>
    public class WorkflowExecutionEngine
    {
        private readonly WorkflowEngine _workflowEngine;
        private readonly IPluginManager _pluginManager;
        private readonly ILogger _logger;
        
        // 执行状态管理
        private WorkflowExecutionState _currentState;
        private CancellationTokenSource _cancellationTokenSource;
        private WorkflowContext? _currentContext;  // 改为可空
        private Task<ExecutionResult>? _executionTask;  // 改为可空
        
        // 执行统计
        private DateTime _executionStartTime;
        private string _currentNodeId;

        /// <summary>
        /// 当前执行状态
        /// </summary>
        public WorkflowExecutionState CurrentState => _currentState;

        /// <summary>
        /// 当前执行上下文
        /// </summary>
        public WorkflowContext CurrentContext => _currentContext;

        /// <summary>
        /// 当前执行的节点ID
        /// </summary>
        public string CurrentNodeId => _currentNodeId;

        /// <summary>
        /// 执行进度事件
        /// </summary>
        public event EventHandler<ExecutionProgress>? ProgressChanged;

        /// <summary>
        /// 节点执行状态变化事件
        /// </summary>
        public event EventHandler<NodeExecutionStatus>? NodeStatusChanged;

        /// <summary>
        /// 执行完成事件
        /// </summary>
        public event EventHandler<ExecutionResult>? ExecutionCompleted;

        public WorkflowExecutionEngine(WorkflowEngine workflowEngine, IPluginManager pluginManager, ILogger logger)
        {
            _workflowEngine = workflowEngine ?? throw new ArgumentNullException(nameof(workflowEngine));
            _pluginManager = pluginManager ?? throw new ArgumentNullException(nameof(pluginManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _currentState = WorkflowExecutionState.Idle;
        }

        #region 同步执行

        /// <summary>
        /// 执行工作流（同步，支持工作流控制节点）
        /// </summary>
        public async Task<ExecutionResult> ExecuteWorkflow(string workflowId, Mat inputImage)
        {
            if (_currentState == WorkflowExecutionState.Running)
            {
                _logger.LogWarning("工作流正在执行中，无法重复执行");
                return ExecutionResult.CreateFailure("工作流正在执行中");
            }

            var workflow = _workflowEngine.GetWorkflow(workflowId);
            if (workflow == null)
            {
                _logger.LogError($"工作流不存在: {workflowId}");
                return ExecutionResult.CreateFailure($"工作流不存在: {workflowId}");
            }

            // 检测循环依赖
            var cycles = workflow.DetectCycles();
            if (cycles.Count > 0)
            {
                var cycleMsg = $"检测到循环依赖: {string.Join("; ", cycles)}";
                _logger.LogError(cycleMsg);
                return ExecutionResult.CreateFailure(cycleMsg);
            }

            // 创建执行上下文
            _currentContext = new WorkflowContext();
            _currentContext.SetVariable("InputImage", inputImage);

            _currentState = WorkflowExecutionState.Running;
            _executionStartTime = DateTime.Now;

            try
            {
                _logger.LogInfo($"开始执行工作流: {workflow.Name}");

                // 检查是否有Start节点
                var hasStartNodes = workflow.Nodes.Any(n => n.Type == NodeType.Start);

                // 智能选择执行策略
                var strategy = ExecutionStrategySelector.SelectStrategy(workflow);
                _logger.LogInfo($"选择执行策略: {strategy}");

                ExecutionResult result;
                switch (strategy)
                {
                    case ExecutionStrategy.Sequential:
                        // 顺序执行
                        _logger.LogInfo("使用顺序执行模式");
                        var executionOrder = workflow.GetExecutionOrder();
                        result = await ExecuteNodesSequential(workflow, executionOrder, inputImage, _currentContext);
                        break;

                    case ExecutionStrategy.Parallel:
                        // 并行执行
                        _logger.LogInfo("使用并行执行模式");
                        result = await ExecuteWorkflowParallel(workflow, inputImage, _currentContext);
                        break;

                    case ExecutionStrategy.Hybrid:
                        // 混合执行
                        _logger.LogInfo("使用混合执行模式");
                        result = await ExecuteWorkflowHybrid(workflow, inputImage, _currentContext);
                        break;

                    case ExecutionStrategy.PerformanceOptimized:
                        // 性能优化执行
                        _logger.LogInfo("使用性能优化执行模式");
                        result = await ExecuteWorkflowOptimized(workflow, inputImage, _currentContext);
                        break;

                    default:
                        // 默认顺序执行
                        executionOrder = workflow.GetExecutionOrder();
                        result = await ExecuteNodesSequential(workflow, executionOrder, inputImage, _currentContext);
                        break;
                }

                _currentState = result.Success ? WorkflowExecutionState.Completed : WorkflowExecutionState.Error;

                _logger.LogInfo($"工作流执行完成: {workflow.Name}, 耗时: {(DateTime.Now - _executionStartTime).TotalMilliseconds:F2}ms");

                return result;
            }
            catch (OperationCanceledException)
            {
                _currentState = WorkflowExecutionState.Stopped;
                _logger.LogWarning("工作流执行被取消");
                var result = ExecutionResult.CreateFailure("工作流执行被取消");
                result.IsStopped = true;
                return result;
            }
            catch (Exception ex)
            {
                _currentState = WorkflowExecutionState.Error;
                _logger.LogError($"工作流执行失败: {ex.Message}", ex);
                return ExecutionResult.CreateFailure($"{ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                _currentContext = null;
                if (_currentState != WorkflowExecutionState.Paused)
                {
                    _currentState = WorkflowExecutionState.Idle;
                }
            }
        }

        /// <summary>
        /// 按顺序执行节点（支持工作流控制节点）
        /// </summary>
        private async Task<ExecutionResult> ExecuteNodesSequential(
            Workflow workflow,
            List<string> executionOrder,
            Mat inputImage,
            WorkflowContext context)
        {
            var result = new ExecutionResult();
            var nodeResults = new Dictionary<string, object>();  // 修改为object类型
            var nodeExecutionResults = new Dictionary<string, NodeExecutionResult>();

            // 记录执行顺序
            _logger.LogInfo($"========== 节点执行顺序 (共{executionOrder.Count}个节点) ==========");
            for (int i = 0; i < executionOrder.Count; i++)
            {
                var node = workflow.Nodes.FirstOrDefault(n => n.Id == executionOrder[i]);
                var nodeInfo = node != null ? $"{node.Name} ({node.AlgorithmType})" : executionOrder[i];
                _logger.LogInfo($"  [{i + 1}] {executionOrder[i]} - {nodeInfo}");
            }
            _logger.LogInfo($"=====================================================");

            int executedNodeCount = 0;
            foreach (var nodeId in executionOrder)
            {
                if (_cancellationTokenSource?.IsCancellationRequested == true)
                {
                    throw new OperationCanceledException();
                }

                var node = workflow.Nodes.FirstOrDefault(n => n.Id == nodeId);
                if (node == null || !node.IsEnabled)
                {
                    continue;
                }

                executedNodeCount++;
                var nodeStartTime = DateTime.Now;
                var nodeIndex = executedNodeCount;

                _currentNodeId = nodeId;
                _logger.LogInfo($"");
                _logger.LogInfo($"[{nodeIndex}/{executionOrder.Count}] 开始执行节点: {node.Name} (ID: {nodeId}, 类型: {node.AlgorithmType})");

                context.UpdateNodeStatus(nodeId, NodeStatus.Running);
                NodeStatusChanged?.Invoke(this, new NodeExecutionStatus { NodeId = nodeId, Status = NodeStatus.Running });

                // 获取节点输入
                var nodeInput = GetNodeInput(workflow, node, inputImage, nodeResults);

                // 执行节点
                var nodeResult = await ExecuteNode(node, nodeInput, context, workflow);

                var nodeEndTime = DateTime.Now;
                var nodeDuration = (nodeEndTime - nodeStartTime).TotalMilliseconds;

                // 记录节点执行结果
                _logger.LogInfo($"[{nodeIndex}/{executionOrder.Count}] 节点执行完成: {node.Name}");
                _logger.LogInfo($"  └─ 状态: {(nodeResult.Success ? "✓ 成功" : "✗ 失败")}");
                _logger.LogInfo($"  └─ 耗时: {nodeDuration:F2} ms");

                if (!nodeResult.Success && nodeResult.ErrorMessages?.Any() == true)
                {
                    foreach (var error in nodeResult.ErrorMessages.Where(e => e != null))
                    {
                        _logger.LogError($"  └─ 错误: {error}");
                    }
                }

                // 记录结果
                if (nodeResult.Success && nodeResult.Outputs?.Any() == true)
                {
                    nodeResults[nodeId] = nodeResult.Outputs.Values.First();
                }

                nodeExecutionResults[nodeId] = nodeResult;
                context.UpdateNodeStatus(nodeId, nodeResult.Success ? NodeStatus.Completed : NodeStatus.Failed);
                NodeStatusChanged?.Invoke(this, new NodeExecutionStatus { NodeId = nodeId, Status = nodeResult.Success ? NodeStatus.Completed : NodeStatus.Failed });

                if (!nodeResult.Success)
                {
                    var errorMessage = nodeResult.ErrorMessages?.FirstOrDefault() ?? "执行失败";
                    result.AddError(errorMessage, nodeId);
                }
            }

            _logger.LogInfo($"");
            _logger.LogInfo($"========== 工作流执行总结 ==========");
            _logger.LogInfo($"  总执行节点: {executedNodeCount}/{executionOrder.Count}");
            _logger.LogInfo($"  成功节点: {nodeExecutionResults.Values.Count(r => r.Success)}");
            _logger.LogInfo($"  失败节点: {nodeExecutionResults.Values.Count(r => !r.Success)}");
            _logger.LogInfo($"  总耗时: {(DateTime.Now - _executionStartTime).TotalMilliseconds:F2} ms");
            _logger.LogInfo($"======================================");

            result.Success = result.Errors.Count == 0;
            result.ExecutionTime = DateTime.Now - _executionStartTime;
            result.NodeResults = nodeExecutionResults;

            // 合并成功节点的输出
            if (result.Success && nodeResults.Any())
            {
                result.Outputs = new Dictionary<string, object>
                {
                    { "FinalResult", nodeResults.Values.Last() }
                };
            }

            ExecutionCompleted?.Invoke(this, result);
            return result;
        }

        /// <summary>
        /// 混合执行工作流 - 根据工作流特征智能选择顺序或并行
        /// </summary>
        private async Task<ExecutionResult> ExecuteWorkflowHybrid(
            Workflow workflow,
            Mat inputImage,
            WorkflowContext context)
        {
            var result = new ExecutionResult();
            var nodeResults = new ConcurrentDictionary<string, object>();
            var nodeExecutionResults = new ConcurrentDictionary<string, NodeExecutionResult>();

            _logger.LogInfo($"========== 混合执行模式 ==========");

            // 获取执行链
            var chains = workflow.GetStartDrivenExecutionChains();

            if (chains.Count == 0)
            {
                // 无执行链,使用顺序执行
                _logger.LogInfo("无执行链,回退到顺序执行");
                var executionOrder = workflow.GetExecutionOrder();
                return await ExecuteNodesSequential(workflow, executionOrder, inputImage, context);
            }

            _logger.LogInfo($"检测到{chains.Count}条执行链");

            // 按依赖关系分组执行
            var executedChainIds = new HashSet<string>();
            int iteration = 0;

            while (executedChainIds.Count < chains.Count)
            {
                iteration++;
                _logger.LogInfo($"========== 执行迭代 {iteration} ==========");

                // 找出所有可以执行的链(依赖已满足)
                var readyChains = chains.Where(chain =>
                    !executedChainIds.Contains(chain.ChainId) &&
                    chain.Dependencies.All(dep => executedChainIds.Contains(dep.SourceChainId))
                ).ToList();

                _logger.LogInfo($"本批次可执行链数: {readyChains.Count}");

                if (readyChains.Count == 0)
                {
                    // 检测到循环依赖
                    _logger.LogError("检测到循环依赖,无法继续执行");
                    result.AddError("检测到循环依赖");
                    break;
                }

                // 并行执行这些链
                var chainTasks = readyChains.Select(chain =>
                    ExecuteChainSequential(workflow, chain, inputImage, context, nodeResults, nodeExecutionResults)
                ).ToList();

                var chainResults = await Task.WhenAll(chainTasks);

                // 合并结果
                foreach (var chainResult in chainResults)
                {
                    result.Merge(chainResult);
                }

                // 标记已执行的链
                foreach (var chain in readyChains)
                {
                    executedChainIds.Add(chain.ChainId);
                }

                _logger.LogInfo($"迭代{iteration}完成, 已执行{executedChainIds.Count}/{chains.Count}条链");
            }

            result.Success = result.Errors.Count == 0;
            result.ExecutionTime = DateTime.Now - _executionStartTime;

            _logger.LogInfo($"========== 混合执行完成 ==========");

            ExecutionCompleted?.Invoke(this, result);
            return result;
        }

        /// <summary>
        /// 性能优化执行工作流 - 基于节点特性优化执行顺序
        /// </summary>
        private async Task<ExecutionResult> ExecuteWorkflowOptimized(
            Workflow workflow,
            Mat inputImage,
            WorkflowContext context)
        {
            var result = new ExecutionResult();
            var nodeResults = new ConcurrentDictionary<string, object>();
            var nodeExecutionResults = new ConcurrentDictionary<string, NodeExecutionResult>();

            _logger.LogInfo($"========== 性能优化执行模式 ==========");

            // 获取执行顺序
            var executionOrder = workflow.GetExecutionOrder();

            // 将节点按特性分组
            var fastNodes = new List<string>();
            var slowNodes = new List<string>();
            var ioNodes = new List<string>();

            foreach (var nodeId in executionOrder)
            {
                var node = workflow.Nodes.FirstOrDefault(n => n.Id == nodeId);
                if (node is AlgorithmNode algorithmNode)
                {
                    var metadata = ToolRegistry.GetToolMetadata(algorithmNode.AlgorithmType);
                    if (metadata != null)
                    {
                        if (metadata.HasSideEffects)
                        {
                            // 有副作用的节点(IO操作等)
                            ioNodes.Add(nodeId);
                        }
                        else if (metadata.EstimatedExecutionTimeMs > 500)
                        {
                            // 慢节点
                            slowNodes.Add(nodeId);
                        }
                        else
                        {
                            // 快节点
                            fastNodes.Add(nodeId);
                        }
                    }
                    else
                    {
                        fastNodes.Add(nodeId);
                    }
                }
                else
                {
                    fastNodes.Add(nodeId);
                }
            }

            _logger.LogInfo($"节点分类: 快节点{fastNodes.Count}, 慢节点{slowNodes.Count}, IO节点{ioNodes.Count}");

            // 优先执行IO节点
            foreach (var nodeId in ioNodes)
            {
                await ExecuteSingleNode(workflow, nodeId, inputImage, context, nodeResults, nodeExecutionResults);
            }

            // 并行执行快节点
            var fastNodeTasks = fastNodes.Select(nodeId =>
                ExecuteSingleNode(workflow, nodeId, inputImage, context, nodeResults, nodeExecutionResults)
            ).ToList();

            await Task.WhenAll(fastNodeTasks);

            // 最后执行慢节点(顺序执行,避免资源竞争)
            foreach (var nodeId in slowNodes)
            {
                await ExecuteSingleNode(workflow, nodeId, inputImage, context, nodeResults, nodeExecutionResults);
            }

            result.Success = result.Errors.Count == 0;
            result.ExecutionTime = DateTime.Now - _executionStartTime;

            _logger.LogInfo($"========== 性能优化执行完成 ==========");

            ExecutionCompleted?.Invoke(this, result);
            return result;
        }

        /// <summary>
        /// 执行单个节点
        /// </summary>
        private async Task ExecuteSingleNode(
            Workflow workflow,
            string nodeId,
            Mat defaultInput,
            WorkflowContext context,
            ConcurrentDictionary<string, object> nodeResults,
            ConcurrentDictionary<string, NodeExecutionResult> nodeExecutionResults)
        {
            var node = workflow.Nodes.FirstOrDefault(n => n.Id == nodeId);
            if (node == null || !node.IsEnabled)
            {
                return;
            }

            _logger.LogInfo($"执行节点: {node.Name} (ID: {nodeId})");

            context.UpdateNodeStatus(nodeId, NodeStatus.Running);
            NodeStatusChanged?.Invoke(this, new NodeExecutionStatus { NodeId = nodeId, Status = NodeStatus.Running });

            var nodeInput = GetNodeInput(workflow, node, defaultInput, nodeResults.ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
            var nodeResult = await ExecuteNode(node, nodeInput, context, workflow);

            if (nodeResult.Success && nodeResult.Outputs?.Any() == true)
            {
                var firstOutput = nodeResult.Outputs.Values.FirstOrDefault();
                if (firstOutput != null)
                {
                    nodeResults.TryAdd(nodeId, firstOutput);
                }
            }

            nodeExecutionResults.TryAdd(nodeId, nodeResult);
            context.UpdateNodeStatus(nodeId, nodeResult.Success ? NodeStatus.Completed : NodeStatus.Failed);
        }

        /// <summary>
        /// 并行执行工作流（支持多个执行链）
        /// </summary>
        private async Task<ExecutionResult> ExecuteWorkflowParallel(
            Workflow workflow,
            Mat inputImage,
            WorkflowContext context)
        {
            var result = new ExecutionResult();
            var nodeResults = new ConcurrentDictionary<string, object>();  // 线程安全
            var nodeExecutionResults = new ConcurrentDictionary<string, NodeExecutionResult>();

            // 获取执行链
            var chains = workflow.GetStartDrivenExecutionChains();

            _logger.LogInfo($"========== 执行链分析 (共{chains.Count}条链) ==========");
            for (int i = 0; i < chains.Count; i++)
            {
                var chain = chains[i];
                _logger.LogInfo($"  执行链[{i}]: {chain.ChainId}");
                _logger.LogInfo($"    - 起始节点: {chain.StartNodeId}");
                _logger.LogInfo($"    - 节点数量: {chain.NodeIds.Count}");
                _logger.LogInfo($"    - 跨链依赖: {chain.Dependencies.Count}");
            }
            _logger.LogInfo($"=====================================================");

            // 并行执行所有链
            var chainTasks = chains.Select(async chain =>
            {
                return await ExecuteChainSequential(workflow, chain, inputImage, context, nodeResults, nodeExecutionResults);
            }).ToList();

            // 等待所有链执行完成
            var chainResults = await Task.WhenAll(chainTasks);

            // 合并所有链的结果
            foreach (var chainResult in chainResults)
            {
                result.Merge(chainResult);
            }

            result.Success = result.Errors.Count == 0;
            result.ExecutionTime = DateTime.Now - _executionStartTime;

            ExecutionCompleted?.Invoke(this, result);
            return result;
        }

        /// <summary>
        /// 顺序执行单个执行链
        /// </summary>
        private async Task<ExecutionResult> ExecuteChainSequential(
            Workflow workflow,
            ExecutionChain chain,
            Mat defaultInput,
            WorkflowContext context,
            ConcurrentDictionary<string, object> nodeResults,
            ConcurrentDictionary<string, NodeExecutionResult> nodeExecutionResults)
        {
            var result = new ExecutionResult();

            _logger.LogInfo($"========== 开始执行链: {chain.ChainId} ==========");

            foreach (var nodeId in chain.NodeIds)
            {
                if (_cancellationTokenSource?.IsCancellationRequested == true)
                {
                    throw new OperationCanceledException();
                }

                var node = workflow.Nodes.FirstOrDefault(n => n.Id == nodeId);
                if (node == null || !node.IsEnabled)
                {
                    continue;
                }

                var nodeStartTime = DateTime.Now;

                _logger.LogInfo($"  执行节点: {node.Name} (ID: {nodeId})");

                context.UpdateNodeStatus(nodeId, NodeStatus.Running);
                NodeStatusChanged?.Invoke(this, new NodeExecutionStatus { NodeId = nodeId, Status = NodeStatus.Running });

                // 获取节点输入（可能是单个对象或List<object>）
                var nodeInput = GetNodeInput(workflow, node, defaultInput, nodeResults.ToDictionary(kvp => kvp.Key, kvp => kvp.Value));

                // 执行节点
                var nodeResult = await ExecuteNode(node, nodeInput, context, workflow);

                var nodeDuration = (DateTime.Now - nodeStartTime).TotalMilliseconds;

                // 记录结果到线程安全字典
                if (nodeResult.Success && nodeResult.Outputs?.Any() == true)
                {
                    var firstOutput = nodeResult.Outputs.Values.FirstOrDefault();
                    if (firstOutput != null)
                    {
                        nodeResults.TryAdd(nodeId, firstOutput);
                    }
                }

                nodeExecutionResults.TryAdd(nodeId, nodeResult);
                context.UpdateNodeStatus(nodeId, nodeResult.Success ? NodeStatus.Completed : NodeStatus.Failed);

                if (!nodeResult.Success)
                {
                    var errorMessage = nodeResult.ErrorMessages?.FirstOrDefault() ?? "执行失败";
                    result.AddError(errorMessage, nodeId);
                }
            }

            _logger.LogInfo($"========== 执行链完成: {chain.ChainId} ==========");

            return result;
        }

        /// <summary>
        /// 获取节点输入 - 总是返回所有父节点的输出
        /// </summary>
        /// <returns>
        /// - 无父节点：返回 defaultInput
        /// - 单个父节点：返回父节点输出（object类型）
        /// - 多个父节点：返回 List<object>，包含所有父节点输出
        /// </returns>
        private object GetNodeInput(Workflow workflow, WorkflowNode node, object defaultInput, Dictionary<string, object> nodeResults)
        {
            // 查找所有父节点
            var parentIds = workflow.Connections
                .Where(kvp => kvp.Value.Contains(node.Id))
                .Select(kvp => kvp.Key)
                .ToList();

            if (!parentIds.Any())
            {
                // 无父节点，返回默认输入
                return defaultInput;
            }

            // 收集所有可用父节点的输出
            var parentOutputs = new List<object>();
            foreach (var parentId in parentIds)
            {
                if (nodeResults.ContainsKey(parentId))
                {
                    parentOutputs.Add(nodeResults[parentId]);
                }
            }

            if (parentOutputs.Count == 0)
            {
                // 父节点尚未执行完成，返回默认输入
                return defaultInput;
            }

            if (parentOutputs.Count == 1)
            {
                // 单个父节点，直接返回其输出
                return parentOutputs[0];
            }

            // 多个父节点，返回所有输出
            return parentOutputs;
        }

        /// <summary>
        /// 执行单个节点（支持多种节点类型）
        /// </summary>
        private async Task<NodeExecutionResult> ExecuteNode(
            WorkflowNode node,
            object inputImage,  // 改为object类型，支持Mat或List<object>
            WorkflowContext context,
            Workflow workflow)
        {
            var nodeResult = new NodeExecutionResult
            {
                NodeId = node.Id,
                StartTime = DateTime.Now
            };

            try
            {
                // 处理多输入情况
                Mat matInput = null;
                if (inputImage is Mat)
                {
                    matInput = inputImage as Mat;
                }
                else if (inputImage is List<object> inputList && inputList.Count > 0)
                {
                    // 多输入情况：使用第一个输入作为主输入（算法节点默认行为）
                    // 算法节点可以通过其他方式获取所有输入
                    matInput = inputList[0] as Mat;
                }
                else
                {
                    matInput = null;
                }

                // 根据节点类型执行
                switch (node)
                {
                    case AlgorithmNode algorithmNode:
                        var algoResult = ExecuteAlgorithmNode(algorithmNode, matInput);
                        nodeResult.Outputs = algoResult.Outputs;
                        nodeResult.Success = algoResult.Success;
                        nodeResult.ErrorMessages = algoResult.ErrorMessages;
                        break;

                    case SubroutineNode subroutineNode:
                        nodeResult = await ExecuteSubroutineNode(subroutineNode, matInput, context, workflow);
                        break;

                    case ConditionNode conditionNode:
                        var condResult = ExecuteConditionNode(conditionNode, matInput, context);
                        nodeResult.Outputs = condResult.Outputs;
                        nodeResult.Success = condResult.Success;
                        nodeResult.ErrorMessages = condResult.ErrorMessages;
                        break;

                    case WorkflowNode workflowNode when workflowNode.Type == NodeType.Start:
                        // Start节点直接传递输入
                        nodeResult.Outputs = new Dictionary<string, object>
                        {
                            { "Output", inputImage ?? context.GetVariable("InputImage") }
                        };
                        nodeResult.Success = true;
                        break;

                    default:
                        throw new NotSupportedException($"不支持的节点类型: {node.GetType().Name}");
                }

                nodeResult.EndTime = DateTime.Now;
                // Duration是计算属性，自动从StartTime和EndTime计算

                return nodeResult;
            }
            catch (Exception ex)
            {
                nodeResult.EndTime = DateTime.Now;
                // Duration是计算属性，自动从StartTime和EndTime计算
                nodeResult.Success = false;
                nodeResult.ErrorMessages = new List<string> { ex.Message };

                _logger.LogError($"节点 {node.Name} 执行失败: {ex.Message}", ex);
                return nodeResult;
            }
        }

        /// <summary>
        /// 执行算法节点
        /// </summary>
        private NodeExecutionResult ExecuteAlgorithmNode(AlgorithmNode node, Mat inputImage)
        {
            var result = new NodeExecutionResult { NodeId = node.Id, StartTime = DateTime.Now, EndTime = DateTime.Now };

            var algorithmResult = node.Execute(inputImage);

            if (algorithmResult.Success && algorithmResult.ResultImage != null)
            {
                result.Outputs = new Dictionary<string, object>
                {
                    { "Output", algorithmResult.ResultImage }
                };
                result.Success = true;
            }
            else if (!algorithmResult.Success)
            {
                result.ErrorMessages = new List<string> { algorithmResult.ErrorMessage };
                result.Success = false;
            }

            return result;
        }

        /// <summary>
        /// 执行子程序节点
        /// </summary>
        private async Task<NodeExecutionResult> ExecuteSubroutineNode(
            SubroutineNode node,
            Mat inputImage,
            WorkflowContext context,
            Workflow workflow)
        {
            // 查找工作流控制插件
            var plugins = _pluginManager.GetPlugins<IWorkflowControlPlugin>();
            var plugin = plugins.FirstOrDefault();

            if (plugin == null)
            {
                throw new InvalidOperationException("未找到工作流控制插件");
            }

            // 设置上下文变量
            context.SetVariable("InputImage", inputImage);
            context.SetVariable("CurrentNode", node);

            // 执行子程序
            var result = await plugin.ExecuteSubroutine(node, context);

            var nodeResult = new NodeExecutionResult { NodeId = node.Id };
            
            if (result.Success && result.Outputs?.Any() == true)
            {
                nodeResult.Outputs = result.Outputs;
            }
            else if (!result.Success)
            {
                nodeResult.ErrorMessages = new List<string> { 
                    result.Errors?.FirstOrDefault() ?? "子程序执行失败" 
                };
            }

            return nodeResult;
        }

        /// <summary>
        /// 执行条件节点
        /// </summary>
        private NodeExecutionResult ExecuteConditionNode(
            ConditionNode node,
            Mat inputImage,
            WorkflowContext context)
        {
            // 查找工作流控制插件
            var plugins = _pluginManager.GetPlugins<IWorkflowControlPlugin>();
            var plugin = plugins.FirstOrDefault();

            if (plugin == null)
            {
                throw new InvalidOperationException("未找到工作流控制插件");
            }

            // 设置上下文变量
            context.SetVariable("InputImage", inputImage);
            context.SetVariable("CurrentNode", node);

            // 评估条件
            var conditionResult = plugin.EvaluateCondition(node, context);

            var nodeResult = new NodeExecutionResult
            {
                NodeId = node.Id,
                StartTime = DateTime.Now,
                EndTime = DateTime.Now,
                Success = true
            };
            nodeResult.Outputs = new Dictionary<string, object>
            {
                { "ConditionResult", conditionResult },
                { "Output", inputImage }
            };

            return nodeResult;
        }

        #endregion

        #region 异步执行

        /// <summary>
        /// 异步执行工作流
        /// </summary>
        public async Task<ExecutionResult> ExecuteWorkflowAsync(string workflowId, Mat inputImage)
        {
            _cancellationTokenSource = new CancellationTokenSource();

            return await Task.Run(async () => await ExecuteWorkflow(workflowId, inputImage), _cancellationTokenSource.Token);
        }

        /// <summary>
        /// 暂停执行
        /// </summary>
        public void PauseExecution()
        {
            if (_currentState == WorkflowExecutionState.Running)
            {
                _currentState = WorkflowExecutionState.Paused;
                _logger.LogWarning("工作流执行已暂停");
            }
        }

        /// <summary>
        /// 恢复执行
        /// </summary>
        public void ResumeExecution()
        {
            if (_currentState == WorkflowExecutionState.Paused)
            {
                _currentState = WorkflowExecutionState.Running;
                _logger.LogWarning("工作流执行已恢复");
            }
        }

        /// <summary>
        /// 停止执行
        /// </summary>
        public void StopExecution()
        {
            _cancellationTokenSource?.Cancel();
            _logger.LogWarning("工作流执行停止请求已发送");
        }

        #endregion

        #region 状态查询

        /// <summary>
        /// 获取执行统计信息
        /// </summary>
        public ExecutionStatistics GetExecutionStatistics()
        {
            if (_currentContext == null)
            {
                return new ExecutionStatistics();
            }

            return _currentContext.GetStatistics();
        }

        #endregion
    }

    /// <summary>
    /// 节点执行状态事件参数
    /// </summary>
    public class NodeExecutionStatus : EventArgs
    {
        public string NodeId { get; set; } = string.Empty;
        public NodeStatus Status { get; set; }
    }
}
