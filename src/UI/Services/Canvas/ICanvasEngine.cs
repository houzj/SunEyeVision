using System.Windows;
using SunEyeVision.UI.Services.Canvas;

namespace SunEyeVision.UI.Services.Canvas
{
    /// <summary>
    /// 画布引擎统一接口
    /// 所有画布引擎都应实现此接口，提供统一的管理方�?
    /// </summary>
    public interface ICanvasEngine
    {
        /// <summary>
        /// 引擎名称
        /// </summary>
        string EngineName { get; }

        /// <summary>
        /// 获取画布控件
        /// </summary>
        FrameworkElement GetControl();

        /// <summary>
        /// 设置数据上下�?
        /// </summary>
        void SetDataContext(object dataContext);

        /// <summary>
        /// 设置路径计算�?
        /// </summary>
        void SetPathCalculator(string pathCalculatorType);

        /// <summary>
        /// 清理资源
        /// </summary>
        void Cleanup();
    }
}
