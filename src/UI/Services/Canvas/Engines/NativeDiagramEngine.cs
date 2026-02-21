using System;
using System.Windows;
using SunEyeVision.UI.Views.Controls.Canvas;
using SunEyeVision.UI.Services.Canvas;

namespace SunEyeVision.UI.Services.Canvas.Engines
{
    /// <summary>
    /// NativeDiagram画布引擎
    /// 包装NativeDiagramControl，使用原生AIStudio.Wpf.DiagramDesigner库
    /// </summary>
    public class NativeDiagramEngine : ICanvasEngine
    {
        private NativeDiagramControl _control;

        public string EngineName => "NativeDiagram";

        public NativeDiagramEngine()
        {
            _control = new NativeDiagramControl();
            _control.Initialize();
        }

        public FrameworkElement GetControl()
        {
            return _control;
        }

        public void SetDataContext(object dataContext)
        {
            if (_control != null)
            {
                _control.DataContext = dataContext;
            }
        }

        public void SetPathCalculator(string pathCalculatorType)
        {
            // NativeDiagramControl 使用原生贝塞尔曲线，不需要路径计算器设置
            // 此方法用于兼容性
            _control.SetPathCalculator(pathCalculatorType);
        }

        public void Cleanup()
        {
            if (_control != null)
            {
                _control.DataContext = null;
            }
        }
    }
}
