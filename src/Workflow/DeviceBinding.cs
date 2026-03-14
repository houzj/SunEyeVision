using System;
using SunEyeVision.Plugin.SDK.Models;

namespace SunEyeVision.Workflow;

/// <summary>
/// 设备绑定关系
/// </summary>
/// <remarks>
/// 定义设备→Workflow+Recipe的绑定关系,支持运行时动态切换。
/// </remarks>
public class DeviceBinding : ObservableObject
{
    private string _deviceName = "";
    private string _recipeGroupName = "default";

    /// <summary>
    /// 设备唯一标识符
    /// </summary>
    public string DeviceId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 设备名称
    /// </summary>
    public string DeviceName
    {
        get => _deviceName;
        set => SetProperty(ref _deviceName, value, "设备名称");
    }

    /// <summary>
    /// 引用的工作流ID
    /// </summary>
    public string WorkflowRef { get; set; } = "";

    /// <summary>
    /// 引用的配方ID
    /// </summary>
    public string RecipeRef { get; set; } = "";

    /// <summary>
    /// 使用的配方组名称
    /// </summary>
    public string RecipeGroupName
    {
        get => _recipeGroupName;
        set => SetProperty(ref _recipeGroupName, value, "配方组");
    }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedTime { get; set; } = DateTime.Now;

    /// <summary>
    /// 最后切换时间
    /// </summary>
    public DateTime LastSwitchTime { get; set; } = DateTime.Now;

    /// <summary>
    /// 克隆设备绑定
    /// </summary>
    public DeviceBinding Clone()
    {
        return new DeviceBinding
        {
            DeviceId = Guid.NewGuid().ToString(),
            DeviceName = DeviceName,
            WorkflowRef = WorkflowRef,
            RecipeRef = RecipeRef,
            RecipeGroupName = RecipeGroupName,
            CreatedTime = DateTime.Now,
            LastSwitchTime = DateTime.Now
        };
    }

    /// <summary>
    /// 验证设备绑定
    /// </summary>
    public (bool IsValid, List<string> Errors) Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrEmpty(DeviceName))
        {
            errors.Add("设备名称为空");
        }

        if (string.IsNullOrEmpty(WorkflowRef))
        {
            errors.Add("设备绑定没有关联的工作流");
        }

        if (string.IsNullOrEmpty(RecipeRef))
        {
            errors.Add("设备绑定没有关联的配方");
        }

        if (string.IsNullOrEmpty(RecipeGroupName))
        {
            errors.Add("配方组名称为空");
        }

        return (errors.Count == 0, errors);
    }
}
