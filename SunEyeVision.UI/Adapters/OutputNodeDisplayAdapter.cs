using System.Windows.Media;
using SunEyeVision.UI.Models;

namespace SunEyeVision.UI.Adapters
{
    /// <summary>
    /// 输出节点显示适配器
    /// </summary>
    public class OutputNodeDisplayAdapter : INodeDisplayAdapter
    {
        public string GetDisplayText(WorkflowNode node)
        {
            return $"输出 {node.Index}";
        }

        public string GetIcon(WorkflowNode node)
        {
            return "📤";
        }

        public Color GetBackgroundColor(WorkflowNode node)
        {
            return Color.FromRgb(255, 248, 240); // 淡黄色背景
        }

        public Color GetBorderColor(WorkflowNode node)
        {
            return Color.FromRgb(255, 165, 0); // 橙色
        }
    }
}
