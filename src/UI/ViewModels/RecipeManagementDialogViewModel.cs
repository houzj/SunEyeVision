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
        /// 配方创建完成事件
        /// </summary>
        public event Action<Recipe>? OnRecipeCreated;

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
        /// 应用配方命令
        /// </summary>
        public ICommand ApplyRecipeCommand { get; private set; }

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
            ApplyRecipeCommand = new RelayCommand(ApplyRecipe, CanApplyRecipe);
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

                // 通知视图层焦点移到新配方的名称单元格
                OnRecipeCreated?.Invoke(recipe);
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

                    // 通知视图层焦点移到克隆配方的名称单元格
                    OnRecipeCreated?.Invoke(cloned);
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
        /// 仅设置默认配方（轻量级操作，用于checkbox点击）
        /// </summary>
        public void SetDefaultRecipeOnly(Recipe recipe)
        {
            if (recipe == null) return;

            try
            {
                var currentSolution = _solutionManager.CurrentSolution;
                if (currentSolution == null) return;

                // 防止取消最后一个默认配方
                var defaultRecipes = Recipes.Where(r => r.IsDefault).ToList();
                if (!recipe.IsDefault && defaultRecipes.Count == 1 && defaultRecipes[0].Id == recipe.Id)
                {
                    LogWarning("至少需要一个默认配方");
                    return;
                }

                // 使用RecipeManager设置默认配方
                var recipeManager = new RecipeManager(currentSolution);
                var success = recipeManager.SetDefaultRecipe(recipe.Id);

                if (success)
                {
                    // 更新UI：清除其他配方的默认标记
                    foreach (var r in Recipes)
                    {
                        r.IsDefault = r.Id == recipe.Id;
                    }

                    LogInfo($"设为默认配方: {recipe.Name}");
                }
            }
            catch (Exception ex)
            {
                LogError($"设置默认配方失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 应用配方（同时设为默认）
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
                    // 同时设为默认配方
                    recipeManager.SetDefaultRecipe(SelectedRecipe.Id);

                    // 更新UI：清除其他配方的默认标记
                    foreach (var recipe in Recipes)
                    {
                        recipe.IsDefault = recipe.Id == SelectedRecipe.Id;
                    }

                    LogSuccess($"应用配方并设为默认: {SelectedRecipe.Name}");

                    // 提示用户
                    MessageBox.Show(
                        $"配方 '{SelectedRecipe.Name}' 已成功应用并设为默认！\n\n工作流节点参数已更新。",
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
        /// 关闭
        /// </summary>
        private void Close()
        {
            LogInfo("关闭配方管理器");
        }
    }
}
