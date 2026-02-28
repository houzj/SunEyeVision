using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.SDK.Execution.Results;
using SunEyeVision.Plugin.SDK.Metadata;

namespace SunEyeVision.Plugin.SDK.ViewModels
{
    /// <summary>
    /// 工具 ViewModel 基类
    /// </summary>
    /// <remarks>
    /// 提供工具界面的通用功能，包括参数管理、状态更新、属性变更通知等。
    /// 支持异步执行、取消操作和真正的工具调用。
    /// 
    /// 使用方式：
    /// 1. 子类实现 GetRunParameters() 方法构建参数
    /// 2. 可选重写 UpdateResultItems() 处理特定结果
    /// 3. 可选重写 OnExecutionCompleted() 处理执行完成回调
    /// 
    /// 命名参考 VisionPro 等视觉软件的 Tool 概念。
    /// </remarks>
    public abstract class ToolViewModelBase : INotifyPropertyChanged
    {
        #region 私有字段

        private string _toolId = string.Empty;
        private string _toolName = string.Empty;
        private string _toolStatus = "就绪";
        private string _statusMessage = "准备就绪";
        private string _debugMessage = string.Empty;
        private string _executionTime = "0 ms";
        private IToolPlugin? _toolPlugin;
        private ToolMetadata? _toolMetadata;

        // 工具执行相关
        private ToolRunner? _runner;
        private ITool? _tool;
        private Mat? _currentImage;
        private bool _isExecuting;

        #endregion

        #region 属性

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
        /// 是否正在执行
        /// </summary>
        public bool IsExecuting
        {
            get => _isExecuting;
            private set => SetProperty(ref _isExecuting, value);
        }

        /// <summary>
        /// 是否可以执行
        /// </summary>
        public bool CanExecute => !IsExecuting && CurrentImage != null && !CurrentImage.Empty();

        /// <summary>
        /// 工具插件实例
        /// </summary>
        public IToolPlugin? ToolPlugin => _toolPlugin;

        /// <summary>
        /// 工具元数据
        /// </summary>
        public ToolMetadata? ToolMetadata => _toolMetadata;

        /// <summary>
        /// 当前输入图像
        /// </summary>
        public Mat? CurrentImage
        {
            get => _currentImage;
            set
            {
                if (_currentImage != value)
                {
                    _currentImage?.Dispose();
                    _currentImage = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CanExecute));
                }
            }
        }

        /// <summary>
        /// 输出图像（用于显示）
        /// </summary>
        public BitmapSource? OutputImage { get; protected set; }

        #endregion

        #region 事件

        /// <summary>
        /// 属性变更事件
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// 执行完成事件
        /// </summary>
        public event EventHandler<RunResult>? ExecutionCompleted;

        #endregion

        #region 初始化

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

            // 初始化工具实例
            _tool = toolPlugin?.CreateToolInstance(toolId);

            // 初始化 ToolRunner
            if (_tool != null)
            {
                var executor = new ToolExecutor(new ParameterResolver());
                _runner = new ToolRunner(executor);
                _runner.RunCompleted += OnRunnerCompleted;
            }

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

        #endregion

        #region 工具执行

        /// <summary>
        /// 运行工具（同步入口，内部调用异步方法）
        /// </summary>
        public void RunTool()
        {
            RunToolAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        }

        /// <summary>
        /// 运行工具（异步版本）
        /// </summary>
        public async Task RunToolAsync()
        {
            if (_runner == null || _tool == null)
            {
                StatusMessage = "工具未初始化";
                ToolStatus = "错误";
                return;
            }

            if (_runner.IsRunning)
            {
                StatusMessage = "工具正在执行中";
                return;
            }

            if (CurrentImage == null || CurrentImage.Empty())
            {
                StatusMessage = "请先加载输入图像";
                ToolStatus = "错误";
                return;
            }

            var stopwatch = Stopwatch.StartNew();

            try
            {
                IsExecuting = true;
                ToolStatus = "运行中";
                StatusMessage = "正在执行...";

                // 获取运行参数
                var parameters = GetRunParameters();
                if (parameters == null)
                {
                    throw new InvalidOperationException("GetRunParameters() 返回 null");
                }

                // 异步执行
                var result = await _runner.RunAsync(_tool, parameters, CurrentImage);

                stopwatch.Stop();

                // 更新 UI
                UpdateExecutionResult(result);

                // 触发完成事件
                ExecutionCompleted?.Invoke(this, result);

                // 调用子类的回调
                OnExecutionCompleted(result);
            }
            catch (OperationCanceledException)
            {
                stopwatch.Stop();
                ToolStatus = "已取消";
                StatusMessage = "执行被用户取消";
                ExecutionTime = $"{stopwatch.ElapsedMilliseconds} ms";
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                ToolStatus = "错误";
                StatusMessage = $"执行失败: {ex.Message}";
                ExecutionTime = $"{stopwatch.ElapsedMilliseconds} ms";
                DebugMessage = $"异常: {ex.Message}\n{ex.StackTrace}";
            }
            finally
            {
                IsExecuting = false;
            }
        }

        /// <summary>
        /// 停止工具执行
        /// </summary>
        public virtual void StopTool()
        {
            if (_runner?.IsRunning == true)
            {
                _runner.Cancel();
                ToolStatus = "取消中";
                StatusMessage = "正在取消执行...";
            }
            else
            {
                ToolStatus = "已停止";
                StatusMessage = "工具已停止";
            }
        }

        /// <summary>
        /// 重置参数
        /// </summary>
        public virtual void ResetParameters()
        {
            LoadParameters(_toolMetadata);
            StatusMessage = "参数已重置";
            DebugMessage = string.Empty;
        }

        #endregion

        #region 参数构建

        /// <summary>
        /// 获取当前运行参数（子类必须实现）
        /// </summary>
        /// <remarks>
        /// 子类应该根据 ParamValues 或自己的属性构建具体类型的参数对象。
        /// 命名参考 VisionPro 等视觉软件的 RunParams 概念。
        /// 例如：
        /// <code>
        /// protected override ToolParameters GetRunParameters()
        /// {
        ///     return new ThresholdParameters
        ///     {
        ///         Threshold = this.Threshold,
        ///         MaxValue = this.MaxValue,
        ///         Type = ParseThresholdType(this.ThresholdType)
        ///     };
        /// }
        /// </code>
        /// 或者使用辅助方法：
        /// <code>
        /// protected override ToolParameters GetRunParameters()
        /// {
        ///     return GetParametersFromDictionary&lt;ThresholdParameters&gt;();
        /// }
        /// </code>
        /// </remarks>
        protected abstract ToolParameters GetRunParameters();

        /// <summary>
        /// 从字典获取参数（辅助方法）
        /// </summary>
        /// <typeparam name="T">参数类型</typeparam>
        /// <returns>构建的参数对象</returns>
        protected T GetParametersFromDictionary<T>() where T : ToolParameters, new()
        {
            var parameters = new T();
            var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var prop in props)
            {
                if (!prop.CanWrite) continue;
                if (prop.Name == "Version") continue;

                if (ParamValues.TryGetValue(prop.Name, out var value))
                {
                    try
                    {
                        if (value?.GetType() == prop.PropertyType)
                        {
                            prop.SetValue(parameters, value);
                        }
                        else if (value != null)
                        {
                            var converted = Convert.ChangeType(value, prop.PropertyType);
                            prop.SetValue(parameters, converted);
                        }
                    }
                    catch
                    {
                        // 转换失败，使用默认值
                    }
                }
            }

            return parameters;
        }

        #endregion

        #region 结果处理

        /// <summary>
        /// 更新执行结果
        /// </summary>
        protected virtual void UpdateExecutionResult(RunResult result)
        {
            ExecutionTime = $"{result.ExecutionTimeMs} ms";

            if (result.IsSuccess)
            {
                ToolStatus = "成功";
                StatusMessage = "执行完成";

                // 获取输出图像
                var outputImage = GetOutputImage(result.ToolResult);
                if (outputImage != null && !outputImage.Empty())
                {
                    OutputImage = outputImage.ToBitmapSource();
                    OnPropertyChanged(nameof(OutputImage));
                }

                // 显示结果项
                var resultItems = result.GetResultItems();
                if (resultItems.Count > 0)
                {
                    UpdateResultItems(resultItems);
                }
            }
            else
            {
                ToolStatus = "失败";
                StatusMessage = result.ErrorMessage ?? "执行失败";
                DebugMessage = result.ErrorStackTrace ?? result.ErrorMessage ?? "未知错误";
            }
        }

        /// <summary>
        /// 从工具结果中获取输出图像
        /// </summary>
        private Mat? GetOutputImage(ToolResults? toolResult)
        {
            if (toolResult == null) return null;

            // 尝试通过反射获取 OutputImage 属性
            var outputImageProp = toolResult.GetType().GetProperty("OutputImage", BindingFlags.Public | BindingFlags.Instance);
            if (outputImageProp != null)
            {
                var value = outputImageProp.GetValue(toolResult);
                if (value is Mat mat)
                {
                    return mat;
                }
            }

            // 尝试从结果项中获取图像
            var resultItems = toolResult.GetResultItems();
            var imageItem = resultItems.FirstOrDefault(i => i.Type == ResultItemType.Image);
            if (imageItem?.Value is Mat imageMat)
            {
                return imageMat;
            }

            return null;
        }

        /// <summary>
        /// 更新结果项（子类可重写）
        /// </summary>
        protected virtual void UpdateResultItems(IReadOnlyList<ResultItem> resultItems)
        {
            // 子类可以重写此方法来显示特定的结果项
            var summary = new List<string>();
            foreach (var item in resultItems)
            {
                if (item.Type != ResultItemType.Image)  // 跳过图像
                {
                    var valueStr = item.Value?.ToString() ?? "null";
                    var unitStr = !string.IsNullOrEmpty(item.Unit) ? $" {item.Unit}" : "";
                    var displayName = item.DisplayName ?? item.Name;
                    summary.Add($"{displayName}: {valueStr}{unitStr}");
                }
            }

            if (summary.Count > 0)
            {
                DebugMessage = string.Join("\n", summary);
            }
        }

        /// <summary>
        /// 执行完成回调（子类可重写）
        /// </summary>
        protected virtual void OnExecutionCompleted(RunResult result)
        {
            // 子类可重写此方法处理执行结果
        }

        /// <summary>
        /// Runner 完成事件处理
        /// </summary>
        private void OnRunnerCompleted(object? sender, RunResult result)
        {
            // 可以在这里添加额外的处理逻辑
        }

        #endregion

        #region 属性通知

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
        /// 获取当前参数字典
        /// </summary>
        public Dictionary<string, object> GetParameters()
        {
            return new Dictionary<string, object>(ParamValues);
        }

        #endregion

        #region 资源清理

        /// <summary>
        /// 清理资源
        /// </summary>
        public virtual void Cleanup()
        {
            _runner?.Cancel();
            _runner = null;
            _tool = null;
            _currentImage?.Dispose();
            _currentImage = null;
        }

        #endregion
    }
}
