using System;
using System.Collections.Generic;
using System.Linq;
using SunEyeVision.Plugin.SDK.Validation;

namespace SunEyeVision.Plugin.SDK.Metadata
{
    /// <summary>
    /// 副作用类型枚举
    /// </summary>
    public enum SideEffectType
    {
        /// <summary>
        /// 纯函数 - 无副作用，相同输入总是产生相同输出
        /// </summary>
        Pure,

        /// <summary>
        /// 只读副作用 - 如读取文件、网络请求等
        /// </summary>
        ReadOnly,

        /// <summary>
        /// 写入副作用 - 如修改文件、数据库写入等
        /// </summary>
        Write,

        /// <summary>
        /// 混合副作用 - 同时包含读写操作
        /// </summary>
        Mixed
    }

    /// <summary>
    /// 工具元数据 - 描述工具的完整信息
    /// </summary>
    public class ToolMetadata
    {
        #region 基本信息

        /// <summary>
        /// 工具ID (唯一标识符)
        /// </summary>
        public required string Id { get; init; }

        /// <summary>
        /// 工具名称 (代码标识符)
        /// </summary>
        public required string Name { get; init; }

        /// <summary>
        /// 显示名称 (UI显示)
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// 工具描述
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 工具图标
        /// </summary>
        public string Icon { get; set; } = "?";

        /// <summary>
        /// 工具分类
        /// </summary>
        public string Category { get; set; } = "未分类";

        /// <summary>
        /// 工具版本
        /// </summary>
        public string Version { get; set; } = "1.0.0";

        /// <summary>
        /// 工具作者
        /// </summary>
        public string Author { get; set; } = "SunEyeVision";

        /// <summary>
        /// 是否已启用
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        #endregion

        #region 算法信息

        /// <summary>
        /// 算法类型
        /// </summary>
        public Type? AlgorithmType { get; set; }

        /// <summary>
        /// 是否有调试界面
        /// </summary>
        public bool HasDebugInterface { get; set; } = true;

        #endregion

        #region 参数定义

        /// <summary>
        /// 输入参数列表
        /// </summary>
        public List<ParameterMetadata> InputParameters { get; set; } = [];

        /// <summary>
        /// 输出参数列表
        /// </summary>
        public List<ParameterMetadata> OutputParameters { get; set; } = [];

        #endregion

        #region 执行特性

        /// <summary>
        /// 是否支持并行执行
        /// </summary>
        public bool SupportParallel { get; set; } = true;

        /// <summary>
        /// 副作用类型 (替代原 IsPureFunction 和 HasSideEffects)
        /// </summary>
        public SideEffectType SideEffect { get; set; } = SideEffectType.Pure;

        /// <summary>
        /// 是否为纯函数 (便捷属性，基于 SideEffect)
        /// </summary>
        public bool IsPureFunction => SideEffect == SideEffectType.Pure;

        /// <summary>
        /// 是否有副作用 (便捷属性，基于 SideEffect)
        /// </summary>
        public bool HasSideEffects => SideEffect != SideEffectType.Pure;

        /// <summary>
        /// 估计执行时间 (毫秒)
        /// </summary>
        public int EstimatedExecutionTimeMs { get; set; } = 100;

        /// <summary>
        /// 是否支持结果缓存
        /// </summary>
        public bool SupportCaching { get; set; } = true;

        /// <summary>
        /// 缓存有效期 (毫秒)
        /// </summary>
        public int CacheTtlMs { get; set; } = 60000;

        /// <summary>
        /// 最大重试次数
        /// </summary>
        public int MaxRetryCount { get; set; } = 3;

        /// <summary>
        /// 重试延迟 (毫秒)
        /// </summary>
        public int RetryDelayMs { get; set; } = 1000;

        /// <summary>
        /// 资源需求等级 (1-10, 数字越大资源需求越高)
        /// </summary>
        public int ResourceDemand { get; set; } = 5;

        #endregion

        #region 数据绑定支持

        /// <summary>
        /// 是否支持数据绑定
        /// </summary>
        public bool SupportsDataBinding { get; set; } = false;

        /// <summary>
        /// 强类型参数类型
        /// </summary>
        public Type? ParameterType { get; set; }

        /// <summary>
        /// 强类型结果类型
        /// </summary>
        public Type? ResultType { get; set; }

        #endregion

        #region 便捷方法

        /// <summary>
        /// 获取输入参数数量
        /// </summary>
        public int InputCount => InputParameters.Count;

        /// <summary>
        /// 获取输出参数数量
        /// </summary>
        public int OutputCount => OutputParameters.Count;

        /// <summary>
        /// 获取必填输入参数
        /// </summary>
        public IEnumerable<ParameterMetadata> RequiredInputs => InputParameters.Where(p => p.Required);

        /// <summary>
        /// 获取可选输入参数
        /// </summary>
        public IEnumerable<ParameterMetadata> OptionalInputs => InputParameters.Where(p => !p.Required);

        #endregion

        #region 验证与克隆

        /// <summary>
        /// 验证元数据完整性
        /// </summary>
        public ValidationResult Validate()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(Id))
                errors.Add("工具ID不能为空");

            if (string.IsNullOrWhiteSpace(Name))
                errors.Add("工具名称不能为空");

            if (ResourceDemand is < 1 or > 10)
                errors.Add("资源需求等级必须在1-10之间");

            if (EstimatedExecutionTimeMs < 0)
                errors.Add("估计执行时间不能为负数");

            if (CacheTtlMs < 0)
                errors.Add("缓存有效期不能为负数");

            if (MaxRetryCount < 0)
                errors.Add("最大重试次数不能为负数");

            // 验证参数元数据
            foreach (var param in InputParameters.Concat(OutputParameters))
            {
                if (string.IsNullOrWhiteSpace(param.Name))
                    errors.Add("参数名称不能为空");
            }

            return errors.Count == 0
                ? ValidationResult.Success()
                : ValidationResult.Failure(string.Join("; ", errors));
        }

        /// <summary>
        /// 创建浅拷贝
        /// </summary>
        public ToolMetadata Clone()
        {
            return new ToolMetadata
            {
                Id = Id,
                Name = Name,
                DisplayName = DisplayName,
                Description = Description,
                Icon = Icon,
                Category = Category,
                Version = Version,
                Author = Author,
                IsEnabled = IsEnabled,
                AlgorithmType = AlgorithmType,
                HasDebugInterface = HasDebugInterface,
                InputParameters = [.. InputParameters],
                OutputParameters = [.. OutputParameters],
                SupportParallel = SupportParallel,
                SideEffect = SideEffect,
                EstimatedExecutionTimeMs = EstimatedExecutionTimeMs,
                SupportCaching = SupportCaching,
                CacheTtlMs = CacheTtlMs,
                MaxRetryCount = MaxRetryCount,
                RetryDelayMs = RetryDelayMs,
                ResourceDemand = ResourceDemand
            };
        }

        /// <summary>
        /// 生成工具标识字符串
        /// </summary>
        public override string ToString() => $"{Name} v{Version} [{Category}]";

        #endregion
    }
}
