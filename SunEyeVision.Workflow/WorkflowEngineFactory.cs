using System;
using SunEyeVision.Interfaces;
using SunEyeVision.PluginSystem;
using SunEyeVision.PluginSystem.Base.Interfaces;
using SunEyeVision.PluginSystem.Infrastructure;

namespace SunEyeVision.Workflow
{
    /// <summary>
    /// 工作流引擎工厂 - 用于创建和初始化工作流相关组件
    /// </summary>
    public static class WorkflowEngineFactory
    {
        /// <summary>
        /// 创建完整的工作流引擎套件
        /// </summary>
        public static (WorkflowEngine, WorkflowExecutionEngine, IPluginManager) CreateEngineSuite(ILogger logger)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            // 1. 创建插件管理器（传递logger）
            var pluginManager = new PluginManager(logger);

            // 2. 创建基础工作流引擎
            var workflowEngine = new WorkflowEngine(logger);

            // 3. 创建执行引擎
            var executionEngine = new WorkflowExecutionEngine(workflowEngine, pluginManager, logger);

            // 4. 加载并注册工作流控制插件
            RegisterWorkflowControlPlugin(workflowEngine, pluginManager, logger);

            return (workflowEngine, executionEngine, pluginManager);
        }

        /// <summary>
        /// 注册工作流控制插件
        /// </summary>
        private static void RegisterWorkflowControlPlugin(
            WorkflowEngine workflowEngine,
            IPluginManager pluginManager,
            ILogger logger)
        {
            try
            {
                // 创建工作流控制插件
                var controlPlugin = new SubroutinePlugin(workflowEngine);
                pluginManager.RegisterPlugin(controlPlugin);

                logger.LogInfo("工作流控制插件已注册");
            }
            catch (Exception ex)
            {
                logger.LogError($"注册工作流控制插件失败: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// 创建工作流引擎（仅基础功能）
        /// </summary>
        public static WorkflowEngine CreateBasicEngine(ILogger logger)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            return new WorkflowEngine(logger);
        }

        /// <summary>
        /// 创建工作流执行引擎
        /// </summary>
        public static WorkflowExecutionEngine CreateExecutionEngine(
            WorkflowEngine workflowEngine,
            IPluginManager pluginManager,
            ILogger logger)
        {
            if (workflowEngine == null)
            {
                throw new ArgumentNullException(nameof(workflowEngine));
            }
            if (pluginManager == null)
            {
                throw new ArgumentNullException(nameof(pluginManager));
            }
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            return new WorkflowExecutionEngine(workflowEngine, pluginManager, logger);
        }
    }
}
