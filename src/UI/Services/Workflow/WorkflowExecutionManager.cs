using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SunEyeVision.UI.Models;
using SunEyeVision.UI.ViewModels;
using SunEyeVision.Workflow;
using SunEyeVision.UI.Infrastructure;
using SunEyeVision.UI.Services.Workflow;

namespace SunEyeVision.UI.Services.Workflow
{
    /// <summary>
    /// 宸ヤ綔娴佹墽琛岀鐞嗗櫒
    /// 璐熻矗绠＄悊宸ヤ綔娴佺殑鍗曟杩愯鍜岃繛缁繍琛?
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
        /// 鍗曟杩愯鎸囧畾宸ヤ綔娴?
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
        /// 寮€濮嬭繛缁繍琛屾寚瀹氬伐浣滄祦
        /// </summary>
        public void StartContinuousRun(WorkflowTabViewModel workflow)
        {
            if (workflow == null)
                throw new ArgumentNullException(nameof(workflow));

            var workflowId = workflow.Name;

            lock (_lock)
            {
                // 濡傛灉宸茬粡鍦ㄨ繍琛?鍏堝仠姝?
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

                            // 杩炵画杩愯闂撮殧,閬垮厤CPU鍗犵敤杩囬珮
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
        /// 鍋滄杩炵画杩愯鎸囧畾宸ヤ綔娴?
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
        /// 寮€濮嬭繍琛屾墍鏈夊伐浣滄祦(鍗曟)
        /// </summary>
        public async Task StartAllWorkflowsRun(IEnumerable<WorkflowTabViewModel> workflows)
        {
            if (workflows == null)
                throw new ArgumentNullException(nameof(workflows));

            var tasks = workflows.Select(w => RunSingleAsync(w)).ToList();
            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// 寮€濮嬭繛缁繍琛屾墍鏈夊伐浣滄祦
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
        /// 鍋滄鎵€鏈夊伐浣滄祦鐨勮繛缁繍琛?
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
        /// 鍒ゆ柇鎸囧畾宸ヤ綔娴佹槸鍚︽鍦ㄨ繍琛?
        /// </summary>
        public bool IsRunning(string workflowId)
        {
            lock (_lock)
            {
                return _runningTasks.ContainsKey(workflowId);
            }
        }

        /// <summary>
        /// 鍒ゆ柇鏄惁鏈変换浣曞伐浣滄祦姝ｅ湪杩愯
        /// </summary>
        public bool IsAnyRunning()
        {
            lock (_lock)
            {
                return _runningTasks.Count > 0;
            }
        }

        /// <summary>
        /// 鍐呴儴鎵ц宸ヤ綔娴侀€昏緫
        /// </summary>
        private async Task ExecuteWorkflowInternal(WorkflowTabViewModel workflow, CancellationToken cancellationToken)
        {
            // 鍦ㄦ柟娉曞紑濮嬪氨鍙戦€佷竴鏉℃祴璇曟棩蹇?
            InvokeOnUI(() => WorkflowExecutionProgress?.Invoke(this, new WorkflowExecutionProgressEventArgs(workflow.Name, "[EXEC] ====== 寮€濮嬫墽琛屽伐浣滄祦 ======")));

            try
            {
                // 鑾峰彇杈撳叆鍥惧儚
                object? inputImage = null;
                try
                {
                    InvokeOnUI(() => WorkflowExecutionProgress?.Invoke(this, new WorkflowExecutionProgressEventArgs(workflow.Name, "[EXEC] 姝ｅ湪鑾峰彇杈撳叆鍥惧儚...")));
                    inputImage = await _inputProvider.GetInputImageAsync();
                    InvokeOnUI(() => WorkflowExecutionProgress?.Invoke(this, new WorkflowExecutionProgressEventArgs(workflow.Name, "[EXEC] 杈撳叆鍥惧儚鑾峰彇瀹屾垚")));
                    cancellationToken.ThrowIfCancellationRequested();
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    // 濡傛灉鑾峰彇鍥惧儚澶辫触,杈撳嚭鏃ュ織浣嗙户缁墽琛?
                    InvokeOnUI(() => WorkflowExecutionProgress?.Invoke(this, new WorkflowExecutionProgressEventArgs(workflow.Name, $"[WARN] 鑾峰彇杈撳叆鍥惧儚澶辫触: {ex.Message}")));
                    Console.WriteLine($"[WorkflowExecutionManager] 鑾峰彇杈撳叆鍥惧儚澶辫触: {ex.Message}");
                }

                InvokeOnUI(() => WorkflowExecutionProgress?.Invoke(this, new WorkflowExecutionProgressEventArgs(workflow.Name, "[EXEC] 姝ｅ湪鍒涘缓鏃ュ織鍣?..")));
                // 鍒涘缓绠€鍗曠殑鏃ュ織杈撳嚭
                var logger = new SimpleLogger();

                // 灏嗘棩蹇楄浆鍙戝埌UI绾跨▼
                logger.LogMessage += (sender, message) =>
                {
                    InvokeOnUI(() => WorkflowExecutionProgress?.Invoke(this, new WorkflowExecutionProgressEventArgs(workflow.Name, message)));
                };

                InvokeOnUI(() => WorkflowExecutionProgress?.Invoke(this, new WorkflowExecutionProgressEventArgs(workflow.Name, "[EXEC] 姝ｅ湪鍒涘缓宸ヤ綔娴佸紩鎿?..")));
                var (workflowEngine, executionEngine, context) = WorkflowEngineFactory.CreateEngineSuite(logger);

                // 鍒涘缓宸ヤ綔娴?
                var suneyeWorkflow = workflowEngine.CreateWorkflow(workflow.Name, workflow.Name);
                InvokeOnUI(() => WorkflowExecutionProgress?.Invoke(this, new WorkflowExecutionProgressEventArgs(workflow.Name, $"[EXEC] 宸ヤ綔娴佸凡鍒涘缓, ID: {suneyeWorkflow.Id}")));

                // 浠嶶I鑺傜偣鍒涘缓AlgorithmNode
                InvokeOnUI(() => WorkflowExecutionProgress?.Invoke(this, new WorkflowExecutionProgressEventArgs(workflow.Name, $"[EXEC] 姝ｅ湪娣诲姞鑺傜偣 (鎬绘暟: {workflow.WorkflowNodes.Count})...")));
                foreach (var uiNode in workflow.WorkflowNodes)
                {
                    var algorithmNode = CreateAlgorithmNodeFromUiNode(uiNode, logger);
                    if (algorithmNode != null)
                    {
                        suneyeWorkflow.AddNode(algorithmNode);
                        logger.LogInfo($"娣诲姞鑺傜偣: {uiNode.Name} (ID: {uiNode.Id})");
                    }
                }

                // 浠嶶I杩炴帴鍒涘缓宸ヤ綔娴佽繛鎺?
                InvokeOnUI(() => WorkflowExecutionProgress?.Invoke(this, new WorkflowExecutionProgressEventArgs(workflow.Name, $"[EXEC] 姝ｅ湪娣诲姞杩炴帴 (鎬绘暟: {workflow.WorkflowConnections.Count})...")));
                foreach (var uiConnection in workflow.WorkflowConnections)
                {
                    try
                    {
                        suneyeWorkflow.ConnectNodes(uiConnection.SourceNodeId, uiConnection.TargetNodeId);
                        logger.LogInfo($"娣诲姞杩炴帴: {uiConnection.SourceNodeId} -> {uiConnection.TargetNodeId}");
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning($"娣诲姞杩炴帴澶辫触: {ex.Message}");
                    }
                }

                // 鎵ц宸ヤ綔娴?
                logger.LogInfo($"寮€濮嬫墽琛屽伐浣滄祦: {workflow.Name}");
                InvokeOnUI(() => WorkflowExecutionProgress?.Invoke(this, new WorkflowExecutionProgressEventArgs(workflow.Name, "[EXEC] 姝ｅ湪鍒涘缓娴嬭瘯鍥惧儚...")));
                var matImage = CreateTestMat();
                InvokeOnUI(() => WorkflowExecutionProgress?.Invoke(this, new WorkflowExecutionProgressEventArgs(workflow.Name, "[EXEC] 娴嬭瘯鍥惧儚宸插垱寤? 寮€濮嬫墽琛屽伐浣滄祦...")));
                var result = await executionEngine.ExecuteWorkflowAsync(suneyeWorkflow.Id, matImage);

                // 杈撳嚭鎵ц缁撴灉
                logger.LogInfo($"宸ヤ綔娴佹墽琛屽畬鎴? {workflow.Name}");
                logger.LogInfo($"鎵ц缁撴灉: {(result.Success ? "鎴愬姛" : "澶辫触")}");
                logger.LogInfo($"鎵ц鏃堕棿: {result.ExecutionTime.TotalMilliseconds:F2} ms");

                if (!result.Success && result.Errors.Any())
                {
                    InvokeOnUI(() => WorkflowExecutionProgress?.Invoke(this, new WorkflowExecutionProgressEventArgs(workflow.Name, $"[ERROR] 宸ヤ綔娴佹墽琛屽け璐? 閿欒鏁伴噺: {result.Errors.Count}")));
                    foreach (var error in result.Errors)
                    {
                        logger.LogError($"閿欒: {error}");
                    }
                }
                else
                {
                    InvokeOnUI(() => WorkflowExecutionProgress?.Invoke(this, new WorkflowExecutionProgressEventArgs(workflow.Name, "[EXEC] ====== 宸ヤ綔娴佹墽琛岀粨鏉?======")));
                }
            }
            catch (Exception ex)
            {
                InvokeOnUI(() => WorkflowExecutionProgress?.Invoke(this, new WorkflowExecutionProgressEventArgs(workflow.Name, $"[ERROR] ExecuteWorkflowInternal寮傚父: {ex.Message}")));
                Console.WriteLine($"[WorkflowExecutionManager] ExecuteWorkflowInternal寮傚父: {ex.Message}");
                Console.WriteLine($"[WorkflowExecutionManager] 鍫嗘爤璺熻釜: {ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// 鍒涘缓娴嬭瘯鐢ㄧ殑Mat瀵硅薄
        /// </summary>
        private SunEyeVision.Core.Models.Mat CreateTestMat()
        {
            // 鍒涘缓640x480鐨?閫氶亾娴嬭瘯鍥惧儚
            return new SunEyeVision.Core.Models.Mat(640, 480, 3);
        }

        /// <summary>
        /// 浠嶶I鑺傜偣鍒涘缓AlgorithmNode
        /// </summary>
        private SunEyeVision.Workflow.AlgorithmNode? CreateAlgorithmNodeFromUiNode(Models.WorkflowNode uiNode, SunEyeVision.Core.Interfaces.ILogger logger)
        {
            try
            {
                // 浣跨敤WorkflowNodeFactory鍒涘缓鑺傜偣(浣跨敤鐪熷疄鎻掍欢)
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
                    logger.LogInfo($"閫氳繃WorkflowNodeFactory鍒涘缓鑺傜偣: {uiNode.Name} (ToolId: {toolId})");
                    return node;
                }
                else
                {
                    // 如果工具不存在，使用测试处理器作为后备
                    logger.LogWarning($"工具不存在: {toolId}, 使用测试处理器");
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
                logger.LogError($"鍒涘缓AlgorithmNode澶辫触: {ex.Message}");
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
        /// 娓呯悊璧勬簮
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
    /// 绠€鍗曠殑鏃ュ織杈撳嚭绫?
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
    /// 娴嬭瘯鐢ㄥ浘鍍忓鐞嗗櫒
    /// </summary>
    public class TestImageProcessor : SunEyeVision.Core.Interfaces.IImageProcessor
    {
        private readonly string _algorithmType;

        public TestImageProcessor(string algorithmType)
        {
            _algorithmType = algorithmType;
        }

        public object? Process(object image)
        {
            Console.WriteLine($"[TestImageProcessor] 澶勭悊 {_algorithmType}");
            Console.WriteLine($"[TestImageProcessor] 杈撳叆绫诲瀷: {image?.GetType().Name}");

            // 妯℃嫙涓嶅悓绠楁硶鐨勫鐞嗘椂闂?
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

            Console.WriteLine($"[TestImageProcessor] 妯℃嫙澶勭悊寤惰繜 {delay}ms");
            System.Threading.Thread.Sleep(delay);

            Console.WriteLine($"[TestImageProcessor] 澶勭悊瀹屾垚");
            return image;
        }
    }

    /// <summary>
    /// 宸ヤ綔娴佹墽琛屼簨浠跺弬鏁?
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
    /// 宸ヤ綔娴佹墽琛岃繘搴︿簨浠跺弬鏁?
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
