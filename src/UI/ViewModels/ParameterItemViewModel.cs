using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.SDK.Metadata;

namespace SunEyeVision.UI.ViewModels
{
    /// <summary>
    /// 参数项ViewModel - 支持数据绑定配置
    /// </summary>
    public class ParameterItemViewModel : INotifyPropertyChanged
    {
        private readonly ParameterMetadata _metadata;
        private ParameterBinding _binding;
        private bool _isBindingPopupOpen;

        #region 属性

        /// <summary>
        /// 参数名称
        /// </summary>
        public string Name => _metadata.Name;

        /// <summary>
        /// 显示名称
        /// </summary>
        public string DisplayName => _metadata.DisplayName;

        /// <summary>
        /// 参数描述
        /// </summary>
        public string Description => _metadata.Description;

        /// <summary>
        /// 参数分类
        /// </summary>
        public string Category => _metadata.Category;

        /// <summary>
        /// 参数类型
        /// </summary>
        public ParamDataType ParameterType => _metadata.Type;

        /// <summary>
        /// 是否支持数据绑定
        /// </summary>
        public bool SupportsBinding => _metadata.SupportsBinding;

        /// <summary>
        /// 当前绑定配置
        /// </summary>
        public ParameterBinding Binding
        {
            get => _binding;
            set
            {
                if (_binding != value)
                {
                    _binding = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(BindingType));
                    OnPropertyChanged(nameof(ConstantValue));
                    OnPropertyChanged(nameof(SourceNodeId));
                    OnPropertyChanged(nameof(SourceProperty));
                    OnPropertyChanged(nameof(BindingDescription));
                    OnPropertyChanged(nameof(IsConstantBinding));
                    OnPropertyChanged(nameof(IsDynamicBinding));
                }
            }
        }

        /// <summary>
        /// 绑定类型
        /// </summary>
        public BindingType BindingType
        {
            get => _binding.BindingType;
            set
            {
                if (_binding.BindingType != value)
                {
                    _binding.BindingType = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsConstantBinding));
                    OnPropertyChanged(nameof(IsDynamicBinding));
                    OnPropertyChanged(nameof(BindingDescription));
                }
            }
        }

        /// <summary>
        /// 常量值
        /// </summary>
        public object? ConstantValue
        {
            get => _binding.ConstantValue;
            set
            {
                _binding.ConstantValue = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(BindingDescription));
            }
        }

        /// <summary>
        /// 源节点ID
        /// </summary>
        public string? SourceNodeId
        {
            get => _binding.SourceNodeId;
            set
            {
                _binding.SourceNodeId = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(BindingDescription));
            }
        }

        /// <summary>
        /// 源属性
        /// </summary>
        public string? SourceProperty
        {
            get => _binding.SourceProperty;
            set
            {
                _binding.SourceProperty = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(BindingDescription));
            }
        }

        /// <summary>
        /// 是否为常量绑定
        /// </summary>
        public bool IsConstantBinding => BindingType == BindingType.Constant;

        /// <summary>
        /// 是否为动态绑定
        /// </summary>
        public bool IsDynamicBinding => BindingType == BindingType.DynamicBinding;

        /// <summary>
        /// 绑定描述文本
        /// </summary>
        public string BindingDescription
        {
            get
            {
                return BindingType switch
                {
                    BindingType.Constant => $"{ConstantValue?.ToString() ?? "null"}",
                    BindingType.DynamicBinding => $"← {SourceNodeId}.{SourceProperty}",
                    BindingType.Expression => "表达式",
                    _ => "未知"
                };
            }
        }

        /// <summary>
        /// 绑定配置弹窗是否打开
        /// </summary>
        public bool IsBindingPopupOpen
        {
            get => _isBindingPopupOpen;
            set
            {
                if (_isBindingPopupOpen != value)
                {
                    _isBindingPopupOpen = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 可选的源节点列表（用于动态绑定）
        /// </summary>
        public ObservableCollection<SourceNodeInfo> AvailableSourceNodes { get; } = new();

        /// <summary>
        /// 当前选中的源节点
        /// </summary>
        public SourceNodeInfo? SelectedSourceNode
        {
            get => null;
            set
            {
                if (value != null)
                {
                    SourceNodeId = value.NodeId;
                    // 可用属性列表更新
                    AvailableSourceProperties.Clear();
                    foreach (var prop in value.AvailableProperties)
                    {
                        AvailableSourceProperties.Add(prop);
                    }
                }
            }
        }

        /// <summary>
        /// 可选的源属性列表
        /// </summary>
        public ObservableCollection<string> AvailableSourceProperties { get; } = new();

        #region 类型特定属性

        /// <summary>
        /// 数值最小值
        /// </summary>
        public double? MinValue => _metadata.MinValue as double?;

        /// <summary>
        /// 数值最大值
        /// </summary>
        public double? MaxValue => _metadata.MaxValue as double?;

        /// <summary>
        /// 枚举选项
        /// </summary>
        public object[]? Options => _metadata.Options;

        /// <summary>
        /// 是否只读
        /// </summary>
        public bool IsReadOnly => _metadata.ReadOnly;

        /// <summary>
        /// 是否为数值类型
        /// </summary>
        public bool IsNumericType => ParameterType == ParamDataType.Int || ParameterType == ParamDataType.Double;

        /// <summary>
        /// 是否为布尔类型
        /// </summary>
        public bool IsBoolType => ParameterType == ParamDataType.Bool;

        /// <summary>
        /// 是否为枚举类型
        /// </summary>
        public bool IsEnumType => ParameterType == ParamDataType.Enum;

        /// <summary>
        /// 是否为字符串类型
        /// </summary>
        public bool IsStringType => ParameterType == ParamDataType.String;

        #endregion

        #endregion

        #region 构造函数

        public ParameterItemViewModel(ParameterMetadata metadata)
        {
            _metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
            _binding = ParameterBinding.CreateConstant(metadata.Name, metadata.DefaultValue);
        }

        public ParameterItemViewModel(ParameterMetadata metadata, ParameterBinding binding)
        {
            _metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
            _binding = binding ?? throw new ArgumentNullException(nameof(binding));
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 设置为常量绑定
        /// </summary>
        public void SetConstantBinding(object? value)
        {
            Binding = ParameterBinding.CreateConstant(Name, value);
        }

        /// <summary>
        /// 设置为动态绑定
        /// </summary>
        public void SetDynamicBinding(string sourceNodeId, string sourceProperty, string? transformExpression = null)
        {
            Binding = ParameterBinding.CreateDynamic(Name, sourceNodeId, sourceProperty, transformExpression);
        }

        /// <summary>
        /// 切换绑定配置弹窗
        /// </summary>
        public void ToggleBindingPopup()
        {
            IsBindingPopupOpen = !IsBindingPopupOpen;
        }

        /// <summary>
        /// 关闭绑定配置弹窗
        /// </summary>
        public void CloseBindingPopup()
        {
            IsBindingPopupOpen = false;
        }

        /// <summary>
        /// 添加可用源节点
        /// </summary>
        public void AddAvailableSourceNode(string nodeId, string nodeName, string[] availableProperties)
        {
            AvailableSourceNodes.Add(new SourceNodeInfo
            {
                NodeId = nodeId,
                NodeName = nodeName,
                AvailableProperties = availableProperties
            });
        }

        /// <summary>
        /// 清空可用源节点
        /// </summary>
        public void ClearAvailableSourceNodes()
        {
            AvailableSourceNodes.Clear();
            AvailableSourceProperties.Clear();
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }

    /// <summary>
    /// 源节点信息
    /// </summary>
    public class SourceNodeInfo
    {
        public string NodeId { get; set; } = string.Empty;
        public string NodeName { get; set; } = string.Empty;
        public string[] AvailableProperties { get; set; } = Array.Empty<string>();
        public override string ToString() => NodeName;
    }
}
