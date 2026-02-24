using System;
using System.Collections.Generic;
using System.Linq;

namespace SunEyeVision.Workflow
{
    /// <summary>
    /// 工作流执行结�?
    /// </summary>
    public class ExecutionResult
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 输出数据
        /// </summary>
        public Dictionary<string, object> Outputs { get; set; }

        /// <summary>
        /// 错误列表
        /// </summary>
        public List<string> Errors { get; set; }

        /// <summary>
        /// 执行时间
        /// </summary>
        public TimeSpan ExecutionTime { get; set; }

        /// <summary>
        /// 是否被停�?
        /// </summary>
        public bool IsStopped { get; set; }

        /// <summary>
        /// 节点执行结果
        /// </summary>
        public Dictionary<string, NodeExecutionResult> NodeResults { get; set; }

        public ExecutionResult()
        {
            Outputs = new Dictionary<string, object>();
            Errors = new List<string>();
            NodeResults = new Dictionary<string, NodeExecutionResult>();
        }

        /// <summary>
        /// 创建成功结果
        /// </summary>
        public static ExecutionResult CreateSuccess()
        {
            return new ExecutionResult { Success = true };
        }

        /// <summary>
        /// 创建失败结果
        /// </summary>
        public static ExecutionResult CreateFailure(string errorMessage)
        {
            return new ExecutionResult
            {
                Success = false,
                Errors = new List<string>
                {
                    errorMessage
                }
            };
        }

        /// <summary>
        /// 添加错误
        /// </summary>
        public void AddError(string message, string nodeId = null)
        {
            Success = false;
            Errors.Add(message);
        }

        /// <summary>
        /// 合并另一个执行结�?
        /// </summary>
        public void Merge(ExecutionResult other)
        {
            if (other == null) return;

            Success = Success && other.Success;
            IsStopped = IsStopped || other.IsStopped;
            ExecutionTime = ExecutionTime.Add(other.ExecutionTime);

            foreach (var output in other.Outputs)
            {
                Outputs[output.Key] = output.Value;
            }

            foreach (var error in other.Errors)
            {
                Errors.Add(error);
            }

            foreach (var nodeResult in other.NodeResults)
            {
                NodeResults[nodeResult.Key] = nodeResult.Value;
            }
        }
    }

    /// <summary>
    /// 执行错误
        /// </summary>
    public class NodeExecutionResult
    {
        /// <summary>
        /// 节点ID
        /// </summary>
        public string NodeId { get; set; }

        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 执行开始时�?
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// 执行结束时间
        /// </summary>
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// 执行时长
        /// </summary>
        public TimeSpan? Duration => EndTime.HasValue ? EndTime.Value - StartTime : null;

        /// <summary>
        /// 输出数据
        /// </summary>
        public Dictionary<string, object> Outputs { get; set; }

        /// <summary>
        /// 错误消息
        /// </summary>
        public List<string> ErrorMessages { get; set; }

        public NodeExecutionResult()
        {
            Outputs = new Dictionary<string, object>();
            ErrorMessages = new List<string>();
        }
    }

    /// <summary>
    /// 执行进度
    /// </summary>
    public class ExecutionProgress
    {
        /// <summary>
        /// 当前迭代次数
        /// </summary>
        public int CurrentIteration { get; set; }

        /// <summary>
        /// 总迭代次�?
        /// </summary>
        public int TotalIterations { get; set; }

        /// <summary>
        /// 进度百分�?
        /// </summary>
        public double Progress => TotalIterations > 0 ? (double)CurrentIteration / TotalIterations * 100 : 0;

        /// <summary>
        /// 进度消息
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 当前执行的节点ID
        /// </summary>
        public string CurrentNodeId { get; set; }
    }
}
