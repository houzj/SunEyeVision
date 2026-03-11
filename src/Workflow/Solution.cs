using System;
using System.Collections.Generic;
using System.Text.Json;
using SunEyeVision.Plugin.SDK.Models;

namespace SunEyeVision.Workflow;

/// <summary>
/// 解决方案模型
/// </summary>
public class Solution : ObservableObject
{
    private string _id = Guid.NewGuid().ToString();
    private string _name = "新建方案";
    private string _description = string.Empty;
    private DateTime _createdAt = DateTime.Now;
    private DateTime _modifiedAt = DateTime.Now;

    /// <summary>
    /// 方案唯一标识符
    /// </summary>
    public string Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    /// <summary>
    /// 方案名称
    /// </summary>
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value, "方案名称");
    }

    /// <summary>
    /// 方案描述
    /// </summary>
    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value, "方案描述");
    }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt
    {
        get => _createdAt;
        set => SetProperty(ref _createdAt, value);
    }

    /// <summary>
    /// 修改时间
    /// </summary>
    public DateTime ModifiedAt
    {
        get => _modifiedAt;
        set => SetProperty(ref _modifiedAt, value);
    }

    /// <summary>
    /// 工作流列表
    /// </summary>
    public List<string> Workflows { get; set; } = new();

    /// <summary>
    /// 配方列表
    /// </summary>
    public List<Recipe> Recipes { get; set; } = new();

    /// <summary>
    /// 全局变量
    /// </summary>
    public Dictionary<string, GlobalVariable> GlobalVariables { get; set; } = new();
}
