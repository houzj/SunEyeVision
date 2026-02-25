using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.SDK.Execution.Results;

namespace SunEyeVision.UI.ViewModels
{
    /// <summary>
    /// 参数绑定ViewModel
    /// </summary>
    /// <remarks>
    /// 管理单个参数的绑定配置，支持常量值和动态绑定两种模式。
    /// 
    /// 核心功能：
    /// 1. 切换绑定类型（常量/动态）
    /// 2. 管理常量值
    /// 3. 选择数据源
    /// 4. 配置转换表达式
    /// 5. 验证绑定配置
    /// </remarks>
    public class ParameterBindingViewModel : ViewModelBase
    {
        #region 字段

        private readonly IDataSourceQueryService? _dataSourceQueryService;
        private ParameterBinding _binding;
        private BindingType _selectedBindingType;
        private object? _constantValue;
        private AvailableDataSource? _selectedDataSource;
        private string? _transformExpression;
        private bool _isValid;
        private string _validationMessage = string.Empty;

        #endregion

        #region 属性

        /// <summary>
        /// 参数名称
        /// </summary>
        public string ParameterName => _binding.ParameterName;

        /// <summary>
        /// 参数显示名称
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// 参数类型
        /// </summary>
        public Type ParameterType { get; }

        /// <summary>
        /// 参数类型名称
        /// </summary>
        public string TypeName => ParameterType.Name;

        /// <summary>
        /// 参数描述
        /// </summary>
        public string? Description { get; }

        /// <summary>
        /// 参数默认值
        /// </summary>
        public object? DefaultValue { get; }

        /// <summary>
        /// 绑定类型选项
        /// </summary>
        public ObservableCollection<BindingTypeOption> BindingTypeOptions { get; }

        /// <summary>
        /// 选中的绑定类型
        /// </summary>
        public BindingType SelectedBindingType
        {
            get => _selectedBindingType;
            set
            {
                if (SetProperty(ref _selectedBindingType, value))
                {
                    _binding.BindingType = value;
                    UpdateBindingMode();
                    Validate();
                }
            }
        }

        /// <summary>
        /// 是否为常量绑定模式
        /// </summary>
        public bool IsConstantMode => SelectedBindingType == BindingType.Constant;

        /// <summary>
        /// 是否为动态绑定模式
        /// </summary>
        public bool IsDynamicMode => SelectedBindingType == BindingType.DynamicBinding;

        /// <summary>
        /// 常量值
        /// </summary>
        public object? ConstantValue
        {
            get => _constantValue;
            set
            {
                if (SetProperty(ref _constantValue, value))
                {
                    _binding.ConstantValue = value;
                    Validate();
                }
            }
        }

        /// <summary>
        /// 常量值字符串表示
        /// </summary>
        public string? ConstantValueString
        {
            get => ConstantValue?.ToString();
            set
            {
                if (TryParseValue(value, out var parsedValue))
                {
                    ConstantValue = parsedValue;
                }
            }
        }

        /// <summary>
        /// 可用数据源列表
        /// </summary>
        public ObservableCollection<AvailableDataSource> AvailableDataSources { get; }

        /// <summary>
        /// 选中的数据源
        /// </summary>
        public AvailableDataSource? SelectedDataSource
        {
            get => _selectedDataSource;
            set
            {
                if (SetProperty(ref _selectedDataSource, value))
                {
                    if (value != null)
                    {
                        _binding.SourceNodeId = value.SourceNodeId;
                        _binding.SourceProperty = value.PropertyName;
                    }
                    else
                    {
                        _binding.SourceNodeId = null;
                        _binding.SourceProperty = null;
                    }
                    OnPropertyChanged(nameof(SelectedDataSourceDisplay));
                    Validate();
                }
            }
        }

        /// <summary>
        /// 选中数据源的显示文本
        /// </summary>
        public string SelectedDataSourceDisplay => SelectedDataSource?.GetDisplayText() ?? "选择数据源...";

        /// <summary>
        /// 转换表达式
        /// </summary>
        public string? TransformExpression
        {
            get => _transformExpression;
            set
            {
                if (SetProperty(ref _transformExpression, value))
                {
                    _binding.TransformExpression = value;
                    Validate();
                }
            }
        }

        /// <summary>
        /// 是否有效
        /// </summary>
        public bool IsValid
        {
            get => _isValid;
            private set => SetProperty(ref _isValid, value);
        }

        /// <summary>
        /// 验证消息
        /// </summary>
        public string ValidationMessage
        {
            get => _validationMessage;
            private set => SetProperty(ref _validationMessage, value);
        }

        /// <summary>
        /// 当前绑定配置
        /// </summary>
        public ParameterBinding Binding => _binding;

        #endregion

        #region 命令

        /// <summary>
        /// 选择数据源命令
        /// </summary>
        public ICommand SelectDataSourceCommand { get; }

        /// <summary>
        /// 清除数据源命令
        /// </summary>
        public ICommand ClearDataSourceCommand { get; }

        /// <summary>
        /// 重置为默认值命令
        /// </summary>
        public ICommand ResetToDefaultCommand { get; }

        /// <summary>
        /// 应用绑定命令
        /// </summary>
        public ICommand ApplyBindingCommand { get; }

        #endregion

        #region 事件

        /// <summary>
        /// 绑定变更事件
        /// </summary>
        public event EventHandler<ParameterBinding>? BindingChanged;

        /// <summary>
        /// 数据源选择请求事件
        /// </summary>
        public event EventHandler<DataSourceSelectionRequestEventArgs>? DataSourceSelectionRequested;

        #endregion

        #region 构造函数

        /// <summary>
        /// 创建参数绑定ViewModel
        /// </summary>
        public ParameterBindingViewModel(
            string parameterName,
            string displayName,
            Type parameterType,
            object? defaultValue = null,
            string? description = null,
            IDataSourceQueryService? dataSourceQueryService = null)
        {
            _binding = new ParameterBinding
            {
                ParameterName = parameterName,
                BindingType = BindingType.Constant,
                TargetType = parameterType
            };

            DisplayName = displayName;
            ParameterType = parameterType;
            DefaultValue = defaultValue;
            Description = description;
            _dataSourceQueryService = dataSourceQueryService;

            // 初始化绑定类型选项
            BindingTypeOptions = new ObservableCollection<BindingTypeOption>
            {
                new BindingTypeOption(BindingType.Constant, "常量值", "使用固定的常量值"),
                new BindingTypeOption(BindingType.DynamicBinding, "动态绑定", "从父节点输出获取值")
            };

            // 初始化数据源列表
            AvailableDataSources = new ObservableCollection<AvailableDataSource>();

            // 初始化命令
            SelectDataSourceCommand = new RelayCommand(ExecuteSelectDataSource);
            ClearDataSourceCommand = new RelayCommand(ExecuteClearDataSource, CanClearDataSource);
            ResetToDefaultCommand = new RelayCommand(ExecuteResetToDefault, CanResetToDefault);
            ApplyBindingCommand = new RelayCommand(ExecuteApplyBinding, CanApplyBinding);

            // 设置默认值
            if (defaultValue != null)
            {
                ConstantValue = defaultValue;
            }

            _selectedBindingType = BindingType.Constant;
        }

        /// <summary>
        /// 从现有绑定创建ViewModel
        /// </summary>
        public static ParameterBindingViewModel FromBinding(
            ParameterBinding binding,
            string displayName,
            Type parameterType,
            object? defaultValue = null,
            string? description = null,
            IDataSourceQueryService? dataSourceQueryService = null)
        {
            var viewModel = new ParameterBindingViewModel(
                binding.ParameterName,
                displayName,
                parameterType,
                defaultValue,
                description,
                dataSourceQueryService);

            // 恢复绑定配置
            viewModel._binding = binding.Clone();
            viewModel._selectedBindingType = binding.BindingType;
            viewModel._constantValue = binding.ConstantValue;
            viewModel._transformExpression = binding.TransformExpression;

            // 如果是动态绑定，尝试恢复数据源选择
            if (binding.BindingType == BindingType.DynamicBinding &&
                !string.IsNullOrEmpty(binding.SourceNodeId) &&
                dataSourceQueryService != null)
            {
                var dataSources = dataSourceQueryService.GetAvailableDataSources(binding.SourceNodeId, parameterType);
                viewModel._selectedDataSource = dataSources.FirstOrDefault(ds => ds.PropertyName == binding.SourceProperty);
            }

            viewModel.Validate();
            return viewModel;
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 刷新可用数据源
        /// </summary>
        /// <param name="nodeId">当前节点ID</param>
        public void RefreshAvailableDataSources(string nodeId)
        {
            AvailableDataSources.Clear();

            if (_dataSourceQueryService == null)
                return;

            var dataSources = _dataSourceQueryService.GetAvailableDataSources(nodeId, ParameterType);
            foreach (var dataSource in dataSources)
            {
                AvailableDataSources.Add(dataSource);
            }

            // 如果当前选中的数据源不在列表中，清除选择
            if (SelectedDataSource != null &&
                !AvailableDataSources.Any(ds => ds.GetBindingPath() == SelectedDataSource.GetBindingPath()))
            {
                SelectedDataSource = null;
            }
        }

        /// <summary>
        /// 获取当前绑定配置
        /// </summary>
        public ParameterBinding GetBinding()
        {
            return _binding.Clone();
        }

        /// <summary>
        /// 应用绑定配置
        /// </summary>
        public void ApplyBinding()
        {
            BindingChanged?.Invoke(this, _binding.Clone());
        }

        #endregion

        #region 私有方法

        private void UpdateBindingMode()
        {
            OnPropertyChanged(nameof(IsConstantMode));
            OnPropertyChanged(nameof(IsDynamicMode));
        }

        private void ExecuteSelectDataSource()
        {
            // 触发数据源选择请求事件
            var args = new DataSourceSelectionRequestEventArgs(ParameterName, ParameterType);
            DataSourceSelectionRequested?.Invoke(this, args);

            if (args.SelectedDataSource != null)
            {
                SelectedDataSource = args.SelectedDataSource;
            }
        }

        private bool CanClearDataSource()
        {
            return SelectedDataSource != null;
        }

        private void ExecuteClearDataSource()
        {
            SelectedDataSource = null;
        }

        private bool CanResetToDefault()
        {
            return DefaultValue != null;
        }

        private void ExecuteResetToDefault()
        {
            SelectedBindingType = BindingType.Constant;
            ConstantValue = DefaultValue;
            TransformExpression = null;
            SelectedDataSource = null;
        }

        private bool CanApplyBinding()
        {
            return IsValid;
        }

        private void ExecuteApplyBinding()
        {
            ApplyBinding();
        }

        private void Validate()
        {
            var result = _binding.Validate();
            IsValid = result.IsValid;

            if (result.IsValid)
            {
                ValidationMessage = string.Empty;
            }
            else
            {
                ValidationMessage = string.Join("\n", result.Errors);
            }
        }

        private bool TryParseValue(string? valueString, out object? parsedValue)
        {
            parsedValue = null;

            if (string.IsNullOrWhiteSpace(valueString))
            {
                return true; // 允许空值
            }

            try
            {
                if (ParameterType == typeof(string))
                {
                    parsedValue = valueString;
                }
                else if (ParameterType == typeof(int))
                {
                    parsedValue = int.Parse(valueString);
                }
                else if (ParameterType == typeof(double))
                {
                    parsedValue = double.Parse(valueString);
                }
                else if (ParameterType == typeof(float))
                {
                    parsedValue = float.Parse(valueString);
                }
                else if (ParameterType == typeof(bool))
                {
                    parsedValue = bool.Parse(valueString);
                }
                else if (ParameterType == typeof(long))
                {
                    parsedValue = long.Parse(valueString);
                }
                else
                {
                    parsedValue = Convert.ChangeType(valueString, ParameterType);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion
    }

    /// <summary>
    /// 绑定类型选项
    /// </summary>
    public class BindingTypeOption
    {
        public BindingType Type { get; }
        public string DisplayName { get; }
        public string Description { get; }

        public BindingTypeOption(BindingType type, string displayName, string description)
        {
            Type = type;
            DisplayName = displayName;
            Description = description;
        }
    }

    /// <summary>
    /// 数据源选择请求事件参数
    /// </summary>
    public class DataSourceSelectionRequestEventArgs : EventArgs
    {
        public string ParameterName { get; }
        public Type ParameterType { get; }
        public AvailableDataSource? SelectedDataSource { get; set; }

        public DataSourceSelectionRequestEventArgs(string parameterName, Type parameterType)
        {
            ParameterName = parameterName;
            ParameterType = parameterType;
        }
    }
}
