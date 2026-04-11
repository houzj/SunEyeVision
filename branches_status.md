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
---

## 分支2：feature/tool-improvement

**状态**: 🟢 已合并到 main 分支
**合并时间**: 2026-04-11 22:47
**合并提交**: 7dc17f3
**目标功能**: 优化现有工具插件

### 已完成
- [x] 创建分支
- [x] 完成工具插件优化
- [x] 完全重构数据源管理（查询、绑定、选择）
- [x] 实现基于变量池的设计时绑定框架
- [x] 优化 ThresholdTool 参数系统
- [x] 新增日志面板，移除属性面板
- [x] 完善开发规范文档
- [x] 合并到 main 分支

### 合并统计
- 提交数量: 16个
- 文件变更: 127个
- 新增代码: +18,277行
- 删除代码: -6,024行
- 冲突文件: 6个（已全部解决）

### 待办任务
- [ ] 编译测试
- [ ] 数据源查询功能测试
- [ ] 参数绑定功能测试
- [ ] 工具执行功能测试
- [ ] UI 功能验证
- [ ] 测试通过后推送到远程
- [ ] 删除远程工具分支（可选）
- [ ] 删除本地工具分支（可选）

### 开发注意事项
- 优先级: 🟢 已完成
- 依赖: Plugin.SDK、具体工具模块
- 实际时间: 约20小时

---

**最后更新**: 2026-04-11 22:47
---

## 分支1：feature/camera-type-support

**状态**: 🟢 已合并到 main 分支
**合并时间**: 2026-04-11 23:00
**合并提交**: 0312fe4
**目标功能**: 支持多种相机类型（IP、USB、GigE）

### 已完成
- [x] 创建分支
- [x] 实现相机自动发现功能
- [x] 重构相机管理器系统
- [x] 实现海康相机驱动
- [x] 优化相机管理器UI
- [x] 新增相机设备驱动架构
- [x] 完善相机开发文档
- [x] 合并到 main 分支

### 合并统计
- 提交数量: 9个
- 文件变更: 81个
- 新增代码: +13,057行
- 删除代码: -866行
- 冲突文件: 1个（已全部解决）

### 待办任务
- [ ] 编译测试
- [ ] 相机自动发现功能测试
- [ ] 相机管理器功能测试
- [ ] 相机参数配置测试
- [ ] UI 功能验证
- [ ] 测试通过后推送到远程
- [ ] 删除远程相机分支（可选）
- [ ] 删除本地相机分支（可选）
- [ ] 实现大华相机驱动（后续）
- [ ] 实现USB相机驱动（后续）
- [ ] 实现GigE相机驱动（后续）

### 开发注意事项
- 优先级: 🟢 已完成
- 依赖: Plugin.SDK、DeviceDriver模块
- 实际时间: 约20小时

---

**最后更新**: 2026-04-11 23:00