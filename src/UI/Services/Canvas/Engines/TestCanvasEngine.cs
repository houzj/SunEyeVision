using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using SunEyeVision.UI.Services.Canvas;

namespace SunEyeVision.UI.Services.Canvas.Engines
{
    /// <summary>
    /// 测试画布引擎
    /// 简化的测试引擎，用于性能测试和算法验证
    /// </summary>
    public class TestCanvasEngine : ICanvasEngine
    {
        private System.Windows.Controls.Canvas _control;
        private TextBlock _debugInfo;

        public string EngineName => "TestCanvas";

        public TestCanvasEngine()
        {
            _control = new System.Windows.Controls.Canvas
            {
                Background = new SolidColorBrush(Color.FromRgb(240, 240, 240)),
                Width = 5000,
                Height = 5000
            };

            // 添加调试信息
            _debugInfo = new TextBlock
            {
                Text = "测试引擎"
            };
            _control.Children.Add(_debugInfo);
        }

        public void SetDataContext(object dataContext)
        {
            // 测试引擎可能不需要DataContext
            // 预留接口
        }

        public void SetPathCalculator(string pathCalculatorType)
        {
            // 更新调试信息
            if (_debugInfo != null)
            {
                _debugInfo.Text = $"验证: {pathCalculatorType}";
            }
        }

        public System.Windows.FrameworkElement GetControl()
        {
            return _control;
        }

        public void Cleanup()
        {
            _control.Children.Clear();
        }
    }
}
