using System;
using System.Collections.Generic;
using OpenCvSharp;
using SunEyeVision.Plugin.SDK.Models.Geometry;
using SunEyeVision.Plugin.SDK.Models.Visualization;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.SDK.Validation;

namespace SunEyeVision.Plugin.SDK.Execution.Results
{
    /// <summary>
    /// 执行状态枚举
    /// </summary>
    public enum ExecutionStatus
    {
        /// <summary>
        /// 未执行
        /// </summary>
        NotExecuted = 0,

        /// <summary>
        /// 执行中
        /// </summary>
        Running = 1,

        /// <summary>
        /// 执行成功
        /// </summary>
        Success = 2,

        /// <summary>
        /// 执行失败
        /// </summary>
        Failed = 3,

        /// <summary>
        /// 执行超时
        /// </summary>
        Timeout = 4,

        /// <summary>
        /// 已取消
        /// </summary>
        Cancelled = 5,

        /// <summary>
        /// 部分成功
        /// </summary>
        PartialSuccess = 6
    }

    /// <summary>
    /// 结果项类型
    /// </summary>
    public enum ResultItemType
    {
        /// <summary>
        /// 数值
        /// </summary>
        Numeric,

        /// <summary>
        /// 文本
        /// </summary>
        Text,

        /// <summary>
        /// 布尔值
        /// </summary>
        Boolean,

        /// <summary>
        /// 几何形状
        /// </summary>
        Geometry,

        /// <summary>
        /// 图像
        /// </summary>
        Image,

        /// <summary>
        /// 数组/列表
        /// </summary>
        Array,

        /// <summary>
        /// 自定义对象
        /// </summary>
        Object
    }

    /// <summary>
    /// 结果项
    /// </summary>
    /// <remarks>
    /// 用于表示工具执行结果数据，支持多种数据类型和值的元数据。
    /// </remarks>
    public sealed class ResultItem
    {
        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 显示名称
        /// </summary>
        public string? DisplayName { get; set; }

        /// <summary>
        /// 值
        /// </summary>
        public object? Value { get; set; }

        /// <summary>
        /// 类型
        /// </summary>
        public ResultItemType Type { get; set; }

        /// <summary>
        /// 单位
        /// </summary>
        public string? Unit { get; set; }

        /// <summary>
        /// 描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 是否通过
        /// </summary>
        public bool? IsPass { get; set; }

        /// <summary>
        /// 下限
        /// </summary>
        public double? LowerLimit { get; set; }

        /// <summary>
        /// 上限
        /// </summary>
        public double? UpperLimit { get; set; }

        /// <summary>
        /// 用户自定义数据
        /// </summary>
        public object? Tag { get; set; }

        /// <summary>
        /// 创建数值项
        /// </summary>
        public static ResultItem Numeric(string name, double value, string? unit = null)
        {
            return new ResultItem
            {
                Name = name,
                Value = value,
                Type = ResultItemType.Numeric,
                Unit = unit
            };
        }

        /// <summary>
        /// 创建带规格的数值项
        /// </summary>
        public static ResultItem NumericWithSpec(string name, double value, double? lowerLimit, double? upperLimit, string? unit = null)
        {
            bool isPass = true;
            if (lowerLimit.HasValue && value < lowerLimit.Value) isPass = false;
            if (upperLimit.HasValue && value > upperLimit.Value) isPass = false;

            return new ResultItem
            {
                Name = name,
                Value = value,
                Type = ResultItemType.Numeric,
                Unit = unit,
                LowerLimit = lowerLimit,
                UpperLimit = upperLimit,
                IsPass = isPass
            };
        }

        /// <summary>
        /// 创建文本项
        /// </summary>
        public static ResultItem Text(string name, string value)
        {
            return new ResultItem
            {
                Name = name,
                Value = value,
                Type = ResultItemType.Text
            };
        }

        /// <summary>
        /// 创建布尔项
        /// </summary>
        public static ResultItem Boolean(string name, bool value)
        {
            return new ResultItem
            {
                Name = name,
                Value = value,
                Type = ResultItemType.Boolean,
                IsPass = value
            };
        }

        /// <summary>
        /// 创建几何项
        /// </summary>
        public static ResultItem Geometry(string name, object geometry)
        {
            return new ResultItem
            {
                Name = name,
                Value = geometry,
                Type = ResultItemType.Geometry
            };
        }

        /// <summary>
        /// 创建点结果
        /// </summary>
        public static ResultItem Point(string name, Point2d point)
        {
            return new ResultItem
            {
                Name = name,
                Value = point,
                Type = ResultItemType.Geometry
            };
        }

        /// <summary>
        /// 创建圆结果
        /// </summary>
        public static ResultItem Circle(string name, Circle2d circle)
        {
            return new ResultItem
            {
                Name = name,
                Value = circle,
                Type = ResultItemType.Geometry
            };
        }

        /// <summary>
        /// 获取数值（支持类型匹配）
        /// </summary>
        public double? GetNumericValue()
        {
            if (Type == ResultItemType.Numeric && Value is double d)
                return d;
            if (Value is IConvertible convertible)
                return convertible.ToDouble(null);
            return null;
        }

        /// <summary>
        /// 获取字符串值
        /// </summary>
        public string? GetStringValue()
        {
            return Value?.ToString();
        }
    }

    /// <summary>
    /// 工具执行结果基类
    /// </summary>
    /// <remarks>
    /// 所有工具执行结果的基类，提供统一的执行结果结构
    /// 
    /// 主要功能：
    /// 1. 统一的执行状态和错误信息
    /// 2. 支持可视化元素
    /// 3. 支持结果列表
    /// 4. 支持执行统计
    /// 
    /// 使用示例：
    /// <code>
    /// public class CircleFindResults : ToolResultsBase
    /// {
    ///     public Circle FoundCircle { get; set; }
    ///     public double Score { get; set; }
    ///     public PointD Center => FoundCircle.Center;
    ///     public double Radius => FoundCircle.Radius;
    ///     
    ///     public override IEnumerable&lt;VisualElement&gt; GetVisualElements()
    ///     {
    ///         yield return VisualElement.Circle(FoundCircle, 0xFF00FF00, 2.0);
    ///         yield return VisualElement.Point(Center, 0xFFFF0000, 5.0);
    ///     }
    ///     
    ///     public override IReadOnlyList&lt;ResultItem&gt; GetResultItems()
    ///     {
    ///         return new List&lt;ResultItem&gt;
    ///         {
    ///             ResultItem.Point("圆心", Center),
    ///             ResultItem.Numeric("半径", Radius, "像素"),
    ///             ResultItem.NumericWithSpec("得分", Score, 0.5, 1.0)
    ///         };
    ///     }
    /// }
    /// </code>
    /// </remarks>
    public abstract class ToolResults
    {
        /// <summary>
        /// 执行状态
        /// </summary>
        public ExecutionStatus Status { get; set; } = ExecutionStatus.NotExecuted;

        /// <summary>
        /// 是否成功
        /// </summary>
        public bool IsSuccess => Status == ExecutionStatus.Success || Status == ExecutionStatus.PartialSuccess;

        /// <summary>
        /// 错误信息
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// 错误堆栈
        /// </summary>
        public string? ErrorStackTrace { get; set; }

        /// <summary>
        /// 执行时间（毫秒）
        /// </summary>
        public long ExecutionTimeMs { get; set; }

        /// <summary>
        /// 时间戳
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;

        /// <summary>
        /// 工具名称
        /// </summary>
        public string? ToolName { get; set; }

        /// <summary>
        /// 工具ID
        /// </summary>
        public string? ToolId { get; set; }

        /// <summary>
        /// 警告信息列表
        /// </summary>
        public List<string> Warnings { get; set; } = new List<string>();

        /// <summary>
        /// 用户自定义数据
        /// </summary>
        public object? Tag { get; set; }

        /// <summary>
        /// 获取可视化元素
        /// </summary>
        /// <returns>可视化元素集合</returns>
        public virtual IEnumerable<VisualElement> GetVisualElements()
        {
            return Array.Empty<VisualElement>();
        }

        /// <summary>
        /// 获取结果列表
        /// </summary>
        /// <returns>结果列表</returns>
        public virtual IReadOnlyList<ResultItem> GetResultItems()
        {
            return Array.Empty<ResultItem>();
        }

        /// <summary>
        /// 添加警告
        /// </summary>
        public void AddWarning(string warning)
        {
            Warnings.Add(warning);
        }

        /// <summary>
        /// 设置错误信息
        /// </summary>
        public void SetError(string message, Exception? exception = null)
        {
            Status = ExecutionStatus.Failed;
            ErrorMessage = message;
            if (exception != null)
            {
                ErrorStackTrace = exception.StackTrace;
            }
        }

        /// <summary>
        /// 设置成功
        /// </summary>
        public void SetSuccess(long executionTimeMs = 0)
        {
            Status = ExecutionStatus.Success;
            ExecutionTimeMs = executionTimeMs;
        }

        /// <summary>
        /// 创建失败结果
        /// </summary>
        public static T CreateError<T>(string errorMessage, Exception? exception = null) where T : ToolResults, new()
        {
            var result = new T
            {
                Status = ExecutionStatus.Failed,
                ErrorMessage = errorMessage,
                Timestamp = DateTime.Now
            };
            if (exception != null)
            {
                result.ErrorStackTrace = exception.StackTrace;
            }
            return result;
        }

        /// <summary>
        /// 转换为字典形式数据
        /// </summary>
        public virtual Dictionary<string, object> ToDictionary()
        {
            var dict = new Dictionary<string, object>
            {
                ["Status"] = Status.ToString(),
                ["IsSuccess"] = IsSuccess,
                ["ExecutionTimeMs"] = ExecutionTimeMs,
                ["Timestamp"] = Timestamp
            };

            if (!string.IsNullOrEmpty(ErrorMessage))
                dict["ErrorMessage"] = ErrorMessage;

            if (!string.IsNullOrEmpty(ToolName))
                dict["ToolName"] = ToolName;

            foreach (var item in GetResultItems())
            {
                if (item.Value != null)
                {
                    dict[item.Name] = item.Value;
                }
            }

            return dict;
        }
    }

    /// <summary>
    /// 结果项集合扩展
    /// </summary>
    public static class ResultItemExtensions
    {
        /// <summary>
        /// 添加数值项
        /// </summary>
        public static void AddNumeric(this List<ResultItem> items, string name, double value, string? unit = null)
        {
            items.Add(ResultItem.Numeric(name, value, unit));
        }

        /// <summary>
        /// 添加带规格的数值项
        /// </summary>
        public static void AddNumericWithSpec(this List<ResultItem> items, string name, double value,
            double? lowerLimit, double? upperLimit, string? unit = null)
        {
            items.Add(ResultItem.NumericWithSpec(name, value, lowerLimit, upperLimit, unit));
        }

        /// <summary>
        /// 添加文本项
        /// </summary>
        public static void AddText(this List<ResultItem> items, string name, string value)
        {
            items.Add(ResultItem.Text(name, value));
        }

        /// <summary>
        /// 添加布尔项
        /// </summary>
        public static void AddBoolean(this List<ResultItem> items, string name, bool value)
        {
            items.Add(ResultItem.Boolean(name, value));
        }

        /// <summary>
        /// 添加点项
        /// </summary>
        public static void AddPoint(this List<ResultItem> items, string name, Point2d point)
        {
            items.Add(ResultItem.Point(name, point));
        }

        /// <summary>
        /// 添加圆项
        /// </summary>
        public static void AddCircle(this List<ResultItem> items, string name, Circle2d circle)
        {
            items.Add(ResultItem.Circle(name, circle));
        }
    }
}
