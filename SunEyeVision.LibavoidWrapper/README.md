# SunEyeVision.LibavoidWrapper

## 项目说明

SunEyeVision.LibavoidWrapper 是一个 C++/CLI 包装项目，用于将 libavoid 路由算法库集成到 SunEyeVision 项目中。该项目提供了一个托管接口，使 C# 代码能够使用 libavoid 的路径规划功能。

## 特性

- **正交路径路由**: 支持计算正交（曼哈顿）路径，避免障碍物
- **节点避让**: 自动检测并避开图表中的节点
- **批量路由**: 支持同时计算多条路径
- **端口方向支持**: 支持指定连接端口的进入/退出方向
- **性能优化**: 内置缓存机制，提高重复计算的性能

## 文件结构

```
SunEyeVision.LibavoidWrapper/
├── pch.h                    # 预编译头文件
├── pch.cpp                  # 预编译源文件
├── framework.h              # 框架头文件
├── dllmain.cpp              # DLL 入口点
├── LibavoidWrapper.h        # 主要包装头文件
├── LibavoidWrapper.cpp      # 主要包装实现
├── LibavoidRouter.h         # 路由器头文件（保留用于扩展）
├── LibavoidRouter.cpp       # 路由器实现（保留用于扩展）
└── SunEyeVision.LibavoidWrapper.vcxproj  # 项目文件
```

## 构建要求

- Visual Studio 2022 或更高版本
- Windows SDK
- .NET 9.0 或更高版本
- C++/CLI 支持

## 使用方法

### 在 C# 中使用

```csharp
using SunEyeVision.Algorithms.PathPlanning;

// 创建路径计算器
var calculator = new LibavoidPathCalculator();

// 定义起点和终点
var startPoint = new Point(100, 100);
var endPoint = new Point(300, 300);

// 定义障碍物（可选）
var obstacles = new List<Rect>
{
    new Rect(150, 150, 50, 50),
    new Rect(200, 200, 50, 50)
};

// 计算路径
var path = calculator.CalculateOrthogonalPath(
    startPoint, 
    endPoint, 
    obstacles
);

// 使用路径
foreach (var point in path)
{
    Console.WriteLine($"Point: ({point.X}, {point.Y})");
}
```

### 批量路由

```csharp
// 定义多个路由请求
var requests = new List<PathRequest>
{
    new PathRequest(new Point(100, 100), new Point(300, 300)),
    new PathRequest(new Point(400, 400), new Point(600, 600))
};

// 批量计算路径
var results = calculator.RouteMultiplePaths(requests);
```

## API 参考

### LibavoidPathCalculator

主要路径计算类，实现 `IPathCalculator` 接口。

#### 方法

- `CalculateOrthogonalPath(Point start, Point end, IEnumerable<Rect> obstacles)`
  - 计算从起点到终点的正交路径
  - 参数:
    - `start`: 起点坐标
    - `end`: 终点坐标
    - `obstacles`: 障碍物集合（可选）
  - 返回: 路径点集合

- `RouteMultiplePaths(IEnumerable<PathRequest> requests)`
  - 批量计算多条路径
  - 参数:
    - `requests`: 路径请求集合
  - 返回: 路由结果集合

- `ClearCache()`
  - 清除内部缓存

## 性能说明

- 当前实现使用简化的正交路由算法
- 内置缓存机制可显著提高重复计算的性能
- 对于复杂的图表，建议使用批量路由方法
- 避免在频繁更新的场景中使用（如实时动画）

## 未来扩展

计划在未来版本中添加以下功能：

- 集成完整的 libavoid 库
- 支持曲线路由
- 支持更复杂的路由约束
- 性能优化和并行计算
- 支持动态障碍物

## 许可证

本项目遵循 SunEyeVision 项目的许可证。

## 联系方式

如有问题或建议，请联系项目维护者。
