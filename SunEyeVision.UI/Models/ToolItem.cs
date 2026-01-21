using System;

namespace SunEyeVision.UI.Models
{
    /// <summary>
    /// 工具箱项模型
    /// </summary>
    public class ToolItem
    {
        public string Name { get; set; }
        public string Category { get; set; }
        public string Icon { get; set; }
        public string Description { get; set; }
        public string AlgorithmType { get; set; }

        public ToolItem(string name, string category, string icon, string description, string algorithmType = null)
        {
            Name = name;
            Category = category;
            Icon = icon;
            Description = description;
            AlgorithmType = algorithmType;
        }
    }

    /// <summary>
    /// 工具箱分类模型
    /// </summary>
    public class ToolCategory
    {
        public string Name { get; set; }
        public string Icon { get; set; }
        public string Description { get; set; }
        public int ToolCount { get; set; }

        public ToolCategory(string name, string icon, string description, int toolCount = 0)
        {
            Name = name;
            Icon = icon;
            Description = description;
            ToolCount = toolCount;
        }
    }
}
