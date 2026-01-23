# Sun Eye Vision 代码重命名指南

## 已完成的更新 ✅

### Core 层
- ✅ VisionMaster.Core/Interfaces/ILogger.cs → `SunEyeVision.Interfaces`
- ✅ VisionMaster.Core/Interfaces/IConfigManager.cs → `SunEyeVision.Interfaces`
- ✅ VisionMaster.Core/Interfaces/IDeviceManager.cs → `SunEyeVision.Interfaces`
- ✅ VisionMaster.Core/Interfaces/IImageProcessor.cs → `SunEyeVision.Interfaces`
- ✅ VisionMaster.Core/Models/DeviceInfo.cs → `SunEyeVision.Models`
- ✅ VisionMaster.Core/Models/AlgorithmParameters.cs → `SunEyeVision.Models`
- ✅ VisionMaster.Core/Models/AlgorithmResult.cs → `SunEyeVision.Models`
- ✅ VisionMaster.Core/Models/Mat.cs → `SunEyeVision.Models`
- ✅ VisionMaster.Core/Services/ConsoleLogger.cs → `SunEyeVision.Services`
- ✅ VisionMaster.Core/Services/FileLogger.cs → `SunEyeVision.Services`
- ✅ VisionMaster.Core/Services/JsonConfigManager.cs → `SunEyeVision.Services`

### UI 层
- ✅ MainWindow.xaml - 标题更新为 "Sun Eye Vision"
- ✅ MainWindow.xaml.cs - 所有UI文本更新
- ✅ DocumentationWindow.xaml - 标题更新
- ✅ DocumentationWindow.xaml.cs - 所有文档文本更新

## 待完成的更新 ⚠️

### 1. VisionMaster.Algorithms (6个文件)
需要将所有文件的 `namespace VisionMaster.Algorithms` 改为 `namespace SunEyeVision.Algorithms`

文件列表:
- BaseAlgorithm.cs
- Class1.cs
- EdgeDetectionAlgorithm.cs
- GaussianBlurAlgorithm.cs
- GrayScaleAlgorithm.cs
- ThresholdAlgorithm.cs

**查找和替换规则**:
```csharp
namespace VisionMaster.Algorithms
→ namespace SunEyeVision.Algorithms

using VisionMaster.Models;
→ using SunEyeVision.Models;
```

### 2. VisionMaster.Workflow (5个文件)
需要将命名空间从 `VisionMaster.Workflow` 改为 `SunEyeVision.Workflow`

文件列表:
- AlgorithmNode.cs
- Class1.cs
- Workflow.cs
- WorkflowEngine.cs
- WorkflowNode.cs

**查找和替换规则**:
```csharp
namespace VisionMaster.Workflow
→ namespace SunEyeVision.Workflow

using VisionMaster.Models;
→ using SunEyeVision.Models;

using VisionMaster.Interfaces;
→ using SunEyeVision.Interfaces;
```

### 3. VisionMaster.DeviceDriver (4个文件)
需要将命名空间从 `VisionMaster.DeviceDriver` 改为 `SunEyeVision.DeviceDriver`

文件列表:
- BaseDeviceDriver.cs
- Class1.cs
- DeviceManager.cs
- SimulatedCameraDriver.cs

**查找和替换规则**:
```csharp
namespace VisionMaster.DeviceDriver
→ namespace SunEyeVision.DeviceDriver

using VisionMaster.Models;
→ using SunEyeVision.Models;

using VisionMaster.Interfaces;
→ using SunEyeVision.Interfaces;
```

### 4. VisionMaster.PluginSystem (3个文件)
需要将命名空间从 `VisionMaster.PluginSystem` 改为 `SunEyeVision.PluginSystem`

文件列表:
- Class1.cs
- IVisionPlugin.cs
- PluginLoader.cs

**查找和替换规则**:
```csharp
namespace VisionMaster.PluginSystem
→ namespace SunEyeVision.PluginSystem

using VisionMaster.Interfaces;
→ using SunEyeVision.Interfaces;
```

### 5. VisionMaster.UI (12个文件)
需要将命名空间从 `VisionMaster.UI` 改为 `SunEyeVision.UI`

文件列表:
- App.xaml.cs
- AssemblyInfo.cs
- MainWindow.xaml.cs (部分已更新)
- Converters/BoolToRunningTextConverter.cs
- Models/DeviceItem.cs
- Models/ToolItem.cs
- Models/WorkflowNode.cs
- ViewModels/DevicePanelViewModel.cs
- ViewModels/MainWindowViewModel.cs
- ViewModels/PropertyPanelViewModel.cs
- ViewModels/RelayCommand.cs
- ViewModels/ToolboxViewModel.cs
- ViewModels/ViewModelBase.cs
- ViewModels/WorkflowViewModel.cs
- Views/DocumentationWindow.xaml.cs (部分已更新)

**查找和替换规则**:
```csharp
namespace VisionMaster.UI
→ namespace SunEyeVision.UI

namespace VisionMaster.UI.Models
→ namespace SunEyeVision.UI.Models

namespace VisionMaster.UI.ViewModels
→ namespace SunEyeVision.UI.ViewModels

namespace VisionMaster.UI.Views
→ namespace SunEyeVision.UI.Views

namespace VisionMaster.UI.Converters
→ namespace SunEyeVision.UI.Converters

using VisionMaster.Models;
→ using SunEyeVision.Models;

using VisionMaster.Core;
→ using SunEyeVision.Core;

using VisionMaster.Algorithms;
→ using SunEyeVision.Algorithms;

using VisionMaster.Workflow;
→ using SunEyeVision.Workflow;

using VisionMaster.DeviceDriver;
→ using SunEyeVision.DeviceDriver;

using VisionMaster.PluginSystem;
→ using SunEyeVision.PluginSystem;
```

### 6. VisionMaster.Demo (1个文件)
- Program.cs

### 7. VisionMaster.Test (1个文件)
- Program.cs

### 8. XAML 文件命名空间更新 (5个文件)

需要更新的文件:
- VisionMaster.UI/App.xaml
- VisionMaster.UI/MainWindow.xaml
- VisionMaster.UI/MainWindow_Simple.xaml
- VisionMaster.UI/Views/DocumentationWindow.xaml
- VisionMaster.UI/Resources/AppResources.xaml

**查找和替换规则**:
```xml
xmlns:local="clr-namespace:VisionMaster.UI"
→ xmlns:local="clr-namespace:SunEyeVision.UI"

xmlns:vm="clr-namespace:VisionMaster.UI.ViewModels"
→ xmlns:vm="clr-namespace:SunEyeVision.UI.ViewModels"

x:Class="VisionMaster.UI.App"
→ x:Class="SunEyeVision.UI.App"

x:Class="VisionMaster.UI.Views.MainWindow"
→ x:Class="SunEyeVision.UI.Views.MainWindow"
```

### 9. 项目文件更新 (.csproj) (8个文件)

需要更新所有 .csproj 文件中的项目引用:

```xml
<ProjectReference Include="..\VisionMaster.Core\VisionMaster.Core.csproj" />
→ <ProjectReference Include="..\SunEyeVision.Core\SunEyeVision.Core.csproj" />

<ProjectReference Include="..\VisionMaster.Algorithms\VisionMaster.Algorithms.csproj" />
→ <ProjectReference Include="..\SunEyeVision.Algorithms\SunEyeVision.Algorithms.csproj" />

(其他项目引用类似)
```

### 10. 解决方案文件更新 (VisionMaster.sln)

需要更新所有项目路径和名称。

## 快速批量重命名方法

### 方法1: 使用 Visual Studio 重构
1. 在 VS 中打开解决方案
2. 对每个命名空间使用 Ctrl+R, Ctrl+R (重命名)
3. 输入新名称并预览所有更改
4. 应用更改

### 方法2: 使用 Find and Replace
1. 在 VS 中按 Ctrl+Shift+F (在文件中查找)
2. 选择"在文件中替换"
3. 依次查找并替换:
   - `VisionMaster.Algorithms` → `SunEyeVision.Algorithms`
   - `VisionMaster.Workflow` → `SunEyeVision.Workflow`
   - `VisionMaster.DeviceDriver` → `SunEyeVision.DeviceDriver`
   - `VisionMaster.PluginSystem` → `SunEyeVision.PluginSystem`
   - `VisionMaster.UI` → `SunEyeVision.UI`
   - `VisionMaster.Core` → `SunEyeVision.Core`
   - `VisionMaster.Models` → `SunEyeVision.Models`
   - `VisionMaster.Interfaces` → `SunEyeVision.Interfaces`
   - `VisionMaster.Services` → `SunEyeVision.Services`

### 方法3: 使用提供的脚本
运行之前创建的 Python 脚本:
```bash
python rename_to_suneyevision.py
```

## 完成后验证

1. 清理解决方案: `dotnet clean`
2. 重新构建: `dotnet build`
3. 检查编译错误
4. 运行程序测试功能
5. 检查所有命名空间引用是否正确

## 注意事项

⚠️ **重要提示**:
- 在执行大规模重命名之前,强烈建议先提交代码到版本控制系统
- 建议在单独的分支上测试重命名
- obj/ 和 bin/ 目录会在重新编译时自动更新,不需要手动修改
- XAML 的 x:Class 属性更改可能需要重新生成 .g.cs 文件

## 完成重命名后的文件结构

```
SunEyeVision/
├── SunEyeVision.sln
├── SunEyeVision.Algorithms/
│   ├── SunEyeVision.Algorithms.csproj
│   └── *.cs (namespace: SunEyeVision.Algorithms)
├── SunEyeVision.Core/
│   ├── SunEyeVision.Core.csproj
│   ├── Interfaces/ (namespace: SunEyeVision.Interfaces)
│   ├── Models/ (namespace: SunEyeVision.Models)
│   └── Services/ (namespace: SunEyeVision.Services)
├── SunEyeVision.DeviceDriver/
│   ├── SunEyeVision.DeviceDriver.csproj
│   └── *.cs (namespace: SunEyeVision.DeviceDriver)
├── SunEyeVision.PluginSystem/
│   ├── SunEyeVision.PluginSystem.csproj
│   └── *.cs (namespace: SunEyeVision.PluginSystem)
├── SunEyeVision.UI/
│   ├── SunEyeVision.UI.csproj
│   ├── Models/ (namespace: SunEyeVision.UI.Models)
│   ├── ViewModels/ (namespace: SunEyeVision.UI.ViewModels)
│   ├── Views/ (namespace: SunEyeVision.UI.Views)
│   └── Converters/ (namespace: SunEyeVision.UI.Converters)
├── SunEyeVision.Workflow/
│   ├── SunEyeVision.Workflow.csproj
│   └── *.cs (namespace: SunEyeVision.Workflow)
├── SunEyeVision.Demo/
│   ├── SunEyeVision.Demo.csproj
│   └ Program.cs
└── SunEyeVision.Test/
    ├── SunEyeVision.Test.csproj
    └ Program.cs
```
