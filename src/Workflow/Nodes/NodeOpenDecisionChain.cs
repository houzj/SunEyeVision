using System;
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.SDK.Logging;
using SunEyeVision.Plugin.Infrastructure.Managers.Tool;
using SunEyeVision.Workflow;

namespace SunEyeVision.Workflow.Nodes
{
    /// <summary>
    /// 节点打开决策链 - 集中处理节点双击的决策流程
    /// </summary>
    /// <remarks>
    /// 决策链步骤：
    /// 1. 数据获取：从节点获取基本信息
    /// 2. 元数据获取：从 ToolRegistry 获取工具元数据
    /// 3. 窗口类型决策：根据优先级确定窗口类型
    /// 4. 窗口类型判断：如果是 None，直接返回
    /// 5. 内容创建决策：调用 IToolPlugin.CreateDebugControl()
    /// 6. 策略查找和执行：根据节点类型查找合适的策略并执行
    /// 
    /// 窗口类型优先级：节点级配置 → 工具级配置 → 全局默认值
    /// </remarks>
    public class NodeOpenDecisionChain
    {
        private readonly NodeOpenStrategyRegistry _strategyRegistry;

        public NodeOpenDecisionChain()
        {
            _strategyRegistry = new NodeOpenStrategyRegistry();
        }

        /// <summary>
        /// 执行决策链
        /// </summary>
        /// <param name="node">要打开的节点</param>
        /// <param name="context">节点打开上下文</param>
        /// <returns>true 表示成功处理，false 表示处理失败或无窗口</returns>
        public bool Execute(WorkflowNodeBase node, NodeOpenContext context)
        {
            try
            {
                // Step 1: 数据获取
                context.Node = node;
                VisionLogger.Instance.Log(LogLevel.Info, 
                    $"开始处理节点双击: {node.Name}", 
                    "NodeOpenDecisionChain");

                // Step 2: 元数据获取
                FetchMetadata(context);

                // Step 3: 窗口类型决策
                DecideWindowStyle(context);

                // Step 4: 窗口类型判断
                if (context.WindowStyle == DebugWindowStyle.None)
                {
                    VisionLogger.Instance.Log(LogLevel.Info, 
                        $"窗口类型为 None，不打开窗口: {node.Name}", 
                        "NodeOpenDecisionChain");
                    return true;
                }

                // Step 5: 内容创建决策
                CreateDebugControl(context);

                // Step 6: 策略查找和执行
                ExecuteStrategy(context);

                return true;
            }
            catch (Exception ex)
            {
                VisionLogger.Instance.Log(LogLevel.Error, 
                    $"节点打开失败: {ex.Message}", 
                    "NodeOpenDecisionChain", ex);
                return false;
            }
        }

        /// <summary>
        /// Step 2: 获取工具元数据
        /// </summary>
        private void FetchMetadata(NodeOpenContext context)
        {
            var node = context.Node;
            
            // 从 ToolRegistry 获取元数据
            var metadata = ToolRegistry.GetToolMetadata(node.ToolType);
            context.Metadata = metadata;

            if (metadata != null)
            {
                VisionLogger.Instance.Log(LogLevel.Success, 
                    $"获取工具元数据成功: {metadata.DisplayName}", 
                    "NodeOpenDecisionChain");
            }
            else
            {
                VisionLogger.Instance.Log(LogLevel.Warning, 
                    $"未找到工具元数据: {node.ToolType}", 
                    "NodeOpenDecisionChain");
            }
        }

        /// <summary>
        /// Step 3: 决定窗口类型
        /// </summary>
        /// <remarks>
        /// 窗口类型优先级：节点级配置 → 工具级配置 → 全局默认值
        /// </remarks>
        private void DecideWindowStyle(NodeOpenContext context)
        {
            var node = context.Node;
            var metadata = context.Metadata;

            // 优先级1：节点级配置
            if (node.DebugWindowStyle != DebugWindowStyle.Default)
            {
                context.WindowStyle = node.DebugWindowStyle;
                VisionLogger.Instance.Log(LogLevel.Info, 
                    $"使用节点级窗口类型: {context.WindowStyle}", 
                    "NodeOpenDecisionChain");
                return;
            }

            // 优先级2：工具级配置
            if (metadata?.DebugWindowStyle != DebugWindowStyle.Default)
            {
                context.WindowStyle = metadata.DebugWindowStyle;
                VisionLogger.Instance.Log(LogLevel.Info, 
                    $"使用工具级窗口类型: {context.WindowStyle}", 
                    "NodeOpenDecisionChain");
                return;
            }

            // 优先级3：全局默认值
            context.WindowStyle = DebugWindowStyle.Default;
            VisionLogger.Instance.Log(LogLevel.Info, 
                $"使用全局默认窗口类型: {context.WindowStyle}", 
                "NodeOpenDecisionChain");
        }

        /// <summary>
        /// Step 5: 创建调试控件
        /// </summary>
        private void CreateDebugControl(NodeOpenContext context)
        {
            var node = context.Node;

            try
            {
                // 创建工具实例
                var toolInstance = node.CreateInstance();
                context.ToolInstance = toolInstance;

                if (toolInstance == null)
                {
                    VisionLogger.Instance.Log(LogLevel.Warning, 
                        $"无法创建工具实例: {node.ToolType}", 
                        "NodeOpenDecisionChain");
                    return;
                }

                // 调用 CreateDebugControl 方法
                var debugControl = toolInstance.CreateDebugControl();
                context.DebugControl = debugControl;

                if (debugControl != null)
                {
                    VisionLogger.Instance.Log(LogLevel.Success, 
                        $"创建调试控件成功: {debugControl.GetType().Name}", 
                        "NodeOpenDecisionChain");
                }
                else
                {
                    VisionLogger.Instance.Log(LogLevel.Info, 
                        $"工具未提供调试控件: {node.ToolType}", 
                        "NodeOpenDecisionChain");
                }
            }
            catch (Exception ex)
            {
                VisionLogger.Instance.Log(LogLevel.Error, 
                    $"创建调试控件失败: {ex.Message}", 
                    "NodeOpenDecisionChain", ex);
            }
        }

        /// <summary>
        /// Step 6: 查找并执行策略
        /// </summary>
        private void ExecuteStrategy(NodeOpenContext context)
        {
            var strategy = _strategyRegistry.FindStrategy(context);

            if (strategy != null)
            {
                VisionLogger.Instance.Log(LogLevel.Info, 
                    $"使用策略: {strategy.GetType().Name}", 
                    "NodeOpenDecisionChain");
                strategy.Execute(context);
            }
            else
            {
                VisionLogger.Instance.Log(LogLevel.Warning, 
                    $"未找到合适的策略，使用默认策略", 
                    "NodeOpenDecisionChain");
                _strategyRegistry.GetDefaultStrategy().Execute(context);
            }
        }
    }
}
