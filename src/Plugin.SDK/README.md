# SunEyeVision Plugin SDK

闆朵緷璧栫殑绾绾﹀眰锛岀敤浜庣嫭绔嬪紑鍙?SunEyeVision 鎻掍欢銆?
## 瀹夎

### 鏂瑰紡涓€锛氭湰鍦癗uGet婧愶紙鎺ㄨ崘鐢ㄤ簬鍐呴儴寮€鍙戯級

1. 閰嶇疆鏈湴NuGet婧愶紙棣栨浣跨敤锛夛細
```bash
dotnet nuget add source "C:\path\to\SunEyeVision\nupkg" -n SunEyeVisionLocal
```

2. 鍦ㄩ」鐩腑娣诲姞鍖呭紩鐢細
```bash
dotnet add package SunEyeVision.Plugin.SDK
```

### 鏂瑰紡浜岋細鐩存帴DLL寮曠敤

灏嗙紪璇戝悗鐨?`SunEyeVision.Plugin.SDK.dll` 澶嶅埗鍒颁綘鐨勯」鐩洰褰曪紝鐒跺悗娣诲姞寮曠敤銆?
## 鏍稿績鎺ュ彛

### ITool<TParams, TResult> - 娉涘瀷宸ュ叿鎺ュ彛锛堟帹鑽愶級

```csharp
using SunEyeVision.Plugin.SDK.Interfaces;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.SDK.Execution.Results;
using SunEyeVision.Plugin.SDK.Models.Imaging;
using SunEyeVision.Plugin.SDK.Validation;

// 瀹氫箟寮虹被鍨嬪弬鏁?public class CircleFindParams : ToolParameters
{
    [ParameterRange(0.1, 10000.0, Step = 0.1, Unit = "鍍忕礌")]
    [ParameterDisplay(DisplayName = "鏈€灏忓崐寰?, Description = "妫€娴嬪渾鐨勬渶灏忓崐寰?)]
    public double MinRadius { get; set; } = 5.0;

    [ParameterRange(0.1, 10000.0, Step = 0.1, Unit = "鍍忕礌")]
    [ParameterDisplay(DisplayName = "鏈€澶у崐寰?, Description = "妫€娴嬪渾鐨勬渶澶у崐寰?)]
    public double MaxRadius { get; set; } = 100.0;

    public override ValidationResult Validate()
    {
        var result = base.Validate();
        if (MinRadius > MaxRadius)
            result.AddError("鏈€灏忓崐寰勪笉鑳藉ぇ浜庢渶澶у崐寰?);
        return result;
    }
}

// 瀹氫箟寮虹被鍨嬬粨鏋?public class CircleFindResult : ToolResults
{
    public Circle FoundCircle { get; set; }
    public double Score { get; set; }

    public override IEnumerable<VisualElement> GetVisualElements()
    {
        yield return VisualElement.Circle(FoundCircle, 0xFF00FF00, 2.0);
    }
}

// 瀹炵幇宸ュ叿
public class CircleFindTool : ITool<CircleFindParams, CircleFindResult>
{
    public string Name => "CircleFind";
    public string Description => "鍦ㄥ浘鍍忎腑鏌ユ壘鍦嗗舰";
    public string Version => "1.0.0";
    public string Category => "鍑犱綍妫€娴?;

    public CircleFindResult Execute(Mat image, CircleFindParams parameters)
    {
        // 实现算法逻辑
        return new CircleFindResult { /* ... */ };
    }

    public Task<CircleFindResult> ExecuteAsync(Mat image, CircleFindParams parameters)
        => Task.FromResult(Execute(image, parameters));

    public ValidationResult ValidateParameters(CircleFindParams parameters)
        => parameters.Validate();

    public CircleFindParams GetDefaultParameters() => new();
}
```

### IToolPlugin - 鎻掍欢鎺ュ彛

```csharp
using SunEyeVision.Plugin.SDK.Plugin;
using SunEyeVision.Plugin.SDK.Metadata;

public class MyToolPlugin : IToolPlugin
{
    public string Name => "MyTools";
    public string Version => "1.0.0";
    public string PluginId => "my-company.my-tools";
    public string Description => "鎴戠殑宸ュ叿闆嗗悎";
    public string Icon => "馃敡";
    public string Author => "MyCompany";
    public List<string> Dependencies => new();
    public bool IsLoaded { get; private set; }

    public void Initialize()
    {
        // 鍒濆鍖栨彃浠?        IsLoaded = true;
    }

    public void Unload()
    {
        // 娓呯悊璧勬簮
        IsLoaded = false;
    }

    public List<ToolMetadata> GetToolMetadata()
    {
        return new List<ToolMetadata>
        {
            new ToolMetadata
            {
                Id = "circle-find",
                Name = "CircleFind",
                DisplayName = "鍦嗘煡鎵?,
                Description = "鍦ㄥ浘鍍忎腑妫€娴嬪渾褰?,
                Category = "鍑犱綍妫€娴?
            }
        };
    }

    public List<Type> GetAlgorithmNodes()
    {
        return new List<Type> { typeof(CircleFindTool) };
    }
}
```

## 鐩綍缁撴瀯

```
Plugin.SDK/
鈹溾攢鈹€ Contracts/                # 濂戠害鎺ュ彛
鈹?  鈹斺攢鈹€ ITool.cs
鈹溾攢鈹€ Models/                   # 鏁版嵁妯″瀷
鈹?  鈹溾攢鈹€ Geometry/             # 鍑犱綍绫诲瀷锛圥ointD, Circle, Line绛夛級
鈹?  鈹溾攢鈹€ Imaging/              # 鍥惧儚绫诲瀷锛圛mageData, PixelFormat锛?鈹?  鈹溾攢鈹€ Roi/                  # ROI绫诲瀷锛圛Roi, CircleRoi绛夛級
鈹?  鈹斺攢鈹€ Visualization/        # 鍙鍖栫被鍨嬶紙VisualElement锛?鈹溾攢鈹€ Execution/                # 鎵ц鐩稿叧
鈹?  鈹溾攢鈹€ Parameters/           # 鍙傛暟妯″瀷
鈹?  鈹?  鈹斺攢鈹€ ToolParameters.cs # 鍙傛暟鍩虹被
鈹?  鈹斺攢鈹€ Results/              # 缁撴灉妯″瀷
鈹?      鈹溾攢鈹€ ToolResults.cs    # 缁撴灉鍩虹被
鈹?      鈹斺攢鈹€ ExecutionProgress.cs
鈹溾攢鈹€ Plugin/                   # 鎻掍欢绯荤粺
鈹?  鈹斺攢鈹€ IToolPlugin.cs        # 鎻掍欢鎺ュ彛
鈹溾攢鈹€ Metadata/                 # 鍏冩暟鎹被鍨?鈹?  鈹溾攢鈹€ ToolMetadata.cs
鈹?  鈹斺攢鈹€ ParameterMetadata.cs
鈹溾攢鈹€ Validation/               # 楠岃瘉绫诲瀷
鈹?  鈹斺攢鈹€ ValidationResult.cs
鈹溾攢鈹€ Interfaces/               # 宸ュ叿鎺ュ彛
鈹?  鈹斺攢鈹€ ITool.cs
鈹斺攢鈹€ Samples/                  # 绀轰緥浠ｇ爜
    鈹斺攢鈹€ CircleFindTool.cs
```

## 鍛藉悕绌洪棿

| 鍛藉悕绌洪棿 | 璇存槑 |
|---------|------|
| `SunEyeVision.Plugin.SDK.Plugin` | 鎻掍欢鎺ュ彛 |
| `SunEyeVision.Plugin.SDK.Interfaces` | 娉涘瀷宸ュ叿鎺ュ彛 |
| `SunEyeVision.Plugin.SDK.Models.Geometry` | 鍑犱綍绫诲瀷 |
| `SunEyeVision.Plugin.SDK.Models.Imaging` | 鍥惧儚绫诲瀷 |
| `SunEyeVision.Plugin.SDK.Models.Roi` | ROI绫诲瀷 |
| `SunEyeVision.Plugin.SDK.Models.Visualization` | 鍙鍖栫被鍨?|
| `SunEyeVision.Plugin.SDK.Execution.Parameters` | 鍙傛暟鍩虹被 |
| `SunEyeVision.Plugin.SDK.Execution.Results` | 缁撴灉鍩虹被 |
| `SunEyeVision.Plugin.SDK.Metadata` | 鍏冩暟鎹被鍨?|
| `SunEyeVision.Plugin.SDK.Validation` | 楠岃瘉绫诲瀷 |

## 椤圭洰閰嶇疆

鍒涘缓鎻掍欢椤圭洰鐨?`.csproj` 鏂囦欢锛?
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="SunEyeVision.Plugin.SDK" Version="1.0.1" />
  </ItemGroup>
</Project>
```

## 鎻掍欢閮ㄧ讲

灏嗙紪璇戝悗鐨勬彃浠禗LL澶嶅埗鍒?SunEyeVision 鐨?`plugins` 鐩綍鍗冲彲鑷姩鍔犺浇銆?
## 鐗堟湰鍏煎鎬?
| SDK鐗堟湰 | SunEyeVision鐗堟湰 | 璇存槑 |
|---------|------------------|------|
| 1.0.0   | 1.0.x            | 鍒濆鐗堟湰 |
| 1.0.1   | 1.0.x            | 閲嶆瀯鐗堟湰锛岀幇浠ｅ寲鏋舵瀯 |

## 鍙樻洿璁板綍

### v1.0.1
- 閲嶆瀯鐩綍缁撴瀯锛欳ore 鈫?Models锛孭arams/Results 鈫?Execution
- 鍒犻櫎鏃х増鎺ュ彛锛欼ImageProcessor, AlgorithmParameters, AlgorithmResult
- 鍒犻櫎鏃х増鍖呰鍣細IToolWrapper, GenericToolWrapper, LegacyToolWrapper
- 鍒犻櫎鏃х増鍩虹被锛歍oolPluginBase, ImageProcessorBase
- 鍒犻櫎鏃х増ViewModel锛歍oolDebugViewModelBase
- 閲嶅懡鍚嶏細ToolParamsBase 鈫?ToolParameters, ToolResultsBase 鈫?ToolResults
- 鐜颁唬鍖栧懡鍚嶇┖闂达紝鏇寸鍚?NET鏈€浣冲疄璺?
## 璁稿彲璇?
MIT License
