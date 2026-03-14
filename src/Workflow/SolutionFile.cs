using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using SunEyeVision.Plugin.SDK.Logging;
using SunEyeVision.Plugin.SDK.Models;

namespace SunEyeVision.Workflow;

/// <summary>
/// 解决方案文件（单文件包含所有配置）
/// </summary>
/// <remarks>
/// 所有数据（工作流、配方、绑定关系）都保存在一个 .solution 文件中
///
/// 文件结构：
/// {
///   "name": "解决方案名称",
///   "version": "1.0",
///   "workflows": [ ... ],
///   "recipes": [ ... ],
///   "bindings": [ ... ]
/// }
/// </remarks>
public class SolutionFile : ObservableObject
{
    private string _name = "新建解决方案";
    private string _description = "";
    private string _currentDeviceId = "";
    private DateTime _createdTime = DateTime.Now;
    private DateTime _modifiedTime = DateTime.Now;

    /// <summary>
    /// 解决方案名称
    /// </summary>
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value, "解决方案名称");
    }

    /// <summary>
    /// 描述
    /// </summary>
    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value, "描述");
    }

    /// <summary>
    /// 解决方案版本
    /// </summary>
    public string Version { get; set; } = "1.0";

    /// <summary>
    /// 当前设备ID
    /// </summary>
    public string CurrentDeviceId
    {
        get => _currentDeviceId;
        set => SetProperty(ref _currentDeviceId, value, "当前设备");
    }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedTime
    {
        get => _createdTime;
        set => SetProperty(ref _createdTime, value);
    }

    /// <summary>
    /// 修改时间
    /// </summary>
    public DateTime ModifiedTime
    {
        get => _modifiedTime;
        set => SetProperty(ref _modifiedTime, value);
    }

    /// <summary>
    /// 工作流列表（执行流）
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<Workflow> Workflows { get; set; } = new();

    /// <summary>
    /// 配方列表（数据流）
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<Recipe> Recipes { get; set; } = new();

    /// <summary>
    /// 绑定关系列表（设备→Workflow+Recipe）
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<DeviceBinding> Bindings { get; set; } = new();

    /// <summary>
    /// 保存到文件
    /// </summary>
    public void Save(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            throw new ArgumentException("文件路径不能为空");

        ModifiedTime = DateTime.Now;

        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        var json = JsonSerializer.Serialize(this, jsonOptions);
        File.WriteAllText(filePath, json);

        VisionLogger.Instance.Log(LogLevel.Success, $"保存解决方案: {Name} -> {filePath}", "SolutionFile");
    }

    /// <summary>
    /// 从文件加载
    /// </summary>
    public static SolutionFile? Load(string filePath)
    {
        if (!File.Exists(filePath))
            return null;

        try
        {
            var json = File.ReadAllText(filePath);
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var solution = JsonSerializer.Deserialize<SolutionFile>(json, jsonOptions);
            if (solution != null)
            {
                VisionLogger.Instance.Log(LogLevel.Info, $"加载解决方案: {solution.Name} -> {filePath}", "SolutionFile");
            }

            return solution;
        }
        catch (Exception ex)
        {
            VisionLogger.Instance.Log(LogLevel.Error, $"加载解决方案失败: {filePath}, 错误: {ex.Message}", "SolutionFile", ex);
            return null;
        }
    }

    /// <summary>
    /// 获取当前设备绑定
    /// </summary>
    public DeviceBinding? GetCurrentBinding()
    {
        if (string.IsNullOrEmpty(CurrentDeviceId))
            return null;

        return Bindings.FirstOrDefault(b => b.DeviceId == CurrentDeviceId);
    }

    /// <summary>
    /// 获取当前工作流
    /// </summary>
    public Workflow? GetCurrentWorkflow()
    {
        var binding = GetCurrentBinding();
        if (binding == null || string.IsNullOrEmpty(binding.WorkflowRef))
            return null;

        return Workflows.FirstOrDefault(w => w.Id == binding.WorkflowRef);
    }

    /// <summary>
    /// 获取当前配方
    /// </summary>
    public Recipe? GetCurrentRecipe()
    {
        var binding = GetCurrentBinding();
        if (binding == null || string.IsNullOrEmpty(binding.RecipeRef))
            return null;

        return Recipes.FirstOrDefault(r => r.Id == binding.RecipeRef);
    }

    /// <summary>
    /// 获取当前配方组
    /// </summary>
    public RecipeGroup? GetCurrentRecipeGroup()
    {
        var binding = GetCurrentBinding();
        var recipe = GetCurrentRecipe();

        if (binding == null || recipe == null || string.IsNullOrEmpty(binding.RecipeGroupName))
            return null;

        return recipe.GetRecipeGroup(binding.RecipeGroupName);
    }

    /// <summary>
    /// 创建新解决方案
    /// </summary>
    public static SolutionFile CreateNew(string name)
    {
        return new SolutionFile
        {
            Name = name,
            Version = "1.0",
            CreatedTime = DateTime.Now,
            ModifiedTime = DateTime.Now,
            Workflows = new List<Workflow>(),
            Recipes = new List<Recipe>(),
            Bindings = new List<DeviceBinding>()
        };
    }

    /// <summary>
    /// 添加工作流
    /// </summary>
    public Workflow AddWorkflow(string name, string description = "")
    {
        var workflow = new Workflow
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            Description = description,
            Version = "1.0",
            CreatedTime = DateTime.Now,
            ModifiedTime = DateTime.Now
        };

        Workflows.Add(workflow);
        ModifiedTime = DateTime.Now;
        return workflow;
    }

    /// <summary>
    /// 获取工作流
    /// </summary>
    public Workflow? GetWorkflow(string workflowId)
    {
        return Workflows.FirstOrDefault(w => w.Id == workflowId);
    }

    /// <summary>
    /// 移除工作流
    /// </summary>
    public bool RemoveWorkflow(string workflowId)
    {
        var workflow = GetWorkflow(workflowId);
        if (workflow == null)
            return false;

        Workflows.Remove(workflow);

        // 移除相关的设备绑定
        Bindings.RemoveAll(b => b.WorkflowRef == workflowId);

        ModifiedTime = DateTime.Now;
        return true;
    }

    /// <summary>
    /// 添加配方
    /// </summary>
    public Recipe AddRecipe(string name, string workflowRef, string description = "")
    {
        var recipe = new Recipe
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            Description = description,
            WorkflowRef = workflowRef,
            CreatedTime = DateTime.Now,
            ModifiedTime = DateTime.Now
        };

        // 添加默认配方组
        var defaultGroup = new RecipeGroup
        {
            Name = "default",
            Description = "默认配方组",
            CreatedTime = DateTime.Now,
            ModifiedTime = DateTime.Now
        };

        recipe.AddRecipeGroup(defaultGroup);
        Recipes.Add(recipe);
        ModifiedTime = DateTime.Now;
        return recipe;
    }

    /// <summary>
    /// 获取配方
    /// </summary>
    public Recipe? GetRecipe(string recipeId)
    {
        return Recipes.FirstOrDefault(r => r.Id == recipeId);
    }

    /// <summary>
    /// 移除配方
    /// </summary>
    public bool RemoveRecipe(string recipeId)
    {
        var recipe = GetRecipe(recipeId);
        if (recipe == null)
            return false;

        Recipes.Remove(recipe);

        // 移除相关的设备绑定
        Bindings.RemoveAll(b => b.RecipeRef == recipeId);

        ModifiedTime = DateTime.Now;
        return true;
    }

    /// <summary>
    /// 添加设备绑定
    /// </summary>
    public DeviceBinding AddBinding(string deviceName, string workflowRef, string recipeRef, string recipeGroupName = "default")
    {
        var binding = new DeviceBinding
        {
            DeviceId = Guid.NewGuid().ToString(),
            DeviceName = deviceName,
            WorkflowRef = workflowRef,
            RecipeRef = recipeRef,
            RecipeGroupName = recipeGroupName,
            CreatedTime = DateTime.Now,
            LastSwitchTime = DateTime.Now
        };

        Bindings.Add(binding);
        ModifiedTime = DateTime.Now;
        return binding;
    }

    /// <summary>
    /// 获取设备绑定
    /// </summary>
    public DeviceBinding? GetBinding(string deviceId)
    {
        return Bindings.FirstOrDefault(b => b.DeviceId == deviceId);
    }

    /// <summary>
    /// 移除设备绑定
    /// </summary>
    public bool RemoveBinding(string deviceId)
    {
        var binding = GetBinding(deviceId);
        if (binding == null)
            return false;

        Bindings.Remove(binding);
        ModifiedTime = DateTime.Now;
        return true;
    }
}
