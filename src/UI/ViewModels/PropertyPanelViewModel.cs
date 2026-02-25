using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Windows.Input;
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.SDK.Execution.Results;
using SunEyeVision.UI.Controls.ParameterBinding;
using SunEyeVision.UI.Models;

using WorkflowNode = SunEyeVision.UI.Models.WorkflowNode;

namespace SunEyeVision.UI.ViewModels
{
    /// <summary>
    /// 属性面板视图模型
    /// </summary>
    /// <remarks>
    /// 管理节点属性显示和编辑，集成参数绑定功能。
    /// 
    /// 核心功能：
    /// 1. 显示节点基本信息
    /// 2. 显示工具参数
    /// 3. 管理参数绑定
    /// 4. 提供数据源查询服务
    /// </remarks>
    public class PropertyPanelViewModel : ViewModelBase
    {
        #region 字段

        private WorkflowNode? _selectedNode;
        private ObservableCollection<PropertyItem> _properties;
        private ObservableCollection<ParameterBindingViewModel> _parameterBindings;
        private readonly IDataSourceQueryService? _dataSourceQueryService;
        private string _currentNodeId = string.Empty;

        #endregion

        #region 属性

        /// <summary>
        /// 选中的节点
        /// </summary>
        public WorkflowNode? SelectedNode
        {
            get => _selectedNode;
            set
            {
                if (SetProperty(ref _selectedNode, value))
                {
                    UpdateProperties();
                    UpdateParameterBindings();
                }
            }
        }

        /// <summary>
        /// 基本属性列表
        /// </summary>
        public ObservableCollection<PropertyItem> Properties
        {
            get => _properties;
            set => SetProperty(ref _properties, value);
        }

        /// <summary>
        /// 参数绑定列表
        /// </summary>
        public ObservableCollection<ParameterBindingViewModel> ParameterBindings
        {
            get => _parameterBindings;
            set => SetProperty(ref _parameterBindings, value);
        }

        /// <summary>
        /// 是否有选中的节点
        /// </summary>
        public bool HasSelectedNode => SelectedNode != null;

        /// <summary>
        /// 节点名称
        /// </summary>
        public string NodeName => SelectedNode?.Name ?? "未选择节点";

        /// <summary>
        /// 节点类型
        /// </summary>
        public string NodeType => SelectedNode?.AlgorithmType ?? "";

        /// <summary>
        /// 是否显示参数绑定区域
        /// </summary>
        public bool ShowParameterBindings => SelectedNode != null && ParameterBindings.Count > 0;

        #endregion

        #region 命令

        /// <summary>
        /// 更新属性命令
        /// </summary>
        public ICommand UpdatePropertyCommand { get; }

        /// <summary>
        /// 重置属性命令
        /// </summary>
        public ICommand ResetPropertyCommand { get; }

        /// <summary>
        /// 应用所有绑定命令
        /// </summary>
        public ICommand ApplyAllBindingsCommand { get; }

        /// <summary>
        /// 重置所有绑定命令
        /// </summary>
        public ICommand ResetAllBindingsCommand { get; }

        #endregion

        #region 构造函数

        /// <summary>
        /// 创建属性面板视图模型
        /// </summary>
        public PropertyPanelViewModel() : this(null)
        {
        }

        /// <summary>
        /// 创建属性面板视图模型（带数据源查询服务）
        /// </summary>
        public PropertyPanelViewModel(IDataSourceQueryService? dataSourceQueryService)
        {
            _properties = new ObservableCollection<PropertyItem>();
            _parameterBindings = new ObservableCollection<ParameterBindingViewModel>();
            _dataSourceQueryService = dataSourceQueryService;

            UpdatePropertyCommand = new RelayCommand<PropertyItem>(ExecuteUpdateProperty);
            ResetPropertyCommand = new RelayCommand<PropertyItem>(ExecuteResetProperty);
            ApplyAllBindingsCommand = new RelayCommand(ExecuteApplyAllBindings, CanApplyAllBindings);
            ResetAllBindingsCommand = new RelayCommand(ExecuteResetAllBindings, CanResetAllBindings);
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 设置当前节点ID（用于刷新数据源）
        /// </summary>
        public void SetCurrentNodeId(string nodeId)
        {
            _currentNodeId = nodeId;

            // 刷新所有参数绑定的数据源
            foreach (var binding in ParameterBindings)
            {
                binding.RefreshAvailableDataSources(nodeId);
            }
        }

        /// <summary>
        /// 刷新属性面板
        /// </summary>
        public void Refresh()
        {
            UpdateProperties();
            UpdateParameterBindings();
        }

        /// <summary>
        /// 获取所有参数绑定配置
        /// </summary>
        public Dictionary<string, ParameterBinding> GetAllBindings()
        {
            var result = new Dictionary<string, ParameterBinding>();

            foreach (var bindingVm in ParameterBindings)
            {
                result[bindingVm.ParameterName] = bindingVm.GetBinding();
            }

            return result;
        }

        /// <summary>
        /// 应用绑定配置到节点
        /// </summary>
        public void ApplyBindingsToNode()
        {
            if (SelectedNode == null)
                return;

            var bindings = GetAllBindings();
            foreach (var kvp in bindings)
            {
                SelectedNode.ParameterBindings.SetBinding(kvp.Value);
            }
        }

        #endregion

        #region 私有方法

        private void UpdateProperties()
        {
            Properties.Clear();
            OnPropertyChanged(nameof(HasSelectedNode));
            OnPropertyChanged(nameof(NodeName));
            OnPropertyChanged(nameof(NodeType));

            if (SelectedNode == null)
            {
                return;
            }

            // 基本信息
            Properties.Add(new PropertyItem("名称", SelectedNode.Name, "string", true));
            Properties.Add(new PropertyItem("类型", SelectedNode.AlgorithmType, "string", false));
            Properties.Add(new PropertyItem("启用", SelectedNode.IsEnabled.ToString(), "boolean", true));

            // 根据算法类型添加特定属性
            AddAlgorithmSpecificProperties();
        }

        private void AddAlgorithmSpecificProperties()
        {
            if (SelectedNode == null)
                return;

            // 从 Parameters 中提取属性
            if (SelectedNode.Parameters != null)
            {
                var paramDict = SelectedNode.Parameters.ToDictionary();
                foreach (var kvp in paramDict)
                {
                    var type = kvp.Value?.GetType()?.Name ?? "object";
                    Properties.Add(new PropertyItem(kvp.Key, kvp.Value?.ToString() ?? "", type, true));
                }
            }
        }

        private void UpdateParameterBindings()
        {
            ParameterBindings.Clear();
            OnPropertyChanged(nameof(ShowParameterBindings));

            if (SelectedNode == null || string.IsNullOrEmpty(SelectedNode.AlgorithmType))
                return;

            // 获取工具的参数定义
            var parameterDefinitions = GetToolParameterDefinitions(SelectedNode.AlgorithmType);

            // 创建参数绑定ViewModel
            foreach (var paramDef in parameterDefinitions)
            {
                // 检查是否已有绑定配置
                var existingBinding = SelectedNode.ParameterBindings?.GetBinding(paramDef.Name);

                ParameterBindingViewModel bindingVm;
                if (existingBinding != null)
                {
                    // 从现有绑定创建
                    bindingVm = ParameterBindingViewModel.FromBinding(
                        existingBinding,
                        paramDef.DisplayName,
                        paramDef.Type,
                        paramDef.DefaultValue,
                        paramDef.Description,
                        _dataSourceQueryService);
                }
                else
                {
                    // 创建新绑定
                    bindingVm = new ParameterBindingViewModel(
                        paramDef.Name,
                        paramDef.DisplayName,
                        paramDef.Type,
                        paramDef.DefaultValue,
                        paramDef.Description,
                        _dataSourceQueryService);
                }

                // 订阅绑定变更事件
                bindingVm.BindingChanged += OnBindingChanged;
                bindingVm.DataSourceSelectionRequested += OnDataSourceSelectionRequested;

                // 刷新可用数据源
                if (!string.IsNullOrEmpty(_currentNodeId))
                {
                    bindingVm.RefreshAvailableDataSources(_currentNodeId);
                }

                ParameterBindings.Add(bindingVm);
            }

            OnPropertyChanged(nameof(ShowParameterBindings));
        }

        private List<ParameterDefinition> GetToolParameterDefinitions(string algorithmType)
        {
            var definitions = new List<ParameterDefinition>();

            // 尝试从ITool接口获取参数定义
            try
            {
                // 这里需要从插件管理器或工具注册表获取工具类型
                // 简化实现：返回常见参数
                definitions = GetDefaultParameterDefinitions(algorithmType);
            }
            catch (Exception)
            {
                // 如果获取失败，返回默认参数
                definitions = GetDefaultParameterDefinitions(algorithmType);
            }

            return definitions;
        }

        private List<ParameterDefinition> GetDefaultParameterDefinitions(string algorithmType)
        {
            return algorithmType.ToLower() switch
            {
                "preprocess" => new List<ParameterDefinition>
                {
                    new ParameterDefinition("KernelSize", "核大小", typeof(int), 5, "卷积核大小"),
                    new ParameterDefinition("Sigma", "Sigma值", typeof(double), 1.4, "高斯滤波参数")
                },
                "detection" => new List<ParameterDefinition>
                {
                    new ParameterDefinition("Threshold", "阈值", typeof(int), 128, "检测阈值"),
                    new ParameterDefinition("Method", "方法", typeof(string), "Canny", "边缘检测方法")
                },
                "circlefind" => new List<ParameterDefinition>
                {
                    new ParameterDefinition("MinRadius", "最小半径", typeof(int), 10, "圆的最小半径"),
                    new ParameterDefinition("MaxRadius", "最大半径", typeof(int), 100, "圆的最大半径"),
                    new ParameterDefinition("MinDistance", "最小间距", typeof(int), 20, "圆心之间的最小距离")
                },
                "output" => new List<ParameterDefinition>
                {
                    new ParameterDefinition("SavePath", "保存路径", typeof(string), "", "输出文件路径"),
                    new ParameterDefinition("Format", "格式", typeof(string), "PNG", "输出图像格式")
                },
                _ => new List<ParameterDefinition>()
            };
        }

        private void ExecuteUpdateProperty(PropertyItem? property)
        {
            if (property == null || SelectedNode == null)
                return;

            // 根据属性名称更新节点
            switch (property.Name)
            {
                case "名称":
                    SelectedNode.Name = property.Value;
                    break;
                case "启用":
                    SelectedNode.IsEnabled = bool.TryParse(property.Value, out var enabled) && enabled;
                    break;
                default:
                    // 更新参数
                    if (SelectedNode.Parameters != null)
                    {
                        SelectedNode.Parameters[property.Name] = property.Value;
                    }
                    break;
            }
        }

        private void ExecuteResetProperty(PropertyItem? property)
        {
            if (property == null)
                return;
            // TODO: 实现属性重置逻辑
        }

        private bool CanApplyAllBindings()
        {
            return ParameterBindings.All(b => b.IsValid);
        }

        private void ExecuteApplyAllBindings()
        {
            ApplyBindingsToNode();
        }

        private bool CanResetAllBindings()
        {
            return ParameterBindings.Count > 0;
        }

        private void ExecuteResetAllBindings()
        {
            foreach (var binding in ParameterBindings)
            {
                if (binding.ResetToDefaultCommand.CanExecute(null))
                {
                    binding.ResetToDefaultCommand.Execute(null);
                }
            }
        }

        private void OnBindingChanged(object? sender, ParameterBinding e)
        {
            // 应用单个绑定变更
            if (SelectedNode != null)
            {
                SelectedNode.ParameterBindings.SetBinding(e);
            }
        }

        private void OnDataSourceSelectionRequested(object? sender, DataSourceSelectionRequestEventArgs e)
        {
            // 显示数据源选择对话框
            ShowDataSourcePicker(e);
        }

        private void ShowDataSourcePicker(DataSourceSelectionRequestEventArgs args)
        {
            // 创建数据源选择ViewModel
            var pickerViewModel = new DataSourcePickerViewModel(
                args.ParameterName,
                args.ParameterType,
                _dataSourceQueryService);

            // 设置当前节点ID
            pickerViewModel.LoadDataSources(_currentNodeId);

            // 订阅选择确认事件
            pickerViewModel.SelectionConfirmed += (s, e) =>
            {
                args.SelectedDataSource = e.SelectedDataSource;
            };

            // TODO: 显示选择窗口
            // 这里需要根据实际的窗口管理方式来显示选择器
            // 例如：
            // var window = new DataSourcePickerWindow(pickerViewModel);
            // window.ShowDialog();
        }

        #endregion
    }

    /// <summary>
    /// 参数定义
    /// </summary>
    public class ParameterDefinition
    {
        /// <summary>
        /// 参数名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 显示名称
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// 参数类型
        /// </summary>
        public Type Type { get; set; }

        /// <summary>
        /// 默认值
        /// </summary>
        public object? DefaultValue { get; set; }

        /// <summary>
        /// 描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 是否必需
        /// </summary>
        public bool IsRequired { get; set; }

        /// <summary>
        /// 最小值（用于数值类型）
        /// </summary>
        public object? MinValue { get; set; }

        /// <summary>
        /// 最大值（用于数值类型）
        /// </summary>
        public object? MaxValue { get; set; }

        /// <summary>
        /// 可选值列表（用于枚举类型）
        /// </summary>
        public List<string>? Options { get; set; }

        public ParameterDefinition(string name, string displayName, Type type, object? defaultValue = null, string? description = null)
        {
            Name = name;
            DisplayName = displayName;
            Type = type;
            DefaultValue = defaultValue;
            Description = description;
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
