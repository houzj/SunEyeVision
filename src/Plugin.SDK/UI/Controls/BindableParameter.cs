using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.SDK.Logging;
using SunEyeVision.Plugin.SDK.Metadata;
using System.Collections.Specialized;

namespace SunEyeVision.Plugin.SDK.UI.Controls
{
    /// <summary>
    /// 支持常量/绑定模式切换的多类型参数控件（核心控件，不包含标签和外框）
    /// </summary>
    /// <remarks>
    /// 核心概念：
    /// 1. 绑定类型（BindingType）：回答"值从哪里来" - Constant 或 Binding
    /// 2. 数据类型（DataType）：回答"值是什么类型" - Int, Double, String, Bool 等
    /// 3. 绑定类型与数据类型正交，任何数据类型都可以是常量或绑定
    /// 
    /// V2.0 重构要点：
    /// 1. 单一值属性 Value (object) - 统一入口，支持任意类型
    /// 2. 内部计算属性 InternalNumericValue (只读double) - 用于模板绑定，自动类型转换
    /// 3. 统一范围属性 Minimum/Maximum (object) - 支持任意数值类型
    /// 4. 类型适配器模式 - 自动处理 int ↔ double 转换
    /// 
    /// 注意：此控件只包含核心功能，标签和布局由工具层UI负责。
    /// </remarks>
    public class BindableParameter : Control
    {
        #region 依赖属性

        // ===== 参数名（用于参数绑定） =====
        public static readonly DependencyProperty ParameterNameProperty =
            DependencyProperty.Register(nameof(ParameterName), typeof(string), typeof(BindableParameter),
                new PropertyMetadata(string.Empty));

        // ===== 数据类型 =====
        public static readonly DependencyProperty DataTypeProperty =
            DependencyProperty.Register(nameof(DataType), typeof(ParamDataType), typeof(BindableParameter),
                new PropertyMetadata(ParamDataType.Double, OnDataTypeChanged));

        // ===== 绑定类型 =====
        public static readonly DependencyProperty BindingTypeProperty =
            DependencyProperty.Register(nameof(BindingType), typeof(BindingType), typeof(BindableParameter),
                new FrameworkPropertyMetadata(BindingType.Constant, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        // ===== 绑定源 =====
        public static readonly DependencyProperty BindingSourceProperty =
            DependencyProperty.Register(nameof(BindingSource), typeof(string), typeof(BindableParameter),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        // ===== 单一值属性（统一入口） =====
        /// <summary>
        /// 值属性：统一的值入口，支持任意类型
        /// </summary>
        /// <remarks>
        /// - DataType = Int: Value 存储 int 类型
        /// - DataType = Double: Value 存储 double 类型
        /// - DataType = String: Value 存储 string 类型
        /// - DataType = Bool: Value 存储 bool 类型
        /// </remarks>
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(nameof(Value), typeof(object), typeof(BindableParameter),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged));

        // ===== 内部数值值（只读，用于模板绑定） =====
        /// <summary>
        /// 内部数值值：只读属性，由 Value 自动计算
        /// </summary>
        /// <remarks>
        /// 用途：NumericUpDown 绑定此属性，自动进行类型转换
        /// - DataType = Int: Value (int) → InternalNumericValue (double)
        /// - DataType = Double: Value (double) → InternalNumericValue (double)
        /// </remarks>
        private static readonly DependencyPropertyKey InternalNumericValuePropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(InternalNumericValue), typeof(double), typeof(BindableParameter),
                new PropertyMetadata(0.0));

        public static readonly DependencyProperty InternalNumericValueProperty = InternalNumericValuePropertyKey.DependencyProperty;

        // ===== 统一的范围属性 =====
        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register(nameof(Minimum), typeof(object), typeof(BindableParameter),
                new PropertyMetadata(null, OnRangeChanged));

        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register(nameof(Maximum), typeof(object), typeof(BindableParameter),
                new PropertyMetadata(null, OnRangeChanged));

        // ===== 内部数值范围（只读，用于模板绑定） =====
        private static readonly DependencyPropertyKey InternalNumericMinimumPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(InternalNumericMinimum), typeof(double), typeof(BindableParameter),
                new PropertyMetadata(double.MinValue));

        public static readonly DependencyProperty InternalNumericMinimumProperty = InternalNumericMinimumPropertyKey.DependencyProperty;

        private static readonly DependencyPropertyKey InternalNumericMaximumPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(InternalNumericMaximum), typeof(double), typeof(BindableParameter),
                new PropertyMetadata(double.MaxValue));

        public static readonly DependencyProperty InternalNumericMaximumProperty = InternalNumericMaximumPropertyKey.DependencyProperty;

        // ===== 其他配置属性 =====
        public static readonly DependencyProperty SmallChangeProperty =
            DependencyProperty.Register(nameof(SmallChange), typeof(double), typeof(BindableParameter),
                new PropertyMetadata(1.0));

        public static readonly DependencyProperty LargeChangeProperty =
            DependencyProperty.Register(nameof(LargeChange), typeof(double), typeof(BindableParameter),
                new PropertyMetadata(10.0));

        public static readonly DependencyProperty DecimalPlacesProperty =
            DependencyProperty.Register(nameof(DecimalPlaces), typeof(int), typeof(BindableParameter),
                new PropertyMetadata(2));

        public static readonly DependencyProperty ShowSliderProperty =
            DependencyProperty.Register(nameof(ShowSlider), typeof(bool), typeof(BindableParameter),
                new PropertyMetadata(true, OnShowSliderPropertyChanged));

        // ===== 可用绑定源（树形结构） =====
        /// <summary>
        /// 可用数据源列表（输入：平面列表）
        /// </summary>
        public static readonly DependencyProperty AvailableDataSourcesProperty =
            DependencyProperty.Register(nameof(AvailableDataSources), typeof(System.Collections.ObjectModel.ObservableCollection<AvailableDataSource>), typeof(BindableParameter),
                new PropertyMetadata(null, OnAvailableDataSourcesChanged));

        /// <summary>
        /// 树形结构节点（输出：用于TreeView绑定）
        /// </summary>
        public static readonly DependencyProperty TreeNodesProperty =
            DependencyProperty.Register(nameof(TreeNodes), typeof(System.Collections.ObjectModel.ObservableCollection<TreeNodeData>), typeof(BindableParameter),
                new PropertyMetadata(null));

        public static readonly DependencyProperty AvailableBindingsProperty =
            DependencyProperty.Register(nameof(AvailableBindings), typeof(System.Collections.Generic.List<string>), typeof(BindableParameter),
                new PropertyMetadata(null));

        public static readonly DependencyProperty TransformExpressionProperty =
            DependencyProperty.Register(nameof(TransformExpression), typeof(string), typeof(BindableParameter),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        #endregion

        #region 属性封装

        /// <summary>
        /// 参数名（用于参数绑定，对应 ToolParameters 中的属性名）
        /// </summary>
        public string ParameterName
        {
            get => (string)GetValue(ParameterNameProperty);
            set => SetValue(ParameterNameProperty, value);
        }

        public ParamDataType DataType
        {
            get => (ParamDataType)GetValue(DataTypeProperty);
            set => SetValue(DataTypeProperty, value);
        }

        public BindingType BindingType
        {
            get => (BindingType)GetValue(BindingTypeProperty);
            set => SetValue(BindingTypeProperty, value);
        }

        public string BindingSource
        {
            get => (string)GetValue(BindingSourceProperty);
            set => SetValue(BindingSourceProperty, value);
        }

        /// <summary>
        /// 值属性：统一的值入口
        /// </summary>
        public object? Value
        {
            get => GetValue(ValueProperty);
            set
            {
                // 验证并修正值类型
                var correctedValue = ValidateAndCorrectValueType(value);
                SetValue(ValueProperty, correctedValue);
            }
        }

        /// <summary>
        /// 内部数值值：只读，由 Value 自动计算
        /// </summary>
        public double InternalNumericValue
        {
            get => (double)GetValue(InternalNumericValueProperty);
            private set => SetValue(InternalNumericValuePropertyKey, value);
        }

        /// <summary>
        /// 最小值：支持任意数值类型
        /// </summary>
        public object? Minimum
        {
            get => GetValue(MinimumProperty);
            set => SetValue(MinimumProperty, value);
        }

        /// <summary>
        /// 最大值：支持任意数值类型
        /// </summary>
        public object? Maximum
        {
            get => GetValue(MaximumProperty);
            set => SetValue(MaximumProperty, value);
        }

        /// <summary>
        /// 内部数值最小值：只读，由 Minimum 自动计算
        /// </summary>
        public double InternalNumericMinimum
        {
            get => (double)GetValue(InternalNumericMinimumProperty);
            private set => SetValue(InternalNumericMinimumPropertyKey, value);
        }

        /// <summary>
        /// 内部数值最大值：只读，由 Maximum 自动计算
        /// </summary>
        public double InternalNumericMaximum
        {
            get => (double)GetValue(InternalNumericMaximumProperty);
            private set => SetValue(InternalNumericMaximumPropertyKey, value);
        }

        public double SmallChange
        {
            get => (double)GetValue(SmallChangeProperty);
            set => SetValue(SmallChangeProperty, value);
        }

        public double LargeChange
        {
            get => (double)GetValue(LargeChangeProperty);
            set => SetValue(LargeChangeProperty, value);
        }

        public int DecimalPlaces
        {
            get => (int)GetValue(DecimalPlacesProperty);
            set => SetValue(DecimalPlacesProperty, value);
        }

        public bool ShowSlider
        {
            get => (bool)GetValue(ShowSliderProperty);
            set => SetValue(ShowSliderProperty, value);
        }

        /// <summary>
        /// 可用数据源列表（输入）
        /// </summary>
        public System.Collections.ObjectModel.ObservableCollection<AvailableDataSource>? AvailableDataSources
        {
            get => (System.Collections.ObjectModel.ObservableCollection<AvailableDataSource>?)GetValue(AvailableDataSourcesProperty);
            set => SetValue(AvailableDataSourcesProperty, value);
        }

        /// <summary>
        /// 树形结构节点（输出）
        /// </summary>
        public System.Collections.ObjectModel.ObservableCollection<TreeNodeData>? TreeNodes
        {
            get => (System.Collections.ObjectModel.ObservableCollection<TreeNodeData>?)GetValue(TreeNodesProperty);
            set => SetValue(TreeNodesProperty, value);
        }

        public System.Collections.Generic.List<string> AvailableBindings
        {
            get => (System.Collections.Generic.List<string>)GetValue(AvailableBindingsProperty);
            set => SetValue(AvailableBindingsProperty, value);
        }

        public string TransformExpression
        {
            get => (string)GetValue(TransformExpressionProperty);
            set => SetValue(TransformExpressionProperty, value);
        }

        #endregion

        #region 控件引用

        private NumericUpDown _numericEditor = null!;
        private TextBox _stringTextBox = null!;
        private Button _bindingButton = null!;
        private CheckBox _boolCheckBox = null!;
        private Popup _bindingPopup = null!;
        private TreeView _bindingTreeView = null!;

        #endregion

        #region 事件

        /// <summary>
        /// 值变更事件
        /// </summary>
        public static readonly RoutedEvent ValueChangedEvent =
            EventManager.RegisterRoutedEvent(nameof(ValueChanged), RoutingStrategy.Bubble,
                typeof(RoutedEventHandler), typeof(BindableParameter));

        public event RoutedEventHandler ValueChanged
        {
            add => AddHandler(ValueChangedEvent, value);
            remove => RemoveHandler(ValueChangedEvent, value);
        }

        /// <summary>
        /// 绑定类型变更事件
        /// </summary>
        public static readonly RoutedEvent BindingTypeChangedEvent =
            EventManager.RegisterRoutedEvent(nameof(BindingTypeChanged), RoutingStrategy.Bubble,
                typeof(RoutedEventHandler), typeof(BindableParameter));

        public event RoutedEventHandler BindingTypeChanged
        {
            add => AddHandler(BindingTypeChangedEvent, value);
            remove => RemoveHandler(BindingTypeChangedEvent, value);
        }

        /// <summary>
        /// 绑定源选择事件
        /// </summary>
        public static readonly RoutedEvent BindingSourceSelectedEvent =
            EventManager.RegisterRoutedEvent(nameof(BindingSourceSelected), RoutingStrategy.Bubble,
                typeof(RoutedEventHandler), typeof(BindableParameter));

        public event RoutedEventHandler BindingSourceSelected
        {
            add => AddHandler(BindingSourceSelectedEvent, value);
            remove => RemoveHandler(BindingSourceSelectedEvent, value);
        }

        #endregion

        // 私有字段：保存当前数据源集合的引用，用于取消订阅事件
        private System.Collections.ObjectModel.ObservableCollection<AvailableDataSource>? _currentDataSources;

        static BindableParameter()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(BindableParameter),
                new FrameworkPropertyMetadata(typeof(BindableParameter)));
        }

        public BindableParameter()
        {
            AvailableBindings = new System.Collections.Generic.List<string>();
            TreeNodes = new System.Collections.ObjectModel.ObservableCollection<TreeNodeData>();
        }

        #region 回调方法

        private static void OnAvailableDataSourcesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BindableParameter control)
            {
                // 调试日志：输出绑定状态
                VisionLogger.Instance.Log(LogLevel.Info,
                    $"[BindableParameter] OnAvailableDataSourcesChanged 触发: ParameterName={control.ParameterName}, " +
                    $"NewValue={(e.NewValue != null ? $"ObservableCollection[{((System.Collections.ObjectModel.ObservableCollection<AvailableDataSource>)e.NewValue).Count}]" : "null")}, " +
                    $"OldValue={(e.OldValue != null ? $"ObservableCollection[{((System.Collections.ObjectModel.ObservableCollection<AvailableDataSource>)e.OldValue).Count}]" : "null")}",
                    "BindableParameter");

                // 订阅新集合的事件并构建树形结构
                if (e.NewValue is System.Collections.ObjectModel.ObservableCollection<AvailableDataSource> newDataSources)
                {
                    control._currentDataSources = newDataSources;
                    // 注意：不再订阅 CollectionChanged 事件，避免批量添加数据源时的重复触发
                    // 数据源更新通过 AvailableDataSourcesProperty 变化触发一次 RebuildTreeNodes

                    // 立即构建树形结构
                    control.RebuildTreeNodes();
                }
                else
                {
                    control._currentDataSources = null;
                    control.TreeNodes = new System.Collections.ObjectModel.ObservableCollection<TreeNodeData>();
                    VisionLogger.Instance.Log(LogLevel.Warning,
                        $"[BindableParameter] AvailableDataSources 为 null 或类型不匹配",
                        "BindableParameter");
                }
            }
        }

        /// <summary>
        /// 重新构建树形结构
        /// </summary>
        private void RebuildTreeNodes()
        {
            if (_currentDataSources != null)
            {
                var dataSourceList = _currentDataSources.ToList();
                
                // 🔧 类型过滤：根据 DataType 过滤兼容的数据源
                // 与 ImageSourceSelector 保持一致的设计模式
                var filteredDataSources = FilterDataSourcesByType(dataSourceList);
                
                // 简化日志：只输出关键信息
                VisionLogger.Instance.Log(LogLevel.Info, 
                    $"构建树形结构: {dataSourceList.Count} 个数据源 → {filteredDataSources.Count} 个兼容数据源 (DataType={DataType})", 
                    "BindableParameter");
                
                var treeNodes = BuildTreeStructure(filteredDataSources);
                
                // 输出树形结构概览
                if (treeNodes.Count > 0)
                {
                    var summary = string.Join(", ", treeNodes.Select(n => $"{n.Text}({n.Children.Count})"));
                    VisionLogger.Instance.Log(LogLevel.Success, $"树形结构构建完成: {treeNodes.Count} 个节点根 [{summary}]", "BindableParameter");
                }
                
                TreeNodes = new System.Collections.ObjectModel.ObservableCollection<TreeNodeData>(treeNodes);
            }
            else
            {
                VisionLogger.Instance.Log(LogLevel.Info, $"_currentDataSources 为 null，创建空树", "BindableParameter");
                TreeNodes = new System.Collections.ObjectModel.ObservableCollection<TreeNodeData>();
            }
        }
        
        /// <summary>
        /// 根据参数类型过滤数据源
        /// </summary>
        private System.Collections.Generic.List<AvailableDataSource> FilterDataSourcesByType(
            System.Collections.Generic.List<AvailableDataSource> dataSources)
        {
            // 将 ParamDataType 映射到 OutputTypeCategory
            OutputTypeCategory? expectedCategory = DataType switch
            {
                ParamDataType.Int => OutputTypeCategory.Numeric,
                ParamDataType.Double => OutputTypeCategory.Numeric,
                ParamDataType.String => OutputTypeCategory.Text,
                ParamDataType.Bool => OutputTypeCategory.Numeric,
                _ => null
            };
            
            if (expectedCategory == null)
            {
                // 未指定类型：返回所有数据源
                return dataSources;
            }
            
            // 过滤匹配的数据源
            var filteredDataSources = new System.Collections.Generic.List<AvailableDataSource>();
            
            foreach (var dataSource in dataSources)
            {
                if (dataSource.PropertyType != null)
                {
                    // 使用 OutputTypeCategoryMapper 获取数据源的类型分类
                    var sourceCategory = OutputTypeCategoryMapper.GetCategory(dataSource.PropertyType);
                    
                    // 比较分类是否匹配
                    if (sourceCategory == expectedCategory)
                    {
                        filteredDataSources.Add(dataSource);
                    }
                }
            }
            
            return filteredDataSources;
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (BindableParameter)d;
            
            // 更新内部数值值（类型适配器）
            control.UpdateInternalNumericValue(e.NewValue);
            
            // 更新 UI 控件的显示
            control.UpdateUIFromValue(e.NewValue);
        }

        private static void OnDataTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BindableParameter control)
            {
                // 根据 DataType 自动设置 DecimalPlaces
                if (e.NewValue is ParamDataType dataType)
                {
                    if (dataType == ParamDataType.Int)
                    {
                        // 整数类型：强制设置为 0 位小数
                        control.DecimalPlaces = 0;
                    }
                    // 浮点类型：保持用户设置的 DecimalPlaces（默认为 2）
                }
                
                // 重新计算内部数值值
                control.UpdateInternalNumericValue(control.Value);
                control.UpdateInternalNumericRange();
                
                control.UpdateVisualState();
            }
        }

        private static void OnRangeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BindableParameter control)
            {
                control.UpdateInternalNumericRange();
            }
        }

        private static void OnShowSliderPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BindableParameter control)
            {
                // ShowSlider 属性变化处理（目前无需特殊操作）
            }
        }

        #endregion

        #region 类型适配器

        /// <summary>
        /// 验证并修正值的类型
        /// </summary>
        /// <param name="value">输入值</param>
        /// <returns>符合 DataType 的值</returns>
        private object? ValidateAndCorrectValueType(object? value)
        {
            if (value == null)
                return null;

            var expectedType = DataType switch
            {
                ParamDataType.Int => typeof(int),
                ParamDataType.Double => typeof(double),
                ParamDataType.String => typeof(string),
                ParamDataType.Bool => typeof(bool),
                _ => null
            };

            if (expectedType == null)
                return value;

            // 如果类型已匹配，直接返回
            if (expectedType.IsAssignableFrom(value.GetType()))
                return value;

            // 尝试转换
            try
            {
                object correctedValue = DataType switch
                {
                    ParamDataType.Int => Convert.ToInt32(value),
                    ParamDataType.Double => Convert.ToDouble(value),
                    ParamDataType.String => value.ToString() ?? string.Empty,
                    ParamDataType.Bool => Convert.ToBoolean(value),
                    _ => value
                };

                // 使用标准日志系统记录类型修正
                PluginLogger.Warning(
                    $"值类型自动修正: {value.GetType().Name} → {expectedType.Name}, 参数: {ParameterName}",
                    "BindableParameter");

                return correctedValue;
            }
            catch (Exception ex)
            {
                PluginLogger.Error(
                    $"值类型转换失败: {value.GetType().Name} → {expectedType.Name}, 参数: {ParameterName}, 错误: {ex.Message}",
                    "BindableParameter");

                // 返回默认值
                return DataType switch
                {
                    ParamDataType.Int => 0,
                    ParamDataType.Double => 0.0,
                    ParamDataType.String => string.Empty,
                    ParamDataType.Bool => false,
                    _ => value
                };
            }
        }

        /// <summary>
        /// 更新内部数值值（类型适配器：object → double）
        /// </summary>
        private void UpdateInternalNumericValue(object? value)
        {
            if (value == null)
            {
                InternalNumericValue = 0.0;
                return;
            }

            // 根据数据类型转换
            InternalNumericValue = DataType switch
            {
                ParamDataType.Int => Convert.ToDouble(value),
                ParamDataType.Double => Convert.ToDouble(value),
                _ => 0.0
            };
        }

        /// <summary>
        /// 从内部数值值设置值（类型适配器：double → object）
        /// </summary>
        private void SetFromInternalNumericValue(double numericValue)
        {
            // 修复：显式装箱确保正确的类型推断
            // C# switch 表达式的类型推断会选择"最佳公共类型"
            // 如果不加 (object) 强制转换，编译器会推断为 double 类型
            object? newValue = DataType switch
            {
                ParamDataType.Int => (object)(int)Math.Round(numericValue),  // 显式装箱为 object
                ParamDataType.Double => numericValue,
                _ => numericValue
            };
            
            Value = newValue;
        }

        /// <summary>
        /// 更新内部数值范围（类型适配器：object → double）
        /// </summary>
        private void UpdateInternalNumericRange()
        {
            // 更新最小值
            if (Minimum == null)
            {
                InternalNumericMinimum = DataType == ParamDataType.Int ? int.MinValue : double.MinValue;
            }
            else
            {
                InternalNumericMinimum = Convert.ToDouble(Minimum);
            }

            // 更新最大值
            if (Maximum == null)
            {
                InternalNumericMaximum = DataType == ParamDataType.Int ? int.MaxValue : double.MaxValue;
            }
            else
            {
                InternalNumericMaximum = Convert.ToDouble(Maximum);
            }
        }

        /// <summary>
        /// 从 Value 更新 UI 控件的显示
        /// </summary>
        private void UpdateUIFromValue(object? value)
        {
            // Numeric 控件通过绑定 InternalNumericValue 自动更新，无需手动处理
            
            // String 控件
            if (_stringTextBox != null && DataType == ParamDataType.String)
            {
                var text = value?.ToString() ?? string.Empty;
                if (_stringTextBox.Text != text)
                {
                    _stringTextBox.Text = text;
                }
            }
            
            // Bool 控件
            if (_boolCheckBox != null && DataType == ParamDataType.Bool)
            {
                var isChecked = value is bool b && b;
                if (_boolCheckBox.IsChecked != isChecked)
                {
                    _boolCheckBox.IsChecked = isChecked;
                }
            }
        }

        #endregion

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            // 获取模板中的控件
            _numericEditor = GetTemplateChild("PART_NumericEditor") as NumericUpDown;
            _stringTextBox = GetTemplateChild("PART_StringTextBox") as TextBox;
            _bindingButton = GetTemplateChild("PART_BindingButton") as Button ??
                throw new InvalidOperationException("PART_BindingButton not found");
            _boolCheckBox = GetTemplateChild("PART_BoolCheckBox") as CheckBox;
            _bindingPopup = GetTemplateChild("PART_BindingPopup") as Popup;
            _bindingTreeView = GetTemplateChild("PART_BindingTreeView") as TreeView;

            // 绑定事件
            if (_numericEditor != null)
            {
                _numericEditor.ValueChanged += (s, e) =>
                {
                    // 从 NumericUpDown 的 Value 属性更新到 BindableParameter 的 Value
                    SetFromInternalNumericValue(e.NewValue);
                    RaiseValueChanged();
                };
            }

            if (_stringTextBox != null)
            {
                _stringTextBox.TextChanged += (s, e) =>
                {
                    if (BindingType == BindingType.Constant)
                    {
                        Value = _stringTextBox.Text;
                        RaiseValueChanged();
                    }
                };
            }

            if (_boolCheckBox != null)
            {
                _boolCheckBox.Checked += (s, e) =>
                {
                    Value = true;
                    RaiseValueChanged();
                };
                _boolCheckBox.Unchecked += (s, e) =>
                {
                    Value = false;
                    RaiseValueChanged();
                };
            }

            _bindingButton.Click += OnBindingButtonClick;

            // 绑定 TreeView 选中事件
            if (_bindingTreeView != null)
            {
                _bindingTreeView.SelectedItemChanged += OnBindingTreeViewSelectedItemChanged;
            }

            // 初始化状态
            UpdateVisualState();

            // 🔧 修复 TabControl 虚拟化问题：
            // 当控件在非活动 TabItem 中创建时，OnAvailableDataSourcesChanged 可能不会被触发
            // 主动检查 AvailableDataSources 是否已经有值
            if (AvailableDataSources != null && AvailableDataSources.Count > 0)
            {
                VisionLogger.Instance.Log(LogLevel.Warning,
                    $"[BindableParameter] OnApplyTemplate: AvailableDataSources 已有 {AvailableDataSources.Count} 个数据源，主动触发 RebuildTreeNodes",
                    "BindableParameter");
                
                _currentDataSources = AvailableDataSources;
                RebuildTreeNodes();
            }
        }

        /// <summary>
        /// 从数据源列表构建树形结构
        /// </summary>
        /// <remarks>
        /// 按节点名称分组，每个节点作为根节点，该节点的属性作为子节点
        /// 示例：
        /// - 节点A
        ///   - 结果
        ///     - 输出图像
        ///     - 实际使用的阈值
        /// - 节点B
        ///   - 图像
        ///     - 宽度
        ///     - 高度
        /// </remarks>
        /// <param name="dataSources">数据源列表</param>
        /// <returns>树形结构的根节点列表</returns>
        public static System.Collections.Generic.List<TreeNodeData> BuildTreeStructure(System.Collections.Generic.List<AvailableDataSource> dataSources)
        {
            var rootNodes = new System.Collections.Generic.List<TreeNodeData>();
            var nodeGroups = new Dictionary<string, System.Collections.Generic.List<AvailableDataSource>>();

            // 按节点名称分组
            foreach (var dataSource in dataSources)
            {
                var groupName = dataSource.SourceNodeName;
                if (!nodeGroups.ContainsKey(groupName))
                {
                    nodeGroups[groupName] = new System.Collections.Generic.List<AvailableDataSource>();
                }
                nodeGroups[groupName].Add(dataSource);
            }

            // 为每个节点创建一个根节点
            foreach (var (nodeName, nodeDataSources) in nodeGroups)
            {
                var rootNode = TreeNodeData.CreateGroupNode(nodeName);
                
                // 处理该节点的所有数据源
                var propertyCache = new Dictionary<string, TreeNodeData>();
                
                foreach (var dataSource in nodeDataSources)
                {
                        // 📊 调试日志：输出 FullTreeName 最终值
                        VisionLogger.Instance.Log(LogLevel.Info,
                            $"  [BindableParameter] 数据源: {dataSource.DisplayName}, FullTreeName='{dataSource.FullTreeName ?? "null"}', PropertyName={dataSource.PropertyName}, SourceNodeName='{dataSource.SourceNodeName}'",
                            "BindableParameter");

                    if (string.IsNullOrEmpty(dataSource.FullTreeName))
                    {
                        // 没有 TreeName：直接添加为叶子节点
                        VisionLogger.Instance.Log(LogLevel.Warning,
                            $"    [BindableParameter] TreeName is empty, adding as leaf node: {dataSource.DisplayName}",
                            "BindableParameter");
                        var leafNode = TreeNodeData.CreateDataSourceNode(dataSource);
                        rootNode.Children.Add(leafNode);
                    }
                    else
                    {
                        // 有 TreeName：解析并创建多级树结构，添加到根节点
                        VisionLogger.Instance.Log(LogLevel.Success,
                            $"    [BindableParameter] TreeName exists, building tree: {dataSource.FullTreeName}",
                            "BindableParameter");
                        BuildOrMergeTreeNodeFromFullTreeName(dataSource.FullTreeName!, dataSource, rootNode.Children, propertyCache);
                    }
                }
                
                rootNodes.Add(rootNode);
            }

            return rootNodes;
        }

        /// <summary>
        /// 从完整树形名称构建或合并树节点（跳过根节点名称）
        /// </summary>
        /// <param name="fullTreeName">完整树形名称（例如: "阈值工具.结果.实际使用的阈值"）</param>
        /// <param name="dataSource">数据源</param>
        /// <param name="rootNodes">根节点列表</param>
        /// <param name="nodeCache">节点缓存字典（key: 路径，value: 节点）</param>
        private static void BuildOrMergeTreeNodeFromFullTreeName(string fullTreeName, AvailableDataSource dataSource,
            System.Collections.ObjectModel.ObservableCollection<TreeNodeData> rootNodes, Dictionary<string, TreeNodeData> nodeCache)
        {
            // 将 TreeName 按 `.` 分割
            var parts = fullTreeName.Split('.');

            TreeNodeData? parentNode = null;
            string currentPath = string.Empty;

            // 处理每个层级（跳过根节点名称，从索引 1 开始）
            // 根节点名称（parts[0]）已经在 BuildTreeStructure 中创建
            for (int i = 1; i < parts.Length; i++)
            {
                // 构建当前路径
                if (string.IsNullOrEmpty(currentPath))
                {
                    currentPath = parts[i];
                }
                else
                {
                    currentPath = currentPath + "." + parts[i];
                }

                TreeNodeData? node;

                if (i == parts.Length - 1)
                {
                    // 最后一个部分是叶子节点
                    node = TreeNodeData.CreateDataSourceNode(dataSource);
                    node.Text = parts[i];
                    
                    // 修复：将叶子节点添加到父节点的 Children 中
                    if (parentNode != null)
                    {
                        parentNode.Children.Add(node);
                        VisionLogger.Instance.Log(LogLevel.Success, 
                            $"      [BindableParameter] 叶子节点已添加: {node.Text} -> {parentNode.Text}", 
                            "BindableParameter");
                    }
                    else
                    {
                        // 特殊情况：叶子节点直接作为根节点（FullTreeName 只有一层）
                        rootNodes.Add(node);
                        VisionLogger.Instance.Log(LogLevel.Warning, 
                            $"      [BindableParameter] 叶子节点作为根节点: {node.Text}", 
                            "BindableParameter");
                    }
                }
                else
                {
                    // 中间部分是分组节点
                    if (nodeCache.TryGetValue(currentPath, out node))
                    {
                        // 已存在相同的父节点，直接复用
                        parentNode = node;
                        continue;
                    }

                    // 创建新的分组节点
                    node = TreeNodeData.CreateGroupNode(parts[i]);
                    nodeCache[currentPath] = node;

                    // 添加到父节点或根节点列表
                    if (parentNode != null)
                    {
                        parentNode.Children.Add(node);
                    }
                    else
                    {
                        rootNodes.Add(node);
                    }
                }

                parentNode = node;
            }
        }

        private void RaiseValueChanged()
        {
            RaiseEvent(new RoutedEventArgs(ValueChangedEvent));
        }

        private void OnBindingButtonClick(object sender, RoutedEventArgs e)
        {
            if (BindingType == BindingType.Constant)
            {
                ShowBindingSelector();
            }
            else
            {
                BindingType = BindingType.Constant;
                BindingSource = string.Empty;
                UpdateVisualState();
            }

            RaiseEvent(new RoutedEventArgs(BindingTypeChangedEvent));
        }

        private void ShowBindingSelector()
        {
            if (_bindingPopup != null)
            {
                _bindingPopup.IsOpen = !_bindingPopup.IsOpen;
            }
        }

        private void OnBindingTreeViewSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is TreeNodeData selectedNode && selectedNode.IsSelectable && selectedNode.DataSource != null)
            {
                // 设置绑定源
                BindingSource = selectedNode.DataSource.GetBindingPath();
                BindingType = BindingType.Binding;
                UpdateVisualState();

                // 触发事件
                RaiseEvent(new RoutedEventArgs(BindingSourceSelectedEvent));

                // 关闭 Popup
                if (_bindingPopup != null)
                {
                    _bindingPopup.IsOpen = false;
                }
            }
        }

        private void UpdateVisualState()
        {
            if (_bindingButton == null)
                return;

            if (BindingType == BindingType.Binding)
            {
                // 绑定模式：禁用值编辑控件
                if (_numericEditor != null)
                {
                    _numericEditor.IsEnabled = false;
                }
                if (_stringTextBox != null)
                {
                    _stringTextBox.IsEnabled = false;
                    _stringTextBox.Text = BindingSource;
                }
                if (_boolCheckBox != null) _boolCheckBox.IsEnabled = false;

                _bindingButton.Content = "🔗";
                _bindingButton.ToolTip = $"绑定源: {BindingSource}\n点击解除绑定";
            }
            else
            {
                // 常量模式：启用值编辑控件
                if (_numericEditor != null)
                {
                    _numericEditor.IsEnabled = true;
                }
                if (_stringTextBox != null)
                {
                    _stringTextBox.IsEnabled = true;
                }
                if (_boolCheckBox != null) _boolCheckBox.IsEnabled = true;

                _bindingButton.Content = "⚡";
                _bindingButton.ToolTip = "点击绑定到其他节点输出";
            }
        }

        /// <summary>
        /// 获取当前值（根据数据类型）
        /// </summary>
        public object? GetValue()
        {
            return Value;
        }

        /// <summary>
        /// 设置当前值（根据数据类型）
        /// </summary>
        public void SetValue(object? value)
        {
            Value = value;
        }

        /// <summary>
        /// 转换为 ParameterBinding 对象
        /// </summary>
        public ParameterBinding ToParameterBinding()
        {
            if (BindingType == BindingType.Constant)
            {
                return ParameterBinding.CreateConstant(ParameterName, GetValue());
            }
            else
            {
                var parts = BindingSource.Split('.');
                var nodeId = parts.Length > 0 ? parts[0] : string.Empty;
                var property = parts.Length > 1 ? parts[1] : BindingSource;
                return ParameterBinding.CreateBinding(ParameterName, nodeId, property, TransformExpression);
            }
        }

        /// <summary>
        /// 从 ParameterBinding 对象加载
        /// </summary>
        public void FromParameterBinding(ParameterBinding binding)
        {
            if (binding == null)
                return;

            ParameterName = binding.ParameterName;
            BindingType = binding.BindingType;
            TransformExpression = binding.TransformExpression ?? string.Empty;

            if (binding.BindingType == BindingType.Constant)
            {
                SetValue(binding.ConstantValue);
            }
            else
            {
                BindingSource = string.IsNullOrEmpty(binding.SourceProperty)
                    ? binding.SourceNodeId ?? string.Empty
                    : $"{binding.SourceNodeId}.{binding.SourceProperty}";
            }
        }
    }
}
