using System;
using SunEyeVision.Core.Interfaces;
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.Infrastructure.Infrastructure;
using SunEyeVision.Plugin.Infrastructure.Managers.Plugin;
using SunEyeVision.Plugin.SDK;

namespace SunEyeVision.Workflow
{
    /// <summary>
    /// 湤 - ڴͳʼ
    /// </summary>
    public static class WorkflowEngineFactory
    {
        /// <summary>
        /// Ĺ׼
        /// </summary>
        public static (WorkflowEngine, WorkflowExecutionEngine, IPluginManager) CreateEngineSuite(ILogger logger)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            // 1. logger
            var pluginManager = new PluginManager(logger);

            // 2. 
            var workflowEngine = new WorkflowEngine(logger);

            // 3. ִ
            var executionEngine = new WorkflowExecutionEngine(workflowEngine, pluginManager, logger);

            // 4. زעṤƲ
            RegisterWorkflowControlPlugin(workflowEngine, pluginManager, logger);

            return (workflowEngine, executionEngine, pluginManager);
        }

        /// <summary>
        /// עṤƲ
        /// </summary>
        private static void RegisterWorkflowControlPlugin(
            WorkflowEngine workflowEngine,
            IPluginManager pluginManager,
            ILogger logger)
        {
            try
            {
                // Ʋ
                var controlPlugin = new SubroutinePlugin(workflowEngine);
                pluginManager.RegisterPlugin(controlPlugin);

                logger.LogInfo("Ʋע");
            }
            catch (Exception ex)
            {
                logger.LogError($"עṤƲʧ: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// 棨ܣ
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
        /// ִ
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
