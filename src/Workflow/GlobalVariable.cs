using System;
using System.Collections.Generic;
using System.Linq;
using SunEyeVision.Plugin.SDK.Models;

namespace SunEyeVision.Workflow;

/// <summary>
/// 全局变量
/// </summary>
/// <remarks>
/// 用于存储解决方案级别的全局变量，可以在所有工作流和节点中访问。
/// </remarks>
public class GlobalVariable : ObservableObject
{
    private string _name = "";
    private object? _value = null;
    private string _type = "String";
    private string _description = "";
    private bool _isReadOnly = false;

    /// <summary>
    /// 变量ID
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 变量名称
    /// </summary>
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value, "变量名称");
    }

    /// <summary>
    /// 变量值
    /// </summary>
    public object? Value
    {
        get => _value;
        set => SetProperty(ref _value, value, "变量值");
    }

    /// <summary>
    /// 变量类型
    /// </summary>
    public string Type
    {
        get => _type;
        set => SetProperty(ref _type, value, "变量类型");
    }

    /// <summary>
    /// 变量描述
    /// </summary>
    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value, "变量描述");
    }

    /// <summary>
    /// 是否只读
    /// </summary>
    public bool IsReadOnly
    {
        get => _isReadOnly;
        set => SetProperty(ref _isReadOnly, value, "是否只读");
    }

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
    public GlobalVariable()
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    public GlobalVariable(string name, object? value, string type = "String")
    {
        Name = name;
        Value = value;
        Type = type;
    }

    /// <summary>
    /// 克隆变量
    /// </summary>
    public GlobalVariable Clone()
    {
        return new GlobalVariable
        {
            Id = Guid.NewGuid().ToString(),
            Name = Name,
            Value = Value,
            Type = Type,
            Description = Description,
            IsReadOnly = IsReadOnly,
            CreatedTime = CreatedTime,
            ModifiedTime = DateTime.Now
        };
    }

    /// <summary>
    /// 验证变量
    /// </summary>
    public (bool IsValid, List<string> Errors) Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Name))
        {
            errors.Add("变量名称不能为空");
        }

        if (string.IsNullOrEmpty(Type))
        {
            errors.Add("变量类型不能为空");
        }

        return (errors.Count == 0, errors);
    }
}

/// <summary>
/// 全局变量管理器
/// </summary>
public class GlobalVariableManager
{
    private readonly Dictionary<string, GlobalVariable> _variables = new();

    /// <summary>
    /// 获取所有变量
    /// </summary>
    public List<GlobalVariable> GetAll()
    {
        return _variables.Values.ToList();
    }

    /// <summary>
    /// 获取变量
    /// </summary>
    public GlobalVariable? Get(string name)
    {
        return _variables.TryGetValue(name, out var variable) ? variable : null;
    }

    /// <summary>
    /// 添加变量
    /// </summary>
    public void Add(GlobalVariable variable)
    {
        if (string.IsNullOrWhiteSpace(variable.Name))
            throw new ArgumentException("变量名称不能为空");

        _variables[variable.Name] = variable;
    }

    /// <summary>
    /// 移除变量
    /// </summary>
    public bool Remove(string name)
    {
        return _variables.Remove(name);
    }

    /// <summary>
    /// 更新变量
    /// </summary>
    public bool Update(GlobalVariable variable)
    {
        if (string.IsNullOrWhiteSpace(variable.Name))
            return false;

        if (!_variables.ContainsKey(variable.Name))
            return false;

        _variables[variable.Name] = variable;
        return true;
    }

    /// <summary>
    /// 设置变量值
    /// </summary>
    public void SetValue(string name, object? value)
    {
        if (!_variables.TryGetValue(name, out var variable))
            return;

        variable.Value = value;
        variable.ModifiedTime = DateTime.Now;
    }

    /// <summary>
    /// 获取变量值
    /// </summary>
    public object? GetValue(string name)
    {
        return _variables.TryGetValue(name, out var variable) ? variable.Value : null;
    }

    /// <summary>
    /// 检查变量是否存在
    /// </summary>
    public bool Contains(string name)
    {
        return _variables.ContainsKey(name);
    }

    /// <summary>
    /// 清空所有变量
    /// </summary>
    public void Clear()
    {
        _variables.Clear();
    }
}
