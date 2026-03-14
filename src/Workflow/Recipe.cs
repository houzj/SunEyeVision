using System;
using System.Collections.Generic;
using System.Linq;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.SDK.Models;

namespace SunEyeVision.Workflow;

/// <summary>
/// 配方（数据流配置）
/// </summary>
/// <remarks>
/// 包含特定产品的参数配置,关联一个Workflow,支持多个配方组。
///
/// 特性：
/// - 多配方组：支持default/precision/fast等多种模式
/// - 全局变量：跨节点共享变量
/// - 产品相关：不同产品使用不同的Recipe
/// </remarks>
public class Recipe : ObservableObject
{
    private string _name = "新建配方";
    private string _description = "";

    /// <summary>
    /// 配方唯一标识符
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

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
        set => SetProperty(ref _description, value, "描述");
    }

    /// <summary>
    /// 关联的工作流ID
    /// </summary>
    public string WorkflowRef { get; set; } = "";

    /// <summary>
    /// 配方组字典：Key为配方组名称(default/precision/fast等)
    /// </summary>
    public Dictionary<string, RecipeGroup> RecipeGroups { get; set; } = new();

    /// <summary>
    /// 全局变量字典
    /// </summary>
    public Dictionary<string, GlobalVariable> GlobalVariables { get; set; } = new();

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedTime { get; set; } = DateTime.Now;

    /// <summary>
    /// 修改时间
    /// </summary>
    public DateTime ModifiedTime { get; set; } = DateTime.Now;

    /// <summary>
    /// 获取指定配方组的参数
    /// </summary>
    public RecipeGroup? GetRecipeGroup(string groupName)
    {
        return RecipeGroups.TryGetValue(groupName, out var group) ? group : null;
    }

    /// <summary>
    /// 获取当前配方组（默认为" default "）
    /// </summary>
    public RecipeGroup? GetCurrentRecipeGroup(string groupName = "default")
    {
        return GetRecipeGroup(groupName);
    }

    /// <summary>
    /// 添加配方组
    /// </summary>
    public void AddRecipeGroup(RecipeGroup group)
    {
        if (string.IsNullOrEmpty(group.Name))
            throw new ArgumentException("配方组名称不能为空");

        RecipeGroups[group.Name] = group;
        ModifiedTime = DateTime.Now;
    }

    /// <summary>
    /// 移除配方组
    /// </summary>
    public bool RemoveRecipeGroup(string groupName)
    {
        if (RecipeGroups.Remove(groupName))
        {
            ModifiedTime = DateTime.Now;
            return true;
        }
        return false;
    }

    /// <summary>
    /// 获取全局变量值
    /// </summary>
    public T? GetGlobalVariable<T>(string name)
    {
        if (!GlobalVariables.TryGetValue(name, out var variable))
            return default;

        if (variable.Value is T value)
            return value;

        try
        {
            return (T?)Convert.ChangeType(variable.Value, typeof(T));
        }
        catch
        {
            return default;
        }
    }

    /// <summary>
    /// 设置全局变量
    /// </summary>
    public void SetGlobalVariable(string name, object? value, string type = "String", string description = "")
    {
        GlobalVariables[name] = new GlobalVariable
        {
            Name = name,
            Value = value,
            Type = type,
            Description = description
        };
        ModifiedTime = DateTime.Now;
    }

    /// <summary>
    /// 移除全局变量
    /// </summary>
    public bool RemoveGlobalVariable(string name)
    {
        if (GlobalVariables.Remove(name))
        {
            ModifiedTime = DateTime.Now;
            return true;
        }
        return false;
    }

    /// <summary>
    /// 获取节点参数
    /// </summary>
    public ToolParameters? GetNodeParameters(string nodeId, string groupName = "default")
    {
        var group = GetRecipeGroup(groupName);
        if (group == null)
            return null;

        return group.GetNodeParameters(nodeId);
    }

    /// <summary>
    /// 保存节点参数
    /// </summary>
    public void SaveNodeParameters(string nodeId, ToolParameters parameters, string groupName = "default")
    {
        var group = GetRecipeGroup(groupName);
        if (group == null)
        {
            // 如果配方组不存在，创建一个新的
            group = new RecipeGroup
            {
                Name = groupName,
                Description = $"配方组: {groupName}",
                CreatedTime = DateTime.Now,
                ModifiedTime = DateTime.Now
            };
            AddRecipeGroup(group);
        }

        group.SaveNodeParameters(nodeId, parameters);
        ModifiedTime = DateTime.Now;
    }

    /// <summary>
    /// 克隆配方
    /// </summary>
    public Recipe Clone()
    {
        var cloned = new Recipe
        {
            Id = Guid.NewGuid().ToString(),
            Name = $"{Name} (副本)",
            Description = Description,
            WorkflowRef = WorkflowRef,
            CreatedTime = DateTime.Now,
            ModifiedTime = DateTime.Now
        };

        // 克隆配方组
        foreach (var kvp in RecipeGroups)
        {
            cloned.RecipeGroups[kvp.Key] = kvp.Value.Clone();
        }

        // 克隆全局变量
        foreach (var kvp in GlobalVariables)
        {
            cloned.GlobalVariables[kvp.Key] = new GlobalVariable
            {
                Name = kvp.Value.Name,
                Value = kvp.Value.Value,
                Type = kvp.Value.Type,
                Description = kvp.Value.Description
            };
        }

        return cloned;
    }

    /// <summary>
    /// 验证配方
    /// </summary>
    public (bool IsValid, List<string> Errors) Validate()
    {
        var errors = new List<string>();

        // 检查是否有关联的工作流
        if (string.IsNullOrEmpty(WorkflowRef))
        {
            errors.Add("配方没有关联的工作流");
        }

        // 检查是否有配方组
        if (RecipeGroups.Count == 0)
        {
            errors.Add("配方没有配方组");
        }

        // 检查配方组名称唯一性
        var groupNames = new HashSet<string>();
        foreach (var group in RecipeGroups.Values)
        {
            if (string.IsNullOrEmpty(group.Name))
            {
                errors.Add("配方组名称为空");
            }
            else if (!groupNames.Add(group.Name))
            {
                errors.Add($"配方组名称重复: {group.Name}");
            }
        }

        // 验证每个配方组
        foreach (var group in RecipeGroups.Values)
        {
            var (isValid, groupErrors) = group.Validate();
            if (!isValid)
            {
                errors.AddRange(groupErrors.Select(e => $"配方组 [{group.Name}]: {e}"));
            }
        }

        return (errors.Count == 0, errors);
    }
}
