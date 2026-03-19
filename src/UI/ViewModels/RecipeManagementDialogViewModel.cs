using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using SunEyeVision.Workflow;

namespace SunEyeVision.UI.ViewModels
{
    /// <summary>
    /// 配方管理器 ViewModel
    /// </summary>
    public class RecipeManagementDialogViewModel : ViewModelBase
    {
        private readonly SolutionManager _solutionManager;

        /// <summary>
        /// 配方列表
        /// </summary>
        public ObservableCollection<Recipe> Recipes { get; }

        /// <summary>
        /// 选中的配方
        /// </summary>
        private Recipe? _selectedRecipe;
        public Recipe? SelectedRecipe
        {
            get => _selectedRecipe;
            set => SetProperty(ref _selectedRecipe, value);
        }

        /// <summary>
        /// 创建配方命令
        /// </summary>
        public ICommand CreateRecipeCommand { get; private set; }

        /// <summary>
        /// 克隆配方命令
        /// </summary>
        public ICommand CloneRecipeCommand { get; private set; }

        /// <summary>
        /// 删除配方命令
        /// </summary>
        public ICommand DeleteRecipeCommand { get; private set; }

        /// <summary>
        /// 设为默认命令
        /// </summary>
        public ICommand SetDefaultRecipeCommand { get; private set; }

        /// <summary>
        /// 应用配方命令
        /// </summary>
        public ICommand ApplyRecipeCommand { get; private set; }

        /// <summary>
        /// 保存命令
        /// </summary>
        public ICommand SaveCommand { get; private set; }

        /// <summary>
        /// 关闭命令
        /// </summary>
        public ICommand CloseCommand { get; private set; }

        public RecipeManagementDialogViewModel(SolutionManager solutionManager)
        {
            _solutionManager = solutionManager ?? throw new ArgumentNullException(nameof(solutionManager));

            var currentSolution = _solutionManager.CurrentSolution;
            Recipes = new ObservableCollection<Recipe>(
                currentSolution?.Recipes ?? Enumerable.Empty<Recipe>()
            );

            InitializeCommands();
        }

        private void InitializeCommands()
        {
            CreateRecipeCommand = new RelayCommand(CreateRecipe, () => true);
            CloneRecipeCommand = new RelayCommand(CloneRecipe, CanCloneRecipe);
            DeleteRecipeCommand = new RelayCommand(DeleteRecipe, CanDeleteRecipe);
            SetDefaultRecipeCommand = new RelayCommand(SetDefaultRecipe, CanSetDefaultRecipe);
            ApplyRecipeCommand = new RelayCommand(ApplyRecipe, CanApplyRecipe);
            SaveCommand = new RelayCommand(Save, CanSave);
            CloseCommand = new RelayCommand(Close, () => true);
        }

        /// <summary>
        /// 创建配方
        /// </summary>
        private void CreateRecipe()
        {
            try
            {
                var currentSolution = _solutionManager.CurrentSolution;
                if (currentSolution == null) return;

                // 使用RecipeManager创建配方
                var recipeManager = new RecipeManager(currentSolution);
                var recipe = recipeManager.CreateRecipe($"配方{Recipes.Count + 1}", "");

                Recipes.Add(recipe);
                SelectedRecipe = recipe;

                LogInfo($"创建配方: {recipe.Name}");
            }
            catch (Exception ex)
            {
                LogError($"创建配方失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 克隆配方
        /// </summary>
        private void CloneRecipe()
        {
            if (SelectedRecipe == null) return;

            try
            {
                var currentSolution = _solutionManager.CurrentSolution;
                if (currentSolution == null) return;

                // 使用RecipeManager克隆配方
                var recipeManager = new RecipeManager(currentSolution);
                var cloned = recipeManager.CloneRecipe(SelectedRecipe.Id, $"{SelectedRecipe.Name}_副本");

                if (cloned != null)
                {
                    Recipes.Add(cloned);
                    SelectedRecipe = cloned;

                    LogInfo($"克隆配方: {SelectedRecipe.Name} -> {cloned.Name}");
                }
            }
            catch (Exception ex)
            {
                LogError($"克隆配方失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 是否可以克隆配方
        /// </summary>
        private bool CanCloneRecipe()
        {
            return SelectedRecipe != null;
        }

        /// <summary>
        /// 删除配方
        /// </summary>
        private void DeleteRecipe()
        {
            if (SelectedRecipe == null) return;

            try
            {
                var result = MessageBox.Show(
                    $"确定要删除配方 '{SelectedRecipe.Name}' 吗？",
                    "确认删除",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question
                );

                if (result != MessageBoxResult.Yes) return;

                var currentSolution = _solutionManager.CurrentSolution;
                if (currentSolution == null) return;

                // 使用RecipeManager删除配方
                var recipeManager = new RecipeManager(currentSolution);
                var deleted = recipeManager.DeleteRecipe(SelectedRecipe.Id);

                if (deleted)
                {
                    Recipes.Remove(SelectedRecipe);
                    LogInfo($"删除配方: {SelectedRecipe.Name}");
                    SelectedRecipe = null;
                }
            }
            catch (Exception ex)
            {
                LogError($"删除配方失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 是否可以删除配方
        /// </summary>
        private bool CanDeleteRecipe()
        {
            return SelectedRecipe != null;
        }

        /// <summary>
        /// 设为默认配方
        /// </summary>
        private void SetDefaultRecipe()
        {
            if (SelectedRecipe == null) return;

            try
            {
                var currentSolution = _solutionManager.CurrentSolution;
                if (currentSolution == null) return;

                // 使用RecipeManager设置默认配方
                var recipeManager = new RecipeManager(currentSolution);
                var success = recipeManager.SetDefaultRecipe(SelectedRecipe.Id);

                if (success)
                {
                    // 更新UI：清除其他配方的默认标记
                    foreach (var recipe in Recipes)
                    {
                        recipe.IsDefault = recipe.Id == SelectedRecipe.Id;
                    }

                    LogInfo($"设为默认配方: {SelectedRecipe.Name}");
                }
            }
            catch (Exception ex)
            {
                LogError($"设置默认配方失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 是否可以设为默认配方
        /// </summary>
        private bool CanSetDefaultRecipe()
        {
            return SelectedRecipe != null && !SelectedRecipe.IsDefault;
        }

        /// <summary>
        /// 应用配方
        /// </summary>
        private void ApplyRecipe()
        {
            if (SelectedRecipe == null) return;

            try
            {
                var currentSolution = _solutionManager.CurrentSolution;
                if (currentSolution == null) return;

                // 使用RecipeManager激活配方
                var recipeManager = new RecipeManager(currentSolution);
                var success = recipeManager.ActivateRecipe(SelectedRecipe.Id);

                if (success)
                {
                    LogSuccess($"应用配方: {SelectedRecipe.Name}");

                    // 提示用户
                    MessageBox.Show(
                        $"配方 '{SelectedRecipe.Name}' 已成功应用！\n\n工作流节点参数已更新。",
                        "应用成功",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                }
            }
            catch (Exception ex)
            {
                LogError($"应用配方失败: {ex.Message}");
                MessageBox.Show(
                    $"应用配方失败: {ex.Message}",
                    "错误",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        /// <summary>
        /// 是否可以应用配方
        /// </summary>
        private bool CanApplyRecipe()
        {
            return SelectedRecipe != null;
        }

        /// <summary>
        /// 保存
        /// </summary>
        private void Save()
        {
            var currentSolution = _solutionManager.CurrentSolution;
            if (currentSolution?.FilePath != null)
            {
                try
                {
                    _solutionManager.SaveSolution(currentSolution.FilePath);
                    LogSuccess("配方保存成功");
                }
                catch (Exception ex)
                {
                    LogError($"保存配方失败: {ex.Message}");
                }
            }
            else
            {
                LogWarning("解决方案未保存，无法保存配方");
            }
        }

        /// <summary>
        /// 是否可以保存
        /// </summary>
        private bool CanSave()
        {
            return _solutionManager.CurrentSolution?.FilePath != null;
        }

        /// <summary>
        /// 关闭
        /// </summary>
        private void Close()
        {
            LogInfo("关闭配方管理器");
        }
    }
}
