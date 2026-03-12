using System;
using System.Collections.Generic;
using SunEyeVision.Plugin.SDK.Execution.Parameters;

namespace SunEyeVision.Workflow;

/// <summary>
/// 运行时上下文 - 组合执行流和数据流
/// </summary>
/// <remarks>
/// RuntimeContext 是解耦架构的运行时组合层：
/// - 将 WorkflowDefinition、DataConfiguration、RuntimeBinding 组合为可执行的上下文
/// - 提供统一的参数访问接口
/// - 支持全局变量管理
/// - 支持运行时参数覆盖
/// 
/// 生命周期：
/// 1. 从 SolutionManager 加载 RuntimeContext
/// 2. 执行过程中访问参数和全局变量
/// 3. 执行完成后释放资源
/// </remarks>
public class RuntimeContext
{
    /// <summary>
    /// 工作流定义
    /// </summary>
    public WorkflowDefinition? Workflow { get; set; }

    /// <summary>
    /// 数据配置
    /// </summary>
    public DataConfiguration? DataConfig { get; set; }

    /// <summary>
    /// 绑定信息
    /// </summary>
    public RuntimeBinding? Binding { get; set; }

    /// <summary>
    /// 当前配方组名称
    /// </summary>
    public string CurrentRecipeGroup { get; set; } = "default";

    /// <summary>
    /// 运行时参数覆盖（优先级最高）
    /// </summary>
    /// <remarks>
    /// 用于临时修改参数，不影响持久化数据。
    /// 例如：调试时临时调整阈值。
    /// </remarks>
    public Dictionary<string, ToolParameters> RuntimeOverrides { get; } = new();

    /// <summary>
    /// 节点执行结果缓存
    /// </summary>
    public Dictionary<string, object> NodeResults { get; } = new();

    /// <summary>
    /// 获取节点的运行时参数
    /// </summary>
    /// <param name="nodeId">节点ID</param>
    /// <returns>参数实例，如果不存在则返回null</returns>
    /// <remarks>
    /// 参数获取优先级：
    /// 1. RuntimeOverrides（运行时覆盖）
    /// 2. DataConfiguration 中的配方组参数
    /// 3. WorkflowDefinition 中的默认参数
    /// </remarks>
    public ToolParameters? GetNodeParameters(string nodeId)
    {
        // 优先级1：运行时覆盖
        if (RuntimeOverrides.TryGetValue(nodeId, out var runtimeOverride))
            return runtimeOverride.Clone();

        // 优先级2：数据配置中的配方组参数
        var nodeParams = DataConfig?.GetNodeParams(CurrentRecipeGroup);
        if (nodeParams != null && nodeParams.TryGetValue(nodeId, out var param))
            return param.Clone();

        // 优先级3：工作流默认参数
        if (Workflow?.DefaultParams.TryGetValue(nodeId, out var defaultParam) == true)
            return defaultParam.Clone();

        return null;
    }

    /// <summary>
    /// 设置运行时参数覆盖
    /// </summary>
    /// <param name="nodeId">节点ID</param>
    /// <param name="parameters">参数实例</param>
    public void SetRuntimeOverride(string nodeId, ToolParameters parameters)
    {
        RuntimeOverrides[nodeId] = parameters.Clone();
    }

    /// <summary>
    /// 清除运行时参数覆盖
    /// </summary>
    /// <param name="nodeId">节点ID，如果为null则清除所有</param>
    public void ClearRuntimeOverride(string? nodeId = null)
    {
        if (nodeId == null)
            RuntimeOverrides.Clear();
        else
            RuntimeOverrides.Remove(nodeId);
    }

    /// <summary>
    /// 获取全局变量值
    /// </summary>
    /// <typeparam name="T">变量类型</typeparam>
    /// <param name="name">变量名称</param>
    /// <returns>变量值，如果不存在则返回默认值</returns>
    public T? GetGlobalVariable<T>(string name)
    {
        return DataConfig.GetGlobalVariable<T>(name) ?? default;
    }

    /// <summary>
    /// 设置全局变量
    /// </summary>
    /// <param name="name">变量名称</param>
    /// <param name="value">变量值</param>
    public void SetGlobalVariable(string name, object value)
    {
        DataConfig?.SetGlobalVariable(name, value);
    }

    /// <summary>
    /// 切换配方组
    /// </summary>
    /// <param name="recipeGroupName">配方组名称</param>
    public void SwitchRecipeGroup(string recipeGroupName)
    {
        CurrentRecipeGroup = recipeGroupName;
        Binding?.SwitchRecipeGroup(recipeGroupName);
    }

    /// <summary>
    /// 创建运行时 Workflow 实例
    /// </summary>
    /// <returns>Workflow 实例</returns>
    public Workflow? CreateWorkflow()
    {
        if (Workflow == null)
            return null;

        var nodeParams = DataConfig?.GetNodeParams(CurrentRecipeGroup);

        // 合并运行时覆盖
        var mergedParams = new Dictionary<string, ToolParameters>();
        if (nodeParams != null)
        {
            foreach (var kvp in nodeParams)
            {
                mergedParams[kvp.Key] = kvp.Value;
            }
        }
        foreach (var kvp in RuntimeOverrides)
        {
            mergedParams[kvp.Key] = kvp.Value;
        }

        return Workflow.CreateWorkflow(mergedParams);
    }

    /// <summary>
    /// 获取节点执行结果
    /// </summary>
    /// <typeparam name="T">结果类型</typeparam>
    /// <param name="nodeId">节点ID</param>
    /// <returns>执行结果，如果不存在则返回默认值</returns>
    public T? GetNodeResult<T>(string nodeId)
    {
        if (NodeResults.TryGetValue(nodeId, out var result))
        {
            if (result is T typedResult)
                return typedResult;
            try
            {
                return (T?)Convert.ChangeType(result, typeof(T));
            }
            catch
            {
                return default;
            }
        }
        return default;
    }

    /// <summary>
    /// 设置节点执行结果
    /// </summary>
    /// <param name="nodeId">节点ID</param>
    /// <param name="result">执行结果</param>
    public void SetNodeResult(string nodeId, object result)
    {
        NodeResults[nodeId] = result;
    }

    /// <summary>
    /// 清除节点执行结果
    /// </summary>
    /// <param name="nodeId">节点ID，如果为null则清除所有</param>
    public void ClearNodeResult(string? nodeId = null)
    {
        if (nodeId == null)
            NodeResults.Clear();
        else
            NodeResults.Remove(nodeId);
    }

    /// <summary>
    /// 验证上下文是否完整
    /// </summary>
    /// <returns>验证结果</returns>
    public bool Validate()
    {
        if (Workflow == null)
            return false;

        if (DataConfig == null)
            return false;

        if (Binding != null && !Binding.Validate())
            return false;

        return true;
    }

    /// <summary>
    /// 获取上下文摘要信息
    /// </summary>
    /// <returns>摘要信息字符串</returns>
    public string GetSummary()
    {
        var summary = "=== RuntimeContext Summary ===\n";
        summary += $"Workflow: {Workflow?.Name ?? "N/A"} (ID: {Workflow?.Id ?? "N/A"})\n";
        summary += $"DataConfig: {DataConfig?.Name ?? "N/A"} (ID: {DataConfig?.Id ?? "N/A"})\n";
        summary += $"RecipeGroup: {CurrentRecipeGroup}\n";
        summary += $"Binding: {Binding?.DeviceName ?? "N/A"} (Device: {Binding?.DeviceId ?? "N/A"})\n";
        summary += $"RuntimeOverrides: {RuntimeOverrides.Count} nodes\n";
        summary += $"NodeResults: {NodeResults.Count} nodes\n";
        summary += $"GlobalVariables: {DataConfig?.GlobalVariables.Count ?? 0} variables\n";
        return summary;
    }

    /// <summary>
    /// 克隆上下文（用于并行执行）
    /// </summary>
    /// <returns>克隆的上下文</returns>
    public RuntimeContext Clone()
    {
        var cloned = new RuntimeContext
        {
            Workflow = Workflow,
            DataConfig = DataConfig,
            Binding = Binding,
            CurrentRecipeGroup = CurrentRecipeGroup
        };

        foreach (var kvp in RuntimeOverrides)
        {
            cloned.RuntimeOverrides[kvp.Key] = kvp.Value.Clone();
        }

        // NodeResults 不克隆，每个上下文独立

        return cloned;
    }
}
