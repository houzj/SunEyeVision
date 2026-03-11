using SunEyeVision.Plugin.SDK.Models;

namespace SunEyeVision.Workflow;

/// <summary>
/// 全局变量（系统级配置）
/// </summary>
/// <remarks>
/// 区别于 WorkflowContext 中的运行时变量：
/// - GlobalVariable：系统级配置，保存在解决方案中，持久化存储
/// - WorkflowContext 变量：运行时变量，不持久化，仅在执行期间有效
/// </remarks>
public class GlobalVariable : ObservableObject
{
    private string _name = string.Empty;
    private object? _value;
    private string _type = "String";

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
    /// 变量类型（String/Int/Double/Bool等）
    /// </summary>
    public string Type
    {
        get => _type;
        set => SetProperty(ref _type, value, "变量类型");
    }
}
