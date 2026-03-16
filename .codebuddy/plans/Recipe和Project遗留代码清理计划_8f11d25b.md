---
name: Recipe和Project遗留代码清理计划
overview: 全面清理代码库中的Recipe和Project遗留代码，删除3个Recipe核心类（739行），清理UI层Recipe相关代码（约130行），删除注释代码（约200行），重命名SaveCurrentRecipeCommand为SaveCurrentSolutionCommand，统一到Solution架构。
todos:
  - id: delete-recipe-core
    content: 删除3个Recipe核心类文件（Recipe.cs, RecipeGroup.cs, RecipeJsonConverter.cs）
    status: completed
  - id: cleanup-viewmodel
    content: 清理MainWindowViewModel：重命名SaveCurrentRecipeCommand为SaveCurrentSolutionCommand，删除SaveRecipeAsCommand和SwitchRecipeCommand
    status: completed
    dependencies:
      - delete-recipe-core
  - id: update-ui-binding
    content: 更新MainWindow.xaml中的命令绑定，删除Recipe相关菜单项
    status: completed
    dependencies:
      - cleanup-viewmodel
  - id: clean-comment-code
    content: 删除SolutionConfigurationDialogViewModel中的约200行注释代码
    status: completed
  - id: handle-devicebinding
    content: 处理DeviceBinding.cs中的RecipeRef和RecipeGroupName属性
    status: completed
    dependencies:
      - delete-recipe-core
  - id: verify-compile
    content: 编译项目验证修改正确性
    status: completed
    dependencies:
      - cleanup-viewmodel
      - update-ui-binding
      - clean-comment-code
      - handle-devicebinding
  - id: create-skill
    content: 使用[skill:skill-creator]创建代码清理技能，记录本次清理经验
    status: completed
    dependencies:
      - verify-compile
---

## Product Overview

全面清理代码库中的Recipe和Project遗留代码，统一架构到Solution，提升代码质量和可维护性。

## Core Features

- 删除Recipe核心类（Recipe.cs, RecipeGroup.cs, RecipeJsonConverter.cs）
- 清理UI层Recipe相关命令和绑定
- 重命名SaveCurrentRecipeCommand为SaveCurrentSolutionCommand
- 删除注释代码
- 处理DeviceBinding.cs中的Recipe相关属性

## Tech Stack

- 开发语言: C#
- 项目类型: WPF桌面应用程序
- 开发环境: Visual Studio

## Tech Architecture

### System Architecture

现有WPF应用程序架构，采用MVVM模式：

- View层: XAML界面（MainWindow.xaml等）
- ViewModel层: 业务逻辑（MainWindowViewModel等）
- Model层: 数据模型（Recipe核心类等）

### Module Division

- **Recipe核心清理模块**: 删除Recipe.cs, RecipeGroup.cs, RecipeJsonConverter.cs三个文件
- **ViewModel清理模块**: 修改MainWindowViewModel，重命名命令，删除无用命令
- **UI绑定清理模块**: 更新MainWindow.xaml中的菜单绑定
- **注释代码清理模块**: 清理SolutionConfigurationDialogViewModel中的注释代码
- **DeviceBinding处理模块**: 处理RecipeRef和RecipeGroupName属性

### Data Flow

删除Recipe类 → 修改ViewModel命令 → 更新UI绑定 → 验证编译通过 → 测试核心功能

## Implementation Details

### Core Directory Structure

```
SunEyeVision/
├── Models/
│   ├── Recipe.cs              # 删除
│   ├── RecipeGroup.cs         # 删除
│   └── RecipeJsonConverter.cs # 删除
├── ViewModels/
│   ├── MainWindowViewModel.cs          # 修改
│   └── SolutionConfigurationDialogViewModel.cs # 修改
├── Views/
│   └── MainWindow.xaml                 # 修改
└── Data/
    └── DeviceBinding.cs                # 修改
```

### Key Code Structures

**MainWindowViewModel修改内容**:

```
// 重命名命令
public ICommand SaveCurrentRecipeCommand => _saveCurrentRecipeCommand 
    ??= new RelayCommand(ExecuteSaveCurrentRecipeCommand);

// 删除以下命令
// public ICommand SaveRecipeAsCommand { get; }
// public ICommand SwitchRecipeCommand { get; }
```

**MainWindow.xaml修改内容**:

```xml
<!-- 更新命令绑定 -->
<MenuItem Header="保存当前方案" Command="{Binding SaveCurrentSolutionCommand}"/>
<!-- 删除以下菜单项 -->
<!-- <MenuItem Header="配方另存为" Command="{Binding SaveRecipeAsCommand}"/> -->
<!-- <MenuItem Header="切换配方" Command="{Binding SwitchRecipeCommand}"/> -->
```

### Technical Implementation Plan

1. **删除Recipe核心类**

- Problem Statement: 删除3个无外部引用的核心类
- Solution Approach: 直接删除文件，确保无依赖关系
- Key Technologies: Visual Studio, 编译检查
- Implementation Steps: 

    1. 确认无外部引用
    2. 删除Recipe.cs
    3. 删除RecipeGroup.cs
    4. 删除RecipeJsonConverter.cs

- Testing Strategy: 编译项目，确认无错误

2. **清理ViewModel命令**

- Problem Statement: 重命名并删除MainWindowViewModel中的Recipe命令
- Solution Approach: 重命名SaveCurrentRecipeCommand，删除SaveRecipeAsCommand和SwitchRecipeCommand
- Implementation Steps:

    1. 重命名命令属性和方法
    2. 删除两个无用命令
    3. 更新相关方法实现

- Testing Strategy: 编译检查

3. **更新UI绑定**

- Problem Statement: 更新MainWindow.xaml中的命令绑定
- Solution Approach: 替换Command引用，删除无用菜单项
- Implementation Steps:

    1. 更新SaveCurrentRecipeCommand为SaveCurrentSolutionCommand
    2. 删除SaveRecipeAsCommand菜单项
    3. 删除SwitchRecipeCommand菜单项

- Testing Strategy: 编译检查，UI验证

4. **清理注释代码**

- Problem Statement: 删除SolutionConfigurationDialogViewModel中的约200行注释代码
- Solution Approach: 识别并删除所有注释掉的代码块
- Implementation Steps:

    1. 打开文件
    2. 定位注释代码
    3. 删除注释块

- Testing Strategy: 编译检查

5. **处理DeviceBinding属性**

- Problem Statement: 处理DeviceBinding.cs中的RecipeRef和RecipeGroupName属性
- Solution Approach: 评估属性用途，决定删除或重命名
- Implementation Steps:

    1. 分析属性使用情况
    2. 确认删除或重命名方案
    3. 执行修改

- Testing Strategy: 编译检查

### Integration Points

- Recipe核心类与DataMigrationService的集成（已确认独立，无影响）
- ViewModel与View的命令绑定集成
- DeviceBinding与其他模块的数据集成

## Technical Considerations

### Logging

- 记录删除操作的详细信息
- 记录编译错误和警告

### Performance Optimization

- 无特定性能优化需求
- 删除代码有助于提升编译速度

### Security Measures

- 删除代码前确认无安全敏感信息泄露
- 确保删除操作不破坏现有安全机制

### Scalability

- 清理遗留代码为未来扩展奠定基础
- 统一Solution架构提升可维护性

## Agent Extensions

### Skill

- **skill-creator**
- Purpose: 在清理过程中，如发现需要创建新的自动化技能来辅助代码清理或重构，使用此技能
- Expected outcome: 生成有效的技能定义，提升后续代码维护效率