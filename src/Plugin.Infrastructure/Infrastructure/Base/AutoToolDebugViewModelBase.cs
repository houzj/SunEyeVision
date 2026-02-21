using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SunEyeVision.Plugin.Infrastructure.Base;
using SunEyeVision.Plugin.Infrastructure.Commands;
using SunEyeVision.Plugin.Abstractions;
using System.Windows.Input;

namespace SunEyeVision.Plugin.Infrastructure.Base
{
    /// <summary>
    /// 自动化工具ViewModel基类 - 增强版，包含完整的MVVM命令支持
    /// 支持参数管理、验证、异步执行、进度报告等完整功能
    /// </summary>
    public abstract class AutoToolDebugViewModelBase : ToolDebugViewModelBase
    {
        private Dictionary<string, object> _parameters = new Dictionary<string, object>();
        private string? _validationError;
        private bool _isBusy;
        private double _progress;
        private string _progressMessage = "";
        private ParameterRepository _repository = new ParameterRepository();
        private ParameterValidator _validator = new ParameterValidator();
        private List<ParameterSnapshot> _snapshots = new List<ParameterSnapshot>();

        #region 命令

        /// <summary>
        /// 运行命令（异步）
        /// </summary>
        public ICommand RunCommand { get; }

        /// <summary>
        /// 重置命令
        /// </summary>
        public ICommand ResetCommand { get; }

        /// <summary>
        /// 保存配置命令
        /// </summary>
        public ICommand SaveCommand { get; }

        /// <summary>
        /// 加载配置命令
        /// </summary>
        public ICommand LoadCommand { get; }

        /// <summary>
        /// 验证参数命令
        /// </summary>
        public ICommand ValidateCommand { get; }

        /// <summary>
        /// 创建快照命令
        /// </summary>
        public ICommand CreateSnapshotCommand { get; }

        /// <summary>
        /// 恢复快照命令
        /// </summary>
        public ICommand RestoreSnapshotCommand { get; }

        /// <summary>
        /// 取消执行命令
        /// </summary>
        public ICommand CancelCommand { get; }

        #endregion

        #region 属性

        /// <summary>
        /// 参数项集合（用于UI绑定）
        /// </summary>
        public ObservableCollection<ParameterItem> ParameterItems { get; }

        /// <summary>
        /// 验证错误消息
        /// </summary>
        public string? ValidationError
        {
            get => _validationError;
            set => SetProperty(ref _validationError, value);
        }

        /// <summary>
        /// 是否正在执行
        /// </summary>
        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        /// <summary>
        /// 执行进度（0-100）
        /// </summary>
        public double Progress
        {
            get => _progress;
            set => SetProperty(ref _progress, value);
        }

        /// <summary>
        /// 进度消息
        /// </summary>
        public string ProgressMessage
        {
            get => _progressMessage;
            set => SetProperty(ref _progressMessage, value);
        }

        /// <summary>
        /// 快照列表
        /// </summary>
        public ObservableCollection<ParameterSnapshot> Snapshots { get; }

        /// <summary>
        /// 参数验证器
        /// </summary>
        protected ParameterValidator Validator => _validator;

        /// <summary>
        /// 参数存储库
        /// </summary>
        protected ParameterRepository Repository => _repository;

        #endregion

        #region 构造函数

        protected AutoToolDebugViewModelBase()
        {
            ParameterItems = new ObservableCollection<ParameterItem>();
            Snapshots = new ObservableCollection<ParameterSnapshot>();

            // 初始化命令
            RunCommand = new AsyncRelayCommand(
                async (param, ct) => await RunToolAsync(ct),
                _ => !IsBusy,
                OnExecutionError);

            ResetCommand = new RelayCommand(
                () => ResetParameters(),
                () => !IsBusy);

            SaveCommand = new AsyncRelayCommand(
                async _ => await SaveParametersAsync(),
                _ => !IsBusy);

            LoadCommand = new AsyncRelayCommand(
                async _ => await LoadParametersAsync(),
                _ => !IsBusy);

            ValidateCommand = new RelayCommand(
                () => ValidateAllParameters(),
                () => !IsBusy);

            CreateSnapshotCommand = new RelayCommand(
                () => CreateSnapshot(),
                () => !IsBusy);

            RestoreSnapshotCommand = new RelayCommand<ParameterSnapshot>(
                snapshot => RestoreSnapshot(snapshot),
                snapshot => snapshot != null && !IsBusy);

            CancelCommand = new RelayCommand(
                () => CancelExecution(),
                () => IsBusy);
        }

        #endregion

        #region 参数加载和保存

        /// <summary>
        /// 自动加载参数 - 通过属性名匹配ToolMetadata中的参数
        /// </summary>
        public override void LoadParameters(ToolMetadata? toolMetadata)
        {
            if (toolMetadata?.InputParameters == null)
                return;

            var properties = GetType().GetProperties(
                System.Reflection.BindingFlags.Public | 
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.DeclaredOnly);

            foreach (var prop in properties)
            {
                var param = toolMetadata.InputParameters.FirstOrDefault(p => 
                    p.Name.Equals(prop.Name, StringComparison.OrdinalIgnoreCase));
                
                if (param?.DefaultValue != null && prop.CanWrite)
                {
                    try
                    {
                        var value = Convert.ChangeType(param.DefaultValue, prop.PropertyType);
                        prop.SetValue(this, value);
                        _parameters[prop.Name] = value;
                    }
                    catch
                    {
                        // 忽略转换错误
                    }
                }
            }
        }

        /// <summary>
        /// 自动保存参数 - 收集所有标记的属性
        /// </summary>
        public override Dictionary<string, object> SaveParameters()
        {
            var result = new Dictionary<string, object>();
            var properties = GetType().GetProperties(
                System.Reflection.BindingFlags.Public | 
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.DeclaredOnly);

            foreach (var prop in properties)
            {
                if (prop.CanRead && prop.CanWrite && 
                    prop.GetIndexParameters().Length == 0 &&
                    !prop.Name.Equals("StatusMessage", StringComparison.OrdinalIgnoreCase) &&
                    !prop.Name.Equals("ToolStatus", StringComparison.OrdinalIgnoreCase) &&
                    !prop.Name.Equals("ExecutionTime", StringComparison.OrdinalIgnoreCase) &&
                    !prop.Name.Equals("ToolName", StringComparison.OrdinalIgnoreCase) &&
                    !prop.Name.Equals("ToolId", StringComparison.OrdinalIgnoreCase))
                {
                    result[prop.Name] = prop.GetValue(this);
                }
            }
            return result;
        }

        /// <summary>
        /// 获取当前参数值
        /// </summary>
        protected T GetParamValue<T>(string key, T defaultValue = default)
        {
            return ToolUIHelpers.GetValue(_parameters, key, defaultValue);
        }

        /// <summary>
        /// 设置参数值
        /// </summary>
        protected void SetParamValue(string key, object value)
        {
            _parameters[key] = value;
        }

        /// <summary>
        /// 批量设置参数
        /// </summary>
        protected void SetParamValues(params (string key, object value)[] parameters)
        {
            foreach (var (key, value) in parameters)
            {
                _parameters[key] = value;
            }
        }

        #endregion

        #region 参数管理

        /// <summary>
        /// 添加参数项
        /// </summary>
        protected void AddParameterItem(ParameterItem item)
        {
            ParameterItems.Add(item);
        }

        /// <summary>
        /// 移除参数项
        /// </summary>
        protected void RemoveParameterItem(string name)
        {
            var item = ParameterItems.FirstOrDefault(p => p.Name == name);
            if (item != null)
                ParameterItems.Remove(item);
        }

        /// <summary>
        /// 获取参数项
        /// </summary>
        protected ParameterItem? GetParameterItem(string name)
        {
            return ParameterItems.FirstOrDefault(p => p.Name == name);
        }

        /// <summary>
        /// 从参数项构建参数字典
        /// </summary>
        protected virtual Dictionary<string, object?> BuildParameterDictionary()
        {
            return ParameterItems.ToDictionary(p => p.Name, p => p.Value);
        }

        /// <summary>
        /// 创建参数快照
        /// </summary>
        protected virtual void CreateSnapshot()
        {
            var snapshot = ParameterSnapshot.Create(BuildParameterDictionary(), $"快照 {_snapshots.Count + 1}");
            _snapshots.Add(snapshot);
            Snapshots.Add(snapshot);
            StatusMessage = $"已创建快照：{snapshot.SnapshotTime:HH:mm:ss}";
        }

        /// <summary>
        /// 恢复参数快照
        /// </summary>
        protected virtual void RestoreSnapshot(ParameterSnapshot snapshot)
        {
            if (snapshot == null)
                return;

            foreach (var (name, value) in snapshot.Parameters)
            {
                var item = GetParameterItem(name);
                if (item != null)
                {
                    try
                    {
                        item.Value = Convert.ChangeType(value, item.DataType);
                    }
                    catch
                    {
                        // 忽略转换错误
                    }
                }
            }

            StatusMessage = $"已恢复快照：{snapshot.SnapshotTime:HH:mm:ss}";
        }

        #endregion

        #region 参数验证

        /// <summary>
        /// 验证所有参数
        /// </summary>
        public virtual bool ValidateAllParameters()
        {
            var results = _validator.ValidateItems(ParameterItems);
            var errors = results.Where(r => !r.IsValid).ToList();

            if (errors.Any())
            {
                ValidationError = string.Join("; ", errors.Select(e => e.ErrorMessage));
                return false;
            }

            ValidationError = null;
            return true;
        }

        /// <summary>
        /// 验证参数并返回错误列表
        /// </summary>
        public virtual List<string> ValidateParameters()
        {
            var results = _validator.ValidateItems(ParameterItems);
            return results.Where(r => !r.IsValid)
                          .Select(r => r.ErrorMessage)
                          .ToList();
        }

        #endregion

        #region 异步执行

        /// <summary>
        /// 异步运行工具（子类实现）
        /// </summary>
        protected virtual async Task RunToolAsync(CancellationToken cancellationToken)
        {
            if (!ValidateAllParameters())
            {
                StatusMessage = "参数验证失败，无法执行";
                return;
            }

            IsBusy = true;
            ToolStatus = "运行中";
            StatusMessage = "正在执行工具...";
            Progress = 0;
            ProgressMessage = "开始执行";

            try
            {
                var startTime = DateTime.Now;

                // 执行工具逻辑（子类通过重写此方法或使用事件钩子）
                await ExecuteToolCoreAsync(cancellationToken);

                var duration = (DateTime.Now - startTime).TotalMilliseconds;
                ExecutionTime = $"{duration:F0} ms";

                Progress = 100;
                StatusMessage = "执行完成";
                ToolStatus = "就绪";
            }
            catch (OperationCanceledException)
            {
                StatusMessage = "执行已取消";
                ToolStatus = "已取消";
            }
            catch (Exception ex)
            {
                StatusMessage = $"执行失败: {ex.Message}";
                ToolStatus = "错误";
                throw;
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// 工具核心执行逻辑（子类重写）
        /// </summary>
        protected virtual async Task ExecuteToolCoreAsync(CancellationToken cancellationToken)
        {
            // 默认实现：调用原有的同步RunTool
            await Task.Run(() => RunTool(), cancellationToken);
        }

        /// <summary>
        /// 取消执行
        /// </summary>
        protected virtual void CancelExecution()
        {
            if (RunCommand is AsyncRelayCommand asyncCommand && asyncCommand.IsExecuting)
            {
                asyncCommand.Cancel();
                StatusMessage = "正在取消执行...";
            }
        }

        /// <summary>
        /// 报告进度
        /// </summary>
        protected void ReportProgress(double progress, string? message = null)
        {
            Progress = Math.Max(0, Math.Min(100, progress));
            if (message != null)
                ProgressMessage = message;
        }

        /// <summary>
        /// 报告进度增量
        /// </summary>
        protected void ReportProgressIncrement(double increment, string? message = null)
        {
            ReportProgress(Progress + increment, message);
        }

        #endregion

        #region 保存和加载

        /// <summary>
        /// 异步保存参数到文件
        /// </summary>
        protected virtual async Task SaveParametersAsync(string? filePath = null)
        {
            try
            {
                await Task.Run(() =>
                {
                    var path = filePath ?? GetDefaultConfigPath();
                    _repository.SaveToFile(path, BuildParameterDictionary());
                });

                StatusMessage = "参数已保存";
            }
            catch (Exception ex)
            {
                StatusMessage = $"保存失败: {ex.Message}";
                throw;
            }
        }

        /// <summary>
        /// 异步从文件加载参数
        /// </summary>
        protected virtual async Task LoadParametersAsync(string? filePath = null)
        {
            try
            {
                await Task.Run(() =>
                {
                    var path = filePath ?? GetDefaultConfigPath();
                    var parameters = _repository.LoadFromFile(path);
                    _repository.LoadItemsFromFile(path, ParameterItems);
                });

                StatusMessage = "参数已加载";
            }
            catch (Exception ex)
            {
                StatusMessage = $"加载失败: {ex.Message}";
                throw;
            }
        }

        /// <summary>
        /// 获取默认配置文件路径
        /// </summary>
        protected virtual string GetDefaultConfigPath()
        {
            var configDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SunEyeVision",
                "ToolConfigs");

            Directory.CreateDirectory(configDir);

            return Path.Combine(configDir, $"{ToolId}_config.json");
        }

        #endregion

        #region 错误处理

        /// <summary>
        /// 执行错误处理
        /// </summary>
        protected virtual void OnExecutionError(Exception ex)
        {
            StatusMessage = $"执行出错: {ex.Message}";
            ToolStatus = "错误";
        }

        #endregion

        #region 重写和增强方法

        /// <summary>
        /// 重置参数
        /// </summary>
        public override void ResetParameters()
        {
            foreach (var item in ParameterItems)
            {
                item.Reset();
            }

            Progress = 0;
            ProgressMessage = "";
            ValidationError = null;

            base.ResetParameters();
        }

        #endregion
    }
}
