using System;
using System.Windows;
using System.Windows.Controls;
using SunEyeVision.UI.ViewModels;

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
        /// AIStudioDiagramControl的模板
        /// </summary>
        public DataTemplate? AIStudioDiagramTemplate { get; set; }

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
                    case CanvasType.AIStudioDiagram:
                        System.Diagnostics.Debug.WriteLine("[CanvasTemplateSelector] Returning AIStudioDiagramTemplate");
                        return AIStudioDiagramTemplate;
                }
            }
            else if (item != null)
            {
                System.Diagnostics.Debug.WriteLine($"[CanvasTemplateSelector] ✗ item is not WorkflowTabViewModel");
            }

            System.Diagnostics.Debug.WriteLine($"[CanvasTemplateSelector] Returning WorkflowCanvasTemplate as fallback");
            // 默认返回WorkflowCanvasTemplate
            return WorkflowCanvasTemplate;
        }
    }
}
