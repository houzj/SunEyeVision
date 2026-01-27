using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using SunEyeVision.UI.Models;

namespace SunEyeVision.UI.Commands
{
    public class DeleteNodeCommand : IUndoRedoCommand
    {
        private readonly ObservableCollection<WorkflowNode> _nodes;
        private readonly ObservableCollection<WorkflowConnection> _connections;
        private readonly WorkflowNode _node;
        private readonly List<WorkflowConnection> _removedConnections = new List<WorkflowConnection>();

        public DeleteNodeCommand(
            ObservableCollection<WorkflowNode> nodes,
            ObservableCollection<WorkflowConnection> connections,
            WorkflowNode node)
        {
            _nodes = nodes;
            _connections = connections;
            _node = node;
        }

        public string Description => $"删除节点 {_node.Name}";

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter)
        {
            // 删除与该节点相关的所有连接
            _removedConnections.Clear();
            var connectionsToRemove = _connections
                .Where(c => c.SourceNodeId == _node.Id || c.TargetNodeId == _node.Id)
                .ToList();
            
            foreach (var conn in connectionsToRemove)
            {
                _connections.Remove(conn);
                _removedConnections.Add(conn);
            }

            _nodes.Remove(_node);
        }

        public void Undo()
        {
            _nodes.Add(_node);
            foreach (var conn in _removedConnections)
            {
                _connections.Add(conn);
            }
        }

        public void Redo()
        {
            Execute(null);
        }
    }
}
