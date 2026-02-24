# SunEyeVision 插件模板

简化的插件开发模板，支持离线独立开发。

## 🚀 快速开始（离线模式）

### 第一步：导出SDK

```batch
# 双击运行
setup_sdk.bat
```

这会自动：
1. 编译 SDK 项目
2. 复制 DLL 到 `lib/` 目录
3. 复制 XML 文档（智能提示）

### 第二步：开发插件

编辑 `MyTool.cs`，实现你的算法逻辑。

### 第三步：编译部署

```batch
# 编译
build.bat

# 部署到主程序
deploy.bat
```

---

## 📦 三种开发模式

| 模式 | 适用场景 | 特点 |
|------|---------|------|
| **DLL引用** ★ | 离线开发、完全隔离 | 无需主项目源码 |
| NuGet包 | 在线独立开发 | 标准包管理 |
| 项目引用 | 内部集成开发 | 调试方便 |

### 切换模式方法

编辑 `MyPlugin.Template.csproj`，注释/取消注释对应段落。

---

## 📁 目录结构

```
MyPlugin.Template/
├── lib/                           # SDK DLL（运行setup_sdk.bat生成）
│   ├── SunEyeVision.Plugin.SDK.dll
│   └── SunEyeVision.Plugin.SDK.xml
├── setup_sdk.bat                  # 导出SDK
├── build.bat                      # 编译插件
├── deploy.bat                     # 部署插件
├── MyPlugin.Template.csproj       # 项目配置
├── MyTool.cs                      # 插件实现
└── README.md                      # 本文档
```

---

## 🔧 完全隔离开发

离线模式优势：

1. **零依赖** - 不需要主项目源码
2. **可移植** - 整个目录可拷贝到任意位置
3. **版本锁定** - SDK版本固定不变
4. **快速上手** - 只需一个DLL即可开发

### 分发模板

将以下文件打包即可分发给其他开发者：

```
MyPlugin.Template/
├── lib/
│   ├── SunEyeVision.Plugin.SDK.dll
│   └── SunEyeVision.Plugin.SDK.xml
├── build.bat
├── deploy.bat
├── MyPlugin.Template.csproj
├── MyTool.cs
└── README.md
```

无需任何外部依赖！

---

## 核心接口

| 接口 | 说明 |
|------|------|
| `IToolPlugin` | 插件主接口，定义工具元数据和实例创建 |
| `IImageProcessor` | 图像处理器接口，实现处理逻辑 |
| `AlgorithmParameters` | 参数容器，使用 Get/Set 方法 |
| `ToolMetadata` | 工具元数据，定义输入输出参数 |
| `ValidationResult` | 参数验证结果 |

## 更多示例

参考现有插件实现：
- `src/Plugins/` - 内置插件目录
