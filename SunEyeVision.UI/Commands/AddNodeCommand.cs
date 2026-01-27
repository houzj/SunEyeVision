using System.Collections.ObjectModel;
using System.Windows.Input;
using SunEyeVision.UI.Models;

namespace SunEyeVision.UI.Commands
{
    public class AddNodeCommand : IUndoRedoCommand
    {
        private readonly ObservableCollection<WorkflowNode> _nodes;
        private readonly WorkflowNode _node;

        public AddNodeCommand(ObservableCollection<WorkflowNode> nodes, WorkflowNode node)
        {
            _nodes = nodes;
            _node = node;
        }

        public string Description => $"添加节点 {_node.Name}";

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter)
        {
            _nodes.Add(_node);
        }

        public void Undo()
        {
            _nodes.Remove(_node);
        }

        public void Redo()
        {
            _nodes.Add(_node);
        }
    }
}
