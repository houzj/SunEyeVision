# libavoid 使用示例

本文档提供了在 SunEyeVision 项目中使用 libavoid 路由算法的详细示例。

## 基础用法

### 1. 简单路径计算

```csharp
using SunEyeVision.Algorithms.PathPlanning;
using System.Windows;

public class PathCalculatorExample
{
    public void CalculateSimplePath()
    {
        // 创建路径计算器
        var calculator = new LibavoidPathCalculator();

        // 定义起点和终点
        var startPoint = new Point(100, 100);
        var endPoint = new Point(300, 300);

        // 计算路径（无障碍物）
        var path = calculator.CalculateOrthogonalPath(startPoint, endPoint);

        // 输出路径点
        Console.WriteLine("路径点:");
        foreach (var point in path)
        {
            Console.WriteLine($"  ({point.X}, {point.Y})");
        }
    }
}
```

### 2. 带障碍物的路径计算

```csharp
public void CalculateWithObstacles()
{
    var calculator = new LibavoidPathCalculator();

    // 定义起点和终点
    var startPoint = new Point(100, 100);
    var endPoint = new Point(400, 400);

    // 定义障碍物
    var obstacles = new List<Rect>
    {
        new Rect(200, 150, 50, 50),  // 障碍物1
        new Rect(250, 250, 50, 50),  // 障碍物2
        new Rect(300, 350, 50, 50)   // 障碍物3
    };

    // 计算避开障碍物的路径
    var path = calculator.CalculateOrthogonalPath(startPoint, endPoint, obstacles);

    Console.WriteLine($"找到路径，包含 {path.Count} 个点");
}
```

### 3. 指定端口方向的路径计算

```csharp
public void CalculateWithPortDirections()
{
    var calculator = new LibavoidPathCalculator();

    var startPoint = new Point(100, 100);
    var endPoint = new Point(300, 300);

    var obstacles = new List<Rect>
    {
        new Rect(150, 150, 50, 50)
    };

    // 指定起点和终点的端口方向
    var startDirection = PortDirection.Right;  // 从起点向右出发
    var endDirection = PortDirection.Left;     // 从左侧到达终点

    var path = calculator.CalculateOrthogonalPath(
        startPoint,
        endPoint,
        obstacles,
        startDirection,
        endDirection
    );

    Console.WriteLine($"使用端口方向计算路径: {path.Count} 个点");
}
```

## 高级用法

### 4. 批量路径计算

```csharp
public void CalculateMultiplePaths()
{
    var calculator = new LibavoidPathCalculator();

    // 定义多个路由请求
    var requests = new List<PathRequest>
    {
        new PathRequest(new Point(100, 100), new Point(300, 300)),
        new PathRequest(new Point(400, 400), new Point(600, 600)),
        new PathRequest(new Point(200, 200), new Point(500, 500))
    };

    // 定义共享的障碍物
    var obstacles = new List<Rect>
    {
        new Rect(250, 250, 50, 50),
        new Rect(350, 350, 50, 50)
    };

    // 批量计算路径
    var results = calculator.RouteMultiplePaths(requests, obstacles);

    // 处理结果
    foreach (var result in results)
    {
        Console.WriteLine($"路径 {result.RequestId}:");
        if (result.Success)
        {
            Console.WriteLine($"  成功，包含 {result.Path.Count} 个点");
        }
        else
        {
            Console.WriteLine($"  失败: {result.ErrorMessage}");
        }
    }
}
```

### 5. 在 WPF 中使用

```csharp
using System.Windows.Shapes;
using System.Windows.Media;

public class WpfPathExample
{
    public void DrawPath(Canvas canvas, Point start, Point end, List<Rect> obstacles)
    {
        var calculator = new LibavoidPathCalculator();

        // 计算路径
        var path = calculator.CalculateOrthogonalPath(start, end, obstacles);

        // 创建路径几何
        var geometry = new PathGeometry();
        var figure = new PathFigure();
        figure.StartPoint = path[0];

        for (int i = 1; i < path.Count; i++)
        {
            figure.Segments.Add(new LineSegment(path[i], true));
        }

        geometry.Figures.Add(figure);

        // 创建路径元素
        var pathElement = new Path
        {
            Stroke = Brushes.Blue,
            StrokeThickness = 2,
            Data = geometry
        };

        canvas.Children.Add(pathElement);
    }
}
```

### 6. 与图表编辑器集成

```csharp
public class GraphEditorPathRouting
{
    private LibavoidPathCalculator _calculator = new LibavoidPathCalculator();

    public void RouteConnections(GraphEditor editor)
    {
        // 获取所有连接
        var connections = editor.GetConnections();
        var nodes = editor.GetNodes();

        // 将节点转换为障碍物
        var obstacles = nodes.Select(n => n.Bounds).ToList();

        // 为每个连接计算路径
        foreach (var connection in connections)
        {
            var startPort = connection.StartPort;
            var endPort = connection.EndPort;

            // 计算路径
            var path = _calculator.CalculateOrthogonalPath(
                startPort.Position,
                endPort.Position,
                obstacles,
                startPort.Direction,
                endPort.Direction
            );

            // 更新连接路径
            connection.SetPath(path);
        }
    }
}
```

## 性能优化

### 7. 使用缓存

```csharp
public class CachedPathCalculator
{
    private LibavoidPathCalculator _calculator = new LibavoidPathCalculator();

    public List<Point> GetCachedPath(Point start, Point end, List<Rect> obstacles)
    {
        // 计算器会自动缓存结果
        var path = _calculator.CalculateOrthogonalPath(start, end, obstacles);
        return path;
    }

    public void ClearCache()
    {
        // 清除缓存以释放内存
        _calculator.ClearCache();
    }
}
```

### 8. 异步路径计算

```csharp
public class AsyncPathCalculator
{
    public async Task<List<Point>> CalculatePathAsync(
        Point start, 
        Point end, 
        List<Rect> obstacles)
    {
        return await Task.Run(() =>
        {
            var calculator = new LibavoidPathCalculator();
            return calculator.CalculateOrthogonalPath(start, end, obstacles);
        });
    }
}
```

## 错误处理

### 9. 处理路径计算失败

```csharp
public void HandlePathCalculationFailure()
{
    var calculator = new LibavoidPathCalculator();

    try
    {
        var path = calculator.CalculateOrthogonalPath(
            new Point(100, 100),
            new Point(300, 300),
            new List<Rect> { new Rect(150, 150, 200, 200) }  // 大障碍物
        );

        if (path.Count == 0)
        {
            Console.WriteLine("无法找到有效路径");
            // 使用备用路径
            var fallbackPath = GenerateFallbackPath(
                new Point(100, 100),
                new Point(300, 300)
            );
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"路径计算错误: {ex.Message}");
    }
}

private List<Point> GenerateFallbackPath(Point start, Point end)
{
    // 生成简单的直线路径
    return new List<Point> { start, end };
}
```

## 配置选项

### 10. 自定义路由配置

```csharp
public void ConfigureRouter()
{
    var calculator = new LibavoidPathCalculator();

    // 设置路由参数（如果支持）
    // 注意：当前简化版本可能不支持所有配置选项
    
    // 计算路径
    var path = calculator.CalculateOrthogonalPath(
        new Point(100, 100),
        new Point(300, 300)
    );
}
```

## 最佳实践

1. **重用计算器实例**: 创建一个 `LibavoidPathCalculator` 实例并重用，而不是每次计算都创建新实例
2. **批量处理**: 对于多个路径计算，使用 `RouteMultiplePaths` 方法
3. **管理缓存**: 定期调用 `ClearCache()` 释放内存
4. **异步计算**: 对于耗时的计算，使用异步方法避免阻塞 UI
5. **错误处理**: 始终处理可能的路径计算失败情况

## 故障排除

### 常见问题

1. **找不到路径**
   - 检查障碍物是否完全阻挡了路径
   - 尝试增加障碍物之间的间距
   - 使用备用路径算法

2. **性能问题**
   - 减少障碍物数量
   - 使用批量路由方法
   - 启用缓存

3. **DLL 加载错误**
   - 确保 `SunEyeVision.LibavoidWrapper.dll` 在正确的位置
   - 检查平台架构匹配（x86/x64）

## 更多资源

- [libavoid 集成指南](./libavoid集成指南.md)
- [SunEyeVision.LibavoidWrapper README](../SunEyeVision.LibavoidWrapper/README.md)
