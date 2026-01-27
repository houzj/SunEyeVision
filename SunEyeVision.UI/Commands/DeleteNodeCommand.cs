using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using SunEyeVision.UI.Models;

namespace SunEyeVision.UI.Commands
{
    /// <summary>
    /// 删除节点命令
    /// </summary>
    public class DeleteNodeCommand : ICommand
    {
        private readonly ObservableCollection<WorkflowNode> _nodes;
        private readonly ObservableCollection<WorkflowConnection> _connections;
        private readonly WorkflowNode _node;
        private readonly System.Collections.Generic.List<WorkflowConnection> _deletedConnections;

        public DeleteNodeCommand(ObservableCollection<WorkflowNode> nodes, ObservableCollection<WorkflowConnection> connections, WorkflowNode node)
        {
            _nodes = nodes;
            _connections = connections;
            _node = node;
            _deletedConnections = new System.Collections.Generic.List<WorkflowConnection>();

            // 收集需要删除的连接
            var relatedConnections = _connections.Where(c => c.SourceNodeId == node.Id || c.TargetNodeId == node.Id).ToList();
            _deletedConnections.AddRange(relatedConnections);
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            // 删除连接
            foreach (var connection in _deletedConnections)
            {
                _connections.Remove(connection);
            }

            // 删除节点
            _nodes.Remove(_node);
        }
    }
}
