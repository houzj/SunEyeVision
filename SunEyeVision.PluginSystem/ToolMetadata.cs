using System;
using System.Collections.Generic;

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
