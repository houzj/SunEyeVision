using System;
using System.IO;

namespace SunEyeVision.Workflow;

/// <summary>
/// 启动决策结果
/// </summary>
public enum StartupDecision
{
    /// <summary>
    /// 显示配置界面（空状态，无解决方案）
    /// </summary>
    ShowConfigurationWithEmptyState,

    /// <summary>
    /// 显示配置界面（有最近解决方案）
    /// </summary>
    ShowConfigurationWithRecentSolution,

    /// <summary>
    /// 显示配置界面（默认）
    /// </summary>
    ShowConfiguration,

    /// <summary>
    /// 跳过配置界面，直接进入主界面
    /// </summary>
    SkipConfiguration,

    /// <summary>
    /// 直接进入主界面，自动加载最近解决方案
    /// </summary>
    LoadRecentAndStart
}

/// <summary>
/// 启动决策服务
/// </summary>
/// <remarks>
/// 职责：
/// 1. 检查 SolutionSettings
/// 2. 决定启动时是否显示配置界面
/// 3. 返回决策结果
///
/// 使用场景：
/// 1. 应用启动时调用
/// 2. 根据用户配置决定启动流程
///
/// 重构说明（2026-03-16）：
/// - 已移除对 RuntimeConfig 的依赖
/// - 直接使用 SolutionSettings
/// - 代码更简洁，逻辑更清晰
/// </remarks>
public class StartupDecisionService
{
    private readonly SolutionManager _solutionManager;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="solutionManager">解决方案管理器</param>
    public StartupDecisionService(SolutionManager solutionManager)
    {
        _solutionManager = solutionManager ?? throw new ArgumentNullException(nameof(solutionManager));
    }

    /// <summary>
    /// 获取启动决策
    /// </summary>
    /// <returns>启动决策结果</returns>
    public StartupDecision GetStartupDecision()
    {
        // 1. 检查是否跳过配置
        if (_solutionManager.Settings.SkipStartupConfig)
        {
            // 2. 检查是否有当前解决方案
            if (!string.IsNullOrEmpty(_solutionManager.Settings.CurrentSolutionId))
            {
                var currentSolutionMetadata = _solutionManager.Settings.GetRecentSolution(_solutionManager.Settings.CurrentSolutionId);
                if (currentSolutionMetadata != null &&
                    !string.IsNullOrEmpty(currentSolutionMetadata.FilePath) &&
                    File.Exists(currentSolutionMetadata.FilePath))
                {
                    // 跳过配置，直接加载最近解决方案
                    return StartupDecision.LoadRecentAndStart;
                }
            }

            // 跳过配置，进入主界面（无解决方案）
            return StartupDecision.SkipConfiguration;
        }

        // 3. 检查是否有最近解决方案
        var recentSolutions = _solutionManager.Settings.GetRecentSolutionsCopy();
        if (recentSolutions.Count > 0)
        {
            // 显示配置界面，预选最近解决方案
            return StartupDecision.ShowConfigurationWithRecentSolution;
        }

        // 4. 显示配置界面，空状态
        return StartupDecision.ShowConfigurationWithEmptyState;
    }

    /// <summary>
    /// 获取最近解决方案ID
    /// </summary>
    /// <returns>解决方案ID，如果没有则返回null</returns>
    public string? GetRecentSolutionId()
    {
        // 优先返回当前解决方案
        if (!string.IsNullOrEmpty(_solutionManager.Settings.CurrentSolutionId))
        {
            return _solutionManager.Settings.CurrentSolutionId;
        }

        // 返回第一个最近解决方案
        var recentSolutions = _solutionManager.Settings.GetRecentSolutionsCopy();
        if (recentSolutions.Count > 0)
        {
            return recentSolutions[0].Id;
        }

        return null;
    }
}
