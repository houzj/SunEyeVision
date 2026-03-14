using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SunEyeVision.Plugin.SDK.Logging;

namespace SunEyeVision.Workflow;

/// <summary>
/// 解决方案管理器
/// </summary>
/// <remarks>
/// 统一管理解决方案的加载、保存、创建、删除等操作。
/// </remarks>
public class SolutionManager
{
    private readonly string _solutionsDirectory;
    private readonly string _configFilePath;
    private readonly ILogger _logger;

    /// <summary>
    /// 当前解决方案
    /// </summary>
    public SolutionFile? CurrentSolution { get; private set; }

    /// <summary>
    /// 当前文件路径
    /// </summary>
    public string? CurrentFilePath { get; private set; }

    /// <summary>
    /// 运行时配置
    /// </summary>
    public RuntimeConfig RuntimeConfig { get; }

    /// <summary>
    /// 最近打开的解决方案列表
    /// </summary>
    public List<RecentSolutionInfo> RecentSolutions { get; }

    /// <summary>
    /// 解决方案打开事件
    /// </summary>
    public event EventHandler<SolutionFile>? SolutionOpened;

    /// <summary>
    /// 解决方案保存事件
    /// </summary>
    public event EventHandler<SolutionFile>? SolutionSaved;

    /// <summary>
    /// 解决方案关闭事件
    /// </summary>
    public event EventHandler? SolutionClosed;

    /// <summary>
    /// 工作流添加事件
    /// </summary>
    public event EventHandler<Workflow>? WorkflowAdded;

    /// <summary>
    /// 工作流移除事件
    /// </summary>
    public event EventHandler<Workflow>? WorkflowRemoved;

    /// <summary>
    /// 配方添加事件
    /// </summary>
    public event EventHandler<Recipe>? RecipeAdded;

    /// <summary>
    /// 配方移除事件
    /// </summary>
    public event EventHandler<Recipe>? RecipeRemoved;

    /// <summary>
    /// 设备绑定切换事件
    /// </summary>
    public event EventHandler<DeviceBinding>? BindingSwitched;

    public SolutionManager(string solutionsDirectory)
    {
        _solutionsDirectory = solutionsDirectory;
        _configFilePath = Path.Combine(solutionsDirectory, "solution_config.json");
        _logger = VisionLogger.Instance;
        RuntimeConfig = new RuntimeConfig();
        RecentSolutions = new List<RecentSolutionInfo>();

        Directory.CreateDirectory(solutionsDirectory);
        LoadConfig();
    }

    /// <summary>
    /// 创建新解决方案
    /// </summary>
    public SolutionFile CreateNewSolution(string name, string description = "")
    {
        CloseSolution();

        CurrentSolution = SolutionFile.CreateNew(name);
        CurrentSolution.Description = description;

        // 添加默认工作流和配方
        var defaultWorkflow = CurrentSolution.AddWorkflow("默认工作流", "默认检测流程");
        var defaultRecipe = CurrentSolution.AddRecipe("默认配方", defaultWorkflow.Id, "默认产品配置");

        CurrentFilePath = null;

        _logger.Log(LogLevel.Success, $"创建新解决方案: {name}", "SolutionManager");
        return CurrentSolution;
    }

    /// <summary>
    /// 打开解决方案
    /// </summary>
    public SolutionFile? OpenSolution(string filePath)
    {
        if (!File.Exists(filePath))
        {
            _logger.Log(LogLevel.Error, $"解决方案文件不存在: {filePath}", "SolutionManager");
            return null;
        }

        var solution = SolutionFile.Load(filePath);
        if (solution == null)
        {
            _logger.Log(LogLevel.Error, $"加载解决方案失败: {filePath}", "SolutionManager");
            return null;
        }

        CloseSolution();
        CurrentSolution = solution;
        CurrentFilePath = filePath;

        AddToRecent(filePath, solution.Name);

        SolutionOpened?.Invoke(this, solution);
        _logger.Log(LogLevel.Success, $"打开解决方案: {solution.Name}", "SolutionManager");
        return CurrentSolution;
    }

    /// <summary>
    /// 保存解决方案
    /// </summary>
    public bool SaveSolution(string? filePath = null)
    {
        if (CurrentSolution == null)
        {
            _logger.Log(LogLevel.Warning, "没有可保存的解决方案", "SolutionManager");
            return false;
        }

        var savePath = filePath ?? CurrentFilePath;
        if (string.IsNullOrEmpty(savePath))
        {
            _logger.Log(LogLevel.Warning, "未指定保存路径", "SolutionManager");
            return false;
        }

        try
        {
            CurrentSolution.Save(savePath);
            CurrentFilePath = savePath;
            AddToRecent(savePath, CurrentSolution.Name);
            SolutionSaved?.Invoke(this, CurrentSolution);
            return true;
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Error, $"保存解决方案失败: {ex.Message}", "SolutionManager", ex);
            return false;
        }
    }

    /// <summary>
    /// 另存为解决方案
    /// </summary>
    public bool SaveAsSolution(string filePath)
    {
        if (CurrentSolution == null)
        {
            _logger.Log(LogLevel.Warning, "没有可保存的解决方案", "SolutionManager");
            return false;
        }

        if (string.IsNullOrEmpty(filePath))
        {
            _logger.Log(LogLevel.Warning, "未指定保存路径", "SolutionManager");
            return false;
        }

        try
        {
            CurrentSolution.Save(filePath);
            CurrentFilePath = filePath;
            AddToRecent(filePath, CurrentSolution.Name);
            SolutionSaved?.Invoke(this, CurrentSolution);
            return true;
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Error, $"另存为解决方案失败: {ex.Message}", "SolutionManager", ex);
            return false;
        }
    }

    /// <summary>
    /// 关闭当前解决方案
    /// </summary>
    public void CloseSolution()
    {
        if (CurrentSolution != null && !string.IsNullOrEmpty(CurrentFilePath))
        {
            SaveSolution();
        }

        CurrentSolution = null;
        CurrentFilePath = null;
        SolutionClosed?.Invoke(this, EventArgs.Empty);
        _logger.Log(LogLevel.Info, "关闭解决方案", "SolutionManager");
    }

    /// <summary>
    /// 添加工作流
    /// </summary>
    public Workflow? AddWorkflow(string name, string description = "")
    {
        if (CurrentSolution == null)
        {
            _logger.Log(LogLevel.Warning, "没有打开的解决方案", "SolutionManager");
            return null;
        }

        var workflow = CurrentSolution.AddWorkflow(name, description);
        WorkflowAdded?.Invoke(this, workflow);
        return workflow;
    }

    /// <summary>
    /// 移除工作流
    /// </summary>
    public bool RemoveWorkflow(string workflowId)
    {
        if (CurrentSolution == null)
        {
            _logger.Log(LogLevel.Warning, "没有打开的解决方案", "SolutionManager");
            return false;
        }

        var workflow = CurrentSolution.GetWorkflow(workflowId);
        if (workflow == null)
        {
            _logger.Log(LogLevel.Warning, $"工作流不存在: {workflowId}", "SolutionManager");
            return false;
        }

        if (CurrentSolution.RemoveWorkflow(workflowId))
        {
            WorkflowRemoved?.Invoke(this, workflow);
            return true;
        }

        return false;
    }

    /// <summary>
    /// 添加配方
    /// </summary>
    public Recipe? AddRecipe(string name, string workflowRef, string description = "")
    {
        if (CurrentSolution == null)
        {
            _logger.Log(LogLevel.Warning, "没有打开的解决方案", "SolutionManager");
            return null;
        }

        var recipe = CurrentSolution.AddRecipe(name, workflowRef, description);
        RecipeAdded?.Invoke(this, recipe);
        return recipe;
    }

    /// <summary>
    /// 移除配方
    /// </summary>
    public bool RemoveRecipe(string recipeId)
    {
        if (CurrentSolution == null)
        {
            _logger.Log(LogLevel.Warning, "没有打开的解决方案", "SolutionManager");
            return false;
        }

        var recipe = CurrentSolution.GetRecipe(recipeId);
        if (recipe == null)
        {
            _logger.Log(LogLevel.Warning, $"配方不存在: {recipeId}", "SolutionManager");
            return false;
        }

        if (CurrentSolution.RemoveRecipe(recipeId))
        {
            RecipeRemoved?.Invoke(this, recipe);
            return true;
        }

        return false;
    }

    /// <summary>
    /// 添加设备绑定
    /// </summary>
    public DeviceBinding? AddBinding(string deviceName, string workflowRef, string recipeRef, string recipeGroupName = "default")
    {
        if (CurrentSolution == null)
        {
            _logger.Log(LogLevel.Warning, "没有打开的解决方案", "SolutionManager");
            return null;
        }

        var binding = CurrentSolution.AddBinding(deviceName, workflowRef, recipeRef, recipeGroupName);
        return binding;
    }

    /// <summary>
    /// 移除设备绑定
    /// </summary>
    public bool RemoveBinding(string deviceId)
    {
        if (CurrentSolution == null)
        {
            _logger.Log(LogLevel.Warning, "没有打开的解决方案", "SolutionManager");
            return false;
        }

        return CurrentSolution.RemoveBinding(deviceId);
    }

    /// <summary>
    /// 切换设备配置
    /// </summary>
    public bool SwitchDeviceBinding(string deviceId, string recipeGroupName = "default")
    {
        if (CurrentSolution == null)
            return false;

        var binding = CurrentSolution.GetBinding(deviceId);
        if (binding == null)
        {
            _logger.Log(LogLevel.Warning, $"设备绑定不存在: {deviceId}", "SolutionManager");
            return false;
        }

        binding.RecipeGroupName = recipeGroupName;
        binding.LastSwitchTime = DateTime.Now;
        RuntimeConfig.CurrentRecipe = recipeGroupName;

        BindingSwitched?.Invoke(this, binding);
        _logger.Log(LogLevel.Info, $"切换设备配置: {deviceId} -> {recipeGroupName}", "SolutionManager");
        return true;
    }

    /// <summary>
    /// 获取当前运行时上下文
    /// </summary>
    public RunContext? GetCurrentRunContext()
    {
        if (CurrentSolution == null || string.IsNullOrEmpty(CurrentSolution.CurrentDeviceId))
            return null;

        var binding = CurrentSolution.GetCurrentBinding();
        if (binding == null)
            return null;

        var workflow = CurrentSolution.GetCurrentWorkflow();
        var recipe = CurrentSolution.GetCurrentRecipe();

        if (workflow == null || recipe == null)
            return null;

        // 创建运行时上下文
        var recipeGroup = recipe.GetCurrentRecipeGroup(binding.RecipeGroupName);
        if (recipeGroup == null)
            return null;

        return new RunContext
        {
            Workflow = workflow,
            Recipe = recipe,
            RecipeGroup = recipeGroup
        };
    }

    /// <summary>
    /// 添加到最近打开列表
    /// </summary>
    private void AddToRecent(string filePath, string name)
    {
        // 移除已存在的记录
        RecentSolutions.RemoveAll(r => r.FilePath == filePath);

        // 添加到开头
        RecentSolutions.Insert(0, new RecentSolutionInfo
        {
            FilePath = filePath,
            Name = name,
            LastOpened = DateTime.Now
        });

        // 限制数量
        while (RecentSolutions.Count > 10)
        {
            RecentSolutions.RemoveAt(RecentSolutions.Count - 1);
        }

        SaveConfig();
    }

    /// <summary>
    /// 从最近打开列表移除
    /// </summary>
    public void RemoveFromRecent(string filePath)
    {
        RecentSolutions.RemoveAll(r => r.FilePath == filePath);
        SaveConfig();
    }

    /// <summary>
    /// 加载配置
    /// </summary>
    private void LoadConfig()
    {
        if (!File.Exists(_configFilePath))
            return;

        try
        {
            var json = File.ReadAllText(_configFilePath);
            var config = System.Text.Json.JsonSerializer.Deserialize<SolutionManagerConfig>(json);
            if (config != null)
            {
                RecentSolutions.Clear();
                RecentSolutions.AddRange(config.RecentSolutions ?? new List<RecentSolutionInfo>());

                if (!string.IsNullOrEmpty(config.CurrentDeviceId))
                {
                    RuntimeConfig.CurrentDevice = config.CurrentDeviceId;
                }

                if (!string.IsNullOrEmpty(config.CurrentRecipe))
                {
                    RuntimeConfig.CurrentRecipe = config.CurrentRecipe;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Error, $"加载配置失败: {ex.Message}", "SolutionManager", ex);
        }
    }

    /// <summary>
    /// 保存配置
    /// </summary>
    private void SaveConfig()
    {
        try
        {
            var config = new SolutionManagerConfig
            {
                RecentSolutions = RecentSolutions,
                CurrentDeviceId = RuntimeConfig.CurrentDevice,
                CurrentRecipe = RuntimeConfig.CurrentRecipe
            };

            var options = new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            };

            var json = System.Text.Json.JsonSerializer.Serialize(config, options);
            File.WriteAllText(_configFilePath, json);
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Error, $"保存配置失败: {ex.Message}", "SolutionManager", ex);
        }
    }
}

/// <summary>
/// 最近打开的解决方案信息
/// </summary>
public class RecentSolutionInfo
{
    /// <summary>
    /// 文件路径
    /// </summary>
    public string FilePath { get; set; } = "";

    /// <summary>
    /// 解决方案名称
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// 最后打开时间
    /// </summary>
    public DateTime LastOpened { get; set; } = DateTime.Now;
}

/// <summary>
/// 解决方案管理器配置
/// </summary>
internal class SolutionManagerConfig
{
    /// <summary>
    /// 最近打开的解决方案列表
    /// </summary>
    public List<RecentSolutionInfo>? RecentSolutions { get; set; }

    /// <summary>
    /// 当前设备ID
    /// </summary>
    public string? CurrentDeviceId { get; set; }

    /// <summary>
    /// 当前配方组名称
    /// </summary>
    public string? CurrentRecipe { get; set; }
}
