# 配方管理功能修复总结

## 问题描述

1. **新建项目**没有弹出对话框，直接使用默认值
2. **添加配方**没有弹出对话框，直接使用默认值
3. **配方另存为**功能未实现（TODO 状态）
4. 路径配置逻辑不明确

## 设计原则

根据用户需求和架构设计：

1. **添加配方**：
   - 不弹出路径配置框
   - 只弹出 NewRecipeDialog 让用户输入名称和描述
   - 配方文件路径自动生成：`solutions/projects/{projectId}/recipes/{recipeName}.json`
   - 配方归属于项目，使用项目路径的相对路径

2. **复制配方**：
   - 复制到同一项目
   - 不弹出路径配置框
   - 文件路径自动生成

3. **另存配方**：
   - **需要弹出路径配置框**
   - 让用户选择目标项目和输入新配方名称
   - 支持跨项目另存和同一项目另存
   - 文件路径自动生成

4. **新建项目**：
   - 弹出 NewProjectDialog 让用户输入名称、产品编码、描述
   - 项目路径自动生成：`solutions/projects/{projectId}/`

## 实施方案

### 1. 新建项目功能修复

**文件**: `src/UI/ViewModels/SolutionConfigurationDialogViewModel.cs`

**修改前**:
```csharp
private void NewProject()
{
    // TODO: 打开新建项目对话框
    // 这里先使用默认值
    var newProject = _projectManager.CreateProject(uniqueName, "NEW-001", "新建项目描述");
    // ...
}
```

**修改后**:
```csharp
private void NewProject()
{
    // 打开新建项目对话框
    var dialog = new NewProjectDialog();
    var result = dialog.ShowDialog();

    if (result == true)
    {
        string projectName = dialog.ProjectName;
        string productCode = dialog.ProductCode;
        string description = dialog.Description ?? string.Empty;

        var newProject = _projectManager.CreateProject(projectName, productCode, description);
        // ...
    }
}
```

### 2. 添加配方功能修复

**文件**: `src/UI/ViewModels/SolutionConfigurationDialogViewModel.cs`

**修改前**:
```csharp
private void AddRecipe()
{
    // TODO: 打开新建配方对话框
    var newRecipe = SelectedProject.AddRecipe(uniqueName);
    // ...
}
```

**修改后**:
```csharp
private void AddRecipe()
{
    // 打开新建配方对话框
    var dialog = new NewRecipeDialog();
    var result = dialog.ShowDialog();

    if (result == true)
    {
        string recipeName = dialog.RecipeName;
        string description = dialog.Description ?? string.Empty;

        var newRecipe = SelectedProject.AddRecipe(recipeName, description);
        
        // 保存项目
        _projectManager.SaveProject(SelectedProject);
        // ...
    }
}
```

### 3. 配方另存为功能实现

**新增文件**:
- `src/UI/Views/Windows/SaveRecipeAsDialog.xaml`
- `src/UI/Views/Windows/SaveRecipeAsDialog.xaml.cs`
- `src/UI/ViewModels/SaveRecipeAsDialogViewModel.cs`

**功能**:
- 弹出对话框让用户选择目标项目
- 输入新配方名称和描述
- 支持跨项目另存和同一项目另存
- 同一项目：刷新配方列表并选中新配方
- 跨项目：显示成功提示消息

**修改文件**: `src/UI/ViewModels/SolutionConfigurationDialogViewModel.cs`

**实现逻辑**:
```csharp
private void SaveRecipeAs()
{
    // 弹出配方另存为对话框
    var dialog = new SaveRecipeAsDialog(
        currentProjectId: SelectedProject.Id,
        currentRecipeName: SelectedRecipe.Name,
        currentDescription: SelectedRecipe.Description,
        projects: Projects.ToList());

    if (dialog.ShowDialog() == true)
    {
        string targetProjectId = dialog.TargetProjectId;
        string newRecipeName = dialog.RecipeName;
        string newDescription = dialog.Description ?? string.Empty;

        InspectionRecipe? newRecipe = null;

        // 检查是否是同一项目
        if (targetProjectId == SelectedProject.Id)
        {
            // 同一项目内另存为
            newRecipe = _projectManager.SaveRecipeAs(
                SelectedProject.Id,
                SelectedRecipe.Name,
                newRecipeName,
                newDescription);

            // 刷新配方列表
            UpdateRecipesList();
            SelectedRecipe = Recipes.FirstOrDefault(r => r.Id == newRecipe?.Id);
        }
        else
        {
            // 跨项目另存为
            var targetProject = Projects.FirstOrDefault(p => p.Id == targetProjectId);
            if (targetProject != null)
            {
                // 深度复制配方
                newRecipe = CloneRecipe(SelectedRecipe, targetProject.Id, newRecipeName, newDescription);
                
                // 添加到目标项目
                targetProject.Recipes.Add(newRecipe);
                
                // 保存目标项目
                _projectManager.SaveProject(targetProject);

                // 提示用户
                MessageBox.Show($"配方已另存为到项目：{targetProject.Name}\n配方名称：{newRecipe.Name}",
                    "另存为成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        // ...
    }
}
```

**辅助方法**:
```csharp
/// <summary>
/// 克隆配方（深度复制）
/// </summary>
private InspectionRecipe CloneRecipe(InspectionRecipe sourceRecipe, string targetProjectId, 
                                    string newName, string newDescription)
{
    var newRecipe = new InspectionRecipe
    {
        Name = newName,
        Description = newDescription,
        ProjectId = targetProjectId,
        Device = sourceRecipe.Device,
        CreatedTime = DateTime.Now,
        ModifiedTime = DateTime.Now
    };

    // 复制节点参数
    foreach (var param in sourceRecipe.NodeParams)
    {
        newRecipe.NodeParams[param.Key] = param.Value.Clone();
    }

    // 复制全局变量
    foreach (var variable in sourceRecipe.GlobalVariables)
    {
        newRecipe.GlobalVariables[variable.Key] = new GlobalVariable
        {
            Name = variable.Value.Name,
            Value = variable.Value.Value,
            Type = variable.Value.Type,
            Description = variable.Value.Description
        };
    }

    return newRecipe;
}
```

## 功能对比

| 功能 | 是否弹出对话框 | 是否显示路径配置 | 文件路径 |
|------|---------------|-----------------|----------|
| 新建项目 | ✅ NewProjectDialog | ❌ | 自动生成：`solutions/projects/{projectId}/` |
| 添加配方 | ✅ NewRecipeDialog | ❌ | 自动生成：`solutions/projects/{projectId}/recipes/{recipeName}.json` |
| 复制配方 | ❌ 自动复制 | ❌ | 自动生成：同上 |
| 另存配方 | ✅ SaveRecipeAsDialog | ✅ 选择目标项目 | 自动生成：同上（跨项目） |

## 用户体验

### 新建项目
1. 点击"新建项目"按钮
2. 弹出 NewProjectDialog
3. 输入项目名称、产品编码、描述
4. 点击确定
5. 项目创建成功，自动选中

### 添加配方
1. 选中一个项目
2. 点击"添加配方"按钮
3. 弹出 NewRecipeDialog
4. 输入配方名称、描述
5. 点击确定
6. 配方创建成功，自动选中，配方文件自动保存到项目目录

### 另存配方
1. 选中一个配方
2. 点击"另存配方"按钮
3. 弹出 SaveRecipeAsDialog
4. 选择目标项目、输入新配方名称、输入描述
5. 点击确定
6. 如果是同一项目：刷新配方列表，选中新配方
7. 如果是跨项目：显示成功提示消息

## 技术细节

### 配方文件路径规则

```
solutions/
├── projects/
│   ├── {projectId}/
│   │   ├── project.json
│   │   ├── program.json
│   │   └── recipes/
│   │       ├── {recipeName}.json
│   │       └── {otherRecipeName}.json
```

**规则**:
- 配方文件位于对应项目的 `recipes` 目录下
- 文件名：`{recipeName}.json`
- 自动处理特殊字符，生成安全的文件名

### 跨项目另存实现

1. 深度复制源配方（包括节点参数、全局变量等）
2. 修改 ProjectId 为目标项目 ID
3. 修改 CreatedTime 和 ModifiedTime 为当前时间
4. 添加到目标项目的 Recipes 集合
5. 调用 `_projectManager.SaveProject(targetProject)` 保存
6. 显示成功提示消息

## 相关文件清单

### 修改的文件
- `src/UI/ViewModels/SolutionConfigurationDialogViewModel.cs`
  - `NewProject()` 方法：添加对话框弹出逻辑
  - `AddRecipe()` 方法：添加对话框弹出逻辑和保存调用
  - `SaveRecipeAs()` 方法：完整实现配方另存为功能
  - `CloneRecipe()` 方法：新增辅助方法

### 新增的文件
- `src/UI/Views/Windows/SaveRecipeAsDialog.xaml`
  - 配方另存为对话框 UI 定义

- `src/UI/Views/Windows/SaveRecipeAsDialog.xaml.cs`
  - 配方另存为对话框代码后台

- `src/UI/ViewModels/SaveRecipeAsDialogViewModel.cs`
  - 配方另存为对话框 ViewModel

### 现有文件（未修改）
- `src/UI/Views/Windows/NewProjectDialog.xaml` - 已存在
- `src/UI/Views/Windows/NewProjectDialog.xaml.cs` - 已存在
- `src/UI/Views/Windows/NewRecipeDialog.xaml` - 已存在
- `src/UI/Views/Windows/NewRecipeDialog.xaml.cs` - 已存在

## 测试建议

### 1. 新建项目测试
- [ ] 点击"新建项目"按钮
- [ ] 验证 NewProjectDialog 正确弹出
- [ ] 输入项目信息并确认
- [ ] 验证项目创建成功并选中
- [ ] 检查项目文件是否正确保存

### 2. 添加配方测试
- [ ] 选中一个项目
- [ ] 点击"添加配方"按钮
- [ ] 验证 NewRecipeDialog 正确弹出
- [ ] 输入配方信息并确认
- [ ] 验证配方创建成功并选中
- [ ] 检查配方文件是否正确保存到项目目录

### 3. 另存配方测试 - 同一项目
- [ ] 选中一个配方
- [ ] 点击"另存配方"按钮
- [ ] 验证 SaveRecipeAsDialog 正确弹出
- [ ] 选择当前项目、输入新配方名称
- [ ] 验证配方创建成功并选中
- [ ] 检查配方文件是否正确保存

### 4. 另存配方测试 - 跨项目
- [ ] 选中一个配方
- [ ] 点击"另存配方"按钮
- [ ] 选择另一个项目
- [ ] 验证成功提示消息正确显示
- [ ] 切换到目标项目，验证配方是否存在

## 已知限制

1. **配方名称冲突检查**：
   - 目前只检查同一项目内的名称冲突
   - 跨项目另存时不检查名称冲突

2. **配方引用**：
   - 配方中的节点引用需要保持有效
   - 如果目标项目缺少相关节点，可能有问题

3. **撤销功能**：
   - 目前不支持撤销操作

## 后续优化建议

1. **添加配方名称唯一性验证**：
   - 跨项目另存时检查目标项目中是否已存在同名配方

2. **配方引用完整性检查**：
   - 在另存配方时验证所有引用的节点在目标项目中是否存在

3. **添加撤销/重做功能**：
   - 支持撤销配方操作

4. **配方导入/导出**：
   - 支持将配方导出到文件
   - 支持从文件导入配方

## 修复日期

2026-03-13

## 修复人员

AI Assistant
