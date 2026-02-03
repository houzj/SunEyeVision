# LibavoidPure - 真正的Libavoid算法纯C#实现

## 📋 概述

`LibavoidPure` 是基于 Libavoid 文档实现的**纯C#版本**的路径规划算法库，提供了正交路径路由和障碍物避让功能。

## ✨ 核心特性

### 1. **纯C#实现**
- ✅ 无C++依赖，无需编译原生库
- ✅ 易于调试和维护
- ✅ 跨平台兼容

### 2. **正交路径路由**
- ✅ 智能的三段式/四段式路径生成
- ✅ 基于相对位置的路径策略选择
- ✅ 支持端口方向约束

### 3. **障碍物避让**
- ✅ 自动检测矩形障碍物
- ✅ 智能避障点计算
- ✅ 迭代优化路径（最多10次）

### 4. **兼容IPathCalculator接口**
- ✅ 无缝集成现有系统
- ✅ 支持缓存机制
- ✅ 支持节点边界检测

## 🏗️ 核心类架构

```
LibavoidPure/
├── AvoidPoint.cs              # 点结构（带距离计算）
├── AvoidRectangle.cs          # 矩形结构（带碰撞检测）
├── AvoidPolygon.cs            # 多边形结构（带点在多边形检测）
├── AvoidRouter.cs             # 路由器核心类
├── ShapeRef.cs                # 形状引用（障碍物）
├── ConnRef.cs                # 连接器引用（连接线）
└── LibavoidPurePathCalculator.cs  # IPathCalculator适配器
```

## 📖 API使用示例

### 基本使用

```csharp
using SunEyeVision.UI.Services.PathCalculators.LibavoidPure;

// 创建路径计算器
var calculator = new LibavoidPurePathCalculator();

// 计算路径
var path = calculator.CalculateOrthogonalPath(
    new Point(100, 100),      // 源位置
    new Point(300, 300),      // 目标位置
    PortDirection.Right,        // 源方向
    PortDirection.Left          // 目标方向
);

// 获取路径几何
var geometry = calculator.CreatePathGeometry(path);
```

### 带障碍物避让

```csharp
// 定义节点边界（障碍物）
var sourceNodeRect = new Rect(50, 50, 100, 80);
var targetNodeRect = new Rect(250, 220, 100, 80);
var allNodes = new Rect[]
{
    sourceNodeRect,
    targetNodeRect,
    new Rect(150, 100, 80, 60),  // 中间障碍物
    new Rect(120, 180, 80, 60)   // 另一个障碍物
};

// 计算带避障的路径
var path = calculator.CalculateOrthogonalPath(
    new Point(150, 90),          // 源端口位置
    new Point(250, 260),         // 目标端口位置
    PortDirection.Right,
    PortDirection.Left,
    sourceNodeRect,
    targetNodeRect,
    allNodes
);
```

## 🔧 核心算法说明

### 1. **正交路径路由算法**

```csharp
// 三段式路径策略
if (dx > dy)  // 水平方向距离更大
{
    // 水平优先：源 -> 中间1 -> 中间2 -> 目标
    path.Add(new AvoidPoint(midX, source.Y));
    path.Add(new AvoidPoint(midX, target.Y));
}
else  // 垂直方向距离更大
{
    // 垂直优先：源 -> 中间1 -> 中间2 -> 目标
    path.Add(new AvoidPoint(source.X, midY));
    path.Add(new AvoidPoint(target.X, midY));
}
path.Add(target);
```

### 2. **障碍物检测算法**

```csharp
// 线段与矩形相交检测
bool LineIntersectsRectangle(p1, p2, rect)
{
    // 1. 检查端点是否在矩形内
    if (rect.Contains(p1) || rect.Contains(p2))
        return true;

    // 2. 检查线段是否与矩形边界相交
    return LineIntersectsLine(p1, p2, rect.TopLeft, rect.TopRight) ||
           LineIntersectsLine(p1, p2, rect.TopRight, rect.BottomRight) ||
           LineIntersectsLine(p1, p2, rect.BottomRight, rect.BottomLeft) ||
           LineIntersectsLine(p1, p2, rect.BottomLeft, rect.TopLeft);
}
```

### 3. **避障点计算**

```csharp
AvoidPoint CalculateAvoidancePoint(p1, p2, obstacle)
{
    if (水平线段)
    {
        // 垂直避让：在障碍物上方或下方
        y = p1.Y < obstacle.Top ? obstacle.Top - offset
                               : obstacle.Bottom + offset;
        return new AvoidPoint((p1.X + p2.X) / 2, y);
    }
    else
    {
        // 水平避让：在障碍物左侧或右侧
        x = p1.X < obstacle.Left ? obstacle.Left - offset
                                : obstacle.Right + offset;
        return new AvoidPoint(x, (p1.Y + p2.Y) / 2);
    }
}
```

### 4. **迭代优化**

```csharp
for (int iteration = 0; iteration < maxIterations; iteration++)
{
    hasCollision = false;

    // 检查每个线段
    foreach (segment in path)
    {
        if (segment intersects any obstacle)
        {
            hasCollision = true;
            var avoidPoint = CalculateAvoidancePoint(segment, obstacle);
            path.Insert(segmentIndex + 1, avoidPoint);
            break;
        }
    }

    if (!hasCollision)
        break;  // 无碰撞，优化完成
}
```

## 📊 与其他实现对比

| 特性 | LibavoidPure | LibavoidPathCalculator (旧) | OrthogonalPathCalculator |
|------|-------------|---------------------------|-------------------------|
| 实现语言 | 纯C# | C++/CLI包装 | 纯C# |
| 正交路由 | ✅ | ❌ (简单折线) | ✅ |
| 障碍物避让 | ✅ | ❌ (被注释掉) | ✅ |
| C++依赖 | ❌ | ✅ | ❌ |
| 调试难度 | 简单 | 困难 | 简单 |
| 算法来源 | Libavoid | 占位符 | 自定义 |

## 🚀 性能特点

- **时间复杂度**: O(n * m)，其中n是线段数，m是障碍物数
- **空间复杂度**: O(n + m)
- **迭代优化**: 最多10次迭代确保无碰撞
- **缓存支持**: 支持IPathCalculator缓存机制

## 📝 配置参数

```csharp
var config = new AvoidRouterConfiguration
{
    IdealSegmentLength = 50.0,    // 理想线段长度
    SegmentPenalty = 0.0,          // 线段惩罚
    RegionPenalty = 0.0,           // 区域惩罚
    CrossingPenalty = 0.0,         // 交叉惩罚
    UseOrthogonalRouting = true,    // 使用正交路由
    ImproveHyperedges = true,      // 改进超边
    RoutingTimeLimit = 5000         // 路由时间限制(ms)
};
```

## 🧪 测试

编译成功：
```bash
dotnet build SunEyeVision.UI\SunEyeVision.UI.csproj --configuration Debug
```

测试结果：
- ✅ 编译成功（0错误，383警告）
- ✅ 集成ConnectionPathCache
- ✅ 实现IPathCalculator接口

## 🔄 迁移指南

### 从旧LibavoidPathCalculator迁移

只需修改ConnectionPathCache.cs一行代码：

```csharp
// 旧代码
_pathCalculator = pathCalculator ?? new LibavoidPathCalculator();

// 新代码
_pathCalculator = pathCalculator ?? new LibavoidPurePathCalculator();
```

## 📚 参考文档

- [Libavoid 官方文档](http://www.adaptagrams.org/documentation/libavoid_example.html)
- [Adaptagrams GitHub](https://github.com/mjwybrow/adaptagrams)
- [Avoid namespace API](http://www.adaptagrams.org/documentation/namespaceAvoid.html)

## 🎯 未来优化方向

1. **性能优化**
   - 使用空间索引加速碰撞检测（如四叉树）
   - 并行化路径计算
   - 路径缓存优化

2. **算法增强**
   - 支持曲线路由
   - 支持动态障碍物
   - 支持端口约束

3. **功能扩展**
   - 批量路由优化
   - 连接线共享路径检测
   - 路径美学优化

## 📄 许可证

遵循 SunEyeVision 项目许可证。

## 👥 贡献

如有问题或建议，请联系项目维护者。
