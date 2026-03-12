using System;
using SunEyeVision.Plugin.SDK.Models;

namespace SunEyeVision.Workflow;

/// <summary>
/// 运行时绑定 - 设备与工作流、数据配置的关联关系
/// </summary>
/// <remarks>
/// 运行时绑定是解耦架构的核心组件，实现执行流和数据流的动态组合：
/// - 设备不直接持有工作流和数据配置，而是通过引用关联
/// - 支持快速切换工作流版本、数据配置、配方组
/// - 绑定信息独立存储，修改不影响工作流和数据配置文件
/// 
/// 典型使用场景：
/// 1. 新设备上线：创建新的 RuntimeBinding，引用已有的工作流和数据配置
/// 2. 产品切换：修改 DataConfigRef 和 RecipeGroup，无需修改工作流
/// 3. 工作流升级：修改 WorkflowRef，批量升级只需修改引用
/// 4. 配方组切换：修改 RecipeGroup，实现不同检测模式
/// </remarks>
public class RuntimeBinding : ObservableObject
{
    private string _deviceId = string.Empty;
    private string _deviceName = string.Empty;
    private string _deviceType = string.Empty;
    private string _workflowRef = string.Empty;
    private string _workflowVersion = string.Empty;
    private string _dataConfigRef = string.Empty;
    private string _recipeGroup = "default";
    private bool _isEnabled = true;
    private DateTime _createdTime = DateTime.Now;
    private DateTime _lastSwitchTime = DateTime.Now;

    /// <summary>
    /// 设备唯一标识符
    /// </summary>
    public string DeviceId
    {
        get => _deviceId;
        set => SetProperty(ref _deviceId, value, "设备ID");
    }

    /// <summary>
    /// 设备名称
    /// </summary>
    public string DeviceName
    {
        get => _deviceName;
        set => SetProperty(ref _deviceName, value, "设备名称");
    }

    /// <summary>
    /// 设备类型
    /// </summary>
    /// <remarks>
    /// 用于设备分类管理，如：production（生产设备）、test（测试设备）
    /// </remarks>
    public string DeviceType
    {
        get => _deviceType;
        set => SetProperty(ref _deviceType, value);
    }

    /// <summary>
    /// 工作流引用（相对路径或ID）
    /// </summary>
    /// <remarks>
    /// 支持多种引用格式：
    /// - ID: "wf_001"
    /// - 相对路径: "standard_inspection/v1.0"
    /// - 分类路径: "inspection/standard/v2.1"
    /// </remarks>
    public string WorkflowRef
    {
        get => _workflowRef;
        set => SetProperty(ref _workflowRef, value, "工作流引用");
    }

    /// <summary>
    /// 工作流版本（冗余字段，便于查询）
    /// </summary>
    public string WorkflowVersion
    {
        get => _workflowVersion;
        set => SetProperty(ref _workflowVersion, value);
    }

    /// <summary>
    /// 数据配置引用（相对路径或ID）
    /// </summary>
    /// <remarks>
    /// 支持多种引用格式：
    /// - ID: "dc_001"
    /// - 产品编码: "products/A100"
    /// - 设备专用: "devices/device_001"
    /// </remarks>
    public string DataConfigRef
    {
        get => _dataConfigRef;
        set => SetProperty(ref _dataConfigRef, value, "数据配置引用");
    }

    /// <summary>
    /// 使用的配方组名称
    /// </summary>
    /// <remarks>
    /// 默认为 "default"，可以根据需要切换到其他配方组。
    /// 例如：default、fast、precision
    /// </remarks>
    public string RecipeGroup
    {
        get => _recipeGroup;
        set => SetProperty(ref _recipeGroup, value, "配方组");
    }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetProperty(ref _isEnabled, value);
    }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedTime
    {
        get => _createdTime;
        set => SetProperty(ref _createdTime, value);
    }

    /// <summary>
    /// 最后切换时间
    /// </summary>
    /// <remarks>
    /// 记录最后一次修改绑定的时间，用于审计和追踪。
    /// </remarks>
    public DateTime LastSwitchTime
    {
        get => _lastSwitchTime;
        set => SetProperty(ref _lastSwitchTime, value);
    }

    /// <summary>
    /// 执行统计信息
    /// </summary>
    public BindingStatistics? Statistics { get; set; }

    /// <summary>
    /// 切换产品（数据配置）
    /// </summary>
    /// <param name="newDataConfigRef">新数据配置引用</param>
    /// <param name="recipeGroup">配方组名称（可选）</param>
    public void SwitchProduct(string newDataConfigRef, string? recipeGroup = null)
    {
        DataConfigRef = newDataConfigRef;
        if (recipeGroup != null)
            RecipeGroup = recipeGroup;
        LastSwitchTime = DateTime.Now;
    }

    /// <summary>
    /// 切换配方组
    /// </summary>
    /// <param name="recipeGroup">配方组名称</param>
    public void SwitchRecipeGroup(string recipeGroup)
    {
        RecipeGroup = recipeGroup;
        LastSwitchTime = DateTime.Now;
    }

    /// <summary>
    /// 升级工作流版本
    /// </summary>
    /// <param name="newWorkflowRef">新工作流引用</param>
    /// <param name="version">版本号（可选）</param>
    public void UpgradeWorkflow(string newWorkflowRef, string? version = null)
    {
        WorkflowRef = newWorkflowRef;
        if (version != null)
            WorkflowVersion = version;
        LastSwitchTime = DateTime.Now;
    }

    /// <summary>
    /// 验证绑定配置是否完整
    /// </summary>
    /// <returns>验证结果</returns>
    public bool Validate()
    {
        if (string.IsNullOrEmpty(DeviceId))
            return false;

        if (string.IsNullOrEmpty(WorkflowRef))
            return false;

        if (string.IsNullOrEmpty(DataConfigRef))
            return false;

        if (string.IsNullOrEmpty(RecipeGroup))
            return false;

        return true;
    }

    /// <summary>
    /// 克隆绑定
    /// </summary>
    public RuntimeBinding Clone()
    {
        return new RuntimeBinding
        {
            DeviceId = DeviceId,
            DeviceName = DeviceName,
            DeviceType = DeviceType,
            WorkflowRef = WorkflowRef,
            WorkflowVersion = WorkflowVersion,
            DataConfigRef = DataConfigRef,
            RecipeGroup = RecipeGroup,
            IsEnabled = IsEnabled,
            CreatedTime = CreatedTime,
            LastSwitchTime = LastSwitchTime,
            Statistics = Statistics?.Clone()
        };
    }
}

/// <summary>
/// 绑定统计信息
/// </summary>
public class BindingStatistics : ObservableObject
{
    private long _totalExecutions;
    private long _successCount;
    private long _failureCount;
    private DateTime _lastExecutionTime;
    private double _averageExecutionTime;

    /// <summary>
    /// 总执行次数
    /// </summary>
    public long TotalExecutions
    {
        get => _totalExecutions;
        set => SetProperty(ref _totalExecutions, value);
    }

    /// <summary>
    /// 成功次数
    /// </summary>
    public long SuccessCount
    {
        get => _successCount;
        set => SetProperty(ref _successCount, value);
    }

    /// <summary>
    /// 失败次数
    /// </summary>
    public long FailureCount
    {
        get => _failureCount;
        set => SetProperty(ref _failureCount, value);
    }

    /// <summary>
    /// 最后执行时间
    /// </summary>
    public DateTime LastExecutionTime
    {
        get => _lastExecutionTime;
        set => SetProperty(ref _lastExecutionTime, value);
    }

    /// <summary>
    /// 平均执行时间（毫秒）
    /// </summary>
    public double AverageExecutionTime
    {
        get => _averageExecutionTime;
        set => SetProperty(ref _averageExecutionTime, value);
    }

    /// <summary>
    /// 成功率
    /// </summary>
    public double SuccessRate => TotalExecutions > 0 
        ? (double)SuccessCount / TotalExecutions * 100 
        : 0;

    /// <summary>
    /// 记录执行
    /// </summary>
    /// <param name="success">是否成功</param>
    /// <param name="executionTimeMs">执行时间（毫秒）</param>
    public void RecordExecution(bool success, double executionTimeMs)
    {
        TotalExecutions++;
        if (success)
            SuccessCount++;
        else
            FailureCount++;

        LastExecutionTime = DateTime.Now;

        // 更新平均执行时间
        AverageExecutionTime = ((AverageExecutionTime * (TotalExecutions - 1)) + executionTimeMs) / TotalExecutions;
    }

    /// <summary>
    /// 重置统计
    /// </summary>
    public void Reset()
    {
        TotalExecutions = 0;
        SuccessCount = 0;
        FailureCount = 0;
        AverageExecutionTime = 0;
    }

    /// <summary>
    /// 克隆统计信息
    /// </summary>
    public BindingStatistics Clone()
    {
        return new BindingStatistics
        {
            TotalExecutions = TotalExecutions,
            SuccessCount = SuccessCount,
            FailureCount = FailureCount,
            LastExecutionTime = LastExecutionTime,
            AverageExecutionTime = AverageExecutionTime
        };
    }
}
