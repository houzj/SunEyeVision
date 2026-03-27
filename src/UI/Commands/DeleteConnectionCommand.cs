using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using SunEyeVision.Workflow;

namespace SunEyeVision.UI.Commands
{
    /// <summary>
    /// 删除连接命令
    /// </summary>
    public class DeleteConnectionCommand : IUndoableCommand
    {
        private readonly ObservableCollection<WorkflowConnection> _connections;
        private readonly WorkflowConnection _connection;
        private int _connectionIndex;

        public DeleteConnectionCommand(ObservableCollection<WorkflowConnection> connections, WorkflowConnection connection)
        {
            _connections = connections;
            _connection = connection;
            _connectionIndex = connections.IndexOf(connection);
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter)
        {
            return true;
        }

        public void Execute(object? parameter)
        {
            _connections.Remove(_connection);
        }

        public void Undo()
        {
            if (_connectionIndex >= 0 && _connectionIndex <= _connections.Count)
                _connections.Insert(_connectionIndex, _connection);
            else
                _connections.Add(_connection);
        }
    }
}
