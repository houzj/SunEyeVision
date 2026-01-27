using System.Windows;
using System.Windows.Input;
using SunEyeVision.UI.Models;

namespace SunEyeVision.UI.Commands
{
    public class MoveNodeCommand : IUndoRedoCommand
    {
        private readonly WorkflowNode _node;
        private readonly Point _newPosition;
        private readonly Point _oldPosition;

        public MoveNodeCommand(WorkflowNode node, Point newPosition)
        {
            _node = node;
            _newPosition = newPosition;
            _oldPosition = node.Position;
        }

        public string Description => $"移动节点 {_node.Name}";

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter)
        {
            _node.Position = _newPosition;
        }

        public void Undo()
        {
            _node.Position = _oldPosition;
        }

        public void Redo()
        {
            _node.Position = _newPosition;
        }
    }
}
