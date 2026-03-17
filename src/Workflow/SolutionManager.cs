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

        // 加载设置（轻量级配置，不涉及Solution对象）
        LoadSettings();

        // 扫描文件系统，注册所有元数据（轻量级，200-300字节/个）
        RefreshMetadata();
    }

    /// <summary>
    /// 创建新解决方案（文件级操作）
    /// </summary>
    /// <param name="metadata">解决方案元数据（从对话框获取）</param>
    /// <returns>创建的元数据对象</returns>
    public SolutionMetadata CreateNewSolution(SolutionMetadata metadata)
    {
        CloseSolution();

        // 创建新的 Solution 对象
        var solution = Solution.CreateNew(metadata.Name);
        solution.Description = metadata.Description;
        solution.Version = metadata.Version;

        // 添加默认工作流
        var defaultWorkflow = solution.AddWorkflow("默认工作流");

        // 构建文件路径
        var filePath = string.IsNullOrEmpty(metadata.FilePath)
            ? Path.Combine(metadata.DirectoryPath, $"{metadata.Name}.solution")
            : metadata.FilePath;

        solution.FilePath = filePath;

        // ✅ 文件级操作：保存到文件
        bool success = _repository.Save(solution, filePath);
        if (!success)
        {
            throw new InvalidOperationException($"保存解决方案文件失败: {filePath}");
        }

        // 创建元数据（基于保存的文件）
        var newMetadata = SolutionMetadata.FromSolution(solution);
        newMetadata.FilePath = filePath;
        newMetadata.DirectoryPath = Path.GetDirectoryName(filePath) ?? "";

        // 注册元数据
        _registry.Register(newMetadata);
        _cache.Set(newMetadata.Id, newMetadata);

        // 添加到已知解决方案
        _settings.AddKnownSolution(newMetadata);
        _settings.CurrentSolutionId = newMetadata.Id;
        SaveSettings();

        // 设置当前解决方案
        CurrentSolution = solution;
        CurrentFilePath = filePath;

        _logger.Log(LogLevel.Success, $"创建新解决方案: {metadata.Name} -> {filePath}", "SolutionManager");
        return newMetadata;
    }

    /// <summary>
    /// 创建新解决方案（向后兼容方法）
    /// </summary>
    /// <param name="name">解决方案名称</param>
    /// <param name="description">描述</param>
    /// <param name="solutionPath">保存路径（可选）</param>
    /// <returns>创建的元数据对象</returns>
    public SolutionMetadata CreateNewSolution(string name, string description = "", string? solutionPath = null)
    {
        var metadata = new SolutionMetadata
        {
            Name = name,
            Description = description,
            DirectoryPath = solutionPath ?? _solutionsDirectory
        };
        return CreateNewSolution(metadata);
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

        // 添加到已知解决方案
        _settings.AddKnownSolution(metadata);
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
        _settings.AddKnownSolution(metadata);

        SaveSettings();

        // 触发事件
        SolutionSaved?.Invoke(this, CurrentSolution);
        MetadataChanged?.Invoke(this, EventArgs.Empty);

        _logger.Log(LogLevel.Success, $"保存解决方案成功: {CurrentSolution.Name} -> {savePath}", "SolutionManager");
        return true;
    }

    /// <summary>
    /// 另存为解决方案（文件级操作）
    /// </summary>
    /// <param name="filePath">目标文件路径</param>
    /// <returns>新元数据对象，失败返回 null</returns>
    public SolutionMetadata? SaveAsSolution(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            _logger.Log(LogLevel.Warning, "另存为解决方案失败：未指定保存路径", "SolutionManager");
            return null;
        }

        if (CurrentSolution == null || string.IsNullOrEmpty(CurrentFilePath))
        {
            _logger.Log(LogLevel.Warning, "另存为解决方案失败：没有当前解决方案或文件路径", "SolutionManager");
            return null;
        }

        // ✅ 文件级操作：直接复制文件
        bool success = _repository.Copy(CurrentFilePath, filePath, overwrite: true);
        if (!success)
        {
            return null;
        }

        // ✅ 惰性加载：仅加载元数据（不加载完整 Solution）
        var metadata = _repository.LoadMetadata(filePath);
        if (metadata == null)
        {
            return null;
        }

        // 更新元数据
        metadata.FilePath = filePath;
        metadata.DirectoryPath = Path.GetDirectoryName(filePath) ?? "";
        metadata.ModifiedTime = DateTime.Now;

        // 注册元数据
        _registry.Register(metadata);
        _cache.Set(metadata.Id, metadata);
        _settings.AddKnownSolution(metadata);
        _settings.CurrentSolutionId = metadata.Id;
        SaveSettings();

        // 更新当前文件路径
        CurrentFilePath = filePath;
        CurrentSolution.FilePath = filePath;

        // 触发事件
        SolutionSaved?.Invoke(this, CurrentSolution);
        MetadataChanged?.Invoke(this, EventArgs.Empty);

        _logger.Log(LogLevel.Success, $"另存为解决方案成功: {CurrentSolution.Name} -> {filePath}", "SolutionManager");
        return metadata;
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

        // 从已知解决方案移除
        _settings.RemoveKnownSolution(solutionId);

        SaveSettings();

        // 触发事件
        MetadataChanged?.Invoke(this, EventArgs.Empty);

        _logger.Log(LogLevel.Success, $"删除解决方案: {metadata.Name} (Id={solutionId})", "SolutionManager");
    }

    /// <summary>
    /// 复制解决方案（文件级操作，不加载完整 Solution 对象）
    /// </summary>
    /// <param name="sourcePath">源文件路径</param>
    /// <param name="newName">新名称</param>
    /// <param name="targetDirectory">目标目录（默认与源文件相同）</param>
    /// <returns>复制后的元数据对象，失败返回 null</returns>
    public SolutionMetadata? CopySolution(string sourcePath, string newName, string? targetDirectory = null)
    {
        if (string.IsNullOrEmpty(sourcePath) || string.IsNullOrEmpty(newName))
        {
            _logger.Log(LogLevel.Warning, "复制解决方案失败：源路径或新名称为空", "SolutionManager");
            return null;
        }

        if (!_repository.Exists(sourcePath))
        {
            _logger.Log(LogLevel.Warning, $"复制解决方案失败：源文件不存在: {sourcePath}", "SolutionManager");
            return null;
        }

        try
        {
            // 确定目标目录
            var targetDir = targetDirectory ?? Path.GetDirectoryName(sourcePath) ?? _solutionsDirectory;
            var targetFilePath = Path.Combine(targetDir, $"{newName}.solution");

            // ✅ 文件级操作：直接复制文件
            bool success = _repository.Copy(sourcePath, targetFilePath, overwrite: true);
            if (!success)
            {
                return null;
            }

            // ✅ 惰性加载：仅加载元数据（不加载完整 Solution）
            var metadata = _repository.LoadMetadata(targetFilePath);
            if (metadata == null)
            {
                return null;
            }

            // 更新元数据
            metadata.Name = newName;
            metadata.Id = Guid.NewGuid().ToString();
            metadata.FilePath = targetFilePath;
            metadata.DirectoryPath = targetDir;
            metadata.CreatedTime = DateTime.Now;
            metadata.ModifiedTime = DateTime.Now;

            // 注册元数据
            _registry.Register(metadata);
            _cache.Set(metadata.Id, metadata);

            // 添加到已知解决方案
            _settings.AddKnownSolution(metadata);
            SaveSettings();

            // 触发事件
            MetadataChanged?.Invoke(this, EventArgs.Empty);

            _logger.Log(LogLevel.Success, $"复制解决方案成功: {sourcePath} -> {targetFilePath}", "SolutionManager");
            return metadata;
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Error, $"复制解决方案失败: {sourcePath}, 错误: {ex.Message}", "SolutionManager", ex);
            return null;
        }
    }

    /// <summary>
    /// 注册元数据
    /// </summary>
    /// <param name="metadata">元数据对象</param>
    public void RegisterMetadata(SolutionMetadata metadata)
    {
        if (metadata == null)
            throw new ArgumentNullException(nameof(metadata));

        // 注册到注册表
        _registry.Register(metadata);

        // 加入缓存
        _cache.Set(metadata.Id, metadata);

        _logger.Log(LogLevel.Info, $"注册元数据: {metadata.Name} (Id={metadata.Id})", "SolutionManager");
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
    /// 刷新元数据（从KnownSolutions加载并验证文件存在性）
    /// </summary>
    public void RefreshMetadata()
    {
        _logger.Log(LogLevel.Info, "开始刷新元数据：从KnownSolutions加载", "SolutionManager");

        // 从Settings加载已知解决方案
        var knownSolutions = _settings.GetKnownSolutions();

        // 验证文件存在性并过滤
        var validMetadataList = knownSolutions
            .Where(metadata => !string.IsNullOrEmpty(metadata.FilePath) && _repository.Exists(metadata.FilePath))
            .ToList();

        // 注册元数据
        int registeredCount = _registry.RegisterBatch(validMetadataList);

        // 加入缓存
        foreach (var metadata in validMetadataList)
        {
            _cache.Set(metadata.Id, metadata);
        }

        _logger.Log(LogLevel.Success, $"刷新元数据完成: 已知解决方案 {knownSolutions.Count} 个, 有效 {validMetadataList.Count} 个, 注册了 {registeredCount} 个", "SolutionManager");

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

        // 直接设置，不重复加载（避免违背惰性加载）
        CurrentSolution = solution;
        CurrentFilePath = solution.FilePath;

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

}
