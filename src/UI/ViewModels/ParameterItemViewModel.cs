using System;
using System.Collections.ObjectModel;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.SDK.Models;

namespace SunEyeVision.UI.ViewModels
{
    /// <summary>
    /// 参数项ViewModel - 支持数据绑定配置
    /// </summary>
    public class ParameterItemViewModel : ObservableObject
    {
        private readonly string _name;
        private readonly string _displayName;
        private readonly Type _type;
        private readonly string? _description;
        private ParameterBinding _binding;
        private bool _isBindingPopupOpen;

        #region 属性

        /// <summary>
        /// 参数名称
        /// </summary>
        public string Name => _name;

        /// <summary>
        /// 显示名称
        /// </summary>
        public string DisplayName => _displayName;

        /// <summary>
        /// 参数描述
        /// </summary>
        public string? Description => _description;

        /// <summary>
        /// 参数类型
        /// </summary>
        public Type ParameterType => _type;

        /// <summary>
        /// 是否支持数据绑定
        /// </summary>
        public bool SupportsBinding => true;

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
        public bool IsDynamicBinding => BindingType == BindingType.Binding;

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
                    BindingType.Binding => $"← {SourceNodeId}.{SourceProperty}",
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
        public double? MinValue => null;

        /// <summary>
        /// 数值最大值
        /// </summary>
        public double? MaxValue => null;

        /// <summary>
        /// 是否只读
        /// </summary>
        public bool IsReadOnly => false;

        /// <summary>
        /// 是否为数值类型
        /// </summary>
        public bool IsNumericType =>
            _type == typeof(int) || _type == typeof(long) ||
            _type == typeof(double) || _type == typeof(float) || _type == typeof(decimal);

        /// <summary>
        /// 是否为布尔类型
        /// </summary>
        public bool IsBoolType => _type == typeof(bool);

        /// <summary>
        /// 是否为枚举类型
        /// </summary>
        public bool IsEnumType => _type.IsEnum;

        /// <summary>
        /// 是否为字符串类型
        /// </summary>
        public bool IsStringType => _type == typeof(string);

        #endregion

        #endregion

        #region 构造函数

        public ParameterItemViewModel(string name, string displayName, Type type, object? value, string? description = null)
        {
            _name = name ?? throw new ArgumentNullException(nameof(name));
            _displayName = displayName ?? name;
            _type = type ?? throw new ArgumentNullException(nameof(type));
            _description = description;
            _binding = ParameterBinding.CreateConstant(name, value);
        }

        public ParameterItemViewModel(string name, string displayName, Type type, ParameterBinding binding, string? description = null)
        {
            _name = name ?? throw new ArgumentNullException(nameof(name));
            _displayName = displayName ?? name;
            _type = type ?? throw new ArgumentNullException(nameof(type));
            _description = description;
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
            Binding = ParameterBinding.CreateBinding(Name, sourceNodeId, sourceProperty, transformExpression);
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
