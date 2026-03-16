# ParameterCountMismatch 异常修复报告

**修复时间**: 2026-03-16  
**修复状态**: ✅ 已完成

---

## 📋 问题描述

应用程序启动时出现多次 `System.Reflection.TargetParameterCountException` 异常，异常信息为 "Parameter count mismatch"。

### 异常发生时间点
- 10:02:11.679 - 第一次异常
- 10:02:15.922 - 第二次异常
- 10:02:17.215 - 第三次异常
- 10:02:18.567 - 第四次异常
- 10:02:19.852 - 第五次异常

### 异常堆栈
```
at System.Reflection.MethodBaseInvoker.ThrowTargetParameterCountException()
at System.Reflection.RuntimeMethodInfo.Invoke(Object obj, BindingFlags invokeAttr, Binder binder, Object[] parameters, CultureInfo culture)
at System.Delegate.DynamicInvokeImpl(Object[] args)
at System.Windows.Threading.ExceptionWrapper.InternalRealCall(Delegate callback, Object args, Int32 numArgs)
```

### 调用链
```
ViewModelBase.UpdateCollection[T](ObservableCollection`1 collection, Action`1 updateAction)
→ Application.Current.Dispatcher.Invoke(updateAction)
```

---

## 🔍 根本原因

### 问题1: UILogWriter.cs 第180行（已修复）
**文件**: `src/UI/Services/Logging/UILogWriter.cs`  
**位置**: 第180行

**问题代码**:
```csharp
public void Clear()
{
    if (_dispatcher.CheckAccess())
    {
        _logEntries.Clear();
        LogsCleared?.Invoke(this, EventArgs.Empty);
        StatisticsChanged?.Invoke(this, EventArgs.Empty);
    }
    else
    {
        _dispatcher.Invoke(Clear);  // ❌ 问题在这里！
    }
}
```

**问题分析**:
1. 使用方法组 `Clear` 而不是 lambda 表达式
2. 编译器将方法组转换为 Delegate，但无法正确推断委托类型
3. Dispatcher 在调用时传递 args 和 numArgs 参数，但委托是无参数的

### 问题2: ViewModelBase.cs 第109行（已修复）
**文件**: `src/UI/ViewModels/ViewModelBase.cs`  
**位置**: 第109行

**问题代码**:
```csharp
protected void UpdateCollection<T>(ObservableCollection<T> collection, Action<ObservableCollection<T>> updateAction)
{
    if (Application.Current != null && Application.Current.Dispatcher != null)
    {
        Application.Current.Dispatcher.Invoke(updateAction);  // ❌ 问题在这里！
    }
    else
    {
        updateAction(collection);
    }
}
```

**问题分析**:
1. `updateAction` 是 `Action<ObservableCollection<T>>` 类型，期望接收一个参数
2. `Dispatcher.Invoke` 在调用时传递 args 和 numArgs，但没有传递 collection 参数
3. 导致参数数量不匹配异常

**调用示例**:
```csharp
protected void AddToCollection<T>(ObservableCollection<T> collection, T item)
{
    UpdateCollection(collection, c => c.Add(item));  // c 应该是 collection
}
```

---

## ✅ 修复方案

### 修复1: UILogWriter.cs

**修复前**:
```csharp
_dispatcher.Invoke(Clear);
```

**修复后**:
```csharp
_dispatcher.Invoke(() => Clear());
```

### 修复2: ViewModelBase.cs

**修复前**:
```csharp
Application.Current.Dispatcher.Invoke(updateAction);
```

**修复后**:
```csharp
Application.Current.Dispatcher.Invoke(() => updateAction(collection));
```

---

## 🔧 实施步骤

1. ✅ **定位问题文件**: 
   - `src/UI/Services/Logging/UILogWriter.cs`
   - `src/UI/ViewModels/ViewModelBase.cs`
2. ✅ **定位问题代码**:
   - UILogWriter.cs 第180行
   - ViewModelBase.cs 第109行
3. ✅ **应用修复**:
   - UILogWriter.cs: `_dispatcher.Invoke(() => Clear());`
   - ViewModelBase.cs: `Application.Current.Dispatcher.Invoke(() => updateAction(collection));`
4. ✅ **编译验证**: 编译成功，0 个错误
5. ⏳ **运行验证**: 需要运行应用程序验证异常是否消失

---

## ✅ 验证结果

### 编译结果
```
已用时间 00:00:12.15
1336 个警告
0 个错误
```

### 代码检查
- ✅ UILogWriter.cs - 无 linter 错误
- ✅ ViewModelBase.cs - 无 linter 错误

### 预期结果
- 应用程序启动时不应再出现 "Parameter count mismatch" 异常
- 日志系统应正常工作
- 工作流初始化应正常完成

---

## 📝 相关知识

### Dispatcher.Invoke 正确用法

```csharp
// ✅ 正确 - 使用无参数 lambda 表达式
_dispatcher.Invoke(() => Method());

// ✅ 正确 - 使用带参数的 lambda 表达式
_dispatcher.Invoke(() => Method(param));

// ✅ 正确 - 调用带参数的 Action 委托
_dispatcher.Invoke(() => action(collection));

// ❌ 错误 - 使用方法组（可能导致参数不匹配）
_dispatcher.Invoke(MethodName);

// ❌ 错误 - 直接传递带参数的委托
_dispatcher.Invoke(updateAction);  // updateAction 期望参数但未提供
```

### 委托类型对比

```csharp
// Action<T> - 带参数的委托
Action<ObservableCollection<T>> updateAction = c => c.Add(item);
// 调用方式: updateAction(collection)

// Action - 无参数委托
Action clearAction = () => Clear();
// 调用方式: clearAction()
```

### 最佳实践

1. **始终使用 lambda 表达式**: `_dispatcher.Invoke(() => Method());`
2. **明确传递参数**: `_dispatcher.Invoke(() => action(collection));`
3. **避免使用方法组**: 不要使用 `_dispatcher.Invoke(Method);`
4. **类型安全**: 确保委托参数与 Invoke 方法期望的参数匹配

---

## 🎯 总结

本次修复解决了应用程序启动时的 `TargetParameterCountException` 异常问题。通过将方法组调用改为 lambda 表达式调用，并确保带参数委托正确传递参数，消除了参数数量不匹配问题。

**修复效果**: 预期完全消除 "Parameter count mismatch" 异常。

---

## 📞 后续建议

1. **运行应用程序验证**: 启动应用程序，检查日志是否还有异常
2. **代码审查**: 检查项目中是否还有其他类似问题
3. **编码规范**: 在团队中推广使用 lambda 表达式的最佳实践
4. **单元测试**: 为 ViewModelBase 和 UILogger 添加单元测试

---

**修复完成时间**: 2026-03-16  
**修复人员**: AI Assistant  
**状态**: ✅ 已完成，待运行验证
