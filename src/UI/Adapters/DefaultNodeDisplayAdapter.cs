using System.Windows.Media;
using SunEyeVision.UI.Models;

namespace SunEyeVision.UI.Adapters
{
    /// <summary>
    /// 默认节点显示适配器实现
    /// </summary>
    public class DefaultNodeDisplayAdapter : INodeDisplayAdapter
    {
        public string GetDisplayText(WorkflowNode node)
        {
            return $"{node.Name} {node.Index}";
        }

        public string GetIcon(WorkflowNode node)
        {
            return node.NodeTypeIcon;
        }

        public Color GetBackgroundColor(WorkflowNode node)
        {
            return Colors.White;
        }

        public Color GetBorderColor(WorkflowNode node)
        {
            return Color.FromRgb(255, 149, 0); // #ff9500
        }
    }
}
