using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using SunEyeVision.UI.Models;

namespace SunEyeVision.UI.ViewModels
{
    /// <summary>
    /// 属性面板视图模�?
    /// </summary>
    public class PropertyPanelViewModel : ViewModelBase
    {
        private WorkflowNode? _selectedNode;
        private ObservableCollection<PropertyItem> _properties;

        public WorkflowNode? SelectedNode
        {
            get => _selectedNode;
            set
            {
                if (SetProperty(ref _selectedNode, value))
                {
                    UpdateProperties();
                }
            }
        }

        public ObservableCollection<PropertyItem> Properties
        {
            get => _properties;
            set => SetProperty(ref _properties, value);
        }

        public ICommand UpdatePropertyCommand { get; }
        public ICommand ResetPropertyCommand { get; }

        public PropertyPanelViewModel()
        {
            _properties = new ObservableCollection<PropertyItem>();
            UpdatePropertyCommand = new RelayCommand<PropertyItem>(ExecuteUpdateProperty);
            ResetPropertyCommand = new RelayCommand<PropertyItem>(ExecuteResetProperty);
        }

        private void UpdateProperties()
        {
            Properties.Clear();

            if (SelectedNode == null)
            {
                return;
            }

            Properties.Add(new PropertyItem("名称", SelectedNode.Name, "string", true));
            Properties.Add(new PropertyItem("类型", SelectedNode.AlgorithmType, "string", false));
            Properties.Add(new PropertyItem("位置X", SelectedNode.PositionX.ToString(), "double", true));
            Properties.Add(new PropertyItem("位置Y", SelectedNode.PositionY.ToString(), "double", true));
            Properties.Add(new PropertyItem("状�?, SelectedNode.Status, "string", false));
            Properties.Add(new PropertyItem("启用", SelectedNode.IsEnabled.ToString(), "boolean", true));

            switch (SelectedNode.AlgorithmType.ToLower())
            {
                case "preprocess":
                    Properties.Add(new PropertyItem("核大�?, "5", "int", true));
                    Properties.Add(new PropertyItem("Sigma", "1.4", "double", true));
                    break;
                case "detection":
                    Properties.Add(new PropertyItem("阈�?, "128", "int", true));
                    Properties.Add(new PropertyItem("方法", "Canny", "enum", true));
                    break;
                case "output":
                    Properties.Add(new PropertyItem("保存路径", "", "string", true));
                    Properties.Add(new PropertyItem("格式", "PNG", "enum", true));
                    break;
            }
        }

        private void ExecuteUpdateProperty(PropertyItem? property)
        {
            if (property == null || SelectedNode == null) return;
            // TODO: 实现属性更新逻辑
        }

        private void ExecuteResetProperty(PropertyItem? property)
        {
            if (property == null) return;
            // TODO: 实现属性重置逻辑
        }
    }

    /// <summary>
    /// 属性项模型
    /// </summary>
    public class PropertyItem
    {
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public bool IsEditable { get; set; }

        public PropertyItem(string name, string value, string type, bool isEditable)
        {
            Name = name;
            Value = value;
            Type = type;
            IsEditable = isEditable;
        }
    }
}
