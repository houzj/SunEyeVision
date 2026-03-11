using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.SDK.Models;

namespace SunEyeVision.Workflow;

/// <summary>
/// 配方模型
/// </summary>
public class Recipe : ObservableObject
{
    private string _id = Guid.NewGuid().ToString();
    private string _name = "新建配方";
    private string _description = string.Empty;

    /// <summary>
    /// 配方唯一标识符
    /// </summary>
    public string Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    /// <summary>
    /// 配方名称
    /// </summary>
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value, "配方名称");
    }

    /// <summary>
    /// 配方描述
    /// </summary>
    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value, "配方描述");
    }

    /// <summary>
    /// 参数映射：节点ID -> ToolParameters实例
    /// </summary>
    public Dictionary<string, ToolParameters> ParameterMappings { get; set; } = new();

    /// <summary>
    /// 保存节点参数
    /// </summary>
    public void SaveParameters(string nodeId, ToolParameters parameters)
    {
        if (string.IsNullOrEmpty(nodeId))
            throw new ArgumentException("节点ID不能为空");

        if (parameters == null)
            throw new ArgumentNullException(nameof(parameters));

        // 克隆参数以避免引用问题
        ParameterMappings[nodeId] = parameters.Clone();
    }

    /// <summary>
    /// 获取节点参数
    /// </summary>
    public ToolParameters? GetParameters(string nodeId)
    {
        if (!ParameterMappings.TryGetValue(nodeId, out var parameters))
            return null;

        return parameters.Clone();
    }

    /// <summary>
    /// 移除节点参数
    /// </summary>
    public void RemoveParameters(string nodeId)
    {
        ParameterMappings.Remove(nodeId);
    }

    /// <summary>
    /// 清空所有参数
    /// </summary>
    public void ClearParameters()
    {
        ParameterMappings.Clear();
    }
}
