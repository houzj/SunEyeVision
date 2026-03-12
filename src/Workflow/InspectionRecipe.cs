using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using SunEyeVision.Core.Models;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.SDK.Models;

namespace SunEyeVision.Workflow;

/// <summary>
/// 检测配方（数据流 + 设备信息）
/// </summary>
/// <remarks>
/// 管理检测的数据配置，包含节点参数、全局变量和设备信息。
/// 与 InspectionProgram（执行流）完全分离，支持独立配置。
/// 
/// 使用场景：
/// 1. 同一产品在不同产线的参数配置
/// 2. 不同光照条件下的参数调整
/// 3. 快速切换配方（< 1秒）
/// 
/// 设计原则：
/// - 参数一致就相当于是同一个设备
/// - 配方合并了设备绑定和参数配置
/// </remarks>
public class InspectionRecipe : ObservableObject
{
    private string _id = Guid.NewGuid().ToString();
    private string _name = "标准配方";
    private string _projectId = string.Empty;
    private string _description = string.Empty;
    private DateTime _createdTime = DateTime.Now;
    private DateTime _modifiedTime = DateTime.Now;

    /// <summary>
    /// 配方唯一标识符
    /// </summary>
    public string Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    /// <summary>
    /// 配方名称
    /// </summary>
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value, "配方名称");
    }

    /// <summary>
    /// 所属项目ID
    /// </summary>
    public string ProjectId
    {
        get => _projectId;
        set => SetProperty(ref _projectId, value);
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
    /// 设备信息
    /// </summary>
    /// <remarks>
    /// 参数一致就相当于是同一个设备。
    /// 合并设备绑定到配方中，简化配置流程。
    /// </remarks>
    public DeviceInfo? Device { get; set; }

    /// <summary>
    /// 节点参数映射：NodeId -> ToolParameters
    /// </summary>
    /// <remarks>
    /// 存储该配方中所有节点的参数配置。
    /// ToolParameters 使用 JsonPolymorphic 支持多态序列化。
    /// </remarks>
    public Dictionary<string, ToolParameters> NodeParams { get; set; } = new();

    /// <summary>
    /// 全局变量
    /// </summary>
    /// <remarks>
    /// 配方级别的全局变量，可以在节点参数中引用。
    /// </remarks>
    public Dictionary<string, GlobalVariable> GlobalVariables { get; set; } = new();

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
    /// 保存节点参数
    /// </summary>
    /// <param name="nodeId">节点ID</param>
    /// <param name="parameters">参数实例</param>
    public void SaveNodeParams(string nodeId, ToolParameters parameters)
    {
        if (string.IsNullOrEmpty(nodeId))
            throw new ArgumentException("节点ID不能为空", nameof(nodeId));

        if (parameters == null)
            throw new ArgumentNullException(nameof(parameters));

        NodeParams[nodeId] = parameters.Clone();
        ModifiedTime = DateTime.Now;
    }

    /// <summary>
    /// 获取节点参数
    /// </summary>
    /// <param name="nodeId">节点ID</param>
    /// <returns>参数实例的克隆，如果不存在则返回null</returns>
    public ToolParameters? GetNodeParams(string nodeId)
    {
        if (!NodeParams.TryGetValue(nodeId, out var parameters))
            return null;

        return parameters.Clone();
    }

    /// <summary>
    /// 移除节点参数
    /// </summary>
    /// <param name="nodeId">节点ID</param>
    public void RemoveNodeParams(string nodeId)
    {
        if (NodeParams.Remove(nodeId))
        {
            ModifiedTime = DateTime.Now;
        }
    }

    /// <summary>
    /// 清空所有参数
    /// </summary>
    public void ClearParams()
    {
        NodeParams.Clear();
        ModifiedTime = DateTime.Now;
    }

    /// <summary>
    /// 设置全局变量
    /// </summary>
    public void SetGlobalVariable(string name, object? value, string type = "String", string description = "")
    {
        GlobalVariables[name] = new GlobalVariable
        {
            Name = name,
            Value = value,
            Type = type,
            Description = description
        };
        ModifiedTime = DateTime.Now;
    }

    /// <summary>
    /// 获取全局变量值
    /// </summary>
    public T? GetGlobalVariable<T>(string name)
    {
        if (!GlobalVariables.TryGetValue(name, out var variable))
            return default;

        if (variable.Value is T value)
            return value;

        try
        {
            return (T?)Convert.ChangeType(variable.Value, typeof(T));
        }
        catch
        {
            return default;
        }
    }

    /// <summary>
    /// 移除全局变量
    /// </summary>
    public void RemoveGlobalVariable(string name)
    {
        if (GlobalVariables.Remove(name))
        {
            ModifiedTime = DateTime.Now;
        }
    }

    /// <summary>
    /// 克隆配方
    /// </summary>
    public InspectionRecipe Clone()
    {
        var cloned = new InspectionRecipe
        {
            Id = Guid.NewGuid().ToString(),
            Name = Name + " (副本)",
            ProjectId = ProjectId,
            Description = Description,
            CreatedTime = DateTime.Now,
            ModifiedTime = DateTime.Now
        };

        // 深拷贝设备信息
        if (Device != null)
        {
            cloned.Device = new DeviceInfo
            {
                DeviceId = Device.DeviceId,
                DeviceName = Device.DeviceName,
                DeviceType = Device.DeviceType,
                IsConnected = Device.IsConnected,
                Manufacturer = Device.Manufacturer,
                Model = Device.Model,
                Description = Device.Description
            };
        }

        // 深拷贝节点参数
        foreach (var kvp in NodeParams)
        {
            cloned.NodeParams[kvp.Key] = kvp.Value.Clone();
        }

        // 深拷贝全局变量
        foreach (var kvp in GlobalVariables)
        {
            cloned.GlobalVariables[kvp.Key] = new GlobalVariable
            {
                Name = kvp.Value.Name,
                Value = kvp.Value.Value,
                Type = kvp.Value.Type,
                Description = kvp.Value.Description
            };
        }

        return cloned;
    }
}
