using System;
using System.Collections.Generic;
using SunEyeVision.Workflow.Nodes.Strategies;

namespace SunEyeVision.Workflow.Nodes
{
    /// <summary>
    /// 节点打开策略注册器 - 管理所有节点打开策略
    /// </summary>
    /// <remarks>
    /// 职责：
    /// 1. 注册和管理所有策略
    /// 2. 根据上下文查找合适的策略
    /// 3. 提供默认策略
    /// 
    /// 策略查找优先级：
    /// 1. 特殊节点策略（子程序、条件、循环等）
    /// 2. 默认策略
    /// </remarks>
    public class NodeOpenStrategyRegistry
    {
        private readonly List<INodeOpenStrategy> _strategies;
        private readonly DefaultNodeOpenStrategy _defaultStrategy;

        public NodeOpenStrategyRegistry()
        {
            _strategies = new List<INodeOpenStrategy>();
            _defaultStrategy = new DefaultNodeOpenStrategy();

            // 注册内置策略
            RegisterBuiltInStrategies();
        }

        /// <summary>
        /// 注册内置策略
        /// </summary>
        private void RegisterBuiltInStrategies()
        {
            // 注册特殊节点策略（按优先级顺序）
            _strategies.Add(new SubroutineNodeOpenStrategy());
            _strategies.Add(new ConditionNodeOpenStrategy());
            // 未来可以添加更多策略：
            // _strategies.Add(new LoopNodeOpenStrategy());
            // _strategies.Add(new SwitchNodeOpenStrategy());
        }

        /// <summary>
        /// 注册策略
        /// </summary>
        /// <param name="strategy">策略实例</param>
        public void RegisterStrategy(INodeOpenStrategy strategy)
        {
            if (strategy == null)
                throw new ArgumentNullException(nameof(strategy));

            _strategies.Add(strategy);
        }

        /// <summary>
        /// 查找合适的策略
        /// </summary>
        /// <param name="context">节点打开上下文</param>
        /// <returns>匹配的策略，如果没有匹配则返回 null</returns>
        public INodeOpenStrategy? FindStrategy(NodeOpenContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            // 遍历所有策略，找到第一个能处理的策略
            foreach (var strategy in _strategies)
            {
                if (strategy.CanHandle(context))
                {
                    return strategy;
                }
            }

            // 没有找到匹配的策略
            return null;
        }

        /// <summary>
        /// 获取默认策略
        /// </summary>
        /// <returns>默认策略实例</returns>
        public INodeOpenStrategy GetDefaultStrategy()
        {
            return _defaultStrategy;
        }

        /// <summary>
        /// 清空所有注册的策略（不包括默认策略）
        /// </summary>
        public void ClearStrategies()
        {
            _strategies.Clear();
        }
    }
}
