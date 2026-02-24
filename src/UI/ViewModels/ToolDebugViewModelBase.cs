using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using SunEyeVision.Plugin.SDK.Execution.Parameters;

namespace SunEyeVision.UI.ViewModels
{
    /// <summary>
    /// 工具调试ViewModel基类
    /// </summary>
    /// <remarks>
    /// 为工具自定义调试界面提供参数绑定支持。
    /// 
    /// 核心功能：
    /// 1. 管理参数绑定容器
    /// 2. 提供数据源查询服务接口
    /// 3. 简化绑定设置操作
    /// 4. 支持属性变更通知
    /// 
    /// 使用示例：
    /// <code>
    /// public class CircleFindDebugViewModel : ToolDebugViewModelBase
    /// {
    ///     private double _minRadius = 5.0;
    ///     
    ///     [ParameterBindingProperty("MinRadius")]
    ///     [ParameterRange(1, 1000)]
    ///     public double MinRadius
    ///     {
    ///         get => _minRadius;
    ///         set
    ///         {
    ///             if (SetProperty(ref _minRadius, value))
    ///             {
    ///                 SetParameterBinding("MinRadius", value);
    ///             }
    ///         }
    ///     }
    ///     
    ///     public void BindToParentRadius(string sourceNodeId)
    ///     {
    ///         SetDynamicBinding("MinRadius", sourceNodeId, "Radius", "value * 0.9");
    ///     }
    ///     
    ///     public List&lt;AvailableDataSource&gt; GetAvailableRadiusSources()
    ///     {
    ///         return GetAvailableDataSources&lt;double&gt;();
    ///     }
    /// }
    /// </code>
    /// </remarks>
    public abstract class ToolDebugViewModelBase : ViewModelBase
    {
        private ParameterBindingContainer _bindingContainer;
        private IDataSourceQueryService? _dataSourceQueryService;
        private string? _nodeId;
        private string? _toolName;

        /// <summary>
        /// 参数绑定容器
        /// </summary>
        public ParameterBindingContainer BindingContainer
        {
            get => _bindingContainer;
            set => SetProperty(ref _bindingContainer, value);
        }

        /// <summary>
        /// 数据源查询服务
        /// </summary>
        public IDataSourceQueryService? DataSourceQueryService
        {
            get => _dataSourceQueryService;
            set => SetProperty(ref _dataSourceQueryService, value);
        }

        /// <summary>
        /// 节点ID
        /// </summary>
        public string? NodeId
        {
            get => _nodeId;
            set
            {
                if (SetProperty(ref _nodeId, value) && _bindingContainer != null)
                {
                    _bindingContainer.NodeId = value;
                }
            }
        }

        /// <summary>
        /// 工具名称
        /// </summary>
        public string? ToolName
        {
            get => _toolName;
            set
            {
                if (SetProperty(ref _toolName, value) && _bindingContainer != null)
                {
                    _bindingContainer.ToolName = value;
                }
            }
        }

        /// <summary>
        /// 是否有绑定配置
        /// </summary>
        public bool HasBindings => _bindingContainer != null && _bindingContainer.Count > 0;

        /// <summary>
        /// 绑定数量
        /// </summary>
        public int BindingCount => _bindingContainer?.Count ?? 0;

        /// <summary>
        /// 构造函数
        /// </summary>
        protected ToolDebugViewModelBase()
        {
            _bindingContainer = new ParameterBindingContainer();
        }

        /// <summary>
        /// 构造函数（带数据源查询服务）
        /// </summary>
        /// <param name="dataSourceQueryService">数据源查询服务</param>
        protected ToolDebugViewModelBase(IDataSourceQueryService? dataSourceQueryService)
            : this()
        {
            _dataSourceQueryService = dataSourceQueryService;
        }

        #region 绑定设置方法

        /// <summary>
        /// 设置常量绑定
        /// </summary>
        /// <param name="parameterName">参数名称</param>
        /// <param name="value">常量值</param>
        public void SetParameterBinding(string parameterName, object? value)
        {
            _bindingContainer.SetConstantBinding(parameterName, value);
            OnBindingChanged(parameterName);
        }

        /// <summary>
        /// 设置动态绑定
        /// </summary>
        /// <param name="parameterName">参数名称</param>
        /// <param name="sourceNodeId">源节点ID</param>
        /// <param name="sourceProperty">源属性名称</param>
        /// <param name="transformExpression">转换表达式（可选）</param>
        public void SetDynamicBinding(
            string parameterName,
            string sourceNodeId,
            string sourceProperty,
            string? transformExpression = null)
        {
            _bindingContainer.SetDynamicBinding(parameterName, sourceNodeId, sourceProperty, transformExpression);
            OnBindingChanged(parameterName);
        }

        /// <summary>
        /// 移除绑定
        /// </summary>
        /// <param name="parameterName">参数名称</param>
        public void RemoveBinding(string parameterName)
        {
            _bindingContainer.RemoveBinding(parameterName);
            OnBindingChanged(parameterName);
        }

        /// <summary>
        /// 获取绑定
        /// </summary>
        /// <param name="parameterName">参数名称</param>
        /// <returns>参数绑定</returns>
        public ParameterBinding? GetBinding(string parameterName)
        {
            return _bindingContainer.GetBinding(parameterName);
        }

        /// <summary>
        /// 检查是否有绑定
        /// </summary>
        /// <param name="parameterName">参数名称</param>
        /// <returns>是否存在绑定</returns>
        public bool HasBinding(string parameterName)
        {
            return _bindingContainer.HasBinding(parameterName);
        }

        /// <summary>
        /// 获取绑定类型
        /// </summary>
        /// <param name="parameterName">参数名称</param>
        /// <returns>绑定类型</returns>
        public BindingType GetBindingType(string parameterName)
        {
            var binding = _bindingContainer.GetBinding(parameterName);
            return binding?.BindingType ?? BindingType.Constant;
        }

        #endregion

        #region 数据源查询方法

        /// <summary>
        /// 获取可用数据源
        /// </summary>
        /// <param name="targetType">目标类型（可选）</param>
        /// <returns>可用数据源列表</returns>
        protected List<AvailableDataSource> GetAvailableDataSources(Type? targetType = null)
        {
            if (_dataSourceQueryService == null || string.IsNullOrEmpty(_nodeId))
            {
                return new List<AvailableDataSource>();
            }

            return _dataSourceQueryService.GetAvailableDataSources(_nodeId, targetType);
        }

        /// <summary>
        /// 获取指定类型的可用数据源
        /// </summary>
        /// <typeparam name="T">目标类型</typeparam>
        /// <returns>可用数据源列表</returns>
        protected List<AvailableDataSource> GetAvailableDataSources<T>()
        {
            return GetAvailableDataSources(typeof(T));
        }

        /// <summary>
        /// 获取父节点列表
        /// </summary>
        /// <returns>父节点信息列表</returns>
        protected List<ParentNodeInfo> GetParentNodes()
        {
            if (_dataSourceQueryService == null || string.IsNullOrEmpty(_nodeId))
            {
                return new List<ParentNodeInfo>();
            }

            return _dataSourceQueryService.GetParentNodes(_nodeId);
        }

        /// <summary>
        /// 获取父节点输出属性
        /// </summary>
        /// <param name="parentNodeId">父节点ID</param>
        /// <returns>输出属性列表</returns>
        protected List<AvailableDataSource> GetNodeOutputProperties(string parentNodeId)
        {
            if (_dataSourceQueryService == null)
            {
                return new List<AvailableDataSource>();
            }

            return _dataSourceQueryService.GetNodeOutputProperties(parentNodeId);
        }

        /// <summary>
        /// 验证当前绑定
        /// </summary>
        /// <returns>验证结果</returns>
        public ContainerValidationResult ValidateBindings()
        {
            return _bindingContainer.ValidateAll();
        }

        #endregion

        #region 事件

        /// <summary>
        /// 绑定变更事件
        /// </summary>
        public event Action<string>? BindingChanged;

        /// <summary>
        /// 触发绑定变更事件
        /// </summary>
        /// <param name="parameterName">参数名称</param>
        protected virtual void OnBindingChanged(string parameterName)
        {
            OnPropertyChanged(nameof(HasBindings));
            OnPropertyChanged(nameof(BindingCount));
            BindingChanged?.Invoke(parameterName);
        }

        #endregion

        #region 序列化支持

        /// <summary>
        /// 导出绑定配置
        /// </summary>
        /// <returns>绑定配置字典</returns>
        public Dictionary<string, object> ExportBindings()
        {
            return _bindingContainer.ToDictionary();
        }

        /// <summary>
        /// 导入绑定配置
        /// </summary>
        /// <param name="dict">绑定配置字典</param>
        public void ImportBindings(Dictionary<string, object> dict)
        {
            _bindingContainer = ParameterBindingContainer.FromDictionary(dict);
            OnPropertyChanged(nameof(BindingContainer));
            OnPropertyChanged(nameof(HasBindings));
            OnPropertyChanged(nameof(BindingCount));
        }

        #endregion
    }

    /// <summary>
    /// 参数绑定属性特性
    /// </summary>
    /// <remarks>
    /// 用于标记ViewModel中的属性与参数绑定的关联。
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class ParameterBindingPropertyAttribute : Attribute
    {
        /// <summary>
        /// 参数名称
        /// </summary>
        public string ParameterName { get; }

        /// <summary>
        /// 是否支持动态绑定
        /// </summary>
        public bool SupportsDynamicBinding { get; set; } = true;

        /// <summary>
        /// 描述信息
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 创建参数绑定属性特性
        /// </summary>
        /// <param name="parameterName">参数名称</param>
        public ParameterBindingPropertyAttribute(string parameterName)
        {
            ParameterName = parameterName;
        }
    }
}
