using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.SDK.Models;

namespace SunEyeVision.Workflow;

/// <summary>
/// 工作流定义（执行流配置）
/// </summary>
/// <remarks>
/// 工作流定义管理执行逻辑，与数据配置完全分离：
/// - 包含节点拓扑结构、连接关系
/// - 不包含具体的参数值（参数由 DataConfiguration 管理）
/// - 支持版本管理和独立升级
/// 
/// 解耦优势：
/// 1. 工作流升级：只需更新 WorkflowDefinition，不影响数据配置
/// 2. 批量升级：100台设备只需修改引用，无需逐个修改文件
/// 3. 版本回退：只需切换引用，无需恢复整个方案文件
/// </remarks>
public class WorkflowDefinition : ObservableObject
{
    private string _id = Guid.NewGuid().ToString();
    private string _name = "新建工作流";
    private string _version = "1.0.0";
    private string _description = string.Empty;
    private string _category = string.Empty;
    private DateTime _createdTime = DateTime.Now;
    private DateTime _modifiedTime = DateTime.Now;

    /// <summary>
    /// 工作流定义唯一标识符
    /// </summary>
    public string Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    /// <summary>
    /// 工作流名称
    /// </summary>
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value, "工作流名称");
    }

    /// <summary>
    /// 版本号（语义化版本）
    /// </summary>
    /// <remarks>
    /// 格式：主版本.次版本.修订版本
    /// - 主版本：不兼容的 API 修改
    /// - 次版本：向下兼容的功能性新增
    /// - 修订版本：向下兼容的问题修正
    /// </remarks>
    public string Version
    {
        get => _version;
        set => SetProperty(ref _version, value, "版本号");
    }

    /// <summary>
    /// 描述信息
    /// </summary>
    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    /// <summary>
    /// 分类（用于组织管理）
    /// </summary>
    /// <remarks>
    /// 例如：standard_inspection（标准检测）、detail_inspection（精细检测）
    /// </remarks>
    public string Category
    {
        get => _category;
        set => SetProperty(ref _category, value);
    }

    /// <summary>
    /// 节点拓扑结构
    /// </summary>
    /// <remarks>
    /// 存储工作流中的所有节点定义。
    /// 每个节点包含：ID、名称、类型、算法类型等元数据。
    /// </remarks>
    public List<WorkflowNode> Nodes { get; set; } = new();

    /// <summary>
    /// 节点连接关系（源节点ID -> 目标节点ID列表）
    /// </summary>
    public Dictionary<string, List<string>> Connections { get; set; } = new();

    /// <summary>
    /// 端口级连接列表
    /// </summary>
    /// <remarks>
    /// 精确记录哪个端口连接到哪个端口，支持复杂的连接关系。
    /// </remarks>
    public List<PortConnection> PortConnections { get; set; } = new();

    /// <summary>
    /// 节点默认参数（模板）
    /// </summary>
    /// <remarks>
    /// 用于新建配方时的初始值。
    /// 当 DataConfiguration 中没有对应节点的参数时，使用此默认值。
    /// </remarks>
    public Dictionary<string, ToolParameters> DefaultParams { get; set; } = new();

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedTime
    {
        get => _createdTime;
        set => SetProperty(ref _createdTime, value);
    }

    /// <summary>
    /// 最后修改时间
    /// </summary>
    public DateTime ModifiedTime
    {
        get => _modifiedTime;
        set => SetProperty(ref _modifiedTime, value);
    }

    /// <summary>
    /// 标签（用于搜索和分类）
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// 从现有 Workflow 创建 WorkflowDefinition
    /// </summary>
    /// <param name="workflow">现有工作流实例</param>
    /// <returns>工作流定义实例</returns>
    public static WorkflowDefinition FromWorkflow(Workflow workflow)
    {
        var definition = new WorkflowDefinition
        {
            Id = workflow.Id,
            Name = workflow.Name,
            Description = workflow.Description ?? string.Empty,
            Nodes = new List<WorkflowNode>(workflow.Nodes),
            Connections = new Dictionary<string, List<string>>(workflow.Connections),
            PortConnections = new List<PortConnection>(workflow.PortConnections),
            CreatedTime = DateTime.Now,
            ModifiedTime = DateTime.Now
        };

        // 提取节点默认参数
        foreach (var node in workflow.Nodes)
        {
            if (node.Parameters != null)
            {
                definition.DefaultParams[node.Id] = node.Parameters.Clone();
            }
        }

        return definition;
    }

    /// <summary>
    /// 创建运行时 Workflow 实例
    /// </summary>
    /// <param name="nodeParams">节点参数映射（可选，用于覆盖默认参数）</param>
    /// <returns>Workflow 实例</returns>
    public Workflow CreateWorkflow(Dictionary<string, ToolParameters>? nodeParams = null)
    {
        var workflow = new Workflow(Id, Name, null!);
        workflow.Description = Description;

        // 创建节点实例并应用参数
        foreach (var node in Nodes)
        {
            var nodeInstance = new WorkflowNode(node.Id, node.Name, node.Type)
            {
                AlgorithmType = node.AlgorithmType,
                IsEnabled = node.IsEnabled,
                ParameterBindings = node.ParameterBindings
            };

            // 应用参数：优先使用传入参数，其次使用默认参数
            if (nodeParams != null && nodeParams.TryGetValue(node.Id, out var param))
            {
                nodeInstance.Parameters = param.Clone();
            }
            else if (DefaultParams.TryGetValue(node.Id, out var defaultParam))
            {
                nodeInstance.Parameters = defaultParam.Clone();
            }

            workflow.Nodes.Add(nodeInstance);
        }

        // 复制连接关系
        foreach (var kvp in Connections)
        {
            workflow.Connections[kvp.Key] = new List<string>(kvp.Value);
        }

        workflow.PortConnections = new List<PortConnection>(PortConnections);

        return workflow;
    }

    /// <summary>
    /// 添加节点
    /// </summary>
    public void AddNode(WorkflowNode node)
    {
        Nodes.Add(node);
        if (node.Parameters != null)
        {
            DefaultParams[node.Id] = node.Parameters.Clone();
        }
        ModifiedTime = DateTime.Now;
    }

    /// <summary>
    /// 移除节点
    /// </summary>
    public void RemoveNode(string nodeId)
    {
        var node = Nodes.Find(n => n.Id == nodeId);
        if (node != null)
        {
            Nodes.Remove(node);
            DefaultParams.Remove(nodeId);
            Connections.Remove(nodeId);

            // 移除指向该节点的连接
            foreach (var kvp in Connections)
            {
                kvp.Value.Remove(nodeId);
            }

            // 移除端口连接
            PortConnections.RemoveAll(pc => pc.SourceNodeId == nodeId || pc.TargetNodeId == nodeId);

            ModifiedTime = DateTime.Now;
        }
    }

    /// <summary>
    /// 连接节点
    /// </summary>
    public void ConnectNodes(string sourceNodeId, string targetNodeId)
    {
        if (!Connections.ContainsKey(sourceNodeId))
        {
            Connections[sourceNodeId] = new List<string>();
        }

        if (!Connections[sourceNodeId].Contains(targetNodeId))
        {
            Connections[sourceNodeId].Add(targetNodeId);
            ModifiedTime = DateTime.Now;
        }
    }

    /// <summary>
    /// 通过端口连接节点
    /// </summary>
    public void ConnectNodesByPort(string sourceNodeId, string sourcePort, string targetNodeId, string targetPort)
    {
        ConnectNodes(sourceNodeId, targetNodeId);

        var exists = PortConnections.Exists(pc =>
            pc.SourceNodeId == sourceNodeId && pc.SourcePort == sourcePort &&
            pc.TargetNodeId == targetNodeId && pc.TargetPort == targetPort);

        if (!exists)
        {
            PortConnections.Add(new PortConnection
            {
                SourceNodeId = sourceNodeId,
                SourcePort = sourcePort,
                TargetNodeId = targetNodeId,
                TargetPort = targetPort
            });
            ModifiedTime = DateTime.Now;
        }
    }

    /// <summary>
    /// 断开节点连接
    /// </summary>
    public void DisconnectNodes(string sourceNodeId, string targetNodeId)
    {
        if (Connections.ContainsKey(sourceNodeId))
        {
            Connections[sourceNodeId].Remove(targetNodeId);
        }
        ModifiedTime = DateTime.Now;
    }

    /// <summary>
    /// 更新版本号
    /// </summary>
    public void UpdateVersion(string newVersion)
    {
        Version = newVersion;
        ModifiedTime = DateTime.Now;
    }

    /// <summary>
    /// 克隆工作流定义
    /// </summary>
    public WorkflowDefinition Clone()
    {
        var cloned = new WorkflowDefinition
        {
            Id = Guid.NewGuid().ToString(),
            Name = Name + " (副本)",
            Version = Version,
            Description = Description,
            Category = Category,
            CreatedTime = DateTime.Now,
            ModifiedTime = DateTime.Now
        };

        // 深拷贝节点
        foreach (var node in Nodes)
        {
            cloned.Nodes.Add(new WorkflowNode(node.Id, node.Name, node.Type)
            {
                AlgorithmType = node.AlgorithmType,
                IsEnabled = node.IsEnabled,
                ParameterBindings = node.ParameterBindings
            });
        }

        // 深拷贝连接
        foreach (var kvp in Connections)
        {
            cloned.Connections[kvp.Key] = new List<string>(kvp.Value);
        }

        cloned.PortConnections = new List<PortConnection>(PortConnections);

        // 深拷贝默认参数
        foreach (var kvp in DefaultParams)
        {
            cloned.DefaultParams[kvp.Key] = kvp.Value.Clone();
        }

        cloned.Tags = new List<string>(Tags);

        return cloned;
    }
}
