using System;
using System.Windows.Input;

namespace SunEyeVision.PluginSystem.Commands
{
    /// <summary>
    /// 参数变更命令，用于处理参数值变化时的业务逻辑
    /// 支持参数验证、自动计算、依赖更新等场景
    /// </summary>
    public class ParameterChangedCommand : ICommand
    {
        private readonly Action<object?, string?, object?> _execute;
        private readonly Func<object?, string?, object?, bool>? _canExecute;
        private readonly Action<Exception>? _onError;

        /// <summary>
        /// 命令可执行状态改变事件
        /// </summary>
        public event EventHandler? CanExecuteChanged;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="execute">参数变更执行委托</param>
        /// <param name="canExecute">可执行条件委托（可选）</param>
        /// <param name="onError">错误处理委托（可选）</param>
        /// <param name="alwaysCanExecute">是否总是可执行</param>
        public ParameterChangedCommand(
            Action<object?, string?, object?> execute,
            Func<object?, string?, object?, bool>? canExecute = null,
            Action<Exception>? onError = null,
            bool alwaysCanExecute = false)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = alwaysCanExecute ? null : canExecute;
            _onError = onError;
        }

        /// <summary>
        /// 判断命令是否可执行
        /// </summary>
        public bool CanExecute(object? parameter)
        {
            if (parameter is ParameterChangedEventArgs args)
            {
                return _canExecute == null || _canExecute(args.ParameterOwner, args.ParameterName, args.NewValue);
            }
            return _canExecute == null;
        }

        /// <summary>
        /// 执行命令
        /// </summary>
        public void Execute(object? parameter)
        {
            try
            {
                if (parameter is ParameterChangedEventArgs args)
                {
                    _execute(args.ParameterOwner, args.ParameterName, args.NewValue);
                }
                else
                {
                    _execute(null, null, parameter);
                }
            }
            catch (Exception ex)
            {
                _onError?.Invoke(ex);
            }
        }

        /// <summary>
        /// 触发CanExecuteChanged事件
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// 参数变更事件参数
    /// </summary>
    public class ParameterChangedEventArgs
    {
        /// <summary>
        /// 参数所属对象
        /// </summary>
        public object? ParameterOwner { get; }

        /// <summary>
        /// 参数名称
        /// </summary>
        public string? ParameterName { get; }

        /// <summary>
        /// 新值
        /// </summary>
        public object? NewValue { get; }

        /// <summary>
        /// 旧值
        /// </summary>
        public object? OldValue { get; }

        /// <summary>
        /// 构造函数
        /// </summary>
        public ParameterChangedEventArgs(object? owner, string? name, object? newValue, object? oldValue = null)
        {
            ParameterOwner = owner;
            ParameterName = name;
            NewValue = newValue;
            OldValue = oldValue;
        }
    }

    /// <summary>
    /// 泛型版本的参数变更命令
    /// </summary>
    /// <typeparam name="TOwner">参数所属类型</typeparam>
    /// <typeparam name="TValue">参数值类型</typeparam>
    public class ParameterChangedCommand<TOwner, TValue> : ICommand
    {
        private readonly Action<TOwner?, string?, TValue?> _execute;
        private readonly Func<TOwner?, string?, TValue?, bool>? _canExecute;
        private readonly Action<Exception>? _onError;

        /// <summary>
        /// 命令可执行状态改变事件
        /// </summary>
        public event EventHandler? CanExecuteChanged;

        /// <summary>
        /// 构造函数
        /// </summary>
        public ParameterChangedCommand(
            Action<TOwner?, string?, TValue?> execute,
            Func<TOwner?, string?, TValue?, bool>? canExecute = null,
            Action<Exception>? onError = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
            _onError = onError;
        }

        /// <summary>
        /// 判断命令是否可执行
        /// </summary>
        public bool CanExecute(object? parameter)
        {
            if (parameter is ParameterChangedEventArgs<TValue> args)
            {
                if (args.ParameterOwner is TOwner owner)
                {
                    return _canExecute == null || _canExecute(owner, args.ParameterName, args.NewValue);
                }
            }
            return _canExecute == null;
        }

        /// <summary>
        /// 执行命令
        /// </summary>
        public void Execute(object? parameter)
        {
            try
            {
                if (parameter is ParameterChangedEventArgs<TValue> args)
                {
                    if (args.ParameterOwner is TOwner owner)
                    {
                        _execute(owner, args.ParameterName, args.NewValue);
                    }
                }
            }
            catch (Exception ex)
            {
                _onError?.Invoke(ex);
            }
        }

        /// <summary>
        /// 触发CanExecuteChanged事件
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// 泛型版本的参数变更事件参数
    /// </summary>
    /// <typeparam name="TValue">参数值类型</typeparam>
    public class ParameterChangedEventArgs<TValue> : ParameterChangedEventArgs
    {
        /// <summary>
        /// 强类型的新值
        /// </summary>
        public new TValue? NewValue { get; }

        /// <summary>
        /// 强类型的旧值
        /// </summary>
        public new TValue? OldValue { get; }

        /// <summary>
        /// 构造函数
        /// </summary>
        public ParameterChangedEventArgs(object? owner, string? name, TValue? newValue, TValue? oldValue = default)
            : base(owner, name, newValue, oldValue)
        {
            NewValue = newValue;
            OldValue = oldValue;
        }
    }
}
