using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SunEyeVision.Plugin.SDK.Models;
using SunEyeVision.UI.Controls.Helpers;
using SunEyeVision.UI.Views.Windows;
using SunEyeVision.Workflow;

namespace SunEyeVision.UI.ViewModels
{
    /// <summary>
    /// 解决方案配置对话框 ViewModel
    /// </summary>
    /// <remarks>
    /// 已从 Project 迁移到 Solution 架构
    /// - Project → Solution
    /// - ProjectManager → SolutionManager
    /// - InspectionRecipe → Recipe（已移除）
    /// </remarks>
    public class SolutionConfigurationDialogViewModel : ViewModelBase
    {
        private readonly SolutionManager _solutionManager;
        private readonly string? _preselectSolutionId;

        // 状态1：元数据列表（轻量级，仅用于显示和配置）
        public ObservableCollection<SolutionMetadata> SolutionMetadatas { get; } = new();

        // 状态2：选中的元数据（轻量级）
        private SolutionMetadata? _selectedMetadata;
        public SolutionMetadata? SelectedMetadata
        {
            get => _selectedMetadata;
            set
            {
                if (SetProperty(ref _selectedMetadata, value))
                {
                    // 选中时只标记状态，不加载完整解决方案
                    OnPropertyChanged(nameof(SelectedMetadataId));
                    OnPropertyChanged(nameof(HasSelectedSolution));
                    OnPropertyChanged(nameof(SolutionStoragePath));
                    OnPropertyChanged(nameof(SolutionInfo));
                }
            }
        }

        // 辅助属性：选中的元数据ID（用于XAML数据触发器）
        public string? SelectedMetadataId => SelectedMetadata?.Id;

        // 状态3：完整的 Solution 对象（懒加载，仅在启动时加载）
        private Solution? _selectedSolution;
        public Solution? SelectedSolution
        {
            get => _selectedSolution;
            private set
            {
                if (SetProperty(ref _selectedSolution, value))
                {
                    OnPropertyChanged(nameof(SelectedSolutionId));
                }
            }
        }

        // 辅助属性：选中的Solution ID（用于XAML数据触发器）
        public string? SelectedSolutionId => SelectedSolution?.Id;

        private bool _skipStartupConfig;
        private bool _isLaunched;
        private Solution? _launchResult;
        private Window? _ownerWindow;

        public SolutionConfigurationDialogViewModel(SolutionManager solutionManager, string? preselectSolutionId = null)
        {
            _solutionManager = solutionManager ?? throw new ArgumentNullException(nameof(solutionManager));
            _preselectSolutionId = preselectSolutionId;

            InitializeCommands();
            LoadData();

            // 监听元数据变更事件，自动刷新UI
            _solutionManager.MetadataChanged += (sender, e) => LoadSolutions();
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
        public Solution? LaunchResult => _launchResult;

        /// <summary>
        /// 是否已启动
        /// </summary>
        public bool IsLaunched => _isLaunched;

        /// <summary>
        /// 保存当前解决方案
        /// </summary>
        public void SaveCurrentSolution()
        {
            if (SelectedSolution != null)
            {
                try
                {
                    _solutionManager.SaveSolution(SelectedSolution.FilePath);
                }
                catch (Exception ex)
                {
                    LogError($"保存解决方案失败: {ex.Message}");
                }
            }
        }

        #region 解决方案相关

        /// <summary>
        /// 解决方案元数据列表（轻量级，仅用于显示和配置）
        /// </summary>
        // public ObservableCollection<SolutionMetadata> SolutionMetadatas { get; } = new();  // 已在构造函数前定义

        /// <summary>
        /// 选中的完整解决方案（懒加载）
        /// </summary>
        // public Solution? SelectedSolution { get; private set; }  // 已在构造函数前定义

        /// <summary>
        /// 是否选中了解决方案
        /// </summary>
        public bool HasSelectedSolution => SelectedMetadata != null;

        /// <summary>
        /// 解决方案存储路径
        /// </summary>
        public string SolutionStoragePath => SelectedMetadata?.FilePath ?? "未选择解决方案";

        /// <summary>
        /// 解决方案信息
        /// </summary>
        public string SolutionInfo
        {
            get
            {
                if (SelectedMetadata == null) return string.Empty;
                return $"版本: {SelectedMetadata.Version}\n" +
                       $"工作流数: {SelectedMetadata.WorkflowCount}";
            }
        }

        /// <summary>
        /// 新建解决方案命令
        /// </summary>
        public ICommand NewSolutionCommand { get; private set; } = null!;

        /// <summary>
        /// 打开解决方案命令
        /// </summary>
        public ICommand OpenSolutionCommand { get; private set; } = null!;

        /// <summary>
        /// 另存为解决方案命令
        /// </summary>
        public ICommand SaveSolutionAsCommand { get; private set; } = null!;

        /// <summary>
        /// 删除解决方案命令
        /// </summary>
        public ICommand DeleteSolutionCommand { get; private set; } = null!;

        /// <summary>
        /// 复制解决方案命令
        /// </summary>
        public ICommand CopySolutionCommand { get; private set; } = null!;

        /// <summary>
        /// 重命名解决方案命令
        /// </summary>
        public ICommand RenameSolutionCommand { get; private set; } = null!;

        /// <summary>
        /// 编辑解决方案命令（名称+描述）
        /// </summary>
        public ICommand EditSolutionCommand { get; private set; } = null!;

        /// <summary>
        /// 选中解决方案命令
        /// </summary>
        public ICommand SelectCommand { get; private set; } = null!;

        private void InitializeCommands()
        {
            NewSolutionCommand = new RelayCommand(NewSolution, () => true);
            OpenSolutionCommand = new RelayCommand(OpenSolution, () => true);
            CopySolutionCommand = new RelayCommand(CopySolution, () => CanCopySolution());
            RenameSolutionCommand = new RelayCommand(RenameSolution, () => CanRenameSolution());
            EditSolutionCommand = new RelayCommand(EditSolution, () => CanEditSolution());
            SaveSolutionAsCommand = new RelayCommand(SaveSolutionAs, () => CanSaveSolutionAs());
            DeleteSolutionCommand = new RelayCommand(DeleteSolution, () => CanDeleteSolution());
            SelectCommand = new RelayCommand(SelectSolution, () => true);
            SkipCommand = new RelayCommand(Skip, () => true);
            LaunchCommand = new RelayCommand(Launch, () => true);
        }

        /// <summary>
        /// 选中解决方案（单击卡片，只标记选中状态，不加载完整解决方案）
        /// </summary>
        private void SelectSolution(object? parameter)
        {
            if (parameter is SolutionMetadata metadata)
            {
                SelectedMetadata = metadata;
                LogInfo($"选中解决方案: {metadata.Name}");
            }
            else if (parameter != null)
            {
                LogWarning($"SelectCommand收到未知类型参数: {parameter.GetType().Name}");
            }
        }

        private void NewSolution()
        {
            LogInfo("开始创建新解决方案");

            var defaultPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            try
            {
                var dialog = new NewSolutionDialog(defaultPath);
                var result = dialog.ShowDialog();

                if (result == true && !string.IsNullOrEmpty(dialog.SolutionName))
                {
                    LogInfo($"用户输入: 名称={dialog.SolutionName}, 路径={dialog.SolutionPath}, 描述={dialog.Description}");

                    try
                    {
                        // ✅ 使用新的 CreateNewSolution 方法，接受元数据
                        var metadata = new SolutionMetadata
                        {
                            Name = dialog.SolutionName,
                            Description = dialog.Description ?? "",
                            DirectoryPath = dialog.SolutionPath
                        };

                        var newMetadata = _solutionManager.CreateNewSolution(metadata);

                        if (newMetadata == null)
                        {
                            LogError("SolutionManager.CreateNewSolution 返回了 null");
                            MessageBox.Show("创建解决方案失败: 返回了 null", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        // 添加到UI列表
                        SolutionMetadatas.Add(newMetadata);
                        LogInfo($"添加到列表，当前数量: {SolutionMetadatas.Count}");

                        // 设置选中状态
                        SelectedMetadata = newMetadata;
                        LogInfo($"已设置选中解决方案");

                        LogSuccess($"创建解决方案成功: {newMetadata.Name}");
                    }
                    catch (Exception ex)
                    {
                        LogError($"创建解决方案失败: {ex.Message}");
                        MessageBox.Show($"创建解决方案失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else if (result == true)
                {
                    LogWarning("用户点击了创建但未输入解决方案名称");
                }
                else
                {
                    LogInfo("用户取消了创建解决方案");
                }
            }
            catch (Exception ex)
            {
                LogError($"打开新建对话框失败: {ex.Message}");
                MessageBox.Show($"打开新建对话框失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenSolution()
        {
            var initialPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var selectedPath = FolderBrowserHelper.BrowseForFolder("选择解决方案目录", initialPath);

            if (!string.IsNullOrEmpty(selectedPath))
            {
                try
                {
                    var solution = _solutionManager.OpenSolution(selectedPath);
                    if (solution != null)
                    {
                        // 创建元数据
                        var metadata = SolutionMetadata.FromSolution(solution);

                        // 检查是否已存在
                        if (!SolutionMetadatas.Any(s => s.Id == solution.Id))
                        {
                            SolutionMetadatas.Add(metadata);
                        }

                        // 设置选中状态
                        SelectedMetadata = metadata;
                        LogSuccess($"打开解决方案: {solution.Name}");
                    }
                    else
                    {
                        MessageBox.Show("所选目录中没有找到有效的解决方案文件", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    LogError($"打开解决方案失败: {ex.Message}");
                    MessageBox.Show($"打开解决方案失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private bool CanSaveSolutionAs()
        {
            return SelectedSolution != null;
        }

        private void SaveSolutionAs()
        {
            if (SelectedMetadata == null) return;

            var defaultName = $"{SelectedMetadata.Name}_copy";
            var defaultPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var defaultDescription = SelectedMetadata.Description;

            try
            {
                // 显示另存为对话框
                var dialog = new NewSolutionDialog(defaultPath)
                {
                    SolutionName = defaultName,
                    Description = defaultDescription
                };
                var result = dialog.ShowDialog();

                if (result == true && !string.IsNullOrEmpty(dialog.SolutionName))
                {
                    // ✅ 文件级操作：直接复制文件，不加载完整 Solution 对象
                    var targetFilePath = Path.Combine(dialog.SolutionPath, $"{dialog.SolutionName}.solution");

                    // 需要先打开当前解决方案（如果尚未打开）
                    if (SelectedSolution == null)
                    {
                        _solutionManager.LoadSolutionOnly(SelectedMetadata.FilePath);
                    }

                    var newMetadata = _solutionManager.SaveAsSolution(targetFilePath);

                    if (newMetadata != null)
                    {
                        SolutionMetadatas.Add(newMetadata);
                        SelectedMetadata = newMetadata;
                        LogSuccess($"解决方案另存为: {dialog.SolutionName}");
                    }
                    else
                    {
                        MessageBox.Show("另存为失败", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                LogError($"另存为失败: {ex.Message}");
                MessageBox.Show($"另存为失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanDeleteSolution()
        {
            return SelectedMetadata != null;
        }

        private void DeleteSolution()
        {
            if (SelectedMetadata == null) return;

            var result = MessageBox.Show(
                $"确定要删除解决方案「{SelectedMetadata.Name}」吗？此操作不可恢复！",
                "确认删除",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var solutionToDelete = SelectedMetadata;

                    // 从列表中移除
                    SolutionMetadatas.Remove(solutionToDelete);

                    // 从SolutionManager中删除
                    _solutionManager.DeleteSolution(solutionToDelete.Id);

                    SelectedMetadata = null;
                    LogSuccess($"删除解决方案: {solutionToDelete.Name}");
                }
                catch (Exception ex)
                {
                    LogError($"删除解决方案失败: {ex.Message}");
                    MessageBox.Show($"删除解决方案失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private bool CanCopySolution()
        {
            return SelectedMetadata != null;
        }

        private void CopySolution()
        {
            if (SelectedMetadata == null) return;

            try
            {
                // ✅ 文件级操作：不加载完整 Solution 对象
                var baseName = SelectedMetadata.Name;
                var copyName = GenerateUniqueCopyName(baseName);
                var defaultPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

                var newMetadata = _solutionManager.CopySolution(
                    SelectedMetadata.FilePath,
                    copyName,
                    defaultPath);

                if (newMetadata != null)
                {
                    SolutionMetadatas.Add(newMetadata);
                    SelectedMetadata = newMetadata;
                    LogSuccess($"复制解决方案: {copyName}");
                }
            }
            catch (Exception ex)
            {
                LogError($"复制解决方案失败: {ex.Message}");
                MessageBox.Show($"复制解决方案失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanRenameSolution()
        {
            return SelectedMetadata != null;
        }

        private void RenameSolution()
        {
            if (SelectedMetadata == null) return;

            // 创建简单的重命名对话框
            var dialog = new Window
            {
                Title = "重命名解决方案",
                Width = 400,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.NoResize,
                Owner = _ownerWindow
            };

            var grid = new Grid { Margin = new Thickness(20) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var label = new TextBlock { Text = "请输入新的解决方案名称:", Margin = new Thickness(0, 0, 0, 10) };
            Grid.SetRow(label, 0);

            var textBox = new TextBox
            {
                Text = SelectedMetadata.Name,
                Margin = new Thickness(0, 0, 0, 15),
                Padding = new Thickness(8, 6, 8, 6)
            };
            Grid.SetRow(textBox, 1);

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            Grid.SetRow(buttonPanel, 3);

            var cancelButton = new Button
            {
                Content = "取消",
                Width = 100,
                Height = 32,
                Margin = new Thickness(0, 0, 8, 0)
            };
            var okButton = new Button
            {
                Content = "确定",
                Width = 100,
                Height = 32
            };

            buttonPanel.Children.Add(cancelButton);
            buttonPanel.Children.Add(okButton);

            grid.Children.Add(label);
            grid.Children.Add(textBox);
            grid.Children.Add(buttonPanel);

            dialog.Content = grid;

            string? newName = null;
            okButton.Click += (s, e) =>
            {
                newName = textBox.Text.Trim();
                dialog.DialogResult = true;
                dialog.Close();
            };

            cancelButton.Click += (s, e) =>
            {
                dialog.DialogResult = false;
                dialog.Close();
            };

            if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(newName) && newName != SelectedMetadata.Name)
            {
                try
                {
                    // 需要加载完整的 Solution 对象来重命名
                    var fullSolution = _solutionManager.LoadSolutionOnly(SelectedMetadata.FilePath);
                    if (fullSolution != null)
                    {
                        var oldName = fullSolution.Name;
                        fullSolution.Name = newName;
                        _solutionManager.SaveSolution(fullSolution.FilePath);

                        // 更新元数据
                        var newMetadata = SolutionMetadata.FromSolution(fullSolution);
                        var index = SolutionMetadatas.IndexOf(SelectedMetadata);
                        SolutionMetadatas[index] = newMetadata;
                        SelectedMetadata = newMetadata;

                        LogSuccess($"重命名解决方案: {oldName} → {newName}");
                    }
                }
                catch (Exception ex)
                {
                    LogError($"重命名解决方案失败: {ex.Message}");
                    MessageBox.Show($"重命名解决方案失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private bool CanEditSolution()
        {
            return SelectedMetadata != null;
        }

        private void EditSolution()
        {
            if (SelectedMetadata == null) return;

            try
            {
                var dialog = new EditSolutionDialog(SelectedMetadata.Name, SelectedMetadata.Description)
                {
                    Owner = _ownerWindow
                };

                var result = dialog.ShowDialog();

                if (result == true)
                {
                    var oldName = SelectedMetadata.Name;
                    var oldDescription = SelectedMetadata.Description;

                    // 检查是否有变化
                    bool nameChanged = dialog.SolutionName != SelectedMetadata.Name;
                    bool descriptionChanged = dialog.Description != SelectedMetadata.Description;

                    if (!nameChanged && !descriptionChanged)
                    {
                        LogInfo("解决方案信息未变化");
                        return;
                    }

                    // 需要加载完整的 Solution 对象来编辑
                    var fullSolution = _solutionManager.LoadSolutionOnly(SelectedMetadata.FilePath);
                    if (fullSolution != null)
                    {
                        // 更新名称和描述
                        if (nameChanged)
                        {
                            fullSolution.Name = dialog.SolutionName;
                            LogInfo($"解决方案名称: {oldName} → {dialog.SolutionName}");
                        }

                        if (descriptionChanged)
                        {
                            fullSolution.Description = dialog.Description ?? "";
                            LogInfo($"解决方案描述已更新");
                        }

                        // 保存到文件
                        _solutionManager.SaveSolution(fullSolution.FilePath);

                        // 更新元数据
                        var newMetadata = SolutionMetadata.FromSolution(fullSolution);
                        var index = SolutionMetadatas.IndexOf(SelectedMetadata);
                        SolutionMetadatas[index] = newMetadata;
                        SelectedMetadata = newMetadata;

                        LogSuccess("解决方案信息已保存");
                    }
                }
                else
                {
                    LogInfo("用户取消编辑解决方案");
                }
            }
            catch (Exception ex)
            {
                LogError($"编辑解决方案失败: {ex.Message}");
                MessageBox.Show($"编辑解决方案失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 生成唯一的复制名称
        /// </summary>
        private string GenerateUniqueCopyName(string baseName)
        {
            var existingNames = SolutionMetadatas.Select(s => s.Name).ToHashSet();
            var copyName = $"{baseName}_副本";
            var counter = 2;

            while (existingNames.Contains(copyName))
            {
                copyName = $"{baseName}_副本{counter}";
                counter++;
            }

            return copyName;
        }

        #endregion



        #region 配置和启动

        /// <summary>
        /// 启动时跳过配置界面
        /// </summary>
        public bool SkipStartupConfig
        {
            get => _solutionManager.Settings.SkipStartupConfig;  // ✅ 改进：使用 SolutionSettings
            set => _solutionManager.Settings.SkipStartupConfig = value;
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

        private void Launch(object? parameter)
        {
            // 获取要启动的元数据
            var metadataToLaunch = parameter as SolutionMetadata ?? SelectedMetadata;

            if (metadataToLaunch != null)
            {
                // 如果参数传入的不是当前选中的解决方案，更新选中状态
                if (SelectedMetadata?.Id != metadataToLaunch.Id)
                {
                    SelectedMetadata = metadataToLaunch;
                }

                // ✅ 懒加载：仅在启动时加载完整 Solution 对象
                var fullSolution = _solutionManager.LoadSolutionOnly(metadataToLaunch.FilePath);
                if (fullSolution != null)
                {
                    SelectedSolution = fullSolution;
                    LogInfo($"加载完整解决方案对象: {fullSolution.Name}");

                    _solutionManager.SetCurrentSolution(fullSolution);
                    _solutionManager.Settings.CurrentSolutionId = fullSolution.Id;
                    _isLaunched = true;

                    // 设置启动结果
                    _launchResult = fullSolution;

                    if (_ownerWindow != null)
                    {
                        _ownerWindow.DialogResult = true;
                        _ownerWindow.Close();
                    }

                    LogSuccess($"启动解决方案: {fullSolution.Name}");
                }
                else
                {
                    LogError($"加载解决方案失败: {metadataToLaunch.Name}");
                    MessageBox.Show($"加载解决方案失败: {metadataToLaunch.Name}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        #endregion

        #region 数据加载

        /// <summary>
        /// 加载数据
        /// </summary>
        private void LoadData()
        {
            LoadSolutions();

            // 预选指定的解决方案
            if (!string.IsNullOrEmpty(_preselectSolutionId))
            {
                var metadata = SolutionMetadatas.FirstOrDefault(s => s.Id == _preselectSolutionId);
                if (metadata != null)
                {
                    SelectedMetadata = metadata;
                }
            }
        }

        /// <summary>
        /// 从 SolutionManager 加载解决方案元数据列表
        /// </summary>
        /// <remarks>
        /// 架构改进（2026-03-16）：
        /// - 直接使用 SolutionMetadata，避免不必要的转换层
        /// - 避免加载 Workflows、NodeParameters、GlobalVariables、Devices、Communications 等大数据
        /// - 完整 Solution 对象只在需要编辑或执行时通过 LoadSolutionOnly() 加载
        /// - 性能提升：启动时间从数秒降低到毫秒级
        ///
        /// 使用 SolutionManager.GetAllMetadata() 获取所有元数据
        /// 利用 SolutionCache 和 SolutionRegistry 实现懒加载优化
        /// </remarks>
        private void LoadSolutions()
        {
            // 先清空集合
            SolutionMetadatas.Clear();

            // 使用 SolutionManager.GetAllMetadata() 获取所有元数据
            var metadataList = _solutionManager.GetAllMetadata();

            // 批量添加，减少 UI 更新次数
            foreach (var metadata in metadataList)
            {
                SolutionMetadatas.Add(metadata);
            }

            LogInfo($"加载 {metadataList.Count} 个解决方案（元数据模式 + 缓存优化）");
        }

        /// <summary>
        /// 加载解决方案的配方列表（已废弃：Recipe功能已在新架构中移除）
        /// </summary>
        /*
        private void LoadRecipes(SolutionFile solution)
        {
            Recipes.Clear();
            foreach (var recipe in solution.Recipes)
            {
                Recipes.Add(recipe);
            }
            LogInfo($"加载 {Recipes.Count} 个配方");
        }
        */

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
        /// 简单的命令实现（支持参数传递）
        /// </summary>
        private class RelayCommand : ICommand
        {
            private readonly Action<object?> _execute;
            private readonly Func<bool> _canExecute;

            public RelayCommand(Action execute, Func<bool> canExecute)
            {
                _execute = (parameter) => execute();
                _canExecute = canExecute ?? (() => true);
            }

            public RelayCommand(Action<object?> execute, Func<bool> canExecute)
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
                _execute(parameter);
            }

            public void RaiseCanExecuteChanged()
            {
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        #endregion
    }
}
