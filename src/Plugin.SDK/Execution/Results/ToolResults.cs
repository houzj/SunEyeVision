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
    /// ִ״̬ö
    /// </summary>
    public enum ExecutionStatus
    {
        /// <summary>
        /// δִ
        /// </summary>
        NotExecuted = 0,

        /// <summary>
        /// ִ
        /// </summary>
        Running = 1,

        /// <summary>
        /// ִгɹ
        /// </summary>
        Success = 2,

        /// <summary>
        /// ִʧ
        /// </summary>
        Failed = 3,

        /// <summary>
        /// ִгʱ
        /// </summary>
        Timeout = 4,

        /// <summary>
        /// ȡ
        /// </summary>
        Cancelled = 5,

        /// <summary>
        /// ֳɹ
        /// </summary>
        PartialSuccess = 6
    }

    /// <summary>
    /// 
    /// </summary>
    public enum ResultItemType
    {
        /// <summary>
        /// ֵ
        /// </summary>
        Numeric,

        /// <summary>
        /// ı
        /// </summary>
        Text,

        /// <summary>
        /// ֵ
        /// </summary>
        Boolean,

        /// <summary>
        /// ״
        /// </summary>
        Geometry,

        /// <summary>
        /// ͼ
        /// </summary>
        Image,

        /// <summary>
        /// /
        /// </summary>
        Array,

        /// <summary>
        /// Զ
        /// </summary>
        Object
    }

    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// ڱʾݣ֧ͻֵԪݡ
    /// </remarks>
    public sealed class ResultItem
    {
        /// <summary>
        /// 
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// ʾ
        /// </summary>
        public string? DisplayName { get; set; }

        /// <summary>
        /// ֵ
        /// </summary>
        public object? Value { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public ResultItemType Type { get; set; }

        /// <summary>
        /// λ
        /// </summary>
        public string? Unit { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Ƿͨ
        /// </summary>
        public bool? IsPass { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public double? LowerLimit { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public double? UpperLimit { get; set; }

        /// <summary>
        /// ûԶ
        /// </summary>
        public object? Tag { get; set; }

        /// <summary>
        /// ֵ
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
        /// ֵ
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
        /// ı
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
        /// 
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
        /// ν
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
        /// 
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
        /// Բ
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
        /// ȡֵƥ䣩
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
        /// ȡֵַ
        /// </summary>
        public string? GetStringValue()
        {
            return Value?.ToString();
        }
    }

    /// <summary>
    /// ߽
    /// </summary>
    /// <remarks>
    /// й߽Ļ࣬ṩͳһִнṹ
    /// 
    /// 
    /// 1. ͳһ״̬ʹϢ
    /// 2. ֿ֧ӻԪ
    /// 3. ֽ֧б
    /// 4. ִ֧ͳ
    /// 
    /// ʹʾ
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
    ///             ResultItem.Point("Բ", Center),
    ///             ResultItem.Numeric("뾶", Radius, ""),
    ///             ResultItem.NumericWithSpec("÷", Score, 0.5, 1.0)
    ///         };
    ///     }
    /// }
    /// </code>
    /// </remarks>
    public abstract class ToolResults
    {
        /// <summary>
        /// ִ״̬
        /// </summary>
        public ExecutionStatus Status { get; set; } = ExecutionStatus.NotExecuted;

        /// <summary>
        /// Ƿɹ
        /// </summary>
        public bool IsSuccess => Status == ExecutionStatus.Success || Status == ExecutionStatus.PartialSuccess;

        /// <summary>
        /// Ϣ
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// ջ
        /// </summary>
        public string? ErrorStackTrace { get; set; }

        /// <summary>
        /// ִʱ䣨룩
        /// </summary>
        public long ExecutionTimeMs { get; set; }

        /// <summary>
        /// ʱ
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;

        /// <summary>
        /// 
        /// </summary>
        public string? ToolName { get; set; }

        /// <summary>
        /// ID
        /// </summary>
        public string? ToolId { get; set; }

        /// <summary>
        /// Ϣб
        /// </summary>
        public List<string> Warnings { get; set; } = new List<string>();

        /// <summary>
        /// ûԶ
        /// </summary>
        public object? Tag { get; set; }

        /// <summary>
        /// ȡӻԪ
        /// </summary>
        /// <returns>ӻԪؼ</returns>
        public virtual IEnumerable<VisualElement> GetVisualElements()
        {
            return Array.Empty<VisualElement>();
        }

        /// <summary>
        /// ȡб
        /// </summary>
        /// <returns>б</returns>
        public virtual IReadOnlyList<ResultItem> GetResultItems()
        {
            return Array.Empty<ResultItem>();
        }

        /// <summary>
        /// Ӿ
        /// </summary>
        public void AddWarning(string warning)
        {
            Warnings.Add(warning);
        }

        /// <summary>
        /// ôϢ
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
        /// óɹ
        /// </summary>
        public void SetSuccess(long executionTimeMs = 0)
        {
            Status = ExecutionStatus.Success;
            ExecutionTimeMs = executionTimeMs;
        }

        /// <summary>
        /// ʧܽ
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
        /// תΪֵʽݣ
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
    /// չ
    /// </summary>
    public static class ResultItemExtensions
    {
        /// <summary>
        /// ֵ
        /// </summary>
        public static void AddNumeric(this List<ResultItem> items, string name, double value, string? unit = null)
        {
            items.Add(ResultItem.Numeric(name, value, unit));
        }

        /// <summary>
        /// Ӵֵ
        /// </summary>
        public static void AddNumericWithSpec(this List<ResultItem> items, string name, double value,
            double? lowerLimit, double? upperLimit, string? unit = null)
        {
            items.Add(ResultItem.NumericWithSpec(name, value, lowerLimit, upperLimit, unit));
        }

        /// <summary>
        /// ı
        /// </summary>
        public static void AddText(this List<ResultItem> items, string name, string value)
        {
            items.Add(ResultItem.Text(name, value));
        }

        /// <summary>
        /// Ӳ
        /// </summary>
        public static void AddBoolean(this List<ResultItem> items, string name, bool value)
        {
            items.Add(ResultItem.Boolean(name, value));
        }

        /// <summary>
        /// ӵ
        /// </summary>
        public static void AddPoint(this List<ResultItem> items, string name, Point2d point)
        {
            items.Add(ResultItem.Point(name, point));
        }

        /// <summary>
        /// Բ
        /// </summary>
        public static void AddCircle(this List<ResultItem> items, string name, Circle2d circle)
        {
            items.Add(ResultItem.Circle(name, circle));
        }
    }
}
