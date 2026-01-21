# Sun Eye Vision 重命名说明

## 已完成的更改

### UI 文本更新
✅ MainWindow.xaml
  - Title: "VisionMaster - 机器视觉平台" → "Sun Eye Vision - 机器视觉平台"

✅ MainWindow.xaml.cs
  - 注释: "VisionMaster风格的主界面窗口" → "Sun Eye Vision风格的主界面窗口"
  - 关于窗口标题: "关于 VisionMaster" → "关于 Sun Eye Vision"
  - 标题文字: " VisionMaster" → " Sun Eye Vision"
  - 平台名称: "VisionMaster 机器视觉平台" → "Sun Eye Vision 机器视觉平台"
  - 版权信息: "VisionMaster Team" → "Sun Eye Vision Team"

✅ DocumentationWindow.xaml
  - Title: "VisionMaster - 文档中心" → "Sun Eye Vision - 文档中心"
  - 标题文字: " VisionMaster 文档中心" → " Sun Eye Vision 文档中心"

✅ DocumentationWindow.xaml.cs
  - 文档标题1: "VisionMaster 开发计划" → "Sun Eye Vision 开发计划"
  - 文档描述: "VisionMaster 是一个..." → "Sun Eye Vision 是一个..."
  - 文档标题2: "VisionMaster 文件架构说明" → "Sun Eye Vision 文件架构说明"
  - 文档标题3: "VisionMaster 快速入门" → "Sun Eye Vision 快速入门"
  - 打印文档名: "VisionMaster 文档" → "Sun Eye Vision 文档"

## 待完成的更改

由于项目规模较大，以下更改需要手动执行或使用脚本完成：

### 1. 命名空间重命名 (需要重命名所有 .cs 文件)
- `VisionMaster` → `SunEyeVision`
- `VisionMaster.Algorithms` → `SunEyeVision.Algorithms`
- `VisionMaster.Core` → `SunEyeVision.Core`
- `VisionMaster.UI` → `SunEyeVision.UI`
- `VisionMaster.Workflow` → `SunEyeVision.Workflow`
- `VisionMaster.DeviceDriver` → `SunEyeVision.DeviceDriver`
- `VisionMaster.PluginSystem` → `SunEyeVision.PluginSystem`
- `VisionMaster.Interfaces` → `SunEyeVision.Interfaces`
- `VisionMaster.Models` → `SunEyeVision.Models`
- `VisionMaster.Services` → `SunEyeVision.Services`

### 2. 文件夹重命名
- `VisionMaster.Algorithms/` → `SunEyeVision.Algorithms/`
- `VisionMaster.Core/` → `SunEyeVision.Core/`
- `VisionMaster.Demo/` → `SunEyeVision.Demo/`
- `VisionMaster.DeviceDriver/` → `SunEyeVision.DeviceDriver/`
- `VisionMaster.PluginSystem/` → `SunEyeVision.PluginSystem/`
- `VisionMaster.Test/` → `SunEyeVision.Test/`
- `VisionMaster.UI/` → `SunEyeVision.UI/`
- `VisionMaster.Workflow/` → `SunEyeVision.Workflow/`

### 3. 文件重命名
- `VisionMaster.sln` → `SunEyeVision.sln`
- 所有 `.csproj` 文件中的项目名称

### 4. 项目引用更新
- 所有 `.csproj` 文件中的 ProjectReference 路径

### 5. XAML 命名空间引用更新
- `x:Class` 属性
- `xmlns:local` 和 `xmlns:vm` 等命名空间声明

### 6. 其他文件
- `UsageExample.cs`
- `VisionMaster.Demo/Program.cs`
- `VisionMaster.Test/Program.cs`
- `tools/PythonService/README.md`
- `tools/PythonService/main.py`
- `fix_logs.py`

## 重命名工具

已提供两个重命名脚本：

1. `rename_to_suneyevision.py` - Python 脚本（推荐）
2. `rename_to_suneyevision.ps1` - PowerShell 脚本

运行脚本可以自动完成所有重命名操作：
```bash
# 如果系统有 Python
python rename_to_suneyevision.py

# 或使用 PowerShell
powershell -ExecutionPolicy Bypass -File rename_to_suneyevision.ps1
```

## 验证步骤

运行脚本后：
1. 重新构建项目
2. 检查编译错误
3. 运行程序验证功能正常
4. 检查文档显示是否正确

## 注意事项

⚠️ 在执行完整重命名之前，建议：
1. 提交当前代码到版本控制系统
2. 备份整个项目
3. 在分支上测试重命名脚本
4. 确认无问题后再在主分支执行
