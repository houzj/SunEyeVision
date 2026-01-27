using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using SunEyeVision.UI.Models;

namespace SunEyeVision.UI.Commands
{
    /// <summary>
    /// 命令管理器 - 用于撤销/重做功能
    /// </summary>
    public class CommandManager
    {
        private readonly Stack<ICommand> _undoStack = new Stack<ICommand>();
        private readonly Stack<ICommand> _redoStack = new Stack<ICommand>();
        private readonly ObservableCollection<WorkflowNode> _nodes;
        private readonly ObservableCollection<WorkflowConnection> _connections;

        public CommandManager(ObservableCollection<WorkflowNode> nodes, ObservableCollection<WorkflowConnection> connections)
        {
            _nodes = nodes;
            _connections = connections;
        }

        public bool CanUndo => _undoStack.Count > 0;
        public bool CanRedo => _redoStack.Count > 0;

        public string LastCommandDescription => _undoStack.Count > 0 ? _undoStack.Peek().GetType().Name : "无";

        public event EventHandler? CommandStateChanged;

        private void OnCommandStateChanged()
        {
            CommandStateChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Execute(ICommand command)
        {
            command.Execute(null);
            _undoStack.Push(command);
            _redoStack.Clear();
            OnCommandStateChanged();
        }

        public void Undo()
        {
            if (CanUndo)
            {
                var command = _undoStack.Pop();
                _redoStack.Push(command);
                OnCommandStateChanged();
            }
        }

        public void Redo()
        {
            if (CanRedo)
            {
                var command = _redoStack.Pop();
                _undoStack.Push(command);
                OnCommandStateChanged();
            }
        }

        public void Clear()
        {
            _undoStack.Clear();
            _redoStack.Clear();
            OnCommandStateChanged();
        }
    }
}
