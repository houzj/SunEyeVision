using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using SunEyeVision.Plugin.Abstractions;

namespace SunEyeVision.Plugin.Infrastructure.Commands
{
    /// <summary>
    /// 复合命令，将多个命令组合成一个命令
    /// 当执行复合命令时，所有子命令都会被执行
    /// 只有当所有子命令都可执行时，复合命令才可执行
    /// </summary>
    public class CompositeCommand : ICommand
    {
        private readonly List<ICommand> _commands;
        private readonly bool _executeAllCommands;
        private readonly Func<bool>? _customCanExecute;

        /// <summary>
        /// 命令可执行状态改变事件
        /// </summary>
        public event EventHandler? CanExecuteChanged;

        /// <summary>
        /// 子命令集合
        /// </summary>
        public IReadOnlyList<ICommand> Commands => _commands.AsReadOnly();

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="executeAllCommands">是否执行所有子命令，否则只执行第一个可执行的命令</param>
        public CompositeCommand(bool executeAllCommands = true)
        {
            _commands = new List<ICommand>();
            _executeAllCommands = executeAllCommands;
        }

        /// <summary>
        /// 构造函数，带自定义的可执行条件
        /// </summary>
        /// <param name="customCanExecute">自定义的可执行条件</param>
        /// <param name="executeAllCommands">是否执行所有子命令</param>
        public CompositeCommand(Func<bool> customCanExecute, bool executeAllCommands = true)
            : this(executeAllCommands)
        {
            _customCanExecute = customCanExecute;
        }

        /// <summary>
        /// 添加子命令
        /// </summary>
        public void RegisterCommand(ICommand command)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            _commands.Add(command);
            command.CanExecuteChanged += (s, e) => RaiseCanExecuteChanged();
            RaiseCanExecuteChanged();
        }

        /// <summary>
        /// 移除子命令
        /// </summary>
        public void UnregisterCommand(ICommand command)
        {
            if (command != null && _commands.Remove(command))
            {
                command.CanExecuteChanged -= (s, e) => RaiseCanExecuteChanged();
                RaiseCanExecuteChanged();
            }
        }

        /// <summary>
        /// 判断命令是否可执行
        /// </summary>
        public bool CanExecute(object? parameter)
        {
            if (_customCanExecute != null)
            {
                return _customCanExecute();
            }

            if (_commands.Count == 0)
                return false;

            if (_executeAllCommands)
            {
                return _commands.All(cmd => cmd.CanExecute(parameter));
            }
            else
            {
                return _commands.Any(cmd => cmd.CanExecute(parameter));
            }
        }

        /// <summary>
        /// 执行命令
        /// </summary>
        public void Execute(object? parameter)
        {
            if (_executeAllCommands)
            {
                foreach (var command in _commands.Where(cmd => cmd.CanExecute(parameter)))
                {
                    command.Execute(parameter);
                }
            }
            else
            {
                var firstExecutable = _commands.FirstOrDefault(cmd => cmd.CanExecute(parameter));
                firstExecutable?.Execute(parameter);
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
    /// 异步复合命令，支持异步操作的复合命令
    /// </summary>
    public class AsyncCompositeCommand : AsyncRelayCommand
    {
        private readonly List<AsyncRelayCommand> _asyncCommands;
        private readonly bool _executeAllCommands;

        /// <summary>
        /// 子命令集合
        /// </summary>
        public IReadOnlyList<AsyncRelayCommand> AsyncCommands => _asyncCommands.AsReadOnly();

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="executeAllCommands">是否执行所有子命令</param>
        public AsyncCompositeCommand(bool executeAllCommands = true)
            : base(async (param, ct) => await Task.CompletedTask)
        {
            _asyncCommands = new List<AsyncRelayCommand>();
            _executeAllCommands = executeAllCommands;
        }

        /// <summary>
        /// 添加子命令
        /// </summary>
        public void RegisterCommand(AsyncRelayCommand command)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            _asyncCommands.Add(command);
            command.CanExecuteChanged += (s, e) => RaiseCanExecuteChanged();
            RaiseCanExecuteChanged();
        }

        /// <summary>
        /// 移除子命令
        /// </summary>
        public void UnregisterCommand(AsyncRelayCommand command)
        {
            if (command != null && _asyncCommands.Remove(command))
            {
                command.CanExecuteChanged -= (s, e) => RaiseCanExecuteChanged();
                RaiseCanExecuteChanged();
            }
        }

        /// <summary>
        /// 重写执行方法，执行所有子命令
        /// </summary>
        public override async void Execute(object? parameter)
        {
            var tasks = new List<Task>();

            if (_executeAllCommands)
            {
                foreach (var command in _asyncCommands.Where(cmd => cmd.CanExecute(parameter)))
                {
                    var task = Task.Run(async () =>
                    {
                        try
                        {
                            await Task.Run(() => command.Execute(parameter), CancellationToken);
                        }
                        catch { }
                    }, CancellationToken);
                    tasks.Add(task);
                }
            }
            else
            {
                var firstExecutable = _asyncCommands.FirstOrDefault(cmd => cmd.CanExecute(parameter));
                if (firstExecutable != null)
                {
                    var task = Task.Run(() => firstExecutable.Execute(parameter), CancellationToken);
                    tasks.Add(task);
                }
            }

            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// 判断命令是否可执行
        /// </summary>
        public new bool CanExecute(object? parameter)
        {
            if (_asyncCommands.Count == 0)
                return false;

            if (_executeAllCommands)
            {
                return _asyncCommands.All(cmd => cmd.CanExecute(parameter));
            }
            else
            {
                return _asyncCommands.Any(cmd => cmd.CanExecute(parameter));
            }
        }
    }
}
