using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using SunEyeVision.UI.Models;

namespace SunEyeVision.UI.Commands
{
    /// <summary>
    /// 批量删除节点命令
    /// </summary>
    public class BatchDeleteNodesCommand : ICommand
    {
        private readonly ObservableCollection<WorkflowNode> _nodes;
        private readonly ObservableCollection<WorkflowConnection> _connections;
        private readonly List<WorkflowNode> _deletedNodes;
        private readonly List<WorkflowConnection> _deletedConnections;

        public BatchDeleteNodesCommand(ObservableCollection<WorkflowNode> nodes, ObservableCollection<WorkflowConnection> connections, List<WorkflowNode> nodesToDelete)
        {
            _nodes = nodes;
            _connections = connections;
            _deletedNodes = new List<WorkflowNode>(nodesToDelete);
            _deletedConnections = new List<WorkflowConnection>();

            // 收集需要删除的连接
            foreach (var node in nodesToDelete)
            {
                var relatedConnections = _connections.Where(c => c.SourceNodeId == node.Id || c.TargetNodeId == node.Id).ToList();
                _deletedConnections.AddRange(relatedConnections);
            }
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
            foreach (var node in _deletedNodes)
            {
                _nodes.Remove(node);
            }
        }
    }
}
