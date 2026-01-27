using System.Collections.ObjectModel;
using System.Windows.Input;
using SunEyeVision.UI.Models;

namespace SunEyeVision.UI.Commands
{
    public class AddConnectionCommand : IUndoRedoCommand
    {
        private readonly ObservableCollection<WorkflowConnection> _connections;
        private readonly WorkflowConnection _connection;

        public AddConnectionCommand(ObservableCollection<WorkflowConnection> connections, WorkflowConnection connection)
        {
            _connections = connections;
            _connection = connection;
        }

        public string Description => $"添加连接 {_connection.SourceNodeId} -> {_connection.TargetNodeId}";

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter)
        {
            _connections.Add(_connection);
        }

        public void Undo()
        {
            _connections.Remove(_connection);
        }

        public void Redo()
        {
            _connections.Add(_connection);
        }
    }
}
