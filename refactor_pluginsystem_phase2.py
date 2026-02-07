#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
SunEyeVision.PluginSystem 重构脚本 - 阶段二
合并 SampleTools 和 Tools
"""

import os
import sys
import shutil
from pathlib import Path

# 设置控制台输出编码为UTF-8
if sys.platform == "win32":
    import io
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')
    sys.stderr = io.TextIOWrapper(sys.stderr.buffer, encoding='utf-8')

# 基础路径
BASE_PATH = Path(r"d:\MyWork\SunEyeVision\SunEyeVision\SunEyeVision.PluginSystem")

def print_step(step_num, step_name):
    """打印步骤信息"""
    print(f"\n{'='*60}")
    print(f"步骤 {step_num}: {step_name}")
    print(f"{'='*60}\n")

def step_1_merge_tool_implementation():
    """步骤1：合并工具实现类"""
    print_step(1, "合并工具实现类到 Tools 子目录")
    
    tools = [
        "ColorConvertTool",
        "EdgeDetectionTool",
        "GaussianBlurTool",
        "ImageCaptureTool",
        "ImageSaveTool",
        "OCRTool",
        "ROICropTool",
        "TemplateMatchingTool",
        "ThresholdTool"
    ]
    
    for tool_name in tools:
        # 源文件
        src = BASE_PATH / "SampleTools" / f"{tool_name}.cs"
        # 目标文件
        dst = BASE_PATH / "Tools" / tool_name / f"{tool_name}.cs"
        
        if src.exists():
            if dst.exists():
                print(f"! 跳过: {tool_name}.cs (目标文件已存在)")
            else:
                shutil.move(str(src), str(dst))
                print(f"+ 移动: {tool_name}.cs -> Tools/{tool_name}/{tool_name}.cs")
        else:
            print(f"✗ 文件不存在: {tool_name}.cs")

def step_2_move_viewmodels():
    """步骤2：移动 ViewModel 到根目录"""
    print_step(2, "移动 ViewModel 到 Tools 子目录根级别")
    
    tools = [
        "ColorConvertTool",
        "EdgeDetectionTool",
        "GaussianBlurTool",
        "ImageCaptureTool",
        "ImageSaveTool",
        "OCRTool",
        "ROICropTool",
        "TemplateMatchingTool",
        "ThresholdTool"
    ]
    
    for tool_name in tools:
        # 源文件
        src = BASE_PATH / "Tools" / tool_name / "ViewModels" / f"{tool_name}ViewModel.cs"
        # 目标文件
        dst = BASE_PATH / "Tools" / tool_name / f"{tool_name}ViewModel.cs"
        
        if src.exists():
            shutil.move(str(src), str(dst))
            print(f"+ 移动: {tool_name}ViewModel.cs -> Tools/{tool_name}/{tool_name}ViewModel.cs")
        else:
            print(f"✗ 文件不存在: {tool_name}ViewModel.cs")

def step_3_move_dtos():
    """步骤3：移动 DTOs 到根目录"""
    print_step(3, "移动 DTOs 到 Tools 子目录根级别")
    
    # 检查每个工具是否有 DTOs 目录
    tools_dir = BASE_PATH / "Tools"
    if tools_dir.exists():
        for tool_dir in tools_dir.iterdir():
            if tool_dir.is_dir():
                dtos_dir = tool_dir / "DTOs"
                if dtos_dir.exists():
                    # 移动所有 DTO 文件到工具目录根级别
                    for dto_file in dtos_dir.glob("*.cs"):
                        dst = tool_dir / dto_file.name
                        shutil.move(str(dto_file), str(dst))
                        print(f"+ 移动: {dto_file.name} -> Tools/{tool_dir.name}/{dto_file.name}")
                    
                    # 删除空的 DTOs 目录
                    try:
                        dtos_dir.rmdir()
                        print(f"- 删除空目录: Tools/{tool_dir.name}/DTOs")
                    except:
                        pass

def step_4_update_tool_namespaces():
    """步骤4：更新工具类的命名空间"""
    print_step(4, "更新工具类的命名空间")
    
    tools = [
        "ColorConvertTool",
        "EdgeDetectionTool",
        "GaussianBlurTool",
        "ImageCaptureTool",
        "ImageSaveTool",
        "OCRTool",
        "ROICropTool",
        "TemplateMatchingTool",
        "ThresholdTool"
    ]
    
    for tool_name in tools:
        tool_file = BASE_PATH / "Tools" / tool_name / f"{tool_name}.cs"
        if tool_file.exists():
            try:
                content = tool_file.read_text(encoding='utf-8')
                # 更新命名空间
                content = content.replace(
                    "namespace SunEyeVision.PluginSystem.SampleTools",
                    f"namespace SunEyeVision.PluginSystem.Tools.{tool_name}"
                )
                tool_file.write_text(content, encoding='utf-8')
                print(f"+ 更新命名空间: {tool_name}.cs")
            except Exception as e:
                print(f"✗ 错: {tool_name}.cs - {e}")

def step_5_update_viewmodel_namespaces():
    """步骤5：更新 ViewModel 的命名空间"""
    print_step(5, "更新 ViewModel 的命名空间")
    
    tools = [
        "ColorConvertTool",
        "EdgeDetectionTool",
        "GaussianBlurTool",
        "ImageCaptureTool",
        "ImageSaveTool",
        "OCRTool",
        "ROICropTool",
        "TemplateMatchingTool",
        "ThresholdTool"
    ]
    
    for tool_name in tools:
        vm_file = BASE_PATH / "Tools" / tool_name / f"{tool_name}ViewModel.cs"
        if vm_file.exists():
            try:
                content = vm_file.read_text(encoding='utf-8')
                # 更新命名空间
                content = content.replace(
                    "namespace SunEyeVision.PluginSystem.Tools.ViewModels",
                    f"namespace SunEyeVision.PluginSystem.Tools.{tool_name}"
                )
                vm_file.write_text(content, encoding='utf-8')
                print(f"+ 更新命名空间: {tool_name}ViewModel.cs")
            except Exception as e:
                print(f"✗ 错: {tool_name}ViewModel.cs - {e}")

def step_6_cleanup_directories():
    """步骤6：清理空目录"""
    print_step(6, "清理空目录")
    
    # 删除 SampleTools 目录
    sampletools_dir = BASE_PATH / "SampleTools"
    if sampletools_dir.exists() and not any(sampletools_dir.iterdir()):
        sampletools_dir.rmdir()
        print(f"- 删除空目录: SampleTools")
    elif sampletools_dir.exists():
        print(f"! SampleTools 非空，跳过删除")
    
    # 删除 ViewModels 目录
    tools_dir = BASE_PATH / "Tools"
    if tools_dir.exists():
        for tool_dir in tools_dir.iterdir():
            if tool_dir.is_dir():
                viewmodels_dir = tool_dir / "ViewModels"
                if viewmodels_dir.exists():
                    try:
                        viewmodels_dir.rmdir()
                        print(f"- 删除空目录: Tools/{tool_dir.name}/ViewModels")
                    except:
                        pass

def main():
    """主函数"""
    print("\n" + "="*60)
    print("SunEyeVision.PluginSystem 重构脚本 - 阶段二")
    print("合并 SampleTools 和 Tools")
    print("="*60)
    
    try:
        # 执行步骤
        step_1_merge_tool_implementation()
        step_2_move_viewmodels()
        step_3_move_dtos()
        step_4_update_tool_namespaces()
        step_5_update_viewmodel_namespaces()
        step_6_cleanup_directories()
        
        print("\n" + "="*60)
        print("✓ 阶段二完成！")
        print("="*60)
        print("\n下一步：")
        print("1. 验证编译是否通过")
        print("2. 更新引用这些类型的外部代码")
        
    except Exception as e:
        print(f"\n✗ 错误: {e}")
        import traceback
        traceback.print_exc()

if __name__ == "__main__":
    main()
