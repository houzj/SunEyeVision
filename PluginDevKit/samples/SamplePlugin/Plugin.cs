using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OpenCvSharp;
using SunEyeVision.Plugin.SDK;
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.SDK.Validation;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.SDK.Execution.Results;
using SunEyeVision.Plugin.SDK.Metadata;

namespace SamplePlugin;

/// <summary>
/// 示例插件入口点
/// 实现 IToolPlugin 接口定义插件基本信息和工具列表
/// </summary>
[ToolPlugin("sample-plugin", "SamplePlugin")]
public class Plugin : IToolPlugin
{
    private List<ToolMetadata>? _cachedMetadata;

    #region 插件基本信息

    public string Name => "示例插件";
    public string Version => "1.0.0";
    public string Author => "SunEyeVision Team";
    public string Description => "用于演示插件开发流程的示例插件";
    public string PluginId => "suneyevision.sample-plugin";
    public string Icon => "🔧";
    public List<string> Dependencies => new List<string>();
    public bool IsLoaded { get; private set; }

    #endregion

    #region 生命周期管理

    public void Initialize() => IsLoaded = true;
    public void Unload() => IsLoaded = false;

    #endregion

    #region 工具管理

    /// <summary>
    /// 获取工具元数据
    /// </summary>
    public List<ToolMetadata> GetToolMetadata()
    {
        if (_cachedMetadata != null)
            return _cachedMetadata;

        _cachedMetadata = new List<ToolMetadata>
        {
            new ToolMetadata
            {
                Id = "sample-tool",
                Name = "SampleTool",
                DisplayName = "示例工具",
                Icon = "🔧",
                Category = "示例",
                Description = "一个简单的示例工具，执行二值化处理",
                AlgorithmType = typeof(SampleTool),
                Version = Version,
                Author = Author,
                HasDebugInterface = false,
                InputParameters = GenerateInputParameterMetadata(),
                OutputParameters = GenerateOutputParameterMetadata(),
                SupportsDataBinding = true,
                ParameterType = typeof(SampleToolParameters),
                ResultType = typeof(SampleToolResult)
            }
        };

        return _cachedMetadata;
    }

    /// <summary>
    /// 创建工具实例
    /// </summary>
    public ITool? CreateToolInstance(string toolId)
    {
        return toolId == "sample-tool" ? new SampleTool() : null;
    }

    /// <summary>
    /// 获取默认参数
    /// </summary>
    public AlgorithmParameters GetDefaultParameters(string toolId)
    {
        var result = new AlgorithmParameters();
        result.Set("threshold", 128.0);
        result.Set("enableBlur", true);
        result.Set("blurKernelSize", 5);
        return result;
    }

    #endregion

    #region 元数据生成

    private List<ParameterMetadata> GenerateInputParameterMetadata()
    {
        return new List<ParameterMetadata>
        {
            new ParameterMetadata
            {
                Name = "Threshold",
                DisplayName = "阈值",
                Description = "二值化阈值(0-255)",
                Type = ParamDataType.Double,
                DefaultValue = 128.0,
                MinValue = 0,
                MaxValue = 255,
                Required = true,
                Category = "基本参数",
                EditableInDebug = true,
                SupportsBinding = true
            },
            new ParameterMetadata
            {
                Name = "EnableBlur",
                DisplayName = "启用模糊",
                Description = "是否在阈值化前应用高斯模糊",
                Type = ParamDataType.Bool,
                DefaultValue = true,
                Required = false,
                Category = "高级参数",
                EditableInDebug = true,
                SupportsBinding = true
            },
            new ParameterMetadata
            {
                Name = "BlurKernelSize",
                DisplayName = "模糊核大小",
                Description = "高斯模糊核大小(奇数)",
                Type = ParamDataType.Int,
                DefaultValue = 5,
                MinValue = 1,
                MaxValue = 31,
                Required = false,
                Category = "高级参数",
                EditableInDebug = true,
                SupportsBinding = true
            }
        };
    }

    private List<ParameterMetadata> GenerateOutputParameterMetadata()
    {
        return new List<ParameterMetadata>
        {
            new ParameterMetadata
            {
                Name = "ProcessedImage",
                DisplayName = "处理后的图像",
                Description = "二值化后的图像",
                Type = ParamDataType.Image
            },
            new ParameterMetadata
            {
                Name = "ProcessedPixels",
                DisplayName = "处理像素数",
                Description = "处理的像素总数",
                Type = ParamDataType.Int
            },
            new ParameterMetadata
            {
                Name = "WhitePixelCount",
                DisplayName = "白色像素数",
                Description = "结果图像中白色像素的数量",
                Type = ParamDataType.Int
            }
        };
    }

    #endregion
}
