using System;
using System.Windows;
using System.Windows.Controls;
using SunEyeVision.UI.Views.Controls;
using SunEyeVision.UI.Services.Canvas;
using SunEyeVision.UI.Services.Canvas.Engines;
using SunEyeVision.UI.ViewModels;

namespace SunEyeVision.UI.Services.Canvas
{
    /// <summary>
    /// 画布引擎管理�?
    /// 静态管理器，用于创建、切换和管理画布引擎
    /// 支持通过后台代码进行画布切换，无需配置面板
    /// </summary>
    public static class CanvasEngineManager
    {
        private static ICanvasEngine? _currentEngine;
        private static object? _dataContext;

        /// <summary>
        /// 创建指定类型的画布引�?
        /// </summary>
        public static ICanvasEngine CreateEngine(string engineType)
        {
            return engineType.ToLower() switch
            {
                "workflow" => new WorkflowCanvasEngine(),
                "native" => new NativeDiagramEngine(),
                "test" => new TestCanvasEngine(),
                _ => throw new ArgumentException($"不支持的画布引擎类型: {engineType}")
            };
        }

        /// <summary>
        /// 切换画布引擎
        /// </summary>
        public static ICanvasEngine SwitchEngine(string engineType, Decorator container)
        {
            // 清理旧引�?
            _currentEngine?.Cleanup();

            // 创建新引�?
            var newEngine = CreateEngine(engineType);

            // 如果有数据上下文，设置到新引�?
            if (_dataContext != null)
            {
                newEngine.SetDataContext(_dataContext);
            }

            // 替换容器内容
            var control = newEngine.GetControl();
            container.Child = control;

            // 更新当前引擎
            _currentEngine = newEngine;

            return newEngine;
        }

        /// <summary>
        /// 设置当前引擎的数据上下文
        /// </summary>
        public static void SetDataContext(object dataContext)
        {
            _dataContext = dataContext;
            _currentEngine?.SetDataContext(dataContext);
        }

        /// <summary>
        /// 设置路径计算�?
        /// </summary>
        public static void SetPathCalculator(string pathCalculatorType)
        {
            _currentEngine?.SetPathCalculator(pathCalculatorType);
        }

        /// <summary>
        /// 获取当前引擎
        /// </summary>
        public static ICanvasEngine? GetCurrentEngine()
        {
            return _currentEngine;
        }

        /// <summary>
        /// 重置管理�?
        /// </summary>
        public static void Reset()
        {
            _currentEngine?.Cleanup();
            _currentEngine = null;
            _dataContext = null;
        }
    }
}
