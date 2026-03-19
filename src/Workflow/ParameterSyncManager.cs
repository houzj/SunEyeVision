using System;
using System.Collections.Generic;
using System.Linq;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.SDK.Logging;

namespace SunEyeVision.Workflow
{
    /// <summary>
    /// 参数同步管理器
    /// </summary>
    /// <remarks>
    /// 职责：统一管理所有参数同步场景
    /// 
    /// 核心原则：
    /// - Recipe 是唯一参数源（持久化存储）
    /// - NodeParameters 是运行时缓存（执行时使用）
    /// - 所有参数修改都同步到当前激活的配方
    /// 
    /// 同步场景：
    /// 1. 节点生命周期：添加/删除/克隆节点
    /// 2. 配方切换：保存旧配方参数，应用新配方参数
    /// 3. 调试窗口：节点窗口关闭时保存参数
    /// 4. 解决方案保存/加载：确保数据一致性
    /// 
    /// 设计原则（rule-004）：
    /// - 单一职责：只负责参数同步逻辑
    /// - 依赖注入：通过构造函数注入 Solution 和 RecipeManager
    /// 
    /// 日志规范（rule-003）：
    /// - 使用 VisionLogger 记录日志
    /// - 使用适当的日志级别
    /// </remarks>
    public class ParameterSyncManager
    {
        private readonly Solution _solution;
        private readonly RecipeManager _recipeManager;
        private readonly ILogger _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="solution">解决方案</param>
        /// <param name="recipeManager">配方管理器</param>
        public ParameterSyncManager(Solution solution, RecipeManager recipeManager)
        {
            _solution = solution ?? throw new ArgumentNullException(nameof(solution));
            _recipeManager = recipeManager ?? throw new ArgumentNullException(nameof(recipeManager));
            _logger = VisionLogger.Instance;
        }

        #region 节点生命周期同步

        /// <summary>
        /// 节点添加时同步参数
        /// </summary>
        /// <param name="nodeId">节点ID</param>
        /// <param name="workflow">所属工作流</param>
        public void OnNodeAdded(string nodeId, Workflow workflow)
        {
            if (string.IsNullOrEmpty(nodeId))
            {
                _logger.Log(LogLevel.Warning, "节点添加同步失败：节点ID为空", "ParameterSyncManager");
                return;
            }

            var activeRecipe = _recipeManager.ActiveRecipe;
            if (activeRecipe == null)
            {
                _logger.Log(LogLevel.Warning, $"节点添加同步失败：没有激活的配方 (节点: {nodeId})", "ParameterSyncManager");
                return;
            }

            // 检查配方中是否已有该节点的参数
            if (activeRecipe.ParameterMappings.ContainsKey(nodeId))
            {
                // 从配方加载参数到运行时缓存
                var parameters = activeRecipe.GetParameters(nodeId);
                if (parameters != null)
                {
                    _solution.SaveNodeParameters(nodeId, parameters);
                    _logger.Log(LogLevel.Info, $"节点添加同步：从配方加载参数 (节点: {nodeId})", "ParameterSyncManager");
                }
            }
            else
            {
                _logger.Log(LogLevel.Info, $"节点添加同步：配方中无该节点参数 (节点: {nodeId})", "ParameterSyncManager");
            }
        }

        /// <summary>
        /// 节点删除时同步参数
        /// </summary>
        /// <param name="nodeId">节点ID</param>
        public void OnNodeDeleted(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId))
            {
                _logger.Log(LogLevel.Warning, "节点删除同步失败：节点ID为空", "ParameterSyncManager");
                return;
            }

            // 从运行时缓存移除
            _solution.RemoveNodeParameters(nodeId);

            // 从所有配方中移除
            int removedCount = 0;
            foreach (var recipe in _solution.Recipes)
            {
                if (recipe.RemoveParameters(nodeId))
                {
                    removedCount++;
                }
            }

            _logger.Log(LogLevel.Info, $"节点删除同步：移除节点参数 (节点: {nodeId}, 从 {removedCount} 个配方中移除)", "ParameterSyncManager");
        }

        /// <summary>
        /// 节点克隆时同步参数
        /// </summary>
        /// <param name="oldNodeId">原节点ID</param>
        /// <param name="newNodeId">新节点ID</param>
        public void OnNodeCloned(string oldNodeId, string newNodeId)
        {
            if (string.IsNullOrEmpty(oldNodeId) || string.IsNullOrEmpty(newNodeId))
            {
                _logger.Log(LogLevel.Warning, "节点克隆同步失败：节点ID为空", "ParameterSyncManager");
                return;
            }

            var activeRecipe = _recipeManager.ActiveRecipe;
            if (activeRecipe == null)
            {
                _logger.Log(LogLevel.Warning, $"节点克隆同步失败：没有激活的配方", "ParameterSyncManager");
                return;
            }

            // 获取原节点参数
            var oldParameters = activeRecipe.GetParameters(oldNodeId);
            if (oldParameters != null)
            {
                // 克隆参数到新节点
                activeRecipe.SaveParameters(newNodeId, oldParameters);
                _solution.SaveNodeParameters(newNodeId, oldParameters);
                _logger.Log(LogLevel.Info, $"节点克隆同步：复制参数 ({oldNodeId} -> {newNodeId})", "ParameterSyncManager");
            }
        }

        /// <summary>
        /// 节点重命名时同步参数
        /// </summary>
        /// <param name="nodeId">节点ID</param>
        /// <param name="oldName">旧名称</param>
        /// <param name="newName">新名称</param>
        public void OnNodeRenamed(string nodeId, string oldName, string newName)
        {
            // 节点重命名不影响参数映射（使用ID作为key）
            _logger.Log(LogLevel.Info, $"节点重命名：{oldName} -> {newName} (ID: {nodeId})，参数映射不受影响", "ParameterSyncManager");
        }

        #endregion

        #region 调试窗口同步

        /// <summary>
        /// 节点窗口关闭时保存参数
        /// </summary>
        /// <param name="nodeId">节点ID</param>
        /// <param name="parameters">工具参数</param>
        public void OnNodeWindowClosing(string nodeId, ToolParameters parameters)
        {
            if (string.IsNullOrEmpty(nodeId))
            {
                _logger.Log(LogLevel.Warning, "节点窗口关闭同步失败：节点ID为空", "ParameterSyncManager");
                return;
            }

            if (parameters == null)
            {
                _logger.Log(LogLevel.Warning, $"节点窗口关闭同步失败：参数为空 (节点: {nodeId})", "ParameterSyncManager");
                return;
            }

            var activeRecipe = _recipeManager.ActiveRecipe;
            if (activeRecipe == null)
            {
                _logger.Log(LogLevel.Warning, $"节点窗口关闭同步失败：没有激活的配方 (节点: {nodeId})", "ParameterSyncManager");
                return;
            }

            // 保存到当前激活配方
            activeRecipe.SaveParameters(nodeId, parameters);

            // 同步到运行时缓存
            _solution.SaveNodeParameters(nodeId, parameters);

            _logger.Log(LogLevel.Success, $"节点窗口关闭同步：保存参数 (节点: {nodeId})", "ParameterSyncManager");
        }

        #endregion

        #region 配方切换同步

        /// <summary>
        /// 配方切换前：保存当前参数到旧配方
        /// </summary>
        /// <param name="oldRecipeId">旧配方ID</param>
        /// <param name="newRecipeId">新配方ID</param>
        public void OnRecipeSwitching(string? oldRecipeId, string newRecipeId)
        {
            if (string.IsNullOrEmpty(newRecipeId))
            {
                _logger.Log(LogLevel.Warning, "配方切换前同步失败：新配方ID为空", "ParameterSyncManager");
                return;
            }

            // 如果有旧配方，保存当前所有节点参数到旧配方
            if (!string.IsNullOrEmpty(oldRecipeId))
            {
                var oldRecipe = _recipeManager.GetRecipe(oldRecipeId);
                if (oldRecipe != null)
                {
                    // 同步所有当前节点参数到旧配方
                    foreach (var kvp in _solution.NodeParameters)
                    {
                        oldRecipe.SaveParameters(kvp.Key, kvp.Value);
                    }

                    _logger.Log(LogLevel.Info, $"配方切换前同步：保存参数到旧配方 (配方: {oldRecipe.Name}, 节点数: {_solution.NodeParameters.Count})", "ParameterSyncManager");
                }
            }
        }

        /// <summary>
        /// 配方切换后：应用新配方参数
        /// </summary>
        /// <param name="recipeId">新配方ID</param>
        public void OnRecipeSwitched(string recipeId)
        {
            if (string.IsNullOrEmpty(recipeId))
            {
                _logger.Log(LogLevel.Warning, "配方切换后同步失败：配方ID为空", "ParameterSyncManager");
                return;
            }

            var recipe = _recipeManager.GetRecipe(recipeId);
            if (recipe == null)
            {
                _logger.Log(LogLevel.Warning, $"配方切换后同步失败：找不到配方 (ID: {recipeId})", "ParameterSyncManager");
                return;
            }

            // 清空运行时缓存
            _solution.NodeParameters.Clear();

            // 应用配方参数到运行时缓存
            foreach (var kvp in recipe.ParameterMappings)
            {
                _solution.SaveNodeParameters(kvp.Key, kvp.Value);
            }

            _logger.Log(LogLevel.Success, $"配方切换后同步：应用配方参数 (配方: {recipe.Name}, 节点数: {recipe.ParameterMappings.Count})", "ParameterSyncManager");
        }

        #endregion

        #region 工作流管理同步

        /// <summary>
        /// 工作流添加时同步
        /// </summary>
        /// <param name="workflow">工作流</param>
        public void OnWorkflowAdded(Workflow workflow)
        {
            if (workflow == null)
            {
                _logger.Log(LogLevel.Warning, "工作流添加同步失败：工作流为空", "ParameterSyncManager");
                return;
            }

            _logger.Log(LogLevel.Info, $"工作流添加同步：{workflow.Name} (ID: {workflow.Id})", "ParameterSyncManager");
        }

        /// <summary>
        /// 工作流删除时同步
        /// </summary>
        /// <param name="workflow">工作流</param>
        public void OnWorkflowDeleted(Workflow workflow)
        {
            if (workflow == null)
            {
                _logger.Log(LogLevel.Warning, "工作流删除同步失败：工作流为空", "ParameterSyncManager");
                return;
            }

            // 移除该工作流下所有节点的参数
            if (workflow.Nodes != null)
            {
                foreach (var node in workflow.Nodes)
                {
                    OnNodeDeleted(node.Id);
                }
            }

            _logger.Log(LogLevel.Info, $"工作流删除同步：{workflow.Name} (ID: {workflow.Id})", "ParameterSyncManager");
        }

        #endregion

        #region 解决方案保存/加载同步

        /// <summary>
        /// 解决方案保存前同步
        /// </summary>
        public void OnSolutionSaving()
        {
            var activeRecipe = _recipeManager.ActiveRecipe;
            if (activeRecipe == null)
            {
                _logger.Log(LogLevel.Info, "解决方案保存同步：没有激活的配方，跳过同步", "ParameterSyncManager");
                return;
            }

            // 同步所有当前节点参数到激活配方
            foreach (var kvp in _solution.NodeParameters)
            {
                activeRecipe.SaveParameters(kvp.Key, kvp.Value);
            }

            _logger.Log(LogLevel.Info, $"解决方案保存同步：同步参数到激活配方 (配方: {activeRecipe.Name}, 节点数: {_solution.NodeParameters.Count})", "ParameterSyncManager");
        }

        /// <summary>
        /// 解决方案加载后同步
        /// </summary>
        public void OnSolutionLoaded()
        {
            // 确保有激活的配方
            if (_recipeManager.ActiveRecipe == null && _solution.Recipes.Count > 0)
            {
                // 优先激活默认配方
                var defaultRecipe = _solution.Recipes.FirstOrDefault(r => r.Id == _solution.DefaultRecipeId);
                if (defaultRecipe != null)
                {
                    _recipeManager.ActivateRecipe(defaultRecipe.Id);
                }
                else
                {
                    // 激活第一个配方
                    _recipeManager.ActivateRecipe(_solution.Recipes[0].Id);
                }
            }

            var activeRecipe = _recipeManager.ActiveRecipe;
            if (activeRecipe == null)
            {
                _logger.Log(LogLevel.Warning, "解决方案加载同步：没有激活的配方", "ParameterSyncManager");
                return;
            }

            // 清空运行时缓存
            _solution.NodeParameters.Clear();

            // 应用激活配方参数到运行时缓存
            foreach (var kvp in activeRecipe.ParameterMappings)
            {
                _solution.SaveNodeParameters(kvp.Key, kvp.Value);
            }

            _logger.Log(LogLevel.Success, $"解决方案加载同步：应用配方参数 (配方: {activeRecipe.Name}, 节点数: {activeRecipe.ParameterMappings.Count})", "ParameterSyncManager");
        }

        #endregion

        #region 参数同步核心方法

        /// <summary>
        /// 保存节点参数到当前激活配方
        /// </summary>
        /// <param name="nodeId">节点ID</param>
        /// <param name="parameters">工具参数</param>
        public void SaveNodeParameters(string nodeId, ToolParameters parameters)
        {
            if (string.IsNullOrEmpty(nodeId))
            {
                _logger.Log(LogLevel.Warning, "保存节点参数失败：节点ID为空", "ParameterSyncManager");
                return;
            }

            if (parameters == null)
            {
                _logger.Log(LogLevel.Warning, $"保存节点参数失败：参数为空 (节点: {nodeId})", "ParameterSyncManager");
                return;
            }

            // 保存到运行时缓存
            _solution.SaveNodeParameters(nodeId, parameters);

            // 保存到当前激活配方
            var activeRecipe = _recipeManager.ActiveRecipe;
            if (activeRecipe != null)
            {
                activeRecipe.SaveParameters(nodeId, parameters);
                _logger.Log(LogLevel.Info, $"保存节点参数到配方 (节点: {nodeId}, 配方: {activeRecipe.Name})", "ParameterSyncManager");
            }
            else
            {
                _logger.Log(LogLevel.Warning, $"保存节点参数失败：没有激活的配方 (节点: {nodeId})", "ParameterSyncManager");
            }
        }

        /// <summary>
        /// 从当前激活配方获取节点参数
        /// </summary>
        /// <param name="nodeId">节点ID</param>
        /// <returns>工具参数，不存在返回null</returns>
        public ToolParameters? GetNodeParameters(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId))
            {
                _logger.Log(LogLevel.Warning, "获取节点参数失败：节点ID为空", "ParameterSyncManager");
                return null;
            }

            // 优先从运行时缓存获取
            var parameters = _solution.GetNodeParameters(nodeId);
            if (parameters != null)
            {
                return parameters;
            }

            // 从当前激活配方获取
            var activeRecipe = _recipeManager.ActiveRecipe;
            if (activeRecipe != null)
            {
                parameters = activeRecipe.GetParameters(nodeId);
                if (parameters != null)
                {
                    // 同步到运行时缓存
                    _solution.SaveNodeParameters(nodeId, parameters);
                    return parameters;
                }
            }

            return null;
        }

        /// <summary>
        /// 验证参数一致性
        /// </summary>
        /// <returns>验证结果</returns>
        public (bool IsValid, List<string> Errors) ValidateConsistency()
        {
            var errors = new List<string>();

            var activeRecipe = _recipeManager.ActiveRecipe;
            if (activeRecipe == null)
            {
                errors.Add("没有激活的配方");
                return (false, errors);
            }

            // 检查运行时缓存中的节点是否都在配方中
            foreach (var nodeId in _solution.NodeParameters.Keys)
            {
                if (!activeRecipe.ParameterMappings.ContainsKey(nodeId))
                {
                    errors.Add($"节点 {nodeId} 在运行时缓存中存在，但不在激活配方中");
                }
            }

            // 检查配方中的节点是否都在运行时缓存中
            foreach (var nodeId in activeRecipe.ParameterMappings.Keys)
            {
                if (!_solution.NodeParameters.ContainsKey(nodeId))
                {
                    errors.Add($"节点 {nodeId} 在激活配方中存在，但不在运行时缓存中");
                }
            }

            return (errors.Count == 0, errors);
        }

        #endregion
    }
}
