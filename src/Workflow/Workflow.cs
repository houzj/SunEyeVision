using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.SDK.Models;

namespace SunEyeVision.Workflow
{
/// <summary>
/// 工作流（执行流定义）
/// </summary>
/// <remarks>
/// 定义检测任务的执行逻辑,包括节点拓扑和连接关系。
///
/// 特性：
/// - 可复用：多个Solution可以共享同一个Workflow
/// - 可升级：修改Workflow不影响NodeParameters数据
/// - 可版本化：支持多个版本共存
/// </remarks>
public class Workflow : ObservableObject
{
    private string _name = "新建工作流";

    /// <summary>
    /// 工作流唯一标识符
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 工作流名称
    /// </summary>
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value, "工作流名称");
    }

    /// <summary>
    /// 节点列表（ObservableCollection 支持UI直接绑定）
    /// </summary>
    public ObservableCollection<WorkflowNodeBase> Nodes { get; set; } = new();

    /// <summary>
    /// 连接列表（ObservableCollection 支持UI直接绑定）
    /// </summary>
    public ObservableCollection<Connection> Connections { get; set; } = new();

    /// <summary>
    /// 节点添加事件
    /// </summary>
    public event EventHandler<WorkflowNodeEventArgs>? NodeAdded;

    /// <summary>
    /// 节点移除事件
    /// </summary>
    public event EventHandler<WorkflowNodeEventArgs>? NodeRemoved;

    /// <summary>
    /// 无参构造函数
    /// </summary>
    public Workflow()
    {
    }

    /// <summary>
    /// 两参数构造函数
    /// </summary>
    /// <param name="id">工作流ID</param>
    /// <param name="name">工作流名称</param>
    public Workflow(string id, string name)
    {
        Id = id;
        Name = name;
    }

    /// <summary>
    /// 克隆工作流
    /// </summary>
    public Workflow Clone()
    {
        var cloned = new Workflow
        {
            Id = Guid.NewGuid().ToString(),
            Name = $"{Name} (副本)"
        };

        // 克隆节点
        foreach (var node in Nodes)
        {
            cloned.Nodes.Add(node.Clone());
        }

        // 克隆连接
        foreach (var connection in Connections)
        {
            cloned.Connections.Add(new Connection
            {
                Id = Guid.NewGuid().ToString(),
                SourceNode = connection.SourceNode,
                SourcePort = connection.SourcePort,
                TargetNode = connection.TargetNode,
                TargetPort = connection.TargetPort
            });
        }

        return cloned;
    }

        /// <summary>
        /// 验证工作流
        /// </summary>
        public (bool IsValid, List<string> Errors) Validate()
        {
            var errors = new List<string>();

            // 检查是否有节点
            if (Nodes.Count == 0)
            {
                errors.Add("工作流没有节点");
            }

            // 检查节点ID唯一性
            var nodeIds = new HashSet<string>();
            foreach (var node in Nodes)
            {
                if (string.IsNullOrEmpty(node.Id))
                {
                    errors.Add($"节点 {node.Name} 没有ID");
                }
                else if (!nodeIds.Add(node.Id))
                {
                    errors.Add($"节点ID重复: {node.Id}");
                }
            }

            // 检查连接的有效性
            var nodeIdsSet = new HashSet<string>(Nodes.Select(n => n.Id));
            foreach (var connection in Connections)
            {
                if (!nodeIdsSet.Contains(connection.SourceNode))
                {
                    errors.Add($"连接源节点不存在: {connection.SourceNode}");
                }

                if (!nodeIdsSet.Contains(connection.TargetNode))
                {
                    errors.Add($"连接目标节点不存在: {connection.TargetNode}");
                }
            }

            // 检查是否有孤立节点（既没有输入也没有输出）
            var connectedNodes = new HashSet<string>();
            foreach (var connection in Connections)
            {
                connectedNodes.Add(connection.SourceNode);
                connectedNodes.Add(connection.TargetNode);
            }

            var isolatedNodes = Nodes
                .Where(n => !connectedNodes.Contains(n.Id))
                .Select(n => n.Name)
                .ToList();

            if (isolatedNodes.Count > 0)
            {
                errors.Add($"存在孤立节点: {string.Join(", ", isolatedNodes)}");
            }

            // 检查循环依赖
            if (HasCycle())
            {
                errors.Add("工作流存在循环依赖");
            }

            return (errors.Count == 0, errors);
        }

        /// <summary>
        /// 检查是否存在循环依赖
        /// </summary>
        private bool HasCycle()
        {
            var graph = new Dictionary<string, List<string>>();
            foreach (var node in Nodes)
            {
                graph[node.Id] = new List<string>();
            }

            foreach (var connection in Connections)
            {
                graph[connection.SourceNode].Add(connection.TargetNode);
            }

            var visited = new HashSet<string>();
            var recursionStack = new HashSet<string>();

            foreach (var node in Nodes)
            {
                if (HasCycleUtil(node.Id, graph, visited, recursionStack))
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasCycleUtil(
            string nodeId,
            Dictionary<string, List<string>> graph,
            HashSet<string> visited,
            HashSet<string> recursionStack)
        {
            if (recursionStack.Contains(nodeId))
            {
                return true;
            }

            if (visited.Contains(nodeId))
            {
                return false;
            }

            visited.Add(nodeId);
            recursionStack.Add(nodeId);

            foreach (var neighbor in graph.GetValueOrDefault(nodeId, new List<string>()))
            {
                if (HasCycleUtil(neighbor, graph, visited, recursionStack))
                {
                    return true;
                }
            }

            recursionStack.Remove(nodeId);
            return false;
        }

        /// <summary>
        /// 获取节点
        /// </summary>
        public WorkflowNodeBase? GetNode(string nodeId)
        {
            return Nodes.FirstOrDefault(n => n.Id == nodeId);
        }

    /// <summary>
    /// 添加节点
    /// </summary>
    public void AddNode(WorkflowNodeBase node)
    {
        Nodes.Add(node);

        // 触发节点添加事件
        NodeAdded?.Invoke(this, new WorkflowNodeEventArgs { Node = node, Workflow = this });
    }

    /// <summary>
    /// 移除节点
    /// </summary>
    public bool RemoveNode(string nodeId)
    {
        var node = GetNode(nodeId);
        if (node == null)
            return false;

        Nodes.Remove(node);

        // 移除相关连接（ObservableCollection 不支持 RemoveAll，使用循环删除）
        for (int i = Connections.Count - 1; i >= 0; i--)
        {
            if (Connections[i].SourceNode == nodeId || Connections[i].TargetNode == nodeId)
            {
                Connections.RemoveAt(i);
            }
        }

        // 触发节点移除事件
        NodeRemoved?.Invoke(this, new WorkflowNodeEventArgs { Node = node, Workflow = this });

        return true;
    }

    /// <summary>
    /// 添加连接
    /// </summary>
    public void AddConnection(Connection connection)
    {
        Connections.Add(connection);
    }

    /// <summary>
    /// 连接两个节点
    /// </summary>
    /// <param name="sourceNodeId">源节点ID</param>
    /// <param name="targetNodeId">目标节点ID</param>
    /// <returns>创建的连接</returns>
    public Connection ConnectNodes(string sourceNodeId, string targetNodeId)
    {
        var connection = new Connection
        {
            Id = Guid.NewGuid().ToString(),
            SourceNode = sourceNodeId,
            TargetNode = targetNodeId,
            SourcePort = "output",
            TargetPort = "input"
        };
        Connections.Add(connection);
        return connection;
    }

    /// <summary>
    /// 移除连接
    /// </summary>
    public bool RemoveConnection(string connectionId)
    {
        var connection = Connections.FirstOrDefault(c => c.Id == connectionId);
        if (connection == null)
            return false;

        Connections.Remove(connection);
        return true;
    }

        /// <summary>
        /// 检测循环依赖
        /// </summary>
        public List<string> DetectCycles()
        {
            var cycles = new List<string>();
            var graph = new Dictionary<string, List<string>>();

            // 构建邻接表
            foreach (var node in Nodes)
            {
                graph[node.Id] = new List<string>();
            }

            foreach (var connection in Connections)
            {
                graph[connection.SourceNode].Add(connection.TargetNode);
            }

            var visited = new HashSet<string>();
            var recursionStack = new HashSet<string>();
            var path = new List<string>();

            foreach (var node in Nodes)
            {
                if (FindCycles(node.Id, graph, visited, recursionStack, path, cycles))
                {
                    break;
                }
            }

            return cycles;
        }

        private bool FindCycles(
            string nodeId,
            Dictionary<string, List<string>> graph,
            HashSet<string> visited,
            HashSet<string> recursionStack,
            List<string> path,
            List<string> cycles)
        {
            if (recursionStack.Contains(nodeId))
            {
                // 找到循环，记录循环路径
                var cycleIndex = path.IndexOf(nodeId);
                if (cycleIndex >= 0)
                {
                    var cyclePath = path.Skip(cycleIndex).Concat(new[] { nodeId });
                    cycles.Add(string.Join(" -> ", cyclePath));
                }
                return true;
            }

            if (visited.Contains(nodeId))
            {
                return false;
            }

            visited.Add(nodeId);
            recursionStack.Add(nodeId);
            path.Add(nodeId);

            foreach (var neighbor in graph.GetValueOrDefault(nodeId, new List<string>()))
            {
                if (FindCycles(neighbor, graph, visited, recursionStack, path, cycles))
                {
                    recursionStack.Remove(nodeId);
                    path.RemoveAt(path.Count - 1);
                    return true;
                }
            }

            recursionStack.Remove(nodeId);
            path.RemoveAt(path.Count - 1);
            return false;
        }

        /// <summary>
        /// 获取并行执行组（基于执行链识别）
        /// </summary>
        public List<List<string>> GetParallelExecutionGroupsByChains()
        {
            var groups = new List<List<string>>();
            var visited = new HashSet<string>();
            var inDegree = new Dictionary<string, int>();

            // 计算入度
            foreach (var node in Nodes)
            {
                inDegree[node.Id] = 0;
            }

            foreach (var connection in Connections)
            {
                inDegree[connection.TargetNode]++;
            }

            // 使用队列进行拓扑排序
            var queue = new Queue<string>();
            foreach (var node in Nodes)
            {
                if (inDegree[node.Id] == 0)
                {
                    queue.Enqueue(node.Id);
                }
            }

            while (queue.Count > 0)
            {
                // 处理当前层（可以并行执行的节点）
                var currentGroup = new List<string>();
                var currentLevelSize = queue.Count;

                for (int i = 0; i < currentLevelSize; i++)
                {
                    var nodeId = queue.Dequeue();
                    currentGroup.Add(nodeId);
                    visited.Add(nodeId);

                    // 更新相邻节点的入度
                    foreach (var connection in Connections.Where(c => c.SourceNode == nodeId))
                    {
                        inDegree[connection.TargetNode]--;
                        if (inDegree[connection.TargetNode] == 0)
                        {
                            queue.Enqueue(connection.TargetNode);
                        }
                    }
                }

                groups.Add(currentGroup);
            }

            return groups;
        }
    }

    /// <summary>
    /// 连接类
    /// </summary>
    public class Connection
    {
        /// <summary>
        /// 连接唯一标识符
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 源节点ID
        /// </summary>
        public string SourceNode { get; set; } = "";

        /// <summary>
        /// 源端口名称
        /// </summary>
        public string SourcePort { get; set; } = "";

        /// <summary>
        /// 目标节点ID
        /// </summary>
        public string TargetNode { get; set; } = "";

        /// <summary>
        /// 目标端口名称
        /// </summary>
        public string TargetPort { get; set; } = "";
    }

    /// <summary>
    /// 工作流节点事件参数
    /// </summary>
    public class WorkflowNodeEventArgs : EventArgs
    {
        /// <summary>
        /// 节点
        /// </summary>
        public WorkflowNodeBase? Node { get; set; }

        /// <summary>
        /// 所属工作流
        /// </summary>
        public Workflow? Workflow { get; set; }
    }
}
