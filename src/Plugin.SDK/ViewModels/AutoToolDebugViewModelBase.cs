using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.SDK.Metadata;

namespace SunEyeVision.Plugin.SDK.ViewModels
{
    /// <summary>
    /// 算法执行结果（用于 ViewModel 回调）
    /// </summary>
    public class AlgorithmResult
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Success { get; set; } = true;

        /// <summary>
        /// 结果数据
        /// </summary>
        public object? Data { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// 执行时间（毫秒）
        /// </summary>
        public double ExecutionTimeMs { get; set; }
    }

    /// <summary>
    /// 工具调试 ViewModel 基类
    /// </summary>
    /// <remarks>
    /// 提供工具调试界面的通用功能，包括参数管理、状态更新、属性变更通知等。
    /// </remarks>
    public abstract class AutoToolDebugViewModelBase : INotifyPropertyChanged
    {
        private string _toolId = string.Empty;
        private string _toolName = string.Empty;
        private string _toolStatus = "就绪";
        private string _statusMessage = "准备就绪";
        private string _debugMessage = string.Empty;
        private string _executionTime = "0 ms";
        private IToolPlugin? _toolPlugin;
        private ToolMetadata? _toolMetadata;

        /// <summary>
        /// 参数值字典
        /// </summary>
        protected Dictionary<string, object> ParamValues { get; } = new();

        /// <summary>
        /// 工具ID
        /// </summary>
        public string ToolId
        {
            get => _toolId;
            set => SetProperty(ref _toolId, value);
        }

        /// <summary>
        /// 工具名称
        /// </summary>
        public string ToolName
        {
            get => _toolName;
            set => SetProperty(ref _toolName, value);
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
        /// 调试消息
        /// </summary>
        public string DebugMessage
        {
            get => _debugMessage;
            set => SetProperty(ref _debugMessage, value);
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
        /// 工具插件实例
        /// </summary>
        public IToolPlugin? ToolPlugin => _toolPlugin;

        /// <summary>
        /// 工具元数据
        /// </summary>
        public ToolMetadata? ToolMetadata => _toolMetadata;

        /// <summary>
        /// 属性变更事件
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// 触发属性变更通知
        /// </summary>
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// 设置属性值并触发通知
        /// </summary>
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        /// <summary>
        /// 设置参数值
        /// </summary>
        protected void SetParamValue(string paramName, object value)
        {
            ParamValues[paramName] = value;
        }

        /// <summary>
        /// 获取参数值
        /// </summary>
        protected T? GetParamValue<T>(string paramName, T? defaultValue = default)
        {
            if (ParamValues.TryGetValue(paramName, out var value))
            {
                if (value is T typedValue)
                    return typedValue;
                try
                {
                    return (T?)Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }

        /// <summary>
        /// 初始化 ViewModel
        /// </summary>
        public virtual void Initialize(string toolId, IToolPlugin? toolPlugin, ToolMetadata? toolMetadata)
        {
            _toolId = toolId;
            _toolPlugin = toolPlugin;
            _toolMetadata = toolMetadata;
            ToolName = toolMetadata?.DisplayName ?? "未知工具";
            ToolStatus = "就绪";
            StatusMessage = "准备就绪";
            LoadParameters(toolMetadata);
        }

        /// <summary>
        /// 从元数据加载参数
        /// </summary>
        protected virtual void LoadParameters(ToolMetadata? metadata)
        {
            if (metadata?.InputParameters == null)
                return;

            foreach (var param in metadata.InputParameters)
            {
                if (param.DefaultValue != null)
                {
                    ParamValues[param.Name] = param.DefaultValue;
                }
            }
        }

        /// <summary>
        /// 构建参数字典（供子类重写）
        /// </summary>
        protected virtual Dictionary<string, object> BuildParameterDictionary()
        {
            return new Dictionary<string, object>(ParamValues);
        }

        /// <summary>
        /// 执行完成回调
        /// </summary>
        protected virtual void OnExecutionCompleted(AlgorithmResult result)
        {
            // 子类可重写此方法处理执行结果
        }

        /// <summary>
        /// 运行工具
        /// </summary>
        public abstract void RunTool();

        /// <summary>
        /// 停止工具
        /// </summary>
        public virtual void StopTool()
        {
            ToolStatus = "已停止";
            StatusMessage = "工具已停止";
        }

        /// <summary>
        /// 重置参数
        /// </summary>
        public virtual void ResetParameters()
        {
            LoadParameters(_toolMetadata);
            StatusMessage = "参数已重置";
        }

        /// <summary>
        /// 获取当前参数字典
        /// </summary>
        public Dictionary<string, object> GetParameters()
        {
            return new Dictionary<string, object>(ParamValues);
        }
    }
}
