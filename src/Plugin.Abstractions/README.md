# SunEyeVision Plugin SDK

零依赖的纯契约层，用于独立开发 SunEyeVision 插件。

## 安装

### 方式一：本地NuGet源（推荐用于内部开发）

1. 配置本地NuGet源（首次使用）：
```bash
dotnet nuget add source "C:\path\to\SunEyeVision\nupkg" -n SunEyeVisionLocal
```

2. 在项目中添加包引用：
```bash
dotnet add package SunEyeVision.Plugin.SDK
```

### 方式二：直接DLL引用

将编译后的 `SunEyeVision.Plugin.Abstractions.dll` 复制到你的项目目录，然后添加引用。

## 核心接口

### IImageProcessor - 图像处理器接口

```csharp
using SunEyeVision.Plugin.Abstractions.Core;

public class MyImageProcessor : IImageProcessor
{
    public string Name => "MyImageProcessor";
    public string Description => "自定义图像处理器";
    public string Version => "1.0.0";

    public AlgorithmResult Execute(AlgorithmParameters parameters)
    {
        // 实现你的图像处理逻辑
        var image = parameters.GetImage("InputImage");
        // ... 处理图像 ...

        var result = new AlgorithmResult();
        result.SetOutput("ProcessedImage", processedImage);
        return result;
    }

    public IEnumerable<ParameterMetadata> GetParameterDefinitions()
    {
        yield return new ParameterMetadata
        {
            Name = "InputImage",
            Type = ParameterType.Image,
            IsRequired = true,
            Description = "输入图像"
        };
    }
}
```

### IToolPlugin - 工具插件接口

```csharp
using SunEyeVision.Plugin.Abstractions;

public class MyToolPlugin : IToolPlugin
{
    public ToolMetadata Metadata => new ToolMetadata
    {
        Name = "MyTool",
        Category = "ImageProcessing",
        Description = "自定义图像处理工具",
        Version = "1.0.0"
    };

    public IImageProcessor CreateProcessor()
    {
        return new MyImageProcessor();
    }

    public IEnumerable<ParameterMetadata> GetParameters()
    {
        // 返回工具参数定义
    }

    public ValidationResult ValidateParameters(Dictionary<string, object> parameters)
    {
        // 验证参数
        return ValidationResult.Success();
    }
}
```

## 命名空间

- `SunEyeVision.Plugin.Abstractions` - 核心插件接口
- `SunEyeVision.Plugin.Abstractions.Core` - 图像处理核心接口
- `SunEyeVision.Plugin.Abstractions.ViewModels` - ViewModel基类

## 项目配置

创建插件项目的 `.csproj` 文件：

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="SunEyeVision.Plugin.SDK" Version="1.0.0" />
  </ItemGroup>
</Project>
```

## 插件部署

将编译后的插件DLL复制到 SunEyeVision 的 `plugins` 目录即可自动加载。

## 版本兼容性

| SDK版本 | SunEyeVision版本 |
|---------|------------------|
| 1.0.0   | 1.0.x            |

## 许可证

MIT License
