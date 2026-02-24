using System;
using SunEyeVision.Core;
using SunEyeVision.Core.Interfaces;

namespace SunEyeVision.Core.Services
{
    /// <summary>
    /// 日志管理�?- 全局单例，统一管理日志
    /// </summary>
    public static class LogManager
    {
        private static ILogger? _instance;
        private static readonly object _lockObj = new object();

        /// <summary>
        /// 获取日志实例
        /// </summary>
        public static ILogger Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lockObj)
                    {
                        if (_instance == null)
                        {
                            _instance = CreateDefaultLogger();
                        }
                    }
                }
                return _instance;
            }
            set
            {
                lock (_lockObj)
                {
                    _instance = value;
                }
            }
        }

        /// <summary>
        /// 获取优化的日志实�?
        /// </summary>
        public static OptimizedLogger? OptimizedInstance => Instance as OptimizedLogger;

        /// <summary>
        /// 创建默认日志记录�?
        /// </summary>
        private static ILogger CreateDefaultLogger()
        {
            return new OptimizedLogger(
                minLevel: LogLevel.Info,
                sampleRate: 100
            );
        }

        /// <summary>
        /// 设置日志级别
        /// </summary>
        public static void SetLogLevel(LogLevel level)
        {
            if (Instance is OptimizedLogger optimizedLogger)
            {
                optimizedLogger.CurrentLevel = level;
            }
        }

        /// <summary>
        /// 设置采样�?
        /// </summary>
        public static void SetSampleRate(int rate)
        {
            if (Instance is OptimizedLogger optimizedLogger)
            {
                // 需要重新创建实�?
                Instance = new OptimizedLogger(
                    minLevel: optimizedLogger.CurrentLevel,
                    sampleRate: rate
                );
            }
        }
    }
}
