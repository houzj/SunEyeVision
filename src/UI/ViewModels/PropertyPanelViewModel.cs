using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Windows.Input;
using SunEyeVision.Plugin.Infrastructure.Managers.Tool;
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.SDK.Execution.Results;
using SunEyeVision.UI.Controls.ParameterBinding;
using SunEyeVision.UI.Events;
using SunEyeVision.UI.Models;
using SunEyeVision.UI.Services.Logging;

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
    public class PropertyPanelViewModel : ViewModelBase, IDisposable
    {
        #region 字段

        private WorkflowNode? _selectedNode;
        private ObservableCollection<PropertyItem> _properties;
        private ObservableCollection<ParameterBindingViewModel> _parameterBindings;
        private readonly IDataSourceQueryService? _dataSourceQueryService;
        private readonly IParameterChangeLogger? _parameterLogger;
        private string _currentNodeId = string.Empty;
        private bool _disposed;

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
        public PropertyPanelViewModel() : this(null, null)
        {
        }

        /// <summary>
        /// 创建属性面板视图模型（带数据源查询服务）
        /// </summary>
        public PropertyPanelViewModel(IDataSourceQueryService? dataSourceQueryService) : this(dataSourceQueryService, null)
        {
        }

        /// <summary>
        /// 创建属性面板视图模型（带数据源查询服务和参数变更日志记录器）
        /// </summary>
        public PropertyPanelViewModel(
            IDataSourceQueryService? dataSourceQueryService,
            IParameterChangeLogger? parameterLogger)
        {
            _properties = new ObservableCollection<PropertyItem>();
            _parameterBindings = new ObservableCollection<ParameterBindingViewModel>();
            _dataSourceQueryService = dataSourceQueryService;
            _parameterLogger = parameterLogger;

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
            var changeCount = 0;

            foreach (var kvp in bindings)
            {
                var existingBinding = SelectedNode.ParameterBindings?.GetBinding(kvp.Key);
                if (existingBinding == null || !existingBinding.Equals(kvp.Value))
                {
                    SelectedNode.ParameterBindings.SetBinding(kvp.Value);
                    changeCount++;
                }
            }

            // 记录批量变更日志
            if (changeCount > 0)
            {
                _parameterLogger?.LogBatchParameterChange(SelectedNode.Name, changeCount);
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
                        _dataSourceQueryService,
                        paramDef.Min,
                        paramDef.Max,
                        paramDef.Step,
                        paramDef.Unit);
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
                        _dataSourceQueryService,
                        paramDef.Min,
                        paramDef.Max,
                        paramDef.Step,
                        paramDef.Unit);
                }

                // 订阅绑定变更事件
                bindingVm.BindingChanged += OnBindingChanged;
                bindingVm.DataSourceSelectionRequested += OnDataSourceSelectionRequested;
                bindingVm.ParameterChanged += OnParameterChanged;

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

            // 尝试从工具注册表获取参数定义
            try
            {
                var toolMetadata = ToolRegistry.GetToolMetadata(algorithmType);
                if (toolMetadata?.AlgorithmType != null)
                {
                    // 从 AlgorithmType 获取参数类型并反射获取范围信息
                    var toolType = toolMetadata.AlgorithmType;
                    var interfaces = toolType.GetInterfaces();
                    
                    foreach (var iface in interfaces)
                    {
                        if (iface.IsGenericType && iface.GetGenericTypeDefinition().Name.StartsWith("IToolPlugin"))
                        {
                            var genericArgs = iface.GetGenericArguments();
                            if (genericArgs.Length >= 1 && typeof(ToolParameters).IsAssignableFrom(genericArgs[0]))
                            {
                                var paramsType = genericArgs[0];
                                var defaultParams = Activator.CreateInstance(paramsType) as ToolParameters;
                                if (defaultParams != null)
                                {
                                    var runtimeMetadata = defaultParams.GetRuntimeParameterMetadata();
                                    foreach (var meta in runtimeMetadata)
                                    {
                                        definitions.Add(new ParameterDefinition(
                                            meta.Name,
                                            meta.DisplayName,
                                            meta.Type,
                                            meta.Value,
                                            meta.Description)
                                        {
                                            Min = meta.Min,
                                            Max = meta.Max,
                                            Step = meta.Step,
                                            Unit = meta.Unit
                                        });
                                    }
                                    return definitions;
                                }
                            }
                            break;
                        }
                    }
                }
            }
            catch (Exception)
            {
                // 如果获取失败，使用默认参数
            }

            // 回退到硬编码默认参数
            return GetDefaultParameterDefinitions(algorithmType);
        }

        private List<ParameterDefinition> GetDefaultParameterDefinitions(string algorithmType)
        {
            // 工具应通过 ToolMetadata.AlgorithmType 自描述参数
            // 如果执行到这里，说明工具未正确配置元数据
            System.Diagnostics.Debug.WriteLine(
                $"[警告] 工具 '{algorithmType}' 未正确配置参数元数据。" +
                $"请确保工具实现了 ITool<TParams, TResult> 接口。");

            return new List<ParameterDefinition>();
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

        /// <summary>
        /// 处理参数变更事件
        /// </summary>
        private void OnParameterChanged(object? sender, ParameterChangeEventArgs e)
        {
            if (_parameterLogger == null)
                return;

            // 补充节点信息
            var enrichedArgs = new ParameterChangeEventArgs
            {
                ParameterName = e.ParameterName,
                DisplayName = e.DisplayName,
                ChangeType = e.ChangeType,
                OldValue = e.OldValue,
                NewValue = e.NewValue,
                NodeName = SelectedNode?.Name,
                NodeId = SelectedNode?.Id,
                Timestamp = e.Timestamp,
                AdditionalInfo = e.AdditionalInfo
            };

            _parameterLogger.LogParameterChange(enrichedArgs);
        }

        /// <summary>
        /// 取消订阅所有参数事件
        /// </summary>
        private void UnsubscribeParameterEvents()
        {
            foreach (var binding in ParameterBindings)
            {
                binding.BindingChanged -= OnBindingChanged;
                binding.DataSourceSelectionRequested -= OnDataSourceSelectionRequested;
                binding.ParameterChanged -= OnParameterChanged;
            }
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                UnsubscribeParameterEvents();
                _disposed = true;
            }
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
        public double? Min { get; set; }

        /// <summary>
        /// 最大值（用于数值类型）
        /// </summary>
        public double? Max { get; set; }

        /// <summary>
        /// 步进值
        /// </summary>
        public double Step { get; set; } = 1.0;

        /// <summary>
        /// 单位
        /// </summary>
        public string? Unit { get; set; }

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
