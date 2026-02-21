using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using SunEyeVision.UI.Models;

namespace SunEyeVision.UI.Commands
{
    /// <summary>
    /// 添加节点命令
    /// </summary>
    public class AddNodeCommand : ICommand
    {
        private readonly ObservableCollection<WorkflowNode> _nodes;
        private readonly WorkflowNode _node;

        public AddNodeCommand(ObservableCollection<WorkflowNode> nodes, WorkflowNode node)
        {
            _nodes = nodes;
            _node = node;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            _nodes.Add(_node);
        }
    }
}
