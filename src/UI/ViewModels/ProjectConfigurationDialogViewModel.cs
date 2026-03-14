using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using SunEyeVision.Plugin.SDK.Models;
using SunEyeVision.UI.Controls.Helpers;
using SunEyeVision.UI.Views.Windows;
using SunEyeVision.Workflow;

namespace SunEyeVision.UI.ViewModels
{
    /// <summary>
    /// 项目配置对话框 ViewModel
    /// </summary>
    public class ProjectConfigurationDialogViewModel : ViewModelBase
    {
        private readonly ProjectManager _projectManager;
        private readonly string? _preselectProjectId;
        private readonly string? _preselectRecipeName;
        private WorkspaceFolderItem? _selectedWorkspace;
        private Project _selectedProject;
        private InspectionRecipe _selectedRecipe;
        private bool _skipStartupConfig;
        private bool _isLaunched;
        private (Project? Project, InspectionRecipe? Recipe)? _launchResult;
        private Window? _ownerWindow;

        public ProjectConfigurationDialogViewModel(ProjectManager projectManager, string? preselectProjectId = null, string? preselectRecipeName = null)
        {
            _projectManager = projectManager ?? throw new ArgumentNullException(nameof(projectManager));
            _preselectProjectId = preselectProjectId;
            _preselectRecipeName = preselectRecipeName;
            
            InitializeCommands();
            LoadData();
        }

        /// <summary>
        /// 设置所有者窗口
        /// </summary>
        public void SetOwnerWindow(Window window)
        {
            _ownerWindow = window;
        }

        /// <summary>
        /// 启动结果
        /// </summary>
        public (Project? Project, InspectionRecipe? Recipe)? LaunchResult => _launchResult;

        /// <summary>
        /// 是否已启动
        /// </summary>
        public bool IsLaunched => _isLaunched;

        /// <summary>
        /// 保存当前项目和配方
        /// </summary>
        public void SaveCurrentProject()
        {
            if (_selectedProject != null)
            {
                try
                {
                    _projectManager.SaveProject(_selectedProject);
                }
                catch (Exception ex)
                {
                    LogError($"保存项目失败: {ex.Message}");
                }
            }
        }

        #region 工作空间相关

        /// <summary>
        /// 工作空间列表
        /// </summary>
        public ObservableCollection<WorkspaceFolderItem> WorkspaceFolders { get; } = new();

        /// <summary>
        /// 选中的工作空间
        /// </summary>
        public WorkspaceFolderItem? SelectedWorkspace
        {
            get => _selectedWorkspace;
            set => SetProperty(ref _selectedWorkspace, value);
        }

        /// <summary>
        /// 添加工作空间命令
        /// </summary>
        public ICommand AddWorkspaceCommand { get; private set; } = null!;

        /// <summary>
        /// 移除工作空间命令
        /// </summary>
        public ICommand RemoveWorkspaceCommand { get; private set; } = null!;

        /// <summary>
        /// 刷新工作空间命令
        /// </summary>
        public ICommand RefreshWorkspaceCommand { get; private set; } = null!;

        private void InitializeCommands()
        {
            AddWorkspaceCommand = new RelayCommand(AddWorkspace, () => true);
            RemoveWorkspaceCommand = new RelayCommand(RemoveWorkspace, () => CanRemoveWorkspace());
            RefreshWorkspaceCommand = new RelayCommand(RefreshWorkspace, () => true);
            ClearRecentCommand = new RelayCommand(ClearRecent, () => RecentProjects.Count > 0);
            NewProjectCommand = new RelayCommand(NewProject, () => true);
            OpenProjectCommand = new RelayCommand(OpenProject, () => true);
            SaveProjectAsCommand = new RelayCommand(SaveProjectAs, () => CanSaveProjectAs());
            DeleteProjectCommand = new RelayCommand(DeleteProject, () => CanDeleteProject());
            AddRecipeCommand = new RelayCommand(AddRecipe, () => CanAddRecipe());
            DuplicateRecipeCommand = new RelayCommand(DuplicateRecipe, () => CanDuplicateRecipe());
            SaveRecipeAsCommand = new RelayCommand(SaveRecipeAs, () => CanSaveRecipeAs());
            DeleteRecipeCommand = new RelayCommand(DeleteRecipe, () => CanDeleteRecipe());
            SkipCommand = new RelayCommand(Skip, () => true);
            LaunchCommand = new RelayCommand(Launch, () => CanLaunch());
        }

        private void AddWorkspace()
        {
            var selectedPath = FolderBrowserHelper.BrowseForFolder("选择工作空间目录");
            if (!string.IsNullOrEmpty(selectedPath))
            {
                var workspace = new WorkspaceFolderItem
                {
                    Name = Path.GetFileName(selectedPath),
                    Path = selectedPath,
                    ProjectCount = 0
                };

                WorkspaceFolders.Add(workspace);
                _projectManager.RuntimeConfig.AddWorkspaceFolder(selectedPath);
                RefreshWorkspace();
                LogSuccess($"添加工作空间: {selectedPath}");
            }
        }

        private bool CanRemoveWorkspace()
        {
            return SelectedWorkspace != null;
        }

        private void RemoveWorkspace()
        {
            if (SelectedWorkspace == null) return;

            var result = MessageBox.Show(
                $"确定要移除工作空间「{SelectedWorkspace.Name}」吗？",
                "确认移除",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _projectManager.RuntimeConfig.RemoveWorkspaceFolder(SelectedWorkspace.Path);
                WorkspaceFolders.Remove(SelectedWorkspace);
                LogSuccess($"移除工作空间: {SelectedWorkspace.Path}");
            }
        }

        private void RefreshWorkspace()
        {
            LoadWorkspaceFolders();
            LoadProjectsFromWorkspaces();
            LogInfo("刷新工作空间列表");
        }

        #endregion

        #region 最近打开相关

        /// <summary>
        /// 最近打开的项目列表
        /// </summary>
        public ObservableCollection<RecentProjectItem> RecentProjects { get; } = new();

        /// <summary>
        /// 清除最近打开命令
        /// </summary>
        public ICommand ClearRecentCommand { get; private set; } = null!;

        private void ClearRecent()
        {
            var result = MessageBox.Show(
                "确定要清除所有最近打开的记录吗？",
                "确认清除",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                RecentProjects.Clear();
                _projectManager.RuntimeConfig.ClearRecentProjects();
                LogSuccess("清除最近打开记录");
            }
        }

        #endregion

        #region 项目相关

        /// <summary>
        /// 项目列表
        /// </summary>
        public ObservableCollection<Project> Projects { get; } = new();

        /// <summary>
        /// 选中的项目
        /// </summary>
        public Project? SelectedProject
        {
            get => _selectedProject;
            set
            {
                if (SetProperty(ref _selectedProject, value))
                {
                    if (value != null)
                    {
                        LoadRecipes(value);
                    }
                    else
                    {
                        Recipes.Clear();
                    }
                    OnPropertyChanged(nameof(HasSelectedProject));
                    OnPropertyChanged(nameof(ProjectStoragePath));
                    OnPropertyChanged(nameof(ProjectInfo));
                }
            }
        }

        /// <summary>
        /// 是否选中了项目
        /// </summary>
        public bool HasSelectedProject => SelectedProject != null;

        /// <summary>
        /// 项目存储路径
        /// </summary>
        public string ProjectStoragePath => SelectedProject?.StoragePath ?? "未选择项目";

        /// <summary>
        /// 项目信息
        /// </summary>
        public string ProjectInfo
        {
            get
            {
                if (SelectedProject == null) return string.Empty;
                return $"创建时间: {SelectedProject.CreatedTime:yyyy-MM-dd HH:mm}\n" +
                       $"修改时间: {SelectedProject.ModifiedTime:yyyy-MM-dd HH:mm}";
            }
        }

        /// <summary>
        /// 新建项目命令
        /// </summary>
        public ICommand NewProjectCommand { get; private set; } = null!;

        /// <summary>
        /// 打开项目命令
        /// </summary>
        public ICommand OpenProjectCommand { get; private set; } = null!;

        /// <summary>
        /// 另存为项目命令
        /// </summary>
        public ICommand SaveProjectAsCommand { get; private set; } = null!;

        /// <summary>
        /// 删除项目命令
        /// </summary>
        public ICommand DeleteProjectCommand { get; private set; } = null!;

        private void NewProject()
        {
            var defaultPath = WorkspaceFolders.FirstOrDefault()?.Path ?? Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var dialog = new NewProjectDialog(defaultPath);
            var result = dialog.ShowDialog();
            
            if (result == true)
            {
                var project = _projectManager.CreateProject(dialog.ProjectName, dialog.Description ?? "", dialog.ProjectPath);
                Projects.Add(project);
                SelectedProject = project;
                
                // 添加到最近打开
                _projectManager.RuntimeConfig.AddRecentProject(project.Id, project.Name, project.StoragePath, "新配方");
                LoadRecentProjects();
                
                LogSuccess($"创建项目: {project.Name}");
            }
        }

        private void OpenProject()
        {
            // 使用文件夹浏览器选择项目目录
            var initialPath = WorkspaceFolders.FirstOrDefault()?.Path ?? Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var selectedPath = FolderBrowserHelper.BrowseForFolder("选择项目目录", initialPath);
            
            if (!string.IsNullOrEmpty(selectedPath))
            {
                try
                {
                    var projectFile = Directory.GetFiles(selectedPath, "project.json").FirstOrDefault();
                    if (projectFile == null)
                    {
                        MessageBox.Show("所选目录中没有找到项目文件（project.json）", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    var project = _projectManager.LoadProjectFromPath(selectedPath);
                    if (project != null)
                    {
                        if (!Projects.Any(p => p.Id == project.Id))
                        {
                            Projects.Add(project);
                        }
                        SelectedProject = project;
                        
                        // 添加到最近打开
                        _projectManager.RuntimeConfig.AddRecentProject(project.Id, project.Name, project.StoragePath, SelectedRecipe?.Name);
                        LoadRecentProjects();
                        
                        LogSuccess($"打开项目: {project.Name}");
                    }
                }
                catch (Exception ex)
                {
                    LogError($"打开项目失败: {ex.Message}");
                    MessageBox.Show($"打开项目失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private bool CanSaveProjectAs()
        {
            return SelectedProject != null;
        }

        private void SaveProjectAs()
        {
            if (SelectedProject == null) return;

            var defaultName = $"{SelectedProject.Name}_copy";
            var defaultPath = WorkspaceFolders.FirstOrDefault()?.Path ?? Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var defaultDescription = SelectedProject.Description;

            var dialog = new SaveProjectAsDialog(defaultName, defaultPath, defaultDescription);
            var result = dialog.ShowDialog();
            
            if (result == true)
            {
                try
                {
                    var newProject = _projectManager.SaveProjectAs(
                        SelectedProject.Id, 
                        dialog.ProjectName, 
                        dialog.Description ?? SelectedProject.Description,
                        dialog.ProjectPath);
                    
                    if (newProject != null)
                    {
                        Projects.Add(newProject);
                        SelectedProject = newProject;
                        LogSuccess($"项目另存为: {dialog.ProjectName}");
                    }
                }
                catch (Exception ex)
                {
                    LogError($"另存为失败: {ex.Message}");
                    MessageBox.Show($"另存为失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private bool CanDeleteProject()
        {
            return SelectedProject != null;
        }

        private void DeleteProject()
        {
            if (SelectedProject == null) return;

            var result = MessageBox.Show(
                $"确定要删除项目「{SelectedProject.Name}」吗？此操作不可恢复！",
                "确认删除",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var projectToDelete = SelectedProject;
                    _projectManager.DeleteProject(SelectedProject.Id);
                    Projects.Remove(SelectedProject);
                    SelectedProject = null;
                    LogSuccess($"删除项目: {projectToDelete.Name}");
                }
                catch (Exception ex)
                {
                    LogError($"删除项目失败: {ex.Message}");
                    MessageBox.Show($"删除项目失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        #endregion

        #region 配方相关

        /// <summary>
        /// 配方列表
        /// </summary>
        public ObservableCollection<InspectionRecipe> Recipes { get; } = new();

        /// <summary>
        /// 选中的配方
        /// </summary>
        public InspectionRecipe? SelectedRecipe
        {
            get => _selectedRecipe;
            set
            {
                if (SetProperty(ref _selectedRecipe, value))
                {
                    OnPropertyChanged(nameof(HasSelectedRecipe));
                    OnPropertyChanged(nameof(RecipeStoragePath));
                }
            }
        }

        /// <summary>
        /// 是否选中了配方
        /// </summary>
        public bool HasSelectedRecipe => SelectedRecipe != null;

        /// <summary>
        /// 配方存储路径
        /// </summary>
        public string RecipeStoragePath
        {
            get
            {
                if (SelectedProject == null || SelectedRecipe == null)
                    return "未选择配方";
                var projectPath = _projectManager.GetProjectPath(SelectedProject.Id);
                return Path.Combine(projectPath ?? "", "recipes", $"{SanitizeFileName(SelectedRecipe.Name)}.json");
            }
        }

        /// <summary>
        /// 添加配方命令
        /// </summary>
        public ICommand AddRecipeCommand { get; private set; } = null!;

        /// <summary>
        /// 复制配方命令
        /// </summary>
        public ICommand DuplicateRecipeCommand { get; private set; } = null!;

        /// <summary>
        /// 另存为配方命令
        /// </summary>
        public ICommand SaveRecipeAsCommand { get; private set; } = null!;

        /// <summary>
        /// 删除配方命令
        /// </summary>
        public ICommand DeleteRecipeCommand { get; private set; } = null!;

        private bool CanAddRecipe()
        {
            return SelectedProject != null;
        }

        private void AddRecipe()
        {
            if (SelectedProject == null) return;

            var dialog = new NewRecipeDialog();
            var result = dialog.ShowDialog();
            
            if (result == true)
            {
                try
                {
                    var recipe = SelectedProject.AddRecipe(dialog.RecipeName, dialog.Description ?? "");
                    Recipes.Add(recipe);
                    SelectedRecipe = recipe;
                    LogSuccess($"创建配方: {recipe.Name}");
                }
                catch (Exception ex)
                {
                    LogError($"创建配方失败: {ex.Message}");
                    MessageBox.Show($"创建配方失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private bool CanDuplicateRecipe()
        {
            return SelectedRecipe != null && SelectedProject != null;
        }

        private void DuplicateRecipe()
        {
            if (SelectedRecipe == null || SelectedProject == null) return;

            try
            {
                var newRecipe = SelectedProject.DuplicateRecipe(SelectedRecipe.Id, $"{SelectedRecipe.Name}_副本");
                Recipes.Add(newRecipe);
                SelectedRecipe = newRecipe;
                LogSuccess($"复制配方: {newRecipe.Name}");
            }
            catch (Exception ex)
            {
                LogError($"复制配方失败: {ex.Message}");
                MessageBox.Show($"复制配方失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanSaveRecipeAs()
        {
            return SelectedRecipe != null && SelectedProject != null;
        }

        private void SaveRecipeAs()
        {
            if (SelectedRecipe == null || SelectedProject == null) return;

            var dialog = new SaveRecipeAsDialog(
                SelectedProject.Id,
                SelectedRecipe.Name,
                SelectedRecipe.Description,
                Projects.ToList());
            var result = dialog.ShowDialog();
            
            if (result == true)
            {
                try
                {
                    var newRecipe = _projectManager.SaveRecipeAs(
                        SelectedProject.Id, 
                        SelectedRecipe.Name,
                        dialog.RecipeName,
                        dialog.Description ?? SelectedRecipe.Description);
                    
                    if (newRecipe != null)
                    {
                        Recipes.Add(newRecipe);
                        SelectedRecipe = newRecipe;
                        LogSuccess($"配方另存为: {newRecipe.Name}");
                    }
                }
                catch (Exception ex)
                {
                    LogError($"另存为失败: {ex.Message}");
                    MessageBox.Show($"另存为失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private bool CanDeleteRecipe()
        {
            return SelectedRecipe != null && SelectedProject != null;
        }

        private void DeleteRecipe()
        {
            if (SelectedRecipe == null || SelectedProject == null) return;

            var result = MessageBox.Show(
                $"确定要删除配方「{SelectedRecipe.Name}」吗？此操作不可恢复！",
                "确认删除",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var recipeToDelete = SelectedRecipe;
                    SelectedProject.RemoveRecipe(SelectedRecipe.Id);
                    Recipes.Remove(SelectedRecipe);
                    SelectedRecipe = null;
                    LogSuccess($"删除配方: {recipeToDelete.Name}");
                }
                catch (Exception ex)
                {
                    LogError($"删除配方失败: {ex.Message}");
                    MessageBox.Show($"删除配方失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        #endregion

        #region 配置和启动

        /// <summary>
        /// 启动时跳过配置界面
        /// </summary>
        public bool SkipStartupConfig
        {
            get => _projectManager.RuntimeConfig.SkipStartupConfig;
            set => _projectManager.RuntimeConfig.SkipStartupConfig = value;
        }

        /// <summary>
        /// 跳过命令
        /// </summary>
        public ICommand SkipCommand { get; private set; } = null!;

        /// <summary>
        /// 启动命令
        /// </summary>
        public ICommand LaunchCommand { get; private set; } = null!;

        private void Skip()
        {
            if (_ownerWindow != null)
            {
                _ownerWindow.DialogResult = false;
                _ownerWindow.Close();
            }
        }

        private bool CanLaunch()
        {
            return SelectedProject != null;
        }

        private void Launch()
        {
            if (SelectedProject != null)
            {
                _projectManager.SetCurrentProject(SelectedProject.Id, SelectedRecipe?.Name ?? "standard");
                _isLaunched = true;
                
                // 设置启动结果
                if (SelectedRecipe != null)
                {
                    _launchResult = (SelectedProject, SelectedRecipe);
                }
                else
                {
                    _launchResult = (SelectedProject, null);
                }
                
                if (_ownerWindow != null)
                {
                    _ownerWindow.DialogResult = true;
                    _ownerWindow.Close();
                }
                
                LogSuccess($"启动项目: {SelectedProject.Name}");
            }
        }

        #endregion

        #region 数据加载

        /// <summary>
        /// 加载数据
        /// </summary>
        private void LoadData()
        {
            LoadWorkspaceFolders();
            LoadRecentProjects();
            LoadProjectsFromWorkspaces();

            // 预选指定的项目和配方
            if (!string.IsNullOrEmpty(_preselectProjectId))
            {
                var project = Projects.FirstOrDefault(p => p.Id == _preselectProjectId);
                if (project != null)
                {
                    SelectedProject = project;
                    
                    if (!string.IsNullOrEmpty(_preselectRecipeName))
                    {
                        var recipe = Recipes.FirstOrDefault(r => r.Name == _preselectRecipeName);
                        if (recipe != null)
                        {
                            SelectedRecipe = recipe;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 加载工作空间列表
        /// </summary>
        private void LoadWorkspaceFolders()
        {
            WorkspaceFolders.Clear();
            var workspaces = _projectManager.RuntimeConfig.WorkspaceFolders;
            foreach (var ws in workspaces)
            {
                if (Directory.Exists(ws))
                {
                    var projectFiles = Directory.GetFiles(ws, "project.json", SearchOption.AllDirectories);
                    WorkspaceFolders.Add(new WorkspaceFolderItem
                    {
                        Name = Path.GetFileName(ws),
                        Path = ws,
                        ProjectCount = projectFiles.Length
                    });
                }
            }
            LogInfo($"加载 {WorkspaceFolders.Count} 个工作空间");
        }

        /// <summary>
        /// 加载最近打开项目
        /// </summary>
        private void LoadRecentProjects()
        {
            RecentProjects.Clear();
            var recentProjects = _projectManager.RuntimeConfig.RecentProjects;
            foreach (var rp in recentProjects.Take(20))
            {
                RecentProjects.Add(new RecentProjectItem
                {
                    ProjectId = rp.ProjectId,
                    ProjectName = rp.ProjectName,
                    LastOpenedTime = rp.LastOpenedTime,
                    LastRecipeName = rp.LastRecipeName
                });
            }
            LogInfo($"加载 {RecentProjects.Count} 个最近打开项目");
        }

        /// <summary>
        /// 从工作空间加载项目
        /// </summary>
        private void LoadProjectsFromWorkspaces()
        {
            Projects.Clear();
            foreach (var workspace in WorkspaceFolders)
            {
                // 递归搜索所有项目文件
                var projectFiles = Directory.GetFiles(workspace.Path, "project.json", SearchOption.AllDirectories);
                foreach (var projectFile in projectFiles)
                {
                    try
                    {
                        var projectDir = Path.GetDirectoryName(projectFile);
                        if (projectDir != null)
                        {
                            var project = _projectManager.LoadProjectFromPath(projectDir);
                            if (project != null && !Projects.Any(p => p.Id == project.Id))
                            {
                                Projects.Add(project);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogError($"加载项目失败: {projectFile} - {ex.Message}");
                    }
                }
            }
            LogInfo($"加载 {Projects.Count} 个项目");
        }

        /// <summary>
        /// 加载项目的配方列表
        /// </summary>
        private void LoadRecipes(Project project)
        {
            Recipes.Clear();
            foreach (var recipe in project.Recipes)
            {
                Recipes.Add(recipe);
            }
            LogInfo($"加载 {Recipes.Count} 个配方");
        }

        /// <summary>
        /// 清理文件名（移除非法字符）
        /// </summary>
        private string SanitizeFileName(string fileName)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = string.Join("_", fileName.Split(invalidChars));
            return sanitized;
        }

        #endregion

        #region 辅助类

        /// <summary>
        /// 工作空间项
        /// </summary>
        public class WorkspaceFolderItem : ObservableObject
        {
            private string _name = string.Empty;
            private string _path = string.Empty;
            private int _projectCount;

            public string Name
            {
                get => _name;
                set => SetProperty(ref _name, value, "工作空间名称");
            }

            public string Path
            {
                get => _path;
                set => SetProperty(ref _path, value, "工作空间路径");
            }

            public int ProjectCount
            {
                get => _projectCount;
                set => SetProperty(ref _projectCount, value, "项目数量");
            }
        }

        /// <summary>
        /// 最近打开项目项
        /// </summary>
        public class RecentProjectItem : ObservableObject
        {
            private string _projectId = string.Empty;
            private string _projectName = string.Empty;
            private DateTime _lastOpenedTime;
            private string _lastRecipeName = string.Empty;

            public string ProjectId
            {
                get => _projectId;
                set => SetProperty(ref _projectId, value, "项目ID");
            }

            public string ProjectName
            {
                get => _projectName;
                set => SetProperty(ref _projectName, value, "项目名称");
            }

            public DateTime LastOpenedTime
            {
                get => _lastOpenedTime;
                set => SetProperty(ref _lastOpenedTime, value, "最后打开时间");
            }

            public string LastRecipeName
            {
                get => _lastRecipeName;
                set => SetProperty(ref _lastRecipeName, value, "上次配方");
            }
        }

        /// <summary>
        /// 简单的命令实现
        /// </summary>
        private class RelayCommand : ICommand
        {
            private readonly Action _execute;
            private readonly Func<bool> _canExecute;

            public RelayCommand(Action execute, Func<bool> canExecute)
            {
                _execute = execute ?? throw new ArgumentNullException(nameof(execute));
                _canExecute = canExecute ?? (() => true);
            }

            public event EventHandler? CanExecuteChanged;

            public bool CanExecute(object? parameter)
            {
                return _canExecute();
            }

            public void Execute(object? parameter)
            {
                _execute();
            }

            public void RaiseCanExecuteChanged()
            {
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        #endregion
    }
}
