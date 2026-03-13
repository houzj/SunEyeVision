using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using SunEyeVision.Plugin.SDK.Models;

namespace SunEyeVision.Workflow;

/// <summary>
/// 项目（产品检测方案）
/// </summary>
/// <remarks>
/// 一个项目对应一个产品的检测方案，包含一个程序和多个配方。
/// 
/// 架构层次：
/// - Solution（解决方案）：目录层级，不需要索引文件
/// - Project（项目）：一个产品对应一个项目
///   - InspectionProgram（检测程序）：执行流
///   - InspectionRecipe（检测配方）：数据流 + 设备信息
/// 
/// 使用场景：
/// 1. 新产品检测方案开发
/// 2. 产品切换（加载不同项目）
/// 3. 配方管理（同一产品的不同检测模式）
/// </remarks>
public class Project : ObservableObject
{
    private string _id = Guid.NewGuid().ToString();
    private string _name = "新建项目";
    private string _productCode = string.Empty;
    private string _description = string.Empty;
    private string _storagePath = string.Empty;
    private DateTime _createdTime = DateTime.Now;
    private DateTime _modifiedTime = DateTime.Now;

    /// <summary>
    /// 项目唯一标识符
    /// </summary>
    public string Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    /// <summary>
    /// 项目名称
    /// </summary>
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value, "项目名称");
    }

    /// <summary>
    /// 产品编码
    /// </summary>
    /// <remarks>
    /// 用于产品识别和追踪。
    /// 例如：A100-2026-001
    /// </remarks>
    public string ProductCode
    {
        get => _productCode;
        set => SetProperty(ref _productCode, value, "产品编码");
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
    /// 项目存储路径
    /// </summary>
    /// <remarks>
    /// 记录项目的实际存储路径，支持自定义路径。
    /// 如果为空，则使用默认路径（solutions/projects/{projectId}）。
    /// </remarks>
    [JsonIgnore]
    public string StoragePath
    {
        get => _storagePath;
        set => SetProperty(ref _storagePath, value);
    }

    /// <summary>
    /// 检测程序（执行流）
    /// </summary>
    /// <remarks>
    /// 每个项目只有一个程序，定义检测的执行逻辑。
    /// </remarks>
    public InspectionProgram Program { get; set; } = new();

    /// <summary>
    /// 检测配方列表（数据流 + 设备信息）
    /// </summary>
    /// <remarks>
    /// 一个项目可以有多个配方，对应不同的产线、设备或检测模式。
    /// 例如：标准配方、产线1配方、产线2配方、快速检测配方
    /// </remarks>
    public ObservableCollection<InspectionRecipe> Recipes { get; set; } = new();

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
    /// 添加配方
    /// </summary>
    public InspectionRecipe AddRecipe(string name, string? description = null)
    {
        var recipe = new InspectionRecipe
        {
            Name = name,
            ProjectId = Id,
            Description = description ?? string.Empty,
            CreatedTime = DateTime.Now,
            ModifiedTime = DateTime.Now
        };

        // 从程序的默认参数创建初始配方
        foreach (var kvp in Program.DefaultParams)
        {
            recipe.NodeParams[kvp.Key] = kvp.Value.Clone();
        }

        Recipes.Add(recipe);
        ModifiedTime = DateTime.Now;

        return recipe;
    }

    /// <summary>
    /// 移除配方
    /// </summary>
    public void RemoveRecipe(string recipeId)
    {
        var recipe = GetRecipe(recipeId);
        if (recipe != null)
        {
            Recipes.Remove(recipe);
            ModifiedTime = DateTime.Now;
        }
    }

    /// <summary>
    /// 获取配方
    /// </summary>
    public InspectionRecipe? GetRecipe(string recipeId)
    {
        foreach (var recipe in Recipes)
        {
            if (recipe.Id == recipeId)
                return recipe;
        }
        return null;
    }

    /// <summary>
    /// 获取配方（按名称）
    /// </summary>
    public InspectionRecipe? GetRecipeByName(string name)
    {
        foreach (var recipe in Recipes)
        {
            if (recipe.Name == name)
                return recipe;
        }
        return null;
    }

    /// <summary>
    /// 重命名配方
    /// </summary>
    /// <remarks>
    /// 返回旧配方名称，用于删除旧文件。
    /// </remarks>
    public string? RenameRecipe(string recipeId, string newName)
    {
        var recipe = GetRecipe(recipeId);
        if (recipe == null)
            return null;

        var oldName = recipe.Name;
        recipe.Name = newName;
        recipe.ModifiedTime = DateTime.Now;
        ModifiedTime = DateTime.Now;

        return oldName;
    }

    /// <summary>
    /// 复制配方
    /// </summary>
    public InspectionRecipe DuplicateRecipe(string recipeId, string newName)
    {
        var original = GetRecipe(recipeId);
        if (original == null)
            throw new ArgumentException($"配方不存在: {recipeId}");

        var cloned = original.Clone();
        cloned.Name = newName;
        cloned.ProjectId = Id;
        cloned.CreatedTime = DateTime.Now;
        cloned.ModifiedTime = DateTime.Now;

        Recipes.Add(cloned);
        ModifiedTime = DateTime.Now;

        return cloned;
    }

    /// <summary>
    /// 验证项目配置
    /// </summary>
    public bool Validate()
    {
        if (string.IsNullOrEmpty(Name))
            return false;

        if (Program == null)
            return false;

        // 验证所有配方
        foreach (var recipe in Recipes)
        {
            if (string.IsNullOrEmpty(recipe.Name))
                return false;

            if (recipe.ProjectId != Id)
                return false;
        }

        return true;
    }

    /// <summary>
    /// 克隆项目
    /// </summary>
    public Project Clone()
    {
        var cloned = new Project
        {
            Id = Guid.NewGuid().ToString(),
            Name = Name + " (副本)",
            ProductCode = ProductCode,
            Description = Description,
            CreatedTime = DateTime.Now,
            ModifiedTime = DateTime.Now
        };

        // 深拷贝程序
        cloned.Program = Program.Clone();

        // 深拷贝配方
        foreach (var recipe in Recipes)
        {
            var clonedRecipe = recipe.Clone();
            clonedRecipe.ProjectId = cloned.Id;
            cloned.Recipes.Add(clonedRecipe);
        }

        return cloned;
    }
}
