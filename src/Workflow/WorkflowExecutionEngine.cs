using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OpenCvSharp;
using SunEyeVision.Core.Interfaces;
using SunEyeVision.Core.Models;
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.Infrastructure.Infrastructure;

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
    /// 简化版本：使用基于拓扑排序的并行组执行
    /// </summary>
    public class WorkflowExecutionEngine
    {
        private readonly WorkflowEngine _workflowEngine;
        private readonly IPluginManager _pluginManager;
        private readonly ILogger _logger;
        private readonly ToolExecutor _toolExecutor;
        
        // 执行状态管理
        private WorkflowExecutionState _currentState;
        private CancellationTokenSource _cancellationTokenSource;
        private WorkflowContext? _currentContext;
        
        // 执行统计
        private DateTime _executionStartTime;

        /// <summary>
        /// 当前执行状态
        /// </summary>
        public WorkflowExecutionState CurrentState => _currentState;

        /// <summary>
        /// 当前执行上下文
        /// </summary>
        public WorkflowContext CurrentContext => _currentContext;

        /// <summary>
        /// 节点执行状态变化事件
        /// </summary>
        public event EventHandler<NodeExecutionStatus>? NodeStatusChanged;

        /// <summary>
        /// 执行完成事件
        /// </summary>
        public event EventHandler<ExecutionResult>? ExecutionCompleted;

        /// <summary>
        /// 创建工作流执行引擎
        /// </summary>
        /// <param name="workflowEngine">工作流引擎</param>
        /// <param name="pluginManager">插件管理器</param>
        /// <param name="logger">日志器</param>
        /// <param name="toolExecutor">工具执行器（可选，不提供时自动创建）</param>
        public WorkflowExecutionEngine(
            WorkflowEngine workflowEngine, 
            IPluginManager pluginManager, 
            ILogger logger,
            ToolExecutor? toolExecutor = null)
        {
            _workflowEngine = workflowEngine ?? throw new ArgumentNullException(nameof(workflowEngine));
            _pluginManager = pluginManager ?? throw new ArgumentNullException(nameof(pluginManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _toolExecutor = toolExecutor ?? new ToolExecutor(new ParameterResolver());
            _currentState = WorkflowExecutionState.Idle;
        }

        #region 同步执行

        /// <summary>
        /// 使用基于拓扑排序的并行组执行工作流（简化方案）
        /// </summary>
        private async Task<ExecutionResult> ExecuteWorkflowWithParallelGroups(
            Workflow workflow,
            Mat inputImage,
            WorkflowContext context)
        {
            var result = new ExecutionResult();
            var nodeResults = new ConcurrentDictionary<string, object>();
            var nodeExecutionResults = new ConcurrentDictionary<string, NodeExecutionResult>();
            int totalExecutedNodes = 0;

            _logger.LogInfo($"========== 使用基于执行链的并行组执行模式 ==========");

            // 获取并行执行组（基于执行链识别）
            var parallelGroups = workflow.GetParallelExecutionGroupsByChains();
            _logger.LogInfo($"获取到 {parallelGroups.Count} 个并行执行组");

            // 显示执行组信息
            for (int i = 0; i < parallelGroups.Count; i++)
            {
                var group = parallelGroups[i];
                var groupInfo = $"组{i + 1}: {string.Join(", ", group.Select(id => 
                {
                    var node = workflow.Nodes.FirstOrDefault(n => n.Id == id);
                    return node != null ? $"{node.Name}({id})" : id;
                }))}";
                _logger.LogInfo($"  {groupInfo}");
            }
            _logger.LogInfo($"=====================================================");

            // 按组顺序执行
            for (int groupIndex = 0; groupIndex < parallelGroups.Count; groupIndex++)
            {
                var group = parallelGroups[groupIndex];
                _logger.LogInfo($"");
                _logger.LogInfo($"========== 执行组 {groupIndex + 1}/{parallelGroups.Count} ({group.Count}个节点) ==========");

                if (_cancellationTokenSource?.IsCancellationRequested == true)
                {
                    throw new OperationCanceledException();
                }

                // 并行执行当前组的节点
                var groupStartTime = DateTime.Now;
                var groupResult = await ExecuteNodesInParallel(
                    workflow,
                    group,
                    inputImage,
                    context,
                    nodeResults,
                    nodeExecutionResults);

                totalExecutedNodes += group.Count;

                var groupDuration = (DateTime.Now - groupStartTime).TotalMilliseconds;
                _logger.LogInfo($"组 {groupIndex + 1} 执行完成, 耗时: {groupDuration:F2}ms, 成功节点: {groupResult.Success}");

                if (!groupResult.Success)
                {
                    _currentState = WorkflowExecutionState.Error;
                    return groupResult;
                }
            }

            // 合并所有节点结果
            result.Success = nodeExecutionResults.Values.All(r => r.Success);
            result.ExecutionTime = DateTime.Now - _executionStartTime;
            result.NodeResults = nodeExecutionResults.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            // 收集错误
            foreach (var nodeResult in nodeExecutionResults.Values.Where(r => !r.Success))
            {
                if (nodeResult.ErrorMessages?.Any() == true)
                {
                    foreach (var error in nodeResult.ErrorMessages.Where(e => e != null))
                    {
                        result.AddError(error, nodeResult.NodeId);
                    }
                }
            }

            // 合并成功节点的输出
            if (result.Success && nodeResults.Any())
            {
                result.Outputs = new Dictionary<string, object>
                {
                    { "FinalResult", nodeResults.Values.Last() }
                };
            }

            _logger.LogInfo($"");
            _logger.LogInfo($"========== 工作流执行总结 ==========");
            _logger.LogInfo($"  总执行节点: {totalExecutedNodes}");
            _logger.LogInfo($"  执行组数: {parallelGroups.Count}");
            _logger.LogInfo($"  成功节点: {nodeExecutionResults.Values.Count(r => r.Success)}");
            _logger.LogInfo($"  失败节点: {nodeExecutionResults.Values.Count(r => !r.Success)}");
            _logger.LogInfo($"  总耗时: {(DateTime.Now - _executionStartTime).TotalMilliseconds:F2} ms");
            _logger.LogInfo($"======================================");

            ExecutionCompleted?.Invoke(this, result);
            return result;
        }

        /// <summary>
        /// 并行执行一组节点（同一组内节点无依赖关系）
        /// </summary>
        private async Task<ExecutionResult> ExecuteNodesInParallel(
            Workflow workflow,
            List<string> nodeIds,
            Mat defaultInput,
            WorkflowContext context,
            ConcurrentDictionary<string, object> nodeResults,
            ConcurrentDictionary<string, NodeExecutionResult> nodeExecutionResults)
        {
            var result = new ExecutionResult();

            // 如果组内只有一个节点，直接顺序执行
            if (nodeIds.Count == 1)
            {
                var node = workflow.Nodes.FirstOrDefault(n => n.Id == nodeIds[0]);
                if (node != null && node.IsEnabled)
                {
                    _logger.LogInfo($"  执行节点: {node.Name} (ID: {nodeIds[0]})");
                    context.UpdateNodeStatus(node.Id, NodeStatus.Running);
                    NodeStatusChanged?.Invoke(this, new NodeExecutionStatus { NodeId = node.Id, Status = NodeStatus.Running });

                    var nodeInput = GetNodeInput(workflow, node, defaultInput, nodeResults.ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
                    var nodeResult = await ExecuteNode(node, nodeInput, context, workflow);

                    var duration = nodeResult.Duration?.TotalMilliseconds ?? 0;
                    _logger.LogInfo($"  节点执行完成: {node.Name}, 状态: {(nodeResult.Success ? "✅ 成功" : "❌ 失败")}, 耗时: {duration:F2}ms");

                    if (nodeResult.Success && nodeResult.Outputs?.Any() == true)
                    {
                        var firstOutput = nodeResult.Outputs.Values.FirstOrDefault();
                        if (firstOutput != null)
                        {
                            nodeResults.TryAdd(node.Id, firstOutput);
                        }
                    }

                    nodeExecutionResults.TryAdd(node.Id, nodeResult);
                    context.UpdateNodeStatus(node.Id, nodeResult.Success ? NodeStatus.Completed : NodeStatus.Failed);
                    NodeStatusChanged?.Invoke(this, new NodeExecutionStatus { NodeId = node.Id, Status = nodeResult.Success ? NodeStatus.Completed : NodeStatus.Failed });

                    if (!nodeResult.Success)
                    {
                        var errorMessage = nodeResult.ErrorMessages?.FirstOrDefault() ?? "执行失败";
                        result.AddError(errorMessage, node.Id);
                    }
                }
                result.Success = result.Errors.Count == 0;
                return result;
            }

            // 并行执行组内所有节点
            var tasks = nodeIds.Select(async nodeId =>
            {
                var node = workflow.Nodes.FirstOrDefault(n => n.Id == nodeId);
                if (node != null && node.IsEnabled)
                {
                    context.UpdateNodeStatus(node.Id, NodeStatus.Running);
                    NodeStatusChanged?.Invoke(this, new NodeExecutionStatus { NodeId = node.Id, Status = NodeStatus.Running });

                    var nodeInput = GetNodeInput(workflow, node, defaultInput, nodeResults.ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
                    var nodeResult = await ExecuteNode(node, nodeInput, context, workflow);

                    var duration = nodeResult.Duration?.TotalMilliseconds ?? 0;
                    _logger.LogInfo($"  节点执行完成: {node.Name}, 状态: {(nodeResult.Success ? "✅ 成功" : "❌ 失败")}, 耗时: {duration:F2}ms");

                    if (nodeResult.Success && nodeResult.Outputs?.Any() == true)
                    {
                        var firstOutput = nodeResult.Outputs.Values.FirstOrDefault();
                        if (firstOutput != null)
                        {
                            nodeResults.TryAdd(node.Id, firstOutput);
                        }
                    }

                    nodeExecutionResults.TryAdd(node.Id, nodeResult);
                    context.UpdateNodeStatus(node.Id, nodeResult.Success ? NodeStatus.Completed : NodeStatus.Failed);
                    NodeStatusChanged?.Invoke(this, new NodeExecutionStatus { NodeId = node.Id, Status = nodeResult.Success ? NodeStatus.Completed : NodeStatus.Failed });

                    if (!nodeResult.Success)
                    {
                        var errorMessage = nodeResult.ErrorMessages?.FirstOrDefault() ?? "执行失败";
                        result.AddError(errorMessage, node.Id);
                    }
                }
            }).ToArray();

            await Task.WhenAll(tasks);

            result.Success = result.Errors.Count == 0;
            return result;
        }

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
                
                // 使用基于拓扑排序的并行组执行（简化方案）
                var result = await ExecuteWorkflowWithParallelGroups(workflow, inputImage, _currentContext);
                
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
        /// 获取节点输入 - 总是返回所有父节点的输出
        /// </summary>
        /// <returns>
        /// - 无父节点：返回 defaultInput
        /// - 单个父节点：返回父节点输出（object类型）
        /// - 多个父节点：返回 List<object>，包含所有父节点输出
        /// </returns>
        private static object GetNodeInput(Workflow workflow, WorkflowNode node, object defaultInput, Dictionary<string, object> nodeResults)
        {
            // 查找所有父节点
            var parentIds = workflow.Connections
                .Where(kvp => kvp.Value.Contains(node.Id))
                .Select(kvp => kvp.Key)
                .ToList();

            if (parentIds.Count == 0)
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
            object inputImage,
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
                return nodeResult;
            }
            catch (Exception ex)
            {
                nodeResult.EndTime = DateTime.Now;
                nodeResult.Success = false;
                nodeResult.ErrorMessages = new List<string> { ex.Message };

                _logger.LogError($"节点 {node.Name} 执行失败: {ex.Message}", ex);
                return nodeResult;
            }
        }

        /// <summary>
        /// 执行算法节点
        /// </summary>
        /// <remarks>
        /// 使用 ToolExecutor 统一执行工具，确保与单工具调试使用相同的执行路径。
        /// </remarks>
        private NodeExecutionResult ExecuteAlgorithmNode(AlgorithmNode node, Mat inputImage)
        {
            var result = new NodeExecutionResult { NodeId = node.Id, StartTime = DateTime.Now, EndTime = DateTime.Now };

            // 直接调用 AlgorithmNode.Execute，它内部已经包含了参数绑定解析逻辑
            // 未来可以重构为完全使用 ToolExecutor
            var algorithmResult = node.Execute(inputImage);

            if (algorithmResult.Success && algorithmResult.ResultImage != null)
            {
                result.Outputs = new Dictionary<string, object>
                {
                    { "Output", algorithmResult.ResultImage }
                };
                result.Success = true;
                
                // 传递结果项和原始工具结果
                result.ResultItems = algorithmResult.ResultItems;
                result.ToolResult = algorithmResult.ToolResults;
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
