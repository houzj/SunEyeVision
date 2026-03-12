using System;
using System.Collections.Concurrent;
using System.Threading;
using SunEyeVision.Workflow;
using SunEyeVision.Plugin.SDK.Logging;

namespace SunEyeVision.UI.Services.Performance
{
    /// <summary>
    /// 工作流引擎池 - 避免频繁创建/销毁引擎实例
    /// 使用对象池模式优化性能
    /// </summary>
    public class WorkflowEnginePool : IDisposable
    {
        private readonly ConcurrentBag<WorkflowEngine> _pool;
        private readonly Func<WorkflowEngine> _factory;
        private readonly int _maxPoolSize;
        private int _currentCount;
        private bool _disposed;
        private readonly object _lock = new object();

        /// <summary>
        /// 创建工作流引擎池
        /// </summary>
        /// <param name="maxPoolSize">最大池大小（默认3个）</param>
        /// <param name="factory">引擎创建工厂（可选）</param>
        public WorkflowEnginePool(int maxPoolSize = 3, Func<WorkflowEngine>? factory = null)
        {
            _maxPoolSize = maxPoolSize;
            _pool = new ConcurrentBag<WorkflowEngine>();
            _factory = factory ?? CreateDefaultEngine;
            _currentCount = 0;

            // 预热：创建一个实例
            Prewarm(1);
        }

        /// <summary>
        /// 默认引擎创建方法
        /// </summary>
        private WorkflowEngine CreateDefaultEngine()
        {
            // WorkflowEngine需要ILogger参数
            // 使用VisionLogger.Instance作为默认
            return new WorkflowEngine(VisionLogger.Instance);
        }

        /// <summary>
        /// 预热：提前创建引擎实例
        /// </summary>
        /// <param name="count">要创建的实例数量</param>
        public void Prewarm(int count)
        {
            for (int i = 0; i < count && _currentCount < _maxPoolSize; i++)
            {
                var engine = _factory();
                _pool.Add(engine);
                Interlocked.Increment(ref _currentCount);
            }
        }

        /// <summary>
        /// 从池中获取引擎实例
        /// </summary>
        /// <returns>引擎实例</returns>
        public WorkflowEngine Rent()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(WorkflowEnginePool));

            if (_pool.TryTake(out var engine))
            {
                return engine;
            }

            // 池为空，创建新实例
            lock (_lock)
            {
                if (_currentCount < _maxPoolSize)
                {
                    Interlocked.Increment(ref _currentCount);
                    return _factory();
                }
            }

            // 已达最大数量，等待或创建临时实例
            // 这里简单起见，创建临时实例（不会被归还到池中）
            return _factory();
        }

        /// <summary>
        /// 将引擎实例归还到池中
        /// </summary>
        /// <param name="engine">引擎实例</param>
        public void Return(WorkflowEngine engine)
        {
            if (_disposed || engine == null)
                return;

            // 重置引擎状态（如果有需要）
            // engine.Reset();

            // 只有未达最大数量才归还
            if (_currentCount <= _maxPoolSize)
            {
                _pool.Add(engine);
            }
            // 否则丢弃（让GC回收）
        }

        /// <summary>
        /// 清空池
        /// </summary>
        public void Clear()
        {
            while (_pool.TryTake(out _))
            {
                // 清空池
            }
            _currentCount = 0;
        }

        /// <summary>
        /// 获取池中可用实例数量
        /// </summary>
        public int AvailableCount => _pool.Count;

        /// <summary>
        /// 获取已创建的实例总数
        /// </summary>
        public int TotalCount => _currentCount;

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            Clear();
        }
    }
}
