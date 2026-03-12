using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using SunEyeVision.Plugin.SDK.Models;

namespace SunEyeVision.Workflow;

/// <summary>
/// 数据配置（数据流配置）
/// </summary>
/// <remarks>
/// 数据配置管理参数和全局变量，与执行逻辑完全分离：
/// - 包含配方组、全局变量
/// - 不包含节点拓扑结构（由 WorkflowDefinition 管理）
/// - 支持快速切换和独立管理
/// 
/// 解耦优势：
/// 1. 产品切换：只需切换 DataConfiguration 引用（< 1秒）
/// 2. 配方组切换：只需切换 RecipeGroup 引用（< 1秒）
/// 3. 参数调优：修改数据配置不影响工作流结构
/// 4. 批量导入：新产品只需创建新的 DataConfiguration
/// </remarks>
public class DataConfiguration : ObservableObject
{
    private string _id = Guid.NewGuid().ToString();
    private string _name = "新建数据配置";
    private string _description = string.Empty;
    private string _productCode = string.Empty;
    private DateTime _createdTime = DateTime.Now;
    private DateTime _modifiedTime = DateTime.Now;

    /// <summary>
    /// 数据配置唯一标识符
    /// </summary>
    public string Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    /// <summary>
    /// 数据配置名称
    /// </summary>
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value, "数据配置名称");
    }

    /// <summary>
    /// 描述信息
    /// </summary>
    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    /// <summary>
    /// 产品编码（用于关联产品信息）
    /// </summary>
    public string ProductCode
    {
        get => _productCode;
        set => SetProperty(ref _productCode, value, "产品编码");
    }

    /// <summary>
    /// 配方组集合
    /// </summary>
    /// <remarks>
    /// Key: 配方组名称
    /// Value: 配方组实例
    /// 
    /// 支持一个产品有多种检测模式：
    /// - default: 默认模式
    /// - fast: 快速模式
    /// - precision: 高精度模式
    /// </remarks>
    public Dictionary<string, RecipeGroup> RecipeGroups { get; set; } = new();

    /// <summary>
    /// 全局变量集合
    /// </summary>
    /// <remarks>
    /// Key: 变量名称
    /// Value: 变量实例
    /// 
    /// 全局变量可以在工作流中跨节点共享。
    /// </remarks>
    public Dictionary<string, GlobalVariable> GlobalVariables { get; set; } = new();

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
    /// 获取指定配方组的节点参数
    /// </summary>
    /// <param name="recipeGroupName">配方组名称，默认为 "default"</param>
    /// <returns>节点参数映射，如果不存在则返回null</returns>
    public Dictionary<string, Plugin.SDK.Execution.Parameters.ToolParameters>? GetNodeParams(string recipeGroupName = "default")
    {
        if (RecipeGroups.TryGetValue(recipeGroupName, out var group))
            return group.NodeParams;
        return null;
    }

    /// <summary>
    /// 设置指定配方组的节点参数
    /// </summary>
    /// <param name="recipeGroupName">配方组名称</param>
    /// <param name="nodeParams">节点参数映射</param>
    public void SetNodeParams(string recipeGroupName, Dictionary<string, Plugin.SDK.Execution.Parameters.ToolParameters> nodeParams)
    {
        if (!RecipeGroups.TryGetValue(recipeGroupName, out var group))
        {
            group = new RecipeGroup { Name = recipeGroupName };
            RecipeGroups[recipeGroupName] = group;
        }

        group.NodeParams = nodeParams;
        group.ModifiedTime = DateTime.Now;
        ModifiedTime = DateTime.Now;
    }

    /// <summary>
    /// 获取全局变量值
    /// </summary>
    /// <typeparam name="T">变量类型</typeparam>
    /// <param name="name">变量名称</param>
    /// <returns>变量值，如果不存在则返回默认值</returns>
    public T? GetGlobalVariable<T>(string name)
    {
        if (GlobalVariables.TryGetValue(name, out var variable))
        {
            if (variable.Value is T typedValue)
                return typedValue;
            try
            {
                return (T?)Convert.ChangeType(variable.Value, typeof(T));
            }
            catch
            {
                return default;
            }
        }
        return default;
    }

    /// <summary>
    /// 设置全局变量
    /// </summary>
    /// <param name="name">变量名称</param>
    /// <param name="value">变量值</param>
    /// <param name="description">变量描述</param>
    public void SetGlobalVariable(string name, object value, string? description = null)
    {
        if (GlobalVariables.TryGetValue(name, out var existing))
        {
            existing.Value = value;
            if (description != null)
                existing.Description = description;
        }
        else
        {
            GlobalVariables[name] = new GlobalVariable
            {
                Name = name,
                Value = value,
                Description = description ?? string.Empty
            };
        }
        ModifiedTime = DateTime.Now;
    }

    /// <summary>
    /// 移除全局变量
    /// </summary>
    /// <param name="name">变量名称</param>
    public void RemoveGlobalVariable(string name)
    {
        if (GlobalVariables.Remove(name))
        {
            ModifiedTime = DateTime.Now;
        }
    }

    /// <summary>
    /// 添加配方组
    /// </summary>
    /// <param name="group">配方组实例</param>
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
    /// <param name="name">配方组名称</param>
    public void RemoveRecipeGroup(string name)
    {
        if (RecipeGroups.Remove(name))
        {
            ModifiedTime = DateTime.Now;
        }
    }

    /// <summary>
    /// 获取或创建配方组
    /// </summary>
    /// <param name="name">配方组名称</param>
    /// <returns>配方组实例</returns>
    public RecipeGroup GetOrCreateRecipeGroup(string name)
    {
        if (!RecipeGroups.TryGetValue(name, out var group))
        {
            group = new RecipeGroup
            {
                Name = name,
                Description = $"{name} 配方组",
                CreatedTime = DateTime.Now,
                ModifiedTime = DateTime.Now
            };
            RecipeGroups[name] = group;
            ModifiedTime = DateTime.Now;
        }
        return group;
    }

    /// <summary>
    /// 从现有 Recipe 创建 DataConfiguration
    /// </summary>
    /// <param name="recipe">现有配方</param>
    /// <param name="groupName">配方组名称，默认为 "default"</param>
    /// <returns>数据配置实例</returns>
    public static DataConfiguration FromRecipe(Recipe recipe, string groupName = "default")
    {
        var config = new DataConfiguration
        {
            Id = recipe.Id,
            Name = recipe.Name,
            Description = recipe.Description,
            CreatedTime = DateTime.Now,
            ModifiedTime = DateTime.Now
        };

        config.RecipeGroups[groupName] = RecipeGroup.FromRecipe(recipe);

        return config;
    }

    /// <summary>
    /// 从现有 Solution 创建 DataConfiguration
    /// </summary>
    /// <param name="solution">现有方案</param>
    /// <returns>数据配置实例</returns>
    public static DataConfiguration FromSolution(Solution solution)
    {
        var config = new DataConfiguration
        {
            Id = solution.Id + "_data",
            Name = solution.Name + " 数据配置",
            Description = solution.Description,
            CreatedTime = solution.CreatedAt,
            ModifiedTime = solution.ModifiedAt
        };

        // 转换全局变量
        if (solution.GlobalVariables != null)
        {
            foreach (var kvp in solution.GlobalVariables)
            {
                config.GlobalVariables[kvp.Key] = kvp.Value;
            }
        }

        // 转换配方为配方组
        if (solution.Recipes != null)
        {
            foreach (var recipe in solution.Recipes)
            {
                config.RecipeGroups[recipe.Name] = RecipeGroup.FromRecipe(recipe);
            }
        }

        return config;
    }

    /// <summary>
    /// 克隆数据配置
    /// </summary>
    public DataConfiguration Clone()
    {
        var cloned = new DataConfiguration
        {
            Id = Guid.NewGuid().ToString(),
            Name = Name + " (副本)",
            Description = Description,
            ProductCode = ProductCode,
            CreatedTime = DateTime.Now,
            ModifiedTime = DateTime.Now
        };

        // 深拷贝配方组
        foreach (var kvp in RecipeGroups)
        {
            var groupClone = new RecipeGroup
            {
                Name = kvp.Value.Name,
                Description = kvp.Value.Description,
                CreatedTime = kvp.Value.CreatedTime,
                ModifiedTime = DateTime.Now
            };

            foreach (var paramKvp in kvp.Value.NodeParams)
            {
                groupClone.NodeParams[paramKvp.Key] = paramKvp.Value.Clone();
            }

            cloned.RecipeGroups[kvp.Key] = groupClone;
        }

        // 深拷贝全局变量
        foreach (var kvp in GlobalVariables)
        {
            cloned.GlobalVariables[kvp.Key] = new GlobalVariable
            {
                Name = kvp.Value.Name,
                Value = kvp.Value.Value,
                Type = kvp.Value.Type
            };
        }

        return cloned;
    }
}
