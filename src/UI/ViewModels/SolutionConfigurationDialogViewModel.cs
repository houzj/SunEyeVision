using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using SunEyeVision.Workflow;
using SunEyeVision.UI.ViewModels;
using SunEyeVision.UI.Views.Windows;

namespace SunEyeVision.UI.ViewModels
{
/// <summary>
/// 解决方案配置对话框 ViewModel
/// </summary>
/// <remarks>
/// 职责：
/// 1. 管理项目列表
/// 2. 管理配方列表
/// 3. 提供命令（新建、打开、删除、启动）
/// 4. 与 ProjectManager 交互
/// </remarks>
public class SolutionConfigurationDialogViewModel : ViewModelBase
{
    private readonly ProjectManager _projectManager;
    private Project? _selectedProject;
    private InspectionRecipe? _selectedRecipe;
    private bool _skipStartupConfig;
    private string _statusMessage = string.Empty;

    /// <summary>
    /// 项目列表
    /// </summary>
    public ObservableCollection<Project> Projects { get; set; } = new();

    /// <summary>
    /// 当前选中的项目
    /// </summary>
    public Project? SelectedProject
    {
        get => _selectedProject;
        set
        {
            if (SetProperty(ref _selectedProject, value, "选中项目"))
            {
                // 更新配方列表
                UpdateRecipesList();
                OnPropertyChanged(nameof(HasSelectedProject));
                OnPropertyChanged(nameof(ProjectInfo));
                OnPropertyChanged(nameof(ProjectStoragePath));
            }
        }
    }

    /// <summary>
    /// 配方列表
    /// </summary>
    public ObservableCollection<InspectionRecipe> Recipes { get; set; } = new();

    /// <summary>
    /// 当前选中的配方
    /// </summary>
    public InspectionRecipe? SelectedRecipe
    {
        get => _selectedRecipe;
        set
        {
            if (SetProperty(ref _selectedRecipe, value, "选中配方"))
            {
                OnPropertyChanged(nameof(HasSelectedRecipe));
                OnPropertyChanged(nameof(RecipeStoragePath));
            }
        }
    }

    /// <summary>
    /// 启动时跳过配置界面
    /// </summary>
    public bool SkipStartupConfig
    {
        get => _skipStartupConfig;
        set => SetProperty(ref _skipStartupConfig, value, "启动时跳过配置界面");
    }

    /// <summary>
    /// 状态消息
    /// </summary>
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    /// <summary>
    /// 是否有选中的项目
    /// </summary>
    public bool HasSelectedProject => SelectedProject != null;

    /// <summary>
    /// 是否有选中的配方
    /// </summary>
    public bool HasSelectedRecipe => SelectedRecipe != null;

    /// <summary>
    /// 项目信息（只读部分）
    /// </summary>
    public string ProjectInfo => SelectedProject != null
        ? $"创建时间: {SelectedProject.CreatedTime:yyyy-MM-dd HH:mm:ss}\n" +
          $"修改时间: {SelectedProject.ModifiedTime:yyyy-MM-dd HH:mm:ss}"
        : "请选择项目";

    /// <summary>
    /// 项目存储路径
    /// </summary>
    public string ProjectStoragePath => SelectedProject != null
        ? _projectManager.GetProjectPath(SelectedProject.Id) ?? "未知路径"
        : string.Empty;

    /// <summary>
    /// 配方存储路径
    /// </summary>
    public string RecipeStoragePath => SelectedRecipe != null
        ? GetRecipeStoragePath()
        : string.Empty;

    /// <summary>
    /// 获取配方存储路径
    /// </summary>
    private string GetRecipeStoragePath()
    {
        if (SelectedRecipe == null || SelectedProject == null)
            return string.Empty;

        var projectPath = _projectManager.GetProjectPath(SelectedProject.Id);
        if (string.IsNullOrEmpty(projectPath))
            return "未知路径";

        return Path.Combine(projectPath, "recipes", $"{SelectedRecipe.Name}.json");
    }

    /// <summary>
    /// 新建项目命令
    /// </summary>
    public ICommand NewProjectCommand { get; }

    /// <summary>
    /// 打开项目命令
    /// </summary>
    public ICommand OpenProjectCommand { get; }

    /// <summary>
    /// 删除项目命令
    /// </summary>
    public ICommand DeleteProjectCommand { get; }

    /// <summary>
    /// 项目另存为命令
    /// </summary>
    public ICommand SaveProjectAsCommand { get; }

    /// <summary>
    /// 配方另存为命令
    /// </summary>
    public ICommand SaveRecipeAsCommand { get; }

    /// <summary>
    /// 添加配方命令
    /// </summary>
    public ICommand AddRecipeCommand { get; }

    /// <summary>
    /// 复制配方命令
    /// </summary>
    public ICommand DuplicateRecipeCommand { get; }

    /// <summary>
    /// 删除配方命令
    /// </summary>
    public ICommand DeleteRecipeCommand { get; }

    /// <summary>
    /// 启动命令
    /// </summary>
    public ICommand LaunchCommand { get; }

    /// <summary>
    /// 跳过配置命令
    /// </summary>
    public ICommand SkipCommand { get; }

    /// <summary>
    /// 结果（成功启动的项目和配方）
    /// </summary>
    public (Project? Project, InspectionRecipe? Recipe)? LaunchResult { get; private set; }

    /// <summary>
    /// 是否成功启动
    /// </summary>
    public bool IsLaunched { get; private set; }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="projectManager">项目管理器</param>
    /// <param name="preselectProjectId">预选中的项目ID</param>
    /// <param name="preselectRecipeName">预选中的配方名称</param>
    public SolutionConfigurationDialogViewModel(ProjectManager projectManager, string? preselectProjectId = null, string? preselectRecipeName = null)
    {
        _projectManager = projectManager ?? throw new ArgumentNullException(nameof(projectManager));

        // 初始化命令
        NewProjectCommand = new RelayCommand(NewProject, CanNewProject);
        OpenProjectCommand = new RelayCommand(OpenProject, CanOpenProject);
        DeleteProjectCommand = new RelayCommand(DeleteProject, CanDeleteProject);
        SaveProjectAsCommand = new RelayCommand(SaveProjectAs, CanSaveProjectAs);
        AddRecipeCommand = new RelayCommand(AddRecipe, CanAddRecipe);
        DuplicateRecipeCommand = new RelayCommand(DuplicateRecipe, CanDuplicateRecipe);
        DeleteRecipeCommand = new RelayCommand(DeleteRecipe, CanDeleteRecipe);
        SaveRecipeAsCommand = new RelayCommand(SaveRecipeAs, CanSaveRecipeAs);
        LaunchCommand = new RelayCommand(Launch, CanLaunch);
        SkipCommand = new RelayCommand(Skip);

        // 加载配置
        LoadConfiguration();

        // 加载项目列表
        LoadProjects();

        // 预选项目
        PreselectItems(preselectProjectId, preselectRecipeName);
    }

    /// <summary>
    /// 加载配置
    /// </summary>
    private void LoadConfiguration()
    {
        try
        {
            var config = _projectManager.RuntimeConfig;
            SkipStartupConfig = config.SkipStartupConfig;
            LogInfo($"加载运行时配置: SkipStartupConfig = {SkipStartupConfig}");
        }
        catch (Exception ex)
        {
            LogError($"加载运行时配置失败: {ex.Message}", null, ex);
        }
    }

    /// <summary>
    /// 加载项目列表
    /// </summary>
    private void LoadProjects()
    {
        try
        {
            Projects.Clear();
            // TODO: 需要添加 ProjectManager.LoadAllProjects() 公共方法
            // var projects = _projectManager.LoadAllProjects();

            // 临时实现：使用构造函数中已加载的项目
            var projects = _projectManager.Projects;

            foreach (var project in projects)
            {
                Projects.Add(project);
            }

            LogInfo($"加载项目列表成功，共 {Projects.Count} 个项目");
        }
        catch (Exception ex)
        {
            LogError($"加载项目列表失败: {ex.Message}", null, ex);
            StatusMessage = "加载项目列表失败";
        }
    }

    /// <summary>
    /// 更新配方列表
    /// </summary>
    private void UpdateRecipesList()
    {
        try
        {
            Recipes.Clear();

            if (SelectedProject != null)
            {
                foreach (var recipe in SelectedProject.Recipes)
                {
                    Recipes.Add(recipe);
                }

                LogInfo($"更新配方列表成功，共 {Recipes.Count} 个配方");
            }
        }
        catch (Exception ex)
        {
            LogError($"更新配方列表失败: {ex.Message}", null, ex);
        }
    }

    /// <summary>
    /// 预选项目
    /// </summary>
    private void PreselectItems(string? preselectProjectId, string? preselectRecipeName)
    {
        if (!string.IsNullOrEmpty(preselectProjectId))
        {
            SelectedProject = Projects.FirstOrDefault(p => p.Id == preselectProjectId);

            if (!string.IsNullOrEmpty(preselectRecipeName) && SelectedProject != null)
            {
                SelectedRecipe = Recipes.FirstOrDefault(r => r.Name == preselectRecipeName);
            }
        }
    }

    /// <summary>
    /// 新建项目
    /// </summary>
    private void NewProject()
    {
        try
        {
            LogInfo("开始新建项目");

            // 获取默认路径
            var defaultPath = _projectManager.ProjectsDirectory;

            // 打开新建项目对话框
            var dialog = new NewProjectDialog(defaultPath);
            var result = dialog.ShowDialog();

            if (result == true)
            {
                // 获取用户输入
                string projectName = dialog.ProjectName;
                string projectPath = dialog.ProjectPath;
                string description = dialog.Description ?? string.Empty;

                // 创建项目（使用自定义路径）
                var newProject = _projectManager.CreateProject(
                    projectName,
                    description,
                    projectPath);

                // 直接添加到项目列表（避免重新加载导致StoragePath丢失）
                Projects.Add(newProject);

                // 选中新项目
                SelectedProject = newProject;

                LogSuccess($"新建项目成功: {newProject.Name} ({newProject.Id})");
                StatusMessage = $"新建项目成功: {newProject.Name}";
            }
            else
            {
                LogInfo("取消新建项目");
            }
        }
        catch (Exception ex)
        {
            LogError($"新建项目失败: {ex.Message}", "新建项目", ex);
            StatusMessage = "新建项目失败";
            MessageBox.Show($"新建项目失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 生成唯一项目名称
    /// </summary>
    private string GenerateUniqueProjectName(string baseName)
    {
        var existingNames = Projects.Select(p => p.Name).ToHashSet();

        if (!existingNames.Contains(baseName))
            return baseName;

        int counter = 1;
        while (existingNames.Contains($"{baseName}_{counter}"))
        {
            counter++;
        }

        return $"{baseName}_{counter}";
    }

    /// <summary>
    /// 是否可以新建项目
    /// </summary>
    private bool CanNewProject() => true;

    /// <summary>
    /// 打开项目
    /// </summary>
    private void OpenProject()
    {
        try
        {
            LogInfo("打开项目命令（功能待实现）");
            StatusMessage = "打开项目功能待实现";
        }
        catch (Exception ex)
        {
            LogError($"打开项目失败: {ex.Message}", null, ex);
        }
    }

    /// <summary>
    /// 是否可以打开项目
    /// </summary>
    private bool CanOpenProject() => true;

    /// <summary>
    /// 删除项目
    /// </summary>
    private void DeleteProject()
    {
        try
        {
            if (SelectedProject == null)
            {
                LogWarning("未选择项目，无法删除");
                return;
            }

            LogInfo($"删除项目: {SelectedProject.Name} ({SelectedProject.Id})");

            _projectManager.DeleteProject(SelectedProject.Id);

            // 刷新项目列表
            LoadProjects();

            // 清除选择
            SelectedProject = null;
            SelectedRecipe = null;

            LogSuccess($"删除项目成功: {SelectedProject?.Name}");
            StatusMessage = "删除项目成功";
        }
        catch (Exception ex)
        {
            LogError($"删除项目失败: {ex.Message}", null, ex);
            StatusMessage = "删除项目失败";
        }
    }

    /// <summary>
    /// 是否可以删除项目
    /// </summary>
    private bool CanDeleteProject() => SelectedProject != null;

    /// <summary>
    /// 添加配方
    /// </summary>
    private void AddRecipe()
    {
        try
        {
            if (SelectedProject == null)
            {
                LogWarning("未选择项目，无法添加配方");
                return;
            }

            LogInfo($"添加配方到项目: {SelectedProject.Name}");

            // 生成唯一配方名称
            var uniqueRecipeName = GenerateUniqueRecipeName("新配方");

            // 打开新建配方对话框
            var dialog = new NewRecipeDialog(uniqueRecipeName);
            var result = dialog.ShowDialog();

            if (result == true)
            {
                // 获取用户输入
                string recipeName = dialog.RecipeName;
                string description = dialog.Description ?? string.Empty;

                // 检查配方名称是否重复（当前项目内）
                if (IsRecipeNameExistsInProject(SelectedProject, recipeName))
                {
                    MessageBox.Show($"配方名称 '{recipeName}' 已存在，请使用其他名称", "提示",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 添加配方到项目
                var newRecipe = SelectedProject.AddRecipe(recipeName, description);

                // 保存项目
                _projectManager.SaveProject(SelectedProject);

                // 刷新配方列表
                UpdateRecipesList();

                // 选中新配方
                SelectedRecipe = Recipes.FirstOrDefault(r => r.Id == newRecipe.Id);

                LogSuccess($"添加配方成功: {newRecipe.Name}");
                StatusMessage = $"添加配方成功: {newRecipe.Name}";
            }
            else
            {
                LogInfo("取消添加配方");
            }
        }
        catch (Exception ex)
        {
            LogError($"添加配方失败: {ex.Message}", "添加配方", ex);
            StatusMessage = "添加配方失败";
            MessageBox.Show($"添加配方失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 检查配方名称是否存在于项目中
    /// </summary>
    private bool IsRecipeNameExistsInProject(Project project, string recipeName)
    {
        return project.Recipes.Any(r => r.Name == recipeName);
    }

    /// <summary>
    /// 生成唯一配方名称
    /// </summary>
    private string GenerateUniqueRecipeName(string baseName)
    {
        var existingNames = Recipes.Select(r => r.Name).ToHashSet();

        if (!existingNames.Contains(baseName))
            return baseName;

        int counter = 1;
        while (existingNames.Contains($"新配方_{counter}"))
        {
            counter++;
        }

        return $"新配方_{counter}";
    }

    /// <summary>
    /// 是否可以添加配方
    /// </summary>
    private bool CanAddRecipe() => SelectedProject != null;

    /// <summary>
    /// 复制配方
    /// </summary>
    private void DuplicateRecipe()
    {
        try
        {
            if (SelectedRecipe == null)
            {
                LogWarning("未选择配方，无法复制");
                return;
            }

            if (SelectedProject == null)
            {
                LogWarning("未选择项目，无法复制配方");
                return;
            }

            LogInfo($"复制配方: {SelectedRecipe.Name}");

            var newRecipe = _projectManager.DuplicateRecipe(SelectedProject.Id, SelectedRecipe.Name);

            // 刷新配方列表
            UpdateRecipesList();

            // 选中新配方
            SelectedRecipe = Recipes.FirstOrDefault(r => r.Id == newRecipe.Id);

            LogSuccess($"复制配方成功: {newRecipe.Name}");
            StatusMessage = $"复制配方成功: {newRecipe.Name}";
        }
        catch (Exception ex)
        {
            LogError($"复制配方失败: {ex.Message}", null, ex);
            StatusMessage = "复制配方失败";
        }
    }

    /// <summary>
    /// 是否可以复制配方
    /// </summary>
    private bool CanDuplicateRecipe() => SelectedRecipe != null;

    /// <summary>
    /// 删除配方
    /// </summary>
    private void DeleteRecipe()
    {
        try
        {
            if (SelectedRecipe == null)
            {
                LogWarning("未选择配方，无法删除");
                return;
            }

            if (SelectedProject == null)
            {
                LogWarning("未选择项目，无法删除配方");
                return;
            }

            LogInfo($"删除配方: {SelectedRecipe.Name}");

            SelectedProject.RemoveRecipe(SelectedRecipe.Id);

            // 刷新配方列表
            UpdateRecipesList();

            // 清除选择
            SelectedRecipe = null;

            LogSuccess($"删除配方成功: {SelectedRecipe?.Name}");
            StatusMessage = "删除配方成功";
        }
        catch (Exception ex)
        {
            LogError($"删除配方失败: {ex.Message}", null, ex);
            StatusMessage = "删除配方失败";
        }
    }

    /// <summary>
    /// 是否可以删除配方
    /// </summary>
    private bool CanDeleteRecipe() => SelectedRecipe != null;

    /// <summary>
    /// 启动
    /// </summary>
    private void Launch()
    {
        try
        {
            if (SelectedProject == null)
            {
                LogWarning("未选择项目，无法启动");
                StatusMessage = "请先选择项目";
                return;
            }

            if (SelectedRecipe == null)
            {
                LogWarning("未选择配方，无法启动");
                StatusMessage = "请先选择配方";
                return;
            }

            LogInfo($"启动: 项目={SelectedProject.Name}, 配方={SelectedRecipe.Name}");

            // 保存当前项目和配方
            SaveCurrentProject();

            // 保存配置
            SaveConfiguration();

            // 设置当前项目和配方
            _projectManager.SetCurrentProject(SelectedProject.Id, SelectedRecipe.Name);

            // 记录启动结果
            LaunchResult = (SelectedProject, SelectedRecipe);
            IsLaunched = true;

            LogSuccess($"启动成功: 项目={SelectedProject.Name}, 配方={SelectedRecipe.Name}");
            StatusMessage = $"启动成功: {SelectedProject.Name} - {SelectedRecipe.Name}";
        }
        catch (Exception ex)
        {
            LogError($"启动失败: {ex.Message}", null, ex);
            StatusMessage = "启动失败";
            LaunchResult = null;
            IsLaunched = false;
        }
    }

    /// <summary>
    /// 是否可以启动
    /// </summary>
    private bool CanLaunch() => SelectedProject != null && SelectedRecipe != null;

    /// <summary>
    /// 跳过配置
    /// </summary>
    private void Skip()
    {
        try
        {
            LogInfo("跳过配置");

            // 保存当前项目和配方
            SaveCurrentProject();

            // 保存配置
            SaveConfiguration();

            // 记录跳过
            LaunchResult = null;
            IsLaunched = false;

            LogInfo("跳过配置成功");
        }
        catch (Exception ex)
        {
            LogError($"跳过配置失败: {ex.Message}", null, ex);
        }
    }

    /// <summary>
    /// 保存配置
    /// </summary>
    private void SaveConfiguration()
    {
        try
        {
            var config = _projectManager.RuntimeConfig;
            config.SkipStartupConfig = SkipStartupConfig;
            _projectManager.SaveRuntimeConfig();

            LogInfo($"保存配置成功: SkipStartupConfig = {SkipStartupConfig}");
        }
        catch (Exception ex)
        {
            LogError($"保存配置失败: {ex.Message}", null, ex);
        }
    }

    /// <summary>
    /// 保存当前项目和配方
    /// </summary>
    public void SaveCurrentProject()
    {
        try
        {
            // 保存选中的项目（包括修改的名称和描述）
            if (SelectedProject != null)
            {
                _projectManager.SaveProject(SelectedProject);
                LogInfo($"保存项目: {SelectedProject.Name}");
            }
        }
        catch (Exception ex)
        {
            LogError($"保存项目失败: {ex.Message}", null, ex);
        }
    }

    /// <summary>
    /// 项目另存为
    /// </summary>
    private void SaveProjectAs()
    {
        try
        {
            if (SelectedProject == null)
            {
                LogWarning("未选择项目，无法另存为");
                StatusMessage = "请先选择项目";
                return;
            }

            LogInfo($"项目另存为: {SelectedProject.Name}");

            // 弹出项目另存为对话框
            var defaultName = SelectedProject.Name + "_副本";
            var defaultPath = _projectManager.ProjectsDirectory;
            var dialog = new SaveProjectAsDialog(
                defaultName: defaultName,
                defaultPath: defaultPath,
                defaultDescription: SelectedProject.Description);

            var result = dialog.ShowDialog();

            if (result == true)
            {
                var newName = dialog.ProjectName;
                var customPath = dialog.ProjectPath;
                var newDescription = dialog.Description;

                // 利用 ProjectManager 的方法另存为
                var newProject = _projectManager.SaveProjectAs(SelectedProject.Id, newName, newDescription, customPath);

                if (newProject != null)
                {
                    // 刷新项目列表
                    LoadProjects();

                    // 选中新项目
                    SelectedProject = Projects.FirstOrDefault(p => p.Id == newProject.Id);

                    LogSuccess($"项目另存为成功: {newProject.Name} ({newProject.Id})");
                    StatusMessage = $"项目另存为成功: {newProject.Name}";
                }
                else
                {
                    LogError("项目另存为失败", "项目另存为");
                    StatusMessage = "项目另存为失败";
                }
            }
            else
            {
                LogInfo("取消项目另存为");
            }
        }
        catch (Exception ex)
        {
            LogError($"项目另存为失败: {ex.Message}", "项目另存为", ex);
            StatusMessage = "项目另存为失败";
            MessageBox.Show($"项目另存为失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 是否可以另存为项目
    /// </summary>
    private bool CanSaveProjectAs() => SelectedProject != null;

    /// <summary>
    /// 配方另存为
    /// </summary>
    private void SaveRecipeAs()
    {
        try
        {
            if (SelectedRecipe == null || SelectedProject == null)
            {
                LogWarning("未选择配方或项目，无法另存为");
                return;
            }

            LogInfo($"配方另存为: {SelectedRecipe.Name}");

            // 弹出配方另存为对话框
            var dialog = new SaveRecipeAsDialog(
                currentProjectId: SelectedProject.Id,
                currentRecipeName: SelectedRecipe.Name,
                currentDescription: SelectedRecipe.Description,
                projects: Projects.ToList());

            var result = dialog.ShowDialog();

            if (result == true)
            {
                // 获取用户输入
                string targetProjectId = dialog.TargetProjectId;
                string newRecipeName = dialog.RecipeName;
                string newDescription = dialog.Description ?? string.Empty;

                InspectionRecipe? newRecipe = null;

                // 检查是否是同一项目
                if (targetProjectId == SelectedProject.Id)
                {
                    // 同一项目内另存为
                    newRecipe = _projectManager.SaveRecipeAs(
                        SelectedProject.Id,
                        SelectedRecipe.Name,
                        newRecipeName,
                        newDescription);

                    // 刷新配方列表
                    UpdateRecipesList();
                    SelectedRecipe = Recipes.FirstOrDefault(r => r.Id == newRecipe?.Id);
                }
                else
                {
                    // 跨项目另存为
                    var targetProject = Projects.FirstOrDefault(p => p.Id == targetProjectId);
                    if (targetProject != null)
                    {
                        // 深度复制配方
                        newRecipe = CloneRecipe(SelectedRecipe, targetProject.Id, newRecipeName, newDescription);
                        
                        // 添加到目标项目
                        targetProject.Recipes.Add(newRecipe);
                        
                        // 保存目标项目
                        _projectManager.SaveProject(targetProject);

                        // 提示用户
                        MessageBox.Show(
                            $"配方已另存为到项目：{targetProject.Name}\n配方名称：{newRecipe.Name}",
                            "另存为成功",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                }

                if (newRecipe != null)
                {
                    LogSuccess($"配方另存为成功: {SelectedRecipe.Name} -> {newRecipe.Name}");
                    StatusMessage = $"配方另存为成功: {newRecipe.Name}";
                }
                else
                {
                    LogError("配方另存为失败", "配方另存为");
                    StatusMessage = "配方另存为失败";
                }
            }
            else
            {
                LogInfo("取消配方另存为");
            }
        }
        catch (Exception ex)
        {
            LogError($"配方另存为失败: {ex.Message}", "配方另存为", ex);
            StatusMessage = "配方另存为失败";
            MessageBox.Show($"配方另存为失败: {ex.Message}", "错误", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 克隆配方（深度复制）
    /// </summary>
    private InspectionRecipe CloneRecipe(InspectionRecipe sourceRecipe, string targetProjectId, 
                                        string newName, string newDescription)
    {
        var newRecipe = new InspectionRecipe
        {
            Name = newName,
            Description = newDescription,
            ProjectId = targetProjectId,
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
            newRecipe.GlobalVariables[variable.Key] = new SunEyeVision.Workflow.GlobalVariable
            {
                Name = variable.Value.Name,
                Value = variable.Value.Value,
                Type = variable.Value.Type,
                Description = variable.Value.Description
            };
        }

        return newRecipe;
    }

    /// <summary>
    /// 是否可以另存为配方
    /// </summary>
    private bool CanSaveRecipeAs() => SelectedRecipe != null && SelectedProject != null;
}
}
