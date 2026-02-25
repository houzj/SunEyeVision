using SunEyeVision.Plugin.Abstractions;
using SunEyeVision.Plugin.Abstractions.Models;

namespace SamplePlugin;

/// <summary>
/// 示例图像处理器插件
/// 演示如何创建一个简单的图像处理插件
/// </summary>
public class SampleImageProcessor : IImageProcessor
{
    public string Name => "示例图像处理器";
    public string Description => "演示插件开发流程的示例图像处理器";
    public string Version => "1.0.0";
    public string Author => "SunEyeVision Team";

    public IEnumerable<ParameterMetadata> GetParameters()
    {
        return new[]
        {
            new ParameterMetadata
            {
                Name = "Brightness",
                DisplayName = "亮度调整",
                Description = "调整图像亮度，范围 -100 到 100",
                Type = ParameterType.Integer,
                DefaultValue = 0,
                MinValue = -100,
                MaxValue = 100
            },
            new ParameterMetadata
            {
                Name = "Contrast",
                DisplayName = "对比度调整",
                Description = "调整图像对比度，范围 -100 到 100",
                Type = ParameterType.Integer,
                DefaultValue = 0,
                MinValue = -100,
                MaxValue = 100
            }
        };
    }

    public AlgorithmResult Process(AlgorithmParameters parameters)
    {
        try
        {
            // 获取输入图像
            var inputImage = parameters.GetInputImage();
            if (inputImage == null)
            {
                return AlgorithmResult.CreateError("输入图像不能为空");
            }

            // 获取参数
            var brightness = parameters.GetParameter<int>("Brightness", 0);
            var contrast = parameters.GetParameter<int>("Contrast", 0);

            // TODO: 实现实际的图像处理逻辑
            // 这里仅作演示，实际应调用图像处理库

            return AlgorithmResult.CreateSuccess(new Dictionary<string, object>
            {
                ["OutputImage"] = inputImage,
                ["ProcessingTime"] = DateTime.Now
            });
        }
        catch (Exception ex)
        {
            return AlgorithmResult.CreateError($"处理失败: {ex.Message}");
        }
    }

    public bool ValidateParameters(AlgorithmParameters parameters)
    {
        var brightness = parameters.GetParameter<int>("Brightness", 0);
        var contrast = parameters.GetParameter<int>("Contrast", 0);

        return brightness >= -100 && brightness <= 100 &&
               contrast >= -100 && contrast <= 100;
    }

    public void Initialize() { }
    public void Dispose() { }
}
