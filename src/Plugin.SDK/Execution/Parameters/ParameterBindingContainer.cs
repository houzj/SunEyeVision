using System;
using System.Collections.Generic;
using System.Linq;

namespace SunEyeVision.Plugin.SDK.Execution.Parameters
{
    /// <summary>
    /// 参数绑定容器
    /// </summary>
    /// <remarks>
    /// 管理一个节点或工具的所有参数绑定配置。
    /// 提供绑定的增删改查、验证和序列化功能。
    /// 
    /// 核心功能：
    /// 1. 管理多个参数绑定
    /// 2. 支持快速查找和更新
    /// 3. 批量验证
    /// 4. 序列化支持
    /// 
    /// 使用示例：
    /// <code>
    /// var container = new ParameterBindingContainer();
    /// 
    /// // 添加常量绑定
    /// container.SetBinding(ParameterBinding.CreateConstant("Threshold", 128));
    /// 
    /// // 添加动态绑定
    /// container.SetBinding(ParameterBinding.CreateDynamic("MinRadius", "node_001", "Radius"));
    /// 
    /// // 获取绑定
    /// var thresholdBinding = container.GetBinding("Threshold");
    /// 
    /// // 验证所有绑定
    /// var validationResult = container.ValidateAll();
    /// </code>
    /// </remarks>
    public class ParameterBindingContainer
    {
        private readonly Dictionary<string, ParameterBinding> _bindings = new Dictionary<string, ParameterBinding>();

        /// <summary>
        /// 节点ID（可选）
        /// </summary>
        public string? NodeId { get; set; }

        /// <summary>
        /// 工具名称（可选）
        /// </summary>
        public string? ToolName { get; set; }

        /// <summary>
        /// 所有绑定数量
        /// </summary>
        public int Count => _bindings.Count;

        /// <summary>
        /// 所有参数名称
        /// </summary>
        public IEnumerable<string> ParameterNames => _bindings.Keys;

        /// <summary>
        /// 所有绑定
        /// </summary>
        public IEnumerable<ParameterBinding> Bindings => _bindings.Values;

        /// <summary>
        /// 获取或设置绑定
        /// </summary>
        /// <param name="parameterName">参数名称</param>
        /// <returns>参数绑定，如果不存在则返回null</returns>
        public ParameterBinding? this[string parameterName]
        {
            get => GetBinding(parameterName);
            set
            {
                if (value != null)
                {
                    SetBinding(value);
                }
                else
                {
                    RemoveBinding(parameterName);
                }
            }
        }

        /// <summary>
        /// 设置绑定
        /// </summary>
        /// <param name="binding">参数绑定</param>
        public void SetBinding(ParameterBinding binding)
        {
            if (binding == null)
                throw new ArgumentNullException(nameof(binding));

            _bindings[binding.ParameterName] = binding;
        }

        /// <summary>
        /// 设置常量绑定
        /// </summary>
        /// <param name="parameterName">参数名称</param>
        /// <param name="value">常量值</param>
        public void SetConstantBinding(string parameterName, object? value)
        {
            SetBinding(ParameterBinding.CreateConstant(parameterName, value));
        }

        /// <summary>
        /// 设置动态绑定
        /// </summary>
        /// <param name="parameterName">参数名称</param>
        /// <param name="sourceNodeId">源节点ID</param>
        /// <param name="sourceProperty">源属性名称</param>
        /// <param name="transformExpression">转换表达式（可选）</param>
        public void SetDynamicBinding(
            string parameterName,
            string sourceNodeId,
            string sourceProperty,
            string? transformExpression = null)
        {
            SetBinding(ParameterBinding.CreateDynamic(parameterName, sourceNodeId, sourceProperty, transformExpression));
        }

        /// <summary>
        /// 获取绑定
        /// </summary>
        /// <param name="parameterName">参数名称</param>
        /// <returns>参数绑定，如果不存在则返回null</returns>
        public ParameterBinding? GetBinding(string parameterName)
        {
            _bindings.TryGetValue(parameterName, out var binding);
            return binding;
        }

        /// <summary>
        /// 检查是否存在绑定
        /// </summary>
        /// <param name="parameterName">参数名称</param>
        /// <returns>是否存在</returns>
        public bool HasBinding(string parameterName)
        {
            return _bindings.ContainsKey(parameterName);
        }

        /// <summary>
        /// 移除绑定
        /// </summary>
        /// <param name="parameterName">参数名称</param>
        /// <returns>是否成功移除</returns>
        public bool RemoveBinding(string parameterName)
        {
            return _bindings.Remove(parameterName);
        }

        /// <summary>
        /// 清空所有绑定
        /// </summary>
        public void Clear()
        {
            _bindings.Clear();
        }

        /// <summary>
        /// 验证所有绑定
        /// </summary>
        /// <returns>批量验证结果</returns>
        public ContainerValidationResult ValidateAll()
        {
            var result = new ContainerValidationResult { IsValid = true };

            foreach (var binding in _bindings.Values)
            {
                var bindingResult = binding.Validate();
                if (!bindingResult.IsValid)
                {
                    result.IsValid = false;
                    result.BindingErrors[binding.ParameterName] = bindingResult.Errors;
                }
            }

            return result;
        }

        /// <summary>
        /// 获取所有动态绑定
        /// </summary>
        /// <returns>动态绑定列表</returns>
        public IEnumerable<ParameterBinding> GetDynamicBindings()
        {
            return _bindings.Values.Where(b => b.BindingType == BindingType.DynamicBinding);
        }

        /// <summary>
        /// 获取所有常量绑定
        /// </summary>
        /// <returns>常量绑定列表</returns>
        public IEnumerable<ParameterBinding> GetConstantBindings()
        {
            return _bindings.Values.Where(b => b.BindingType == BindingType.Constant);
        }

        /// <summary>
        /// 获取指定源节点的绑定
        /// </summary>
        /// <param name="sourceNodeId">源节点ID</param>
        /// <returns>绑定列表</returns>
        public IEnumerable<ParameterBinding> GetBindingsBySourceNode(string sourceNodeId)
        {
            return _bindings.Values.Where(b => b.SourceNodeId == sourceNodeId);
        }

        /// <summary>
        /// 克隆容器
        /// </summary>
        /// <returns>克隆的容器</returns>
        public ParameterBindingContainer Clone()
        {
            var cloned = new ParameterBindingContainer
            {
                NodeId = NodeId,
                ToolName = ToolName
            };

            foreach (var binding in _bindings.Values)
            {
                cloned.SetBinding(binding.Clone());
            }

            return cloned;
        }

        /// <summary>
        /// 转换为字典（用于序列化）
        /// </summary>
        public Dictionary<string, object> ToDictionary()
        {
            var dict = new Dictionary<string, object>();

            if (!string.IsNullOrEmpty(NodeId))
                dict["NodeId"] = NodeId;

            if (!string.IsNullOrEmpty(ToolName))
                dict["ToolName"] = ToolName;

            var bindingsList = new List<Dictionary<string, object>>();
            foreach (var binding in _bindings.Values)
            {
                bindingsList.Add(binding.ToDictionary());
            }
            dict["Bindings"] = bindingsList;

            return dict;
        }

        /// <summary>
        /// 从字典创建容器（用于反序列化）
        /// </summary>
        public static ParameterBindingContainer FromDictionary(Dictionary<string, object> dict)
        {
            var container = new ParameterBindingContainer();

            if (dict.TryGetValue("NodeId", out var nodeId))
                container.NodeId = nodeId?.ToString();

            if (dict.TryGetValue("ToolName", out var toolName))
                container.ToolName = toolName?.ToString();

            if (dict.TryGetValue("Bindings", out var bindingsObj) && bindingsObj is List<object> bindingsList)
            {
                foreach (var bindingObj in bindingsList)
                {
                    if (bindingObj is Dictionary<string, object> bindingDict)
                    {
                        var binding = ParameterBinding.FromDictionary(bindingDict);
                        container.SetBinding(binding);
                    }
                }
            }

            return container;
        }

        /// <summary>
        /// 获取描述字符串
        /// </summary>
        public override string ToString()
        {
            var desc = $"ParameterBindingContainer({Count} bindings)";
            if (!string.IsNullOrEmpty(NodeId))
                desc += $" [Node: {NodeId}]";
            if (!string.IsNullOrEmpty(ToolName))
                desc += $" [Tool: {ToolName}]";
            return desc;
        }
    }

    /// <summary>
    /// 容器验证结果
    /// </summary>
    public class ContainerValidationResult
    {
        /// <summary>
        /// 是否有效
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// 各参数的错误信息
        /// </summary>
        public Dictionary<string, List<string>> BindingErrors { get; set; } = new Dictionary<string, List<string>>();

        /// <summary>
        /// 所有错误信息
        /// </summary>
        public IEnumerable<string> AllErrors => BindingErrors.Values.SelectMany(e => e);
    }
}
