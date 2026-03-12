using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using SunEyeVision.Plugin.SDK.Logging;

namespace SunEyeVision.Workflow;

/// <summary>
/// 解决方案管理器
/// </summary>
/// <remarks>
/// 支持两种架构模式：
/// 1. 传统模式：Solution 包含 Workflow 和 Recipe（向后兼容）
/// 2. 解耦模式：WorkflowDefinition + DataConfiguration + RuntimeBinding（推荐）
/// 
/// 解耦模式优势：
/// - 执行流与数据流完全分离
/// - 支持快速产品切换（< 1秒）
/// - 支持批量升级（100台设备 < 5分钟）
/// - 支持配方组快速切换
/// </remarks>
public class SolutionManager
{
    private readonly string _solutionDirectory;
    private readonly ILogger _logger;
    private readonly Dictionary<string, Solution> _solutions;
    private SolutionConfigurationTable _configurationTable;

    // ========== 解耦架构：新增目录和缓存 ==========
    private readonly string _workflowsDirectory;
    private readonly string _dataConfigsDirectory;
    private readonly string _bindingsDirectory;

    private readonly Dictionary<string, WorkflowDefinition> _workflows;
    private readonly Dictionary<string, DataConfiguration> _dataConfigs;
    private readonly Dictionary<string, RuntimeBinding> _bindings;

    /// <summary>
    /// 解决方案管理器
    /// </summary>
    public SolutionManager(string solutionDirectory)
    {
        _solutionDirectory = solutionDirectory;
        _logger = VisionLogger.Instance;
        _solutions = new Dictionary<string, Solution>();
        _configurationTable = new SolutionConfigurationTable();

        // 初始化解耦架构目录
        _workflowsDirectory = Path.Combine(solutionDirectory, "workflows");
        _dataConfigsDirectory = Path.Combine(solutionDirectory, "data_configs");
        _bindingsDirectory = Path.Combine(solutionDirectory, "bindings");

        _workflows = new Dictionary<string, WorkflowDefinition>();
        _dataConfigs = new Dictionary<string, DataConfiguration>();
        _bindings = new Dictionary<string, RuntimeBinding>();

        LoadAllSolutions();
        LoadAllWorkflows();
        LoadAllDataConfigs();
        LoadAllBindings();
    }

    /// <summary>
    /// 创建新解决方案
    /// </summary>
    public Solution CreateSolution(string name, string description = "")
    {
        var solution = new Solution
        {
            Name = name,
            Description = description,
            CreatedAt = DateTime.Now,
            ModifiedAt = DateTime.Now
        };

        _solutions[solution.Id] = solution;
        SaveSolution(solution);

        _logger.Log(LogLevel.Info, $"创建解决方案: {name}", "SolutionManager");
        return solution;
    }

    /// <summary>
    /// 保存解决方案
    /// </summary>
    public void SaveSolution(Solution solution)
    {
        if (solution == null)
            throw new ArgumentNullException(nameof(solution));

        solution.ModifiedAt = DateTime.Now;

        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        var filePath = GetSolutionFilePath(solution.Id);
        var json = JsonSerializer.Serialize(solution, jsonOptions);
        File.WriteAllText(filePath, json);

        _solutions[solution.Id] = solution;
        _logger.Log(LogLevel.Success, $"保存解决方案: {solution.Name}", "SolutionManager");
    }

    /// <summary>
    /// 加载解决方案
    /// </summary>
    public Solution? LoadSolution(string solutionId)
    {
        if (_solutions.TryGetValue(solutionId, out var solution))
            return solution;

        var filePath = GetSolutionFilePath(solutionId);
        if (!File.Exists(filePath))
            return null;

        var json = File.ReadAllText(filePath);
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        solution = JsonSerializer.Deserialize<Solution>(json, jsonOptions);
        if (solution != null)
        {
            _solutions[solution.Id] = solution;
            _logger.Log(LogLevel.Info, $"加载解决方案: {solution.Name}", "SolutionManager");
        }

        return solution;
    }

    /// <summary>
    /// 删除解决方案
    /// </summary>
    public void DeleteSolution(string solutionId)
    {
        if (!_solutions.ContainsKey(solutionId))
            throw new ArgumentException($"解决方案 {solutionId} 不存在");

        var filePath = GetSolutionFilePath(solutionId);
        if (File.Exists(filePath))
            File.Delete(filePath);

        _solutions.Remove(solutionId);
        _logger.Log(LogLevel.Warning, $"删除解决方案: {solutionId}", "SolutionManager");
    }

    /// <summary>
    /// 加载所有解决方案
    /// </summary>
    public void LoadAllSolutions()
    {
        if (!Directory.Exists(_solutionDirectory))
            Directory.CreateDirectory(_solutionDirectory);

        var files = Directory.GetFiles(_solutionDirectory, "*.json");
        foreach (var file in files)
        {
            try
            {
                var json = File.ReadAllText(file);
                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var solution = JsonSerializer.Deserialize<Solution>(json, jsonOptions);
                if (solution != null && !string.IsNullOrEmpty(solution.Id))
                {
                    _solutions[solution.Id] = solution;
                }
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, $"加载解决方案失败: {file}, 错误: {ex.Message}", "SolutionManager", ex);
            }
        }

        _logger.Log(LogLevel.Info, $"加载了 {_solutions.Count} 个解决方案", "SolutionManager");
    }

    /// <summary>
    /// 获取所有解决方案
    /// </summary>
    public IReadOnlyList<Solution> GetAllSolutions()
    {
        return _solutions.Values.ToList();
    }

    /// <summary>
    /// 导入解决方案
    /// </summary>
    public Solution ImportSolution(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"文件不存在: {filePath}");

        var json = File.ReadAllText(filePath);
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var solution = JsonSerializer.Deserialize<Solution>(json, jsonOptions);
        if (solution == null)
            throw new InvalidOperationException("反序列化失败");

        // 生成新的ID避免冲突
        solution.Id = Guid.NewGuid().ToString();
        solution.CreatedAt = DateTime.Now;
        solution.ModifiedAt = DateTime.Now;

        SaveSolution(solution);
        _logger.Log(LogLevel.Success, $"导入解决方案: {solution.Name}", "SolutionManager");

        return solution;
    }

    /// <summary>
    /// 导出解决方案
    /// </summary>
    public void ExportSolution(string solutionId, string exportPath)
    {
        var solution = LoadSolution(solutionId);
        if (solution == null)
            throw new ArgumentException($"解决方案 {solutionId} 不存在");

        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        var json = JsonSerializer.Serialize(solution, jsonOptions);
        File.WriteAllText(exportPath, json);

        _logger.Log(LogLevel.Success, $"导出解决方案: {solution.Name} 到 {exportPath}", "SolutionManager");
    }

    /// <summary>
    /// 绑定产品ID到解决方案ID
    /// </summary>
    public void BindProductToSolution(string productId, string solutionId)
    {
        _configurationTable.SetMapping(productId, solutionId);
        _logger.Log(LogLevel.Info, $"绑定产品 {productId} 到解决方案 {solutionId}", "SolutionManager");
    }

    /// <summary>
    /// 根据产品ID获取解决方案ID
    /// </summary>
    public string? GetSolutionIdByProduct(string productId)
    {
        return _configurationTable.GetSolutionId(productId);
    }

    /// <summary>
    /// 根据产品ID加载解决方案
    /// </summary>
    public Solution? LoadSolutionByProduct(string productId)
    {
        var solutionId = _configurationTable.GetSolutionId(productId);
        if (string.IsNullOrEmpty(solutionId))
            return null;

        return LoadSolution(solutionId);
    }

    /// <summary>
    /// 获取解决方案文件路径
    /// </summary>
    private string GetSolutionFilePath(string solutionId)
    {
        return Path.Combine(_solutionDirectory, $"{solutionId}.json");
    }

    // ====================================================================
    // 解耦架构：WorkflowDefinition 管理
    // ====================================================================

    /// <summary>
    /// 创建工作流定义
    /// </summary>
    /// <param name="name">工作流名称</param>
    /// <param name="version">版本号</param>
    /// <param name="category">分类（可选）</param>
    /// <returns>工作流定义实例</returns>
    public WorkflowDefinition CreateWorkflow(string name, string version = "1.0.0", string? category = null)
    {
        var workflow = new WorkflowDefinition
        {
            Name = name,
            Version = version,
            Category = category ?? string.Empty,
            CreatedTime = DateTime.Now,
            ModifiedTime = DateTime.Now
        };

        _workflows[workflow.Id] = workflow;
        SaveWorkflow(workflow);

        _logger.Log(LogLevel.Success, $"创建工作流定义: {name} v{version}", "SolutionManager");
        return workflow;
    }

    /// <summary>
    /// 保存工作流定义
    /// </summary>
    public void SaveWorkflow(WorkflowDefinition workflow)
    {
        if (workflow == null)
            throw new ArgumentNullException(nameof(workflow));

        workflow.ModifiedTime = DateTime.Now;

        var filePath = GetWorkflowFilePath(workflow.Id);
        SaveToFile(workflow, filePath);

        _workflows[workflow.Id] = workflow;
        _logger.Log(LogLevel.Success, $"保存工作流定义: {workflow.Name}", "SolutionManager");
    }

    /// <summary>
    /// 加载工作流定义
    /// </summary>
    public WorkflowDefinition? LoadWorkflow(string refOrId)
    {
        // 尝试从缓存获取
        if (_workflows.TryGetValue(refOrId, out var workflow))
            return workflow;

        // 尝试解析引用路径
        var filePath = ResolveWorkflowPath(refOrId);
        if (filePath == null || !File.Exists(filePath))
            return null;

        workflow = LoadFromFile<WorkflowDefinition>(filePath);
        if (workflow != null)
        {
            _workflows[workflow.Id] = workflow;
            _logger.Log(LogLevel.Info, $"加载工作流定义: {workflow.Name}", "SolutionManager");
        }

        return workflow;
    }

    /// <summary>
    /// 删除工作流定义
    /// </summary>
    public void DeleteWorkflow(string workflowId)
    {
        if (!_workflows.ContainsKey(workflowId))
            throw new ArgumentException($"工作流定义 {workflowId} 不存在");

        var filePath = GetWorkflowFilePath(workflowId);
        if (File.Exists(filePath))
            File.Delete(filePath);

        _workflows.Remove(workflowId);
        _logger.Log(LogLevel.Warning, $"删除工作流定义: {workflowId}", "SolutionManager");
    }

    /// <summary>
    /// 获取所有工作流定义
    /// </summary>
    public IReadOnlyList<WorkflowDefinition> GetAllWorkflows()
    {
        return _workflows.Values.ToList();
    }

    /// <summary>
    /// 按分类获取工作流定义
    /// </summary>
    public IReadOnlyList<WorkflowDefinition> GetWorkflowsByCategory(string category)
    {
        return _workflows.Values.Where(w => w.Category == category).ToList();
    }

    // ====================================================================
    // 解耦架构：DataConfiguration 管理
    // ====================================================================

    /// <summary>
    /// 创建数据配置
    /// </summary>
    /// <param name="name">配置名称</param>
    /// <param name="productCode">产品编码（可选）</param>
    /// <param name="description">描述（可选）</param>
    /// <returns>数据配置实例</returns>
    public DataConfiguration CreateDataConfig(string name, string? productCode = null, string? description = null)
    {
        var config = new DataConfiguration
        {
            Name = name,
            ProductCode = productCode ?? string.Empty,
            Description = description ?? string.Empty,
            CreatedTime = DateTime.Now,
            ModifiedTime = DateTime.Now
        };

        _dataConfigs[config.Id] = config;
        SaveDataConfig(config);

        _logger.Log(LogLevel.Success, $"创建数据配置: {name}", "SolutionManager");
        return config;
    }

    /// <summary>
    /// 保存数据配置
    /// </summary>
    public void SaveDataConfig(DataConfiguration config)
    {
        if (config == null)
            throw new ArgumentNullException(nameof(config));

        config.ModifiedTime = DateTime.Now;

        var filePath = GetDataConfigFilePath(config.Id);
        SaveToFile(config, filePath);

        _dataConfigs[config.Id] = config;
        _logger.Log(LogLevel.Success, $"保存数据配置: {config.Name}", "SolutionManager");
    }

    /// <summary>
    /// 加载数据配置
    /// </summary>
    public DataConfiguration? LoadDataConfig(string refOrId)
    {
        // 尝试从缓存获取
        if (_dataConfigs.TryGetValue(refOrId, out var config))
            return config;

        // 尝试解析引用路径
        var filePath = ResolveDataConfigPath(refOrId);
        if (filePath == null || !File.Exists(filePath))
            return null;

        config = LoadFromFile<DataConfiguration>(filePath);
        if (config != null)
        {
            _dataConfigs[config.Id] = config;
            _logger.Log(LogLevel.Info, $"加载数据配置: {config.Name}", "SolutionManager");
        }

        return config;
    }

    /// <summary>
    /// 删除数据配置
    /// </summary>
    public void DeleteDataConfig(string configId)
    {
        if (!_dataConfigs.ContainsKey(configId))
            throw new ArgumentException($"数据配置 {configId} 不存在");

        var filePath = GetDataConfigFilePath(configId);
        if (File.Exists(filePath))
            File.Delete(filePath);

        _dataConfigs.Remove(configId);
        _logger.Log(LogLevel.Warning, $"删除数据配置: {configId}", "SolutionManager");
    }

    /// <summary>
    /// 获取所有数据配置
    /// </summary>
    public IReadOnlyList<DataConfiguration> GetAllDataConfigs()
    {
        return _dataConfigs.Values.ToList();
    }

    /// <summary>
    /// 按产品编码获取数据配置
    /// </summary>
    public DataConfiguration? GetDataConfigByProductCode(string productCode)
    {
        return _dataConfigs.Values.FirstOrDefault(c => c.ProductCode == productCode);
    }

    // ====================================================================
    // 解耦架构：RuntimeBinding 管理
    // ====================================================================

    /// <summary>
    /// 创建运行时绑定
    /// </summary>
    /// <param name="deviceId">设备ID</param>
    /// <param name="deviceName">设备名称</param>
    /// <param name="workflowRef">工作流引用</param>
    /// <param name="dataConfigRef">数据配置引用</param>
    /// <param name="recipeGroup">配方组名称</param>
    /// <returns>运行时绑定实例</returns>
    public RuntimeBinding CreateBinding(
        string deviceId,
        string deviceName,
        string workflowRef,
        string dataConfigRef,
        string recipeGroup = "default")
    {
        var binding = new RuntimeBinding
        {
            DeviceId = deviceId,
            DeviceName = deviceName,
            WorkflowRef = workflowRef,
            DataConfigRef = dataConfigRef,
            RecipeGroup = recipeGroup,
            CreatedTime = DateTime.Now,
            LastSwitchTime = DateTime.Now,
            Statistics = new BindingStatistics()
        };

        _bindings[deviceId] = binding;
        SaveBinding(binding);

        _logger.Log(LogLevel.Success,
            $"创建绑定: {deviceName} -> 工作流:{workflowRef} + 数据配置:{dataConfigRef}",
            "SolutionManager");

        return binding;
    }

    /// <summary>
    /// 保存运行时绑定
    /// </summary>
    public void SaveBinding(RuntimeBinding binding)
    {
        if (binding == null)
            throw new ArgumentNullException(nameof(binding));

        binding.LastSwitchTime = DateTime.Now;

        var filePath = GetBindingFilePath(binding.DeviceId);
        SaveToFile(binding, filePath);

        _bindings[binding.DeviceId] = binding;
        _logger.Log(LogLevel.Info, $"保存绑定: {binding.DeviceName}", "SolutionManager");
    }

    /// <summary>
    /// 加载运行时绑定
    /// </summary>
    public RuntimeBinding? LoadBinding(string deviceId)
    {
        // 尝试从缓存获取
        if (_bindings.TryGetValue(deviceId, out var binding))
            return binding;

        var filePath = GetBindingFilePath(deviceId);
        if (!File.Exists(filePath))
            return null;

        binding = LoadFromFile<RuntimeBinding>(filePath);
        if (binding != null)
        {
            _bindings[binding.DeviceId] = binding;
        }

        return binding;
    }

    /// <summary>
    /// 删除运行时绑定
    /// </summary>
    public void DeleteBinding(string deviceId)
    {
        if (!_bindings.ContainsKey(deviceId))
            throw new ArgumentException($"设备绑定 {deviceId} 不存在");

        var filePath = GetBindingFilePath(deviceId);
        if (File.Exists(filePath))
            File.Delete(filePath);

        _bindings.Remove(deviceId);
        _logger.Log(LogLevel.Warning, $"删除设备绑定: {deviceId}", "SolutionManager");
    }

    /// <summary>
    /// 获取所有运行时绑定
    /// </summary>
    public IReadOnlyList<RuntimeBinding> GetAllBindings()
    {
        return _bindings.Values.ToList();
    }

    // ====================================================================
    // 解耦架构：RuntimeContext 加载
    // ====================================================================

    /// <summary>
    /// 加载运行时上下文
    /// </summary>
    /// <param name="deviceId">设备ID</param>
    /// <returns>运行时上下文实例</returns>
    public RuntimeContext LoadRuntimeContext(string deviceId)
    {
        if (!_bindings.TryGetValue(deviceId, out var binding))
            throw new ArgumentException($"设备绑定不存在: {deviceId}");

        // 加载工作流
        var workflow = LoadWorkflow(binding.WorkflowRef);
        if (workflow == null)
            throw new FileNotFoundException($"工作流不存在: {binding.WorkflowRef}");

        // 加载数据配置
        var dataConfig = LoadDataConfig(binding.DataConfigRef);
        if (dataConfig == null)
            throw new FileNotFoundException($"数据配置不存在: {binding.DataConfigRef}");

        var context = new RuntimeContext
        {
            Workflow = workflow,
            DataConfig = dataConfig,
            Binding = binding,
            CurrentRecipeGroup = binding.RecipeGroup
        };

        _logger.Log(LogLevel.Info,
            $"加载运行时上下文: {binding.DeviceName} -> {workflow.Name} + {dataConfig.Name}",
            "SolutionManager");

        return context;
    }

    /// <summary>
    /// 验证运行时上下文
    /// </summary>
    public bool ValidateRuntimeContext(string deviceId)
    {
        try
        {
            var context = LoadRuntimeContext(deviceId);
            return context.Validate();
        }
        catch
        {
            return false;
        }
    }

    // ====================================================================
    // 解耦架构：快速切换方法
    // ====================================================================

    /// <summary>
    /// 切换产品（只修改绑定引用）
    /// </summary>
    /// <param name="deviceId">设备ID</param>
    /// <param name="newDataConfigRef">新数据配置引用</param>
    /// <param name="recipeGroup">配方组名称（可选）</param>
    public void SwitchProduct(string deviceId, string newDataConfigRef, string? recipeGroup = null)
    {
        if (!_bindings.TryGetValue(deviceId, out var binding))
            throw new ArgumentException($"设备绑定不存在: {deviceId}");

        var oldConfigRef = binding.DataConfigRef;
        binding.SwitchProduct(newDataConfigRef, recipeGroup);
        SaveBinding(binding);

        _logger.Log(LogLevel.Info,
            $"设备 {binding.DeviceName} 切换产品: {oldConfigRef} -> {newDataConfigRef}",
            "SolutionManager");
    }

    /// <summary>
    /// 切换配方组
    /// </summary>
    /// <param name="deviceId">设备ID</param>
    /// <param name="recipeGroup">配方组名称</param>
    public void SwitchRecipeGroup(string deviceId, string recipeGroup)
    {
        if (!_bindings.TryGetValue(deviceId, out var binding))
            throw new ArgumentException($"设备绑定不存在: {deviceId}");

        var oldGroup = binding.RecipeGroup;
        binding.SwitchRecipeGroup(recipeGroup);
        SaveBinding(binding);

        _logger.Log(LogLevel.Info,
            $"设备 {binding.DeviceName} 切换配方组: {oldGroup} -> {recipeGroup}",
            "SolutionManager");
    }

    /// <summary>
    /// 升级工作流版本
    /// </summary>
    /// <param name="deviceId">设备ID</param>
    /// <param name="newWorkflowRef">新工作流引用</param>
    /// <param name="version">版本号（可选）</param>
    public void UpgradeWorkflow(string deviceId, string newWorkflowRef, string? version = null)
    {
        if (!_bindings.TryGetValue(deviceId, out var binding))
            throw new ArgumentException($"设备绑定不存在: {deviceId}");

        var oldRef = binding.WorkflowRef;
        binding.UpgradeWorkflow(newWorkflowRef, version);
        SaveBinding(binding);

        _logger.Log(LogLevel.Info,
            $"设备 {binding.DeviceName} 升级工作流: {oldRef} -> {newWorkflowRef}",
            "SolutionManager");
    }

    /// <summary>
    /// 批量升级工作流
    /// </summary>
    /// <param name="oldVersion">旧版本标识</param>
    /// <param name="newVersion">新版本标识</param>
    /// <returns>更新的设备数量</returns>
    public int BatchUpgradeWorkflow(string oldVersion, string newVersion)
    {
        var updatedCount = 0;
        foreach (var binding in _bindings.Values)
        {
            if (binding.WorkflowRef.Contains(oldVersion))
            {
                var newRef = binding.WorkflowRef.Replace(oldVersion, newVersion);
                binding.UpgradeWorkflow(newRef, newVersion);
                SaveBinding(binding);
                updatedCount++;
            }
        }

        _logger.Log(LogLevel.Success,
            $"批量升级完成: {updatedCount} 个设备从 {oldVersion} 升级到 {newVersion}",
            "SolutionManager");

        return updatedCount;
    }

    /// <summary>
    /// 批量切换产品
    /// </summary>
    /// <param name="deviceIds">设备ID列表</param>
    /// <param name="newDataConfigRef">新数据配置引用</param>
    /// <param name="recipeGroup">配方组名称（可选）</param>
    /// <returns>成功的设备数量</returns>
    public int BatchSwitchProduct(IEnumerable<string> deviceIds, string newDataConfigRef, string? recipeGroup = null)
    {
        var successCount = 0;
        foreach (var deviceId in deviceIds)
        {
            try
            {
                SwitchProduct(deviceId, newDataConfigRef, recipeGroup);
                successCount++;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, $"切换产品失败: {deviceId}, 错误: {ex.Message}", "SolutionManager");
            }
        }

        _logger.Log(LogLevel.Success,
            $"批量切换产品完成: {successCount} 个设备切换到 {newDataConfigRef}",
            "SolutionManager");

        return successCount;
    }

    // ====================================================================
    // 解耦架构：迁移工具
    // ====================================================================

    /// <summary>
    /// 将旧 Solution 迁移到解耦架构
    /// </summary>
    /// <param name="solutionId">解决方案ID</param>
    /// <returns>迁移结果</returns>
    public MigrationResult MigrateSolution(string solutionId)
    {
        var solution = LoadSolution(solutionId);
        if (solution == null)
            throw new ArgumentException($"解决方案不存在: {solutionId}");

        var result = new MigrationResult
        {
            SolutionId = solutionId,
            SolutionName = solution.Name,
            Success = true
        };

        try
        {
            // 1. 创建 WorkflowDefinition
            var workflowDef = new WorkflowDefinition
            {
                Id = solution.Id + "_workflow",
                Name = solution.Name + " 工作流",
                Description = solution.Description,
                CreatedTime = solution.CreatedAt,
                ModifiedTime = DateTime.Now
            };

            _workflows[workflowDef.Id] = workflowDef;
            SaveWorkflow(workflowDef);
            result.WorkflowId = workflowDef.Id;

            // 2. 创建 DataConfiguration
            var dataConfig = DataConfiguration.FromSolution(solution);
            _dataConfigs[dataConfig.Id] = dataConfig;
            SaveDataConfig(dataConfig);
            result.DataConfigId = dataConfig.Id;

            // 3. 创建默认绑定
            var binding = new RuntimeBinding
            {
                DeviceId = solution.Id + "_default",
                DeviceName = solution.Name + " 默认设备",
                WorkflowRef = workflowDef.Id,
                DataConfigRef = dataConfig.Id,
                RecipeGroup = "default",
                CreatedTime = DateTime.Now,
                LastSwitchTime = DateTime.Now
            };

            _bindings[binding.DeviceId] = binding;
            SaveBinding(binding);
            result.BindingId = binding.DeviceId;

            _logger.Log(LogLevel.Success,
                $"迁移解决方案成功: {solution.Name} -> 工作流:{workflowDef.Id}, 数据配置:{dataConfig.Id}",
                "SolutionManager");
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            _logger.Log(LogLevel.Error, $"迁移解决方案失败: {solution.Name}, 错误: {ex.Message}", "SolutionManager", ex);
        }

        return result;
    }

    // ====================================================================
    // 解耦架构：文件路径和加载方法
    // ====================================================================

    private string GetWorkflowFilePath(string workflowId)
    {
        return Path.Combine(_workflowsDirectory, $"{workflowId}.json");
    }

    private string GetDataConfigFilePath(string configId)
    {
        return Path.Combine(_dataConfigsDirectory, $"{configId}.json");
    }

    private string GetBindingFilePath(string deviceId)
    {
        return Path.Combine(_bindingsDirectory, $"{deviceId}.json");
    }

    private string? ResolveWorkflowPath(string refOrId)
    {
        // 尝试直接作为ID解析
        var filePath = GetWorkflowFilePath(refOrId);
        if (File.Exists(filePath))
            return filePath;

        // 尝试作为相对路径解析（如 "standard_inspection/v1.0"）
        filePath = Path.Combine(_workflowsDirectory, refOrId.Replace('/', Path.DirectorySeparatorChar) + ".json");
        if (File.Exists(filePath))
            return filePath;

        return null;
    }

    private string? ResolveDataConfigPath(string refOrId)
    {
        // 尝试直接作为ID解析
        var filePath = GetDataConfigFilePath(refOrId);
        if (File.Exists(filePath))
            return filePath;

        // 尝试作为相对路径解析
        filePath = Path.Combine(_dataConfigsDirectory, refOrId.Replace('/', Path.DirectorySeparatorChar) + ".json");
        if (File.Exists(filePath))
            return filePath;

        return null;
    }

    private void LoadAllWorkflows()
    {
        if (!Directory.Exists(_workflowsDirectory))
        {
            Directory.CreateDirectory(_workflowsDirectory);
            return;
        }

        var files = Directory.GetFiles(_workflowsDirectory, "*.json", SearchOption.AllDirectories);
        foreach (var file in files)
        {
            try
            {
                var workflow = LoadFromFile<WorkflowDefinition>(file);
                if (workflow != null && !string.IsNullOrEmpty(workflow.Id))
                {
                    _workflows[workflow.Id] = workflow;
                }
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, $"加载工作流定义失败: {file}, 错误: {ex.Message}", "SolutionManager");
            }
        }

        _logger.Log(LogLevel.Info, $"加载了 {_workflows.Count} 个工作流定义", "SolutionManager");
    }

    private void LoadAllDataConfigs()
    {
        if (!Directory.Exists(_dataConfigsDirectory))
        {
            Directory.CreateDirectory(_dataConfigsDirectory);
            return;
        }

        var files = Directory.GetFiles(_dataConfigsDirectory, "*.json", SearchOption.AllDirectories);
        foreach (var file in files)
        {
            try
            {
                var config = LoadFromFile<DataConfiguration>(file);
                if (config != null && !string.IsNullOrEmpty(config.Id))
                {
                    _dataConfigs[config.Id] = config;
                }
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, $"加载数据配置失败: {file}, 错误: {ex.Message}", "SolutionManager");
            }
        }

        _logger.Log(LogLevel.Info, $"加载了 {_dataConfigs.Count} 个数据配置", "SolutionManager");
    }

    private void LoadAllBindings()
    {
        if (!Directory.Exists(_bindingsDirectory))
        {
            Directory.CreateDirectory(_bindingsDirectory);
            return;
        }

        var files = Directory.GetFiles(_bindingsDirectory, "*.json");
        foreach (var file in files)
        {
            try
            {
                var binding = LoadFromFile<RuntimeBinding>(file);
                if (binding != null && !string.IsNullOrEmpty(binding.DeviceId))
                {
                    _bindings[binding.DeviceId] = binding;
                }
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, $"加载设备绑定失败: {file}, 错误: {ex.Message}", "SolutionManager");
            }
        }

        _logger.Log(LogLevel.Info, $"加载了 {_bindings.Count} 个设备绑定", "SolutionManager");
    }

    private void SaveToFile<T>(T obj, string filePath)
    {
        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(obj, jsonOptions);
        File.WriteAllText(filePath, json);
    }

    private T? LoadFromFile<T>(string filePath) where T : class
    {
        if (!File.Exists(filePath))
            return null;

        var json = File.ReadAllText(filePath);
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        return JsonSerializer.Deserialize<T>(json, jsonOptions);
    }
}

/// <summary>
/// 迁移结果
/// </summary>
public class MigrationResult
{
    /// <summary>
    /// 原解决方案ID
    /// </summary>
    public string SolutionId { get; set; } = string.Empty;

    /// <summary>
    /// 原解决方案名称
    /// </summary>
    public string SolutionName { get; set; } = string.Empty;

    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 创建的工作流ID
    /// </summary>
    public string? WorkflowId { get; set; }

    /// <summary>
    /// 创建的数据配置ID
    /// </summary>
    public string? DataConfigId { get; set; }

    /// <summary>
    /// 创建的绑定ID
    /// </summary>
    public string? BindingId { get; set; }
}
