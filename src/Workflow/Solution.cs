using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.SDK.Logging;
using SunEyeVision.Plugin.SDK.Models;
using SunEyeVision.Core.Services.Serialization;

namespace SunEyeVision.Workflow;

/// <summary>
/// 解决方案（纯数据模型，元数据已分离到 SolutionMetadata）
/// </summary>
/// <remarks>
/// 架构改进（2026-03-18）：
/// - 元数据（Name、Description、times）已分离到 SolutionMetadata
/// - Solution 只包含实际数据（Workflows、NodeParameters、GlobalVariables等）
/// - 通过 ID 与 SolutionMetadata 关联
/// - FilePath 为运行时属性，不序列化到文件
/// 
/// 设计原则（rule-005）：
/// - 最优和最合理：直接序列化，不需要转换层
/// - JSON格式直观：使用 System.Text.Json 多态序列化
/// - 单一职责：Solution 只负责存储实际数据
/// 
/// 完全对齐VisionMaster架构，一个解决方案包含：
/// - 工作流列表：执行逻辑定义
/// - 节点参数：所有节点的工具参数配置（替代Recipe/RecipeGroup概念）
/// - 全局变量：跨工作流共享的变量
/// - 设备配置：相机、光源等设备配置
/// - 通讯配置：PLC、串口等通讯配置
/// - 数据库配置：数据存储配置
/// - 执行策略：工作流执行策略
/// </remarks>
public class Solution : ObservableObject
{
    /// <summary>
    /// 解决方案ID（不可变，与 SolutionMetadata 关联）
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 解决方案版本
    /// </summary>
    public string Version { get; set; } = "1.0";

    /// <summary>
    /// 解决方案文件完整路径（运行时属性，不序列化）
    /// </summary>
    [JsonIgnore]
    public string? FilePath { get; set; }

    /// <summary>
    /// 工作流列表
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<Workflow> Workflows { get; set; } = new();

    /// <summary>
    /// 节点参数映射：NodeId -> ToolParameters
    /// </summary>
    /// <remarks>
    /// 存储所有节点的工具参数配置，替代原Recipe/RecipeGroup结构。
    /// 一个解决方案只有一套参数配置。
    /// ToolParameters 使用 JsonPolymorphic 支持多态序列化。
    /// </remarks>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, ToolParameters> NodeParameters { get; set; } = new();

    /// <summary>
    /// 全局变量列表
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<GlobalVariable> GlobalVariables { get; set; } = new();

    /// <summary>
    /// 配方列表
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<Recipe> Recipes { get; set; } = new();

    /// <summary>
    /// 默认配方ID
    /// </summary>
    public string? DefaultRecipeId { get; set; }

    /// <summary>
    /// 当前激活的配方ID（运行时属性，不序列化）
    /// </summary>
    [JsonIgnore]
    public string? CurrentRecipeId { get; set; }

    /// <summary>
    /// 设备列表
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<Device> Devices { get; set; } = new();

    /// <summary>
    /// 通讯配置列表
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<Communication> Communications { get; set; } = new();

    /// <summary>
    /// 数据库配置
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DatabaseConfiguration? DatabaseConfiguration { get; set; }

    /// <summary>
    /// 执行策略
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ExecutionStrategy? ExecutionStrategy { get; set; }

    /// <summary>
    /// 版本历史
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<SolutionVersion> VersionHistory { get; set; } = new();

    /// <summary>
    /// 保存到文件
    /// </summary>
    public void Save(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            throw new ArgumentException("文件路径不能为空");

        FilePath = filePath;

        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            VisionLogger.Instance.Log(LogLevel.Info, $"创建目录: {directory}", "Solution");
            Directory.CreateDirectory(directory);
        }

        VisionLogger.Instance.Log(LogLevel.Info, $"开始序列化解决方案: Id={Id}", "Solution");
        var json = JsonSerializer.Serialize(this, JsonSerializationOptions.Default);
        VisionLogger.Instance.Log(LogLevel.Info, $"序列化完成, JSON长度: {json.Length} 字符", "Solution");

        File.WriteAllText(filePath, json);

        var fileInfo = new FileInfo(filePath);
        VisionLogger.Instance.Log(LogLevel.Success, $"保存解决方案: Id={Id} -> {filePath}, 文件大小: {fileInfo.Length} 字节", "Solution");
    }

    /// <summary>
    /// 从文件加载
    /// </summary>
    public static Solution? Load(string filePath)
    {
        if (!File.Exists(filePath))
            return null;

        try
        {
            var json = File.ReadAllText(filePath);
            var solution = JsonSerializer.Deserialize<Solution>(json, JsonSerializationOptions.Default);
            if (solution != null)
            {
                solution.FilePath = filePath;
                VisionLogger.Instance.Log(LogLevel.Info, $"加载解决方案: Id={solution.Id} -> {filePath}", "Solution");
            }

            return solution;
        }
        catch (Exception ex)
        {
            VisionLogger.Instance.Log(LogLevel.Error, $"加载解决方案失败: {filePath}, 错误: {ex.Message}", "Solution", ex);
            return null;
        }
    }

    /// <summary>
    /// 创建新解决方案
    /// </summary>
    /// <remarks>
    /// 自动创建默认配方，确保参数有存储位置。
    /// 
    /// 设计原则（rule-002）：
    /// - 方法命名使用 Create()，与 RunContext.Create、SolutionMetadata.Create 保持一致
    /// </remarks>
    public static Solution Create()
    {
        var solution = new Solution
        {
            Id = Guid.NewGuid().ToString(),
            Version = "1.0",
            Workflows = new List<Workflow>(),
            NodeParameters = new Dictionary<string, ToolParameters>(),
            GlobalVariables = new List<GlobalVariable>(),
            Recipes = new List<Recipe>(),
            Devices = new List<Device>(),
            Communications = new List<Communication>(),
            DatabaseConfiguration = new DatabaseConfiguration(),
            ExecutionStrategy = new ExecutionStrategy(),
            VersionHistory = new List<SolutionVersion>()
        };

        // 自动创建默认配方
        var defaultRecipe = new Recipe
        {
            Name = "默认配方",
            Description = "默认参数配置",
            IsDefault = true,
            CreatedTime = DateTime.Now,
            LastModifiedTime = DateTime.Now
        };

        solution.Recipes.Add(defaultRecipe);
        solution.DefaultRecipeId = defaultRecipe.Id;
        solution.CurrentRecipeId = defaultRecipe.Id;

        VisionLogger.Instance.Log(LogLevel.Info, $"创建解决方案: Id={solution.Id}, 自动创建默认配方: {defaultRecipe.Name}", "Solution");

        return solution;
    }

    /// <summary>
    /// 添加工作流
    /// </summary>
    public Workflow AddWorkflow(string name)
    {
        var workflow = new Workflow
        {
            Id = Guid.NewGuid().ToString(),
            Name = name
        };

        Workflows.Add(workflow);
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
        return true;
    }

    /// <summary>
    /// 保存节点参数
    /// </summary>
    public void SaveNodeParameters(string nodeId, ToolParameters parameters)
    {
        if (string.IsNullOrEmpty(nodeId))
            throw new ArgumentException("节点ID不能为空", nameof(nodeId));

        if (parameters == null)
            throw new ArgumentNullException(nameof(parameters));

        NodeParameters[nodeId] = parameters.Clone();
    }

    /// <summary>
    /// 获取节点参数
    /// </summary>
    public ToolParameters? GetNodeParameters(string nodeId)
    {
        if (!NodeParameters.TryGetValue(nodeId, out var parameters))
            return null;

        return parameters.Clone();
    }

    /// <summary>
    /// 移除节点参数
    /// </summary>
    public bool RemoveNodeParameters(string nodeId)
    {
        return NodeParameters.Remove(nodeId);
    }

    /// <summary>
    /// 添加全局变量
    /// </summary>
    public GlobalVariable AddGlobalVariable(string name, object? value, string type = "String")
    {
        var globalVariable = new GlobalVariable(name, value, type);
        GlobalVariables.Add(globalVariable);
        return globalVariable;
    }

    /// <summary>
    /// 获取全局变量
    /// </summary>
    public GlobalVariable? GetGlobalVariable(string name)
    {
        return GlobalVariables.FirstOrDefault(v => v.Name == name);
    }

    /// <summary>
    /// 移除全局变量
    /// </summary>
    public bool RemoveGlobalVariable(string name)
    {
        var variable = GetGlobalVariable(name);
        if (variable == null)
            return false;

        return GlobalVariables.Remove(variable);
    }

    /// <summary>
    /// 添加设备
    /// </summary>
    public Device AddDevice(string name, DeviceType type)
    {
        var device = new Device(name, type);
        Devices.Add(device);
        return device;
    }

    /// <summary>
    /// 获取设备
    /// </summary>
    public Device? GetDevice(string deviceId)
    {
        return Devices.FirstOrDefault(d => d.Id == deviceId);
    }

    /// <summary>
    /// 移除设备
    /// </summary>
    public bool RemoveDevice(string deviceId)
    {
        var device = GetDevice(deviceId);
        if (device == null)
            return false;

        return Devices.Remove(device);
    }

    /// <summary>
    /// 添加通讯配置
    /// </summary>
    public Communication AddCommunication(string name, CommunicationType type)
    {
        var communication = new Communication(name, type);
        Communications.Add(communication);
        return communication;
    }

    /// <summary>
    /// 获取通讯配置
    /// </summary>
    public Communication? GetCommunication(string communicationId)
    {
        return Communications.FirstOrDefault(c => c.Id == communicationId);
    }

    /// <summary>
    /// 移除通讯配置
    /// </summary>
    public bool RemoveCommunication(string communicationId)
    {
        var communication = GetCommunication(communicationId);
        if (communication == null)
            return false;

        return Communications.Remove(communication);
    }

    /// <summary>
    /// 添加版本历史
    /// </summary>
    public SolutionVersion AddVersion(string description, List<string>? changes = null, string author = "")
    {
        var version = new SolutionVersion(Version, description, changes)
        {
            Author = author
        };
        VersionHistory.Insert(0, version);
        return version;
    }

    /// <summary>
    /// 克隆解决方案
    /// </summary>
    public Solution Clone()
    {
        return new Solution
        {
            Id = Guid.NewGuid().ToString(),
            Version = Version,
            Workflows = Workflows.Select(w => w.Clone()).ToList(),
            NodeParameters = new Dictionary<string, ToolParameters>(
                NodeParameters.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Clone()
                )
            ),
            GlobalVariables = GlobalVariables.Select(v => v.Clone()).ToList(),
            Recipes = Recipes.Select(r => r.Clone()).ToList(),
            DefaultRecipeId = DefaultRecipeId,
            CurrentRecipeId = null, // 运行时不克隆当前配方ID
            Devices = Devices.Select(d => d.Clone()).ToList(),
            Communications = Communications.Select(c => c.Clone()).ToList(),
            DatabaseConfiguration = DatabaseConfiguration?.Clone(),
            ExecutionStrategy = ExecutionStrategy?.Clone(),
            VersionHistory = new List<SolutionVersion>(VersionHistory)
        };
    }

    /// <summary>
    /// 验证解决方案
    /// </summary>
    public (bool IsValid, List<string> Errors) Validate()
    {
        var errors = new List<string>();

        // 验证工作流
        foreach (var workflow in Workflows)
        {
            var (isValid, workflowErrors) = workflow.Validate();
            if (!isValid)
            {
                errors.AddRange(workflowErrors.Select(e => $"工作流 '{workflow.Name}': {e}"));
            }
        }

        // 验证全局变量
        foreach (var globalVariable in GlobalVariables)
        {
            var (isValid, varErrors) = globalVariable.Validate();
            if (!isValid)
            {
                errors.AddRange(varErrors.Select(e => $"全局变量 '{globalVariable.Name}': {e}"));
            }
        }

        // 验证配方
        foreach (var recipe in Recipes)
        {
            var (isValid, recipeErrors) = recipe.Validate();
            if (!isValid)
            {
                errors.AddRange(recipeErrors.Select(e => $"配方 '{recipe.Name}': {e}"));
            }
        }

        // 验证默认配方
        if (Recipes.Count > 0 && !Recipes.Any(r => r.IsDefault))
        {
            errors.Add("存在配方但未设置默认配方");
        }

        // 验证设备
        foreach (var device in Devices)
        {
            var (isValid, deviceErrors) = device.Validate();
            if (!isValid)
            {
                errors.AddRange(deviceErrors.Select(e => $"设备 '{device.Name}': {e}"));
            }
        }

        // 验证通讯配置
        foreach (var communication in Communications)
        {
            var (isValid, commErrors) = communication.Validate();
            if (!isValid)
            {
                errors.AddRange(commErrors.Select(e => $"通讯 '{communication.Name}': {e}"));
            }
        }

        // 验证数据库配置
        if (DatabaseConfiguration != null)
        {
            var (isValid, dbErrors) = DatabaseConfiguration.Validate();
            if (!isValid)
            {
                errors.AddRange(dbErrors.Select(e => $"数据库配置: {e}"));
            }
        }

        // 验证执行策略
        if (ExecutionStrategy != null)
        {
            var (isValid, strategyErrors) = ExecutionStrategy.Validate();
            if (!isValid)
            {
                errors.AddRange(strategyErrors.Select(e => $"执行策略: {e}"));
            }
        }

        return (errors.Count == 0, errors);
    }
}
