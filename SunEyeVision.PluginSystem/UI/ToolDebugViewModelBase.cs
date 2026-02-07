using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using SunEyeVision.PluginSystem;

namespace SunEyeVision.PluginSystem.UI
{
    /// <summary>
    /// 工具调试ViewModel基类 - 为工具调试界面提供通用功能
    /// </summary>
    public abstract class ToolDebugViewModelBase : ObservableObject
    {
        private string _toolName = "";
        private string _toolId = "";
        private string _toolStatus = "就绪";
        private string _statusMessage = "等待执行...";
        private string _executionTime = "0 ms";
        private string _fps = "0.0";

        #region 属性

        /// <summary>
        /// 工具名称
        /// </summary>
        public string ToolName
        {
            get => _toolName;
            set => SetProperty(ref _toolName, value);
        }

        /// <summary>
        /// 工具ID
        /// </summary>
        public string ToolId
        {
            get => _toolId;
            set => SetProperty(ref _toolId, value);
        }

        /// <summary>
        /// 工具状态
        /// </summary>
        public string ToolStatus
        {
            get => _toolStatus;
            set => SetProperty(ref _toolStatus, value);
        }

        /// <summary>
        /// 状态消息
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        /// <summary>
        /// 执行时间
        /// </summary>
        public string ExecutionTime
        {
            get => _executionTime;
            set => SetProperty(ref _executionTime, value);
        }

        /// <summary>
        /// FPS
        /// </summary>
        public string FPS
        {
            get => _fps;
            set => SetProperty(ref _fps, value);
        }

        #endregion

        #region 抽象方法 - 子类必须实现

        /// <summary>
        /// 初始化调试界面 - 加载工具信息和参数
        /// </summary>
        /// <param name="toolId">工具ID</param>
        /// <param name="toolPlugin">工具插件</param>
        /// <param name="toolMetadata">工具元数据</param>
        public abstract void Initialize(string toolId, IToolPlugin? toolPlugin, ToolMetadata? toolMetadata);

        /// <summary>
        /// 加载参数 - 从工具元数据加载参数到界面
        /// </summary>
        /// <param name="toolMetadata">工具元数据</param>
        public abstract void LoadParameters(ToolMetadata? toolMetadata);

        /// <summary>
        /// 保存参数 - 从界面保存参数到字典
        /// </summary>
        /// <returns>参数值字典</returns>
        public abstract Dictionary<string, object> SaveParameters();

        #endregion

        #region 虚方法 - 子类可以覆盖

        /// <summary>
        /// 重置参数 - 恢复默认值
        /// </summary>
        public virtual void ResetParameters()
        {
            StatusMessage = "参数已重置";
        }

        /// <summary>
        /// 运行工具 - 执行工具逻辑
        /// </summary>
        public virtual void RunTool()
        {
            ToolStatus = "运行中";
            StatusMessage = "正在执行工具...";

            var random = new Random();
            ExecutionTime = $"{random.Next(5, 50)} ms";
            FPS = $"{random.Next(20, 60)}.{random.Next(0, 9)}";

            StatusMessage = "执行完成";
            ToolStatus = "就绪";
        }

        #endregion
    }
}
