using System;
using System.Threading.Tasks;
using SunEyeVision.Models;
using SunEyeVision.PluginSystem;

namespace SunEyeVision.Workflow
{
    /// <summary>
    /// 工作流控制类型（内部使用，用于控制节点分类）
    /// </summary>
    public enum WorkflowControlType
    {
        /// <summary>
        /// 子程序调用
        /// </summary>
        Subroutine,

        /// <summary>
        /// 条件判断
        /// </summary>
        Condition,

        /// <summary>
        /// 多路分支
        /// </summary>
        Switch
    }

    /// <summary>
    /// 工作流控制节点基类
    /// </summary>
    public abstract class WorkflowControlNode : WorkflowNode
    {
        /// <summary>
        /// 控制类型
        /// </summary>
        public WorkflowControlType ControlType { get; set; }

        /// <summary>
        /// 是否启用验证
        /// </summary>
        public bool EnableValidation { get; set; } = true;

        /// <summary>
        /// 最后验证结果
        /// </summary>
        public ValidationResult LastValidationResult { get; private set; }

        public WorkflowControlNode(string id, string name, WorkflowControlType controlType)
            : base(id, name, MapControlTypeToNodeType(controlType))
        {
            ControlType = controlType;
        }

        /// <summary>
        /// 将WorkflowControlType映射到NodeType
        /// </summary>
        private static NodeType MapControlTypeToNodeType(WorkflowControlType controlType)
        {
            return controlType switch
            {
                WorkflowControlType.Subroutine => NodeType.Subroutine,
                WorkflowControlType.Condition => NodeType.Condition,
                WorkflowControlType.Switch => NodeType.Switch,
                _ => throw new ArgumentException($"Unknown control type: {controlType}")
            };
        }

        /// <summary>
        /// 执行控制逻辑（抽象方法）
        /// </summary>
        /// <param name="context">工作流执行上下文</param>
        /// <returns>执行结果</returns>
        public abstract Task<ExecutionResult> ExecuteControl(WorkflowContext context);

        /// <summary>
        /// 验证控制节点配置
        /// </summary>
        /// <returns>验证结果</returns>
        public abstract ValidationResult Validate();

        /// <summary>
        /// 执行验证并更新最后验证结果
        /// </summary>
        public ValidationResult ValidateAndUpdate()
        {
            LastValidationResult = Validate();
            return LastValidationResult;
        }

        /// <summary>
        /// 验证控制节点是否配置正确
        /// </summary>
        /// <returns>是否验证通过</returns>
        public bool IsValid()
        {
            if (!EnableValidation) return true;
            var result = Validate();
            return result.IsValid;
        }
    }
}
