using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SunEyeVision.Interfaces;
using SunEyeVision.Models;
using SunEyeVision.PluginSystem;

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
        private WorkflowContext _currentContext;
        private Task<ExecutionResult> _executionTask;
        
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

                // 执行节点
                var executionOrder = workflow.GetExecutionOrder();
                var result = await ExecuteNodesSequential(workflow, executionOrder, inputImage, _currentContext);

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
            var nodeResults = new Dictionary<string, Mat>();
            var nodeExecutionResults = new Dictionary<string, NodeExecutionResult>();

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

                _currentNodeId = nodeId;
                context.UpdateNodeStatus(nodeId, NodeStatus.Running);
                NodeStatusChanged?.Invoke(this, new NodeExecutionStatus { NodeId = nodeId, Status = NodeStatus.Running });

                // 获取节点输入
                var nodeInput = GetNodeInput(workflow, node, inputImage, nodeResults);

                // 执行节点
                var nodeResult = await ExecuteNode(node, nodeInput, context, workflow);
                
                // 记录结果
                if (nodeResult.Success && nodeResult.Outputs?.Any() == true)
                {
                    nodeResults[nodeId] = nodeResult.Outputs.Values.First() as Mat;
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
        /// 获取节点输入
        /// </summary>
        private Mat GetNodeInput(Workflow workflow, WorkflowNode node, Mat defaultInput, Dictionary<string, Mat> nodeResults)
        {
            // 查找父节点
            var parentIds = workflow.Connections
                .Where(kvp => kvp.Value.Contains(node.Id))
                .Select(kvp => kvp.Key)
                .ToList();

            if (!parentIds.Any())
            {
                return defaultInput;
            }

            // 使用第一个可用父节点的输出
            foreach (var parentId in parentIds)
            {
                if (nodeResults.ContainsKey(parentId))
                {
                    return nodeResults[parentId];
                }
            }

            return defaultInput;
        }

        /// <summary>
        /// 执行单个节点（支持多种节点类型）
        /// </summary>
        private async Task<NodeExecutionResult> ExecuteNode(
            WorkflowNode node,
            Mat inputImage,
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
                // 根据节点类型执行
                switch (node)
                {
                    case AlgorithmNode algorithmNode:
                        var algoResult = ExecuteAlgorithmNode(algorithmNode, inputImage);
                        nodeResult.Outputs = algoResult.Outputs;
                        nodeResult.Success = algoResult.Success;
                        nodeResult.ErrorMessages = algoResult.ErrorMessages;
                        break;

                    case SubroutineNode subroutineNode:
                        nodeResult = await ExecuteSubroutineNode(subroutineNode, inputImage, context, workflow);
                        break;

                    case ConditionNode conditionNode:
                        var condResult = ExecuteConditionNode(conditionNode, inputImage, context);
                        nodeResult.Outputs = condResult.Outputs;
                        nodeResult.Success = condResult.Success;
                        nodeResult.ErrorMessages = condResult.ErrorMessages;
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
