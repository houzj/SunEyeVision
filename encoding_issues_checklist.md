# SunEyeVision 编码问题修复清单

## 统计摘要
- **问题文件总数**: 129 个
- **总问题行数**: 2167 行
- **生成时间**: 2026-02-25

## 问题模式分析

编码问题主要表现为中文字符被截断，常见模式：
1. `??` - 中文字符末尾被截断（如 "调试" 显示为 "调??"）
2. 乱码字符 - 完整的中文变成乱码

## 高优先级文件（问题行数 > 50）

| 序号 | 文件路径 | 问题行数 | 状态 |
|------|----------|----------|------|
| 1 | src\UI\Views\Controls\Canvas\WorkflowCanvasControl.xaml.cs | 412 | [ ] |
| 2 | src\UI\Services\Thumbnail\PriorityThumbnailLoader.cs | 254 | [ ] |
| 3 | src\UI\Views\Windows\MainWindow.xaml.cs | 248 | [ ] |
| 4 | src\UI\Services\PathCalculators\OrthogonalPathCalculator.cs | 160 | [ ] |
| 5 | src\Workflow\WorkflowContext.cs | 91 | [ ] |

## 中优先级文件（问题行数 20-50）

| 序号 | 文件路径 | 问题行数 | 状态 |
|------|----------|----------|------|
| 6 | src\UI\Services\Thumbnail\SmartThumbnailLoader.cs | 69 | [ ] |
| 7 | src\UI\Services\Thumbnail\Decoders\ImageSharpDecoder.cs | 51 | [ ] |
| 8 | src\UI\ViewModels\ToolboxViewModel.cs | 34 | [ ] |
| 9 | src\UI\Views\Controls\Canvas\NativeDiagramControl.xaml.cs | 34 | [ ] |
| 10 | src\UI\Services\Canvas\CanvasConfig.cs | 31 | [ ] |
| 11 | src\UI\Services\Thumbnail\Decoders\AdvancedGpuDecoder.cs | 26 | [ ] |
| 12 | src\UI\Services\PathCalculators\AIStudioPathCalculator.cs | 23 | [ ] |
| 13 | src\UI\Controls\Rendering\ExifThumbnailExtractor.cs | 21 | [ ] |
| 14 | src\UI\Adapters\DiagramAdapter.cs | 21 | [ ] |
| 15 | src\UI\Services\PathCalculators\BezierPathCalculator.cs | 20 | [ ] |
| 16 | src\UI\App.xaml.cs | 20 | [ ] |
| 17 | src\UI\Services\Interaction\BoxSelectionHandler.cs | 19 | [ ] |
| 18 | src\UI\Controls\Rendering\DirectXGpuThumbnailLoader.cs | 18 | [ ] |
| 19 | src\UI\Services\Connection\ConnectionPathCache.cs | 18 | [ ] |
| 20 | src\UI\Services\Performance\EnhancedBatchUpdateManager.cs | 18 | [ ] |
| 21 | src\UI\Services\Path\IPathCalculator.cs | 18 | [ ] |
| 22 | src\UI\Services\Interaction\ConnectionDragHandler.cs | 17 | [ ] |
| 23 | src\UI\Services\Interaction\ConnectionCreator.cs | 16 | [ ] |
| 24 | src\Core\Services\PluginManager.cs | 16 | [ ] |
| 25 | src\UI\Services\Interaction\SelectionHandler.cs | 16 | [ ] |

## 低优先级文件（问题行数 < 20）

| 序号 | 文件路径 | 问题行数 | 状态 |
|------|----------|----------|------|
| 26 | src\UI\Services\Interaction\DragDropHandler.cs | 15 | [ ] |
| 27 | src\UI\Services\Thumbnail\Caching\WeakReferenceCache.cs | 14 | [ ] |
| 28 | src\UI\Services\Connection\ConnectionService.cs | 14 | [ ] |
| 29 | src\UI\Converters\Path\SmartPathMultiConverter.cs | 13 | [ ] |
| 30 | src\UI\Services\Toolbox\ToolboxPopupStateManager.cs | 13 | [ ] |
| 31 | src\UI\Models\LayoutConfig.cs | 13 | [ ] |
| 32 | src\UI\Adapters\LibraryValidator.cs | 13 | [ ] |
| 33 | src\Core\Interfaces\IConfigManager.cs | 13 | [ ] |
| 34 | src\UI\Services\Interaction\PortInteractionHandler2.cs | 12 | [ ] |
| 35 | src\Workflow\WorkflowEngineFactory.cs | 12 | [ ] |
| 36 | src\UI\Services\Thumbnail\IThumbnailDecoder.cs | 12 | [ ] |
| 37 | src\UI\Services\Connection\ConnectionBatchUpdateManager.cs | 12 | [ ] |
| 38 | src\UI\Adapters\NodeDisplayAdapterConfig.cs | 12 | [ ] |
| 39 | src\UI\Converters\Path\SmartPathConverter.cs | 10 | [ ] |
| 40 | src\UI\Services\Interaction\NodeDragHandler.cs | 10 | [ ] |

## 修复策略

### 策略1: 自动修复常见模式
使用脚本批量替换常见的编码错误模式：
- `调??` → `调试`
- `信??` → `信息`
- `接??` → `接口`
- `结??` → `结果`
- `问??` → `问题`
- `配??` → `配置`

### 策略2: 手动修复复杂情况
对于无法自动识别的上下文，需要手动查看并修复

### 策略3: 重新编写注释
对于严重损坏的注释，考虑根据代码逻辑重新编写

## 修复进度

- [ ] 阶段1: 修复高优先级文件（5个文件，1065行）
- [ ] 阶段2: 修复中优先级文件（20个文件，约400行）
- [ ] 阶段3: 修复低优先级文件（104个文件，约700行）
- [ ] 阶段4: 验证修复结果

## 注意事项

1. 修复前备份原文件
2. 每修复一个文件后进行编译测试
3. 保持代码功能不变，仅修复注释
4. 对于不确定的内容，查阅相关文档或保留原文
