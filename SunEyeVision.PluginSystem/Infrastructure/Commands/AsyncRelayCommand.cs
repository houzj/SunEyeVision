using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using SunEyeVision.PluginSystem.Base.Interfaces;
using SunEyeVision.PluginSystem.Base.Models;
using SunEyeVision.PluginSystem.Base.Base;

namespace SunEyeVision.PluginSystem.Commands
{
    /// <summary>
    /// 异步命令实现，支持异步操作且不阻塞UI线程
    /// 自动管理执行状态，防止重复执行
    /// </summary>
    public class AsyncRelayCommand : ICommand
    {
        private readonly Func<object?, CancellationToken, Task> _execute;
        private readonly Func<object?, bool>? _canExecute;
        private readonly Action<Exception>? _onError;
        private bool _isExecuting;
        private CancellationTokenSource? _cts;

        /// <summary>
        /// 命令可执行状态改变事件
        /// </summary>
        public event EventHandler? CanExecuteChanged;

        /// <summary>
        /// 当前是否正在执行
        /// </summary>
        public bool IsExecuting => _isExecuting;

        /// <summary>
        /// 取消令牌，用于取消异步操作
        /// </summary>
        public CancellationToken CancellationToken => _cts?.Token ?? CancellationToken.None;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="execute">异步执行委托</param>
        /// <param name="canExecute">可执行条件委托（可选）</param>
        /// <param name="onError">错误处理委托（可选）</param>
        public AsyncRelayCommand(
            Func<object?, CancellationToken, Task> execute,
            Func<object?, bool>? canExecute = null,
            Action<Exception>? onError = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
            _onError = onError;
        }

        /// <summary>
        /// 简化构造函数，不需要CancellationToken
        /// </summary>
        public AsyncRelayCommand(
            Func<object?, Task> execute,
            Func<object?, bool>? canExecute = null,
            Action<Exception>? onError = null)
        {
            _execute = (param, _) => execute(param);
            _canExecute = canExecute;
            _onError = onError;
        }

        /// <summary>
        /// 无参数的简化构造函数
        /// </summary>
        public AsyncRelayCommand(
            Func<CancellationToken, Task> execute,
            Func<bool>? canExecute = null,
            Action<Exception>? onError = null)
        {
            _execute = (_, ct) => execute(ct);
            _canExecute = canExecute != null ? _ => canExecute() : null;
            _onError = onError;
        }

        /// <summary>
        /// 判断命令是否可执行
        /// </summary>
        public bool CanExecute(object? parameter)
        {
            return !_isExecuting && (_canExecute?.Invoke(parameter) ?? true);
        }

        /// <summary>
        /// 执行异步命令
        /// </summary>
        public virtual async void Execute(object? parameter)
        {
            if (_isExecuting)
                return;

            _isExecuting = true;
            _cts = new CancellationTokenSource();
            RaiseCanExecuteChanged();

            try
            {
                await _execute(parameter, _cts.Token);
            }
            catch (OperationCanceledException)
            {
                // 取消操作是正常的，不需要处理
            }
            catch (Exception ex)
            {
                _onError?.Invoke(ex);
            }
            finally
            {
                _isExecuting = false;
                _cts?.Dispose();
                _cts = null;
                RaiseCanExecuteChanged();
            }
        }

        /// <summary>
        /// 取消当前正在执行的异步操作
        /// </summary>
        public void Cancel()
        {
            _cts?.Cancel();
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
    /// 泛型版本的异步命令，提供类型安全的命令参数
    /// </summary>
    /// <typeparam name="T">命令参数类型</typeparam>
    public class AsyncRelayCommand<T> : ICommand
    {
        private readonly Func<T?, CancellationToken, Task> _execute;
        private readonly Predicate<T?>? _canExecute;
        private readonly Action<Exception>? _onError;
        private bool _isExecuting;
        private CancellationTokenSource? _cts;

        /// <summary>
        /// 命令可执行状态改变事件
        /// </summary>
        public event EventHandler? CanExecuteChanged;

        /// <summary>
        /// 当前是否正在执行
        /// </summary>
        public bool IsExecuting => _isExecuting;

        /// <summary>
        /// 取消令牌
        /// </summary>
        public CancellationToken CancellationToken => _cts?.Token ?? CancellationToken.None;

        /// <summary>
        /// 构造函数
        /// </summary>
        public AsyncRelayCommand(
            Func<T?, CancellationToken, Task> execute,
            Predicate<T?>? canExecute = null,
            Action<Exception>? onError = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
            _onError = onError;
        }

        /// <summary>
        /// 简化构造函数
        /// </summary>
        public AsyncRelayCommand(
            Func<T?, Task> execute,
            Predicate<T?>? canExecute = null,
            Action<Exception>? onError = null)
        {
            _execute = (param, _) => execute(param);
            _canExecute = canExecute;
            _onError = onError;
        }

        /// <summary>
        /// 判断命令是否可执行
        /// </summary>
        public bool CanExecute(object? parameter)
        {
            if (_isExecuting)
                return false;

            if (parameter == null && typeof(T).IsValueType)
                return _canExecute == null;

            if (parameter == null || parameter is T)
                return _canExecute == null || _canExecute((T?)parameter);

            return false;
        }

        /// <summary>
        /// 执行异步命令
        /// </summary>
        public virtual async void Execute(object? parameter)
        {
            if (_isExecuting)
                return;

            _isExecuting = true;
            _cts = new CancellationTokenSource();
            RaiseCanExecuteChanged();

            try
            {
                await _execute((T?)parameter, _cts.Token);
            }
            catch (OperationCanceledException)
            {
                // 取消操作是正常的
            }
            catch (Exception ex)
            {
                _onError?.Invoke(ex);
            }
            finally
            {
                _isExecuting = false;
                _cts?.Dispose();
                _cts = null;
                RaiseCanExecuteChanged();
            }
        }

        /// <summary>
        /// 取消当前正在执行的异步操作
        /// </summary>
        public void Cancel()
        {
            _cts?.Cancel();
        }

        /// <summary>
        /// 触发CanExecuteChanged事件
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
