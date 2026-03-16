using System;
using System.Collections.Generic;
using SunEyeVision.Plugin.SDK.Logging;
using SunEyeVision.Core.Models;
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.SDK.Execution.Parameters;

namespace SunEyeVision.Workflow
{
    /// <summary>
    /// 工作流节点
    /// </summary>
    public class WorkflowNode
    {
        /// <summary>
        /// 节点ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// 节点名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 节点类型
        /// </summary>
        public NodeType Type { get; set; }

        /// <summary>
        /// 算法类型名称
        /// </summary>
        public string AlgorithmType { get; set; }

        /// <summary>
        /// 参数类型程序集限定名（用于反序列化）
        /// </summary>
        public string? ParametersTypeName { get; set; }

        /// <summary>
        /// 节点参数
        /// </summary>
        public ToolParameters Parameters { get; set; }

        /// <summary>
        /// 参数绑定配置
        /// </summary>
        /// <remarks>
        /// 支持参数与父节点输出的动态绑定。
        /// 用于在执行时自动从父节点获取参数值。
        /// </remarks>
        public ParameterBindingContainer ParameterBindings { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 执行前事件
        /// </summary>
        public event Action<WorkflowNode> BeforeExecute;

        /// <summary>
        /// 执行后事件
        /// </summary>
        public event Action<WorkflowNode, AlgorithmResult> AfterExecute;

        public WorkflowNode(string id, string name, NodeType type)
        {
            Id = id;
            Name = name;
            Type = type;
            AlgorithmType = string.Empty;
            Parameters = new GenericToolParameters();
            ParameterBindings = new ParameterBindingContainer();
        }

        /// <summary>
        /// 触发执行前事件
        /// </summary>
        protected virtual void OnBeforeExecute()
        {
            BeforeExecute?.Invoke(this);
        }

        /// <summary>
        /// 触发执行后事件
        /// </summary>
        protected virtual void OnAfterExecute(AlgorithmResult result)
        {
            AfterExecute?.Invoke(this, result);
        }

        /// <summary>
        /// 创建工具实例（的初始化处理方法）
        /// </summary>
        public virtual IToolPlugin? CreateInstance()
        {
            throw new NotImplementedException($"Algorithm type '{AlgorithmType}' is not implemented.");
        }

        /// <summary>
        /// 获取节点的可序列化数据
        /// </summary>
        public Dictionary<string, object> ToDictionary()
        {
            var dict = new Dictionary<string, object>
            {
                ["Id"] = Id,
                ["Name"] = Name,
                ["Type"] = (int)Type,
                ["AlgorithmType"] = AlgorithmType,
                ["IsEnabled"] = IsEnabled,
                ["Parameters"] = Parameters.ToSerializableDictionary()
            };

            // 保存参数类型名称用于反序列化
            if (Parameters.GetType() != typeof(GenericToolParameters))
            {
                dict["ParametersTypeName"] = Parameters.GetType().AssemblyQualifiedName ?? string.Empty;
            }

            if (ParameterBindings != null && ParameterBindings.Count > 0)
            {
                dict["ParameterBindings"] = ParameterBindings.ToDictionary();
            }

            return dict;
        }

        /// <summary>
        /// 从字典创建节点
        /// </summary>
        public static WorkflowNode FromDictionary(Dictionary<string, object> dict, NodeType type)
        {
            var id = dict.TryGetValue("Id", out var idVal) ? idVal?.ToString() ?? Guid.NewGuid().ToString() : Guid.NewGuid().ToString();
            var name = dict.TryGetValue("Name", out var nameVal) ? nameVal?.ToString() ?? "Node" : "Node";
            var algorithmType = dict.TryGetValue("AlgorithmType", out var algoVal) ? algoVal?.ToString() ?? string.Empty : string.Empty;
            var isEnabled = dict.TryGetValue("IsEnabled", out var enabledVal) && Convert.ToBoolean(enabledVal);
            var parametersTypeName = dict.TryGetValue("ParametersTypeName", out var ptnVal) ? ptnVal?.ToString() : null;

            var node = new WorkflowNode(id, name, type)
            {
                AlgorithmType = algorithmType,
                IsEnabled = isEnabled
            };

            // 恢复参数
            if (dict.TryGetValue("Parameters", out var paramsVal) && paramsVal is Dictionary<string, object?> paramsDict)
            {
                // 尝试使用类型名称创建强类型参数
                if (!string.IsNullOrEmpty(parametersTypeName))
                {
                    var paramsType = System.Type.GetType(parametersTypeName);
                    if (paramsType != null && typeof(ToolParameters).IsAssignableFrom(paramsType))
                    {
                        var instance = Activator.CreateInstance(paramsType) as ToolParameters;
                        instance?.LoadFromDictionary(paramsDict);
                        if (instance != null)
                            node.Parameters = instance;
                    }
                }

                // 回退到字典创建
                if (node.Parameters is GenericToolParameters)
                {
                    var restored = ToolParameters.CreateFromDictionary(paramsDict);
                    if (restored != null)
                        node.Parameters = restored;
                }
            }

            // 恢复参数绑定
            if (dict.TryGetValue("ParameterBindings", out var bindingsVal) && bindingsVal is Dictionary<string, object> bindingsDict)
            {
                node.ParameterBindings = ParameterBindingContainer.FromDictionary(bindingsDict);
            }

            return node;
        }

        /// <summary>
        /// 克隆节点
        /// </summary>
        public WorkflowNode Clone()
        {
            var cloned = new WorkflowNode(
                Guid.NewGuid().ToString(),
                $"{Name}_副本",
                Type
            )
            {
                AlgorithmType = AlgorithmType,
                IsEnabled = IsEnabled,
                ParametersTypeName = ParametersTypeName,
                Parameters = Parameters?.Clone(),
                ParameterBindings = ParameterBindings?.Clone()
            };

            return cloned;
        }
    }
}
