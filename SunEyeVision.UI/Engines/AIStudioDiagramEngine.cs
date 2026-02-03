using System;
using System.Windows;
using SunEyeVision.UI.Controls;
using SunEyeVision.UI.Interfaces;

namespace SunEyeVision.UI.Engines
{
    /// <summary>
    /// AIStudioDiagram画布引擎
    /// 包装现有的AIStudioDiagramControl，不修改其内部逻辑
    /// </summary>
    public class AIStudioDiagramEngine : ICanvasEngine
    {
        private AIStudioDiagramControl _control;

        public string EngineName => "AIStudioDiagram";

        public AIStudioDiagramEngine()
        {
            _control = new AIStudioDiagramControl();
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
            // 调用控件的SetPathCalculator方法，实现实际的路径计算器切换
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
