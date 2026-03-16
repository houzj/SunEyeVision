# DispatcherTimer 监控系统 - 快速入门

## 概述

DispatcherTimer监控系统已成功实施，用于诊断"Parameter count mismatch"异常。

## 如何使用

### 1. 立即开始（无需修改代码）

监控系统已经集成到应用程序中，**无需任何修改即可使用**。

### 2. 运行应用程序

1. 关闭Visual Studio
2. 运行编译后的应用程序
3. 正常使用应用程序

### 3. 查看监控日志

#### 方法1: 通过Debug输出查看

在Visual Studio的Output窗口中，搜索"DispatcherTimer"关键字。

#### 方法2: 直接查看日志文件

监控日志文件位于应用程序运行目录：
- `dispatcher_timer_monitor.log` - Timer监控日志
- `parameter_mismatch_detailed.log` - 异常详细日志（仅在发生异常时生成）

### 4. 等待问题重现

应用程序运行期间，如果"Parameter count mismatch"异常再次发生：

1. 系统会自动记录完整的诊断信息到`parameter_mismatch_detailed.log`
2. 查看该文件，找到异常发生的完整调用堆栈
3. 根据堆栈信息定位问题代码

### 5. 手动生成监控报告

如果在运行中需要查看当前所有Timer的状态：

在Debug窗口的立即执行窗口中输入：
```csharp
SunEyeVision.UI.Services.Monitoring.DispatcherTimerMonitor.Instance.PrintAllTimersInfo()
```

## 监控报告示例

```
=== DispatcherTimer 监控报告 ===
生成时间: 2026-03-16 09:36:45.896
Timer总数: 10
--------------------------------------------------------------------------------

Timer ID: 1
  名称: BatchUpdateTimer
  所有者: SunEyeVision.UI.Services.Performance.BatchUIUpdater
  间隔: 0.033秒
  启用: True
  创建时间: 09:36:18.843
  Tick次数: 842
  最后Tick: 09:36:45.893
  最后状态变更: 09:36:18.950
  Tick事件处理器:
    处理器数量: 1
    - 方法: SunEyeVision.UI.Services.Performance.BatchUIUpdater.OnTick
      签名: Void OnTick(System.Object, System.EventArgs)
      参数: Object sender, EventArgs e

Timer ID: 2
  名称: ScrollTimer
  所有者: SunEyeVision.UI.Views.Controls.Panels.PropertyPanelControl
  间隔: 0.1秒
  启用: False
  创建时间: 09:36:19.050
  Tick次数: 15
  最后Tick: 09:36:44.123
  最后状态变更: 09:36:44.233
  Tick事件处理器:
    处理器数量: 1
    [错误] 签名验证失败: 参数类型不正确: 期望(object, EventArgs/DispatcherEventArgs), 实际(object, ScrollEventArgs)
    - 方法: PropertyPanelControl.OnScrollTimerTick
      签名: Void OnScrollTimerTick(System.Object, System.Windows.Controls.Primitives.ScrollEventArgs)
      参数: Object sender, ScrollEventArgs e

================================================================================
```

## 如何解读监控报告

### 正常的Timer

```
Timer ID: 1
  Tick事件处理器:
    处理器数量: 1
    - 方法: SunEyeVision.UI.Services.Performance.BatchUIUpdater.OnTick
      签名: Void OnTick(System.Object, System.EventArgs)
      参数: Object sender, EventArgs e
```

✅ 签名正确：`void OnTick(object sender, EventArgs e)`

### 有问题的Timer

```
Timer ID: 2
  Tick事件处理器:
    处理器数量: 1
    [错误] 签名验证失败: 参数类型不正确: 期望(object, EventArgs/DispatcherEventArgs), 实际(object, ScrollEventArgs)
    - 方法: PropertyPanelControl.OnScrollTimerTick
      签名: Void OnScrollTimerTick(System.Object, System.Windows.Controls.Primitives.ScrollEventArgs)
      参数: Object sender, ScrollEventArgs e
```

❌ 签名错误：第二个参数应该是`EventArgs`，而不是`ScrollEventArgs`

## 如何修复问题

如果监控报告显示某个Timer的Tick事件处理器签名不正确：

### 示例：修复ScrollTimer

**问题代码**：
```csharp
// ❌ 错误的签名
private void OnScrollTimerTick(object sender, ScrollEventArgs e)
{
    // ...
}
```

**修复代码**：
```csharp
// ✅ 正确的签名
private void OnScrollTimerTick(object sender, EventArgs e)
{
    // 如果需要访问ScrollEventArgs，需要转换
    var scrollArgs = e as ScrollEventArgs;
    if (scrollArgs != null)
    {
        // 使用scrollArgs...
    }
}
```

## 常见问题

### Q1: 监控系统会影响性能吗？

A: 影响很小。每个Timer仅增加约200字节的内存，CPU开销可忽略不计。

### Q2: 日志文件会无限增长吗？

A: 会，但增长速度很慢。建议定期清理日志文件。

### Q3: 需要在生产环境启用监控吗？

A: 可以启用，建议定期清理日志文件以避免磁盘占用过大。

### Q4: 如何禁用监控系统？

A: 注释掉`App.xaml.cs`中的`InitializeMonitoring()`调用。

### Q5: 只监控特定的Timer吗？

A: 不，监控系统会监控所有DispatcherTimer，包括你创建的定时器。

## 下一步

1. **编译并运行**应用程序
2. **正常使用**应用程序
3. **等待问题重现**（如果问题再次发生）
4. **查看日志文件**定位问题根源
5. **修复问题**并验证

## 技术支持

如有问题，请参考：
- 完整使用文档：`docs/DispatcherTimer监控使用指南.md`
- 实施报告：`DispatcherTimer监控系统实施报告.md`

---

**重要提示**：监控系统已经准备好，无需任何修改。只需编译、运行、等待问题重现即可！
