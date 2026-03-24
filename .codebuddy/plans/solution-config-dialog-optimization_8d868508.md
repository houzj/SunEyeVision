---
name: solution-config-dialog-optimization
overview: 实施解决方案配置对话框优化,修复默认解决方案标记持久化、SkipStartupConfig同步持久化失败问题,优化SaveSettings频繁调用问题
todos:
  - id: add-dirty-flag-mechanism
    content: 在SolutionManager.cs添加脏标记字段和相关方法
    status: completed
  - id: replace-save-settings-calls
    content: 替换SolutionManager.cs中12处SaveSettings()调用为MarkSettingsDirty()或ForceSaveSettings()
    status: completed
    dependencies:
      - add-dirty-flag-mechanism
  - id: fix-setpreference-notification
    content: 修复SolutionSettings.cs的SetPreference()方法，添加PropertyChanged事件触发
    status: completed
  - id: add-viewmodel-save-method
    content: 在SolutionConfigurationDialogViewModel.cs添加SaveSettings()公开方法
    status: completed
  - id: modify-onclosing-logic
    content: 修改SolutionConfigurationDialog.xaml.cs的OnClosing()方法，统一保存设置逻辑
    status: completed
    dependencies:
      - add-viewmodel-save-method
---

## 产品概述

实施解决方案配置对话框优化方案，修复三个关键问题：默认解决方案标记持久化失败、SkipStartupConfig同步和持久化失败、频繁调用SaveSettings导致的性能问题。

## 核心功能

1. **修复默认标记持久化**：设置默认解决方案后关闭窗口重新打开，默认标记（📌）能够正确显示
2. **修复SkipStartupConfig同步**：勾选checkbox后UI立即更新，关闭窗口后settings.json正确保存
3. **性能优化**：使用脏标记机制减少磁盘IO，将多次文件写入优化为一次批量写入

## 技术栈

- C# / WPF
- System.Text.Json 序列化
- INotifyPropertyChanged 属性通知机制

## 实施方案

采用方案文档中设计的脏标记机制和统一关闭逻辑，遵循rule-008原型设计期代码纯净原则，直接删除旧代码不保留兼容路径。

## 当前状态分析

**已完成**：

- ✅ `SolutionRegistry.UpdateAllIsDefaultFlags()` 方法已添加（第281-319行）
- ✅ `SolutionManager.UpdateMetadataIsDefaultFlags()` 方法已修复（第1021-1037行）

**待实施**：

1. `SolutionManager.cs` - 添加脏标记字段和方法，替换12处SaveSettings()调用
2. `SolutionSettings.cs` - 修复SetPreference()方法，添加PropertyChanged事件触发
3. `SolutionConfigurationDialogViewModel.cs` - 添加SaveSettings()公开方法
4. `SolutionConfigurationDialog.xaml.cs` - 修改OnClosing()方法，统一保存逻辑

## 目录结构

```
src/
├── Workflow/
│   ├── SolutionManager.cs           # [MODIFY] 添加脏标记机制，替换SaveSettings调用
│   └── SolutionSettings.cs          # [MODIFY] 修复SetPreference属性通知
└── UI/
    ├── ViewModels/
    │   └── SolutionConfigurationDialogViewModel.cs  # [MODIFY] 添加SaveSettings方法
    └── Views/Windows/
        └── SolutionConfigurationDialog.xaml.cs      # [MODIFY] 修改OnClosing逻辑
```