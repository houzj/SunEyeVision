using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.SDK.Models;

namespace SunEyeVision.Workflow
{
    /// <summary>
    /// 配方（参数快照）
    /// </summary>
    /// <remarks>
    /// 设计原则（rule-005）：
    /// - 最优和最合理：直接序列化 ToolParameters，不需要转换层
    /// - JSON格式直观：使用 System.Text.Json 多态序列化
    /// - 单一职责：Recipe 只负责存储参数快照
    /// 
    /// 配方是节点参数的快照，用于：
    /// - 同一个工作流对应多套参数配置
    /// - 快速切换参数配置
    /// - 参数配置的版本管理
    /// 
    /// 配方管理采用列表形式，不使用选项卡。
    /// 配方不支持参数预览，只存储参数快照。
    /// </remarks>
    public class Recipe : ObservableObject
    {
        /// <summary>
        /// 配方ID
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 配方名称
        /// </summary>
        public string Name { get; set; } = "新建配方";

        /// <summary>
        /// 配方描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 最后修改时间
        /// </summary>
        public DateTime LastModifiedTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 是否为默认配方
        /// </summary>
        private bool _isDefault = false;
        public bool IsDefault
        {
            get => _isDefault;
            set => SetProperty(ref _isDefault, value, "默认配方");
        }

        /// <summary>
        /// 参数映射：NodeId -> ToolParameters
        /// </summary>
        /// <remarks>
        /// 存储工作流中所有节点的参数快照。
        /// ToolParameters 使用 JsonPolymorphic 支持多态序列化。
        /// </remarks>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, ToolParameters> ParameterMappings { get; set; } = new();

        /// <summary>
        /// 克隆配方
        /// </summary>
        public Recipe Clone()
        {
            var cloned = new Recipe
            {
                Id = Guid.NewGuid().ToString(),
                Name = $"{Name}_副本",
                Description = Description,
                CreatedTime = DateTime.Now,
                LastModifiedTime = DateTime.Now,
                IsDefault = false,
                ParameterMappings = new Dictionary<string, ToolParameters>(
                    ParameterMappings.ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.Clone()
                    )
                )
            };

            return cloned;
        }

        /// <summary>
        /// 保存节点参数
        /// </summary>
        /// <param name="nodeId">节点ID</param>
        /// <param name="parameters">工具参数</param>
        public void SaveParameters(string nodeId, ToolParameters parameters)
        {
            if (string.IsNullOrEmpty(nodeId))
                throw new ArgumentException("节点ID不能为空", nameof(nodeId));

            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            ParameterMappings[nodeId] = parameters.Clone();
            LastModifiedTime = DateTime.Now;
        }

        /// <summary>
        /// 获取节点参数
        /// </summary>
        /// <param name="nodeId">节点ID</param>
        /// <returns>工具参数的克隆，如果不存在则返回null</returns>
        public ToolParameters? GetParameters(string nodeId)
        {
            if (!ParameterMappings.TryGetValue(nodeId, out var parameters))
                return null;

            return parameters.Clone();
        }

        /// <summary>
        /// 移除节点参数
        /// </summary>
        /// <param name="nodeId">节点ID</param>
        /// <returns>是否移除成功</returns>
        public bool RemoveParameters(string nodeId)
        {
            var removed = ParameterMappings.Remove(nodeId);
            if (removed)
            {
                LastModifiedTime = DateTime.Now;
            }
            return removed;
        }

        /// <summary>
        /// 清空所有参数
        /// </summary>
        public void ClearParameters()
        {
            ParameterMappings.Clear();
            LastModifiedTime = DateTime.Now;
        }

        /// <summary>
        /// 验证配方
        /// </summary>
        public (bool IsValid, List<string> Errors) Validate()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(Name))
            {
                errors.Add("配方名称不能为空");
            }

            // 验证参数映射
            foreach (var kvp in ParameterMappings)
            {
                if (string.IsNullOrEmpty(kvp.Key))
                {
                    errors.Add("参数映射中存在空的节点ID");
                }

                if (kvp.Value == null)
                {
                    errors.Add($"节点 '{kvp.Key}' 的参数为null");
                }
            }

            return (errors.Count == 0, errors);
        }
    }
}
