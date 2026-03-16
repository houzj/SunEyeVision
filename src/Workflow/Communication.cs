using System;
using System.Collections.Generic;
using SunEyeVision.Plugin.SDK.Models;

namespace SunEyeVision.Workflow;

/// <summary>
/// 通讯类型枚举
/// </summary>
public enum CommunicationType
{
    /// <summary>
    /// HTTP
    /// </summary>
    HTTP,

    /// <summary>
    /// Modbus TCP
    /// </summary>
    ModbusTCP,

    /// <summary>
    /// Modbus RTU
    /// </summary>
    ModbusRTU,

    /// <summary>
    /// OPC UA
    /// </summary>
    OPCUA,

    /// <summary>
    /// TCP/IP
    /// </summary>
    TCP,

    /// <summary>
    /// UDP
    /// </summary>
    UDP,

    /// <summary>
    /// 串口
    /// </summary>
    Serial,

    /// <summary>
    /// 其他
    /// </summary>
    Other
}

/// <summary>
/// 通讯模型
/// </summary>
/// <remarks>
/// 用于管理解决方案中的通讯配置，包括HTTP、Modbus、OPC UA等。
/// </remarks>
public class Communication : ObservableObject
{
    private string _name = "";
    private bool _enabled = true;
    private CommunicationType _connectionType = CommunicationType.HTTP;
    private string _description = "";

    /// <summary>
    /// 通讯ID
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 通讯名称
    /// </summary>
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value, "通讯名称");
    }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled
    {
        get => _enabled;
        set => SetProperty(ref _enabled, value, "是否启用");
    }

    /// <summary>
    /// 通讯类型
    /// </summary>
    public CommunicationType ConnectionType
    {
        get => _connectionType;
        set => SetProperty(ref _connectionType, value, "通讯类型");
    }

    /// <summary>
    /// 通讯描述
    /// </summary>
    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value, "通讯描述");
    }

    /// <summary>
    /// 通讯设置（JSON格式）
    /// </summary>
    public Dictionary<string, object> Settings { get; set; } = new();

    /// <summary>
    /// 数据映射配置
    /// </summary>
    public Dictionary<string, object> Mapping { get; set; } = new();

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedTime { get; set; } = DateTime.Now;

    /// <summary>
    /// 修改时间
    /// </summary>
    public DateTime ModifiedTime { get; set; } = DateTime.Now;

    /// <summary>
    /// 无参构造函数
    /// </summary>
    public Communication()
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    public Communication(string name, CommunicationType connectionType)
    {
        Name = name;
        ConnectionType = connectionType;
    }

    /// <summary>
    /// 克隆通讯配置
    /// </summary>
    public Communication Clone()
    {
        return new Communication
        {
            Id = Guid.NewGuid().ToString(),
            Name = Name,
            IsEnabled = IsEnabled,
            ConnectionType = ConnectionType,
            Description = Description,
            Settings = new Dictionary<string, object>(Settings),
            Mapping = new Dictionary<string, object>(Mapping),
            CreatedTime = CreatedTime,
            ModifiedTime = DateTime.Now
        };
    }

    /// <summary>
    /// 验证通讯配置
    /// </summary>
    public (bool IsValid, List<string> Errors) Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Name))
        {
            errors.Add("通讯名称不能为空");
        }

        if (Settings.Count == 0)
        {
            errors.Add("通讯设置不能为空");
        }

        return (errors.Count == 0, errors);
    }

    /// <summary>
    /// 获取连接字符串
    /// </summary>
    public string GetConnectionString()
    {
        if (Settings.TryGetValue("host", out var host) && Settings.TryGetValue("port", out var port))
        {
            return $"{host}:{port}";
        }
        else if (Settings.TryGetValue("ipAddress", out var ipAddress) && Settings.TryGetValue("port", out port))
        {
            return $"{ipAddress}:{port}";
        }
        else
        {
            return Name;
        }
    }
}
