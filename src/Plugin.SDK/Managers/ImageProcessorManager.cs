using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OpenCvSharp;
using SunEyeVision.Plugin.SDK.Core;

namespace SunEyeVision.Plugin.SDK.Managers
{
    /// <summary>
    /// 图像处理器管理器
    /// </summary>
    /// <remarks>
    /// 提供图像处理器的注册、查询和执行功能。
    /// 线程安全的实现，支持并发访问。
    /// </remarks>
    public class ImageProcessorManager : IImageProcessorManager
    {
        private readonly Dictionary<string, IImageProcessor> _processors;
        private readonly Dictionary<Type, List<IParametricImageProcessor>> _parametricProcessors;
        private readonly object _lock = new();

        /// <summary>
        /// 构造函数
        /// </summary>
        public ImageProcessorManager()
        {
            _processors = new Dictionary<string, IImageProcessor>(StringComparer.OrdinalIgnoreCase);
            _parametricProcessors = new Dictionary<Type, List<IParametricImageProcessor>>();
        }

        /// <summary>
        /// 注册图像处理器
        /// </summary>
        public void RegisterProcessor(string name, IImageProcessor processor)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("处理器名称不能为空", nameof(name));

            if (processor == null)
                throw new ArgumentNullException(nameof(processor));

            lock (_lock)
            {
                _processors[name] = processor;

                // 如果支持参数，也注册到参数索引
                if (processor is IParametricImageProcessor parametricProcessor)
                {
                    var paramType = parametricProcessor.ParameterType;
                    if (!_parametricProcessors.ContainsKey(paramType))
                    {
                        _parametricProcessors[paramType] = new List<IParametricImageProcessor>();
                    }
                    _parametricProcessors[paramType].Add(parametricProcessor);
                }
            }
        }

        /// <summary>
        /// 注销图像处理器
        /// </summary>
        public bool UnregisterProcessor(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            lock (_lock)
            {
                if (_processors.TryGetValue(name, out var processor))
                {
                    _processors.Remove(name);

                    // 从参数索引中移除
                    if (processor is IParametricImageProcessor parametricProcessor)
                    {
                        var paramType = parametricProcessor.ParameterType;
                        if (_parametricProcessors.TryGetValue(paramType, out var list))
                        {
                            list.Remove(parametricProcessor);
                            if (list.Count == 0)
                            {
                                _parametricProcessors.Remove(paramType);
                            }
                        }
                    }

                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// 获取图像处理器
        /// </summary>
        public IImageProcessor? GetProcessor(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            lock (_lock)
            {
                return _processors.TryGetValue(name, out var processor) ? processor : null;
            }
        }

        /// <summary>
        /// 检查处理器是否存在
        /// </summary>
        public bool HasProcessor(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            lock (_lock)
            {
                return _processors.ContainsKey(name);
            }
        }

        /// <summary>
        /// 获取所有处理器名称
        /// </summary>
        public IEnumerable<string> GetProcessorNames()
        {
            lock (_lock)
            {
                return _processors.Keys.ToList();
            }
        }

        /// <summary>
        /// 按参数类型查找处理器
        /// </summary>
        public IEnumerable<IParametricImageProcessor> FindProcessorsByParameterType(Type parameterType)
        {
            if (parameterType == null)
                return Enumerable.Empty<IParametricImageProcessor>();

            lock (_lock)
            {
                return _parametricProcessors.TryGetValue(parameterType, out var processors)
                    ? processors.ToList()
                    : Enumerable.Empty<IParametricImageProcessor>();
            }
        }

        /// <summary>
        /// 处理图像
        /// </summary>
        public Mat Process(string processorName, Mat input, object? parameters = null)
        {
            var processor = GetProcessor(processorName);
            if (processor == null)
                throw new InvalidOperationException($"处理器 '{processorName}' 未找到");

            if (processor is IParametricImageProcessor parametricProcessor && parameters != null)
            {
                return parametricProcessor.Process(input, parameters);
            }

            return processor.Process(input);
        }

        /// <summary>
        /// 处理图像（带矩形ROI）
        /// </summary>
        public Mat Process(string processorName, Mat input, Rect roi, object? parameters = null)
        {
            var processor = GetProcessor(processorName);
            if (processor == null)
                throw new InvalidOperationException($"处理器 '{processorName}' 未找到");

            if (processor is IParametricImageProcessor parametricProcessor && parameters != null)
            {
                return parametricProcessor.Process(input, parameters, roi);
            }

            return processor.Process(input, roi);
        }

        /// <summary>
        /// 处理图像（带圆形ROI）
        /// </summary>
        public Mat Process(string processorName, Mat input, Point2f center, float radius, object? parameters = null)
        {
            var processor = GetProcessor(processorName);
            if (processor == null)
                throw new InvalidOperationException($"处理器 '{processorName}' 未找到");

            if (processor is IParametricImageProcessor parametricProcessor && parameters != null)
            {
                return parametricProcessor.Process(input, parameters, center, radius);
            }

            return processor.Process(input, center, radius);
        }

        /// <summary>
        /// 异步处理图像
        /// </summary>
        public async Task<Mat> ProcessAsync(string processorName, Mat input, object? parameters = null, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() => Process(processorName, input, parameters), cancellationToken);
        }

        /// <summary>
        /// 异步处理图像（带矩形ROI）
        /// </summary>
        public async Task<Mat> ProcessAsync(string processorName, Mat input, Rect roi, object? parameters = null, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() => Process(processorName, input, roi, parameters), cancellationToken);
        }

        /// <summary>
        /// 异步处理图像（带圆形ROI）
        /// </summary>
        public async Task<Mat> ProcessAsync(string processorName, Mat input, Point2f center, float radius, object? parameters = null, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() => Process(processorName, input, center, radius, parameters), cancellationToken);
        }

        /// <summary>
        /// 清空所有处理器
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                _processors.Clear();
                _parametricProcessors.Clear();
            }
        }
    }
}
