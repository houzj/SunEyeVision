using System;
using System.Windows;
using System.Windows.Controls;
using SunEyeVision.UI.ViewModels;
using SunEyeVision.UI;
using SunEyeVision.UI.Views.Windows;

namespace SunEyeVision.UI.Views.Controls.Canvas
{
    /// <summary>
    /// 画布模板选择器 - 根据画布类型选择不同的模板
    /// </summary>
    public class CanvasTemplateSelector : DataTemplateSelector
    {
        /// <summary>
        /// WorkflowCanvasControl的模板
        /// </summary>
        public DataTemplate? WorkflowCanvasTemplate { get; set; }

        /// <summary>
        /// NativeDiagramControl的模板（原生AIStudio.Wpf.DiagramDesigner控件）
        /// </summary>
        public DataTemplate? NativeDiagramTemplate { get; set; }

        /// <summary>
        /// 根据画布类型选择模板
        /// </summary>
        public override DataTemplate? SelectTemplate(object item, DependencyObject container)
        {
            if (item is WorkflowTabViewModel workflowViewModel)
            {
                switch (workflowViewModel.CanvasType)
                {
                    case CanvasType.WorkflowCanvas:
                        return WorkflowCanvasTemplate;
                    case CanvasType.NativeDiagram:
                        return NativeDiagramTemplate;
                }
            }
            else
            {
                // 尝试从 Application 获取 MainWindow，然后获取当前选中的 WorkflowTab
                if (Application.Current?.MainWindow is MainWindow mainWindow)
                {
                    if (mainWindow.DataContext is MainWindowViewModel mainWindowViewModel)
                    {
                        var selectedTab = mainWindowViewModel?.WorkflowTabViewModel?.SelectedTab;
                        if (selectedTab != null)
                        {
                            switch (selectedTab.CanvasType)
                            {
                                case CanvasType.WorkflowCanvas:
                                    return WorkflowCanvasTemplate;
                                case CanvasType.NativeDiagram:
                                    return NativeDiagramTemplate;
                            }
                        }
                    }
                }
            }

            // 默认返回 WorkflowCanvasTemplate（改为默认值）
            return WorkflowCanvasTemplate;
        }
    }
}
