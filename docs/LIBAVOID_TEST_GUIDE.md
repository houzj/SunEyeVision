# LibavoidPathCalculator 测试指南

**更新时间**: 2026-02-02

## 快速测试步骤

### 1. 启动程序

运行程序：
```
d:\MyWork\SunEyeVision\SunEyeVision\SunEyeVision.UI\bin\Release\net9.0-windows\SunEyeVision.UI.exe
```

### 2. 查看调试输出

在 Visual Studio 中：
1. 点击菜单 **调试** → **窗口** → **输出**
2. 在输出窗口中选择 **调试** 选项
3. 清空现有日志（便于观察）

### 3. 创建连线测试

在工作流画布上创建连线：
1. 从一个节点拖拽到另一个节点
2. 观察 Visual Studio 的输出窗口

## 如何识别使用的路径计算器

### ✅ 如果使用 LibavoidPathCalculator

你会在输出窗口看到：

```
╔════════════════════════════════════════════════════════════╗
║     🚀 LibavoidPathCalculator.CalculateOrthogonalPath    ║
╚════════════════════════════════════════════════════════════╝
[Libavoid] 源点: (100.0, 100.0), 方向: Right
[Libavoid] 目标点: (300.0, 200.0), 方向: Left
[Libavoid] 开始初始化路由器...
[Libavoid] === 开始初始化 LibavoidRouter ===
[Libavoid] 检查 config 是否为 null...
[Libavoid] config 不为 null，准备创建 LibavoidRouter
[Libavoid] 调用 new LibavoidRouter(config)...
[Libavoid] ✅ 路由器初始化完成
[Libavoid] ✅ 路由器初始化完成
[Libavoid] 开始调用 router.RoutePath...
[Libavoid] ✅ 路由成功！路径点数: 4
[Libavoid] 路径点:
[Libavoid]   点1: (100.0, 100.0)
[Libavoid]   点2: (200.0, 100.0)
[Libavoid]   点3: (200.0, 200.0)
[Libavoid]   点4: (300.0, 200.0)
╔════════════════════════════════════════════════════════════╗
║     LibavoidPathCalculator 计算完成                   ║
╚════════════════════════════════════════════════════════════╝
```

**关键标识**：
- 🚀 火箭图标
- `[Libavoid]` 前缀
- 边框装饰 `╔══════╗`

### ❌ 如果使用 OrthogonalPathCalculator

你会在输出窗口看到：

```
[OrthogonalPath] ========== 开始路径计算 ==========
[OrthogonalPath] 源位置:(100.0,100.0), 目标位置（箭头尾部）:(300.0,200.0)
[OrthogonalPath] 源方向:Right, 目标方向:Left
[OrthogonalPath] 源节点边界:(90.0,90.0,180.0x80.0)
[OrthogonalPath] 目标节点边界:(210.0,160.0,180.0x80.0)
[OrthogonalPath] 碰撞检测节点数:5
[OrthogonalPath] 路径计算完成: 路径点数=4
[OrthogonalPath]   路径点[0]:(100.0,100.0)
[OrthogonalPath]   路径点[1]:(200.0,100.0)
[OrthogonalPath]   路径点[2]:(200.0,200.0)
[OrthogonalPath]   路径点[3]:(300.0,200.0)
[OrthogonalPath] ========== 路径计算完成 ==========
```

**关键标识**：
- `[OrthogonalPath]` 前缀
- 简单的 `=======` 分隔线

## 常见问题

### Q1: 程序启动后立即闪退

**原因**: LibavoidRouter 初始化失败（C++/CLI 栈溢出）

**检查**: 查看 WorkflowCanvasControl.xaml.cs 中的 try-catch 是否捕获了异常

**日志应该显示**:
```
[WorkflowCanvas] 正在创建 LibavoidPathCalculator...
[LibavoidPathCalculator] === 构造函数开始 ===
...
```

如果看不到这些日志，说明崩溃发生更早。

### Q2: 创建连线时闪退

**原因**: LibavoidRouter 延迟初始化失败

**检查**: 看日志是否显示：
```
[Libavoid] 调用 new LibavoidRouter(config)...
```

如果这行之后没有日志，说明 C++ 构造函数崩溃。

### Q3: 看到 "已切换到 OrthogonalPathCalculator 作为备用方案"

这说明 LibavoidPathCalculator 创建失败，程序自动降级到了 OrthogonalPathCalculator。

**检查**:
1. 查看之前的错误消息
2. 确认 LibavoidWrapper.dll 是否存在
3. 检查 C++/CLI DLL 是否正确部署

## 验证测试

### 测试 1: 基础连接

创建最简单的水平连接：
1. 创建两个节点
2. 节点 A 在左边，节点 B 在右边
3. 从 A 的右端口连接到 B 的左端口

**预期**: 使用 Libavoid 时，应该看到 `[Libavoid]` 日志

### 测试 2: 复杂场景

创建多个节点，产生复杂的路径：
1. 创建 4-5 个节点
2. 连接它们形成交叉路径
3. 观察路径是否合理

**预期**: Libavoid 应该能够生成智能避让的路径

### 测试 3: 对比测试

为了验证差异，可以：
1. 临时改回 OrthogonalPathCalculator
2. 记录相同场景下的路径
3. 切换回 LibavoidPathCalculator
4. 对比路径差异

## 日志分析

### 成功的 Libavoid 日志顺序：

```
1. [WorkflowCanvas] 正在创建 LibavoidPathCalculator...
2. [LibavoidPathCalculator] === 构造函数开始 ===
3. [LibavoidPathCalculator] 步骤1: 创建 RouterConfiguration
4. [LibavoidPathCalculator] 步骤2: 设置配置属性
5. [LibavoidPathCalculator] 步骤3: 配置设置完成
6. [LibavoidPathCalculator] === 构造函数成功 ===
7. [WorkflowCanvas] LibavoidPathCalculator 创建成功！
8. [Libavoid] ... (创建连线时显示)
```

### 失败的 Libavoid 日志顺序：

```
1. [WorkflowCanvas] 正在创建 LibavoidPathCalculator...
2. (如果崩溃，可能看不到后续日志)
3. 或者: [WorkflowCanvas] LibavoidPathCalculator 创建失败: 异常信息
4. [WorkflowCanvas] 已切换到 OrthogonalPathCalculator 作为备用方案
5. [OrthogonalPath] ... (后续使用 Orthogonal)
```

## 总结

**使用 LibavoidPathCalculator 的关键指标**:
- ✅ 启动时看到 `LibavoidPathCalculator 创建成功！`
- ✅ 创建连线时看到 `🚀 LibavoidPathCalculator` 标识
- ✅ 所有日志都有 `[Libavoid]` 前缀
- ✅ 没有切换到备用方案的提示

**未使用 LibavoidPathCalculator 的指标**:
- ❌ 看到 `已切换到 OrthogonalPathCalculator`
- ❌ 创建连线时只看到 `[OrthogonalPath]`
- ❌ 看到异常错误消息

## 如果仍然无法使用 LibavoidPathCalculator

**可能的解决方案**:

1. **检查 DLL 部署**:
   ```
   SunEyeVision.LibavoidWrapper.dll 应该在:
   d:\MyWork\SunEyeVision\SunEyeVision\SunEyeVision.UI\bin\Release\net9.0-windows\
   ```

2. **使用调试模式运行**:
   - 在 Visual Studio 中以 Debug 模式启动
   - 设置断点查看详细的错误信息

3. **暂时使用 OrthogonalPathCalculator**:
   - 功能完整，性能良好
   - 只是缺少 C++/CLI 的智能避让功能
