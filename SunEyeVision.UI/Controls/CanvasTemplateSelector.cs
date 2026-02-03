using System;
using System.Windows;
using System.Windows.Controls;
using SunEyeVision.UI.ViewModels;
using SunEyeVision.UI;

namespace SunEyeVision.UI.Controls
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
            System.Diagnostics.Debug.WriteLine($"[CanvasTemplateSelector] ===== SelectTemplate called =====");
            System.Diagnostics.Debug.WriteLine($"[CanvasTemplateSelector] item: {item}");
            System.Diagnostics.Debug.WriteLine($"[CanvasTemplateSelector] item type: {item?.GetType().FullName ?? "null"}");

            if (item is WorkflowTabViewModel workflowViewModel)
            {
                System.Diagnostics.Debug.WriteLine($"[CanvasTemplateSelector] ✓ WorkflowTabViewModel found");
                System.Diagnostics.Debug.WriteLine($"[CanvasTemplateSelector]   CanvasType: {workflowViewModel.CanvasType}");
                System.Diagnostics.Debug.WriteLine($"[CanvasTemplateSelector]   Name: {workflowViewModel.Name}");

                switch (workflowViewModel.CanvasType)
                {
                    case CanvasType.WorkflowCanvas:
                        System.Diagnostics.Debug.WriteLine("[CanvasTemplateSelector] Returning WorkflowCanvasTemplate");
                        return WorkflowCanvasTemplate;
                    case CanvasType.NativeDiagram:
                        System.Diagnostics.Debug.WriteLine("[CanvasTemplateSelector] Returning NativeDiagramTemplate");
                        return NativeDiagramTemplate;
                }
            }
            else if (item != null)
            {
                System.Diagnostics.Debug.WriteLine($"[CanvasTemplateSelector] ✗ item is not WorkflowTabViewModel");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[CanvasTemplateSelector] ⚠ item is null, trying to get CanvasType from Application");

                // 尝试从 Application 获取 MainWindow，然后获取当前选中的 WorkflowTab
                if (Application.Current?.MainWindow is MainWindow mainWindow)
                {
                    if (mainWindow.DataContext is MainWindowViewModel mainWindowViewModel)
                    {
                        var selectedTab = mainWindowViewModel?.WorkflowTabViewModel?.SelectedTab;
                        if (selectedTab != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"[CanvasTemplateSelector] ✓ Got CanvasType from SelectedTab: {selectedTab.CanvasType}");
                            switch (selectedTab.CanvasType)
                            {
                                case CanvasType.WorkflowCanvas:
                                    System.Diagnostics.Debug.WriteLine("[CanvasTemplateSelector] Returning WorkflowCanvasTemplate");
                                    return WorkflowCanvasTemplate;
                                case CanvasType.NativeDiagram:
                                    System.Diagnostics.Debug.WriteLine("[CanvasTemplateSelector] Returning NativeDiagramTemplate");
                                    return NativeDiagramTemplate;
                            }
                        }
                    }
                }
            }

            System.Diagnostics.Debug.WriteLine($"[CanvasTemplateSelector] Returning WorkflowCanvasTemplate as default (changed from NativeDiagramTemplate)");
            // 默认返回 WorkflowCanvasTemplate（改为默认值）
            return WorkflowCanvasTemplate;
        }
    }
}
