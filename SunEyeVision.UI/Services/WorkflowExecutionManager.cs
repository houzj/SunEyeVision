using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SunEyeVision.UI.Models;
using SunEyeVision.UI.ViewModels;
using SunEyeVision.Workflow;

namespace SunEyeVision.UI.Services
{
    /// <summary>
    /// 工作流执行管理器
    /// 负责管理工作流的单次运行和连续运行
    /// </summary>
    public class WorkflowExecutionManager
    {
        private readonly IInputProvider _inputProvider;
        private readonly Dictionary<string, CancellationTokenSource> _cancellationTokens;
        private readonly Dictionary<string, Task> _runningTasks;
        private readonly object _lock = new object();
        private SynchronizationContext? _synchronizationContext;

        public event EventHandler<WorkflowExecutionEventArgs>? WorkflowExecutionStarted;
        public event EventHandler<WorkflowExecutionEventArgs>? WorkflowExecutionCompleted;
        public event EventHandler<WorkflowExecutionEventArgs>? WorkflowExecutionStopped;
        public event EventHandler<WorkflowExecutionEventArgs>? WorkflowExecutionError;
        public event EventHandler<WorkflowExecutionProgressEventArgs>? WorkflowExecutionProgress;

        public WorkflowExecutionManager(IInputProvider inputProvider)
        {
            _inputProvider = inputProvider ?? throw new ArgumentNullException(nameof(inputProvider));
            _cancellationTokens = new Dictionary<string, CancellationTokenSource>();
            _runningTasks = new Dictionary<string, Task>();
            _synchronizationContext = SynchronizationContext.Current;
        }

        /// <summary>
        /// 单次运行指定工作流
        /// </summary>
        public async Task RunSingleAsync(WorkflowTabViewModel workflow, CancellationToken cancellationToken = default)
        {
            if (workflow == null)
                throw new ArgumentNullException(nameof(workflow));

            var workflowId = workflow.Name;
            OnWorkflowExecutionStarted(workflowId);

            try
            {
                await ExecuteWorkflowInternal(workflow, cancellationToken);
                OnWorkflowExecutionCompleted(workflowId);
            }
            catch (OperationCanceledException)
            {
                OnWorkflowExecutionStopped(workflowId);
            }
            catch (Exception ex)
            {
                OnWorkflowExecutionError(workflowId, ex.Message);
            }
        }

        /// <summary>
        /// 开始连续运行指定工作流
        /// </summary>
        public void StartContinuousRun(WorkflowTabViewModel workflow)
        {
            if (workflow == null)
                throw new ArgumentNullException(nameof(workflow));

            var workflowId = workflow.Name;

            lock (_lock)
            {
                // 如果已经在运行,先停止
                if (_runningTasks.ContainsKey(workflowId))
                {
                    StopContinuousRun(workflow);
                }

                var cts = new CancellationTokenSource();
                _cancellationTokens[workflowId] = cts;

                var task = Task.Run(async () =>
                {
                    OnWorkflowExecutionStarted(workflowId);

                    try
                    {
                        while (!cts.Token.IsCancellationRequested)
                        {
                            await ExecuteWorkflowInternal(workflow, cts.Token);
                            OnWorkflowExecutionCompleted(workflowId);

                            // 连续运行间隔,避免CPU占用过高
                            await Task.Delay(10, cts.Token);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        OnWorkflowExecutionStopped(workflowId);
                    }
                    catch (Exception ex)
                    {
                        OnWorkflowExecutionError(workflowId, ex.Message);
                    }
                    finally
                    {
                        lock (_lock)
                        {
                            _cancellationTokens.Remove(workflowId);
                            _runningTasks.Remove(workflowId);
                        }
                    }
                }, cts.Token);

                _runningTasks[workflowId] = task;
            }
        }

        /// <summary>
        /// 停止连续运行指定工作流
        /// </summary>
        public void StopContinuousRun(WorkflowTabViewModel workflow)
        {
            if (workflow == null)
                throw new ArgumentNullException(nameof(workflow));

            var workflowId = workflow.Name;

            lock (_lock)
            {
                if (_cancellationTokens.TryGetValue(workflowId, out var cts))
                {
                    cts.Cancel();
                    _cancellationTokens.Remove(workflowId);
                }

                _runningTasks.Remove(workflowId);
            }
        }

        /// <summary>
        /// 开始运行所有工作流(单次)
        /// </summary>
        public async Task StartAllWorkflowsRun(IEnumerable<WorkflowTabViewModel> workflows)
        {
            if (workflows == null)
                throw new ArgumentNullException(nameof(workflows));

            var tasks = workflows.Select(w => RunSingleAsync(w)).ToList();
            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// 开始连续运行所有工作流
        /// </summary>
        public void StartAllContinuousRun(IEnumerable<WorkflowTabViewModel> workflows)
        {
            if (workflows == null)
                throw new ArgumentNullException(nameof(workflows));

            foreach (var workflow in workflows)
            {
                StartContinuousRun(workflow);
            }
        }

        /// <summary>
        /// 停止所有工作流的连续运行
        /// </summary>
        public void StopAllContinuousRun()
        {
            lock (_lock)
            {
                foreach (var cts in _cancellationTokens.Values)
                {
                    cts.Cancel();
                }
                _cancellationTokens.Clear();
                _runningTasks.Clear();
            }
        }

        /// <summary>
        /// 判断指定工作流是否正在运行
        /// </summary>
        public bool IsRunning(string workflowId)
        {
            lock (_lock)
            {
                return _runningTasks.ContainsKey(workflowId);
            }
        }

        /// <summary>
        /// 判断是否有任何工作流正在运行
        /// </summary>
        public bool IsAnyRunning()
        {
            lock (_lock)
            {
                return _runningTasks.Count > 0;
            }
        }

        /// <summary>
        /// 内部执行工作流逻辑
        /// </summary>
        private async Task ExecuteWorkflowInternal(WorkflowTabViewModel workflow, CancellationToken cancellationToken)
        {
            // 在方法开始就发送一条测试日志
            InvokeOnUI(() => WorkflowExecutionProgress?.Invoke(this, new WorkflowExecutionProgressEventArgs(workflow.Name, "[EXEC] ====== 开始执行工作流 ======")));

            try
            {
                // 获取输入图像
                object? inputImage = null;
                try
                {
                    InvokeOnUI(() => WorkflowExecutionProgress?.Invoke(this, new WorkflowExecutionProgressEventArgs(workflow.Name, "[EXEC] 正在获取输入图像...")));
                    inputImage = await _inputProvider.GetInputImageAsync();
                    InvokeOnUI(() => WorkflowExecutionProgress?.Invoke(this, new WorkflowExecutionProgressEventArgs(workflow.Name, "[EXEC] 输入图像获取完成")));
                    cancellationToken.ThrowIfCancellationRequested();
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    // 如果获取图像失败,输出日志但继续执行
                    InvokeOnUI(() => WorkflowExecutionProgress?.Invoke(this, new WorkflowExecutionProgressEventArgs(workflow.Name, $"[WARN] 获取输入图像失败: {ex.Message}")));
                    Console.WriteLine($"[WorkflowExecutionManager] 获取输入图像失败: {ex.Message}");
                }

                InvokeOnUI(() => WorkflowExecutionProgress?.Invoke(this, new WorkflowExecutionProgressEventArgs(workflow.Name, "[EXEC] 正在创建日志器...")));
                // 创建简单的日志输出
                var logger = new SimpleLogger();

                // 将日志转发到UI线程
                logger.LogMessage += (sender, message) =>
                {
                    InvokeOnUI(() => WorkflowExecutionProgress?.Invoke(this, new WorkflowExecutionProgressEventArgs(workflow.Name, message)));
                };

                InvokeOnUI(() => WorkflowExecutionProgress?.Invoke(this, new WorkflowExecutionProgressEventArgs(workflow.Name, "[EXEC] 正在创建工作流引擎...")));
                var (workflowEngine, executionEngine, context) = Workflow.WorkflowEngineFactory.CreateEngineSuite(logger);

                // 创建工作流
                var suneyeWorkflow = workflowEngine.CreateWorkflow(workflow.Name, workflow.Name);
                InvokeOnUI(() => WorkflowExecutionProgress?.Invoke(this, new WorkflowExecutionProgressEventArgs(workflow.Name, $"[EXEC] 工作流已创建, ID: {suneyeWorkflow.Id}")));

                // 从UI节点创建AlgorithmNode
                InvokeOnUI(() => WorkflowExecutionProgress?.Invoke(this, new WorkflowExecutionProgressEventArgs(workflow.Name, $"[EXEC] 正在添加节点 (总数: {workflow.WorkflowNodes.Count})...")));
                foreach (var uiNode in workflow.WorkflowNodes)
                {
                    var algorithmNode = CreateAlgorithmNodeFromUiNode(uiNode, logger);
                    if (algorithmNode != null)
                    {
                        suneyeWorkflow.AddNode(algorithmNode);
                        logger.LogInfo($"添加节点: {uiNode.Name} (ID: {uiNode.Id})");
                    }
                }

                // 从UI连接创建工作流连接
                InvokeOnUI(() => WorkflowExecutionProgress?.Invoke(this, new WorkflowExecutionProgressEventArgs(workflow.Name, $"[EXEC] 正在添加连接 (总数: {workflow.WorkflowConnections.Count})...")));
                foreach (var uiConnection in workflow.WorkflowConnections)
                {
                    try
                    {
                        suneyeWorkflow.ConnectNodes(uiConnection.SourceNodeId, uiConnection.TargetNodeId);
                        logger.LogInfo($"添加连接: {uiConnection.SourceNodeId} -> {uiConnection.TargetNodeId}");
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning($"添加连接失败: {ex.Message}");
                    }
                }

                // 执行工作流
                logger.LogInfo($"开始执行工作流: {workflow.Name}");
                InvokeOnUI(() => WorkflowExecutionProgress?.Invoke(this, new WorkflowExecutionProgressEventArgs(workflow.Name, "[EXEC] 正在创建测试图像...")));
                var matImage = CreateTestMat();
                InvokeOnUI(() => WorkflowExecutionProgress?.Invoke(this, new WorkflowExecutionProgressEventArgs(workflow.Name, "[EXEC] 测试图像已创建, 开始执行工作流...")));
                var result = await executionEngine.ExecuteWorkflowAsync(suneyeWorkflow.Id, matImage);

                // 输出执行结果
                logger.LogInfo($"工作流执行完成: {workflow.Name}");
                logger.LogInfo($"执行结果: {(result.Success ? "成功" : "失败")}");
                logger.LogInfo($"执行时间: {result.ExecutionTime.TotalMilliseconds:F2} ms");

                if (!result.Success && result.Errors.Any())
                {
                    InvokeOnUI(() => WorkflowExecutionProgress?.Invoke(this, new WorkflowExecutionProgressEventArgs(workflow.Name, $"[ERROR] 工作流执行失败, 错误数量: {result.Errors.Count}")));
                    foreach (var error in result.Errors)
                    {
                        logger.LogError($"错误: {error}");
                    }
                }
                else
                {
                    InvokeOnUI(() => WorkflowExecutionProgress?.Invoke(this, new WorkflowExecutionProgressEventArgs(workflow.Name, "[EXEC] ====== 工作流执行结束 ======")));
                }
            }
            catch (Exception ex)
            {
                InvokeOnUI(() => WorkflowExecutionProgress?.Invoke(this, new WorkflowExecutionProgressEventArgs(workflow.Name, $"[ERROR] ExecuteWorkflowInternal异常: {ex.Message}")));
                Console.WriteLine($"[WorkflowExecutionManager] ExecuteWorkflowInternal异常: {ex.Message}");
                Console.WriteLine($"[WorkflowExecutionManager] 堆栈跟踪: {ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// 创建测试用的Mat对象
        /// </summary>
        private SunEyeVision.Models.Mat CreateTestMat()
        {
            // 创建640x480的3通道测试图像
            return new SunEyeVision.Models.Mat(640, 480, 3);
        }

        /// <summary>
        /// 从UI节点创建AlgorithmNode
        /// </summary>
        private Workflow.AlgorithmNode? CreateAlgorithmNodeFromUiNode(Models.WorkflowNode uiNode, SunEyeVision.Interfaces.ILogger logger)
        {
            try
            {
                // 使用WorkflowNodeFactory创建节点(使用真实插件)
                var toolId = uiNode.AlgorithmType ?? uiNode.Name;
                var node = Workflow.WorkflowNodeFactory.CreateAlgorithmNode(
                    toolId,
                    uiNode.Id,
                    uiNode.Name,
                    enableCaching: true,
                    enableRetry: false
                );

                if (node != null)
                {
                    logger.LogInfo($"通过WorkflowNodeFactory创建节点: {uiNode.Name} (ToolId: {toolId})");
                    return node;
                }
                else
                {
                    // 如果工具不存在,使用测试处理器作为后备
                    logger.LogWarning($"工具不存在: {toolId}, 使用测试处理器");
                    var processor = new TestImageProcessor(toolId);
                    return new Workflow.AlgorithmNode(
                        uiNode.Id,
                        uiNode.Name,
                        processor
                    );
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"创建AlgorithmNode失败: {ex.Message}");
                return null;
            }
        }

        private void OnWorkflowExecutionStarted(string workflowId)
        {
            InvokeOnUI(() => WorkflowExecutionStarted?.Invoke(this, new WorkflowExecutionEventArgs(workflowId)));
        }

        private void OnWorkflowExecutionCompleted(string workflowId)
        {
            InvokeOnUI(() => WorkflowExecutionCompleted?.Invoke(this, new WorkflowExecutionEventArgs(workflowId)));
        }

        private void OnWorkflowExecutionStopped(string workflowId)
        {
            InvokeOnUI(() => WorkflowExecutionStopped?.Invoke(this, new WorkflowExecutionEventArgs(workflowId)));
        }

        private void OnWorkflowExecutionError(string workflowId, string errorMessage)
        {
            InvokeOnUI(() => WorkflowExecutionError?.Invoke(this, new WorkflowExecutionEventArgs(workflowId, errorMessage)));
        }

        private void InvokeOnUI(Action action)
        {
            if (_synchronizationContext != null)
            {
                _synchronizationContext.Post(_ => action(), null);
            }
            else
            {
                action();
            }
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        public void Dispose()
        {
            StopAllContinuousRun();

            lock (_lock)
            {
                foreach (var cts in _cancellationTokens.Values)
                {
                    cts.Dispose();
                }
                _cancellationTokens.Clear();
            }
        }
    }

    /// <summary>
    /// 简单的日志输出类
    /// </summary>
    public class SimpleLogger : SunEyeVision.Interfaces.ILogger
    {
        public event EventHandler<string>? LogMessage;

        public void LogDebug(string message)
        {
            var log = $"[DEBUG] {DateTime.Now:HH:mm:ss.fff} {message}";
            Console.WriteLine(log);
            LogMessage?.Invoke(this, log);
        }

        public void LogInfo(string message)
        {
            var log = $"[INFO] {DateTime.Now:HH:mm:ss.fff} {message}";
            Console.WriteLine(log);
            LogMessage?.Invoke(this, log);
        }

        public void LogWarning(string message)
        {
            var log = $"[WARN] {DateTime.Now:HH:mm:ss.fff} {message}";
            Console.WriteLine(log);
            LogMessage?.Invoke(this, log);
        }

        public void LogError(string message, Exception? exception = null)
        {
            var log = $"[ERROR] {DateTime.Now:HH:mm:ss.fff} {message}";
            Console.WriteLine(log);
            if (exception != null)
            {
                Console.WriteLine($"[ERROR] Exception: {exception.Message}");
                log += $" | {exception.Message}";
            }
            LogMessage?.Invoke(this, log);
        }
    }

    /// <summary>
    /// 测试用图像处理器
    /// </summary>
    public class TestImageProcessor : SunEyeVision.Interfaces.IImageProcessor
    {
        private readonly string _algorithmType;

        public TestImageProcessor(string algorithmType)
        {
            _algorithmType = algorithmType;
        }

        public object? Process(object image)
        {
            Console.WriteLine($"[TestImageProcessor] 处理 {_algorithmType}");
            Console.WriteLine($"[TestImageProcessor] 输入类型: {image?.GetType().Name}");

            // 模拟不同算法的处理时间
            var delay = _algorithmType switch
            {
                "image_capture" => 10,
                "gaussian_blur" => 20,
                "edge_detection" => 30,
                "gray_scale" => 15,
                "threshold" => 10,
                "morphology" => 25,
                _ => 10
            };

            Console.WriteLine($"[TestImageProcessor] 模拟处理延迟 {delay}ms");
            System.Threading.Thread.Sleep(delay);

            Console.WriteLine($"[TestImageProcessor] 处理完成");
            return image;
        }
    }

    /// <summary>
    /// 工作流执行事件参数
    /// </summary>
    public class WorkflowExecutionEventArgs : EventArgs
    {
        public string WorkflowId { get; }
        public string? ErrorMessage { get; }
        public DateTime Timestamp { get; }

        public WorkflowExecutionEventArgs(string workflowId, string? errorMessage = null)
        {
            WorkflowId = workflowId;
            ErrorMessage = errorMessage;
            Timestamp = DateTime.Now;
        }
    }

    /// <summary>
    /// 工作流执行进度事件参数
    /// </summary>
    public class WorkflowExecutionProgressEventArgs : EventArgs
    {
        public string WorkflowId { get; }
        public string Message { get; }
        public DateTime Timestamp { get; }

        public WorkflowExecutionProgressEventArgs(string workflowId, string message)
        {
            WorkflowId = workflowId;
            Message = message;
            Timestamp = DateTime.Now;
        }
    }
}
