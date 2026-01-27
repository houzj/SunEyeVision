using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using SunEyeVision.UI.Models;

namespace SunEyeVision.UI.Commands
{
    public class BatchDeleteNodesCommand : IUndoRedoCommand
    {
        private readonly ObservableCollection<WorkflowNode> _nodes;
        private readonly ObservableCollection<WorkflowConnection> _connections;
        private readonly List<WorkflowNode> _deletedNodes = new List<WorkflowNode>();
        private readonly List<WorkflowConnection> _deletedConnections = new List<WorkflowConnection>();

        public BatchDeleteNodesCommand(
            ObservableCollection<WorkflowNode> nodes,
            ObservableCollection<WorkflowConnection> connections)
        {
            _nodes = nodes;
            _connections = connections;
        }

        public string Description => $"批量删除 {_deletedNodes.Count} 个节点";

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => _nodes.Any(n => n.IsSelected);

        public void Execute(object? parameter)
        {
            _deletedNodes.Clear();
            _deletedConnections.Clear();

            // 查找所有选中的节点
            var selectedNodes = _nodes.Where(n => n.IsSelected).ToList();

            // 删除与选中节点相关的所有连接
            var relatedConnections = _connections
                .Where(c => selectedNodes.Any(n => n.Id == c.SourceNodeId || n.Id == c.TargetNodeId))
                .ToList();

            foreach (var conn in relatedConnections)
            {
                _connections.Remove(conn);
                _deletedConnections.Add(conn);
            }

            // 删除选中的节点
            foreach (var node in selectedNodes)
            {
                _nodes.Remove(node);
                _deletedNodes.Add(node);
            }
        }

        public void Undo()
        {
            // 恢复节点
            foreach (var node in _deletedNodes)
            {
                _nodes.Add(node);
            }

            // 恢复连接
            foreach (var conn in _deletedConnections)
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
