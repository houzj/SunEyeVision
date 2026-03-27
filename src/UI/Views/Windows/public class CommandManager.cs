public class CommandManager
{
    private readonly ObservableCollection<WorkflowNode> _nodes;
    private readonly ObservableCollection<WorkflowConnection> _connections;
    private readonly ObservableCollection<Connection>? _workflowConnections;  // 新增

    public CommandManager(
        ObservableCollection<WorkflowNode> nodes,
        ObservableCollection<WorkflowConnection> connections,
        ObservableCollection<Connection>? workflowConnections = null)
    {
        _nodes = nodes;
        _connections = connections;
        _workflowConnections = workflowConnections;
    }
}
