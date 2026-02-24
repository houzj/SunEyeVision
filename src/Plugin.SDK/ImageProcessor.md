# 图像处理器高性能方案

## 📋 方案概述

本方案提供了完全基于原生图像类型的图像处理器接口，实现了零转换开销的高性能图像处理。

### 🎯 核心优势

1. **完全直接使用** - 直接使用原生图像类型（Mat），无需任何转换
2. **零转换开销** - 避免 ImageData ↔ Mat 之间的转换开销
3. **高性能** - 直接使用底层优化实现
4. **对外中立** - 接口和类名不包含特定技术字样
5. **灵活扩展** - 支持多种实现和参数类型

## 🏗️ 架构设计

```
┌─────────────────────────────────────────┐
│           IImageProcessor               │  ← 中立接口命名
│  - Process(Mat input)                   │  ← 直接使用Mat
│  - Process(Mat input, Rect roi)         │
│  - Process(Mat input, Point2f, radius)  │
└─────────────────────────────────────────┘
              ↓ 实现
┌─────────────────────────────────────────┐
│      CannyEdgeProcessor                 │  ← 具体实现
│      GaussianBlurProcessor              │
│      CustomProcessor                    │
└─────────────────────────────────────────┘
              ↓ 管理
┌─────────────────────────────────────────┐
│      ImageProcessorManager              │  ← 处理器管理
│  - RegisterProcessor()                  │
│  - Process()                            │
└─────────────────────────────────────────┘
```

## 📁 文件结构

```
src/Plugin.SDK/
├── Core/
│   ├── IImageProcessor.cs           # 图像处理器接口
│   ├── IImageProcessorManager.cs    # 管理器接口
│   └── ITool.cs                     # 现有工具接口（保持不变）
├── Managers/
│   └── ImageProcessorManager.cs     # 管理器实现
├── Implementations/
│   ├── CannyEdgeProcessor.cs        # Canny边缘检测示例
│   └── GaussianBlurProcessor.cs     # 高斯模糊示例
└── Samples/
    └── ImageProcessorExamples.cs    # 使用示例
```

## 🚀 快速开始

### 1. 创建管理器并注册处理器

```csharp
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.SDK.Managers;
using SunEyeVision.Plugin.SDK.Implementations;

// 创建管理器
var manager = new ImageProcessorManager();

// 注册处理器
manager.RegisterProcessor("canny", new CannyEdgeProcessor());
manager.RegisterProcessor("blur", new GaussianBlurProcessor());
```

### 2. 基础使用

```csharp
using OpenCvSharp;

// 直接使用Mat，无需转换
using var inputImage = Cv2.ImRead("input.jpg");

// 使用默认参数处理
using var edges = manager.Process("canny", inputImage);
Cv2.ImShow("Edges", edges);
```

### 3. 使用自定义参数

```csharp
var cannyParams = new CannyEdgeParameters
{
    Threshold1 = 30.0,
    Threshold2 = 100.0
};

using var customEdges = manager.Process("canny", inputImage, cannyParams);
Cv2.ImShow("Custom Edges", customEdges);
```

### 4. ROI处理

```csharp
// 矩形ROI
var rectRoi = new Rect(100, 100, 300, 200);
using var rectResult = manager.Process("canny", inputImage, rectRoi);

// 圆形ROI
var center = new Point2f(250, 200);
float radius = 100;
using var circleResult = manager.Process("canny", inputImage, center, radius);
```

### 5. 异步处理

```csharp
// 异步处理多个图像
var cannyTask = manager.ProcessAsync("canny", inputImage, cannyParams);
var blurTask = manager.ProcessAsync("blur", inputImage, blurParams);

await Task.WhenAll(cannyTask, blurTask);

using var cannyResult = await cannyTask;
using var blurResult = await blurTask;
```

## 🔧 自定义处理器

### 实现处理器接口

```csharp
using OpenCvSharp;
using SunEyeVision.Plugin.SDK.Core;

public class CustomProcessor : IAsyncParametricImageProcessor
{
    public string Name => "Custom";
    public string Description => "自定义处理器";
    public Type ParameterType => typeof(CustomParameters);

    public Mat Process(Mat input)
    {
        // 实现处理逻辑
        using var result = new Mat();
        // ... 处理代码 ...
        return result.Clone();
    }

    public Mat Process(Mat input, Rect roi)
    {
        using var roiMat = new Mat(input, roi);
        return Process(roiMat);
    }

    public Mat Process(Mat input, Point2f center, float radius)
    {
        // 实现圆形ROI处理
        // ...
    }

    public Mat Process(Mat input, object parameters)
    {
        var customParams = parameters as CustomParameters 
            ?? new CustomParameters();
        // 使用参数处理
        // ...
    }

    // 实现其他接口方法...
}
```

### 注册和使用

```csharp
manager.RegisterProcessor("custom", new CustomProcessor());

var customParams = new CustomParameters
{
    // 设置参数
};

using var result = manager.Process("custom", inputImage, customParams);
```

## 📊 性能对比

### 传统方案（使用适配器）

```csharp
// 需要转换
ImageData inputImageData = ...;
using var mat = inputImageData.ToMat();  // 转换开销
using var resultMat = Process(mat);
var result = resultMat.ToImageData();    // 转换开销
```

### 新方案（直接使用）

```csharp
// 无需转换
using var mat = Cv2.ImRead("input.jpg");  // 直接使用Mat
using var result = manager.Process("canny", mat);  // 直接处理
```

**性能提升：**
- ✅ 零转换开销
- ✅ 减少内存分配
- ✅ 降低GC压力
- ✅ 提升处理速度

## 🎯 适用场景

1. **实时图像处理** - 需要高性能的场景
2. **批量图像处理** - 大量图像处理任务
3. **嵌入式应用** - 资源受限的环境
4. **科学计算** - 需要精确控制的场景

## 📝 注意事项

1. **资源管理** - Mat对象需要正确释放（使用using语句）
2. **线程安全** - ImageProcessorManager是线程安全的
3. **参数验证** - 自定义处理器应验证参数有效性
4. **错误处理** - 捕获并处理OpenCV异常

## 🔄 与现有架构的兼容性

本方案与现有的 `ITool` 接口并存：

- **ITool** - 使用 ImageData，适合通用场景
- **IImageProcessor** - 使用 Mat，适合高性能场景

开发者可以根据具体需求选择合适的接口。

## 📚 更多示例

查看 `Samples/ImageProcessorExamples.cs` 获取更多使用示例。

## 📖 总结

这个方案提供了最佳的性能和灵活性，完全基于原生图像类型，避免了所有不必要的转换开销，同时保持了接口的抽象性，不会暴露底层技术选型。
