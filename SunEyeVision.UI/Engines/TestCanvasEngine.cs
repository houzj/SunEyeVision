using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using SunEyeVision.UI.Interfaces;

namespace SunEyeVision.UI.Engines
{
    /// <summary>
    /// 测试画布引擎
    /// 简化的测试引擎，用于性能测试和算法验证
    /// </summary>
    public class TestCanvasEngine : ICanvasEngine
    {
        private Canvas _control;
        private TextBlock _debugInfo;

        public string EngineName => "TestCanvas";

        public TestCanvasEngine()
        {
            _control = new Canvas
            {
                Background = new SolidColorBrush(Color.FromRgb(240, 240, 240)),
                Width = 5000,
                Height = 5000
            };

            // 添加调试信息
            _debugInfo = new TextBlock
            {
                Text = "测试画布引擎\n用于性能测试和算法验证",
                FontSize = 24,
                Foreground = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            Canvas.SetLeft(_debugInfo, 2500);
            Canvas.SetTop(_debugInfo, 2500);
            _control.Children.Add(_debugInfo);
        }

        public FrameworkElement GetControl()
        {
            return _control;
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
                _debugInfo.Text = $"测试画布引擎\n当前路径算法: {pathCalculatorType}\n用于性能测试和算法验证";
            }
        }

        public void Cleanup()
        {
            _control.Children.Clear();
        }
    }
}
