using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SunEyeVision.Plugin.Abstractions;

namespace SunEyeVision.Workflow
{
    /// <summary>
    /// 循环类型
    /// </summary>
    public enum LoopType
    {
        /// <summary>
        /// 固定次数循环
        /// </summary>
        FixedCount,

        /// <summary>
        /// 条件循环（While）
        /// </summary>
        ConditionBased,

        /// <summary>
        /// 数据驱动循环（For Each）
        /// </summary>
        DataDriven
    }

    /// <summary>
    /// 参数映射
    /// </summary>
    public class ParameterMapping
    {
        /// <summary>
        /// 外部端口ID
        /// </summary>
        public string ExternalPortId { get; set; }

        /// <summary>
        /// 子程序内部端口ID
        /// </summary>
        public string InternalPortId { get; set; }

        /// <summary>
        /// 映射名称
        /// </summary>
        public string MappingName { get; set; }

        /// <summary>
        /// 数据类型
        /// </summary>
        public Type DataType { get; set; }

        /// <summary>
        /// 默认值
        /// </summary>
        public object DefaultValue { get; set; }
    }

    /// <summary>
    /// 子程序节点
    /// </summary>
    public class SubroutineNode : WorkflowControlNode
    {
        /// <summary>
        /// 子程序唯一标识
        /// </summary>
        public string SubroutineId { get; set; }

        /// <summary>
        /// 子程序名称
        /// </summary>
        public string SubroutineName { get; set; }

        /// <summary>
        /// 子程序描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 输入参数映射
        /// </summary>
        public List<ParameterMapping> InputMappings { get; set; }

        /// <summary>
        /// 输出参数映射
        /// </summary>
        public List<ParameterMapping> OutputMappings { get; set; }

        /// <summary>
        /// 是否启用循环
        /// </summary>
        public bool IsLoop { get; set; }

        /// <summary>
        /// 最大迭代次数
        /// </summary>
        public int MaxIterations { get; set; }

        /// <summary>
        /// 循环条件表达式
        /// </summary>
        public string LoopCondition { get; set; }

        /// <summary>
        /// 循环类型
        /// </summary>
        public LoopType LoopType { get; set; }

        /// <summary>
        /// 当前迭代次数
        /// </summary>
        public int CurrentIteration { get; set; }

        /// <summary>
        /// 总执行时间
        /// </summary>
        public TimeSpan TotalExecutionTime { get; set; }

        /// <summary>
        /// 是否启用并行执行
        /// </summary>
        public bool EnableParallel { get; set; }

        /// <summary>
        /// 子程序执行计数
        /// </summary>
        public int ExecutionCount { get; set; }

        public SubroutineNode()
            : base(Guid.NewGuid().ToString(), "Subroutine", WorkflowControlType.Subroutine)
        {
            InputMappings = new List<ParameterMapping>();
            OutputMappings = new List<ParameterMapping>();
            IsLoop = false;
            MaxIterations = 1;
            LoopType = LoopType.FixedCount;
        }

        public SubroutineNode(string id, string name)
            : base(id, name, WorkflowControlType.Subroutine)
        {
            InputMappings = new List<ParameterMapping>();
            OutputMappings = new List<ParameterMapping>();
            IsLoop = false;
            MaxIterations = 1;
            LoopType = LoopType.FixedCount;
        }

        /// <summary>
        /// 执行子程序控制逻辑
        /// </summary>
        public override async Task<ExecutionResult> ExecuteControl(WorkflowContext context)
        {
            var plugin = context.WorkflowControlPlugin;
            if (plugin == null)
            {
                return ExecutionResult.CreateFailure("工作流控制插件未加载");
            }

            ExecutionCount++;
            return await plugin.ExecuteSubroutine(this, context);
        }

        /// <summary>
        /// 验证子程序节点配置
        /// </summary>
        public override ValidationResult Validate()
        {
            var result = new ValidationResult { IsValid = true };

            if (string.IsNullOrEmpty(SubroutineId))
            {
                result.AddError("子程序ID不能为空");
            }

            if (string.IsNullOrEmpty(SubroutineName))
            {
                result.AddError("子程序名称不能为空");
            }

            if (IsLoop)
            {
                if (MaxIterations <= 0)
                {
                    result.AddError("循环次数必须大于0");
                }

                if (MaxIterations > 10000)
                {
                    result.AddWarning("循环次数过大，可能导致性能问题");
                }

                if (LoopType == LoopType.ConditionBased && string.IsNullOrEmpty(LoopCondition))
                {
                    result.AddError("条件循环必须设置循环条件表达式");
                }
            }

            // 验证参数映射
            if (InputMappings == null || InputMappings.Count == 0)
            {
                result.AddWarning("子程序没有配置输入参数映射");
            }

            if (OutputMappings == null || OutputMappings.Count == 0)
            {
                result.AddWarning("子程序没有配置输出参数映射");
            }

            // 检查是否有重复的映射
            var inputPortIds = new HashSet<string>();
            var outputPortIds = new HashSet<string>();

            foreach (var mapping in InputMappings ?? new List<ParameterMapping>())
            {
                if (!inputPortIds.Add(mapping.ExternalPortId))
                {
                    result.AddError($"输入端口ID重复: {mapping.ExternalPortId}");
                }
            }

            foreach (var mapping in OutputMappings ?? new List<ParameterMapping>())
            {
                if (!outputPortIds.Add(mapping.ExternalPortId))
                {
                    result.AddError($"输出端口ID重复: {mapping.ExternalPortId}");
                }
            }

            return result;
        }

        /// <summary>
        /// 重置执行状态
        /// </summary>
        public void ResetExecutionState()
        {
            CurrentIteration = 0;
            TotalExecutionTime = TimeSpan.Zero;
        }

        /// <summary>
        /// 添加输入参数映射
        /// </summary>
        public void AddInputMapping(string externalPortId, string internalPortId, string mappingName, Type dataType)
        {
            if (InputMappings == null)
            {
                InputMappings = new List<ParameterMapping>();
            }

            InputMappings.Add(new ParameterMapping
            {
                ExternalPortId = externalPortId,
                InternalPortId = internalPortId,
                MappingName = mappingName,
                DataType = dataType
            });
        }

        /// <summary>
        /// 添加输出参数映射
        /// </summary>
        public void AddOutputMapping(string externalPortId, string internalPortId, string mappingName, Type dataType)
        {
            if (OutputMappings == null)
            {
                OutputMappings = new List<ParameterMapping>();
            }

            OutputMappings.Add(new ParameterMapping
            {
                ExternalPortId = externalPortId,
                InternalPortId = internalPortId,
                MappingName = mappingName,
                DataType = dataType
            });
        }
    }
}
