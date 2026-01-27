using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using SunEyeVision.UI.Models;

namespace SunEyeVision.UI.Commands
{
    public class BatchMoveNodesCommand : IUndoRedoCommand
    {
        private readonly List<(WorkflowNode node, Point oldPosition, Point newPosition)> _nodeMoves = new List<(WorkflowNode, Point, Point)>();

        public BatchMoveNodesCommand(ObservableCollection<WorkflowNode> nodes, List<Point> offsets)
        {
            for (int i = 0; i < nodes.Count && i < offsets.Count; i++)
            {
                var node = nodes[i];
                var offset = offsets[i];
                var newPosition = new Point(node.Position.X + offset.X, node.Position.Y + offset.Y);
                _nodeMoves.Add((node, node.Position, newPosition));
            }
        }

        public string Description => $"移动 {_nodeMoves.Count} 个节点";

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => _nodeMoves.Count > 0;

        public void Execute(object? parameter)
        {
            foreach (var (node, oldPosition, newPosition) in _nodeMoves)
            {
                node.Position = newPosition;
            }
        }

        public void Undo()
        {
            foreach (var (node, oldPosition, newPosition) in _nodeMoves)
            {
                node.Position = oldPosition;
            }
        }

        public void Redo()
        {
            Execute(null);
        }
    }
}
