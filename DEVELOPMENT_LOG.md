# 太阳眼视觉 - 开发日志

## 2026-01-21 (星期三)

### 主要完成工作

#### 1. 实现分层混合模式调试界面
- **目标**: 解决不同工具调试界面和参数适配的问题
- **实现内容**:
  - 创建 `IDebugControlProvider` 接口,用于自定义调试控件
  - 实现 `ToolRegistry` 静态注册表,集中管理工具插件
  - 实现 `ToolInitializer` 自动加载工具插件
  - 构建 `ParameterControlFactory` 参数控件工厂,支持13种参数类型映射
  - 更新 `DebugWindow` 支持混合模式(自定义控件 → 自动生成 → 降级处理)

#### 2. 解决命名空间冲突问题
- **问题**: `System.Windows` 和 `System.Windows.Forms` 存在类型冲突
- **解决**:
  - 移除项目配置中的 `UseWindowsForms` 引用
  - 移除所有命名空间别名
  - 将返回类型从 `Control` 改为 `FrameworkElement` 以解决类型层次结构问题

#### 3. 为示例工具添加元数据
- 创建了8个带完整元数据的工具插件:
  1. `ImageCaptureTool` (image_capture) - 图像采集
  2. `TemplateMatchingTool` (template_matching) - 模板匹配
  3. `GaussianBlurTool` (gaussian_blur) - 高斯模糊
  4. `OCRTool` (ocr_recognition) - OCR识别
  5. `ThresholdTool` (threshold) - 图像阈值化
  6. `EdgeDetectionTool` (edge_detection) - 边缘检测
  7. `ROICropTool` (roi_crop) - ROI裁剪
  8. `ColorConvertTool` (color_convert) - 颜色转换

#### 4. 修复流程编辑区工具双击无法打开调试界面的问题
- **问题1**: 示例节点使用的工具ID与注册的工具ID不匹配
  - 修复前: `"ImageAcquisition"`, `"GrayScale"`, `"EdgeDetection"`
  - 修复后: `"image_capture"`, `"gaussian_blur"`, `"edge_detection"`

- **问题2**: 从工具箱拖拽新工具到流程编辑区后无法打开调试界面
  - 原因: `ToolItem.AlgorithmType` 存储的是算法类名(如 `"ThresholdAlgorithm"`),而非工具ID(如 `"threshold"`)
  - 解决方案:
    - 在 `ToolItem` 模型中添加 `ToolId` 属性
    - 在 `ToolboxViewModel.LoadToolsFromRegistry()` 中正确设置 `toolItem.ToolId = tool.Id`
    - 在 `MainWindow.xaml.cs.WorkflowCanvas_Drop()` 中使用 `tool.ToolId` 而非 `tool.AlgorithmType`

#### 5. 创建测试文档
- 生成 `DEBUG_INTERFACE_TEST_GUIDE.md`,包含所有工具的调试界面测试指南

### 技术要点

#### 参数控件工厂支持的参数类型
1. `Int` - 整数 (TextBox + Slider)
2. `Double` - 双精度浮点数 (TextBox + Slider)
3. `String` - 字符串 (TextBox)
4. `Bool` - 布尔值 (CheckBox)
5. `Enum` - 枚举 (ComboBox)
6. `Color` - 颜色 (ColorPicker - 占位)
7. `Image` - 图像 (Image控件 - 占位)
8. `FilePath` - 文件路径 (TextBox + Button)
9. `Point` - 点 (X, Y两个输入)
10. `Size` - 尺寸 (Width, Height两个输入)
11. `Rect` - 矩形 (X, Y, Width, Height四个输入)
12. `List<T>` - 列表 (ListBox - 占位)

#### 混合模式架构
1. **优先级1**: 工具提供自定义调试控件 (`IDebugControlProvider.CreateDebugControl()`)
2. **优先级2**: 根据 `ToolMetadata` 自动生成参数控件 (`ParameterControlFactory`)
3. **优先级3**: 降级到基础信息显示

### 修改的文件

| 文件路径 | 修改内容 |
|---------|---------|
| `SunEyeVision.UI/IDebugControlProvider.cs` | 从 PluginSystem 移至 UI,定义自定义控件接口 |
| `SunEyeVision.PluginSystem/ToolRegistry.cs` | 静态工具注册表实现 |
| `SunEyeVision.PluginSystem/ToolInitialization.cs` | 工具插件自动加载器 |
| `SunEyeVision.UI/ParameterControlFactory.cs` | 参数控件工厂,支持13种参数类型 |
| `SunEyeVision.UI/DebugWindow.xaml.cs` | 支持混合模式,新增 `InitializeDebugInterface()` 等方法 |
| `SunEyeVision.UI/ViewModels/ToolboxViewModel.cs` | 使用静态 `ToolRegistry`,注册8个示例工具 |
| `SunEyeVision.UI/ViewModels/MainWindowViewModel.cs` | 修复示例节点工具ID,实现双击打开调试窗口 |
| `SunEyeVision.UI/MainWindow.xaml.cs` | 修复拖拽工具时使用正确的ToolId |
| `SunEyeVision.UI/Models/ToolItem.cs` | 新增 `ToolId` 属性 |
| `SunEyeVision.PluginSystem/SampleTools/*.cs` | 创建8个工具插件实现 |

### 构建结果
- **错误数**: 0
- **警告数**: 79 (均为可空引用类型警告,不影响运行)

### 遗留问题
无

### 下一步计划
1. 实现更多参数类型的控件(如 ColorPicker, ImageUploader)
2. 完善工具执行逻辑,支持参数传递
3. 实现工作流执行引擎
4. 添加插件热加载功能
5. 实现参数验证逻辑

---

## 2026-01-22 (星期四)

### 主要完成工作

#### 1. 修复分隔器拖拽尺寸功能
- **问题**: 图像显示区域和属性显示区域之间无法通过拖拽改变尺寸
- **原因**: 右侧面板内的 GridSplitter 缺少必要的布局属性
  - 缺少 `Width="Auto"` 属性
  - 缺少 `HorizontalAlignment="Stretch"` 属性
- **解决方案**: 在 `MainWindow.xaml` 的第 351 行修复 GridSplitter 配置
  ```xml
  <GridSplitter Grid.Row="1" Height="8" Width="Auto" MinHeight="8"
               ResizeDirection="Rows"
               Background="#DDDDDD"
               ShowsPreview="False"
               ResizeBehavior="PreviousAndNext"
               HorizontalAlignment="Stretch"/>
  ```

#### 2. 分隔器布局架构回顾
当前项目中的分隔器分为两类:
1. **自定义分隔器 (`SplitterWithToggle`)** - 用于主区域间:
   - 左侧工具箱与中间工作区之间 (Grid.Column="1")
   - 中间工作区与右侧面板之间 (Grid.Column="3")
   - 特点: 带有折叠/展开按钮,支持快速切换面板可见性

2. **标准分隔器 (`GridSplitter`)** - 用于区域内部分割:
   - 右侧面板内: 图像显示区域与属性面板之间 (Grid.Row="1")
   - 特点: 纯粹的尺寸调整功能,无额外UI元素

### 技术要点

#### GridSplitter 关键配置属性
- `ResizeDirection`: `"Columns"` (垂直分隔) 或 `"Rows"` (水平分隔)
- `ResizeBehavior`: `"PreviousAndNext"` (同时调整前后两个元素)
- `ShowsPreview`: `"False"` (实时调整) 或 `"True"` (预览模式)
- `HorizontalAlignment`: `"Stretch"` (必须设置为拉伸才能响应全宽度拖拽)
- `Width/Height`: 值为 "Auto" 时根据方向自适应

#### 布局容器关系
- 主窗口: `DockPanel` → `Grid` (MainContentGrid)
- 主内容区 Grid:
  - Column 0: 工具箱
  - Column 1: 垂直分隔器 (自定义)
  - Column 2: 工作流画布
  - Column 3: 垂直分隔器 (自定义)
  - Column 4: 右侧面板
- 右侧面板 Grid:
  - Row 0: 图像显示区域
  - Row 1: 水平分隔器 (标准)
  - Row 2: 属性面板

### 修改的文件

| 文件路径 | 修改内容 |
|---------|---------|
| `SunEyeVision.UI/MainWindow.xaml` | 修复右侧面板内 GridSplitter 的布局属性配置 |

### 构建结果
- **错误数**: 0
- **警告数**: 79 (可空引用类型警告,不影响运行)

### 遗留问题
无

### 下一步计划
1. 继续完善用户界面交互细节
2. 测试所有分隔器的拖拽功能
3. 实现更多参数类型的控件(如 ColorPicker, ImageUploader)
4. 完善工具执行逻辑,支持参数传递
5. 实现工作流执行引擎
6. 添加插件热加载功能
7. 实现参数验证逻辑

---

**注意**: 本日志记录的是截至2026-01-22的开发工作。后续开发工作将在需要时更新到本日志中。
