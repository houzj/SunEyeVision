using System;
using System.Collections.Generic;

namespace SunEyeVision.Plugin.SDK.UI.Controls.Region.Models
{
    /// <summary>
    /// 区域数据源提供者接口 - 用于获取和订阅节点输出
    /// </summary>
    public interface IRegionDataSourceProvider
    {
        /// <summary>
        /// 获取父节点输出列表（用于选择器）
        /// </summary>
        /// <param name="targetDataType">目标数据类型（用于筛选）</param>
        /// <returns>节点输出信息树</returns>
        IEnumerable<NodeOutputInfo> GetParentNodeOutputs(string? targetDataType = null);

        /// <summary>
        /// 订阅输出值变更
        /// </summary>
        /// <param name="nodeId">节点ID</param>
        /// <param name="outputName">输出名称</param>
        /// <param name="propertyPath">属性路径</param>
        /// <param name="onChanged">变更回调</param>
        /// <returns>取消订阅的Disposable</returns>
        IDisposable SubscribeOutputChanged(string nodeId, string outputName, 
            string? propertyPath, Action<object?> onChanged);

        /// <summary>
        /// 获取绑定显示路径
        /// </summary>
        string GetBindingDisplayPath(string nodeId, string outputName, string? propertyPath);

        /// <summary>
        /// 获取当前绑定值
        /// </summary>
        object? GetCurrentBindingValue(string nodeId, string outputName, string? propertyPath);

        /// <summary>
        /// 刷新所有输出信息
        /// </summary>
        void RefreshOutputs();
    }
}
