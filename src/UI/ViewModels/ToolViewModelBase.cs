using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.SDK.Execution.Results;
using SunEyeVision.Plugin.SDK.Metadata;
using SunEyeVision.Plugin.SDK.Validation;
using SunEyeVision.UI.Services.ParameterBinding;
using SunEyeVision.UI.Services.Thumbnail;

namespace SunEyeVision.UI.ViewModels
{
    /// <summary>
    /// 工具ViewModel基类
    /// </summary>
    /// <remarks>
    /// 为工具节点提供统一的参数绑定管理界面，简化工具开发人员的工作。
    /// 
    /// 核心功能：
    /// 1. 自动参数绑定管理
    /// 2. 参数验证
    /// 3. 执行状态管理
    /// 4. 数据源服务集成
    /// 
    /// 使用示例：
    /// <code>
    /// public class GaussianBlurViewModel : ToolViewModelBase&lt;GaussianBlurParameters, GaussianBlurResults&gt;
    /// {
    ///     public GaussianBlurViewModel(
    ///         ToolMetadata metadata,
    ///         IDataSourceQueryService dataSourceQueryService,
    ///         SmartThumbnailLoader thumbnailLoader)
    ///         : base(metadata, dataSourceQueryService, thumbnailLoader)
    ///     {
    ///     }
    ///     
    ///     protected override GaussianBlurParameters CreateDefaultParameters()
    ///     {
    ///         return new GaussianBlurParameters { KernelSize = 5, Sigma = 1.0 };
    ///     }
    /// }
    /// </code>
    /// </remarks>
    public abstract class ToolViewModelBase : ViewModelBase
    {
        #region 字段

        private readonly IDataSourceQueryService? _dataSourceQueryService;
        private readonly SmartThumbnailLoader? _thumbnailLoader;
        private readonly ImageDataSourceService? _imageDataSourceService;
        private string _nodeId = string.Empty;
        private string _nodeName = string.Empty;
        private ExecutionStatus _executionStatus = ExecutionStatus.NotExecuted;
        private string? _errorMessage;
        private long _executionTimeMs;
        private bool _isEnabled = true;

        #endregion

        #region 属性

        /// <summary>
        /// 工具元数据
        /// </summary>
        public ToolMetadata Metadata { get; }

        /// <summary>
        /// 节点ID
        /// </summary>
        public string NodeId
        {
            get => _nodeId;
            set => SetProperty(ref _nodeId, value);
        }

        /// <summary>
        /// 节点名称
        /// </summary>
        public string NodeName
        {
            get => _nodeName;
            set => SetProperty(ref _nodeName, value);
        }

        /// <summary>
        /// 执行状态
        /// </summary>
        public ExecutionStatus ExecutionStatus
        {
            get => _executionStatus;
            set
            {
                if (SetProperty(ref _executionStatus, value))
                {
                    OnPropertyChanged(nameof(StatusText));
                    OnPropertyChanged(nameof(StatusIcon));
                    OnPropertyChanged(nameof(StatusColor));
                    OnPropertyChanged(nameof(CanExecute));
                }
            }
        }

        /// <summary>
        /// 状态显示文本
        /// </summary>
        public string StatusText => ExecutionStatus switch
        {
            ExecutionStatus.NotExecuted => "未执行",
            ExecutionStatus.Running => "执行中...",
            ExecutionStatus.Success => "执行成功",
            ExecutionStatus.Failed => $"执行失败: {ErrorMessage}",
            ExecutionStatus.Timeout => "执行超时",
            ExecutionStatus.Cancelled => "已取消",
            ExecutionStatus.PartialSuccess => "部分成功",
            _ => "未知状态"
        };

        /// <summary>
        /// 状态图标
        /// </summary>
        public string StatusIcon => ExecutionStatus switch
        {
            ExecutionStatus.NotExecuted => "○",
            ExecutionStatus.Running => "⟳",
            ExecutionStatus.Success => "✓",
            ExecutionStatus.Failed => "✗",
            ExecutionStatus.Timeout => "⏱",
            ExecutionStatus.Cancelled => "⏹",
            ExecutionStatus.PartialSuccess => "◐",
            _ => "?"
        };

        /// <summary>
        /// 状态颜色
        /// </summary>
        public string StatusColor => ExecutionStatus switch
        {
            ExecutionStatus.NotExecuted => "Gray",
            ExecutionStatus.Running => "Orange",
            ExecutionStatus.Success => "Green",
            ExecutionStatus.Failed => "Red",
            ExecutionStatus.Timeout => "OrangeRed",
            ExecutionStatus.Cancelled => "Gray",
            ExecutionStatus.PartialSuccess => "Goldenrod",
            _ => "Gray"
        };

        /// <summary>
        /// 错误信息
        /// </summary>
        public string? ErrorMessage
        {
            get => _errorMessage;
            set
            {
                if (SetProperty(ref _errorMessage, value))
                {
                    OnPropertyChanged(nameof(StatusText));
                }
            }
        }

        /// <summary>
        /// 执行时间（毫秒）
        /// </summary>
        public long ExecutionTimeMs
        {
            get => _executionTimeMs;
            set => SetProperty(ref _executionTimeMs, value);
        }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled
        {
            get => _isEnabled;
            set => SetProperty(ref _isEnabled, value);
        }

        /// <summary>
        /// 是否可以执行
        /// </summary>
        public virtual bool CanExecute =>
            IsEnabled &&
            ExecutionStatus != ExecutionStatus.Running &&
            ValidateParameters().IsValid;

        /// <summary>
        /// 参数绑定列表
        /// </summary>
        public ObservableCollection<ParameterBindingViewModelBase> ParameterBindings { get; }

        #endregion

        #region 命令

        /// <summary>
        /// 执行命令
        /// </summary>
        public ICommand ExecuteCommand { get; }

        /// <summary>
        /// 验证命令
        /// </summary>
        public ICommand ValidateCommand { get; }

        /// <summary>
        /// 重置参数命令
        /// </summary>
        public ICommand ResetParametersCommand { get; }

        /// <summary>
        /// 刷新数据源命令
        /// </summary>
        public ICommand RefreshDataSourcesCommand { get; }

        #endregion

        #region 事件

        /// <summary>
        /// 执行完成事件
        /// </summary>
        public event EventHandler<ToolExecutionCompletedEventArgs>? ExecutionCompleted;

        /// <summary>
        /// 参数变更事件
        /// </summary>
        public event EventHandler<ParameterChangedEventArgs>? ParameterChanged;

        #endregion

        #region 构造函数

        /// <summary>
        /// 创建工具ViewModel基类
        /// </summary>
        protected ToolViewModelBase(
            ToolMetadata metadata,
            IDataSourceQueryService? dataSourceQueryService = null,
            SmartThumbnailLoader? thumbnailLoader = null)
        {
            Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
            _dataSourceQueryService = dataSourceQueryService;
            _thumbnailLoader = thumbnailLoader;

            if (dataSourceQueryService != null)
            {
                _imageDataSourceService = new ImageDataSourceService(dataSourceQueryService);
            }

            ParameterBindings = new ObservableCollection<ParameterBindingViewModelBase>();

            ExecuteCommand = new RelayCommand(ExecuteExecute, () => CanExecute);
            ValidateCommand = new RelayCommand(ExecuteValidate);
            ResetParametersCommand = new RelayCommand(ExecuteResetParameters);
            RefreshDataSourcesCommand = new RelayCommand(ExecuteRefreshDataSources);
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 初始化参数绑定
        /// </summary>
        /// <param name="nodeId">节点ID</param>
        public virtual void InitializeParameterBindings(string nodeId)
        {
            NodeId = nodeId;
            ParameterBindings.Clear();

            var parameters = CreateDefaultParameters();
            if (parameters == null)
                return;

            var runtimeParams = parameters.GetRuntimeParameterMetadata();
            foreach (var param in runtimeParams)
            {
                ParameterBindingViewModelBase bindingVm;

                // 检查是否为图像类型
                if (_imageDataSourceService != null && ImageDataSourceService.IsImageType(param.Type))
                {
                    // 创建图像参数绑定
                    bindingVm = CreateImageParameterBinding(param);
                }
                else
                {
                    // 创建普通参数绑定
                    bindingVm = CreateStandardParameterBinding(param);
                }

                bindingVm.BindingChanged += OnBindingChanged;
                ParameterBindings.Add(bindingVm);
            }

            // 加载数据源
            ExecuteRefreshDataSources();
        }

        /// <summary>
        /// 刷新数据源列表
        /// </summary>
        public void RefreshDataSources()
        {
            ExecuteRefreshDataSources();
        }

        /// <summary>
        /// 验证参数
        /// </summary>
        public virtual ValidationResult ValidateParameters()
        {
            var result = new ValidationResult();

            foreach (var binding in ParameterBindings)
            {
                if (!binding.IsValid)
                {
                    result.AddError($"{binding.DisplayName}: {binding.ValidationMessage}");
                }
            }

            return result;
        }

        /// <summary>
        /// 获取参数绑定配置容器
        /// </summary>
        public ParameterBindingContainer GetBindingContainer()
        {
            var container = new ParameterBindingContainer();

            foreach (var binding in ParameterBindings)
            {
                container.SetBinding(binding.GetBinding());
            }

            return container;
        }

        /// <summary>
        /// 设置执行结果
        /// </summary>
        public virtual void SetExecutionResult(ToolResults results)
        {
            ExecutionStatus = results.Status;
            ExecutionTimeMs = results.ExecutionTimeMs;
            ErrorMessage = results.ErrorMessage;
        }

        #endregion

        #region 抽象方法

        /// <summary>
        /// 创建默认参数
        /// </summary>
        protected abstract ToolParameters CreateDefaultParameters();

        #endregion

        #region 虚方法

        /// <summary>
        /// 创建图像参数绑定ViewModel
        /// </summary>
        protected virtual ImageParameterBindingViewModel CreateImageParameterBinding(RuntimeParameterMetadata param)
        {
            return new ImageParameterBindingViewModel(
                param.Name,
                param.DisplayName,
                _dataSourceQueryService!,
                _thumbnailLoader,
                param.Type,
                param.Description);
        }

        /// <summary>
        /// 创建标准参数绑定ViewModel
        /// </summary>
        protected virtual ParameterBindingViewModel CreateStandardParameterBinding(RuntimeParameterMetadata param)
        {
            return new ParameterBindingViewModel(
                param.Name,
                param.DisplayName,
                param.Type,
                param.Value,
                param.Description,
                _dataSourceQueryService);
        }

        #endregion

        #region 私有方法

        private void ExecuteExecute()
        {
            // 由具体实现类处理执行逻辑
        }

        private void ExecuteValidate()
        {
            var result = ValidateParameters();
            OnPropertyChanged(nameof(CanExecute));
        }

        private void ExecuteResetParameters()
        {
            InitializeParameterBindings(NodeId);
        }

        private void ExecuteRefreshDataSources()
        {
            foreach (var binding in ParameterBindings)
            {
                if (binding is ImageParameterBindingViewModel imageBinding)
                {
                    imageBinding.LoadImageDataSources(NodeId);
                }
                else if (binding is ParameterBindingViewModel standardBinding)
                {
                    standardBinding.RefreshAvailableDataSources(NodeId);
                }
            }
        }

        private void OnBindingChanged(object? sender, ParameterBinding e)
        {
            ParameterChanged?.Invoke(this, new ParameterChangedEventArgs(e.ParameterName, e));
        }

        #endregion
    }

    /// <summary>
    /// 泛型工具ViewModel基类
    /// </summary>
    public abstract class ToolViewModelBase<TParameters, TResults> : ToolViewModelBase
        where TParameters : ToolParameters, new()
        where TResults : ToolResults, new()
    {
        private TParameters _parameters = new();

        /// <summary>
        /// 当前参数
        /// </summary>
        public TParameters Parameters
        {
            get => _parameters;
            protected set => SetProperty(ref _parameters, value);
        }

        /// <summary>
        /// 最近执行结果
        /// </summary>
        public TResults? LastResult { get; protected set; }

        protected ToolViewModelBase(
            ToolMetadata metadata,
            IDataSourceQueryService? dataSourceQueryService = null,
            SmartThumbnailLoader? thumbnailLoader = null)
            : base(metadata, dataSourceQueryService, thumbnailLoader)
        {
        }

        /// <summary>
        /// 创建默认参数
        /// </summary>
        protected override ToolParameters CreateDefaultParameters()
        {
            return new TParameters();
        }

        /// <summary>
        /// 设置执行结果
        /// </summary>
        public override void SetExecutionResult(ToolResults results)
        {
            base.SetExecutionResult(results);

            if (results is TResults typedResults)
            {
                LastResult = typedResults;
            }
        }

        /// <summary>
        /// 应用参数值
        /// </summary>
        public void ApplyParameterValues(TParameters parameters)
        {
            Parameters = parameters;
            OnPropertyChanged(nameof(Parameters));
        }
    }

    #region 辅助类

    /// <summary>
    /// 参数绑定ViewModel基类（用于统一接口）
    /// </summary>
    public abstract class ParameterBindingViewModelBase : ViewModelBase
    {
        private bool _isValid = true;
        private string _validationMessage = string.Empty;

        /// <summary>
        /// 参数名称
        /// </summary>
        public abstract string ParameterName { get; }

        /// <summary>
        /// 显示名称
        /// </summary>
        public abstract string DisplayName { get; }

        /// <summary>
        /// 是否有效
        /// </summary>
        public bool IsValid
        {
            get => _isValid;
            protected set => SetProperty(ref _isValid, value);
        }

        /// <summary>
        /// 验证消息
        /// </summary>
        public string ValidationMessage
        {
            get => _validationMessage;
            protected set => SetProperty(ref _validationMessage, value);
        }

        /// <summary>
        /// 获取绑定配置
        /// </summary>
        public abstract ParameterBinding GetBinding();

        /// <summary>
        /// 绑定变更事件
        /// </summary>
        public event EventHandler<ParameterBinding>? BindingChanged;

        protected void RaiseBindingChanged(ParameterBinding binding)
        {
            BindingChanged?.Invoke(this, binding);
        }
    }

    /// <summary>
    /// 工具执行完成事件参数
    /// </summary>
    public class ToolExecutionCompletedEventArgs : EventArgs
    {
        public ToolResults Results { get; }
        public bool Success => Results.Status == ExecutionStatus.Success;

        public ToolExecutionCompletedEventArgs(ToolResults results)
        {
            Results = results;
        }
    }

    /// <summary>
    /// 参数变更事件参数
    /// </summary>
    public class ParameterChangedEventArgs : EventArgs
    {
        public string ParameterName { get; }
        public ParameterBinding NewBinding { get; }

        public ParameterChangedEventArgs(string parameterName, ParameterBinding newBinding)
        {
            ParameterName = parameterName;
            NewBinding = newBinding;
        }
    }

    #endregion
}
