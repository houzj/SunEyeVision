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
    public Project CreateProject(string name, string productCode, string description = "")
    {
        var project = new Project
        {
            Name = name,
            ProductCode = productCode,
            Description = description,
            CreatedTime = DateTime.Now,
            ModifiedTime = DateTime.Now
        };

        // 添加默认配方
        project.AddRecipe("标准配方", "默认标准配方");

        _projects[project.Id] = project;
        SaveProject(project);

        _logger.Log(LogLevel.Success, $"创建项目: {name} ({productCode})", "ProjectManager");
        return project;
    }

    /// <summary>
    /// 保存项目
    /// </summary>
    public void SaveProject(Project project)
    {
        if (project == null)
            throw new ArgumentNullException(nameof(project));

        project.ModifiedTime = DateTime.Now;

        var projectDir = GetProjectDirectory(project.Id);
        Directory.CreateDirectory(projectDir);

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
    // 迁移工具
    // ====================================================================

    /// <summary>
    /// 从旧 Solution 迁移到新 Project
    /// </summary>
    public Project MigrateFromSolution(Solution solution)
    {
        if (solution == null)
            throw new ArgumentNullException(nameof(solution));

        var project = new Project
        {
            Id = solution.Id,
            Name = solution.Name,
            Description = solution.Description,
            CreatedTime = solution.CreatedAt,
            ModifiedTime = solution.ModifiedAt
        };

        // 迁移工作流定义到程序
        // 注意：这里需要根据实际情况调整
        // 如果 Solution 中有 WorkflowDefinition，需要转换为 InspectionProgram

        // 迁移配方
        foreach (var recipe in solution.Recipes)
        {
            var inspectionRecipe = new InspectionRecipe
            {
                Id = recipe.Id,
                Name = recipe.Name,
                Description = recipe.Description,
                ProjectId = project.Id,
                NodeParams = new Dictionary<string, Plugin.SDK.Execution.Parameters.ToolParameters>(recipe.ParameterMappings),
                CreatedTime = DateTime.Now,
                ModifiedTime = DateTime.Now
            };

            project.Recipes.Add(inspectionRecipe);
        }

        // 迁移全局变量
        if (solution.GlobalVariables != null && solution.GlobalVariables.Count > 0)
        {
            // 将全局变量添加到第一个配方中
            if (project.Recipes.Count > 0)
            {
                project.Recipes[0].GlobalVariables = new Dictionary<string, GlobalVariable>(solution.GlobalVariables);
            }
        }

        _projects[project.Id] = project;
        SaveProject(project);

        _logger.Log(LogLevel.Success, $"迁移解决方案: {solution.Name} -> 项目: {project.Name}", "ProjectManager");
        return project;
    }

    // ====================================================================
    // 辅助方法
    // ====================================================================

    private string GetProjectDirectory(string projectId)
    {
        // 使用项目ID作为目录名
        return Path.Combine(_projectsDirectory, projectId);
    }

    private string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", fileName.Split(invalidChars));
        return sanitized;
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
