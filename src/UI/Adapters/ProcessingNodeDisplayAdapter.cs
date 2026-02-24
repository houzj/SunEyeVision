using System.Windows.Media;
using SunEyeVision.UI.Models;
using SunEyeVision.UI.Adapters;

namespace SunEyeVision.UI.Adapters
{
    /// <summary>
    /// 处理节点显示适配�?
    /// </summary>
    public class ProcessingNodeDisplayAdapter : INodeDisplayAdapter
    {
        public string GetDisplayText(WorkflowNode node)
        {
            return $"处理 {node.Index}";
        }

        public string GetIcon(WorkflowNode node)
        {
            return "⚙️";
        }

        public Color GetBackgroundColor(WorkflowNode node)
        {
            return Color.FromRgb(240, 255, 240); // 淡绿色背�?
        }

        public Color GetBorderColor(WorkflowNode node)
        {
            return Color.FromRgb(34, 139, 34); // 森林�?
        }
    }
}
