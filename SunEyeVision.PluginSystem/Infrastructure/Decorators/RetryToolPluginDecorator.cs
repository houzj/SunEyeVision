using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SunEyeVision.Interfaces;
using SunEyeVision.Models;
using SunEyeVision.PluginSystem.Base.Interfaces;
using SunEyeVision.PluginSystem.Base.Models;
using SunEyeVision.PluginSystem.Base.Base;
using SunEyeVision.PluginSystem.Base.Interfaces;
using SunEyeVision.PluginSystem.Base.Models;
using SunEyeVision.PluginSystem.Base.Base;

namespace SunEyeVision.PluginSystem.Decorators
{
    /// <summary>
    /// 重试工具插件装饰器 - 为工具添加重试功能
    /// </summary>
    public class RetryToolPluginDecorator : IToolPlugin
    {
        private readonly IToolPlugin _innerPlugin;
        private readonly ToolMetadata _toolMetadata;

        public string PluginId => _innerPlugin.PluginId;
        public string Name => $"Retry_{_innerPlugin.Name}";
        public string Version => _innerPlugin.Version;
        public string Author => _innerPlugin.Author;
        public string Description => $"Retry: {_innerPlugin.Description}";
        public string Icon => _innerPlugin.Icon;
        public List<string> Dependencies => _innerPlugin.Dependencies;
        public bool IsLoaded => _innerPlugin.IsLoaded;

        public RetryToolPluginDecorator(IToolPlugin innerPlugin, ToolMetadata toolMetadata)
        {
            _innerPlugin = innerPlugin ?? throw new ArgumentNullException(nameof(innerPlugin));
            _toolMetadata = toolMetadata ?? throw new ArgumentNullException(nameof(toolMetadata));
        }

        public void Initialize()
        {
            _innerPlugin.Initialize();
        }

        public void Unload()
        {
            _innerPlugin.Unload();
        }

        public List<Type> GetAlgorithmNodes()
        {
            return _innerPlugin.GetAlgorithmNodes();
        }

        /// <summary>
        /// 获取工具元数据列表
        /// </summary>
        public List<ToolMetadata> GetToolMetadata()
        {
            return _innerPlugin.GetToolMetadata();
        }

        /// <summary>
        /// 创建工具实例(带重试)
        /// </summary>
        public IImageProcessor CreateToolInstance(string toolId)
        {
            var innerProcessor = _innerPlugin.CreateToolInstance(toolId);
            var metadata = _toolMetadata;

            if (metadata.MaxRetryCount <= 0)
            {
                return innerProcessor;
            }

            return new RetryImageProcessor(innerProcessor, metadata);
        }

        /// <summary>
        /// 获取工具的默认参数
        /// </summary>
        public AlgorithmParameters GetDefaultParameters(string toolId)
        {
            return _innerPlugin.GetDefaultParameters(toolId);
        }

        /// <summary>
        /// 验证参数
        /// </summary>
        public ValidationResult ValidateParameters(string toolId, AlgorithmParameters parameters)
        {
            return _innerPlugin.ValidateParameters(toolId, parameters);
        }

        /// <summary>
        /// 带重试的图像处理器
        /// </summary>
        private class RetryImageProcessor : IImageProcessor
        {
            private readonly IImageProcessor _innerProcessor;
            private readonly ToolMetadata _metadata;
            private int _currentRetryCount = 0;

            public RetryImageProcessor(IImageProcessor innerProcessor, ToolMetadata metadata)
            {
                _innerProcessor = innerProcessor;
                _metadata = metadata;
            }

            public object? Process(object image)
            {
                _currentRetryCount = 0;
                Exception lastException = null;

                while (_currentRetryCount <= _metadata.MaxRetryCount)
                {
                    try
                    {
                        // 执行处理
                        var result = _innerProcessor.Process(image);
                        return result;
                    }
                    catch (Exception ex)
                    {
                        lastException = ex;
                        _currentRetryCount++;

                        if (_currentRetryCount <= _metadata.MaxRetryCount)
                        {
                            // 重试前延迟
                            Thread.Sleep(_metadata.RetryDelayMs);
                        }
                    }
                }

                // 所有重试都失败,抛出最后一个异常
                throw new InvalidOperationException(
                    $"工具执行失败,已重试{_metadata.MaxRetryCount}次",
                    lastException);
            }
        }
    }
}
