using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.SDK.Validation;

namespace SunEyeVision.Workflow
{
    /// <summary>
    /// 条件判断节点
    /// </summary>
    public class ConditionNode : WorkflowControlNode
    {
        /// <summary>
        /// 条件表达式
        /// </summary>
        public string ConditionExpression { get; set; }

        /// <summary>
        /// 真分支ID
        /// </summary>
        public string TrueBranchId { get; set; }

        /// <summary>
        /// 假分支ID
        /// </summary>
        public string FalseBranchId { get; set; }

        /// <summary>
        /// 真分支节点名称
        /// </summary>
        public string TrueBranchName { get; set; }

        /// <summary>
        /// 假分支节点名称
        /// </summary>
        public string FalseBranchName { get; set; }

        /// <summary>
        /// 真分支返回值
        /// </summary>
        public object TrueValue { get; set; }

        /// <summary>
        /// 假分支返回值
        /// </summary>
        public object FalseValue { get; set; }

        /// <summary>
        /// 评估结果
        /// </summary>
        public bool EvaluationResult { get; private set; }

        /// <summary>
        /// 是否已评估
        /// </summary>
        public bool IsEvaluated { get; private set; }

        /// <summary>
        /// 评估次数统计
        /// </summary>
        public int EvaluationCount { get; private set; }

        /// <summary>
        /// 真分支执行次数
        /// </summary>
        public int TrueBranchCount { get; private set; }

        /// <summary>
        /// 假分支执行次数
        /// </summary>
        public int FalseBranchCount { get; private set; }

        /// <summary>
        /// 条件类型
        /// </summary>
        public ConditionType ConditionType { get; set; }

        /// <summary>
        /// 比较操作符
        /// </summary>
        public ComparisonOperator Operator { get; set; }

        /// <summary>
        /// 左操作数变量名
        /// </summary>
        public string LeftOperand { get; set; }

        /// <summary>
        /// 右操作数变量名或值
        /// </summary>
        public string RightOperand { get; set; }

        /// <summary>
        /// 右操作数是否为字面量值
        /// </summary>
        public bool RightOperandIsLiteral { get; set; }

        public ConditionNode()
            : base(Guid.NewGuid().ToString(), "Condition", WorkflowControlType.Condition)
        {
            TrueValue = true;
            FalseValue = false;
            ConditionType = ConditionType.Expression;
            Operator = ComparisonOperator.Equal;
            RightOperandIsLiteral = true;
        }

        public ConditionNode(string id, string name)
            : base(id, name, WorkflowControlType.Condition)
        {
            TrueValue = true;
            FalseValue = false;
            ConditionType = ConditionType.Expression;
            Operator = ComparisonOperator.Equal;
            RightOperandIsLiteral = true;
        }

        /// <summary>
        /// 执行条件判断控制逻辑
        /// </summary>
        public override async Task<ExecutionResult> ExecuteControl(WorkflowContext context)
        {
            EvaluationCount++;

            var plugin = context.WorkflowControlPlugin;
            if (plugin == null)
            {
                return ExecutionResult.CreateFailure("工作流控制插件未加载");
            }

            EvaluationResult = plugin.EvaluateCondition(this, context);
            IsEvaluated = true;

            // 更新分支统计
            if (EvaluationResult)
            {
                TrueBranchCount++;
            }
            else
            {
                FalseBranchCount++;
            }

            var result = ExecutionResult.CreateSuccess();
            result.Outputs["result"] = EvaluationResult ? TrueValue : FalseValue;
            result.Outputs["branch"] = EvaluationResult ? TrueBranchId : FalseBranchId;
            result.Outputs["branchName"] = EvaluationResult ? TrueBranchName : FalseBranchName;

            return result;
        }

        /// <summary>
        /// 验证条件节点配置
        /// </summary>
        public override ValidationResult Validate()
        {
            var result = new ValidationResult { IsValid = true };

            if (ConditionType == ConditionType.Expression)
            {
                if (string.IsNullOrEmpty(ConditionExpression))
                {
                    result.AddError("条件表达式不能为空");
                }
            }
            else
            {
                // 简单条件类型验证
                if (string.IsNullOrEmpty(LeftOperand))
                {
                    result.AddError("左操作数不能为空");
                }

                if (string.IsNullOrEmpty(RightOperand))
                {
                    result.AddError("右操作数不能为空");
                }
            }

            if (string.IsNullOrEmpty(TrueBranchId) && string.IsNullOrEmpty(TrueBranchName))
            {
                result.AddError("必须设置真分支节点");
            }

            if (string.IsNullOrEmpty(FalseBranchId) && string.IsNullOrEmpty(FalseBranchName))
            {
                result.AddError("必须设置假分支节点");
            }

            return result;
        }

        /// <summary>
        /// 重置评估状态
        /// </summary>
        public void ResetEvaluationState()
        {
            EvaluationResult = false;
            IsEvaluated = false;
            EvaluationCount = 0;
            TrueBranchCount = 0;
            FalseBranchCount = 0;
        }

        /// <summary>
        /// 设置简单条件
        /// </summary>
        public void SetSimpleCondition(
            string leftOperand,
            ComparisonOperator op,
            string rightOperand,
            bool rightOperandIsLiteral = true)
        {
            ConditionType = ConditionType.Simple;
            LeftOperand = leftOperand;
            Operator = op;
            RightOperand = rightOperand;
            RightOperandIsLiteral = rightOperandIsLiteral;
        }

        /// <summary>
        /// 设置表达式条件
        /// </summary>
        public void SetExpressionCondition(string expression)
        {
            ConditionType = ConditionType.Expression;
            ConditionExpression = expression;
        }

        /// <summary>
        /// 获取统计信息
        /// </summary>
        public ConditionStatistics GetStatistics()
        {
            return new ConditionStatistics
            {
                TotalEvaluations = EvaluationCount,
                TrueBranchCount = TrueBranchCount,
                FalseBranchCount = FalseBranchCount,
                TrueBranchPercentage = EvaluationCount > 0 ? (double)TrueBranchCount / EvaluationCount * 100 : 0,
                FalseBranchPercentage = EvaluationCount > 0 ? (double)FalseBranchCount / EvaluationCount * 100 : 0
            };
        }
    }

    /// <summary>
    /// 条件类型
    /// </summary>
    public enum ConditionType
    {
        /// <summary>
        /// 表达式类型
        /// </summary>
        Expression,

        /// <summary>
        /// 简单比较类型
        /// </summary>
        Simple,

        /// <summary>
        /// 多条件组合类型
        /// </summary>
        Compound
    }

    /// <summary>
    /// 比较操作符
    /// </summary>
    public enum ComparisonOperator
    {
        /// <summary>
        /// 等于
        /// </summary>
        Equal,

        /// <summary>
        /// 不等于
        /// </summary>
        NotEqual,

        /// <summary>
        /// 大于
        /// </summary>
        GreaterThan,

        /// <summary>
        /// 大于等于
        /// </summary>
        GreaterThanOrEqual,

        /// <summary>
        /// 小于
        /// </summary>
        LessThan,

        /// <summary>
        /// 小于等于
        /// </summary>
        LessThanOrEqual,

        /// <summary>
        /// 包含
        /// </summary>
        Contains,

        /// <summary>
        /// 不包含
        /// </summary>
        NotContains,

        /// <summary>
        /// 正则匹配
        /// </summary>
        RegexMatch
    }

    /// <summary>
    /// 条件统计信息
    /// </summary>
    public class ConditionStatistics
    {
        /// <summary>
        /// 总评估次数
        /// </summary>
        public int TotalEvaluations { get; set; }

        /// <summary>
        /// 真分支执行次数
        /// </summary>
        public int TrueBranchCount { get; set; }

        /// <summary>
        /// 假分支执行次数
        /// </summary>
        public int FalseBranchCount { get; set; }

        /// <summary>
        /// 真分支百分比
        /// </summary>
        public double TrueBranchPercentage { get; set; }

        /// <summary>
        /// 假分支百分比
        /// </summary>
        public double FalseBranchPercentage { get; set; }
    }
}
