using System;
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.SDK.Metadata;
using SunEyeVision.Plugin.SDK.ViewModels;

namespace SunEyeVision.Plugin.SDK.UI
{
    /// <summary>
    /// 泛型工具调试窗口基类 - 自动关联ViewModel
    /// </summary>
    /// <typeparam name="TViewModel">ViewModel类型</typeparam>
    /// <remarks>
    /// 此类继承 BaseToolDebugWindow 并自动关联指定类型的 ViewModel，
    /// 实现运行按钮自动调用 RunTool()，重置按钮自动调用 ResetParameters()。
    /// 
    /// 使用示例：
    /// <code>
    /// public partial class ThresholdToolDebugWindow 
    ///     : BaseToolDebugWindow&lt;ThresholdToolViewModel&gt;
    /// {
    ///     public ThresholdToolDebugWindow(string toolId, IToolPlugin toolPlugin, ToolMetadata metadata)
    ///         : base(toolId, toolPlugin, metadata)
    ///     {
    ///         InitializeComponent();
    ///         // 构建 UI
    ///         AddToBasicParams(new ParamSlider { ... });
    ///     }
    ///     
    ///     protected override void OnViewModelInitialized(ThresholdToolViewModel viewModel)
    ///     {
    ///         // ViewModel 初始化完成后的回调
    ///         // 可以在这里进行额外的绑定设置
    ///     }
    /// }
    /// </code>
    /// </remarks>
    public abstract class BaseToolDebugWindow<TViewModel> : BaseToolDebugWindow
        where TViewModel : ToolViewModelBase, new()
    {
        /// <summary>
        /// 关联的ViewModel实例
        /// </summary>
        protected TViewModel ViewModel { get; private set; } = null!;

        /// <summary>
        /// 默认构造函数 - 创建新的ViewModel实例
        /// </summary>
        protected BaseToolDebugWindow()
        {
            ViewModel = new TViewModel();
            DataContext = ViewModel;
        }

        /// <summary>
        /// 带初始化参数的构造函数
        /// </summary>
        /// <param name="toolId">工具ID</param>
        /// <param name="toolPlugin">工具插件实例</param>
        /// <param name="toolMetadata">工具元数据</param>
        protected BaseToolDebugWindow(string toolId, IToolPlugin? toolPlugin, ToolMetadata? toolMetadata)
            : this()
        {
            Initialize(toolId, toolPlugin, toolMetadata);
        }

        /// <summary>
        /// 初始化ViewModel
        /// </summary>
        /// <param name="toolId">工具ID</param>
        /// <param name="toolPlugin">工具插件实例</param>
        /// <param name="toolMetadata">工具元数据</param>
        public void Initialize(string toolId, IToolPlugin? toolPlugin, ToolMetadata? toolMetadata)
        {
            ViewModel.Initialize(toolId, toolPlugin, toolMetadata);
            OnViewModelInitialized(ViewModel);
        }

        /// <summary>
        /// ViewModel初始化完成后的回调 - 子类可重写以进行额外设置
        /// </summary>
        /// <param name="viewModel">已初始化的ViewModel</param>
        protected virtual void OnViewModelInitialized(TViewModel viewModel)
        {
            // 子类可重写此方法进行额外的绑定设置
        }

        /// <summary>
        /// 运行按钮点击时调用 - 自动调用ViewModel.RunTool()
        /// </summary>
        protected override void OnExecuteRequested()
        {
            ViewModel.RunTool();
            base.OnExecuteRequested();
        }

        /// <summary>
        /// 重置按钮点击时调用 - 自动调用ViewModel.ResetParameters()
        /// </summary>
        protected override void OnResetRequested()
        {
            ViewModel.ResetParameters();
            base.OnResetRequested();
        }

        /// <summary>
        /// 异步运行工具
        /// </summary>
        protected async void RunToolAsync()
        {
            await ViewModel.RunToolAsync();
        }
    }
}
