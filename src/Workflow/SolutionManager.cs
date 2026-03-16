using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SunEyeVision.Plugin.SDK.Logging;
using SunEyeVision.Core.Services.Serialization;

namespace SunEyeVision.Workflow;

/// <summary>
/// 解决方案管理器（重构版本）
/// </summary>
/// <remarks>
/// 职责：高层协调，整合所有组件
///
/// 特性：
/// - 组合所有组件（Registry、Cache、Repository、Settings）
/// - 提供统一的高层API
/// - 触发事件通知UI层
/// - 管理当前解决方案
/// - 懒加载优化（通过元数据缓存）
///
/// 架构原则（rule-004）：
/// - 职责清晰：仅负责高层协调
/// - 单一职责：不直接操作文件系统（委托给Repository）
/// - 依赖注入：通过构造函数注入依赖
/// - 事件驱动：通过事件通知UI层
///
/// 设计原则（rule-002）：
/// - 命名符合视觉软件行业标准
/// - 方法使用 PascalCase，动词开头
///
/// 日志规范（rule-003）：
/// - 使用 VisionLogger 记录日志
/// - 使用适当的日志级别（Info/Success/Warning/Error）
/// </remarks>
public class SolutionManager
{
    private readonly string _solutionsDirectory;
    private readonly string _configFilePath;
    private readonly ILogger _logger;

    private readonly SolutionRegistry _registry;
    private readonly SolutionCache _cache;
    private readonly SolutionRepository _repository;
    private readonly SolutionSettings _settings;

    /// <summary>
    /// 当前解决方案
    /// </summary>
    public Solution? CurrentSolution { get; private set; }

    /// <summary>
    /// 当前文件路径
    /// </summary>
    public string? CurrentFilePath { get; private set; }

    /// <summary>
    /// 解决方案目录
    /// </summary>
    public string SolutionsDirectory => _solutionsDirectory;

    /// <summary>
    /// 用户设置
    /// </summary>
    public SolutionSettings Settings => _settings;

    /// <summary>
    /// 解决方案打开事件
    /// </summary>
    public event EventHandler<Solution>? SolutionOpened;

    /// <summary>
    /// 解决方案保存事件
    /// </summary>
    public event EventHandler<Solution>? SolutionSaved;

    /// <summary>
    /// 解决方案关闭事件
    /// </summary>
    public event EventHandler? SolutionClosed;

    /// <summary>
    /// 元数据变更事件
    /// </summary>
    public event EventHandler? MetadataChanged;

    /// <summary>
    /// 工作流添加事件
    /// </summary>
    public event EventHandler<Workflow>? WorkflowAdded;

    /// <summary>
    /// 工作流移除事件
    /// </summary>
    public event EventHandler<Workflow>? WorkflowRemoved;

    /// <summary>
    /// 全局变量添加事件
    /// </summary>
    public event EventHandler<GlobalVariable>? GlobalVariableAdded;

    /// <summary>
    /// 设备添加事件
    /// </summary>
    public event EventHandler<Device>? DeviceAdded;

    /// <summary>
    /// 通讯添加事件
    /// </summary>
    public event EventHandler<Communication>? CommunicationAdded;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="solutionsDirectory">解决方案目录</param>
    public SolutionManager(string solutionsDirectory)
    {
        _solutionsDirectory = solutionsDirectory;
        _configFilePath = Path.Combine(solutionsDirectory, "solution_settings.json");
        _logger = VisionLogger.Instance;

        // 初始化组件
        _registry = new SolutionRegistry();
        _cache = new SolutionCache(maxSize: 100, expirationTime: TimeSpan.FromMinutes(30));
        _repository = new SolutionRepository();
        _settings = new SolutionSettings();

        // 创建目录
        Directory.CreateDirectory(solutionsDirectory);

        _logger.Log(LogLevel.Info, $"解决方案管理器初始化: 目录={_solutionsDirectory}, 配置文件={_configFilePath}", "SolutionManager");

        // 加载设置
        LoadSettings();

        // 自动加载最近解决方案
        AutoLoadRecentSolution();
    }

    /// <summary>
    /// 自动加载最近解决方案
    /// </summary>
    private void AutoLoadRecentSolution()
    {
        _logger.Log(LogLevel.Info, $"开始自动加载最近解决方案, 当前SolutionId: {_settings.CurrentSolutionId}", "SolutionManager");

        if (string.IsNullOrEmpty(_settings.CurrentSolutionId))
        {
            _logger.Log(LogLevel.Info, "当前SolutionId为空，跳过自动加载", "SolutionManager");
            return;
        }

        var metadata = _settings.GetRecentSolution(_settings.CurrentSolutionId);
        if (metadata == null)
        {
            _logger.Log(LogLevel.Warning, $"未找到SolutionId对应的最近使用记录: {_settings.CurrentSolutionId}", "SolutionManager");
            return;
        }

        _logger.Log(LogLevel.Info, $"找到最近使用记录: Name={metadata.Name}, FilePath={metadata.FilePath}", "SolutionManager");

        if (!string.IsNullOrEmpty(metadata.FilePath) && _repository.Exists(metadata.FilePath))
        {
            _logger.Log(LogLevel.Info, $"解决方案文件存在，开始加载: {metadata.FilePath}", "SolutionManager");
            var solution = OpenSolution(metadata.FilePath);
            if (solution != null)
            {
                _logger.Log(LogLevel.Success, $"自动加载最近解决方案成功: {solution.Name}", "SolutionManager");
            }
            else
            {
                _logger.Log(LogLevel.Error, $"加载解决方案失败: {metadata.FilePath}", "SolutionManager");
            }
        }
        else
        {
            _logger.Log(LogLevel.Warning, $"解决方案文件不存在: FilePath={metadata.FilePath}", "SolutionManager");
        }
    }

    /// <summary>
    /// 创建新解决方案
    /// </summary>
    /// <param name="name">解决方案名称</param>
    /// <param name="description">描述</param>
    /// <param name="solutionPath">保存路径（可选）</param>
    /// <returns>创建的解决方案对象</returns>
    public Solution CreateNewSolution(string name, string description = "", string? solutionPath = null)
    {
        CloseSolution();

        CurrentSolution = Solution.CreateNew(name);
        CurrentSolution.Description = description;

        // 添加默认工作流
        var defaultWorkflow = CurrentSolution.AddWorkflow("默认工作流");

        // 创建元数据
        var metadata = SolutionMetadata.FromSolution(CurrentSolution);

        if (!string.IsNullOrEmpty(solutionPath))
        {
            // 构建完整文件路径
            var filePath = Path.Combine(solutionPath, $"{name}.solution");
            CurrentFilePath = filePath;
            CurrentSolution.FilePath = filePath;
            metadata.FilePath = filePath;
            metadata.DirectoryPath = solutionPath;

            // 注册元数据
            _registry.Register(metadata);
            _cache.Set(metadata.Id, metadata);
        }
        else
        {
            CurrentFilePath = null;
        }

        // 添加到最近使用
        _settings.AddRecentSolution(metadata);
        _settings.CurrentSolutionId = metadata.Id;
        SaveSettings();

        _logger.Log(LogLevel.Success, $"创建新解决方案: {name}", "SolutionManager");
        return CurrentSolution;
    }

    /// <summary>
    /// 打开解决方案（懒加载）
    /// </summary>
    /// <param name="filePath">解决方案文件路径</param>
    /// <returns>解决方案对象，失败返回 null</returns>
    public Solution? OpenSolution(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            _logger.Log(LogLevel.Warning, "打开解决方案失败：文件路径为空", "SolutionManager");
            return null;
        }

        // 检查文件是否存在
        if (!_repository.Exists(filePath))
        {
            _logger.Log(LogLevel.Error, $"打开解决方案失败：文件不存在: {filePath}", "SolutionManager");
            return null;
        }

        CloseSolution();

        // 加载解决方案
        var solution = _repository.Load(filePath);
        if (solution == null)
        {
            _logger.Log(LogLevel.Error, $"打开解决方案失败：加载失败: {filePath}", "SolutionManager");
            return null;
        }

        CurrentSolution = solution;
        CurrentFilePath = filePath;

        // 创建或更新元数据
        var metadata = SolutionMetadata.FromSolution(solution);
        metadata.FilePath = filePath;
        metadata.DirectoryPath = Path.GetDirectoryName(filePath) ?? "";

        // 注册元数据
        _registry.Register(metadata);
        _cache.Set(metadata.Id, metadata);

        // 添加到最近使用
        _settings.AddRecentSolution(metadata);
        _settings.CurrentSolutionId = metadata.Id;
        SaveSettings();

        // 触发事件
        SolutionOpened?.Invoke(this, solution);
        MetadataChanged?.Invoke(this, EventArgs.Empty);

        _logger.Log(LogLevel.Success, $"打开解决方案: {solution.Name} -> {filePath}", "SolutionManager");
        return CurrentSolution;
    }

    /// <summary>
    /// 从路径加载解决方案（别名方法）
    /// </summary>
    /// <param name="filePath">解决方案文件路径</param>
    /// <returns>解决方案对象，失败返回 null</returns>
    public Solution? LoadSolutionFromPath(string filePath)
    {
        return OpenSolution(filePath);
    }

    /// <summary>
    /// 仅加载解决方案对象（不设置为当前解决方案）
    /// </summary>
    /// <param name="filePath">解决方案文件路径</param>
    /// <returns>解决方案对象，失败返回 null</returns>
    public Solution? LoadSolutionOnly(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            _logger.Log(LogLevel.Warning, "仅加载解决方案失败：文件路径为空", "SolutionManager");
            return null;
        }

        return _repository.Load(filePath);
    }

    /// <summary>
    /// 保存当前解决方案
    /// </summary>
    /// <param name="filePath">文件路径（可选，默认使用当前路径）</param>
    /// <returns>是否成功</returns>
    public bool SaveSolution(string? filePath = null)
    {
        if (CurrentSolution == null)
        {
            _logger.Log(LogLevel.Warning, "保存解决方案失败：没有当前解决方案", "SolutionManager");
            return false;
        }

        var savePath = filePath ?? CurrentFilePath;
        if (string.IsNullOrEmpty(savePath))
        {
            _logger.Log(LogLevel.Warning, "保存解决方案失败：未指定保存路径", "SolutionManager");
            return false;
        }

        // 保存文件
        bool success = _repository.Save(CurrentSolution, savePath);
        if (!success)
        {
            return false;
        }

        CurrentFilePath = savePath;

        // 更新元数据
        var metadata = SolutionMetadata.FromSolution(CurrentSolution);
        metadata.FilePath = savePath;
        metadata.DirectoryPath = Path.GetDirectoryName(savePath) ?? "";

        _registry.Register(metadata);
        _cache.Set(metadata.Id, metadata);
        _settings.AddRecentSolution(metadata);

        SaveSettings();

        // 触发事件
        SolutionSaved?.Invoke(this, CurrentSolution);
        MetadataChanged?.Invoke(this, EventArgs.Empty);

        _logger.Log(LogLevel.Success, $"保存解决方案成功: {CurrentSolution.Name} -> {savePath}", "SolutionManager");
        return true;
    }

    /// <summary>
    /// 另存为解决方案
    /// </summary>
    /// <param name="filePath">目标文件路径</param>
    /// <returns>是否成功</returns>
    public bool SaveAsSolution(string filePath)
    {
        if (CurrentSolution == null)
        {
            _logger.Log(LogLevel.Warning, "另存为解决方案失败：没有当前解决方案", "SolutionManager");
            return false;
        }

        if (string.IsNullOrEmpty(filePath))
        {
            _logger.Log(LogLevel.Warning, "另存为解决方案失败：未指定保存路径", "SolutionManager");
            return false;
        }

        // 保存到新路径
        bool success = _repository.Save(CurrentSolution, filePath);
        if (!success)
        {
            return false;
        }

        CurrentFilePath = filePath;

        // 创建新元数据
        var newMetadata = SolutionMetadata.FromSolution(CurrentSolution);
        newMetadata.FilePath = filePath;
        newMetadata.DirectoryPath = Path.GetDirectoryName(filePath) ?? "";

        // 注册新元数据
        _registry.Register(newMetadata);
        _cache.Set(newMetadata.Id, newMetadata);
        _settings.AddRecentSolution(newMetadata);
        _settings.CurrentSolutionId = newMetadata.Id;

        SaveSettings();

        // 触发事件
        SolutionSaved?.Invoke(this, CurrentSolution);
        MetadataChanged?.Invoke(this, EventArgs.Empty);

        _logger.Log(LogLevel.Success, $"另存为解决方案成功: {CurrentSolution.Name} -> {filePath}", "SolutionManager");
        return true;
    }

    /// <summary>
    /// 另存为解决方案（带新名称和描述）
    /// </summary>
    /// <param name="solutionId">解决方案ID</param>
    /// <param name="newName">新名称</param>
    /// <param name="newDescription">新描述</param>
    /// <param name="newPath">新路径</param>
    /// <returns>新解决方案对象，失败返回 null</returns>
    public Solution? SaveSolutionAs(string solutionId, string newName, string newDescription, string newPath)
    {
        if (CurrentSolution?.Id != solutionId)
        {
            _logger.Log(LogLevel.Warning, "另存为解决方案失败：只能另存为当前解决方案", "SolutionManager");
            return null;
        }

        // 创建副本
        var newSolution = CurrentSolution.Clone();
        newSolution.Id = Guid.NewGuid().ToString();
        newSolution.Name = newName;
        newSolution.Description = newDescription;
        newSolution.FilePath = newPath;

        // 保存到新路径
        bool success = _repository.Save(newSolution, newPath);
        if (!success)
        {
            return null;
        }

        // 创建新元数据
        var newMetadata = SolutionMetadata.FromSolution(newSolution);
        newMetadata.FilePath = newPath;
        newMetadata.DirectoryPath = Path.GetDirectoryName(newPath) ?? "";

        // 注册新元数据
        _registry.Register(newMetadata);
        _cache.Set(newMetadata.Id, newMetadata);
        _settings.AddRecentSolution(newMetadata);
        _settings.CurrentSolutionId = newMetadata.Id;

        SaveSettings();

        // 触发事件
        MetadataChanged?.Invoke(this, EventArgs.Empty);

        _logger.Log(LogLevel.Success, $"另存为解决方案成功: {newName} -> {newPath}", "SolutionManager");
        return newSolution;
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
    /// 删除解决方案
    /// </summary>
    /// <param name="solutionId">解决方案ID</param>
    public void DeleteSolution(string solutionId)
    {
        if (string.IsNullOrEmpty(solutionId))
        {
            _logger.Log(LogLevel.Warning, "删除解决方案失败：解决方案ID为空", "SolutionManager");
            return;
        }

        // 如果是当前解决方案，先关闭
        if (CurrentSolution?.Id == solutionId)
        {
            CloseSolution();
        }

        // 获取元数据
        var metadata = _registry.Get(solutionId);
        if (metadata == null)
        {
            _logger.Log(LogLevel.Warning, $"删除解决方案失败：元数据不存在: {solutionId}", "SolutionManager");
            return;
        }

        // 删除文件
        if (!string.IsNullOrEmpty(metadata.FilePath) && _repository.Exists(metadata.FilePath))
        {
            _repository.Delete(metadata.FilePath);
        }

        // 从注册表移除
        _registry.Unregister(solutionId);

        // 从缓存移除
        _cache.Invalidate(solutionId);

        // 从最近使用移除
        _settings.RemoveRecentSolution(solutionId);

        SaveSettings();

        // 触发事件
        MetadataChanged?.Invoke(this, EventArgs.Empty);

        _logger.Log(LogLevel.Success, $"删除解决方案: {metadata.Name} (Id={solutionId})", "SolutionManager");
    }

    /// <summary>
    /// 获取元数据（优先从缓存）
    /// </summary>
    /// <param name="solutionId">解决方案ID</param>
    /// <returns>元数据对象，不存在返回 null</returns>
    public SolutionMetadata? GetMetadata(string solutionId)
    {
        if (string.IsNullOrEmpty(solutionId))
            return null;

        // 先从缓存获取
        var metadata = _cache.Get(solutionId);
        if (metadata != null)
            return metadata;

        // 从注册表获取
        metadata = _registry.Get(solutionId);
        if (metadata != null)
        {
            // 加入缓存
            _cache.Set(solutionId, metadata);
            return metadata;
        }

        return null;
    }

    /// <summary>
    /// 获取所有元数据
    /// </summary>
    /// <returns>元数据列表</returns>
    public List<SolutionMetadata> GetAllMetadata()
    {
        return _registry.GetAll();
    }

    /// <summary>
    /// 获取最近使用的解决方案列表
    /// </summary>
    /// <returns>元数据列表</returns>
    public List<SolutionMetadata> GetRecentSolutions()
    {
        return _settings.GetRecentSolutionsCopy();
    }

    /// <summary>
    /// 刷新元数据（扫描文件系统）
    /// </summary>
    public void RefreshMetadata()
    {
        _logger.Log(LogLevel.Info, "开始刷新元数据：扫描目录", "SolutionManager");

        // 扫描目录
        var metadataList = _repository.ScanDirectory(_solutionsDirectory);

        // 注册元数据
        int registeredCount = _registry.RegisterBatch(metadataList);

        // 加入缓存
        foreach (var metadata in metadataList)
        {
            _cache.Set(metadata.Id, metadata);
        }

        _logger.Log(LogLevel.Success, $"刷新元数据完成: 扫描到 {metadataList.Count} 个解决方案, 注册了 {registeredCount} 个", "SolutionManager");

        // 触发事件
        MetadataChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 扫描指定目录并注册元数据
    /// </summary>
    /// <param name="directoryPath">目录路径</param>
    public void ScanDirectory(string directoryPath)
    {
        _logger.Log(LogLevel.Info, $"开始扫描目录: {directoryPath}", "SolutionManager");

        // 扫描目录
        var metadataList = _repository.ScanDirectory(directoryPath);

        // 注册元数据
        int registeredCount = _registry.RegisterBatch(metadataList);

        // 加入缓存
        foreach (var metadata in metadataList)
        {
            _cache.Set(metadata.Id, metadata);
        }

        _logger.Log(LogLevel.Success, $"扫描目录完成: 扫描到 {metadataList.Count} 个解决方案, 注册了 {registeredCount} 个", "SolutionManager");

        // 触发事件
        MetadataChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 批量加载元数据
    /// </summary>
    /// <param name="filePaths">文件路径列表</param>
    /// <returns>元数据列表</returns>
    public List<SolutionMetadata> LoadMetadataBatch(System.Collections.Generic.IEnumerable<string> filePaths)
    {
        _logger.Log(LogLevel.Info, $"开始批量加载元数据: 数量={filePaths.Count()}", "SolutionManager");

        // 加载元数据
        var metadataList = _repository.LoadMetadataBatch(filePaths);

        // 注册元数据
        int registeredCount = _registry.RegisterBatch(metadataList);

        // 加入缓存
        foreach (var metadata in metadataList)
        {
            _cache.Set(metadata.Id, metadata);
        }

        _logger.Log(LogLevel.Success, $"批量加载元数据完成: 加载 {metadataList.Count} 个, 注册 {registeredCount} 个", "SolutionManager");

        return metadataList;
    }

    /// <summary>
    /// 添加到最近使用
    /// </summary>
    /// <param name="solutionId">解决方案ID</param>
    public void AddToRecent(string solutionId)
    {
        if (string.IsNullOrEmpty(solutionId))
            return;

        var metadata = GetMetadata(solutionId);
        if (metadata != null)
        {
            _settings.AddRecentSolution(metadata);
            SaveSettings();
        }
    }

    /// <summary>
    /// 工作流管理
    /// </summary>
    public Workflow? AddWorkflow(string name, string description = "")
    {
        if (CurrentSolution == null)
        {
            _logger.Log(LogLevel.Warning, "添加工作流失败：没有当前解决方案", "SolutionManager");
            return null;
        }

        var workflow = CurrentSolution.AddWorkflow(name);
        WorkflowAdded?.Invoke(this, workflow);
        return workflow;
    }

    public bool RemoveWorkflow(string workflowId)
    {
        if (CurrentSolution == null)
        {
            _logger.Log(LogLevel.Warning, "移除工作流失败：没有当前解决方案", "SolutionManager");
            return false;
        }

        var workflow = CurrentSolution.GetWorkflow(workflowId);
        if (workflow == null)
        {
            _logger.Log(LogLevel.Warning, $"移除工作流失败：工作流不存在: {workflowId}", "SolutionManager");
            return false;
        }

        bool removed = CurrentSolution.RemoveWorkflow(workflowId);
        if (removed)
        {
            WorkflowRemoved?.Invoke(this, workflow);
        }
        return removed;
    }

    /// <summary>
    /// 全局变量管理
    /// </summary>
    public GlobalVariable? AddGlobalVariable(string name, object? value, string type = "String")
    {
        if (CurrentSolution == null)
        {
            _logger.Log(LogLevel.Warning, "添加全局变量失败：没有当前解决方案", "SolutionManager");
            return null;
        }

        var globalVariable = CurrentSolution.AddGlobalVariable(name, value, type);
        GlobalVariableAdded?.Invoke(this, globalVariable);
        return globalVariable;
    }

    public bool RemoveGlobalVariable(string name)
    {
        if (CurrentSolution == null)
        {
            _logger.Log(LogLevel.Warning, "移除全局变量失败：没有当前解决方案", "SolutionManager");
            return false;
        }

        return CurrentSolution.RemoveGlobalVariable(name);
    }

    /// <summary>
    /// 设备管理
    /// </summary>
    public Device? AddDevice(string name, DeviceType type)
    {
        if (CurrentSolution == null)
        {
            _logger.Log(LogLevel.Warning, "添加设备失败：没有当前解决方案", "SolutionManager");
            return null;
        }

        var device = CurrentSolution.AddDevice(name, type);
        DeviceAdded?.Invoke(this, device);
        return device;
    }

    public bool RemoveDevice(string deviceId)
    {
        if (CurrentSolution == null)
        {
            _logger.Log(LogLevel.Warning, "移除设备失败：没有当前解决方案", "SolutionManager");
            return false;
        }

        return CurrentSolution.RemoveDevice(deviceId);
    }

    /// <summary>
    /// 通讯配置管理
    /// </summary>
    public Communication? AddCommunication(string name, CommunicationType type)
    {
        if (CurrentSolution == null)
        {
            _logger.Log(LogLevel.Warning, "添加通讯配置失败：没有当前解决方案", "SolutionManager");
            return null;
        }

        var communication = CurrentSolution.AddCommunication(name, type);
        CommunicationAdded?.Invoke(this, communication);
        return communication;
    }

    public bool RemoveCommunication(string communicationId)
    {
        if (CurrentSolution == null)
        {
            _logger.Log(LogLevel.Warning, "移除通讯配置失败：没有当前解决方案", "SolutionManager");
            return false;
        }

        return CurrentSolution.RemoveCommunication(communicationId);
    }

    /// <summary>
    /// 设置当前解决方案
    /// </summary>
    public void SetCurrentSolution(Solution solution)
    {
        if (solution == null)
            throw new ArgumentNullException(nameof(solution));

        // 如果有文件路径，从文件重新加载
        if (!string.IsNullOrEmpty(solution.FilePath) && _repository.Exists(solution.FilePath))
        {
            var reloadedSolution = _repository.Load(solution.FilePath);
            if (reloadedSolution != null)
            {
                CurrentSolution = reloadedSolution;
                CurrentFilePath = solution.FilePath;
            }
            else
            {
                CurrentSolution = solution;
            }
        }
        else
        {
            CurrentSolution = solution;
        }

        // 更新设置
        _settings.CurrentSolutionId = solution.Id;
        SaveSettings();

        // 触发事件
        SolutionOpened?.Invoke(this, CurrentSolution);

        _logger.Log(LogLevel.Info, $"设置当前解决方案: {CurrentSolution.Name}", "SolutionManager");
    }

    /// <summary>
    /// 加载设置
    /// </summary>
    private void LoadSettings()
    {
        _logger.Log(LogLevel.Info, $"开始加载设置文件: {_configFilePath}", "SolutionManager");

        _settings.Load(_configFilePath);

        _logger.Log(LogLevel.Success, $"设置加载完成: {_settings.GetStatistics()}", "SolutionManager");
    }

    /// <summary>
    /// 保存设置
    /// </summary>
    private void SaveSettings()
    {
        _logger.Log(LogLevel.Info, $"开始保存设置文件: {_configFilePath}", "SolutionManager");

        _settings.Save(_configFilePath);

        _logger.Log(LogLevel.Success, "设置保存完成", "SolutionManager");
    }

    /// <summary>
    /// 获取统计信息
    /// </summary>
    /// <returns>统计信息字符串</returns>
    public string GetStatistics()
    {
        return $"解决方案管理器统计:\n" +
               $"  - 注册表数量: {_registry.Count}\n" +
               $"  - 缓存数量: {_cache.Count}, 命中率: {_cache.HitRate:P2}\n" +
               $"  - 当前解决方案: {CurrentSolution?.Name ?? "无"}\n" +
               $"  - 设置统计: {_settings.GetStatistics()}";
    }

    // ========== 向后兼容的遗留方法 ==========

    /// <summary>
    /// 创建新解决方案（别名方法）
    /// </summary>
    [System.Obsolete("请使用 CreateNewSolution 方法")]
    public Solution CreateSolution(string name, string description = "", string? solutionPath = null)
    {
        return CreateNewSolution(name, description, solutionPath);
    }

    /// <summary>
    /// 获取配置文件路径（向后兼容）
    /// </summary>
    [System.Obsolete("配置文件已迁移到 SolutionSettings")]
    public string ConfigFilePath => _configFilePath;

}
