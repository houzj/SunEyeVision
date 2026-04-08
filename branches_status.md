# 分支状态看板

---

## 分支1：feature/camera-type-support

**状态**: 🟢 已创建，准备开始开发
**创建时间**: 2026-04-08 08:14
**目标功能**: 支持多种相机类型（IP、USB、GigE）

### 待办任务
- [ ] 定义 Device 基类
- [ ] 实现 IpCameraDevice 类
- [ ] 实现 UsbCameraDevice 类
- [ ] 实现 GigECameraDevice 类
- [ ] 定义 ICameraProvider 接口
- [ ] 实现 HikvisionProvider
- [ ] 实现 DahuaProvider
- [ ] 更新 CameraManagerViewModel
- [ ] 更新 CameraManagerDialog UI
- [ ] 添加单元测试
- [ ] 集成测试

### 已完成
- [x] 创建分支

### 开发注意事项
- 优先级：🔴 高
- 依赖：Plugin.SDK、Workflow 模块
- 预估时间：10-15小时

---

## 分支2：feature/tool-improvement

**状态**: 🟢 已创建，准备开始开发
**创建时间**: 2026-04-08 08:14
**目标功能**: 优化现有工具插件（具体功能待定）

### 待办任务
- [ ] 确定优化目标
- [ ] 分析现有工具代码
- [ ] 设计优化方案
- [ ] 实现优化
- [ ] 添加单元测试
- [ ] 集成测试

### 已完成
- [x] 创建分支

### 开发注意事项
- 优先级：🟡 中
- 依赖：Plugin.SDK、具体工具模块
- 预估时间：5-8小时（根据具体优化目标）

---

## 主分支：main

**状态**: 🟢 最新
**最后同步**: 2026-04-08 08:14

### 当前修改
```
M src/Plugin.SDK/UI/Controls/ColorPicker/ColorPicker.cs
M src/Plugin.SDK/UI/Controls/ColorPicker/ColorPickerPalette.cs
M src/Plugin.SDK/UI/Controls/ColorPicker/ColorSelector.cs
M src/Plugin.SDK/UI/Controls/ColorPicker/HsvColor.cs
M src/Plugin.SDK/UI/Themes/Generic.xaml
M tools/SunEyeVision.Tool.Threshold/Models/ThresholdDisplayConfig.cs
M tools/SunEyeVision.Tool.Threshold/Views/ThresholdToolDebugControl.xaml
```

---

## 冲突检测

**当前状态**: ✅ 无冲突

---

## 合并计划

### 阶段1：独立开发
- [ ] 分支1完成开发和测试
- [ ] 分支2完成开发和测试

### 阶段2：顺序合并
- [ ] 先合并分支2（feature/tool-improvement）到 main
- [ ] 再合并分支1（feature/camera-type-support）到 main

### 阶段3：测试验证
- [ ] 编译测试
- [ ] 集成测试
- [ ] 功能验证

---

## 快速操作

```bash
# 切换到相机管理器分支
.\manage_branches.ps1 -Branch camera

# 切换到工具插件分支
.\manage_branches.ps1 -Branch tool

# 切换到主分支
.\manage_branches.ps1 -Branch main

# 查看分支状态
git branch -a

# 查看当前分支
git branch --show-current
```

---

**最后更新**: 2026-04-08 08:14
