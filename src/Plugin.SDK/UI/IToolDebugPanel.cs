using System;
using System.Windows;
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.SDK.Execution.Parameters;

namespace SunEyeVision.Plugin.SDK.UI
{
    /// <summary>
    /// 工具调试面板接口 - 仅负责参数配置
    /// </summary>
    /// <remarks>
    /// 调试面板只处理参数，图像显示和ROI编辑由主界面统一管理。
    /// 
    /// 使用示例：
    /// <code>
    /// public class ThresholdDebugPanel : IToolDebugPanel
    /// {
    ///     public FrameworkElement ParameterPanel => _panel;
    ///     public event EventHandler? ExecuteRequested;
    ///     
    ///     public void SetTool(ITool tool) { ... }
    ///     public void Reset() { ... }
    /// }
    /// </code>
    /// </remarks>
    public interface IToolDebugPanel
    {
        /// <summary>
        /// 关联的工具实例
        /// </summary>
        ITool? Tool { get; }

        /// <summary>
        /// 参数面板UI元素
        /// </summary>
        FrameworkElement ParameterPanel { get; }

        /// <summary>
        /// 执行请求事件 - 用户点击运行按钮时触发
        /// </summary>
        event EventHandler? ExecuteRequested;

        /// <summary>
        /// ROI编辑请求事件 - 需要编辑ROI时触发（由主界面处理）
        /// </summary>
        event EventHandler<RoiEditRequestedEventArgs>? RoiEditRequested;

        /// <summary>
        /// 参数变更事件 - 参数值发生变化时触发
        /// </summary>
        event EventHandler? ParametersChanged;

        /// <summary>
        /// 设置关联的工具实例
        /// </summary>
        void SetTool(ITool tool);

        /// <summary>
        /// 重置参数到默认值
        /// </summary>
        void Reset();

        /// <summary>
        /// 获取当前参数
        /// </summary>
        ToolParameters? GetParameters();
    }

    /// <summary>
    /// ROI编辑请求事件参数
    /// </summary>
    public class RoiEditRequestedEventArgs : EventArgs
    {
        /// <summary>
        /// ROI属性名称
        /// </summary>
        public string PropertyName { get; set; } = string.Empty;

        /// <summary>
        /// ROI类型
        /// </summary>
        public string RoiType { get; set; } = string.Empty;

        /// <summary>
        /// 当前ROI数据
        /// </summary>
        public object? CurrentRoi { get; set; }

        /// <summary>
        /// ROI编辑完成回调
        /// </summary>
        public Action<object?>? OnRoiEdited { get; set; }
    }
}
