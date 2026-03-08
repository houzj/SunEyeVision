using System;
using System.Windows.Input;
using SunEyeVision.Core.Services.Logging;
using SunEyeVision.Plugin.SDK.Logging;

namespace SunEyeVision.UI.ViewModels
{
    /// <summary>
    /// RelayCommand实现,用于ViewModel中的命令绑定
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Func<object, bool> _canExecute;
        private readonly string _name;

        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null, string name = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
            _name = name ?? execute.Method.Name;
        }

        public RelayCommand(Action execute, Func<bool> canExecute = null, string name = null)
        {
            _execute = _ => execute();
            _canExecute = canExecute == null ? null : _ => canExecute();
            _name = name ?? execute.Method.Name;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute(parameter);
        }

        public void Execute(object parameter)
        {
            VisionLogger.Instance.Log(LogLevel.Info, $"RelayCommand.Execute 开始执行 - 命令: {_name}, 委托: {_execute.Method.Name}, 是否为空: {_execute == null}", "RelayCommand");

            // 尝试捕获并记录任何异常
            try
            {
                _execute(parameter);
                VisionLogger.Instance.Log(LogLevel.Success, $"RelayCommand.Execute 执行成功 - 命令: {_name}", "RelayCommand");
            }
            catch (Exception ex)
            {
                // 命令执行失败，记录到系统日志
                var logger = VisionLogger.Instance;
                logger.Log(LogLevel.Error, $"RelayCommand 执行失败 - 命令: {_name}, 错误: {ex.Message}", "RelayCommand", ex);
            }
        }

        public void RaiseCanExecuteChanged()
        {
            CommandManager.RequerySuggested += null;
        }
    }

    /// <summary>
    /// 通用RelayCommand,简化参数处理。
    /// </summary>
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Func<T, bool> _canExecute;

        public RelayCommand(Action<T> execute, Func<T, bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute((T)parameter);
        }

        public void Execute(object parameter)
        {
            _execute((T)parameter);
        }
    }
}
