using System;
using System.Collections.Generic;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.SDK.Models;

namespace SunEyeVision.Workflow;

/// <summary>
/// 配方组 - 同一产品的多种检测模式
/// </summary>
/// <remarks>
/// 配方组用于管理同一产品的不同检测场景：
/// - 一个产品可以有多个配方组（如：正常模式、快速模式、高精度模式）
/// - 每个配方组包含一组节点参数配置
/// - 支持运行时动态切换配方组
/// 
/// 使用场景：
/// 1. 不同光照条件下的检测参数
/// 2. 不同速度要求下的参数配置
/// 3. 不同精度要求下的参数配置
/// </remarks>
public class RecipeGroup : ObservableObject
{
    private string _name = string.Empty;
    private string _description = string.Empty;
    private DateTime _createdTime = DateTime.Now;
    private DateTime _modifiedTime = DateTime.Now;

    /// <summary>
    /// 配方组名称
    /// </summary>
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value, "配方组名称");
    }

    /// <summary>
    /// 配方组描述
    /// </summary>
    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    /// <summary>
    /// 节点参数映射：NodeId -> ToolParameters
    /// </summary>
    /// <remarks>
    /// 存储该配方组中所有节点的参数配置。
    /// ToolParameters 使用 JsonPolymorphic 支持多态序列化。
    /// </remarks>
    public Dictionary<string, ToolParameters> NodeParams { get; set; } = new();

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedTime
    {
        get => _createdTime;
        set => SetProperty(ref _createdTime, value);
    }

    /// <summary>
    /// 最后修改时间
    /// </summary>
    public DateTime ModifiedTime
    {
        get => _modifiedTime;
        set => SetProperty(ref _modifiedTime, value);
    }

    /// <summary>
    /// 保存节点参数
    /// </summary>
    /// <param name="nodeId">节点ID</param>
    /// <param name="parameters">参数实例</param>
    public void SaveNodeParams(string nodeId, ToolParameters parameters)
    {
        if (string.IsNullOrEmpty(nodeId))
            throw new ArgumentException("节点ID不能为空", nameof(nodeId));

        if (parameters == null)
            throw new ArgumentNullException(nameof(parameters));

        NodeParams[nodeId] = parameters.Clone();
        ModifiedTime = DateTime.Now;
    }

    /// <summary>
    /// 获取节点参数
    /// </summary>
    /// <param name="nodeId">节点ID</param>
    /// <returns>参数实例的克隆，如果不存在则返回null</returns>
    public ToolParameters? GetNodeParams(string nodeId)
    {
        if (!NodeParams.TryGetValue(nodeId, out var parameters))
            return null;

        return parameters.Clone();
    }

    /// <summary>
    /// 移除节点参数
    /// </summary>
    /// <param name="nodeId">节点ID</param>
    public void RemoveNodeParams(string nodeId)
    {
        if (NodeParams.Remove(nodeId))
        {
            ModifiedTime = DateTime.Now;
        }
    }

    /// <summary>
    /// 清空所有参数
    /// </summary>
    public void ClearParams()
    {
        NodeParams.Clear();
        ModifiedTime = DateTime.Now;
    }

    /// <summary>
    /// 获取节点参数
    /// </summary>
    public ToolParameters? GetNodeParameters(string nodeId)
    {
        return GetNodeParams(nodeId);
    }

    /// <summary>
    /// 保存节点参数
    /// </summary>
    public void SaveNodeParameters(string nodeId, ToolParameters parameters)
    {
        SaveNodeParams(nodeId, parameters);
    }

    /// <summary>
    /// 克隆配方组
    /// </summary>
    public RecipeGroup Clone()
    {
        var cloned = new RecipeGroup
        {
            Name = Name,
            Description = Description,
            CreatedTime = DateTime.Now,
            ModifiedTime = DateTime.Now
        };

        // 深拷贝节点参数
        foreach (var kvp in NodeParams)
        {
            cloned.NodeParams[kvp.Key] = kvp.Value.Clone();
        }

        return cloned;
    }

    /// <summary>
    /// 验证配方组
    /// </summary>
    public (bool IsValid, List<string> Errors) Validate()
    {
        var errors = new List<string>();

        // 检查是否有节点参数
        if (NodeParams.Count == 0)
        {
            errors.Add("配方组没有节点参数");
        }

        // 验证节点参数
        foreach (var kvp in NodeParams)
        {
            if (kvp.Value == null)
            {
                errors.Add($"节点 {kvp.Key} 的参数为空");
            }
        }

        return (errors.Count == 0, errors);
    }

    /// <summary>
    /// 从 Workflow 创建默认 RecipeGroup
    /// </summary>
    /// <param name="workflow">工作流</param>
    /// <returns>配方组实例</returns>
    public static RecipeGroup FromWorkflow(Workflow workflow)
    {
        var group = new RecipeGroup
        {
            Name = "default",
            Description = "默认配方组",
            CreatedTime = DateTime.Now,
            ModifiedTime = DateTime.Now
        };

        // 从工作流的默认参数复制
        foreach (var kvp in workflow.DefaultParams)
        {
            group.NodeParams[kvp.Key] = kvp.Value?.Clone();
        }

        return group;
    }
}
