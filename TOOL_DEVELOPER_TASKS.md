# Tool Developer 任务清单

## 📋 基本信息
- **Agent名称**: tool-developer
- **工作目录**: `d:/MyWork/SunEyeVision_Dev-tool`
- **工作分支**: `feature/tool-improvement`
- **目标**: 优化工具插件（以ThresholdTool为例）

---

## 🎯 核心任务

### 任务1：分析现有工具代码

**文件**: `tools/SunEyeVision.Tool.Threshold/`

**需求**:
- 分析ThresholdTool的当前实现
- 识别需要优化的部分
- 理解工具插件的工作机制

**分析要点**:
- 查看工具的ViewModel
- 查看工具的参数系统
- 查看工具的UI实现
- 查看工具的执行逻辑

**输出**: 创建分析文档 `tools/SunEyeVision.Tool.Threshold/ANALYSIS.md`

**提交**:
```bash
cd d:/MyWork/SunEyeVision_Dev-tool
git add tools/SunEyeVision.Tool.Threshold/ANALYSIS.md
git commit -m "docs: 添加ThresholdTool分析文档"
git push origin feature/tool-improvement
```

---

### 任务2：优化参数系统

**需求**:
- 简化参数定义
- 提高参数系统的可扩展性
- 优化参数UI绑定

**实现要点**:
- 使用 `ToolParameters` 基类
- 使用 `ParameterAttribute` 特性
- 优化参数验证逻辑

**文件**:
- `tools/SunEyeVision.Tool.Threshold/Models/ThresholdParameters.cs`
- `tools/SunEyeVision.Tool.Threshold/ViewModels/ThresholdToolViewModel.cs`

**提交**:
```bash
git add tools/SunEyeVision.Tool.Threshold/Models/ThresholdParameters.cs
git add tools/SunEyeVision.Tool.Threshold/ViewModels/ThresholdToolViewModel.cs
git commit -m "refactor: 优化ThresholdTool参数系统"
git push origin feature/tool-improvement
```

---

### 任务3：优化UI交互

**需求**:
- 改进工具UI的用户体验
- 优化参数面板布局
- 改进图像显示效果

**实现要点**:
- 使用项目提供的UI控件（如ColorPicker, ToggleSwitch等）
- 优化布局结构
- 添加实时预览

**文件**:
- `tools/SunEyeVision.Tool.Threshold/Views/ThresholdToolDebugControl.xaml`
- `tools/SunEyeVision.Tool.Threshold/Views/ThresholdToolDebugControl.xaml.cs`

**提交**:
```bash
git add tools/SunEyeVision.Tool.Threshold/Views/ThresholdToolDebugControl.*
git commit -m "refactor: 优化ThresholdTool UI交互"
git push origin feature/tool-improvement
```

---

### 任务4：优化性能

**需求**:
- 优化图像处理算法
- 减少内存占用
- 提高处理速度

**实现要点**:
- 使用高效的OpenCV函数
- 避免不必要的内存拷贝
- 使用并行处理（如果适用）

**文件**:
- `tools/SunEyeVision.Tool.Threshold/Core/ThresholdProcessor.cs`

**提交**:
```bash
git add tools/SunEyeVision.Tool.Threshold/Core/ThresholdProcessor.cs
git commit -m "perf: 优化ThresholdTool性能"
git push origin feature/tool-improvement
```

---

### 任务5：添加高级功能

**需求**:
- 添加自动阈值算法（如Otsu）
- 添加自适应阈值
- 添加ROI支持

**实现要点**:
- 扩展ThresholdParameters
- 实现多种阈值算法
- 集成ROI功能

**文件**:
- `tools/SunEyeVision.Tool.Threshold/Models/ThresholdParameters.cs`
- `tools/SunEyeVision.Tool.Threshold/Core/ThresholdProcessor.cs`
- `tools/SunEyeVision.Tool.Threshold/Views/ThresholdToolDebugControl.xaml`

**提交**:
```bash
git add tools/SunEyeVision.Tool.Threshold/Models/ThresholdParameters.cs
git add tools/SunEyeVision.Tool.Threshold/Core/ThresholdProcessor.cs
git add tools/SunEyeVision.Tool.Threshold/Views/ThresholdToolDebugControl.*
git commit -m "feat: 添加ThresholdTool高级功能"
git push origin feature/tool-improvement
```

---

### 任务6：优化DebugControl

**需求**:
- 改进DebugControl的布局
- 添加参数历史记录
- 添加参数预设功能

**实现要点**:
- 使用项目提供的UI控件
- 优化参数面板
- 添加预设管理

**文件**:
- `tools/SunEyeVision.Tool.Threshold/Models/ThresholdDisplayConfig.cs`
- `tools/SunEyeVision.Tool.Threshold/Views/ThresholdToolDebugControl.xaml`
- `tools/SunEyeVision.Tool.Threshold/Views/ThresholdToolDebugControl.xaml.cs`

**提交**:
```bash
git add tools/SunEyeVision.Tool.Threshold/Models/ThresholdDisplayConfig.cs
git add tools/SunEyeVision.Tool.Threshold/Views/ThresholdToolDebugControl.*
git commit -m "feat: 优化ThresholdTool DebugControl"
git push origin feature/tool-improvement
```

---

### 任务7：添加测试

**需求**:
- 编写单元测试
- 编写集成测试
- 测试各种阈值算法

**文件**: `tests/Tools/ThresholdToolTests.cs`

**提交**:
```bash
git add tests/Tools/ThresholdToolTests.cs
git commit -m "test: 添加ThresholdTool测试"
git push origin feature/tool-improvement
```

---

### 任务8：更新文档

**需求**:
- 更新工具README
- 添加使用示例
- 添加参数说明

**文件**:
- `tools/SunEyeVision.Tool.Threshold/README.md`
- `docs/tools/threshold-tool.md`

**提交**:
```bash
git add tools/SunEyeVision.Tool.Threshold/README.md
git add docs/tools/threshold-tool.md
git commit -m "docs: 更新ThresholdTool文档"
git push origin feature/tool-improvement
```

---

### 任务9：优化其他工具（可选）

**需求**:
- 应用相同的优化模式到其他工具
- 创建工具优化模板
- 提取通用代码

**文件**: 根据需要选择工具进行优化

**提交**:
```bash
git add tools/...
git commit -m "refactor: 优化其他工具"
git push origin feature/tool-improvement
```

---

## 🔄 开发流程

### 日常开发
```powershell
# 1. 切换到工作目录
cd d:/MyWork/SunEyeVision_Dev-tool

# 2. 拉取最新代码（如果有）
git pull origin feature/tool-improvement

# 3. 开发功能
# ... 编写代码 ...

# 4. 编译验证
dotnet build

# 5. 提交代码
git add .
git commit -m "refactor: 描述你的修改"
git push origin feature/tool-improvement
```

### 同步主分支
```powershell
# 定期同步主分支的最新修改
cd d:/MyWork/SunEyeVision_Dev-tool
git fetch origin main
git rebase origin/main
```

---

## 📊 进度跟踪

- [ ] 任务1：分析现有工具代码
- [ ] 任务2：优化参数系统
- [ ] 任务3：优化UI交互
- [ ] 任务4：优化性能
- [ ] 任务5：添加高级功能
- [ ] 任务6：优化DebugControl
- [ ] 任务7：添加测试
- [ ] 任务8：更新文档
- [ ] 任务9：优化其他工具（可选）

---

## 🚨 注意事项

1. **只在 feature/tool-improvement 分支工作**
2. **不要修改 feature/camera-type-support 分支的代码**
3. **定期同步主分支**
4. **提交前先编译验证**
5. **遵循项目编码规范**
   - 使用 `ObservableObject` 基类
   - 使用项目日志系统
   - 使用 PascalCase 命名
   - 使用项目提供的UI控件（ColorPicker, ToggleSwitch等）

---

## 💡 优化原则

### 1. 参数系统优化
- 使用 `ToolParameters` 基类
- 使用 `ParameterAttribute` 特性
- 提供默认值
- 添加参数验证

### 2. UI优化
- 使用项目提供的UI控件
- 保持一致的视觉风格
- 优化布局结构
- 添加实时预览

### 3. 性能优化
- 使用高效的OpenCV函数
- 避免不必要的内存拷贝
- 使用并行处理（如果适用）
- 优化算法复杂度

### 4. 可维护性优化
- 提取通用代码
- 添加充分的注释
- 编写单元测试
- 更新文档

---

## 💬 联系方式

- Team Lead: `team-lead@camera-tool-dev`
- Camera Developer: `camera-developer@camera-tool-dev`
