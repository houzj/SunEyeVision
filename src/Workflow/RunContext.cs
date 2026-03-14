using System;
using System.Collections.Generic;
using System.Linq;
using SunEyeVision.Plugin.SDK.Execution.Parameters;

namespace SunEyeVision.Workflow;

/// <summary>
/// 执行上下文（运行时临时对象）
/// </summary>
/// <remarks>
/// 组合 Workflow（执行流）和 Recipe（数据流），提供运行时执行环境。
/// 与 RuntimeConfig（持久化配置）不同，RunContext 是临时的运行时对象。
///
/// 使用场景：
/// 1. 执行检测任务时创建
/// 2. 提供节点参数查询（优先配方参数，其次工作流默认参数）
/// 3. 任务完成后释放
///
/// 生命周期：
/// - 创建：开始执行时
/// - 使用：执行过程中
/// - 释放：执行完成后
/// </remarks>
public class RunContext
{
    /// <summary>
    /// 工作流（执行流）
    /// </summary>
    public Workflow Workflow { get; set; } = new();

    /// <summary>
    /// 配方（数据流）
    /// </summary>
    public Recipe Recipe { get; set; } = new();

    /// <summary>
    /// 配方组
    /// </summary>
    public RecipeGroup? RecipeGroup { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedTime { get; } = DateTime.Now;

    /// <summary>
    /// 执行ID
    /// </summary>
    public string ExecutionId { get; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 获取节点参数
    /// </summary>
    /// <remarks>
    /// 优先级：
    /// 1. 配方组中的节点参数
    /// 2. 配方中的节点参数（如果配方组不存在）
    /// 3. 工作流中的默认参数
    /// </remarks>
    public ToolParameters? GetNodeParameters(string nodeId, string? recipeGroupName = null)
    {
        // 如果指定了配方组名称，优先使用配方组参数
        if (!string.IsNullOrEmpty(recipeGroupName))
        {
            RecipeGroup = Recipe.GetRecipeGroup(recipeGroupName);
        }

        // 优先使用配方组参数
        if (RecipeGroup != null)
        {
            var groupParams = RecipeGroup.GetNodeParameters(nodeId);
            if (groupParams != null)
                return groupParams;
        }

        // 其次使用配方默认参数（配方组的default）
        var defaultParams = Recipe.GetRecipeGroup("default");
        if (defaultParams != null)
        {
            var recipeParams = defaultParams.GetNodeParameters(nodeId);
            if (recipeParams != null)
                return recipeParams;
        }

        // 最后使用工作流默认参数
        if (Workflow.DefaultParams.TryGetValue(nodeId, out var workflowParam))
            return workflowParam.Clone();

        return null;
    }

    /// <summary>
    /// 获取节点参数（指定类型）
    /// </summary>
    public T? GetNodeParameters<T>(string nodeId, string? recipeGroupName = null) where T : ToolParameters
    {
        var parameters = GetNodeParameters(nodeId, recipeGroupName);
        return parameters as T;
    }

    /// <summary>
    /// 获取全局变量值
    /// </summary>
    public T? GetGlobalVariable<T>(string name)
    {
        return Recipe.GetGlobalVariable<T>(name);
    }

    /// <summary>
    /// 获取所有节点ID
    /// </summary>
    public List<string> GetAllNodeIds()
    {
        var nodeIds = new List<string>();
        foreach (var node in Workflow.Nodes)
        {
            nodeIds.Add(node.Id);
        }
        return nodeIds;
    }

    /// <summary>
    /// 获取启用的节点列表
    /// </summary>
    public List<WorkflowNode> GetEnabledNodes()
    {
        var nodes = new List<WorkflowNode>();
        foreach (var node in Workflow.Nodes)
        {
            if (node.IsEnabled)
                nodes.Add(node);
        }
        return nodes;
    }

    /// <summary>
    /// 获取节点的输入连接
    /// </summary>
    public List<Connection> GetInputConnections(string nodeId)
    {
        var connections = new List<Connection>();
        foreach (var conn in Workflow.Connections)
        {
            if (conn.TargetNode == nodeId)
                connections.Add(conn);
        }
        return connections;
    }

    /// <summary>
    /// 获取节点的输出连接
    /// </summary>
    public List<Connection> GetOutputConnections(string nodeId)
    {
        var connections = new List<Connection>();
        foreach (var conn in Workflow.Connections)
        {
            if (conn.SourceNode == nodeId)
                connections.Add(conn);
        }
        return connections;
    }

    /// <summary>
    /// 验证执行上下文
    /// </summary>
    public (bool IsValid, List<string> Errors) Validate()
    {
        var errors = new List<string>();

        if (Workflow == null)
        {
            errors.Add("工作流为空");
            return (false, errors);
        }

        if (Recipe == null)
        {
            errors.Add("配方为空");
            return (false, errors);
        }

        // 验证配方组
        if (RecipeGroup == null)
        {
            RecipeGroup = Recipe.GetRecipeGroup("default");
            if (RecipeGroup == null)
            {
                errors.Add("配方组为空");
                return (false, errors);
            }
        }

        // 验证所有启用的节点都有参数
        foreach (var node in GetEnabledNodes())
        {
            var parameters = GetNodeParameters(node.Id);
            if (parameters == null)
            {
                errors.Add($"节点 {node.Name} ({node.Id}) 没有参数配置");
            }
        }

        return (errors.Count == 0, errors);
    }

    /// <summary>
    /// 获取执行统计信息
    /// </summary>
    public string GetStatistics()
    {
        var enabledNodes = GetEnabledNodes();
        var nodeParamsCount = RecipeGroup?.NodeParams.Count ?? 0;
        var globalVarsCount = Recipe.GlobalVariables.Count;

        return $"节点数: {enabledNodes.Count}/{Workflow.Nodes.Count}, " +
               $"连接数: {Workflow.Connections.Count}, " +
               $"配方参数: {nodeParamsCount}, " +
               $"全局变量: {globalVarsCount}";
    }

    /// <summary>
    /// 从工作流和配方创建执行上下文
    /// </summary>
    public static RunContext Create(Workflow workflow, Recipe recipe, string? recipeGroupName = null)
    {
        if (workflow == null)
            throw new ArgumentNullException(nameof(workflow));

        if (recipe == null)
            throw new ArgumentNullException(nameof(recipe));

        var recipeGroup = recipe.GetRecipeGroup(recipeGroupName ?? "default");

        return new RunContext
        {
            Workflow = workflow,
            Recipe = recipe,
            RecipeGroup = recipeGroup
        };
    }

    /// <summary>
    /// 从解决方案和设备ID创建执行上下文
    /// </summary>
    public static RunContext? Create(SolutionFile solution, string deviceId)
    {
        if (solution == null)
            throw new ArgumentNullException(nameof(solution));

        if (string.IsNullOrEmpty(deviceId))
            throw new ArgumentException("设备ID不能为空", nameof(deviceId));

        // 查找设备绑定
        var binding = solution.Bindings.FirstOrDefault(b => b.DeviceId == deviceId);
        if (binding == null)
            return null;

        // 查找工作流
        var workflow = solution.Workflows.FirstOrDefault(w => w.Id == binding.WorkflowRef);
        if (workflow == null)
            return null;

        // 查找配方
        var recipe = solution.Recipes.FirstOrDefault(r => r.Id == binding.RecipeRef);
        if (recipe == null)
            return null;

        return Create(workflow, recipe, binding.RecipeGroupName);
    }
}
