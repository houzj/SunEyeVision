#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
SunEyeVision.PluginSystem 重构脚本
按照方案一（合并式重构）重新组织目录结构
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

def step_1_create_directory_structure():
    """步骤1：创建新的目录结构"""
    print_step(1, "创建新的目录结构")
    
    dirs = [
        "Core/Interfaces",
        "Core/Models",
        "Core/Services",
        "Infrastructure/Base",
        "Infrastructure/Commands",
        "Infrastructure/Decorators",
        "Infrastructure/UI/Converters",
        "Infrastructure/UI/Windows",
        "Parameters",
    ]
    
    for dir_path in dirs:
        full_path = BASE_PATH / dir_path
        full_path.mkdir(parents=True, exist_ok=True)
        print(f"✓ 创建目录: {dir_path}")

def step_2_move_core_interfaces():
    """步骤2：移动核心接口"""
    print_step(2, "移动核心接口到 Core/Interfaces/")
    
    interfaces = [
        "IPluginManager.cs",
        "IToolPlugin.cs",
        "IVisionPlugin.cs",
    ]
    
    for interface_file in interfaces:
        src = BASE_PATH / interface_file
        dst = BASE_PATH / "Core/Interfaces" / interface_file
        if src.exists():
            shutil.move(str(src), str(dst))
            print(f"✓ 移动: {interface_file} -> Core/Interfaces/{interface_file}")
        else:
            print(f"✗ 文件不存在: {interface_file}")

def step_3_move_core_models():
    """步骤3：移动核心模型"""
    print_step(3, "移动核心模型到 Core/Models/")
    
    models = [
        "ParameterMetadata.cs",
        "ToolMetadata.cs",
    ]
    
    for model_file in models:
        src = BASE_PATH / model_file
        dst = BASE_PATH / "Core/Models" / model_file
        if src.exists():
            shutil.move(str(src), str(dst))
            print(f"✓ 移动: {model_file} -> Core/Models/{model_file}")
        else:
            print(f"✗ 文件不存在: {model_file}")

def step_4_move_core_services():
    """步骤4：移动核心服务"""
    print_step(4, "移动核心服务到 Core/Services/")
    
    services = [
        "ToolRegistry.cs",
        "PluginLoader.cs",
        "ToolInitialization.cs",
    ]
    
    for service_file in services:
        src = BASE_PATH / service_file
        dst = BASE_PATH / "Core/Services" / service_file
        if src.exists():
            shutil.move(str(src), str(dst))
            print(f"✓ 移动: {service_file} -> Core/Services/{service_file}")
        else:
            print(f"✗ 文件不存在: {service_file}")

def step_5_split_plugin_manager():
    """步骤5：拆分 IPluginManager.cs 为接口和实现"""
    print_step(5, "拆分 PluginManager 为接口和实现类")
    
    src = BASE_PATH / "Core/Interfaces/IPluginManager.cs"
    if not src.exists():
        print(f"✗ 文件不存在: IPluginManager.cs")
        return
    
    # 读取原文件
    content = src.read_text(encoding='utf-8')
    
    # 提取接口部分（第1-49行）
    lines = content.split('\n')
    interface_content = '\n'.join(lines[:49])
    
    # 提取实现类部分（第50-128行）
    impl_lines = lines[49:]
    
    # 修改实现类的命名空间
    impl_content = '\n'.join(impl_lines)
    impl_content = impl_content.replace(
        "namespace SunEyeVision.PluginSystem",
        "namespace SunEyeVision.PluginSystem.Core.Services"
    )
    
    # 更新接口文件的命名空间
    interface_content = interface_content.replace(
        "namespace SunEyeVision.PluginSystem",
        "namespace SunEyeVision.PluginSystem.Core.Interfaces"
    )
    
    # 写回接口文件
    src.write_text(interface_content, encoding='utf-8')
    print(f"✓ 更新接口命名空间: Core/Interfaces/IPluginManager.cs")
    
    # 创建实现类文件
    impl_file = BASE_PATH / "Core/Services/PluginManager.cs"
    impl_file.write_text(impl_content, encoding='utf-8')
    print(f"✓ 创建实现类: Core/Services/PluginManager.cs")

def step_6_update_namespaces_in_core():
    """步骤6：更新核心文件的命名空间"""
    print_step(6, "更新核心文件的命名空间")
    
    # 接口文件命名空间更新
    interface_files = [
        ("Core/Interfaces/IPluginManager.cs", "SunEyeVision.PluginSystem.Core.Interfaces"),
        ("Core/Interfaces/IToolPlugin.cs", "SunEyeVision.PluginSystem.Core.Interfaces"),
        ("Core/Interfaces/IVisionPlugin.cs", "SunEyeVision.PluginSystem.Core.Interfaces"),
    ]
    
    for file_path, new_namespace in interface_files:
        full_path = BASE_PATH / file_path
        if full_path.exists():
            content = full_path.read_text(encoding='utf-8')
            content = content.replace(
                "namespace SunEyeVision.PluginSystem",
                f"namespace {new_namespace}"
            )
            full_path.write_text(content, encoding='utf-8')
            print(f"✓ 更新命名空间: {file_path} -> {new_namespace}")
    
    # 模型文件命名空间更新
    model_files = [
        ("Core/Models/ParameterMetadata.cs", "SunEyeVision.PluginSystem.Core.Models"),
        ("Core/Models/ToolMetadata.cs", "SunEyeVision.PluginSystem.Core.Models"),
    ]
    
    for file_path, new_namespace in model_files:
        full_path = BASE_PATH / file_path
        if full_path.exists():
            content = full_path.read_text(encoding='utf-8')
            content = content.replace(
                "namespace SunEyeVision.PluginSystem",
                f"namespace {new_namespace}"
            )
            full_path.write_text(content, encoding='utf-8')
            print(f"✓ 更新命名空间: {file_path} -> {new_namespace}")
    
    # 服务文件命名空间更新
    service_files = [
        ("Core/Services/ToolRegistry.cs", "SunEyeVision.PluginSystem.Core.Services"),
        ("Core/Services/PluginLoader.cs", "SunEyeVision.PluginSystem.Core.Services"),
        ("Core/Services/ToolInitialization.cs", "SunEyeVision.PluginSystem.Core.Services"),
    ]
    
    for file_path, new_namespace in service_files:
        full_path = BASE_PATH / file_path
        if full_path.exists():
            content = full_path.read_text(encoding='utf-8')
            content = content.replace(
                "namespace SunEyeVision.PluginSystem",
                f"namespace {new_namespace}"
            )
            full_path.write_text(content, encoding='utf-8')
            print(f"✓ 更新命名空间: {file_path} -> {new_namespace}")

def step_7_move_base_classes():
    """步骤7：移动基础设施基类"""
    print_step(7, "移动基础设施基类到 Infrastructure/Base/")
    
    base_files = [
        "AutoToolDebugViewModelBase.cs",
        "ToolDebugViewModelBase.cs",
        "ObservableObject.cs",
    ]
    
    for base_file in base_files:
        src = BASE_PATH / "Base" / base_file
        dst = BASE_PATH / "Infrastructure/Base" / base_file
        if src.exists():
            shutil.move(str(src), str(dst))
            print(f"✓ 移动: Base/{base_file} -> Infrastructure/Base/{base_file}")
        else:
            print(f"✗ 文件不存在: Base/{base_file}")

def step_8_update_base_namespaces():
    """步骤8：更新基础设施基类命名空间"""
    print_step(8, "更新基础设施基类命名空间")
    
    base_files = [
        "Infrastructure/Base/AutoToolDebugViewModelBase.cs",
        "Infrastructure/Base/ToolDebugViewModelBase.cs",
        "Infrastructure/Base/ObservableObject.cs",
    ]
    
    for file_path in base_files:
        full_path = BASE_PATH / file_path
        if full_path.exists():
            content = full_path.read_text(encoding='utf-8')
            content = content.replace(
                "namespace SunEyeVision.PluginSystem.Base",
                "namespace SunEyeVision.PluginSystem.Infrastructure.Base"
            )
            full_path.write_text(content, encoding='utf-8')
            print(f"✓ 更新命名空间: {file_path}")

def main():
    """主函数"""
    print("\n" + "="*60)
    print("SunEyeVision.PluginSystem 重构脚本")
    print("方案一：合并式重构")
    print("="*60)
    
    try:
        # 执行步骤
        step_1_create_directory_structure()
        step_2_move_core_interfaces()
        step_3_move_core_models()
        step_4_move_core_services()
        step_5_split_plugin_manager()
        step_6_update_namespaces_in_core()
        step_7_move_base_classes()
        step_8_update_base_namespaces()
        
        print("\n" + "="*60)
        print("✓ 重构阶段一完成！")
        print("="*60)
        print("\n下一步：")
        print("1. 验证编译是否通过")
        print("2. 运行阶段二（移动工具实现）")
        
    except Exception as e:
        print(f"\n✗ 错误: {e}")
        import traceback
        traceback.print_exc()

if __name__ == "__main__":
    main()
