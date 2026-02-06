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
                await ExecuteWorkflowInternal(workflow, false, cancellationToken);
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
                            await ExecuteWorkflowInternal(workflow, true, cts.Token);
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
        private async Task ExecuteWorkflowInternal(WorkflowTabViewModel workflow, bool isContinuous, CancellationToken cancellationToken)
        {
            // TODO: 集成真实的WorkflowExecutionEngine
            // 目前使用模拟执行

            // 获取输入图像
            object? inputImage = null;
            try
            {
                inputImage = await _inputProvider.GetInputImageAsync();
                cancellationToken.ThrowIfCancellationRequested();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                // 如果获取图像失败,使用默认处理
            }

            // 模拟工作流执行耗时
            await Task.Delay(50, cancellationToken);

            // TODO: 使用WorkflowExecutionEngine执行工作流
            // var engine = WorkflowEngineFactory.Create(workflow);
            // var context = new WorkflowContext();
            // var result = await engine.ExecuteAsync(context, cancellationToken);

            // 模拟执行完成
            await Task.CompletedTask;
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
}
