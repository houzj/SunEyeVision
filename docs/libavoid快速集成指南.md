# libavoid 快速集成指南

本指南提供快速完成 libavoid 集成的步骤。

## 🚀 快速开始（5 分钟）

### 步骤 1: 在 Visual Studio 中打开解决方案

1. 打开 `SunEyeVision.sln`
2. 确认解决方案加载成功

### 步骤 2: 添加 C++/CLI 项目到解决方案

1. 在解决方案资源管理器中，右键点击解决方案
2. 选择 **添加** → **现有项目**
3. 导航到 `SunEyeVision.LibavoidWrapper` 文件夹
4. 选择 `SunEyeVision.LibavoidWrapper.vcxproj`
5. 点击 **打开**

### 步骤 3: 配置项目属性

1. 右键点击 `SunEyeVision.LibavoidWrapper` 项目
2. 选择 **属性**
3. 在 **配置** 下拉框中选择 **所有配置**
4. 在 **平台** 下拉框中选择 **所有平台**
5. 确认以下设置：
   - **配置类型**: 动态库 (.dll)
   - **公共语言运行时支持**: 公共语言运行时支持 (/clr)
   - **字符集**: 使用多字节字符集

### 步骤 4: 设置平台目标

1. 在解决方案资源管理器中，右键点击解决方案
2. 选择 **属性**
3. 在 **配置** 下拉框中选择 **Release**
4. 在 **平台** 下拉框中选择 **x64**
5. 点击 **配置管理器**
6. 确保所有项目都配置为 **x64** 平台
7. 点击 **关闭**

### 步骤 5: 构建项目

1. 在解决方案资源管理器中，右键点击 `SunEyeVision.LibavoidWrapper` 项目
2. 选择 **生成**
3. 等待构建完成
4. 确认没有错误

### 步骤 6: 测试集成

1. 在 `SunEyeVision.UI` 项目中，添加测试代码：

```csharp
using SunEyeVision.Algorithms.PathPlanning;
using System.Windows;

// 在适当的位置添加测试代码
var calculator = new LibavoidPathCalculator();
var path = calculator.CalculateOrthogonalPath(
    new Point(100, 100),
    new Point(300, 300)
);

Console.WriteLine($"路径包含 {path.Count} 个点");
```

2. 运行应用程序
3. 确认没有错误

## ✅ 验证集成

### 运行测试程序

创建一个简单的控制台应用程序来测试：

```csharp
using System;
using SunEyeVision.Algorithms.PathPlanning;
using System.Windows;

class Program
{
    static void Main(string[] args)
    {
        try
        {
            Console.WriteLine("开始测试 LibavoidPathCalculator...");

            var calculator = new LibavoidPathCalculator();
            var path = calculator.CalculateOrthogonalPath(
                new Point(100, 100),
                new Point(300, 300)
            );

            Console.WriteLine($"✅ 成功！路径包含 {path.Count} 个点");
            Console.WriteLine("集成验证通过！");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 失败: {ex.Message}");
            Console.WriteLine("请检查集成配置");
        }
    }
}
```

## 🔧 常见问题

### 问题 1: 找不到 SunEyeVision.LibavoidWrapper.dll

**解决方案**:
1. 确认 `SunEyeVision.LibavoidWrapper` 项目已构建
2. 检查输出目录是否正确
3. 确认平台配置匹配（x64）

### 问题 2: 无法加载 DLL

**解决方案**:
1. 确认 Visual C++ Redistributable 已安装
2. 检查 DLL 依赖项
3. 使用 Dependency Walker 检查依赖

### 问题 3: 编译错误

**解决方案**:
1. 确认 C++/CLI 工作负载已安装
2. 检查项目配置
3. 清理并重新生成解决方案

### 问题 4: 链接错误

**解决方案**:
1. 检查链接器设置
2. 确认所有必需的库已链接
3. 检查平台配置

## 📚 详细文档

如果需要更详细的信息，请参考：

- [libavoid集成指南.md](./libavoid集成指南.md) - 完整的集成指南
- [libavoid使用示例.md](./libavoid使用示例.md) - 详细的使用示例
- [libavoid集成检查清单.md](./libavoid集成检查清单.md) - 集成检查清单
- [libavoid集成总结.md](./libavoid集成总结.md) - 集成总结

## 🎯 下一步

集成完成后，您可以：

1. **查看示例代码**: 参考 `libavoid使用示例.md`
2. **运行测试**: 使用 `LibavoidPathCalculatorTest.cs`
3. **集成到应用**: 在您的图表编辑器中使用路径计算
4. **性能优化**: 根据实际需求优化性能

## 📞 获取帮助

如果遇到问题：

1. 查看详细文档
2. 检查集成检查清单
3. 联系项目维护者

---

**快速集成指南版本**: 1.0.0  
**最后更新**: 2026-02-02
