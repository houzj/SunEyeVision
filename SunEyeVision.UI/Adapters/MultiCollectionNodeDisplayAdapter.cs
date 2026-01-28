using System.Windows.Media;
using SunEyeVision.UI.Models;

namespace SunEyeVision.UI.Adapters
{
    /// <summary>
    /// 多集合节点显示适配器
    /// </summary>
    public class MultiCollectionNodeDisplayAdapter : INodeDisplayAdapter
    {
        public string GetDisplayText(WorkflowNode node)
        {
            return $"多集合 {node.Index}";
        }

        public string GetIcon(WorkflowNode node)
        {
            return "🔄";
        }

        public Color GetBackgroundColor(WorkflowNode node)
        {
            return Color.FromRgb(245, 245, 245); // 淡灰色背景
        }

        public Color GetBorderColor(WorkflowNode node)
        {
            return Color.FromRgb(128, 128, 128); // 灰色
        }
    }
}
