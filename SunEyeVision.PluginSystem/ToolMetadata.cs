using System;
using System.Collections.Generic;
using SunEyeVision.Models;

namespace SunEyeVision.PluginSystem
{
    /// <summary>
    /// 工具元数据 - 用于描述工具的完整信息
    /// </summary>
    public class ToolMetadata
    {
        /// <summary>
        /// 工具ID(唯一标识符)
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// 工具名称(代码标识符)
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 显示名称(UI显示)
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// 工具描述
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 工具图标
        /// </summary>
        public string Icon { get; set; } = "🔧";

        /// <summary>
        /// 工具分类
        /// </summary>
        public string Category { get; set; } = "未分类";

        /// <summary>
        /// 节点类型 - 指定此工具创建的工作流节点类型
        /// </summary>
        public NodeType NodeType { get; set; } = NodeType.Algorithm;

        /// <summary>
        /// 算法类型
        /// </summary>
        public Type? AlgorithmType { get; set; }

        /// <summary>
        /// 输入参数列表
        /// </summary>
        public List<ParameterMetadata> InputParameters { get; set; } = new List<ParameterMetadata>();

        /// <summary>
        /// 输出参数列表
        /// </summary>
        public List<ParameterMetadata> OutputParameters { get; set; } = new List<ParameterMetadata>();

        /// <summary>
        /// 是否有调试界面
        /// </summary>
        public bool HasDebugInterface { get; set; } = true;

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

        // ==================== 执行特性 ====================

        /// <summary>
        /// 是否支持并行执行
        /// </summary>
        public bool SupportParallel { get; set; } = true;

        /// <summary>
        /// 是否为纯函数(相同输入总是产生相同输出,无副作用)
        /// </summary>
        public bool IsPureFunction { get; set; } = true;

        /// <summary>
        /// 是否有副作用(如修改全局状态、IO操作等)
        /// </summary>
        public bool HasSideEffects { get; set; } = false;

        /// <summary>
        /// 估计执行时间(毫秒)
        /// </summary>
        public int EstimatedExecutionTimeMs { get; set; } = 100;

        /// <summary>
        /// 是否支持结果缓存
        /// </summary>
        public bool SupportCaching { get; set; } = true;

        /// <summary>
        /// 缓存有效期(毫秒)
        /// </summary>
        public int CacheTtlMs { get; set; } = 60000;

        /// <summary>
        /// 最大重试次数
        /// </summary>
        public int MaxRetryCount { get; set; } = 3;

        /// <summary>
        /// 重试延迟(毫秒)
        /// </summary>
        public int RetryDelayMs { get; set; } = 1000;

        /// <summary>
        /// 资源需求等级(1-10,数字越大资源需求越高)
        /// </summary>
        public int ResourceDemand { get; set; } = 5;
    }

    /// <summary>
    /// 参数验证结果
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// 是否验证通过
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();

        /// <summary>
        /// 警告信息
        /// </summary>
        public List<string> Warnings { get; set; } = new List<string>();

        /// <summary>
        /// 创建验证成功的结果
        /// </summary>
        public static ValidationResult Success()
        {
            return new ValidationResult { IsValid = true };
        }

        /// <summary>
        /// 创建验证失败的结果
        /// </summary>
        public static ValidationResult Failure(string error)
        {
            return new ValidationResult
            {
                IsValid = false,
                Errors = new List<string> { error }
            };
        }

        /// <summary>
        /// 添加错误信息
        /// </summary>
        public void AddError(string error)
        {
            IsValid = false;
            Errors.Add(error);
        }

        /// <summary>
        /// 添加警告信息
        /// </summary>
        public void AddWarning(string warning)
        {
            Warnings.Add(warning);
        }
    }
}
