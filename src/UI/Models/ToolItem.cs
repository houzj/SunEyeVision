using System;
using System.ComponentModel;

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
        public string ToolId { get; set; }
        public string AlgorithmType { get; set; }
        public bool HasDebugInterface { get; set; }

        public ToolItem(string name, string category, string icon, string description, string algorithmType = null, bool hasDebugInterface = true)
        {
            Name = name;
            Category = category;
            Icon = icon;
            Description = description;
            ToolId = algorithmType; // 向后兼容：如果没有显式设置ToolId，使用algorithmType
            AlgorithmType = algorithmType;
            HasDebugInterface = hasDebugInterface;
        }
    }

    /// <summary>
    /// 工具箱分类模�?
    /// </summary>
    public class ToolCategory : INotifyPropertyChanged
    {
        private bool _isExpanded;
        private bool _isSelected;
        private System.Collections.ObjectModel.ObservableCollection<ToolItem>? _tools;

        public string Name { get; set; }
        public string Icon { get; set; }
        public string Description { get; set; }
        public int ToolCount { get; set; }

        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsExpanded)));
                }
            }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
                }
            }
        }

        public System.Collections.ObjectModel.ObservableCollection<ToolItem>? Tools
        {
            get => _tools;
            set
            {
                _tools = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Tools)));
            }
        }

        public ToolCategory(string name, string icon, string description, int toolCount = 0, bool isExpanded = true)
        {
            Name = name;
            Icon = icon;
            Description = description;
            ToolCount = toolCount;
            IsExpanded = isExpanded;
            IsSelected = false;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
