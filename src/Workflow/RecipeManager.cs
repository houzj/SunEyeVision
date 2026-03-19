using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.SDK.Logging;
using SunEyeVision.Plugin.SDK.Models;

namespace SunEyeVision.Workflow
{
    /// <summary>
    /// 配方管理器
    /// </summary>
    /// <remarks>
    /// 配方管理器负责：
    /// - 配方的创建、删除、修改
    /// - 配方的激活和停用
    /// - 默认配方的设置
    /// - 配方切换时的参数应用和恢复
    /// 
    /// 参数同步原则（rule-005）：
    /// - Recipe 是唯一参数源（持久化存储）
    /// - NodeParameters 是运行时缓存（执行时使用）
    /// - 配方切换时自动同步参数
    /// 
    /// 配方管理采用列表形式，不使用选项卡。
    /// 配方不支持参数预览，只存储参数快照。
    /// </remarks>
    public class RecipeManager : ObservableObject
    {
        private readonly Solution _solution;
        private Recipe? _activeRecipe;
        private ParameterSyncManager? _parameterSyncManager;

        /// <summary>
        /// 当前激活的配方
        /// </summary>
        [JsonIgnore]
        public Recipe? ActiveRecipe
        {
            get => _activeRecipe;
            private set => SetProperty(ref _activeRecipe, value);
        }

        /// <summary>
        /// 配方列表
        /// </summary>
        public List<Recipe> Recipes => _solution.Recipes;

        /// <summary>
        /// 默认配方
        /// </summary>
        [JsonIgnore]
        public Recipe? DefaultRecipe => Recipes.FirstOrDefault(r => r.IsDefault);

        /// <summary>
        /// 当前激活的配方ID（用于序列化）
        /// </summary>
        public string? ActiveRecipeId
        {
            get => ActiveRecipe?.Id;
            set
            {
                if (value != null)
                {
                    var recipe = GetRecipe(value);
                    if (recipe != null)
                    {
                        ActivateRecipe(recipe.Id);
                    }
                }
            }
        }

        /// <summary>
        /// 配方变化事件
        /// </summary>
        public event EventHandler<RecipeChangedEventArgs>? RecipeChanged;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="solution">解决方案</param>
        public RecipeManager(Solution solution)
        {
            _solution = solution ?? throw new ArgumentNullException(nameof(solution));
        }

        /// <summary>
        /// 设置参数同步管理器
        /// </summary>
        /// <param name="parameterSyncManager">参数同步管理器</param>
        public void SetParameterSyncManager(ParameterSyncManager parameterSyncManager)
        {
            _parameterSyncManager = parameterSyncManager ?? throw new ArgumentNullException(nameof(parameterSyncManager));
        }

        /// <summary>
        /// 创建配方
        /// </summary>
        /// <param name="name">配方名称</param>
        /// <param name="description">配方描述</param>
        /// <returns>新创建的配方</returns>
        public Recipe CreateRecipe(string name, string? description = null)
        {
            var recipe = new Recipe
            {
                Name = name,
                Description = description,
                CreatedTime = DateTime.Now,
                LastModifiedTime = DateTime.Now
            };

            Recipes.Add(recipe);
            
            VisionLogger.Instance.Log(LogLevel.Info, $"创建配方: {name} (ID: {recipe.Id})", "RecipeManager");

            // 如果是第一个配方，自动设为默认
            if (Recipes.Count == 1)
            {
                SetDefaultRecipe(recipe.Id);
            }

            return recipe;
        }

        /// <summary>
        /// 从现有配方创建副本
        /// </summary>
        /// <param name="sourceRecipeId">源配方ID</param>
        /// <param name="name">新配方名称</param>
        /// <returns>新创建的配方</returns>
        public Recipe? CloneRecipe(string sourceRecipeId, string name)
        {
            var sourceRecipe = GetRecipe(sourceRecipeId);
            if (sourceRecipe == null)
            {
                VisionLogger.Instance.Log(LogLevel.Error, $"克隆配方失败：找不到源配方 {sourceRecipeId}", "RecipeManager");
                return null;
            }

            var cloned = sourceRecipe.Clone();
            cloned.Name = name;

            Recipes.Add(cloned);
            
            VisionLogger.Instance.Log(LogLevel.Info, $"克隆配方: {sourceRecipe.Name} -> {name} (ID: {cloned.Id})", "RecipeManager");

            return cloned;
        }

        /// <summary>
        /// 删除配方
        /// </summary>
        /// <param name="recipeId">配方ID</param>
        /// <returns>是否删除成功</returns>
        public bool DeleteRecipe(string recipeId)
        {
            var recipe = GetRecipe(recipeId);
            if (recipe == null)
            {
                VisionLogger.Instance.Log(LogLevel.Warning, $"删除配方失败：找不到配方 {recipeId}", "RecipeManager");
                return false;
            }

            // 如果删除的是激活配方，先停用
            if (ActiveRecipe?.Id == recipeId)
            {
                DeactivateRecipe();
            }

            // 如果删除的是默认配方，设置新的默认配方
            bool wasDefault = recipe.IsDefault;
            Recipes.Remove(recipe);
            
            VisionLogger.Instance.Log(LogLevel.Info, $"删除配方: {recipe.Name} (ID: {recipeId})", "RecipeManager");

            if (wasDefault && Recipes.Count > 0)
            {
                SetDefaultRecipe(Recipes[0].Id);
            }

            return true;
        }

        /// <summary>
        /// 获取配方
        /// </summary>
        /// <param name="recipeId">配方ID</param>
        /// <returns>配方，如果不存在则返回null</returns>
        public Recipe? GetRecipe(string recipeId)
        {
            return Recipes.FirstOrDefault(r => r.Id == recipeId);
        }

        /// <summary>
        /// 激活配方
        /// </summary>
        /// <param name="recipeId">配方ID</param>
        /// <returns>是否激活成功</returns>
        public bool ActivateRecipe(string recipeId)
        {
            var recipe = GetRecipe(recipeId);
            if (recipe == null)
            {
                VisionLogger.Instance.Log(LogLevel.Error, $"激活配方失败：找不到配方 {recipeId}", "RecipeManager");
                return false;
            }

            // 配方切换前：通过 ParameterSyncManager 保存当前参数到旧配方
            if (_parameterSyncManager != null && ActiveRecipe != null)
            {
                _parameterSyncManager.OnRecipeSwitching(ActiveRecipe.Id, recipeId);
            }

            // 停用当前激活的配方
            if (ActiveRecipe != null)
            {
                DeactivateRecipe();
            }

            // 激活新配方
            ActiveRecipe = recipe;
            _solution.CurrentRecipeId = recipeId;

            // 应用配方参数到节点
            ApplyRecipeParameters(recipe);

            // 配方切换后：通过 ParameterSyncManager 应用新配方参数
            if (_parameterSyncManager != null)
            {
                _parameterSyncManager.OnRecipeSwitched(recipeId);
            }

            VisionLogger.Instance.Log(LogLevel.Success, $"激活配方: {recipe.Name} (ID: {recipeId})", "RecipeManager");

            // 触发事件
            RecipeChanged?.Invoke(this, new RecipeChangedEventArgs
            {
                ChangeType = RecipeChangeType.Activated,
                Recipe = recipe
            });

            return true;
        }

        /// <summary>
        /// 停用配方
        /// </summary>
        public void DeactivateRecipe()
        {
            if (ActiveRecipe == null)
                return;

            var previousRecipe = ActiveRecipe;
            ActiveRecipe = null;

            VisionLogger.Instance.Log(LogLevel.Info, $"停用配方: {previousRecipe.Name} (ID: {previousRecipe.Id})", "RecipeManager");

            // 触发事件
            RecipeChanged?.Invoke(this, new RecipeChangedEventArgs
            {
                ChangeType = RecipeChangeType.Deactivated,
                Recipe = previousRecipe
            });
        }

        /// <summary>
        /// 设置默认配方
        /// </summary>
        /// <param name="recipeId">配方ID</param>
        /// <returns>是否设置成功</returns>
        public bool SetDefaultRecipe(string recipeId)
        {
            var recipe = GetRecipe(recipeId);
            if (recipe == null)
            {
                VisionLogger.Instance.Log(LogLevel.Error, $"设置默认配方失败：找不到配方 {recipeId}", "RecipeManager");
                return false;
            }

            // 清除其他配方的默认标记
            foreach (var r in Recipes)
            {
                r.IsDefault = false;
            }

            // 设置新的默认配方
            recipe.IsDefault = true;
            _solution.DefaultRecipeId = recipeId;

            VisionLogger.Instance.Log(LogLevel.Info, $"设置默认配方: {recipe.Name} (ID: {recipeId})", "RecipeManager");

            // 触发事件
            RecipeChanged?.Invoke(this, new RecipeChangedEventArgs
            {
                ChangeType = RecipeChangeType.SetAsDefault,
                Recipe = recipe
            });

            return true;
        }

        /// <summary>
        /// 重命名配方
        /// </summary>
        /// <param name="recipeId">配方ID</param>
        /// <param name="newName">新名称</param>
        /// <returns>是否重命名成功</returns>
        public bool RenameRecipe(string recipeId, string newName)
        {
            var recipe = GetRecipe(recipeId);
            if (recipe == null)
            {
                VisionLogger.Instance.Log(LogLevel.Error, $"重命名配方失败：找不到配方 {recipeId}", "RecipeManager");
                return false;
            }

            var oldName = recipe.Name;
            recipe.Name = newName;
            recipe.LastModifiedTime = DateTime.Now;

            VisionLogger.Instance.Log(LogLevel.Info, $"重命名配方: {oldName} -> {newName} (ID: {recipeId})", "RecipeManager");

            // 触发事件
            RecipeChanged?.Invoke(this, new RecipeChangedEventArgs
            {
                ChangeType = RecipeChangeType.Renamed,
                Recipe = recipe
            });

            return true;
        }

        /// <summary>
        /// 更新配方描述
        /// </summary>
        /// <param name="recipeId">配方ID</param>
        /// <param name="description">新描述</param>
        /// <returns>是否更新成功</returns>
        public bool UpdateDescription(string recipeId, string? description)
        {
            var recipe = GetRecipe(recipeId);
            if (recipe == null)
            {
                VisionLogger.Instance.Log(LogLevel.Error, $"更新配方描述失败：找不到配方 {recipeId}", "RecipeManager");
                return false;
            }

            recipe.Description = description;
            recipe.LastModifiedTime = DateTime.Now;

            VisionLogger.Instance.Log(LogLevel.Info, $"更新配方描述: {recipe.Name} (ID: {recipeId})", "RecipeManager");

            return true;
        }

        /// <summary>
        /// 从当前节点参数创建配方
        /// </summary>
        /// <param name="name">配方名称</param>
        /// <param name="description">配方描述</param>
        /// <returns>新创建的配方</returns>
        public Recipe CreateRecipeFromCurrentParameters(string name, string? description = null)
        {
            var recipe = CreateRecipe(name, description);

            // 复制当前所有节点参数到配方
            foreach (var kvp in _solution.NodeParameters)
            {
                recipe.SaveParameters(kvp.Key, kvp.Value);
            }

            VisionLogger.Instance.Log(LogLevel.Info, $"从当前参数创建配方: {name}，包含 {recipe.ParameterMappings.Count} 个节点参数", "RecipeManager");

            return recipe;
        }

        /// <summary>
        /// 应用配方参数到节点
        /// </summary>
        /// <param name="recipe">配方</param>
        private void ApplyRecipeParameters(Recipe recipe)
        {
            if (recipe == null)
                return;

            // 应用配方参数到解决方案
            foreach (var kvp in recipe.ParameterMappings)
            {
                _solution.SaveNodeParameters(kvp.Key, kvp.Value);
            }

            VisionLogger.Instance.Log(LogLevel.Info, $"应用配方参数: {recipe.Name}，共 {recipe.ParameterMappings.Count} 个节点", "RecipeManager");
        }

        /// <summary>
        /// 验证配方管理器
        /// </summary>
        public (bool IsValid, List<string> Errors) Validate()
        {
            var errors = new List<string>();

            // 验证所有配方
            foreach (var recipe in Recipes)
            {
                var (isValid, recipeErrors) = recipe.Validate();
                if (!isValid)
                {
                    errors.AddRange(recipeErrors.Select(e => $"配方 '{recipe.Name}': {e}"));
                }
            }

            // 验证默认配方
            if (Recipes.Count > 0 && DefaultRecipe == null)
            {
                errors.Add("存在配方但未设置默认配方");
            }

            return (errors.Count == 0, errors);
        }
    }

    /// <summary>
    /// 配方变化事件参数
    /// </summary>
    public class RecipeChangedEventArgs : EventArgs
    {
        /// <summary>
        /// 变化类型
        /// </summary>
        public RecipeChangeType ChangeType { get; set; }

        /// <summary>
        /// 相关配方
        /// </summary>
        public Recipe? Recipe { get; set; }
    }

    /// <summary>
    /// 配方变化类型
    /// </summary>
    public enum RecipeChangeType
    {
        /// <summary>
        /// 已激活
        /// </summary>
        Activated,

        /// <summary>
        /// 已停用
        /// </summary>
        Deactivated,

        /// <summary>
        /// 设为默认
        /// </summary>
        SetAsDefault,

        /// <summary>
        /// 已重命名
        /// </summary>
        Renamed,

        /// <summary>
        /// 已删除
        /// </summary>
        Deleted,

        /// <summary>
        /// 已创建
        /// </summary>
        Created
    }
}
