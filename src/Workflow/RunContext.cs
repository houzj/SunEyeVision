using System;
using System.Collections.Generic;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.SDK.Models;

namespace SunEyeVision.Workflow;

/// <summary>
/// 执行上下文（运行时临时对象）
/// </summary>
/// <remarks>
/// 组合 Program（执行流）和 Recipe（数据流），提供运行时执行环境。
/// 与 RuntimeConfig（持久化配置）不同，RunContext 是临时的运行时对象。
/// 
/// 使用场景：
/// 1. 执行检测任务时创建
/// 2. 提供节点参数查询（优先配方参数，其次程序默认参数）
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
    /// 检测程序（执行流）
    /// </summary>
    public InspectionProgram Program { get; set; } = new();

    /// <summary>
    /// 检测配方（数据流）
    /// </summary>
    public InspectionRecipe Recipe { get; set; } = new();

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
    /// 1. 配方中的节点参数
    /// 2. 程序中的默认参数
    /// </remarks>
    public ToolParameters? GetNodeParameters(string nodeId)
    {
        // 优先使用配方参数
        if (Recipe.NodeParams.TryGetValue(nodeId, out var recipeParam))
            return recipeParam.Clone();

        // 其次使用程序默认参数
        if (Program.DefaultParams.TryGetValue(nodeId, out var defaultParam))
            return defaultParam.Clone();

        return null;
    }

    /// <summary>
    /// 获取节点参数（指定类型）
    /// </summary>
    public T? GetNodeParameters<T>(string nodeId) where T : ToolParameters
    {
        var parameters = GetNodeParameters(nodeId);
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
        foreach (var node in Program.Nodes)
        {
            nodeIds.Add(node.Id);
        }
        return nodeIds;
    }

    /// <summary>
    /// 获取启用的节点列表
    /// </summary>
    public List<ProgramNode> GetEnabledNodes()
    {
        var nodes = new List<ProgramNode>();
        foreach (var node in Program.Nodes)
        {
            if (node.IsEnabled)
                nodes.Add(node);
        }
        return nodes;
    }

    /// <summary>
    /// 获取节点的输入连接
    /// </summary>
    public List<ProgramConnection> GetInputConnections(string nodeId)
    {
        var connections = new List<ProgramConnection>();
        foreach (var conn in Program.Connections)
        {
            if (conn.TargetNodeId == nodeId)
                connections.Add(conn);
        }
        return connections;
    }

    /// <summary>
    /// 获取节点的输出连接
    /// </summary>
    public List<ProgramConnection> GetOutputConnections(string nodeId)
    {
        var connections = new List<ProgramConnection>();
        foreach (var conn in Program.Connections)
        {
            if (conn.SourceNodeId == nodeId)
                connections.Add(conn);
        }
        return connections;
    }

    /// <summary>
    /// 验证执行上下文
    /// </summary>
    public bool Validate()
    {
        if (Program == null)
            return false;

        if (Recipe == null)
            return false;

        // 验证所有启用的节点都有参数
        foreach (var node in GetEnabledNodes())
        {
            var parameters = GetNodeParameters(node.Id);
            if (parameters == null)
                return false;
        }

        return true;
    }

    /// <summary>
    /// 获取执行统计信息
    /// </summary>
    public string GetStatistics()
    {
        var enabledNodes = GetEnabledNodes();
        return $"节点数: {enabledNodes.Count}/{Program.Nodes.Count}, " +
               $"连接数: {Program.Connections.Count}, " +
               $"配方参数: {Recipe.NodeParams.Count}, " +
               $"全局变量: {Recipe.GlobalVariables.Count}";
    }

    /// <summary>
    /// 从项目和配方创建执行上下文
    /// </summary>
    public static RunContext Create(Project project, InspectionRecipe? recipe = null)
    {
        if (project == null)
            throw new ArgumentNullException(nameof(project));

        // 如果没有指定配方，使用第一个配方
        if (recipe == null)
        {
            if (project.Recipes.Count == 0)
            {
                // 创建默认配方
                recipe = new InspectionRecipe
                {
                    Name = "默认配方",
                    ProjectId = project.Id
                };

                // 从程序的默认参数创建配方参数
                foreach (var kvp in project.Program.DefaultParams)
                {
                    recipe.NodeParams[kvp.Key] = kvp.Value.Clone();
                }
            }
            else
            {
                recipe = project.Recipes[0];
            }
        }

        return new RunContext
        {
            Program = project.Program,
            Recipe = recipe
        };
    }

    /// <summary>
    /// 从项目和配方名称创建执行上下文
    /// </summary>
    public static RunContext Create(Project project, string recipeName)
    {
        if (project == null)
            throw new ArgumentNullException(nameof(project));

        var recipe = project.GetRecipeByName(recipeName);
        if (recipe == null)
            throw new ArgumentException($"配方不存在: {recipeName}");

        return Create(project, recipe);
    }
}
