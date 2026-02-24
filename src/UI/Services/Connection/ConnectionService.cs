using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using SunEyeVision.UI.Models;
using SunEyeVision.UI.Services.Canvas;
using SunEyeVision.UI.Services.Connection;
using SunEyeVision.UI.Services.Node;

namespace SunEyeVision.UI.Services.Connection
{
    /// <summary>
    /// 连接服务接口
    /// </summary>
    public interface IConnectionService
    {
        /// <summary>
        /// 连接集合
        /// </summary>
        ObservableCollection<WorkflowConnection> Connections { get; }

        /// <summary>
        /// 连接创建事件
        /// </summary>
        event EventHandler<ConnectionEventArgs> ConnectionCreated;

        /// <summary>
        /// 连接删除事件
        /// </summary>
        event EventHandler<ConnectionEventArgs> ConnectionDeleted;

        /// <summary>
        /// 创建连接
        /// </summary>
        WorkflowConnection CreateConnection(WorkflowNode sourceNode, WorkflowNode targetNode);

        /// <summary>
        /// 使用指定端口创建连接
        /// </summary>
        WorkflowConnection CreateConnectionWithPorts(
            WorkflowNode sourceNode,
            WorkflowNode targetNode,
            PortDirection sourcePort,
            PortDirection targetPort
        );

        /// <summary>
        /// 删除连接
        /// </summary>
        bool DeleteConnection(WorkflowConnection connection);

        /// <summary>
        /// 删除节点相关的所有连�?
        /// </summary>
        void DeleteNodeConnections(string nodeId);

        /// <summary>
        /// 检查连接是否存�?
        /// </summary>
        bool ConnectionExists(WorkflowNode sourceNode, WorkflowNode targetNode);

        /// <summary>
        /// 获取节点的所有连�?
        /// </summary>
        List<WorkflowConnection> GetNodeConnections(string nodeId);

        /// <summary>
        /// 获取节点的输入连�?
        /// </summary>
        List<WorkflowConnection> GetNodeInputConnections(string nodeId);

        /// <summary>
        /// 获取节点的输出连�?
        /// </summary>
        List<WorkflowConnection> GetNodeOutputConnections(string nodeId);

        /// <summary>
        /// 验证连接是否有效
        /// </summary>
        bool ValidateConnection(WorkflowNode sourceNode, WorkflowNode targetNode);

        /// <summary>
        /// 验证端口连接是否有效
        /// </summary>
        bool ValidatePortConnection(WorkflowNode sourceNode, WorkflowNode targetNode, PortDirection sourcePort, PortDirection targetPort);
    }

    /// <summary>
    /// 连接事件参数
    /// </summary>
    public class ConnectionEventArgs : EventArgs
    {
        public WorkflowConnection Connection { get; set; }
        public ConnectionAction Action { get; set; }
    }

    /// <summary>
    /// 连接动作类型
    /// </summary>
    public enum ConnectionAction
    {
        /// <summary>
        /// 创建
        /// </summary>
        Created,

        /// <summary>
        /// 删除
        /// </summary>
        Deleted
    }

    /// <summary>
    /// 连接服务 - 管理连接的创建、删除和验证
    /// </summary>
    public class ConnectionService : IConnectionService
    {
        private readonly ObservableCollection<WorkflowNode> _nodes;
        private readonly IConnectionPathService _pathService;

        public ObservableCollection<WorkflowConnection> Connections { get; } = new ObservableCollection<WorkflowConnection>();

        public event EventHandler<ConnectionEventArgs> ConnectionCreated;
        public event EventHandler<ConnectionEventArgs> ConnectionDeleted;

        public ConnectionService(
            ObservableCollection<WorkflowNode> nodes,
            IConnectionPathService pathService)
        {
            _nodes = nodes ?? throw new ArgumentNullException(nameof(nodes));
            _pathService = pathService ?? throw new ArgumentNullException(nameof(pathService));
        }

        public WorkflowConnection CreateConnection(WorkflowNode sourceNode, WorkflowNode targetNode)
        {
            if (sourceNode == null || targetNode == null)
            {
                return null;
            }

            if (!ValidateConnection(sourceNode, targetNode))
            {
                return null;
            }

            // 自动选择最佳端�?
            var (sourcePort, targetPort) = DetermineBestPorts(sourceNode, targetNode);
            return CreateConnectionWithPorts(sourceNode, targetNode, sourcePort, targetPort);
        }

        public WorkflowConnection CreateConnectionWithPorts(
            WorkflowNode sourceNode,
            WorkflowNode targetNode,
            PortDirection sourcePort,
            PortDirection targetPort)
        {
            if (sourceNode == null || targetNode == null)
            {
                return null;
            }

            if (!ValidatePortConnection(sourceNode, targetNode, sourcePort, targetPort))
            {
                return null;
            }

            // 检查连接是否已存在
            if (ConnectionExists(sourceNode, targetNode))
            {
                return null;
            }

            // 创建连接
            var connection = new WorkflowConnection
            {
                Id = Guid.NewGuid().ToString(),
                SourceNodeId = sourceNode.Id,
                TargetNodeId = targetNode.Id,
                SourcePort = sourcePort.ToString(),
                TargetPort = targetPort.ToString(),
                SourcePosition = GetPortPosition(sourceNode, sourcePort),
                TargetPosition = GetPortPosition(targetNode, targetPort)
            };

            // 计算路径
            connection.PathData = _pathService.CalculatePath(connection.SourcePosition, connection.TargetPosition);

            // 添加到集�?
            Connections.Add(connection);

            // 触发事件
            OnConnectionCreated(new ConnectionEventArgs
            {
                Connection = connection,
                Action = ConnectionAction.Created
            });

            return connection;
        }

        public bool DeleteConnection(WorkflowConnection connection)
        {
            if (connection == null || !Connections.Contains(connection))
            {
                return false;
            }

            Connections.Remove(connection);

            // 触发事件
            OnConnectionDeleted(new ConnectionEventArgs
            {
                Connection = connection,
                Action = ConnectionAction.Deleted
            });

            return true;
        }

        public void DeleteNodeConnections(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId))
            {
                return;
            }

            var connectionsToDelete = GetNodeConnections(nodeId).ToList();
            foreach (var connection in connectionsToDelete)
            {
                DeleteConnection(connection);
            }
        }

        public bool ConnectionExists(WorkflowNode sourceNode, WorkflowNode targetNode)
        {
            if (sourceNode == null || targetNode == null)
            {
                return false;
            }

            return Connections.Any(c =>
                c.SourceNodeId == sourceNode.Id &&
                c.TargetNodeId == targetNode.Id);
        }

        public List<WorkflowConnection> GetNodeConnections(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId))
            {
                return new List<WorkflowConnection>();
            }

            return Connections.Where(c =>
                c.SourceNodeId == nodeId ||
                c.TargetNodeId == nodeId).ToList();
        }

        public List<WorkflowConnection> GetNodeInputConnections(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId))
            {
                return new List<WorkflowConnection>();
            }

            return Connections.Where(c => c.TargetNodeId == nodeId).ToList();
        }

        public List<WorkflowConnection> GetNodeOutputConnections(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId))
            {
                return new List<WorkflowConnection>();
            }

            return Connections.Where(c => c.SourceNodeId == nodeId).ToList();
        }

        public bool ValidateConnection(WorkflowNode sourceNode, WorkflowNode targetNode)
        {
            // 不能连接到自�?
            if (sourceNode == targetNode)
            {
                return false;
            }

            // 检查是否已存在连接
            if (ConnectionExists(sourceNode, targetNode))
            {
                return false;
            }

            // 检查是否会产生循环
            if (WouldCreateCycle(sourceNode.Id, targetNode.Id))
            {
                return false;
            }

            return true;
        }

        public bool ValidatePortConnection(WorkflowNode sourceNode, WorkflowNode targetNode, PortDirection sourcePort, PortDirection targetPort)
        {
            if (!ValidateConnection(sourceNode, targetNode))
            {
                return false;
            }

            // 验证端口方向是否兼容
            if (!ArePortsCompatible(sourcePort, targetPort))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 从节点和端口方向获取端口位置
        /// </summary>
        private Point GetPortPosition(WorkflowNode node, PortDirection direction)
        {
            var nodeCenterX = node.Position.X + CanvasConfig.NodeWidth / 2;
            var nodeCenterY = node.Position.Y + CanvasConfig.NodeHeight / 2;

            return direction switch
            {
                PortDirection.Top => new Point(nodeCenterX, node.Position.Y),
                PortDirection.Bottom => new Point(nodeCenterX, node.Position.Y + CanvasConfig.NodeHeight),
                PortDirection.Left => new Point(node.Position.X, nodeCenterY),
                PortDirection.Right => new Point(node.Position.X + CanvasConfig.NodeWidth, nodeCenterY),
                _ => new Point(nodeCenterX, nodeCenterY)
            };
        }

        /// <summary>
        /// 确定最佳端口组�?
        /// </summary>
        private (PortDirection sourcePort, PortDirection targetPort) DetermineBestPorts(WorkflowNode sourceNode, WorkflowNode targetNode)
        {
            var deltaX = targetNode.Position.X - sourceNode.Position.X;
            var deltaY = targetNode.Position.Y - sourceNode.Position.Y;

            PortDirection sourcePort;
            PortDirection targetPort;

            if (Math.Abs(deltaX) > Math.Abs(deltaY))
            {
                // 水平方向主导
                if (deltaX > 0)
                {
                    sourcePort = PortDirection.Right;
                    targetPort = PortDirection.Left;
                }
                else
                {
                    sourcePort = PortDirection.Left;
                    targetPort = PortDirection.Right;
                }
            }
            else
            {
                // 垂直方向主导
                if (deltaY > 0)
                {
                    sourcePort = PortDirection.Bottom;
                    targetPort = PortDirection.Top;
                }
                else
                {
                    sourcePort = PortDirection.Top;
                    targetPort = PortDirection.Bottom;
                }
            }

            return (sourcePort, targetPort);
        }

        /// <summary>
        /// 检查端口是否兼�?
        /// </summary>
        private bool ArePortsCompatible(PortDirection sourcePort, PortDirection targetPort)
        {
            // 相对的端口是兼容�?
            return (sourcePort, targetPort) switch
            {
                (PortDirection.Top, PortDirection.Bottom) => true,
                (PortDirection.Bottom, PortDirection.Top) => true,
                (PortDirection.Left, PortDirection.Right) => true,
                (PortDirection.Right, PortDirection.Left) => true,
                _ => false
            };
        }

        /// <summary>
        /// 检查是否会产生循环
        /// </summary>
        private bool WouldCreateCycle(string sourceId, string targetId)
        {
            // 使用深度优先搜索检测循�?
            var visited = new HashSet<string>();
            return HasPathToTarget(targetId, sourceId, visited);
        }

        /// <summary>
        /// 检查是否存在从source到target的路�?
        /// </summary>
        private bool HasPathToTarget(string currentId, string targetId, HashSet<string> visited)
        {
            if (currentId == targetId)
            {
                return true;
            }

            if (visited.Contains(currentId))
            {
                return false;
            }

            visited.Add(currentId);

            // 获取所有从当前节点出发的连�?
            var outgoingConnections = Connections.Where(c => c.SourceNodeId == currentId);
            foreach (var connection in outgoingConnections)
            {
                if (HasPathToTarget(connection.TargetNodeId, targetId, visited))
                {
                    return true;
                }
            }

            return false;
        }

        protected virtual void OnConnectionCreated(ConnectionEventArgs e)
        {
            ConnectionCreated?.Invoke(this, e);
        }

        protected virtual void OnConnectionDeleted(ConnectionEventArgs e)
        {
            ConnectionDeleted?.Invoke(this, e);
        }
    }
}
