#!/usr/bin/env python3
"""
UI目录结构优化迁移脚本
自动执行目录重组、命名空间更新、引用修复
"""

import os
import re
import shutil
import json
from pathlib import Path
from typing import Dict, List, Tuple, Set

# 项目根目录
PROJECT_ROOT = Path(r"D:\MyWork\SunEyeVision\SunEyeVision\src\UI")

# 旧命名空间 -> 新命名空间映射
NAMESPACE_MAPPING = {
    # 根目录文件重新分配
    "SunEyeVision.UI.CanvasConfig": "SunEyeVision.UI.Services.Canvas",
    "SunEyeVision.UI.CanvasHelper": "SunEyeVision.UI.Services.Canvas",
    "SunEyeVision.UI.ParameterControlFactory": "SunEyeVision.UI.Factories",
    "SunEyeVision.UI.IDebugControlProvider": "SunEyeVision.UI.Diagnostics",
    
    # PluginDebug -> Diagnostics
    "SunEyeVision.UI.PluginDebug": "SunEyeVision.UI.Diagnostics",
    
    # WorkflowCanvasService -> Services.Interaction
    "SunEyeVision.UI.WorkflowCanvasService": "SunEyeVision.UI.Services.Interaction",
    
    # Engines -> Services.Canvas
    "SunEyeVision.UI.Engines": "SunEyeVision.UI.Services.Canvas",
    
    # Interfaces -> 按职责分配
    "SunEyeVision.UI.Interfaces": "SunEyeVision.UI.Services.Canvas",  # 大部分是画布接口
    
    # Rendering -> Services.Rendering
    "SunEyeVision.UI.Rendering": "SunEyeVision.UI.Services.Rendering",
    
    # Panel -> Infrastructure
    "SunEyeVision.UI.Panel": "SunEyeVision.UI.Infrastructure",
    
    # MVVM -> Factories
    "SunEyeVision.UI.MVVM": "SunEyeVision.UI.Factories",
    
    # Controls.Helpers -> Services.Interaction
    "SunEyeVision.UI.Controls.Helpers": "SunEyeVision.UI.Services.Interaction",
    
    # Controls.Rendering -> Services.Thumbnail
    "SunEyeVision.UI.Controls.Rendering": "SunEyeVision.UI.Services.Thumbnail",
    
    # Models.WorkflowNode -> Models.WorkflowNodeModel (避免命名冲突)
    "SunEyeVision.UI.Models.WorkflowNode": "SunEyeVision.UI.Models.WorkflowNodeModel",
}

# 文件迁移映射：相对路径 -> 新相对路径
FILE_MIGRATION = {
    # ==================== 根目录文件 ====================
    # 窗口文件 -> Views/Windows/
    "AboutWindow.xaml": "Views/Windows/AboutWindow.xaml",
    "AboutWindow.xaml.cs": "Views/Windows/AboutWindow.xaml.cs",
    "DebugWindow.xaml": "Views/Windows/DebugWindow.xaml",
    "DebugWindow.xaml.cs": "Views/Windows/DebugWindow.xaml.cs",
    "HelpWindow.xaml": "Views/Windows/HelpWindow.xaml",
    "HelpWindow.xaml.cs": "Views/Windows/HelpWindow.xaml.cs",
    "MainWindow.xaml": "Views/Windows/MainWindow.xaml",
    "MainWindow.xaml.cs": "Views/Windows/MainWindow.xaml.cs",
    "MainWindow_Simple.xaml": "Views/Windows/MainWindow_Simple.xaml",
    
    # 应用程序入口保持在根目录
    # App.xaml, App.xaml.cs, AssemblyInfo.cs 保持不变
    
    # 核心服务文件 -> Services/Canvas/
    "CanvasConfig.cs": "Services/Canvas/CanvasConfigHelper.cs",  # 重命名避免与Services下同名文件冲突
    "CanvasHelper.cs": "Services/Canvas/CanvasHelper.cs",
    
    # 工厂类 -> Factories/
    "ParameterControlFactory.cs": "Factories/ParameterControlFactory.cs",
    
    # 接口 -> Diagnostics/
    "IDebugControlProvider.cs": "Diagnostics/IDebugControlProvider.cs",
    
    # ==================== Controls/ ====================
    # 画布控件 -> Views/Controls/Canvas/
    "Controls/WorkflowCanvasControl.xaml": "Views/Controls/Canvas/WorkflowCanvasControl.xaml",
    "Controls/WorkflowCanvasControl.xaml.cs": "Views/Controls/Canvas/WorkflowCanvasControl.xaml.cs",
    "Controls/NativeDiagramControl.xaml": "Views/Controls/Canvas/NativeDiagramControl.xaml",
    "Controls/NativeDiagramControl.xaml.cs": "Views/Controls/Canvas/NativeDiagramControl.xaml.cs",
    "Controls/VirtualizedCanvas.cs": "Views/Controls/Canvas/VirtualizedCanvas.cs",
    "Controls/CanvasTemplateSelector.cs": "Views/Controls/Canvas/CanvasTemplateSelector.cs",
    "Controls/CanvasType.cs": "Views/Controls/Canvas/CanvasType.cs",
    
    # 工具箱控件 -> Views/Controls/Toolbox/
    "Controls/ToolboxControl.xaml": "Views/Controls/Toolbox/ToolboxControl.xaml",
    "Controls/ToolboxControl.xaml.cs": "Views/Controls/Toolbox/ToolboxControl.xaml.cs",
    "Controls/ToolDebugTemplates.xaml": "Views/Controls/Toolbox/ToolDebugTemplates.xaml",
    
    # 面板控件 -> Views/Controls/Panels/
    "Controls/PropertyPanelControl.xaml": "Views/Controls/Panels/PropertyPanelControl.xaml",
    "Controls/PropertyPanelControl.xaml.cs": "Views/Controls/Panels/PropertyPanelControl.xaml.cs",
    "Controls/ImagePreviewControl.xaml": "Views/Controls/Panels/ImagePreviewControl.xaml",
    "Controls/ImagePreviewControl.xaml.cs": "Views/Controls/Panels/ImagePreviewControl.xaml.cs",
    "Controls/ImageDisplayControl.xaml": "Views/Controls/Panels/ImageDisplayControl.xaml",
    "Controls/ImageDisplayControl.xaml.cs": "Views/Controls/Panels/ImageDisplayControl.xaml.cs",
    
    # 通用控件 -> Views/Controls/Common/
    "Controls/LoadingWindow.xaml": "Views/Controls/Common/LoadingWindow.xaml",
    "Controls/LoadingWindow.xaml.cs": "Views/Controls/Common/LoadingWindow.xaml.cs",
    "Controls/SelectionBox.xaml": "Views/Controls/Common/SelectionBox.xaml",
    "Controls/SelectionBox.xaml.cs": "Views/Controls/Common/SelectionBox.xaml.cs",
    "Controls/SplitterWithToggle.cs": "Views/Controls/Common/SplitterWithToggle.cs",
    "Controls/SplitterWithToggle.xaml": "Views/Controls/Common/SplitterWithToggle.xaml",
    
    # ==================== Controls/Helpers/ -> Services/Interaction/ ====================
    "Controls/Helpers/PortPositionService.cs": "Services/Interaction/PortPositionService.cs",
    "Controls/Helpers/WorkflowConnectionCreator.cs": "Services/Interaction/ConnectionCreator.cs",
    "Controls/Helpers/WorkflowConnectionManager.cs": "Services/Interaction/ConnectionManager.cs",
    "Controls/Helpers/WorkflowDragDropHandler.cs": "Services/Interaction/DragDropHandler.cs",
    "Controls/Helpers/WorkflowNodeInteractionHandler.cs": "Services/Interaction/NodeInteractionHandler.cs",
    "Controls/Helpers/WorkflowPathCalculator.cs": "Services/Path/WorkflowPathCalculator.cs",
    "Controls/Helpers/WorkflowPortHighlighter.cs": "Services/Interaction/PortHighlighter.cs",
    "Controls/Helpers/WorkflowPortInteractionHandler.cs": "Services/Interaction/PortInteractionHandler.cs",
    "Controls/Helpers/WorkflowSelectionHandler.cs": "Services/Interaction/SelectionHandler.cs",
    "Controls/Helpers/WorkflowVisualHelper.cs": "Services/Rendering/VisualHelper.cs",
    
    # ==================== Controls/Rendering/ -> Services/Thumbnail/ ====================
    "Controls/Rendering/SmartThumbnailLoader.cs": "Services/Thumbnail/SmartThumbnailLoader.cs",
    "Controls/Rendering/ThumbnailCacheManager.cs": "Services/Thumbnail/ThumbnailCacheManager.cs",
    "Controls/Rendering/PriorityThumbnailLoader.cs": "Services/Thumbnail/PriorityThumbnailLoader.cs",
    "Controls/Rendering/IThumbnailDecoder.cs": "Services/Thumbnail/IThumbnailDecoder.cs",
    "Controls/Rendering/WicGpuDecoder.cs": "Services/Thumbnail/Decoders/WicGpuDecoder.cs",
    "Controls/Rendering/ImageSharpDecoder.cs": "Services/Thumbnail/Decoders/ImageSharpDecoder.cs",
    "Controls/Rendering/AdvancedGpuDecoder.cs": "Services/Thumbnail/Decoders/AdvancedGpuDecoder.cs",
    "Controls/Rendering/GPUCache.cs": "Services/Thumbnail/Caching/GPUCache.cs",
    "Controls/Rendering/WeakReferenceCache.cs": "Services/Thumbnail/Caching/WeakReferenceCache.cs",
    
    # ==================== PluginDebug/ -> Diagnostics/ ====================
    "PluginDebug/DebugControlManager.cs": "Diagnostics/DebugControlManager.cs",
    "PluginDebug/IDebugControlProvider.cs": "Diagnostics/IDebugControlProvider.cs",
    "PluginDebug/SharedDebugControl.xaml": "Diagnostics/SharedDebugControl.xaml",
    "PluginDebug/SharedDebugControl.xaml.cs": "Diagnostics/SharedDebugControl.xaml.cs",
    
    # ==================== WorkflowCanvasService/ -> Services/Interaction/ ====================
    "WorkflowCanvasService/NodeDragHandler.cs": "Services/Interaction/NodeDragHandler.cs",
    "WorkflowCanvasService/ConnectionDragHandler.cs": "Services/Interaction/ConnectionDragHandler.cs",
    "WorkflowCanvasService/PortInteractionHandler.cs": "Services/Interaction/PortInteractionHandler2.cs",  # 避免与上面的重名
    "WorkflowCanvasService/BoxSelectionHandler.cs": "Services/Interaction/BoxSelectionHandler.cs",
    "WorkflowCanvasService/NodeSequenceManager.cs": "Services/Interaction/NodeSequenceManager.cs",
    
    # ==================== Rendering/ -> Services/Rendering/ ====================
    "Rendering/CanvasRenderer.cs": "Services/Rendering/CanvasRenderer.cs",
    "Rendering/GeometryOptimizer.cs": "Services/Rendering/GeometryOptimizer.cs",
    
    # ==================== Engines/ -> Services/Canvas/ ====================
    "Engines/NativeDiagramEngine.cs": "Services/Canvas/Engines/NativeDiagramEngine.cs",
    "Engines/TestCanvasEngine.cs": "Services/Canvas/Engines/TestCanvasEngine.cs",
    "Engines/WorkflowCanvasEngine.cs": "Services/Canvas/Engines/WorkflowCanvasEngine.cs",
    
    # ==================== Interfaces/ -> Services/Canvas/ ====================
    "Interfaces/ICanvasEngine.cs": "Services/Canvas/ICanvasEngine.cs",
    "Interfaces/INodeSequenceManager.cs": "Services/Interaction/INodeSequenceManager.cs",
    "Interfaces/IPathCalculator.cs": "Services/Path/IPathCalculator.cs",
    "Interfaces/IWorkflowNodeFactory.cs": "Services/Workflow/IWorkflowNodeFactory.cs",
    
    # ==================== MVVM/ -> Factories/ ====================
    "MVVM/ParameterControlFactory.cs": "Factories/MvvmParameterControlFactory.cs",  # 重命名避免冲突
    "MVVM/ToolDebugWindowFactory.cs": "Factories/ToolDebugWindowFactory.cs",
    
    # ==================== Panel/ -> Infrastructure/ ====================
    "Panel/PanelExtension.cs": "Infrastructure/PanelExtension.cs",
    "Panel/PanelManager.cs": "Infrastructure/PanelManager.cs",
    
    # ==================== Models/ -> Models/ (重命名冲突文件) ====================
    "Models/WorkflowNode.cs": "Models/WorkflowNodeModel.cs",  # 重命名避免与Workflow项目冲突
    
    # ==================== Converters/ -> Converters/按功能分组 ====================
    "Converters/BoolToVisibilityConverter.cs": "Converters/Visibility/BoolToVisibilityConverter.cs",
    "Converters/InverseBoolToVisibilityConverter.cs": "Converters/Visibility/InverseBoolToVisibilityConverter.cs",
    "Converters/NullToVisibilityConverter.cs": "Converters/Visibility/NullToVisibilityConverter.cs",
    "Converters/StringToVisibilityConverter.cs": "Converters/Visibility/StringToVisibilityConverter.cs",
    "Converters/IntToVisibilityConverter.cs": "Converters/Visibility/IntToVisibilityConverter.cs",
    "Converters/CanvasTypeVisibilityConverter.cs": "Converters/Visibility/CanvasTypeVisibilityConverter.cs",
    
    "Converters/RunModeConverter.cs": "Converters/Workflow/RunModeConverter.cs",
    "Converters/RunModeButtonConverter.cs": "Converters/Workflow/RunModeButtonConverter.cs",
    "Converters/RunModeConverters.cs": "Converters/Workflow/RunModeConverters.cs",
    "Converters/WorkflowStateToColorConverter.cs": "Converters/Workflow/WorkflowStateToColorConverter.cs",
    
    "Converters/SmartPathConverter.cs": "Converters/Path/SmartPathConverter.cs",
    "Converters/SmartPathMultiConverter.cs": "Converters/Path/SmartPathMultiConverter.cs",
    "Converters/PointOffsetConverter.cs": "Converters/Path/PointOffsetConverter.cs",
    
    "Converters/NodeDisplayConverter.cs": "Converters/Node/NodeDisplayConverter.cs",
    "Converters/ImageAreaHeightConverter.cs": "Converters/Node/ImageAreaHeightConverter.cs",
    
    "Converters/BoolToActiveConverter.cs": "Converters/UI/BoolToActiveConverter.cs",
    "Converters/BoolToSelectedBackgroundConverter.cs": "Converters/UI/BoolToSelectedBackgroundConverter.cs",
    "Converters/BoolToSelectedBorderConverter.cs": "Converters/UI/BoolToSelectedBorderConverter.cs",
    "Converters/BoolToSelectedBorderThicknessConverter.cs": "Converters/UI/BoolToSelectedBorderThicknessConverter.cs",
    "Converters/BoolToRunningBackgroundConverter.cs": "Converters/UI/BoolToRunningBackgroundConverter.cs",
    "Converters/BoolToRunningBorderConverter.cs": "Converters/UI/BoolToRunningBorderConverter.cs",
    "Converters/BoolToRunningTextConverter.cs": "Converters/UI/BoolToRunningTextConverter.cs",
    "Converters/BoolToContinuousTextConverter.cs": "Converters/UI/BoolToContinuousTextConverter.cs",
    "Converters/ExpandIconConverter.cs": "Converters/UI/ExpandIconConverter.cs",
    "Converters/ContinuousRunIconTriangleInLoopConverter.cs": "Converters/UI/ContinuousRunIconTriangleInLoopConverter.cs",
    "Converters/ValueConverters.cs": "Converters/UI/ValueConverters.cs",
    
    # ==================== Services/ -> Services/按职责细分 ====================
    # Canvas
    "Services/CanvasConfig.cs": "Services/Canvas/CanvasConfig.cs",
    "Services/CanvasEngineManager.cs": "Services/Canvas/CanvasEngineManager.cs",
    "Services/CanvasStateManager.cs": "Services/Canvas/CanvasStateManager.cs",
    
    # Workflow
    "Services/WorkflowExecutionManager.cs": "Services/Workflow/WorkflowExecutionManager.cs",
    "Services/WorkflowNodeFactory.cs": "Services/Workflow/WorkflowNodeFactory.cs",
    
    # Connection
    "Services/ConnectionService.cs": "Services/Connection/ConnectionService.cs",
    "Services/ConnectionPathService.cs": "Services/Connection/ConnectionPathService.cs",
    "Services/ConnectionPathCache.cs": "Services/Connection/ConnectionPathCache.cs",
    "Services/ConnectionBatchUpdateManager.cs": "Services/Connection/ConnectionBatchUpdateManager.cs",
    
    # Node
    "Services/NodeIndexManager.cs": "Services/Node/NodeIndexManager.cs",
    "Services/NodeSelectionService.cs": "Services/Node/NodeSelectionService.cs",
    "Services/PortService.cs": "Services/Node/PortService.cs",
    "Services/NodeSequenceManager.cs": "Services/Interaction/NodeSequenceManagerService.cs",  # 重命名避免冲突
    
    # Performance
    "Services/PerformanceMonitor.cs": "Services/Performance/PerformanceMonitor.cs",
    "Services/PerformanceBenchmark.cs": "Services/Performance/PerformanceBenchmark.cs",
    "Services/BatchUpdateManager.cs": "Services/Performance/BatchUpdateManager.cs",
    "Services/EnhancedBatchUpdateManager.cs": "Services/Performance/EnhancedBatchUpdateManager.cs",
    
    # Rendering
    "Services/SpatialIndex.cs": "Services/Rendering/SpatialIndex.cs",
    
    # Infrastructure (保持)
    "Services/ServiceLocator.cs": "Infrastructure/ServiceLocator.cs",
    "Services/DefaultInputProvider.cs": "Infrastructure/DefaultInputProvider.cs",
    "Services/IInputProvider.cs": "Infrastructure/IInputProvider.cs",
    "Services/UIEventPublisher.cs": "Infrastructure/UIEventPublisher.cs",
    "Services/ConsoleLogger.cs": "Diagnostics/ConsoleLogger.cs",
    
    # ==================== Resources/ -> Views/Resources/ ====================
    "Resources/AppResources.xaml": "Views/Resources/AppResources.xaml",
    
    # ==================== 其他保持不变 ====================
    # Adapters/ 保持
    # Commands/ 保持
    # Events/ 保持
    # Extensions/ 保持
    # ViewModels/ 保持
}

def get_new_namespace(file_path: str) -> str:
    """根据文件路径计算新的命名空间"""
    path = Path(file_path)
    
    # 检查是否有直接的命名空间映射
    for old_ns, new_ns in NAMESPACE_MAPPING.items():
        if old_ns in file_path.replace("/", "\\").replace("\\", "."):
            # 构建完整的命名空间
            parts = path.parts
            if len(parts) > 1:
                # 找到命名空间基础部分
                return new_ns
    
    # 默认：基于目录结构生成命名空间
    dir_path = path.parent
    ns_parts = ["SunEyeVision", "UI"]
    for part in dir_path.parts:
        if part not in [".", "..", "src", "UI"]:
            ns_parts.append(part)
    
    return ".".join(ns_parts)

def update_namespace_in_file(file_path: Path, new_namespace: str) -> bool:
    """更新文件中的命名空间"""
    try:
        content = file_path.read_text(encoding='utf-8')
        original = content
        
        # 匹配命名空间声明
        pattern = r'namespace\s+[\w.]+'
        new_ns_decl = f'namespace {new_namespace}'
        content = re.sub(pattern, new_ns_decl, content)
        
        if content != original:
            file_path.write_text(content, encoding='utf-8')
            return True
        return False
    except Exception as e:
        print(f"  错误: 更新命名空间失败 - {e}")
        return False

def update_using_statements(file_path: Path, old_ns: str, new_ns: str) -> bool:
    """更新文件中的using语句"""
    try:
        content = file_path.read_text(encoding='utf-8')
        original = content
        
        # 替换using语句
        pattern = rf'using\s+{re.escape(old_ns)}(;|\s)'
        replacement = f'using {new_ns}\\1'
        content = re.sub(pattern, replacement, content)
        
        if content != original:
            file_path.write_text(content, encoding='utf-8')
            return True
        return False
    except Exception as e:
        print(f"  错误: 更新using语句失败 - {e}")
        return False

def update_xaml_references(file_path: Path, old_ns: str, new_ns: str) -> bool:
    """更新XAML文件中的命名空间引用"""
    try:
        content = file_path.read_text(encoding='utf-8')
        original = content
        
        # 替换clr-namespace引用
        pattern = rf'clr-namespace:{re.escape(old_ns)}'
        replacement = f'clr-namespace:{new_ns}'
        content = re.sub(pattern, replacement, content)
        
        if content != original:
            file_path.write_text(content, encoding='utf-8')
            return True
        return False
    except Exception as e:
        print(f"  错误: 更新XAML引用失败 - {e}")
        return False

def create_directory_structure():
    """创建新的目录结构"""
    dirs_to_create = [
        "Views/Windows",
        "Views/Controls/Canvas",
        "Views/Controls/Toolbox",
        "Views/Controls/Panels",
        "Views/Controls/Common",
        "Views/Resources",
        "Services/Canvas/Engines",
        "Services/Workflow",
        "Services/Connection",
        "Services/Node",
        "Services/Interaction",
        "Services/Rendering",
        "Services/Thumbnail/Decoders",
        "Services/Thumbnail/Caching",
        "Services/Path",
        "Services/Performance",
        "Converters/Visibility",
        "Converters/Workflow",
        "Converters/Path",
        "Converters/Node",
        "Converters/UI",
        "Factories",
        "Diagnostics",
        "Infrastructure",
    ]
    
    for dir_path in dirs_to_create:
        full_path = PROJECT_ROOT / dir_path
        full_path.mkdir(parents=True, exist_ok=True)
        print(f"创建目录: {dir_path}")

def migrate_files():
    """迁移文件"""
    migrated = []
    skipped = []
    errors = []
    
    for old_rel, new_rel in FILE_MIGRATION.items():
        old_path = PROJECT_ROOT / old_rel
        new_path = PROJECT_ROOT / new_rel
        
        if not old_path.exists():
            skipped.append(old_rel)
            continue
        
        # 确保目标目录存在
        new_path.parent.mkdir(parents=True, exist_ok=True)
        
        try:
            # 移动文件
            shutil.move(str(old_path), str(new_path))
            migrated.append((old_rel, new_rel))
            print(f"移动: {old_rel} -> {new_rel}")
        except Exception as e:
            errors.append((old_rel, str(e)))
            print(f"错误: {old_rel} - {e}")
    
    return migrated, skipped, errors

def update_all_namespaces():
    """更新所有文件的命名空间"""
    updated = []
    
    for cs_file in PROJECT_ROOT.rglob("*.cs"):
        if "obj" in str(cs_file) or "bin" in str(cs_file):
            continue
        
        # 计算新的命名空间
        rel_path = cs_file.relative_to(PROJECT_ROOT)
        new_ns = get_new_namespace(str(rel_path))
        
        if update_namespace_in_file(cs_file, new_ns):
            updated.append(str(rel_path))
            print(f"更新命名空间: {rel_path} -> {new_ns}")
    
    return updated

def update_all_references():
    """更新所有文件中的引用"""
    # 构建完整的命名空间映射（包括子命名空间）
    full_ns_mapping = {}
    for old_ns, new_ns in NAMESPACE_MAPPING.items():
        # 添加基础映射
        full_ns_mapping[old_ns] = new_ns
    
    updated_files = set()
    
    for file_path in PROJECT_ROOT.rglob("*.*"):
        if "obj" in str(file_path) or "bin" in str(file_path):
            continue
        
        ext = file_path.suffix.lower()
        if ext not in [".cs", ".xaml"]:
            continue
        
        for old_ns, new_ns in full_ns_mapping.items():
            if ext == ".cs":
                if update_using_statements(file_path, old_ns, new_ns):
                    updated_files.add(str(file_path.relative_to(PROJECT_ROOT)))
            elif ext == ".xaml":
                if update_xaml_references(file_path, old_ns, new_ns):
                    updated_files.add(str(file_path.relative_to(PROJECT_ROOT)))
    
    return updated_files

def clean_empty_directories():
    """清理空目录"""
    cleaned = []
    
    for root, dirs, files in os.walk(PROJECT_ROOT, topdown=False):
        root_path = Path(root)
        
        # 跳过obj和bin目录
        if "obj" in str(root_path) or "bin" in str(root_path):
            continue
        
        # 检查目录是否为空
        if not any(root_path.iterdir()):
            try:
                root_path.rmdir()
                cleaned.append(str(root_path.relative_to(PROJECT_ROOT)))
                print(f"删除空目录: {root_path.relative_to(PROJECT_ROOT)}")
            except Exception as e:
                print(f"无法删除目录 {root_path}: {e}")
    
    return cleaned

def main():
    print("=" * 60)
    print("SunEyeVision UI 目录结构优化迁移")
    print("=" * 60)
    print()
    
    # 1. 创建新目录结构
    print("[1/5] 创建新目录结构...")
    create_directory_structure()
    print()
    
    # 2. 迁移文件
    print("[2/5] 迁移文件...")
    migrated, skipped, errors = migrate_files()
    print(f"  已迁移: {len(migrated)} 个文件")
    print(f"  已跳过: {len(skipped)} 个文件")
    print(f"  错误: {len(errors)} 个")
    print()
    
    # 3. 更新命名空间
    print("[3/5] 更新命名空间...")
    ns_updated = update_all_namespaces()
    print(f"  已更新: {len(ns_updated)} 个文件")
    print()
    
    # 4. 更新引用
    print("[4/5] 更新引用...")
    ref_updated = update_all_references()
    print(f"  已更新: {len(ref_updated)} 个文件")
    print()
    
    # 5. 清理空目录
    print("[5/5] 清理空目录...")
    cleaned = clean_empty_directories()
    print(f"  已清理: {len(cleaned)} 个空目录")
    print()
    
    # 输出摘要
    print("=" * 60)
    print("迁移完成!")
    print(f"  文件迁移: {len(migrated)}")
    print(f"  命名空间更新: {len(ns_updated)}")
    print(f"  引用更新: {len(ref_updated)}")
    print(f"  空目录清理: {len(cleaned)}")
    print("=" * 60)
    
    # 保存迁移报告
    report = {
        "migrated": migrated,
        "skipped": skipped,
        "errors": errors,
        "namespace_updated": ns_updated,
        "reference_updated": list(ref_updated),
        "cleaned_directories": cleaned,
    }
    
    report_path = PROJECT_ROOT / "migration_report.json"
    with open(report_path, "w", encoding="utf-8") as f:
        json.dump(report, f, indent=2, ensure_ascii=False)
    print(f"\n迁移报告已保存到: {report_path}")

if __name__ == "__main__":
    main()
