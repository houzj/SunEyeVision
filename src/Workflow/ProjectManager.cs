using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using SunEyeVision.Plugin.SDK.Logging;

namespace SunEyeVision.Workflow;

/// <summary>
/// 项目管理器
/// </summary>
/// <remarks>
/// 管理项目和配方的加载、保存、切换等操作。
/// 
/// 文件结构：
/// solutions/
/// ├── projects/
/// │   └── product_a100/
/// │       ├── project.json
/// │       ├── program.json
/// │       └── recipes/
/// │           ├── standard.json
/// │           └── line1_station1.json
/// └── config.json
/// 
/// 使用场景：
/// 1. 项目创建、加载、保存
/// 2. 配方管理
/// 3. 运行时配置管理
/// 4. 快速产品切换
/// </remarks>
public class ProjectManager
{
    private readonly string _solutionDirectory;
    private readonly string _projectsDirectory;
    private readonly ILogger _logger;

    private readonly Dictionary<string, Project> _projects;
    private RuntimeConfig _runtimeConfig;

    /// <summary>
    /// 当前项目
    /// </summary>
    public Project? CurrentProject { get; private set; }

    /// <summary>
    /// 当前配方
    /// </summary>
    public InspectionRecipe? CurrentRecipe { get; private set; }

    /// <summary>
    /// 运行时配置
    /// </summary>
    public RuntimeConfig RuntimeConfig => _runtimeConfig;

    /// <summary>
    /// 项目目录
    /// </summary>
    public string ProjectsDirectory => _projectsDirectory;

    /// <summary>
    /// 解决方案目录
    /// </summary>
    public string SolutionsDirectory => _solutionDirectory;

    /// <summary>
    /// 所有项目集合
    /// </summary>
    public IEnumerable<Project> Projects => _projects.Values;

    /// <summary>
    /// 项目管理器
    /// </summary>
    public ProjectManager(string solutionDirectory)
    {
        _solutionDirectory = solutionDirectory;
        _projectsDirectory = Path.Combine(solutionDirectory, "projects");
        _logger = VisionLogger.Instance;
        _projects = new Dictionary<string, Project>();
        _runtimeConfig = new RuntimeConfig();

        // 确保目录存在
        Directory.CreateDirectory(_solutionDirectory);
        Directory.CreateDirectory(_projectsDirectory);

        // 加载运行时配置
        LoadRuntimeConfig();

        // 加载所有项目
        LoadAllProjects();
    }

    // ====================================================================
    // 项目管理
    // ====================================================================

    /// <summary>
    /// 创建新项目
    /// </summary>
    public Project CreateProject(string name, string description = "", string? customPath = null)
    {
        var project = new Project
        {
            Name = name,
            Description = description,
            CreatedTime = DateTime.Now,
            ModifiedTime = DateTime.Now
        };

        // 添加默认配方（改名）
        project.AddRecipe("新配方", "默认新配方");

        _projects[project.Id] = project;

        // 使用自定义路径或默认路径
        // 注意：customPath应该是父目录，项目会在该目录下创建以项目名称命名的子目录
        var projectDir = customPath != null
            ? Path.Combine(customPath, SanitizeFileName(name))
            : GetProjectDirectory(project.Id);

        SaveProjectTo(project, projectDir);

        _logger.Log(LogLevel.Success, $"创建项目: {name} -> {projectDir}", "ProjectManager");
        return project;
    }

    /// <summary>
    /// 保存项目
    /// </summary>
    public void SaveProject(Project project)
    {
        if (project == null)
            throw new ArgumentNullException(nameof(project));

        // 优先使用项目记录的存储路径，如果没有或不存在则使用默认路径
        var projectDir = !string.IsNullOrEmpty(project.StoragePath) && Directory.Exists(project.StoragePath)
            ? project.StoragePath
            : GetProjectDirectory(project.Id);

        SaveProjectTo(project, projectDir);
    }

    /// <summary>
    /// 保存项目到指定路径
    /// </summary>
    private void SaveProjectTo(Project project, string projectDir)
    {
        if (project == null)
            throw new ArgumentNullException(nameof(project));

        project.ModifiedTime = DateTime.Now;
        Directory.CreateDirectory(projectDir);

        // 更新项目存储路径
        project.StoragePath = projectDir;

        // 保存项目元数据
        var projectFilePath = Path.Combine(projectDir, "project.json");
        SaveToFile(project, projectFilePath);

        // 保存程序
        var programFilePath = Path.Combine(projectDir, "program.json");
        SaveToFile(project.Program, programFilePath);

        // 保存配方
        var recipesDir = Path.Combine(projectDir, "recipes");
        Directory.CreateDirectory(recipesDir);
        foreach (var recipe in project.Recipes)
        {
            var recipeFilePath = Path.Combine(recipesDir, $"{SanitizeFileName(recipe.Name)}.json");
            SaveToFile(recipe, recipeFilePath);
        }

        _projects[project.Id] = project;
        _logger.Log(LogLevel.Success, $"保存项目: {project.Name}", "ProjectManager");
    }

    /// <summary>
    /// 加载项目
    /// </summary>
    public Project? LoadProject(string projectId)
    {
        if (_projects.TryGetValue(projectId, out var cachedProject))
            return cachedProject;

        var projectDir = GetProjectDirectory(projectId);
        if (!Directory.Exists(projectDir))
            return null;

        try
        {
            // 加载项目元数据
            var projectFilePath = Path.Combine(projectDir, "project.json");
            var project = LoadFromFile<Project>(projectFilePath);
            if (project == null)
                return null;

            // 加载程序
            var programFilePath = Path.Combine(projectDir, "program.json");
            var program = LoadFromFile<InspectionProgram>(programFilePath);
            if (program != null)
                project.Program = program;

            // 加载配方
            var recipesDir = Path.Combine(projectDir, "recipes");
            if (Directory.Exists(recipesDir))
            {
                var recipeFiles = Directory.GetFiles(recipesDir, "*.json");
                project.Recipes.Clear();

                foreach (var recipeFile in recipeFiles)
                {
                    var recipe = LoadFromFile<InspectionRecipe>(recipeFile);
                    if (recipe != null)
                    {
                        recipe.ProjectId = project.Id;
                        project.Recipes.Add(recipe);
                    }
                }
            }

            _projects[project.Id] = project;
            _logger.Log(LogLevel.Info, $"加载项目: {project.Name}", "ProjectManager");

            return project;
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Error, $"加载项目失败: {projectId}, 错误: {ex.Message}", "ProjectManager", ex);
            return null;
        }
    }

    /// <summary>
    /// 删除项目
    /// </summary>
    public void DeleteProject(string projectId)
    {
        var projectDir = GetProjectDirectory(projectId);
        if (Directory.Exists(projectDir))
        {
            Directory.Delete(projectDir, true);
        }

        _projects.Remove(projectId);
        _logger.Log(LogLevel.Warning, $"删除项目: {projectId}", "ProjectManager");
    }

    /// <summary>
    /// 项目另存为
    /// </summary>
    /// <remarks>
    /// 利用现有的 Project.Clone() 方法实现深度复制。
    /// 支持自定义保存路径。
    /// </remarks>
    public Project? SaveProjectAs(string projectId, string newName, string? newDescription = null, string? customPath = null)
    {
        var sourceProject = LoadProject(projectId);
        if (sourceProject == null)
        {
            _logger.Log(LogLevel.Error, $"项目另存为失败: 项目不存在 {projectId}", "ProjectManager");
            return null;
        }

        try
        {
            // 检查重名
            if (IsProjectNameExists(newName))
            {
                _logger.Log(LogLevel.Warning, $"项目另存为失败: 项目名称已存在 {newName}", "ProjectManager");
                return null;
            }

            // 深度复制项目
            var newProject = sourceProject.Clone();
            newProject.Name = newName;
            newProject.Description = newDescription ?? sourceProject.Description;
            newProject.Id = Guid.NewGuid().ToString();
            newProject.CreatedTime = DateTime.Now;
            newProject.ModifiedTime = DateTime.Now;

            // 保存到自定义路径或默认路径
            var projectDir = customPath ?? GetProjectDirectory(newProject.Id);
            SaveProjectTo(newProject, projectDir);
            _projects[newProject.Id] = newProject;

            _logger.Log(LogLevel.Success, $"项目另存为成功: {sourceProject.Name} -> {newProject.Name}", "ProjectManager");
            return newProject;
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Error, $"项目另存为失败: {ex.Message}", "ProjectManager", ex);
            return null;
        }
    }

    /// <summary>
    /// 配方另存为
    /// </summary>
    /// <remarks>
    /// 复制配方到同一项目，支持自定义名称和描述。
    /// </remarks>
    public InspectionRecipe? SaveRecipeAs(string projectId, string recipeName, 
                                        string newRecipeName, string? newDescription = null)
    {
        var project = LoadProject(projectId);
        if (project == null)
        {
            _logger.Log(LogLevel.Error, $"配方另存为失败: 项目不存在 {projectId}", "ProjectManager");
            return null;
        }

        var sourceRecipe = project.GetRecipeByName(recipeName);
        if (sourceRecipe == null)
        {
            _logger.Log(LogLevel.Error, $"配方另存为失败: 配方不存在 {recipeName}", "ProjectManager");
            return null;
        }

        try
        {
            // 检查重名
            if (IsRecipeNameExists(project, newRecipeName))
            {
                _logger.Log(LogLevel.Warning, $"配方另存为失败: 配方名称已存在 {newRecipeName}", "ProjectManager");
                return null;
            }

            // 创建新配方（深度复制）
            var newRecipe = new InspectionRecipe
            {
                Name = newRecipeName,
                Description = newDescription ?? sourceRecipe.Description,
                ProjectId = project.Id,
                Device = sourceRecipe.Device,
                CreatedTime = DateTime.Now,
                ModifiedTime = DateTime.Now
            };

            // 复制节点参数
            foreach (var param in sourceRecipe.NodeParams)
            {
                newRecipe.NodeParams[param.Key] = param.Value.Clone();
            }

            // 复制全局变量
            foreach (var variable in sourceRecipe.GlobalVariables)
            {
                newRecipe.GlobalVariables[variable.Key] = new GlobalVariable
                {
                    Name = variable.Value.Name,
                    Value = variable.Value.Value,
                    Type = variable.Value.Type,
                    Description = variable.Value.Description
                };
            }

            // 添加到项目
            project.Recipes.Add(newRecipe);
            SaveProject(project);

            _logger.Log(LogLevel.Success, $"配方另存为成功: {sourceRecipe.Name} -> {newRecipe.Name}", "ProjectManager");
            return newRecipe;
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Error, $"配方另存为失败: {ex.Message}", "ProjectManager", ex);
            return null;
        }
    }

    /// <summary>
    /// 复制配方
    /// </summary>
    public InspectionRecipe DuplicateRecipe(string projectId, string recipeName)
    {
        var project = LoadProject(projectId);
        if (project == null)
            throw new ArgumentException($"项目不存在: {projectId}");

        var sourceRecipe = project.GetRecipeByName(recipeName);
        if (sourceRecipe == null)
            throw new ArgumentException($"配方不存在: {recipeName}");

        // 生成唯一的配方名称
        var uniqueName = GenerateUniqueRecipeName(project, sourceRecipe.Name + "_副本");

        // 创建新配方（深度复制）
        var newRecipe = new InspectionRecipe
        {
            Name = uniqueName,
            Description = sourceRecipe.Description,
            ProjectId = project.Id,
            Device = sourceRecipe.Device,
            CreatedTime = DateTime.Now,
            ModifiedTime = DateTime.Now
        };

        // 复制节点参数
        foreach (var param in sourceRecipe.NodeParams)
        {
            newRecipe.NodeParams[param.Key] = param.Value.Clone();
        }

        // 复制全局变量
        foreach (var variable in sourceRecipe.GlobalVariables)
        {
            newRecipe.GlobalVariables[variable.Key] = new GlobalVariable
            {
                Name = variable.Value.Name,
                Value = variable.Value.Value,
                Type = variable.Value.Type,
                Description = variable.Value.Description
            };
        }

        // 添加到项目
        project.Recipes.Add(newRecipe);
        SaveProject(project);

        _logger.Log(LogLevel.Success, $"复制配方成功: {sourceRecipe.Name} -> {newRecipe.Name}", "ProjectManager");
        return newRecipe;
    }

    /// <summary>
    /// 重命名配方
    /// </summary>
    /// <remarks>
    /// 重命名配方时，会自动删除旧的配方文件。
    /// </remarks>
    public bool RenameRecipe(string projectId, string recipeId, string newName)
    {
        var project = LoadProject(projectId);
        if (project == null)
        {
            _logger.Log(LogLevel.Error, $"重命名配方失败: 项目不存在 {projectId}", "ProjectManager");
            return false;
        }

        var oldName = project.RenameRecipe(recipeId, newName);
        if (oldName == null)
        {
            _logger.Log(LogLevel.Warning, $"重命名配方失败: 配方不存在 {recipeId}", "ProjectManager");
            return false;
        }

        // 获取项目路径
        var projectPath = GetProjectPath(projectId);
        if (string.IsNullOrEmpty(projectPath))
        {
            _logger.Log(LogLevel.Error, $"重命名配方失败: 项目路径不存在 {projectId}", "ProjectManager");
            return false;
        }

        // 删除旧的配方文件
        var recipesDir = Path.Combine(projectPath, "recipes");
        var oldRecipeFile = Path.Combine(recipesDir, $"{SanitizeFileName(oldName)}.json");

        if (File.Exists(oldRecipeFile) && !string.Equals(oldName, newName, StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                File.Delete(oldRecipeFile);
                _logger.Log(LogLevel.Info, $"删除旧配方文件: {oldRecipeFile}", "ProjectManager");
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Warning, $"删除旧配方文件失败: {ex.Message}", "ProjectManager");
            }
        }

        // 保存项目
        SaveProject(project);

        _logger.Log(LogLevel.Success, $"重命名配方成功: {oldName} -> {newName}", "ProjectManager");
        return true;
    }

    /// <summary>
    /// 获取所有项目
    /// </summary>
    public IReadOnlyList<Project> GetAllProjects()
    {
        return _projects.Values.ToList();
    }

    /// <summary>
    /// 加载所有项目
    /// </summary>
    private void LoadAllProjects()
    {
        if (!Directory.Exists(_projectsDirectory))
            return;

        var projectDirs = Directory.GetDirectories(_projectsDirectory);
        foreach (var projectDir in projectDirs)
        {
            var projectFilePath = Path.Combine(projectDir, "project.json");
            if (!File.Exists(projectFilePath))
                continue;

            try
            {
                var project = LoadFromFile<Project>(projectFilePath);
                if (project != null && !string.IsNullOrEmpty(project.Id))
                {
                    // 设置项目的实际存储路径（因为StoragePath不序列化到JSON）
                    project.StoragePath = projectDir;

                    // 加载程序
                    var programFilePath = Path.Combine(projectDir, "program.json");
                    var program = LoadFromFile<InspectionProgram>(programFilePath);
                    if (program != null)
                        project.Program = program;

                    // 加载配方
                    var recipesDir = Path.Combine(projectDir, "recipes");
                    if (Directory.Exists(recipesDir))
                    {
                        var recipeFiles = Directory.GetFiles(recipesDir, "*.json");
                        project.Recipes.Clear();  // 先清空，避免重复加载
                        foreach (var recipeFile in recipeFiles)
                        {
                            var recipe = LoadFromFile<InspectionRecipe>(recipeFile);
                            if (recipe != null)
                            {
                                recipe.ProjectId = project.Id;
                                project.Recipes.Add(recipe);
                            }
                        }
                    }

                    _projects[project.Id] = project;
                }
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, $"加载项目失败: {projectDir}, 错误: {ex.Message}", "ProjectManager");
            }
        }

        _logger.Log(LogLevel.Info, $"加载了 {_projects.Count} 个项目", "ProjectManager");
    }

    // ====================================================================
    // 项目加载扩展（混合模式）
    // ====================================================================

    /// <summary>
    /// 加载单个项目（从任意路径）
    /// </summary>
    public Project? LoadProjectFromPath(string projectPath)
    {
        try
        {
            var projectFilePath = Path.Combine(projectPath, "project.json");
            if (!File.Exists(projectFilePath))
                return null;

            var project = LoadFromFile<Project>(projectFilePath);
            if (project == null || string.IsNullOrEmpty(project.Id))
                return null;

            project.StoragePath = projectPath;

            var programFilePath = Path.Combine(projectPath, "program.json");
            var program = LoadFromFile<InspectionProgram>(programFilePath);
            if (program != null)
                project.Program = program;

            var recipesDir = Path.Combine(projectPath, "recipes");
            if (Directory.Exists(recipesDir))
            {
                var recipeFiles = Directory.GetFiles(recipesDir, "*.json");
                project.Recipes.Clear();
                foreach (var recipeFile in recipeFiles)
                {
                    var recipe = LoadFromFile<InspectionRecipe>(recipeFile);
                    if (recipe != null)
                    {
                        recipe.ProjectId = project.Id;
                        project.Recipes.Add(recipe);
                    }
                }
            }

            _projects[project.Id] = project;
            _logger.Log(LogLevel.Success, $"加载项目: {project.Name} -> {projectPath}", "ProjectManager");
            return project;
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Error, $"加载项目失败: {projectPath}, 错误: {ex.Message}", "ProjectManager");
            return null;
        }
    }

    /// <summary>
    /// 扫描工作空间目录下的所有项目
    /// </summary>
    public List<Project> ScanWorkspace(string workspacePath)
    {
        var projects = new List<Project>();

        if (!Directory.Exists(workspacePath))
            return projects;

        try
        {
            var subDirs = Directory.GetDirectories(workspacePath);
            foreach (var subDir in subDirs)
            {
                var projectFilePath = Path.Combine(subDir, "project.json");
                if (!File.Exists(projectFilePath))
                    continue;

                var project = LoadProjectFromPath(subDir);
                if (project != null)
                    projects.Add(project);
            }

            _logger.Log(LogLevel.Info, $"扫描工作空间 {workspacePath}，发现 {projects.Count} 个项目", "ProjectManager");
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Error, $"扫描工作空间失败: {workspacePath}, 错误: {ex.Message}", "ProjectManager");
        }

        return projects;
    }

    // ====================================================================
    // 当前项目和配方管理
    // ====================================================================

    /// <summary>
    /// 设置当前项目
    /// </summary>
    public void SetCurrentProject(string projectId, string? recipeName = null)
    {
        var project = LoadProject(projectId);
        if (project == null)
            throw new ArgumentException($"项目不存在: {projectId}");

        CurrentProject = project;

        // 加载配方
        if (recipeName != null)
        {
            CurrentRecipe = project.GetRecipeByName(recipeName);
        }
        else
        {
            // 尝试加载最近使用的配方
            var recentRecipe = _runtimeConfig.GetRecentRecipe(projectId);
            if (recentRecipe != null)
            {
                CurrentRecipe = project.GetRecipeByName(recentRecipe);
            }

            // 如果没有最近使用的配方，使用第一个配方
            if (CurrentRecipe == null && project.Recipes.Count > 0)
            {
                CurrentRecipe = project.Recipes[0];
            }
        }

        // 添加到最近打开
        _runtimeConfig.AddRecentProject(
            project.Id,
            project.Name,
            project.StoragePath ?? string.Empty,
            CurrentRecipe?.Name
        );

        // 更新运行时配置
        _runtimeConfig.SetCurrentProject(projectId, CurrentRecipe?.Name ?? "standard");
        SaveRuntimeConfig();

        _logger.Log(LogLevel.Info, $"切换项目: {project.Name}, 配方: {CurrentRecipe?.Name ?? "无"}", "ProjectManager");
    }

    /// <summary>
    /// 设置当前配方
    /// </summary>
    public void SetCurrentRecipe(string recipeName)
    {
        if (CurrentProject == null)
            throw new InvalidOperationException("没有当前项目");

        var recipe = CurrentProject.GetRecipeByName(recipeName);
        if (recipe == null)
            throw new ArgumentException($"配方不存在: {recipeName}");

        CurrentRecipe = recipe;

        // 更新运行时配置
        _runtimeConfig.SetCurrentRecipe(recipeName);
        SaveRuntimeConfig();

        _logger.Log(LogLevel.Info, $"切换配方: {recipeName}", "ProjectManager");
    }

    /// <summary>
    /// 创建执行上下文
    /// </summary>
    public RunContext? CreateRunContext()
    {
        if (CurrentProject == null)
            return null;

        return RunContext.Create(CurrentProject, CurrentRecipe);
    }

    /// <summary>
    /// 创建执行上下文（指定配方）
    /// </summary>
    public RunContext? CreateRunContext(string recipeName)
    {
        if (CurrentProject == null)
            return null;

        return RunContext.Create(CurrentProject, recipeName);
    }

    // ====================================================================
    // 运行时配置管理
    // ====================================================================

    /// <summary>
    /// 加载运行时配置
    /// </summary>
    private void LoadRuntimeConfig()
    {
        var configFilePath = Path.Combine(_solutionDirectory, "config.json");
        if (!File.Exists(configFilePath))
        {
            _runtimeConfig = new RuntimeConfig();
            return;
        }

        try
        {
            _runtimeConfig = LoadFromFile<RuntimeConfig>(configFilePath) ?? new RuntimeConfig();
            _logger.Log(LogLevel.Info, "加载运行时配置", "ProjectManager");
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Error, $"加载运行时配置失败: {ex.Message}", "ProjectManager", ex);
            _runtimeConfig = new RuntimeConfig();
        }
    }

    /// <summary>
    /// 保存运行时配置
    /// </summary>
    public void SaveRuntimeConfig()
    {
        var configFilePath = Path.Combine(_solutionDirectory, "config.json");
        SaveToFile(_runtimeConfig, configFilePath);
        _logger.Log(LogLevel.Info, "保存运行时配置", "ProjectManager");
    }

    // ====================================================================
    // 辅助方法
    // ====================================================================

    /// <summary>
    /// 获取项目路径
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <returns>项目目录路径，如果项目不存在则返回null</returns>
    public string? GetProjectPath(string projectId)
    {
        // 优先使用项目记录的存储路径
        if (_projects.TryGetValue(projectId, out var project))
        {
            // 优先使用 StoragePath
            if (!string.IsNullOrEmpty(project.StoragePath) && Directory.Exists(project.StoragePath))
            {
                return project.StoragePath;
            }

            // StoragePath 为空或不存在，使用默认路径
            var defaultPath = GetProjectDirectory(projectId);
            if (Directory.Exists(defaultPath))
            {
                return defaultPath;
            }
        }

        return null;
    }

    private string GetProjectDirectory(string projectId)
    {
        if (_projects.TryGetValue(projectId, out var project))
        {
            // 使用项目名称生成唯一目录名
            return Path.Combine(_projectsDirectory, GenerateUniqueProjectDirectoryName(project.Name));
        }
        
        // 降级：使用项目ID
        return Path.Combine(_projectsDirectory, projectId);
    }

    /// <summary>
    /// 生成唯一的项目目录名（基于项目名称）
    /// </summary>
    private string GenerateUniqueProjectDirectoryName(string projectName)
    {
        // 1. 清理项目名称中的非法字符
        var sanitizedName = SanitizeFileName(projectName);
        var targetDir = Path.Combine(_projectsDirectory, sanitizedName);
        
        // 2. 如果目录不存在，直接使用
        if (!Directory.Exists(targetDir))
        {
            return sanitizedName;
        }
        
        // 3. 检查是否已存在同名的项目
        var existingProject = _projects.Values.FirstOrDefault(p => 
            p.Name.Equals(projectName, StringComparison.OrdinalIgnoreCase));
        
        if (existingProject != null)
        {
            // 是同一个项目，直接返回
            return sanitizedName;
        }
        
        // 4. 生成唯一名称：name_2, name_3, ...
        int counter = 2;
        while (true)
        {
            var uniqueName = $"{sanitizedName}_{counter}";
            var uniqueDir = Path.Combine(_projectsDirectory, uniqueName);
            
            // 目录不存在，可以使用
            if (!Directory.Exists(uniqueDir))
            {
                return uniqueName;
            }
            
            // 目录存在，检查是否是同一个项目
            existingProject = _projects.Values.FirstOrDefault(p => 
                p.Name.Equals(uniqueName, StringComparison.OrdinalIgnoreCase));
            
            // 没有同名项目，可以使用这个目录名
            if (existingProject == null)
            {
                return uniqueName;
            }
            
            // 继续尝试下一个序号
            counter++;
        }
    }

    /// <summary>
    /// 检查项目名称是否已存在
    /// </summary>
    private bool IsProjectNameExists(string name)
    {
        return _projects.Values.Any(p => p.Name == name);
    }

    /// <summary>
    /// 检查配方名称是否已存在（在指定项目中）
    /// </summary>
    private bool IsRecipeNameExists(Project project, string name)
    {
        return project.Recipes.Any(r => r.Name == name);
    }

    /// <summary>
    /// 生成唯一配方名称（在指定项目内）
    /// </summary>
    private string GenerateUniqueRecipeName(Project project, string baseName)
    {
        if (!IsRecipeNameExists(project, baseName))
            return baseName;

        int counter = 1;
        while (IsRecipeNameExists(project, $"{baseName}_{counter}"))
        {
            counter++;
        }

        return $"{baseName}_{counter}";
    }

    private string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", fileName.Split(invalidChars));
        return sanitized;
    }

    /// <summary>
    /// 重命名项目目录（使用项目名称）
    /// </summary>
    public bool RenameProjectDirectory(string projectId)
    {
        if (!_projects.TryGetValue(projectId, out var project))
        {
            _logger.Log(LogLevel.Warning, $"项目不存在: {projectId}", "ProjectManager");
            return false;
        }

        var currentDir = project.StoragePath;
        var newDir = Path.Combine(_projectsDirectory, GenerateUniqueProjectDirectoryName(project.Name));
        
        if (currentDir == newDir)
            return true;  // 已经是正确的名称
        
        if (Directory.Exists(newDir))
        {
            _logger.Log(LogLevel.Warning, $"目标目录已存在: {newDir}", "ProjectManager");
            return false;
        }
        
        try
        {
            Directory.Move(currentDir, newDir);
            project.StoragePath = newDir;
            SaveProject(project);
            
            _logger.Log(LogLevel.Success, $"重命名项目目录: {currentDir} -> {newDir}", "ProjectManager");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Error, $"重命名项目目录失败: {ex.Message}", "ProjectManager", ex);
            return false;
        }
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
