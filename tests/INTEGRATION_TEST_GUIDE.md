# SunEyeVision 统一集成测试指南

## 概述

本测试套件为 SunEyeVision 项目提供完整的集成测试，覆盖以下核心模块：

1. **插件系统集成测试** - PluginIntegrationTests.cs
2. **工作流执行集成测试** - WorkflowExecutionIntegrationTests.cs
3. **设备驱动集成测试** - DeviceDriverIntegrationTests.cs
4. **数据绑定集成测试** - DataBindingIntegrationTests.cs

## 测试架构

### 测试框架
- **xUnit 2.6.2** - 主测试框架
- **FluentAssertions 6.12.0** - 流畅断言库
- **Moq 4.20.70** - Mock对象框架
- **Microsoft.NET.Test.Sdk 17.8.0** - 测试SDK

### 测试分类

#### 1. 单元测试 (Unit Tests)
- 位置: `tests/SunEyeVision.Core.Tests/`
- 范围: 单个类、方法、功能的独立测试
- 运行速度: 快 (< 1秒)
- 依赖: 无外部依赖，使用Mock对象

#### 2. 集成测试 (Integration Tests)
- 位置: `tests/SunEyeVision.Core.Tests/Integration/`
- 范围: 多个组件协作、完整功能链路
- 运行速度: 中等 (1-10秒)
- 依赖: 真实组件、模拟数据

## 运行测试

### 方式1: PowerShell脚本 (推荐)
```powershell
cd tests
.\run_integration_tests.ps1
```

### 方式2: Batch脚本
```batch
cd tests
run_integration_tests.bat
```

### 方式3: dotnet CLI
```bash
# 运行所有测试
dotnet test tests/SunEyeVision.Core.Tests/SunEyeVision.Core.Tests.csproj

# 运行特定测试类别
dotnet test --filter "Category=Integration"

# 生成代码覆盖率报告
dotnet test --collect:"XPlat Code Coverage"
```

### 方式4: Visual Studio
1. 打开测试资源管理器 (Test Explorer)
2. 选择要运行的测试
3. 点击"运行"或"调试"

## 测试报告

测试完成后，报告将生成在以下位置：

- **TestResults/test_results.trx** - Visual Studio测试结果文件
- **test_report.txt** - 文本格式测试报告
- **TestResults/coverage.cobertura.xml** - 代码覆盖率报告

## 测试覆盖范围

### 插件系统集成测试 (PluginIntegrationTests)

| 测试项 | 描述 | 状态 |
|--------|------|------|
| PluginManager_LoadPlugins_ShouldDiscoverToolPlugins | 验证插件发现功能 | ✅ |
| PluginManager_GetPlugins_ShouldReturnValidMetadata | 验证插件元数据 | ✅ |
| ToolRegistry_RegisterTool_ShouldStoreMetadata | 验证工具注册 | ✅ |
| ToolRegistry_GetAllTools_ShouldReturnAllRegistered | 验证工具枚举 | ✅ |
| ToolRegistry_GetToolsByCategory_ShouldFilterCorrectly | 验证分类过滤 | ✅ |
| ToolPlugin_GetDefaultParameters_ShouldReturnValidParameters | 验证默认参数 | ✅ |
| ToolPlugin_GetParameterMetadata_ShouldReturnValidMetadata | 验证参数元数据 | ✅ |
| PluginManager_UnloadPlugin_ShouldRemoveFromLoadedList | 验证插件卸载 | ✅ |

### 工作流执行集成测试 (WorkflowExecutionIntegrationTests)

| 测试项 | 描述 | 状态 |
|--------|------|------|
| WorkflowEngine_CreateWorkflow_ShouldInitializeCorrectly | 验证工作流创建 | ✅ |
| WorkflowEngine_CreateMultipleWorkflows_ShouldManageCorrectly | 验证多工作流管理 | ✅ |
| Workflow_AddNode_ShouldStoreInNodesCollection | 验证节点添加 | ✅ |
| Workflow_RemoveNode_ShouldUpdateConnections | 验证节点删除 | ✅ |
| Workflow_AddConnection_ShouldValidateNodes | 验证连接创建 | ✅ |
| WorkflowContext_CreateSubContext_ShouldInheritParentVariables | 验证上下文继承 | ✅ |
| WorkflowContext_SetVariableInSubContext_ShouldNotAffectParent | 验证变量隔离 | ✅ |
| WorkflowContext_CallStack_ShouldTrackNesting | 验证调用栈追踪 | ✅ |
| Workflow_ExecuteEmptyWorkflow_ShouldSucceed | 验证空工作流执行 | ✅ |
| Workflow_Validate_ShouldCheckCyclicDependencies | 验证循环检测 | ✅ |
| WorkflowContext_UpdateNodeStatus_ShouldTrackProgress | 验证状态追踪 | ✅ |
| WorkflowContext_GetStatistics_ShouldProvideExecutionSummary | 验证执行统计 | ✅ |

### 设备驱动集成测试 (DeviceDriverIntegrationTests)

| 测试项 | 描述 | 状态 |
|--------|------|------|
| DeviceManager_GetAvailableDevices_ShouldReturnDeviceList | 验证设备枚举 | ✅ |
| DeviceManager_RegisterDriver_ShouldStoreInRegistry | 验证驱动注册 | ✅ |
| DeviceManager_UnregisterDriver_ShouldRemoveFromRegistry | 验证驱动注销 | ✅ |
| SimulatedCamera_Connect_ShouldSucceed | 验证相机连接 | ✅ |
| SimulatedCamera_Disconnect_ShouldUpdateState | 验证相机断开 | ✅ |
| SimulatedCamera_CaptureImage_ShouldReturnValidImage | 验证图像采集 | ✅ |
| SimulatedCamera_GetDeviceCapabilities_ShouldReturnValidInfo | 验证能力查询 | ✅ |
| BaseDeviceDriver_GetStatus_ShouldReturnCurrentState | 验证状态获取 | ✅ |
| BaseDeviceDriver_OnStatusChanged_ShouldRaiseEvent | 验证事件触发 | ✅ |
| SimulatedCamera_CaptureWithoutConnect_ShouldFail | 验证错误处理 | ✅ |
| DeviceManager_GetNonExistentDriver_ShouldReturnNull | 验证空值处理 | ✅ |

### 数据绑定集成测试 (DataBindingIntegrationTests)

| 测试项 | 描述 | 状态 |
|--------|------|------|
| ObservableObject_SetProperty_ShouldRaisePropertyChanged | 验证属性通知 | ✅ |
| ObservableObject_SetSameValue_ShouldNotRaiseEvent | 验证事件优化 | ✅ |
| ToolResults_SetSuccess_ShouldUpdateProperties | 验证成功结果 | ✅ |
| ToolResults_SetFailure_ShouldRecordError | 验证失败结果 | ✅ |
| ToolResults_GetResultItems_ShouldReturnMetadata | 验证结果项 | ✅ |
| AlgorithmParameters_Validate_ShouldReturnValidResult | 验证参数验证 | ✅ |
| AlgorithmParameters_ValidateWithInvalidData_ShouldReturnErrors | 验证错误收集 | ✅ |

## 测试配置

### xunit.runner.json
控制测试执行行为的配置文件：
- 并行执行: 启用 (最大4线程)
- 超时设置: 60秒
- 诊断消息: 启用

### runsettings.xml
控制测试运行器的配置文件：
- 代码覆盖率: 启用
- 测试适配器: 配置
- 日志输出: 多格式支持

## 最佳实践

1. **命名规范**
   - 测试类: `{ClassName}Tests`
   - 测试方法: `{MethodName}_Should_{ExpectedBehavior}`

2. **测试结构 (AAA模式)**
   ```csharp
   [Fact]
   public void Test_Scenario_ExpectedBehavior()
   {
       // Arrange - 准备
       var obj = new TestObject();
       
       // Act - 执行
       var result = obj.Method();
       
       // Assert - 断言
       result.Should().Be(expected);
   }
   ```

3. **使用FluentAssertions**
   ```csharp
   // 推荐
   result.Should().BeTrue("因为参数有效");
   list.Should().HaveCount(2, "因为添加了两个元素");
   
   // 不推荐
   Assert.True(result);
   Assert.Equal(2, list.Count);
   ```

4. **使用Moq创建模拟对象**
   ```csharp
   var mockLogger = new Mock<ILogger>();
   mockLogger.Setup(x => x.Log(It.IsAny<string>()));
   var service = new MyService(mockLogger.Object);
   ```

## 故障排查

### 常见问题

1. **测试找不到程序集**
   - 确保所有项目引用正确
   - 清理并重新编译解决方案

2. **测试超时**
   - 检查是否有死锁代码
   - 增加超时设置

3. **代码覆盖率报告未生成**
   - 安装 coverlet.collector 包
   - 检查 runsettings.xml 配置

4. **并行测试导致失败**
   - 检查测试间的依赖
   - 使用 `[Collection]` 特性隔离测试

## 持续集成

测试套件已配置用于CI/CD流程：

```yaml
# Azure DevOps Pipeline
- task: DotNetCoreCLI@2
  displayName: 'Run Tests'
  inputs:
    command: 'test'
    projects: '**/*.Tests.csproj'
    arguments: '--configuration Release --no-build --collect:"XPlat Code Coverage"'
```

## 更新日志

### 2026-03-07
- 创建统一集成测试框架
- 添加4个集成测试文件
- 配置xunit.runner.json和runsettings.xml
- 添加PowerShell和Batch测试运行脚本
- 创建测试文档

---

**维护者**: SunEyeVision 开发团队
**最后更新**: 2026-03-07
