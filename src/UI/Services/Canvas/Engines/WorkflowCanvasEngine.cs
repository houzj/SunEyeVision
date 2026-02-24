using System;
using System.Windows;
using SunEyeVision.UI.Views.Controls.Canvas;
using SunEyeVision.UI.Services.Canvas;
using SunEyeVision.UI.Services;
using SunEyeVision.UI.Services.PathCalculators;

namespace SunEyeVision.UI.Services.Canvas.Engines
{
    /// <summary>
    /// WorkflowCanvas画布引擎
    /// 包装现有的WorkflowCanvasControl，不修改其内部逻辑
    /// </summary>
    public class WorkflowCanvasEngine : ICanvasEngine
    {
        private WorkflowCanvasControl _control;

        public string EngineName => "WorkflowCanvas";

        public WorkflowCanvasEngine()
        {
            _control = new WorkflowCanvasControl();
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
            // 调用控件的SetPathCalculator方法，实现实际的路径计算器切�?
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
