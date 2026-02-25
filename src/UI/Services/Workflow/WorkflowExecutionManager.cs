using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OpenCvSharp;
using SunEyeVision.UI.Models;
using SunEyeVision.UI.ViewModels;
using SunEyeVision.Workflow;
using SunEyeVision.UI.Infrastructure;
using SunEyeVision.UI.Services.Workflow;

namespace SunEyeVision.UI.Services.Workflow
{
    /// <summary>
    /// ִ
    /// ĵ˺?
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
        /// ָ?
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
        /// ʼָ
        /// </summary>
        public void StartContinuousRun(WorkflowTabViewModel workflow)
        {
            if (workflow == null)
                throw new ArgumentNullException(nameof(workflow));

            var workflowId = workflow.Name;

            lock (_lock)
            {
                // ѾУֹͣ
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

                            // ˼CPUռù
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
        /// ָͣ?
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
        /// ʼй(单)
        /// </summary>
        public async Task StartAllWorkflowsRun(IEnumerable<WorkflowTabViewModel> workflows)
        {
            if (workflows == null)
                throw new ArgumentNullException(nameof(workflows));

            var tasks = workflows.Select(w => RunSingleAsync(w)).ToList();
            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// ʼй
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
        /// ͣй?
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
        /// жָǷ?
        /// </summary>
        public bool IsRunning(string workflowId)
        {
            lock (_lock)
            {
                return _runningTasks.ContainsKey(workflowId);
            }
        }

        /// <summary>
        /// жϕSκι
        /// </summary>
        public bool IsAnyRunning()
        {
            lock (_lock)
            {
                return _runningTasks.Count > 0;
            }
        }

        /// <summary>
        /// ڲ
        /// </summary>
        private async Task ExecuteWorkflowInternal(WorkflowTabViewModel workflow, CancellationToken cancellationToken)
        {
            // ڷʼͷһ?
            InvokeOnUI(() => WorkflowExecutionProgress?.Invoke(this, new WorkflowExecutionProgressEventArgs(workflow.Name, "[EXEC] ====== 始执行工作流 ======")));

            try
            {
                // ȡ输入图像
                object? inputImage = null;
                try
                {
                    InvokeOnUI(() => WorkflowExecutionProgress?.Invoke(this, new WorkflowExecutionProgressEventArgs(workflow.Name, "[EXEC] 正在ȡ输入图像...")));
                    inputImage = await _inputProvider.GetInputImageAsync();
                    InvokeOnUI(() => WorkflowExecutionProgress?.Invoke(this, new WorkflowExecutionProgressEventArgs(workflow.Name, "[EXEC] 输入图像ȡ完成")));
                    cancellationToken.ThrowIfCancellationRequested();
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    // ͼ־
                    InvokeOnUI(() => WorkflowExecutionProgress?.Invoke(this, new WorkflowExecutionProgressEventArgs(workflow.Name, $"[WARN] ȡ输入图像ʧ: {ex.Message}")));
                    Console.WriteLine($"[WorkflowExecutionManager] ȡ输入图像ʧ: {ex.Message}");
                }

                InvokeOnUI(() => WorkflowExecutionProgress?.Invoke(this, new WorkflowExecutionProgressEventArgs(workflow.Name, "[EXEC] 正在日志?..")));
                // 单的日志输出
                var logger = new SimpleLogger();

                // ־תUI߳
                logger.LogMessage += (sender, message) =>
                {
                    InvokeOnUI(() => WorkflowExecutionProgress?.Invoke(this, new WorkflowExecutionProgressEventArgs(workflow.Name, message)));
                };

                InvokeOnUI(() => WorkflowExecutionProgress?.Invoke(this, new WorkflowExecutionProgressEventArgs(workflow.Name, "[EXEC] 正在工作流引?..")));
                var (workflowEngine, executionEngine, context) = WorkflowEngineFactory.CreateEngineSuite(logger);

                // ?
                var suneyeWorkflow = workflowEngine.CreateWorkflow(workflow.Name, workflow.Name);
                InvokeOnUI(() => WorkflowExecutionProgress?.Invoke(this, new WorkflowExecutionProgressEventArgs(workflow.Name, $"[EXEC] 工作流已, ID: {suneyeWorkflow.Id}")));

                // 从UIڵ㴴AlgorithmNode
                InvokeOnUI(() => WorkflowExecutionProgress?.Invoke(this, new WorkflowExecutionProgressEventArgs(workflow.Name, $"[EXEC] 正在添加ڵ (总数: {workflow.WorkflowNodes.Count})...")));
                foreach (var uiNode in workflow.WorkflowNodes)
                {
                    var algorithmNode = CreateAlgorithmNodeFromUiNode(uiNode, logger);
                    if (algorithmNode != null)
                    {
                        suneyeWorkflow.AddNode(algorithmNode);
                        logger.LogInfo($"添加ڵ: {uiNode.Name} (ID: {uiNode.Id})");
                    }
                }

                // UI?
                InvokeOnUI(() => WorkflowExecutionProgress?.Invoke(this, new WorkflowExecutionProgressEventArgs(workflow.Name, $"[EXEC] 正在添加 (总数: {workflow.WorkflowConnections.Count})...")));
                foreach (var uiConnection in workflow.WorkflowConnections)
                {
                    try
                    {
                        suneyeWorkflow.ConnectNodes(uiConnection.SourceNodeId, uiConnection.TargetNodeId);
                        logger.LogInfo($"添加: {uiConnection.SourceNodeId} -> {uiConnection.TargetNodeId}");
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning($"添加ʧ: {ex.Message}");
                    }
                }

                // ?
                logger.LogInfo($"始执行工作流: {workflow.Name}");
                InvokeOnUI(() => WorkflowExecutionProgress?.Invoke(this, new WorkflowExecutionProgressEventArgs(workflow.Name, "[EXEC] 正在测试图像...")));
                var matImage = CreateTestMat();
                InvokeOnUI(() => WorkflowExecutionProgress?.Invoke(this, new WorkflowExecutionProgressEventArgs(workflow.Name, "[EXEC] 测试图像已创建，始执行工作流...")));
                var result = await executionEngine.ExecuteWorkflowAsync(suneyeWorkflow.Id, matImage);

                // 
                logger.LogInfo($"工作流执行完? {workflow.Name}");
                logger.LogInfo($"ִн: {(result.Success ? "ɹ" : "ʧ")}");
                logger.LogInfo($"ִ时间: {result.ExecutionTime.TotalMilliseconds:F2} ms");

                if (!result.Success && result.Errors.Any())
                {
                    InvokeOnUI(() => WorkflowExecutionProgress?.Invoke(this, new WorkflowExecutionProgressEventArgs(workflow.Name, $"[ERROR] 工作流执行失败，数量: {result.Errors.Count}")));
                    foreach (var error in result.Errors)
                    {
                        logger.LogError($": {error}");
                    }
                }
                else
                {
                    InvokeOnUI(() => WorkflowExecutionProgress?.Invoke(this, new WorkflowExecutionProgressEventArgs(workflow.Name, "[EXEC] ====== 工作流执行结?======")));
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
        /// õMat
        /// </summary>
        private OpenCvSharp.Mat CreateTestMat()
        {
            // 640x4803ͨͼ
            return new OpenCvSharp.Mat(480, 640, OpenCvSharp.MatType.CV_8UC3, new OpenCvSharp.Scalar(0));
        }

        /// <summary>
        /// 从UIڵ㴴AlgorithmNode
        /// </summary>
        private SunEyeVision.Workflow.AlgorithmNode? CreateAlgorithmNodeFromUiNode(Models.WorkflowNode uiNode, SunEyeVision.Core.Interfaces.ILogger logger)
        {
            try
            {
                // 使用WorkflowNodeFactoryڵ(使用真实插件)
                var toolId = uiNode.AlgorithmType ?? uiNode.Name;
                var node = SunEyeVision.Workflow.WorkflowNodeFactory.CreateAlgorithmNode(
                    toolId,
                    uiNode.Id,
                    uiNode.Name,
                    enableCaching: true,
                    enableRetry: false
                );

                if (node != null)
                {
                    logger.LogInfo($"通过WorkflowNodeFactoryڵ: {uiNode.Name} (ToolId: {toolId})");
                    return node;
                }
                else
                {
                    // ߲[ʹòΪ?
                    logger.LogWarning($"߲: {toolId}, ʹòԴ");
                    var processor = new TestImageProcessor(toolId);
                    return new SunEyeVision.Workflow.AlgorithmNode(
                        uiNode.Id,
                        uiNode.Name,
                        processor
                    );
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"AlgorithmNodeʧ: {ex.Message}");
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
    /// 单的日志输出?
    /// </summary>
    public class SimpleLogger : SunEyeVision.Core.Interfaces.ILogger
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
    /// 测试图像处理器
    /// </summary>
    public class TestImageProcessor : SunEyeVision.Plugin.SDK.Core.IImageProcessor
    {
        private readonly string _algorithmType;

        public TestImageProcessor(string algorithmType)
        {
            _algorithmType = algorithmType;
        }

        public string Name => $"Test_{_algorithmType}";

        public string Description => $"{_algorithmType} 测试处理器";

        public OpenCvSharp.Mat Process(OpenCvSharp.Mat input)
        {
            Console.WriteLine($"[TestImageProcessor] 处理 {_algorithmType}");
            Console.WriteLine($"[TestImageProcessor] 输入: {input?.Width}x{input?.Height}");

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

            Console.WriteLine($"[TestImageProcessor] 完成");
            return input;
        }

        public OpenCvSharp.Mat Process(OpenCvSharp.Mat input, OpenCvSharp.Rect roi)
        {
            Console.WriteLine($"[TestImageProcessor] 处理 {_algorithmType} (ROI: {roi})");
            return Process(input);
        }

        public OpenCvSharp.Mat Process(OpenCvSharp.Mat input, OpenCvSharp.Point2f center, float radius)
        {
            Console.WriteLine($"[TestImageProcessor] 处理 {_algorithmType} (圆形ROI: center={center}, radius={radius})");
            return Process(input);
        }
    }

    /// <summary>
    /// ִ¼?
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
    /// ִн¼?
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
