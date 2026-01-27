using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using SunEyeVision.UI.Models;

namespace SunEyeVision.UI.Commands
{
    public class CommandManager
    {
        private readonly Stack<IUndoRedoCommand> _undoStack = new Stack<IUndoRedoCommand>();
        private readonly Stack<IUndoRedoCommand> _redoStack = new Stack<IUndoRedoCommand>();
        private readonly ObservableCollection<WorkflowNode> _nodes;
        private readonly ObservableCollection<WorkflowConnection> _connections;

        public event EventHandler? CommandStateChanged;

        public CommandManager(
            ObservableCollection<WorkflowNode> nodes,
            ObservableCollection<WorkflowConnection> connections)
        {
            _nodes = nodes;
            _connections = connections;
        }

        public string LastCommandDescription { get; private set; } = "";

        public void Execute(ICommand command)
        {
            if (command is IUndoRedoCommand undoRedoCommand)
            {
                undoRedoCommand.Execute(null);
                _undoStack.Push(undoRedoCommand);
                _redoStack.Clear();
                LastCommandDescription = undoRedoCommand.Description;
                CommandStateChanged?.Invoke(this, EventArgs.Empty);
            }
            else if (command.CanExecute(null))
            {
                command.Execute(null);
            }
        }

        public void Undo()
        {
            if (_undoStack.Count > 0)
            {
                var command = _undoStack.Pop();
                command.Undo();
                _redoStack.Push(command);
                LastCommandDescription = command.Description;
                CommandStateChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public void Redo()
        {
            if (_redoStack.Count > 0)
            {
                var command = _redoStack.Pop();
                command.Redo();
                _undoStack.Push(command);
                LastCommandDescription = command.Description;
                CommandStateChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public bool CanUndo => _undoStack.Count > 0;
        public bool CanRedo => _redoStack.Count > 0;
    }

    public interface IUndoRedoCommand : ICommand
    {
        void Undo();
        void Redo();
        string Description { get; }
    }
}
