using System;
using System.Collections.Generic;
using System.Linq;

namespace SunEyeVision.Workflow
{
    /// <summary>
    /// 连接状态枚举
    /// </summary>
    public enum ConnectionStatus
    {
        /// <summary>
        /// 空闲
        /// </summary>
        Idle,
        /// <summary>
        /// 正在传输数据
        /// </summary>
        Transmitting,
        /// <summary>
        /// 已完成
        /// </summary>
        Completed,
        /// <summary>
        /// 错误
        /// </summary>
        Error
    }

    /// <summary>
    /// 执行组 - 包含可以并行执行的节点
    /// </summary>
    public class ExecutionGroup
    {
        /// <summary>
        /// 组序号 (从1开始)
        /// </summary>
        public int GroupNumber { get; set; }

        /// <summary>
        /// 该组包含的节点ID列表
        /// </summary>
        public List<string> NodeIds { get; set; }

        /// <summary>
        /// 该组的状态
        /// </summary>
        public ExecutionGroupStatus Status { get; set; }

        /// <summary>
        /// 开始执行时间
        /// </summary>
        public DateTime? StartTime { get; set; }

        /// <summary>
        /// 结束执行时间
        /// </summary>
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// 执行耗时 (毫秒)
        /// </summary>
        public double? ExecutionTimeMs => StartTime.HasValue && EndTime.HasValue 
            ? (EndTime.Value - StartTime.Value).TotalMilliseconds 
            : (double?)null;

        public ExecutionGroup()
        {
            NodeIds = new List<string>();
            Status = ExecutionGroupStatus.Pending;
        }
    }

    /// <summary>
    /// 执行组状态
    /// </summary>
    public enum ExecutionGroupStatus
    {
        /// <summary>
        /// 等待执行
        /// </summary>
        Pending,
        /// <summary>
        /// 正在执行
        /// </summary>
        Running,
        /// <summary>
        /// 已完成
        /// </summary>
        Completed,
        /// <summary>
        /// 执行失败
        /// </summary>
        Failed
    }

    /// <summary>
    /// 执行计划 - 管理工作流的执行顺序和并行分组
    /// </summary>
    public class ExecutionPlan
    {
        /// <summary>
        /// 执行组列表 (按执行顺序排列)
        /// </summary>
        public List<ExecutionGroup> Groups { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 执行状态
        /// </summary>
        public ExecutionStatus Status { get; set; }

        /// <summary>
        /// 开始执行时间
        /// </summary>
        public DateTime? StartTime { get; set; }

        /// <summary>
        /// 结束执行时间
        /// </summary>
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// 总执行耗时 (毫秒)
        /// </summary>
        public double? TotalExecutionTimeMs => StartTime.HasValue && EndTime.HasValue 
            ? (EndTime.Value - StartTime.Value).TotalMilliseconds 
            : (double?)null;

        /// <summary>
        /// 总节点数
        /// </summary>
        public int TotalNodes => Groups.Sum(g => g.NodeIds.Count);

        /// <summary>
        /// 已完成节点数
        /// </summary>
        public int CompletedNodes => Groups.Where(g => g.Status == ExecutionGroupStatus.Completed)
                                           .Sum(g => g.NodeIds.Count);

        /// <summary>
        /// 执行进度 (0-100)
        /// </summary>
        public double Progress => TotalNodes > 0 ? (double)CompletedNodes / TotalNodes * 100 : 0;

        public ExecutionPlan()
        {
            Groups = new List<ExecutionGroup>();
            CreatedAt = DateTime.Now;
            Status = ExecutionStatus.NotStarted;
        }

        /// <summary>
        /// 开始执行
        /// </summary>
        public void Start()
        {
            Status = ExecutionStatus.Running;
            StartTime = DateTime.Now;
        }

        /// <summary>
        /// 完成执行
        /// </summary>
        public void Complete()
        {
            Status = ExecutionStatus.Completed;
            EndTime = DateTime.Now;
        }

        /// <summary>
        /// 标记执行失败
        /// </summary>
        public void Fail()
        {
            Status = ExecutionStatus.Failed;
            EndTime = DateTime.Now;
        }

        /// <summary>
        /// 获取下一个待执行的组
        /// </summary>
        public ExecutionGroup? GetNextPendingGroup()
        {
            return Groups.FirstOrDefault(g => g.Status == ExecutionGroupStatus.Pending);
        }

        /// <summary>
        /// 获取执行报告
        /// </summary>
        public string GetReport()
        {
            var report = $"执行计划报告\n";
            report += $"{new string('=', 40)}\n";
            report += $"状态: {Status}\n";
            report += $"创建时间: {CreatedAt:yyyy-MM-dd HH:mm:ss}\n";
            report += $"总组数: {Groups.Count}\n";
            report += $"总节点数: {TotalNodes}\n";
            report += $"进度: {Progress:F1}% ({CompletedNodes}/{TotalNodes})\n";

            if (StartTime.HasValue)
            {
                report += $"开始时间: {StartTime.Value:yyyy-MM-dd HH:mm:ss}\n";
            }

            if (EndTime.HasValue)
            {
                report += $"结束时间: {EndTime.Value:yyyy-MM-dd HH:mm:ss}\n";
            }

            if (TotalExecutionTimeMs.HasValue)
            {
                report += $"总耗时: {TotalExecutionTimeMs.Value:F2} ms\n";
            }

            report += $"\n分组详情:\n";

            foreach (var group in Groups)
            {
                var statusText = group.Status switch
                {
                    ExecutionGroupStatus.Pending => "等待",
                    ExecutionGroupStatus.Running => "运行中",
                    ExecutionGroupStatus.Completed => "完成",
                    ExecutionGroupStatus.Failed => "失败",
                    _ => "未知"
                };

                report += $"  组 {group.GroupNumber}: [{string.Join(", ", group.NodeIds)}] - {statusText}";
                
                if (group.ExecutionTimeMs.HasValue)
                {
                    report += $" ({group.ExecutionTimeMs.Value:F2} ms)";
                }
                
                report += "\n";
            }

            return report;
        }
    }

    /// <summary>
    /// 执行状态
    /// </summary>
    public enum ExecutionStatus
    {
        /// <summary>
        /// 未开始
        /// </summary>
        NotStarted,
        /// <summary>
        /// 运行中
        /// </summary>
        Running,
        /// <summary>
        /// 已完成
        /// </summary>
        Completed,
        /// <summary>
        /// 失败
        /// </summary>
        Failed
    }
}
