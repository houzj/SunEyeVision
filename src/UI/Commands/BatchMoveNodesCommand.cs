using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using SunEyeVision.UI.Models;

namespace SunEyeVision.UI.Commands
{
    /// <summary>
    /// 批量移动节点命令
    /// </summary>
    public class BatchMoveNodesCommand : ICommand
    {
        private readonly List<WorkflowNode> _nodes;
        private readonly Vector _delta;

        public BatchMoveNodesCommand(List<WorkflowNode> nodes, Vector delta)
        {
            _nodes = nodes;
            _delta = delta;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            foreach (var node in _nodes)
            {
                node.Position = new Point(node.Position.X + _delta.X, node.Position.Y + _delta.Y);
            }
        }
    }
}
