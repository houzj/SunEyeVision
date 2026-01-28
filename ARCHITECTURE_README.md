# SunEyeVision 架构实施完成

## 快速开始

SunEyeVision 项目已完成框架-插件架构的实施。新架构支持多人协作开发，提供灵活的UI模式，并实现了框架与插件的高度解耦。

## 核心特性

### 🎯 三种UI模式
- **Auto模式**：零代码UI，框架自动生成（推荐简单插件）
- **Hybrid模式**：使用框架通用控件 + 自定义面板（推荐中等复杂插件）
- **Custom模式**：完全自定义UI界面（推荐复杂插件）

### 🔌 插件系统
- 统一管理，不区分内置/自定义插件
- 支持插件元数据（plugin.json）
- 自动加载和生命周期管理
- 智能UI适配器

### 📦 共享UI组件
- GenericPropertyGrid - 通用属性网格
- GenericParameterPanel - 通用参数面板
- ImageVisualizationPanel - 图像可视化面板
- ProgressPanel - 进度面板
- StatusIndicator - 状态指示器

### 🐛 调试系统
- 共享调试控件
- 插件调试支持
- 断点和变量监视（开发中）

## 文档

### 📚 重要文档
- **NEW_PROJECT_STRUCTURE.md** - 完整的文件结构说明（详细）
- **IMPLEMENTATION_GUIDE.md** - 实施指南和使用说明
- **ARCHITECTURE_README.md** - 本文档（快速参考）

### 📁 目录结构
```
SunEyeVision/
├── Framework Layer
│   ├── SunEyeVision.Core/          # 核心接口和PluginManager
│   └── SunEyeVision.UI/            # UI框架和共享组件
│
└── Plugin Layer
    └── Plugins/                     # 所有插件
        ├── ImageProcessing/         # Auto模式示例
        ├── Workflow/                # Hybrid模式示例
        └── CustomFilters/           # Custom模式示例
```

## 快速上手

### 1. 开发新插件

```csharp
using SunEyeVision.Core.Interfaces.Plugins;

public class MyPlugin : IAlgorithmPlugin
{
    // IPlugin成员
    public string PluginId => "MyPlugin";
    public string PluginName => "My Plugin";
    public string Version => "1.0.0";
    public string Description => "My plugin description";
    public string Author => "My Team";

    // IAlgorithmPlugin成员
    public string AlgorithmType => "MyAlgorithm";
    public string Icon => "icon.png";
    public string Category => "My Category";

    public void Initialize() { }
    public void Start() { }
    public void Stop() { }
    public void Cleanup() { }

    public ParameterMetadata[] GetParameters()
    {
        return new[]
        {
            new ParameterMetadata
            {
                Name = "param1",
                DisplayName = "Parameter 1",
                Type = "int",
                DefaultValue = 10
            }
        };
    }

    public object Execute(object input, Dictionary<string, object> parameters)
    {
        // 实现插件逻辑
        return input;
    }

    public bool ValidateParameters(Dictionary<string, object> parameters)
    {
        return true;
    }
}
```

### 2. 创建plugin.json

```json
{
  "pluginId": "MyPlugin",
  "pluginName": "My Plugin",
  "version": "1.0.0",
  "description": "My plugin description",
  "author": "My Team"
}
```

### 3. 编译和部署

```bash
# 编译插件
dotnet build MyPlugin.csproj

# 将生成的DLL和plugin.json复制到输出目录的Plugins/MyPlugin/
```

## 示例插件

### ImageProcessingPlugin（Auto模式）
- 位置：`Plugins/ImageProcessing/`
- 特点：零代码UI，自动生成界面
- 适用：简单算法插件

### WorkflowPlugin（Hybrid模式）
- 位置：`Plugins/Workflow/`
- 特点：使用框架通用控件 + 自定义面板
- 适用：需要部分自定义的插件

### CustomFiltersPlugin（Custom模式）
- 位置：`Plugins/CustomFilters/`
- 特点：完全自定义UI
- 适用：复杂交互插件

## 技术栈

- **框架**：.NET 9.0
- **UI**：WPF
- **序列化**：System.Text.Json
- **架构模式**：插件架构 + 适配器模式

## 主要组件

### 核心接口
- `IPlugin` - 插件基础接口（必须实现）
- `IPluginUIProvider` - UI提供者接口（可选）
- `INodePlugin` - 节点插件接口
- `IAlgorithmPlugin` - 算法插件接口

### 核心服务
- `PluginManager` - 插件管理器
- `PluginUIAdapter` - UI适配器
- `DebugControlManager` - 调试控制管理器
- `PanelManager` - 面板管理器

### 共享UI组件
- `GenericPropertyGrid` - 通用属性网格
- `GenericParameterPanel` - 通用参数面板
- `ImageVisualizationPanel` - 图像可视化面板
- `ProgressPanel` - 进度面板
- `StatusIndicator` - 状态指示器

## 架构优势

✅ **高度解耦**：框架与插件通过接口通信，互不依赖
✅ **灵活扩展**：支持三种UI模式，满足不同需求
✅ **团队协作**：插件隔离，多人开发互不干扰
✅ **代码复用**：共享UI组件，减少重复开发
✅ **易于维护**：清晰的分层结构，职责明确

## 下一步

- [ ] 完善共享UI组件（NumericUpDown、更多可视化组件）
- [ ] 增强PluginManager（依赖检查、热加载）
- [ ] 完善调试系统（断点、变量监视）
- [ ] 创建更多示例插件
- [ ] 开发插件生成器工具
- [ ] 建立插件市场

## 贡献

欢迎贡献代码、提出建议或报告问题！

## 许可证

[待添加]

---

**版本**：1.0.0
**最后更新**：2026-01-28
**维护者**：SunEyeVision Team
