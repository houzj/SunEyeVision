# DispatcherTimer 监控系统实施报告

## 实施日期
2026-03-16

## 问题背景

应用程序定期出现"Parameter count mismatch"异常：
- 异常类型：`System.Reflection.TargetParameterCountException`
- 发生频率：约每1秒一次
- 堆栈跟踪：只到`ExceptionWrapper.InternalRealCall`，未显示具体调用源
- 影响：用户体验，但不导致崩溃

## 实施目标

添加完整的关键监控来精准定位"Parameter count mismatch"异常的根本原因。

## 实施内容

### 1. DispatcherTimerMonitor（监控核心）

**文件位置**: `src/UI/Services/Monitoring/DispatcherTimerMonitor.cs`

**核心功能**:
- 单例模式，全局访问
- 自动监控所有注册的DispatcherTimer
- 分析Tick事件处理器的签名
- 检测参数数量不匹配问题
- 记录Timer的所有状态变更

**关键方法**:
```csharp
// 注册Timer到监控
int RegisterTimer(DispatcherTimer timer, string? ownerName = null, string? timerName = null)

// 记录状态变更
void LogTimerStateChange(int timerId, string action, string? details = null)

// 记录Tick触发
void LogTimerTick(int timerId)

// 获取所有Timer信息
string GetAllTimersInfo()

// 打印监控报告
void PrintAllTimersInfo()
```

**监控内容**:
- Timer基本信息（ID、名称、所有者）
- 配置信息（间隔、启用状态）
- 运行统计（Tick次数、最后Tick时间、最后状态变更）
- Tick事件处理器分析：
  - 处理器数量
  - 方法签名验证（期望：`void OnTick(object sender, EventArgs e)`）
  - 参数类型和数量检查
  - 签名错误检测

### 2. MonitoredDispatcherTimer（包装器）

**文件位置**: `src/UI/Services/Monitoring/MonitoredDispatcherTimer.cs`

**核心功能**:
- 自动包装DispatcherTimer并注册到监控
- 提供与DispatcherTimer相同的API
- 自动记录所有操作到监控
- 支持显式Timer命名，便于识别

**使用示例**:
```csharp
// ❌ 旧代码
var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
timer.Tick += OnTimerTick;
timer.Start();

// ✅ 新代码
var timer = new MonitoredDispatcherTimer(
    interval: TimeSpan.FromSeconds(1),
    timerName: "MyTimer"  // 可选，方便识别
);
timer.Tick += OnTimerTick;
timer.Start();
```

### 3. 应用程序集成

**文件**: `src/UI/App.xaml.cs`

**修改内容**:

1. 添加监控初始化：
```csharp
private void InitializeMonitoring()
{
    try
    {
        Debug.WriteLine("[App] 初始化DispatcherTimer监控...");
        var monitor = DispatcherTimerMonitor.Instance;

        // 创建监控报告定时器
        _monitorTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(_monitorReportInterval)
        };
        _monitorTimer.Tick += (sender, e) =>
        {
            Debug.WriteLine("[App] === DispatcherTimer 监控报告 ===");
            monitor.PrintAllTimersInfo();
        };
        _monitorTimer.Start();

        Debug.WriteLine($"[App] DispatcherTimer监控已启动，报告间隔: {_monitorReportInterval}秒");
    }
    catch (Exception ex)
    {
        Debug.WriteLine($"[App] 初始化监控失败: {ex.Message}");
    }
}
```

2. 在`OnStartup`中调用初始化：
```csharp
// 初始化DispatcherTimer监控
InitializeMonitoring();

Debug.WriteLine("[App] 监控系统已初始化");
```

3. 在`OnExit`中生成最终报告：
```csharp
protected override void OnExit(ExitEventArgs e)
{
    // 停止监控定时器
    _monitorTimer?.Stop();

    // 生成最终的监控报告
    try
    {
        Debug.WriteLine("[App] === 应用程序退出 - 最终监控报告 ===");
        DispatcherTimerMonitor.Instance.PrintAllTimersInfo();
    }
    catch (Exception ex)
    {
        Debug.WriteLine($"[App] 生成最终监控报告失败: {ex.Message}");
    }

    base.OnExit(e);
}
```

4. 增强异常捕获：

在`OnFirstChanceException`中：
```csharp
if (e.Exception is System.Reflection.TargetParameterCountException)
{
    var ex = e.Exception as System.Reflection.TargetParameterCountException;
    Debug.WriteLine($"[App] 捕获TargetParameterCountException(参数数量不匹配): {ex?.Message}");
    Debug.WriteLine($"[App] 异常堆栈: {ex?.StackTrace}");
    
    // 捕获完整调用堆栈
    Debug.WriteLine($"[App] 完整调用堆栈:");
    Debug.WriteLine(Environment.StackTrace);
    
    // 记录到文件以便详细分析
    try
    {
        string diagLog = AppDomain.CurrentDomain.BaseDirectory + "parameter_mismatch_detailed.log";
        string logContent = "\n\n=== FirstChanceException - TargetParameterCountException ===\n";
        logContent += "时间: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "\n";
        logContent += "异常: " + (ex?.Message ?? "") + "\n";
        logContent += "异常类型: " + (ex?.GetType().FullName ?? "") + "\n";
        logContent += "\n异常堆栈:\n" + (ex?.StackTrace ?? "") + "\n";
        logContent += "\n完整调用堆栈:\n" + Environment.StackTrace + "\n";
        if (ex?.InnerException != null)
        {
            logContent += "\n内部异常: " + ex.InnerException.Message + "\n";
            logContent += "内部异常类型: " + ex.InnerException.GetType().FullName + "\n";
            logContent += "内部堆栈:\n" + ex.InnerException.StackTrace + "\n";
        }
        logContent += "===================================\n\n";
        File.AppendAllText(diagLog, logContent);
        Debug.WriteLine($"[App] 详细日志已保存到: {diagLog}");
    }
    catch (Exception writeEx)
    {
        Debug.WriteLine($"[App] 保存详细日志失败: {writeEx.Message}");
    }
}
```

在`OnDispatcherUnhandledException`中：
```csharp
if (e.Exception is System.Reflection.TargetParameterCountException)
{
    Debug.WriteLine($"[App] *** 这是一个参数数量不匹配异常 ***");
    Debug.WriteLine($"[App] 可能的原因:");
    Debug.WriteLine($"[App]   1. Dispatcher.BeginInvoke参数顺序错误");
    Debug.WriteLine($"[App]   2. 事件处理器签名不匹配");
    Debug.WriteLine($"[App]   3. 委托调用时参数数量错误");

    // 捕获完整的调用堆栈
    Debug.WriteLine($"[App] 完整调用堆栈:");
    Debug.WriteLine(Environment.StackTrace);

    // 记录到文件以便详细分析
    try
    {
        string diagLog = AppDomain.CurrentDomain.BaseDirectory + "parameter_mismatch_detailed.log";
        string logContent = "\n\n=== Parameter Mismatch Detail ===\n";
        logContent += "时间: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "\n";
        logContent += "异常: " + e.Exception.Message + "\n";
        logContent += "异常类型: " + e.Exception.GetType().FullName + "\n";
        logContent += "\n异常堆栈:\n" + e.Exception.StackTrace + "\n";
        logContent += "\n完整调用堆栈:\n" + Environment.StackTrace + "\n";
        if (e.Exception.InnerException != null)
        {
            logContent += "\n内部异常: " + e.Exception.InnerException.Message + "\n";
            logContent += "内部异常类型: " + e.Exception.InnerException.GetType().FullName + "\n";
            logContent += "内部堆栈:\n" + e.Exception.InnerException.StackTrace + "\n";
        }
        logContent += "===================================\n\n";
        File.AppendAllText(diagLog, logContent);
        Debug.WriteLine($"[App] 详细日志已保存到: {diagLog}");
    }
    catch (Exception writeEx)
    {
        Debug.WriteLine($"[App] 保存详细日志失败: {writeEx.Message}");
    }
}
```

### 4. 日志文件

#### 4.1 dispatcher_timer_monitor.log

**位置**: 应用程序运行目录

**内容示例**:
```
================================================================================
DispatcherTimerMonitor 初始化: 2026-03-16 09:36:18.843
监控文件: C:\Users\...\dispatcher_timer_monitor.log
================================================================================
[09:36:18.843] Timer注册: ID=1, 名称=BatchUpdateTimer, 所有者=BatchUIUpdater, 间隔=0.033s, 启用=True
[09:36:18.843] Tick事件处理器已注册: Timer ID=1
  - 处理器数量: 1
  - 方法: BatchUIUpdater.OnTick
  - 签名: Void OnTick(System.Object, System.EventArgs)
[09:36:18.950] Timer状态变更: ID=1, 动作=Start
[09:36:19.050] Timer触发: ID=1, 次数=1
[09:36:19.083] Timer触发: ID=1, 次数=2
```

#### 4.2 parameter_mismatch_detailed.log

**位置**: 应用程序运行目录

**触发条件**: 当发生TargetParameterCountException时自动生成

**内容示例**:
```

=== FirstChanceException - TargetParameterCountException ===
时间: 2026-03-16 09:36:24.062
异常: Parameter count mismatch.
异常类型: System.Reflection.TargetParameterCountException

异常堆栈:
   at System.Reflection.MethodBaseInvoker.ThrowTargetParameterCountException()
   at System.Reflection.RuntimeMethodInfo.Invoke(Object obj, BindingFlags invokeAttr, Binder binder, Object[] parameters, CultureInfo culture)
   at System.Delegate.DynamicInvokeImpl(Object[] args)
   at System.Windows.Threading.ExceptionWrapper.InternalRealCall(Delegate callback, Object args, Int32 numArgs)
   at System.Windows.Threading.ExceptionWrapper.TryCatchWhen(Object source, Delegate callback, Object args, Int32 numArgs, Delegate catchHandler)

完整调用堆栈:
   at System.Environment.get_StackTrace()
   at SunEyeVision.UI.App.OnFirstChanceException(Object sender, FirstChanceExceptionEventArgs e)
   ...

===================================
```

### 5. 使用文档

**文件位置**: `docs/DispatcherTimer监控使用指南.md`

**文档内容**:
- 系统概述
- 使用方法（MonitoredDispatcherTimer和手动注册）
- 监控日志文件说明
- 监控报告示例
- 问题诊断指南
- 迁移现有代码步骤
- 注意事项
- 高级用法

## 监控策略

### 1. 自动监控报告

- 频率：每30秒一次
- 输出：Debug输出 + 日志文件
- 内容：所有Timer的详细信息和状态

### 2. 异常触发日志

- 触发条件：发生TargetParameterCountException
- 记录内容：
  - 异常详细信息
  - 完整调用堆栈（关键！）
  - 异常发生时间
  - 内部异常信息

### 3. Timer生命周期监控

- 创建：记录Timer注册
- 启动/停止：记录状态变更
- Tick触发：记录前5次（避免日志过多）
- 销毁：应用程序退出时生成最终报告

## 问题诊断流程

### 步骤1: 查看异常日志

当发生"Parameter count mismatch"异常时：

1. 打开`parameter_mismatch_detailed.log`
2. 找到异常发生的时间点
3. 查看完整调用堆栈，定位调用源

### 步骤2: 查看Timer监控日志

1. 打开`dispatcher_timer_monitor.log`
2. 查看异常时间前后的Timer活动
3. 找到刚刚触发Tick的Timer

### 步骤3: 分析Timer信息

1. 查看该Timer的Tick事件处理器
2. 检查处理器签名是否正确
3. 查看是否有签名验证错误

### 步骤4: 手动生成报告

如果在运行中需要查看当前状态：

```csharp
DispatcherTimerMonitor.Instance.PrintAllTimersInfo();
```

## 常见问题修复

### 问题1: Tick事件处理器签名不正确

**症状**: 定期出现TargetParameterCountException异常

**原因**: Tick事件处理方法的签名不正确

**修复方法**:
```csharp
// ❌ 错误的签名
private void OnTick()  // 缺少参数
{
    // ...
}

// ❌ 错误的签名
private void OnTick(object sender)  // 缺少EventArgs参数
{
    // ...
}

// ❌ 错误的签名
private void OnTick(object sender, RoutedEventArgs e)  // 参数类型错误
{
    // ...
}

// ✅ 正确的签名
private void OnTick(object sender, EventArgs e)
{
    // ...
}
```

### 问题2: 未注册到监控的Timer

**症状**: 某些Timer未出现在监控报告中

**修复方法**:
- 使用MonitoredDispatcherTimer替代DispatcherTimer
- 或者手动注册到DispatcherTimerMonitor

## 性能影响

1. **内存开销**: 每个Timer约增加200字节的监控数据
2. **CPU开销**: 可忽略不计（仅在状态变更时记录）
3. **磁盘IO**: 日志文件大小取决于Timer数量和活动频率

## 后续计划

### 短期（1-2天）

1. 编译并测试监控系统
2. 在开发环境中验证监控功能
3. 等待"Parameter count mismatch"异常再次发生
4. 根据日志定位问题根源

### 中期（1周）

1. 修复发现的签名问题
2. 迁移所有DispatcherTimer到MonitoredDispatcherTimer
3. 验证修复效果

### 长期（持续）

1. 监控日志文件大小，定期清理
2. 根据实际使用情况优化监控策略
3. 添加更多监控指标（如Tick耗时统计）

## 文件清单

### 新增文件

1. `src/UI/Services/Monitoring/DispatcherTimerMonitor.cs` - 监控核心
2. `src/UI/Services/Monitoring/MonitoredDispatcherTimer.cs` - 包装器
3. `docs/DispatcherTimer监控使用指南.md` - 使用文档

### 修改文件

1. `src/UI/App.xaml.cs` - 集成监控和异常捕获

### 日志文件（运行时生成）

1. `dispatcher_timer_monitor.log` - Timer监控日志
2. `parameter_mismatch_detailed.log` - 异常详细日志

## 编译状态

**当前状态**: 等待Visual Studio关闭后重新编译

**预期编译结果**: 成功（0错误，可能有警告）

## 结论

通过实施DispatcherTimer监控系统，我们添加了完整的监控能力来精准定位"Parameter count mismatch"异常。系统将自动：

1. 监控所有DispatcherTimer的生命周期和Tick事件
2. 捕获并记录TargetParameterCountException异常的完整调用堆栈
3. 定期生成监控报告
4. 验证Tick事件处理器的签名是否正确

一旦"Parameter count mismatch"异常再次发生，系统将自动记录详细的诊断信息，帮助我们快速定位和修复问题根源。
