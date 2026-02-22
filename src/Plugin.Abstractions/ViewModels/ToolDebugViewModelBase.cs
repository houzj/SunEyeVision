using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using SunEyeVision.Plugin.Abstractions;

namespace SunEyeVision.Plugin.Abstractions.ViewModels
{
    /// <summary>
    /// 工具调试视图模型基类
    /// </summary>
    public abstract class ToolDebugViewModelBase : INotifyPropertyChanged
    {
        private string _toolId = string.Empty;
        private string _toolName = string.Empty;
        private string _toolStatus = "就绪";
        private string _statusMessage = "准备就绪";
        private string _executionTime = "0 ms";

        public event PropertyChangedEventHandler PropertyChanged;

        public string ToolId
        {
            get => _toolId;
            set => SetProperty(ref _toolId, value);
        }

        public string ToolName
        {
            get => _toolName;
            set => SetProperty(ref _toolName, value);
        }

        public string ToolStatus
        {
            get => _toolStatus;
            set => SetProperty(ref _toolStatus, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public string ExecutionTime
        {
            get => _executionTime;
            set => SetProperty(ref _executionTime, value);
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        /// <summary>
        /// 设置参数值（供子类使用）
        /// </summary>
        protected virtual void SetParamValue(string paramName, object value)
        {
            // 默认实现为空，子类可重写以保存参数
        }

        public virtual void Initialize(string toolId, IToolPlugin? toolPlugin, ToolMetadata? toolMetadata)
        {
            ToolId = toolId;
            ToolName = toolMetadata?.DisplayName ?? "未知工具";
            ToolStatus = "就绪";
            StatusMessage = "准备就绪";
            LoadParameters(toolMetadata);
        }

        public virtual void LoadParameters(ToolMetadata? toolMetadata)
        {
            // 默认实现为空，子类可重写以加载参数
        }

        public virtual Dictionary<string, object> SaveParameters()
        {
            // 默认实现返回空字典，子类可重写以保存参数
            return new Dictionary<string, object>();
        }

        public virtual void ResetParameters()
        {
            // 默认实现为空，子类可重写以重置参数
        }

        protected virtual Dictionary<string, object> BuildParameterDictionary()
        {
            // 默认实现返回空字典，子类可重写以构建参数字典
            return new Dictionary<string, object>();
        }

        public abstract void RunTool();
    }

    /// <summary>
    /// 自动工具调试视图模型基类
    /// </summary>
    public abstract class AutoToolDebugViewModelBase : ToolDebugViewModelBase
    {
        private string _debugMessage = string.Empty;
        
        public string DebugMessage
        {
            get => _debugMessage;
            set => SetProperty(ref _debugMessage, value);
        }
    }
}
