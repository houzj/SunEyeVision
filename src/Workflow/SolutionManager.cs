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
    /// 解决方案添加事件
    /// </summary>
    public event EventHandler<SolutionMetadataEventArgs>? SolutionAdded;

    /// <summary>
    /// 解决方案删除事件
    /// </summary>
    public event EventHandler<SolutionMetadataEventArgs>? SolutionRemoved;

    /// <summary>
    /// 解决方案重命名事件
    /// </summary>
    public event EventHandler<SolutionMetadataEventArgs>? SolutionRenamed;

    /// <summary>
    /// 解决方案更新事件
    /// </summary>
    public event EventHandler<SolutionMetadataEventArgs>? SolutionUpdated;

    /// <summary>
    /// 元数据刷新事件（全量）
    /// </summary>
    public event EventHandler? MetadataRefreshed;

    /// <summary>
    /// 当前解决方案变更事件
    /// </summary>
    public event EventHandler<SolutionMetadataEventArgs>? CurrentSolutionChanged;

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

        // 创建新的 Solution 对象（纯数据模型）
        var solution = Solution.CreateNew();
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
        var newMetadata = metadata.Clone();
        newMetadata.Id = solution.Id;
        newMetadata.FilePath = filePath;
        newMetadata.DirectoryPath = Path.GetDirectoryName(filePath) ?? "";
        newMetadata.CreatedTime = DateTime.Now;
        newMetadata.ModifiedTime = DateTime.Now;
        newMetadata.UpdateStatistics(solution);

        // 注册元数据
        _registry.Register(newMetadata);
        _cache.Set(newMetadata.Id, newMetadata);

        // 添加到已知解决方案
        _settings.AddKnownSolution(newMetadata);
        // ❌ 不自动设置 CurrentSolutionId，需要用户明确启动才设置
        // _settings.CurrentSolutionId = newMetadata.Id;
        SaveSettings();

        // ❌ 不自动设置当前解决方案，需要用户明确启动才设置
        // CurrentSolution = solution;
        // CurrentFilePath = filePath;

        _logger.Log(LogLevel.Success, $"创建新解决方案: {newMetadata.Name} -> {filePath}", "SolutionManager");
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
        var metadata = _repository.LoadMetadata(filePath);
        if (metadata == null)
        {
            // 如果无法加载元数据，创建一个基本的
            metadata = new SolutionMetadata
            {
                Id = solution.Id,
                Version = solution.Version,
                FilePath = filePath,
                DirectoryPath = Path.GetDirectoryName(filePath) ?? "",
                CreatedTime = File.GetCreationTime(filePath),
                ModifiedTime = File.GetLastWriteTime(filePath),
                Name = Path.GetFileNameWithoutExtension(filePath),
                Description = ""
            };
        }
        metadata.UpdateStatistics(solution);

        // 注册元数据
        _registry.Register(metadata);
        _cache.Set(metadata.Id, metadata);

        // 添加到已知解决方案
        _settings.AddKnownSolution(metadata);
        _settings.CurrentSolutionId = metadata.Id;
        SaveSettings();

        // 触发事件
        SolutionOpened?.Invoke(this, solution);

        // ✅ 使用细粒度事件
        var currentSolutionArgs = new SolutionMetadataEventArgs
        {
            Metadata = metadata,
            SolutionId = metadata.Id
        };
        CurrentSolutionChanged?.Invoke(this, currentSolutionArgs);

        _logger.Log(LogLevel.Success, $"打开解决方案: {metadata.Name} -> {filePath}", "SolutionManager");
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

        // ✅ 修复：从缓存或注册表获取现有元数据，不从文件重新加载
        // 原因：Solution 类已不包含 Name 等元数据属性，从文件加载会导致元数据为空
        var metadata = _cache.Get(CurrentSolution.Id) ?? _registry.Get(CurrentSolution.Id);
        if (metadata == null)
        {
            // 如果元数据不存在（不应该发生），创建一个基本的元数据
            metadata = new SolutionMetadata
            {
                Id = CurrentSolution.Id,
                Name = Path.GetFileNameWithoutExtension(savePath),
                Description = "",
                Version = CurrentSolution.Version,
                FilePath = savePath,
                DirectoryPath = Path.GetDirectoryName(savePath) ?? "",
                CreatedTime = File.GetCreationTime(savePath),
                ModifiedTime = DateTime.Now
            };
        }
        else
        {
            // 更新现有元数据
            metadata.FilePath = savePath;
            metadata.DirectoryPath = Path.GetDirectoryName(savePath) ?? "";
            metadata.ModifiedTime = DateTime.Now;
        }

        // 更新统计数据
        metadata.UpdateStatistics(CurrentSolution);

        // 注册元数据
        _registry.Register(metadata);
        _cache.Set(metadata.Id, metadata);
        _settings.AddKnownSolution(metadata);

        SaveSettings();

        // 触发事件
        SolutionSaved?.Invoke(this, CurrentSolution);

        // ✅ 使用细粒度事件
        var updatedArgs = new SolutionMetadataEventArgs
        {
            Metadata = metadata,
            SolutionId = metadata.Id
        };
        SolutionUpdated?.Invoke(this, updatedArgs);

        _logger.Log(LogLevel.Success, $"保存解决方案成功: Id={CurrentSolution.Id} -> {savePath}", "SolutionManager");
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

        // ✅ 使用细粒度事件
        var addedArgs = new SolutionMetadataEventArgs
        {
            Metadata = metadata,
            SolutionId = metadata.Id
        };
        SolutionAdded?.Invoke(this, addedArgs);

        var currentSolutionArgs = new SolutionMetadataEventArgs
        {
            Metadata = metadata,
            SolutionId = metadata.Id
        };
        CurrentSolutionChanged?.Invoke(this, currentSolutionArgs);

        _logger.Log(LogLevel.Success, $"另存为解决方案成功: Id={CurrentSolution.Id} -> {filePath}", "SolutionManager");
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

        // ✅ 使用细粒度事件
        var removedArgs = new SolutionMetadataEventArgs
        {
            SolutionId = solutionId
        };
        SolutionRemoved?.Invoke(this, removedArgs);

        _logger.Log(LogLevel.Success, $"删除解决方案: {metadata.Name} (Id={solutionId})", "SolutionManager");
    }

    /// <summary>
    /// 重命名解决方案文件
    /// </summary>
    /// <param name="solutionId">解决方案ID</param>
    /// <param name="newName">新名称</param>
    /// <returns>是否成功</returns>
    /// <remarks>
    /// 重命名流程（新架构）：
    /// 1. 验证参数有效性
    /// 2. 获取元数据（从注册表）
    /// 3. 构建新文件路径
    /// 4. 重命名物理文件（通过 Repository.Move）
    /// 5. 更新元数据（Name、FilePath）
    /// 6. 更新注册表和缓存
    /// 7. 更新 KnownSolutions 设置
    /// 8. 如果 Solution 已加载，更新其 FilePath（Solution 本身没有 Name 属性）
    /// 
    /// 设计原则（rule-002）：
    /// - 方法使用 PascalCase，动词开头
    /// - 命名符合视觉软件行业标准
    /// 
    /// 日志规范（rule-003）：
    /// - 使用 VisionLogger 记录日志
    /// - 使用适当的日志级别
    /// </remarks>
    public bool RenameSolutionFile(string solutionId, string newName)
    {
        // 1. 参数验证
        if (string.IsNullOrEmpty(solutionId))
        {
            _logger.Log(LogLevel.Warning, "重命名解决方案失败：解决方案ID为空", "SolutionManager");
            return false;
        }

        if (string.IsNullOrWhiteSpace(newName))
        {
            _logger.Log(LogLevel.Warning, "重命名解决方案失败：新名称为空", "SolutionManager");
            return false;
        }

        // 2. 获取元数据
        var metadata = _registry.Get(solutionId);
        if (metadata == null)
        {
            _logger.Log(LogLevel.Warning, $"重命名解决方案失败：元数据不存在: {solutionId}", "SolutionManager");
            return false;
        }

        if (string.IsNullOrEmpty(metadata.FilePath))
        {
            _logger.Log(LogLevel.Warning, $"重命名解决方案失败：文件路径为空: {metadata.Name}", "SolutionManager");
            return false;
        }

        // 3. 检查名称是否变化
        if (metadata.Name == newName)
        {
            _logger.Log(LogLevel.Info, $"解决方案名称未变化，跳过重命名: {metadata.Name}", "SolutionManager");
            return true;
        }

        try
        {
            var oldName = metadata.Name;
            var oldFilePath = metadata.FilePath;
            var directory = Path.GetDirectoryName(oldFilePath) ?? _solutionsDirectory;
            var newFilePath = Path.Combine(directory, $"{newName}.solution");

            _logger.Log(LogLevel.Info, $"开始重命名解决方案: {oldName} -> {newName}", "SolutionManager");
            _logger.Log(LogLevel.Info, $"文件路径: {oldFilePath} -> {newFilePath}", "SolutionManager");

            // 4. 检查目标文件是否已存在
            if (_repository.Exists(newFilePath))
            {
                _logger.Log(LogLevel.Warning, $"重命名解决方案失败：目标文件已存在: {newFilePath}", "SolutionManager");
                return false;
            }

            // 5. 重命名物理文件
            bool moveSuccess = _repository.Move(oldFilePath, newFilePath, overwrite: false);
            if (!moveSuccess)
            {
                _logger.Log(LogLevel.Error, $"重命名解决方案失败：文件移动失败: {oldFilePath} -> {newFilePath}", "SolutionManager");
                return false;
            }

            // 6. 更新元数据
            metadata.Name = newName;
            metadata.FilePath = newFilePath;
            metadata.DirectoryPath = directory;
            metadata.ModifiedTime = DateTime.Now;

            // 7. 更新注册表和缓存
            _registry.Register(metadata);
            _cache.Set(metadata.Id, metadata);

            // 8. 更新 KnownSolutions 设置
            _settings.AddKnownSolution(metadata);
            SaveSettings();

            // 9. 更新当前文件路径（如果是当前解决方案）
            if (CurrentSolution?.Id == solutionId)
            {
                CurrentFilePath = newFilePath;
                CurrentSolution.FilePath = newFilePath;
            }

            // 10. 触发事件
            var renamedArgs = new SolutionMetadataEventArgs
            {
                Metadata = metadata,
                SolutionId = metadata.Id,
                OldName = oldName,
                NewName = newName
            };
            SolutionRenamed?.Invoke(this, renamedArgs);

            _logger.Log(LogLevel.Success, $"重命名解决方案成功: {oldName} -> {newName}", "SolutionManager");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Error, $"重命名解决方案失败: {metadata.Name} -> {newName}, 错误: {ex.Message}", "SolutionManager", ex);
            return false;
        }
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

            // ✅ 使用细粒度事件
            var addedArgs = new SolutionMetadataEventArgs
            {
                Metadata = metadata,
                SolutionId = metadata.Id
            };
            SolutionAdded?.Invoke(this, addedArgs);

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
    /// 更新元数据（不涉及 Solution 文件）
    /// </summary>
    /// <param name="metadata">更新后的元数据</param>
    /// <remarks>
    /// 用于编辑解决方案名称、描述等元数据，不需要加载完整 Solution 文件。
    /// 
    /// 更新内容：
    /// - Name、Description、ModifiedTime 等
    /// - Registry、Cache、Settings 中的元数据
    /// 
    /// 设计原则（rule-002）：
    /// - 方法使用 PascalCase，动词开头
    /// - 命名符合视觉软件行业标准
    /// </remarks>
    public void UpdateMetadata(SolutionMetadata metadata)
    {
        if (metadata == null)
            throw new ArgumentNullException(nameof(metadata));

        // 更新修改时间
        metadata.UpdateModifiedTime();

        // 更新注册表
        _registry.Register(metadata);

        // 更新缓存
        _cache.Set(metadata.Id, metadata);

        // 更新 KnownSolutions
        _settings.AddKnownSolution(metadata);
        SaveSettings();

        // ✅ 使用细粒度事件
        var updatedArgs = new SolutionMetadataEventArgs
        {
            Metadata = metadata,
            SolutionId = metadata.Id
        };
        SolutionUpdated?.Invoke(this, updatedArgs);

        _logger.Log(LogLevel.Success, $"更新解决方案元数据: {metadata.Name} (Id={metadata.Id})", "SolutionManager");
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
    /// <remarks>
    /// 改进（2026-03-17）：添加文件名变化检测
    /// - 自动检测用户直接在文件系统中修改的文件名
    /// - 通过ID匹配查找重命名的文件
    /// - 自动更新 KnownSolutions、Registry、Cache 中的路径
    /// - 记录日志并触发事件通知UI层
    /// </remarks>
    public void RefreshMetadata()
    {
        _logger.Log(LogLevel.Info, "开始刷新元数据：从KnownSolutions加载", "SolutionManager");

        // 从Settings加载已知解决方案
        var knownSolutions = _settings.GetKnownSolutions();
        var validMetadataList = new List<SolutionMetadata>();
        int renamedCount = 0;
        int missingCount = 0;

        foreach (var metadata in knownSolutions)
        {
            // ==================== 步骤1：检测文件名变化 ====================
            if (!string.IsNullOrEmpty(metadata.FilePath) && !_repository.Exists(metadata.FilePath))
            {
                // 文件不存在，尝试查找重命名的文件
                var newFilePath = TryFindRenamedFile(metadata);

                if (newFilePath != null)
                {
                    // ✅ 步骤2：更新元数据对象
                    var oldName = metadata.Name;
                    var oldFilePath = metadata.FilePath;

                    metadata.FilePath = newFilePath;
                    metadata.Name = Path.GetFileNameWithoutExtension(newFilePath);
                    metadata.DirectoryPath = Path.GetDirectoryName(newFilePath) ?? "";
                    metadata.ModifiedTime = DateTime.Now;

                    // ✅ 步骤3：更新 KnownSolutions（后面统一保存）
                    _settings.AddKnownSolution(metadata);

                    // ✅ 步骤4：更新当前解决方案（如果已加载）
                    if (CurrentSolution?.Id == metadata.Id)
                    {
                        CurrentSolution.FilePath = newFilePath;
                    }

                    // ✅ 步骤5：记录日志
                    _logger.Log(LogLevel.Warning,
                        $"检测到文件名变化: {oldName} → {metadata.Name} (Id={metadata.Id})",
                        "SolutionManager");
                    _logger.Log(LogLevel.Info,
                        $"文件路径: {oldFilePath} → {newFilePath}",
                        "SolutionManager");

                    renamedCount++;
                }
                else
                {
                    // 文件确实丢失，记录警告
                    _logger.Log(LogLevel.Warning,
                        $"解决方案文件丢失: {metadata.Name} (FilePath={metadata.FilePath}, Id={metadata.Id})",
                        "SolutionManager");
                    missingCount++;
                    continue; // 跳过此元数据
                }
            }

            // 添加到有效列表
            if (!string.IsNullOrEmpty(metadata.FilePath))
            {
                validMetadataList.Add(metadata);
            }
        }

        // ✅ 步骤6：注册元数据到 Registry
        int registeredCount = _registry.RegisterBatch(validMetadataList);

        // ✅ 步骤7：加入缓存
        foreach (var metadata in validMetadataList)
        {
            _cache.Set(metadata.Id, metadata);
        }

        // ✅ 步骤8：持久化更新 settings.json（如果有重命名）
        if (renamedCount > 0)
        {
            SaveSettings();
            _logger.Log(LogLevel.Success,
                $"已自动更新 {renamedCount} 个重命名的解决方案",
                "SolutionManager");
        }

        _logger.Log(LogLevel.Success,
            $"刷新元数据完成: 已知 {knownSolutions.Count} 个, 有效 {validMetadataList.Count} 个, 注册 {registeredCount} 个, 重命名 {renamedCount} 个, 丢失 {missingCount} 个",
            "SolutionManager");

        // ✅ 步骤9：触发事件通知 UI 层
        MetadataRefreshed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 尝试查找重命名的文件
    /// </summary>
    /// <param name="metadata">元数据对象</param>
    /// <returns>新文件路径，未找到返回 null</returns>
    /// <remarks>
    /// 查找策略：
    /// 1. 在同目录下查找所有 .solution 文件
    /// 2. 快速读取元数据（不加载完整 Solution）
    /// 3. 比较 ID 是否匹配
    /// 4. 找到匹配则返回新文件路径
    ///
    /// 性能优化：
    /// - 使用 LoadMetadata() 仅读取元数据，不加载完整对象
    /// - 文件数量较少时性能影响可忽略
    /// - 文件数量较多时（>100个）建议用户使用重命名功能
    /// </remarks>
    private string? TryFindRenamedFile(SolutionMetadata metadata)
    {
        if (string.IsNullOrEmpty(metadata.FilePath))
            return null;

        var directory = Path.GetDirectoryName(metadata.FilePath);
        if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
            return null;

        try
        {
            // 在同目录下查找所有 .solution 文件
            var solutionFiles = Directory.GetFiles(directory, "*.solution", SearchOption.TopDirectoryOnly);

            _logger.Log(LogLevel.Info,
                $"在目录 {directory} 中查找 {solutionFiles.Length} 个 .solution 文件，匹配 ID={metadata.Id}",
                "SolutionManager");

            foreach (var filePath in solutionFiles)
            {
                // 快速读取元数据（不加载完整 Solution）
                var fileMetadata = _repository.LoadMetadata(filePath);

                if (fileMetadata?.Id == metadata.Id)
                {
                    // 找到了！ID 匹配
                    _logger.Log(LogLevel.Info,
                        $"找到匹配文件: {filePath} (ID={metadata.Id})",
                        "SolutionManager");
                    return filePath;
                }
            }

            _logger.Log(LogLevel.Info,
                $"未找到匹配文件: ID={metadata.Id}",
                "SolutionManager");
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Warning,
                $"查找重命名文件失败: {ex.Message}",
                "SolutionManager");
        }

        return null;
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

        // ✅ 触发事件
        MetadataRefreshed?.Invoke(this, EventArgs.Empty);
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

        // 触发事件（同时触发两个事件，与 OpenSolution 保持一致）
        SolutionOpened?.Invoke(this, CurrentSolution);

        // 触发细粒度事件
        var metadata = _cache.Get(solution.Id) ?? _registry.Get(solution.Id);
        if (metadata != null)
        {
            var currentSolutionArgs = new SolutionMetadataEventArgs
            {
                Metadata = metadata,
                SolutionId = metadata.Id
            };
            CurrentSolutionChanged?.Invoke(this, currentSolutionArgs);
        }

        _logger.Log(LogLevel.Info, $"设置当前解决方案: Id={CurrentSolution.Id}", "SolutionManager");
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
        var currentSolutionInfo = CurrentSolution != null
            ? $"Id={CurrentSolution.Id}"
            : "无";

        return $"解决方案管理器统计:\n" +
               $"  - 注册表数量: {_registry.Count}\n" +
               $"  - 缓存数量: {_cache.Count}, 命中率: {_cache.HitRate:P2}\n" +
               $"  - 当前解决方案: {currentSolutionInfo}\n" +
               $"  - 设置统计: {_settings.GetStatistics()}";
    }

}
