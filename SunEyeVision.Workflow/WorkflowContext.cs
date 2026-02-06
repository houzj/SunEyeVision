using System;
using System.Collections.Generic;
using System.Linq;
using SunEyeVision.PluginSystem;

namespace SunEyeVision.Workflow
{
    /// <summary>
    /// 子程序调用信息
    /// </summary>
    public class SubroutineCallInfo
    {
        /// <summary>
        /// 子程序ID
        /// </summary>
        public string SubroutineId { get; set; }

        /// <summary>
        /// 节点ID
        /// </summary>
        public string NodeId { get; set; }

        /// <summary>
        /// 调用深度
        /// </summary>
        public int CallDepth { get; set; }

        /// <summary>
        /// 调用时间
        /// </summary>
        public DateTime CallTime { get; set; }

        /// <summary>
        /// 输入参数
        /// </summary>
        public Dictionary<string, object> InputParameters { get; set; }

        /// <summary>
        /// 输出结果
        /// </summary>
        public Dictionary<string, object> OutputResults { get; set; }

        /// <summary>
        /// 调用状态
        /// </summary>
        public CallStatus Status { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string Error { get; set; }

        public SubroutineCallInfo()
        {
            InputParameters = new Dictionary<string, object>();
            OutputResults = new Dictionary<string, object>();
        }
    }

    /// <summary>
    /// 调用状态
    /// </summary>
    public enum CallStatus
    {
        /// <summary>
        /// 调用中
        /// </summary>
        Calling,

        /// <summary>
        /// 执行中
        /// </summary>
        Executing,

        /// <summary>
        /// 完成
        /// </summary>
        Completed,

        /// <summary>
        /// 失败
        /// </summary>
        Failed,
        
        /// <summary>
        /// 已停止
        /// </summary>
        Stopped
    }

    /// <summary>
    /// 节点执行状态
    /// </summary>
    public class NodeExecutionStatus
    {
        /// <summary>
        /// 状态
        /// </summary>
        public NodeStatus Status { get; set; }

        /// <summary>
        /// 开始时间
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// 结束时间
        /// </summary>
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// 执行结果
        /// </summary>
        public ExecutionResult Result { get; set; }

        /// <summary>
        /// 错误消息
        /// </summary>
        public List<string> ErrorMessages { get; set; }

        /// <summary>
        /// 重试次数
        /// </summary>
        public int RetryCount { get; set; }

        /// <summary>
        /// 执行时长
        /// </summary>
        public TimeSpan? Duration => EndTime.HasValue ? EndTime.Value - StartTime : null;

        public NodeExecutionStatus()
        {
            ErrorMessages = new List<string>();
        }
    }

    /// <summary>
    /// 节点状态
    /// </summary>
    public enum NodeStatus
    {
        /// <summary>
        /// 等待中
        /// </summary>
        Pending,

        /// <summary>
        /// 执行中
        /// </summary>
        Running,

        /// <summary>
        /// 已完成
        /// </summary>
        Completed,

        /// <summary>
        /// 失败
        /// </summary>
        Failed,

        /// <summary>
        /// 已跳过
        /// </summary>
        Skipped
    }

    /// <summary>
    /// 执行路径项
    /// </summary>
    public class ExecutionPathItem
    {
        /// <summary>
        /// 节点ID
        /// </summary>
        public string NodeId { get; set; }

        /// <summary>
        /// 节点类型
        /// </summary>
        public string NodeType { get; set; }

        /// <summary>
        /// 时间戳
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// 执行时长
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 输出数据摘要
        /// </summary>
        public string OutputSummary { get; set; }
    }

    /// <summary>
    /// 工作流执行上下文
    /// </summary>
    public class WorkflowContext
    {
        /// <summary>
        /// 工作流ID
        /// </summary>
        public string WorkflowId { get; set; }

        /// <summary>
        /// 工作流名称
        /// </summary>
        public string WorkflowName { get; set; }

        /// <summary>
        /// 执行ID（唯一标识一次执行）
        /// </summary>
        public string ExecutionId { get; set; }

        /// <summary>
        /// 执行开始时间
        /// </summary>
        public DateTime ExecutionStartTime { get; set; }

        /// <summary>
        /// 全局变量
        /// </summary>
        public Dictionary<string, object> Variables { get; set; }

        /// <summary>
        /// 子程序调用栈
        /// </summary>
        public Stack<SubroutineCallInfo> CallStack { get; set; }

        /// <summary>
        /// 执行路径
        /// </summary>
        public List<ExecutionPathItem> ExecutionPath { get; set; }

        /// <summary>
        /// 节点执行状态
        /// </summary>
        public Dictionary<string, NodeExecutionStatus> NodeStates { get; set; }

        /// <summary>
        /// 工作流控制插件引用
        /// </summary>
        public IWorkflowControlPlugin WorkflowControlPlugin { get; set; }

        /// <summary>
        /// 取消令牌
        /// </summary>
        public System.Threading.CancellationToken CancellationToken { get; set; }

        /// <summary>
        /// 进度报告器
        /// </summary>
        public IProgress<ExecutionProgress> ProgressReporter { get; set; }

        /// <summary>
        /// 执行日志
        /// </summary>
        public List<ExecutionLog> Logs { get; set; }

        /// <summary>
        /// 执行元数据
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; }

        /// <summary>
        /// 是否启用调试
        /// </summary>
        public bool IsDebugMode { get; set; }

        /// <summary>
        /// 是否启用性能分析
        /// </summary>
        public bool EnableProfiling { get; set; }

        public WorkflowContext()
        {
            ExecutionId = Guid.NewGuid().ToString();
            ExecutionStartTime = DateTime.Now;
            Variables = new Dictionary<string, object>();
            CallStack = new Stack<SubroutineCallInfo>();
            ExecutionPath = new List<ExecutionPathItem>();
            NodeStates = new Dictionary<string, NodeExecutionStatus>();
            Logs = new List<ExecutionLog>();
            Metadata = new Dictionary<string, object>();
            IsDebugMode = false;
            EnableProfiling = false;
        }

        /// <summary>
        /// 设置变量值
        /// </summary>
        public void SetVariable(string key, object value)
        {
            Variables[key] = value;
        }

        /// <summary>
        /// 获取变量值
        /// </summary>
        public object GetVariable(string key)
        {
            return Variables.TryGetValue(key, out var value) ? value : null;
        }

        /// <summary>
        /// 获取变量值（指定类型）
        /// </summary>
        public T GetVariable<T>(string key)
        {
            var value = GetVariable(key);
            if (value is T typedValue)
            {
                return typedValue;
            }
            return default(T);
        }

        /// <summary>
        /// 检查变量是否存在
        /// </summary>
        public bool HasVariable(string key)
        {
            return Variables.ContainsKey(key);
        }

        /// <summary>
        /// 移除变量
        /// </summary>
        public bool RemoveVariable(string key)
        {
            return Variables.Remove(key);
        }

        /// <summary>
        /// 获取当前调用深度
        /// </summary>
        public int GetCurrentCallDepth()
        {
            return CallStack.Count;
        }

        /// <summary>
        /// 推送调用信息到栈
        /// </summary>
        public void PushCallInfo(SubroutineCallInfo callInfo)
        {
            CallStack.Push(callInfo);
        }

        /// <summary>
        /// 从栈弹出调用信息
        /// </summary>
        public SubroutineCallInfo PopCallInfo()
        {
            return CallStack.Count > 0 ? CallStack.Pop() : null;
        }

        /// <summary>
        /// 获取当前调用信息
        /// </summary>
        public SubroutineCallInfo GetCurrentCallInfo()
        {
            return CallStack.Count > 0 ? CallStack.Peek() : null;
        }

        /// <summary>
        /// 更新节点状态
        /// </summary>
        public void UpdateNodeStatus(string nodeId, NodeStatus status, ExecutionResult result = null)
        {
            if (!NodeStates.ContainsKey(nodeId))
            {
                NodeStates[nodeId] = new NodeExecutionStatus
                {
                    StartTime = DateTime.Now,
                    Status = status
                };
            }

            var nodeStatus = NodeStates[nodeId];
            nodeStatus.Status = status;

            if (status == NodeStatus.Completed || status == NodeStatus.Failed)
            {
                nodeStatus.EndTime = DateTime.Now;
                nodeStatus.Result = result;
            }

            if (status == NodeStatus.Failed && result != null)
            {
                nodeStatus.ErrorMessages.AddRange(result.Errors.Select(e => e.Message));
            }
        }

        /// <summary>
        /// 添加执行日志
        /// </summary>
        public void AddLog(string message, LogLevel level = LogLevel.Info)
        {
            Logs.Add(new ExecutionLog
            {
                Timestamp = DateTime.Now,
                Message = message,
                Level = level,
                ExecutionId = ExecutionId
            });
        }

        /// <summary>
        /// 添加执行路径项
        /// </summary>
        public void AddExecutionPathItem(ExecutionPathItem item)
        {
            ExecutionPath.Add(item);
        }

        /// <summary>
        /// 报告进度
        /// </summary>
        public void ReportProgress(ExecutionProgress progress)
        {
            ProgressReporter?.Report(progress);
        }

        /// <summary>
        /// 检查是否已取消
        /// </summary>
        public bool IsCancellationRequested()
        {
            return CancellationToken.IsCancellationRequested;
        }

        /// <summary>
        /// 创建子上下文（用于子程序）
        /// </summary>
        public WorkflowContext CreateSubContext(string subroutineId)
        {
            var subContext = new WorkflowContext
            {
                WorkflowId = subroutineId,
                WorkflowControlPlugin = WorkflowControlPlugin,
                CancellationToken = CancellationToken,
                ProgressReporter = ProgressReporter,
                IsDebugMode = IsDebugMode,
                EnableProfiling = EnableProfiling
            };

            // 复制全局变量
            foreach (var variable in Variables)
            {
                subContext.Variables[variable.Key] = variable.Value;
            }

            // 复制元数据
            foreach (var meta in Metadata)
            {
                subContext.Metadata[meta.Key] = meta.Value;
            }

            return subContext;
        }

        /// <summary>
        /// 获取执行统计信息
        /// </summary>
        public ExecutionStatistics GetStatistics()
        {
            var stats = new ExecutionStatistics
            {
                ExecutionId = ExecutionId,
                StartTime = ExecutionStartTime,
                EndTime = DateTime.Now,
                TotalNodes = NodeStates.Count,
                CompletedNodes = NodeStates.Values.Count(n => n.Status == NodeStatus.Completed),
                FailedNodes = NodeStates.Values.Count(n => n.Status == NodeStatus.Failed),
                SkippedNodes = NodeStates.Values.Count(n => n.Status == NodeStatus.Skipped),
                TotalLogs = Logs.Count
            };

            stats.Duration = stats.EndTime - stats.StartTime;
            stats.CallDepth = CallStack.Count;

            return stats;
        }
    }

    /// <summary>
    /// 执行日志
    /// </summary>
    public class ExecutionLog
    {
        /// <summary>
        /// 时间戳
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// 日志级别
        /// </summary>
        public LogLevel Level { get; set; }

        /// <summary>
        /// 消息
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 执行ID
        /// </summary>
        public string ExecutionId { get; set; }

        /// <summary>
        /// 节点ID
        /// </summary>
        public string NodeId { get; set; }
    }

    /// <summary>
    /// 日志级别
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// 调试
        /// </summary>
        Debug,

        /// <summary>
        /// 信息
        /// </summary>
        Info,

        /// <summary>
        /// 警告
        /// </summary>
        Warning,

        /// <summary>
        /// 错误
        /// </summary>
        Error,

        /// <summary>
        /// 致命错误
        /// </summary>
        Fatal
    }

    /// <summary>
    /// 执行统计信息
    /// </summary>
    public class ExecutionStatistics
    {
        /// <summary>
        /// 执行ID
        /// </summary>
        public string ExecutionId { get; set; }

        /// <summary>
        /// 开始时间
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// 结束时间
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// 执行时长
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// 总节点数
        /// </summary>
        public int TotalNodes { get; set; }

        /// <summary>
        /// 已完成节点数
        /// </summary>
        public int CompletedNodes { get; set; }

        /// <summary>
        /// 失败节点数
        /// </summary>
        public int FailedNodes { get; set; }

        /// <summary>
        /// 跳过节点数
        /// </summary>
        public int SkippedNodes { get; set; }

        /// <summary>
        /// 总日志数
        /// </summary>
        public int TotalLogs { get; set; }

        /// <summary>
        /// 调用深度
        /// </summary>
        public int CallDepth { get; set; }

        /// <summary>
        /// 成功率
        /// </summary>
        public double SuccessRate => TotalNodes > 0 ? (double)CompletedNodes / TotalNodes * 100 : 0;
    }
}
