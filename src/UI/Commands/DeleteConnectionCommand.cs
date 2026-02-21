using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using SunEyeVision.UI.Models;

namespace SunEyeVision.UI.Commands
{
    /// <summary>
    /// 删除连接命令
    /// </summary>
    public class DeleteConnectionCommand : ICommand
    {
        private readonly ObservableCollection<WorkflowConnection> _connections;
        private readonly WorkflowConnection _connection;

        public DeleteConnectionCommand(ObservableCollection<WorkflowConnection> connections, WorkflowConnection connection)
        {
            _connections = connections;
            _connection = connection;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            _connections.Remove(_connection);
        }
    }
}
