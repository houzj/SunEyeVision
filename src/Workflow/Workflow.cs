using System.Collections.Generic;
using System;
using SunEyeVision.Plugin.SDK.Models;

namespace SunEyeVision.Workflow
{
    /// <summary>
    /// 工作流（执行流定义）
    /// </summary>
    /// <remarks>
    /// 定义检测任务的执行逻辑,包括节点拓扑、连接关系和默认参数。
    /// 
    /// 特性：
    /// - 可复用：多个Recipe可以共享同一个Workflow
    /// - 可升级：修改Workflow不影响Recipe数据
    /// - 可版本化：支持多个版本共存
    /// </remarks>
    public class Workflow : ObservableObject
    {
        private string _name = "新建工作流";
        private string _description = "";

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
        /// 工作流描述
        /// </summary>
        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value, "描述");
        }

        /// <summary>
        /// 工作流版本
        /// </summary>
        public string Version { get; set; } = "1.0";

        /// <summary>
        /// 节点列表
        /// </summary>
        public List<WorkflowNode> Nodes { get; set; } = new();

        /// <summary>
        /// 连接列表
        /// </summary>
        public List<Connection> Connections { get; set; } = new();

        /// <summary>
        /// 节点默认参数（新建Recipe时的初始值）
        /// </summary>
        public Dictionary<string, ToolParameters> DefaultParams { get; set; } = new();

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 修改时间
        /// </summary>
        public DateTime ModifiedTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 克隆工作流
        /// </summary>
        public Workflow Clone()
        {
            var cloned = new Workflow
            {
                Id = Guid.NewGuid().ToString(),
                Name = $"{Name} (副本)",
                Description = Description,
                Version = Version,
                CreatedTime = CreatedTime,
                ModifiedTime = DateTime.Now
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

            // 克隆默认参数
            foreach (var param in DefaultParams)
            {
                cloned.DefaultParams[param.Key] = param.Value?.Clone();
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
        public WorkflowNode? GetNode(string nodeId)
        {
            return Nodes.FirstOrDefault(n => n.Id == nodeId);
        }

        /// <summary>
        /// 添加节点
        /// </summary>
        public void AddNode(WorkflowNode node)
        {
            Nodes.Add(node);
            ModifiedTime = DateTime.Now;
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

            // 移除相关连接
            Connections.RemoveAll(c => c.SourceNode == nodeId || c.TargetNode == nodeId);

            // 移除默认参数
            DefaultParams.Remove(nodeId);

            ModifiedTime = DateTime.Now;
            return true;
        }

        /// <summary>
        /// 添加连接
        /// </summary>
        public void AddConnection(Connection connection)
        {
            Connections.Add(connection);
            ModifiedTime = DateTime.Now;
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
            ModifiedTime = DateTime.Now;
            return true;
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
}
