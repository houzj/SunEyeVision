# SunEyeVision Plugin Development Kit

独立插件开发工具包，可分发给插件开发人员进行并行开发。

## 目录结构

```
PluginDevKit/
├── Directory.Build.props    # 统一构建配置
├── sdk/                     # SDK 文件（需要从主项目复制）
│   └── net9.0/
│       ├── SunEyeVision.Plugin.SDK.dll
│       ├── SunEyeVision.Plugin.Abstractions.dll
│       └── ...
├── runtime/                 # 主程序运行时文件（用于调试）
│   ├── SunEyeVision.exe
│   ├── SunEyeVision.dll
│   └── ...
├── scripts/                 # 构建脚本
│   ├── build-sdk.ps1        # 构建 SDK
│   ├── build-runtime.ps1    # 构建 Runtime
│   └── copy-sdk.ps1         # 复制 SDK 到此处
├── samples/                 # 示例插件
│   └── SamplePlugin/
└── templates/               # 插件模板
    └── MyPlugin.Template/
```

## 快速开始

### 1. 准备 SDK

从主项目构建并复制 SDK：

```powershell
# 在主项目根目录执行
.\scripts\prepare-devkit.ps1
```

### 2. 创建新插件

```powershell
# 复制模板
Copy-Item -Recurse templates\MyPlugin.Template samples\MyNewPlugin

# 重命名项目文件
Rename-Item samples\MyNewPlugin\MyPlugin.Template.csproj MyNewPlugin.csproj
```

### 3. 构建和调试

```powershell
# 构建插件
dotnet build samples\MyNewPlugin

# 启动主程序进行调试
.\scripts\launch-debug.ps1
```

## 开发模式

### 本地 DLL 引用（默认）

直接引用 `sdk/` 目录下的 DLL 文件，适合内部开发。

### NuGet 包引用

设置环境变量切换到 NuGet 模式：

```powershell
$env:UseNuGet = "true"
dotnet build
```

## 分发说明

将整个 `PluginDevKit` 目录打包成 ZIP 分发给插件开发者。

开发者只需要：
1. 安装 .NET 9.0 SDK
2. 解压到任意目录
3. 开始插件开发

## 配置说明

### Directory.Build.props

| 属性 | 说明 | 默认值 |
|------|------|--------|
| `SdkPath` | SDK 文件路径 | `sdk/net9.0/` |
| `OutputPath` | 插件输出路径 | `runtime/plugins/` |
| `UseNuGet` | 使用 NuGet 包 | `false` |
