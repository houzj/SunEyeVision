using System.Windows.Media;
using SunEyeVision.UI.Models;
using SunEyeVision.UI.Adapters;

namespace SunEyeVision.UI.Adapters
{
    /// <summary>
    /// 图像源节点显示适配器
    /// </summary>
    public class ImageSourceNodeDisplayAdapter : INodeDisplayAdapter
    {
        public string GetDisplayText(WorkflowNode node)
        {
            return $"图像源 {node.Index}";
        }

        public string GetIcon(WorkflowNode node)
        {
            return "📷";
        }

        public Color GetBackgroundColor(WorkflowNode node)
        {
            return Color.FromRgb(240, 248, 255); // 淡蓝色背景
        }

        public Color GetBorderColor(WorkflowNode node)
        {
            return Color.FromRgb(65, 105, 225); // 皇家蓝
        }
    }
}
