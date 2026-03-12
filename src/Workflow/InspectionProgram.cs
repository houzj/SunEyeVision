using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.SDK.Models;

namespace SunEyeVision.Workflow;

/// <summary>
/// 检测程序（执行流）
/// </summary>
/// <remarks>
/// 管理检测的执行逻辑，包含节点拓扑结构、连接关系和默认参数。
/// 与 InspectionRecipe（数据流）完全分离，支持独立升级。
/// 
/// 使用场景：
/// 1. 创建新的检测流程
/// 2. 升级检测逻辑（不影响配方数据）
/// 3. 批量部署（复制程序文件到多台设备）
/// </remarks>
public class InspectionProgram : ObservableObject
{
    private string _id = Guid.NewGuid().ToString();
    private string _name = "检测程序";
    private string _version = "1.0.0";
    private string _description = string.Empty;
    private DateTime _createdTime = DateTime.Now;
    private DateTime _modifiedTime = DateTime.Now;

    /// <summary>
    /// 程序唯一标识符
    /// </summary>
    public string Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    /// <summary>
    /// 程序名称
    /// </summary>
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value, "程序名称");
    }

    /// <summary>
    /// 版本号（语义化版本）
    /// </summary>
    /// <remarks>
    /// 格式：主版本.次版本.修订版本
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
    /// 节点列表
    /// </summary>
    /// <remarks>
    /// 存储检测程序中的所有节点定义。
    /// </remarks>
    public List<ProgramNode> Nodes { get; set; } = new();

    /// <summary>
    /// 节点连接关系
    /// </summary>
    public List<ProgramConnection> Connections { get; set; } = new();

    /// <summary>
    /// 节点默认参数
    /// </summary>
    /// <remarks>
    /// 用于新建配方时的初始值。
    /// 当配方中没有对应节点的参数时，使用此默认值。
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
    /// 添加节点
    /// </summary>
    public void AddNode(ProgramNode node)
    {
        Nodes.Add(node);
        if (node.DefaultParameters != null)
        {
            DefaultParams[node.Id] = node.DefaultParameters.Clone();
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
            
            // 移除相关连接
            Connections.RemoveAll(c => c.SourceNodeId == nodeId || c.TargetNodeId == nodeId);
            
            ModifiedTime = DateTime.Now;
        }
    }

    /// <summary>
    /// 连接节点
    /// </summary>
    public void ConnectNodes(string sourceNodeId, string sourcePort, string targetNodeId, string targetPort)
    {
        var connection = new ProgramConnection
        {
            SourceNodeId = sourceNodeId,
            SourcePort = sourcePort,
            TargetNodeId = targetNodeId,
            TargetPort = targetPort
        };

        Connections.Add(connection);
        ModifiedTime = DateTime.Now;
    }

    /// <summary>
    /// 断开节点连接
    /// </summary>
    public void DisconnectNodes(string sourceNodeId, string targetNodeId)
    {
        Connections.RemoveAll(c => c.SourceNodeId == sourceNodeId && c.TargetNodeId == targetNodeId);
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
    /// 克隆程序
    /// </summary>
    public InspectionProgram Clone()
    {
        var cloned = new InspectionProgram
        {
            Id = Guid.NewGuid().ToString(),
            Name = Name + " (副本)",
            Version = Version,
            Description = Description,
            CreatedTime = DateTime.Now,
            ModifiedTime = DateTime.Now
        };

        // 深拷贝节点
        foreach (var node in Nodes)
        {
            cloned.Nodes.Add(new ProgramNode
            {
                Id = node.Id,
                Name = node.Name,
                Type = node.Type,
                AlgorithmType = node.AlgorithmType,
                Position = node.Position,
                IsEnabled = node.IsEnabled,
                DefaultParameters = node.DefaultParameters?.Clone()
            });
        }

        // 深拷贝连接
        foreach (var conn in Connections)
        {
            cloned.Connections.Add(new ProgramConnection
            {
                SourceNodeId = conn.SourceNodeId,
                SourcePort = conn.SourcePort,
                TargetNodeId = conn.TargetNodeId,
                TargetPort = conn.TargetPort
            });
        }

        // 深拷贝默认参数
        foreach (var kvp in DefaultParams)
        {
            cloned.DefaultParams[kvp.Key] = kvp.Value.Clone();
        }

        return cloned;
    }
}

/// <summary>
/// 程序节点
/// </summary>
public class ProgramNode
{
    /// <summary>
    /// 节点ID
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 节点名称
    /// </summary>
    public string Name { get; set; } = "节点";

    /// <summary>
    /// 节点类型
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 算法类型
    /// </summary>
    public string? AlgorithmType { get; set; }

    /// <summary>
    /// 位置（画布坐标）
    /// </summary>
    public NodePosition Position { get; set; } = new();

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 默认参数
    /// </summary>
    public ToolParameters? DefaultParameters { get; set; }
}

/// <summary>
/// 程序连接
/// </summary>
public class ProgramConnection
{
    /// <summary>
    /// 源节点ID
    /// </summary>
    public string SourceNodeId { get; set; } = string.Empty;

    /// <summary>
    /// 源端口
    /// </summary>
    public string SourcePort { get; set; } = string.Empty;

    /// <summary>
    /// 目标节点ID
    /// </summary>
    public string TargetNodeId { get; set; } = string.Empty;

    /// <summary>
    /// 目标端口
    /// </summary>
    public string TargetPort { get; set; } = string.Empty;
}

/// <summary>
/// 节点位置
/// </summary>
public class NodePosition
{
    /// <summary>
    /// X坐标
    /// </summary>
    public double X { get; set; }

    /// <summary>
    /// Y坐标
    /// </summary>
    public double Y { get; set; }
}
