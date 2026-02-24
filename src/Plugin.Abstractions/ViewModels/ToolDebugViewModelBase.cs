using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using SunEyeVision.Plugin.Abstractions;
using SunEyeVision.Plugin.Abstractions.Core;

namespace SunEyeVision.Plugin.Abstractions.ViewModels
{
    /// <summary>
    /// 工具调试视图模型基类
    /// </summary>
    /// <remarks>
    /// 重构后的基类统一使用 IImageProcessor.Execute() 执行工具逻辑，
    /// 确保 Debug UI 和 Workflow Engine 使用相同的算法实现。
    /// </remarks>
    public abstract class ToolDebugViewModelBase : INotifyPropertyChanged
    {
        private string _toolId = string.Empty;
        private string _toolName = string.Empty;
        private string _toolStatus = "就绪";
        private string _statusMessage = "准备就绪";
        private string _executionTime = "0 ms";
        private IToolPlugin? _toolPlugin;
        private IImageProcessor? _processor;

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 关联的图像处理器实例
        /// </summary>
        protected IImageProcessor? Processor => _processor;

        /// <summary>
        /// 关联的工具插件实例
        /// </summary>
        protected IToolPlugin? ToolPlugin => _toolPlugin;

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
            _toolPlugin = toolPlugin;
            
            // 创建处理器实例
            if (toolPlugin != null)
            {
                _processor = toolPlugin.CreateToolInstance(toolId);
            }
            
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

        /// <summary>
        /// 构建参数字典（供 Execute 使用）
        /// </summary>
        protected virtual Dictionary<string, object> BuildParameterDictionary()
        {
            // 默认实现返回空字典，子类可重写以构建参数字典
            return new Dictionary<string, object>();
        }

        /// <summary>
        /// 构建 AlgorithmParameters（从 BuildParameterDictionary 转换）
        /// </summary>
        protected virtual AlgorithmParameters BuildAlgorithmParameters()
        {
            var dict = BuildParameterDictionary();
            var parameters = new AlgorithmParameters();
            foreach (var kvp in dict)
            {
                parameters.Set(kvp.Key, kvp.Value);
            }
            return parameters;
        }

        /// <summary>
        /// 运行工具（默认实现使用 Processor.Execute）
        /// </summary>
        public virtual void RunTool()
        {
            if (_processor == null)
            {
                ToolStatus = "错误";
                StatusMessage = "处理器未初始化";
                return;
            }

            ToolStatus = "运行中";
            StatusMessage = $"正在执行 {ToolName}...";

            try
            {
                // 获取输入图像（子类可重写 GetInputImage）
                var inputImage = GetInputImage();
                
                // 构建参数
                var parameters = BuildAlgorithmParameters();
                
                // 执行处理
                var result = _processor.Execute(inputImage, parameters);
                
                // 处理结果
                if (result.Success)
                {
                    ExecutionTime = $"{result.ExecutionTimeMs} ms";
                    StatusMessage = $"{ToolName} 完成 - 耗时: {ExecutionTime}";
                    ToolStatus = "就绪";
                    
                    // 子类可重写以处理输出
                    OnExecutionCompleted(result);
                }
                else
                {
                    StatusMessage = $"执行失败: {result.ErrorMessage}";
                    ToolStatus = "错误";
                    OnExecutionFailed(result);
                }
            }
            catch (System.Exception ex)
            {
                StatusMessage = $"执行异常: {ex.Message}";
                ToolStatus = "错误";
                OnExecutionException(ex);
            }
        }

        /// <summary>
        /// 获取输入图像（子类重写以提供实际图像）
        /// </summary>
        protected virtual object? GetInputImage()
        {
            // 默认返回 null，子类应重写以提供实际输入图像
            return null;
        }

        /// <summary>
        /// 执行成功回调（子类可重写以处理输出）
        /// </summary>
        protected virtual void OnExecutionCompleted(AlgorithmResult result)
        {
            // 默认实现为空，子类可重写
        }

        /// <summary>
        /// 执行失败回调
        /// </summary>
        protected virtual void OnExecutionFailed(AlgorithmResult result)
        {
            // 默认实现为空，子类可重写
        }

        /// <summary>
        /// 执行异常回调
        /// </summary>
        protected virtual void OnExecutionException(System.Exception ex)
        {
            // 默认实现为空，子类可重写
        }
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
