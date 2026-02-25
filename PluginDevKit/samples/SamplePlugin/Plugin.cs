using SunEyeVision.Plugin.Abstractions;

namespace SamplePlugin;

/// <summary>
/// 插件入口点
/// 实现IPlugin接口定义插件基本信息
/// </summary>
public class Plugin : IPlugin
{
    public string Id => "SamplePlugin";
    public string Name => "示例插件";
    public string Description => "用于演示插件开发流程的示例插件";
    public string Version => "1.0.0";
    public string Author => "SunEyeVision Team";

    public IEnumerable<IImageProcessor> GetImageProcessors()
    {
        return new[] { new SampleImageProcessor() };
    }

    public void Initialize()
    {
        // 插件初始化逻辑
    }

    public void Dispose()
    {
        // 清理资源
    }
}
