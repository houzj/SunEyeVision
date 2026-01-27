using System.Collections.ObjectModel;
using System.Windows.Input;
using SunEyeVision.UI.Models;

namespace SunEyeVision.UI.Commands
{
    public class DeleteConnectionCommand : IUndoRedoCommand
    {
        private readonly ObservableCollection<WorkflowConnection> _connections;
        private readonly WorkflowConnection _connection;

        public DeleteConnectionCommand(ObservableCollection<WorkflowConnection> connections, WorkflowConnection connection)
        {
            _connections = connections;
            _connection = connection;
        }

        public string Description => $"删除连接 {_connection.SourceNodeId} -> {_connection.TargetNodeId}";

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter)
        {
            _connections.Remove(_connection);
        }

        public void Undo()
        {
            _connections.Add(_connection);
        }

        public void Redo()
        {
            _connections.Remove(_connection);
        }
    }
}
