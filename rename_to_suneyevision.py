#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Sun Eye Vision 重命名脚本
将 VisionMaster 重命名为 SunEyeVision
"""

import os
import re
import shutil
from pathlib import Path

# 定义映射关系
TEXT_MAPPINGS = {
    "VisionMaster": "SunEyeVision",
    "VisionMaster - 机器视觉平台": "Sun Eye Vision - 机器视觉平台",
    "VisionMaster - 文档中心": "Sun Eye Vision - 文档中心",
    "VisionMaster 机器视觉平台": "Sun Eye Vision 机器视觉平台",
    "VisionMaster 开发计划": "Sun Eye Vision 开发计划",
    "VisionMaster 机器视觉软件": "Sun Eye Vision 机器视觉软件",
    "VisionMaster 框架": "Sun Eye Vision 框架",
    "VisionMaster Python Service": "Sun Eye Vision Python Service",
    "关于 VisionMaster": "关于 Sun Eye Vision",
    "VisionMaster Team": "Sun Eye Vision Team",
}

FOLDER_MAPPINGS = {
    "VisionMaster.Algorithms": "SunEyeVision.Algorithms",
    "VisionMaster.Core": "SunEyeVision.Core",
    "VisionMaster.Demo": "SunEyeVision.Demo",
    "VisionMaster.DeviceDriver": "SunEyeVision.DeviceDriver",
    "VisionMaster.PluginSystem": "SunEyeVision.PluginSystem",
    "VisionMaster.Test": "SunEyeVision.Test",
    "VisionMaster.UI": "SunEyeVision.UI",
    "VisionMaster.Workflow": "SunEyeVision.Workflow",
}

FILE_MAPPINGS = {
    "VisionMaster.sln": "SunEyeVision.sln",
}

CSPROJ_MAPPINGS = {
    "SunEyeVision.Algorithms/VisionMaster.Algorithms.csproj": "SunEyeVision.Algorithms/SunEyeVision.Algorithms.csproj",
    "SunEyeVision.Core/VisionMaster.Core.csproj": "SunEyeVision.Core/SunEyeVision.Core.csproj",
    "SunEyeVision.Demo/VisionMaster.Demo.csproj": "SunEyeVision.Demo/SunEyeVision.Demo.csproj",
    "SunEyeVision.DeviceDriver/VisionMaster.DeviceDriver.csproj": "SunEyeVision.DeviceDriver/SunEyeVision.DeviceDriver.csproj",
    "SunEyeVision.PluginSystem/VisionMaster.PluginSystem.csproj": "SunEyeVision.PluginSystem/SunEyeVision.PluginSystem.csproj",
    "SunEyeVision.Test/VisionMaster.Test.csproj": "SunEyeVision.Test/SunEyeVision.Test.csproj",
    "SunEyeVision.UI/VisionMaster.UI.csproj": "SunEyeVision.UI/SunEyeVision.UI.csproj",
    "SunEyeVision.Workflow/VisionMaster.Workflow.csproj": "SunEyeVision.Workflow/SunEyeVision.Workflow.csproj",
}

EXCLUDE_DIRS = {'obj', 'bin', '.vs', '.git'}

def should_exclude(path):
    """检查是否应该排除该路径"""
    parts = Path(path).parts
    return any(exclude in parts for exclude in EXCLUDE_DIRS)

def update_file_content(filepath):
    """更新文件内容"""
    try:
        with open(filepath, 'r', encoding='utf-8') as f:
            content = f.read()
        
        original_content = content
        for old_text, new_text in TEXT_MAPPINGS.items():
            content = content.replace(old_text, new_text)
        
        if content != original_content:
            with open(filepath, 'w', encoding='utf-8', newline='') as f:
                f.write(content)
            return True
    except Exception as e:
        print(f"  错误处理文件 {filepath}: {e}")
    return False

def update_files_by_extension(extension, step_name):
    """根据扩展名更新文件"""
    print(f"\n{step_name}:")
    count = 0
    for root, dirs, files in os.walk('.'):
        if should_exclude(root):
            continue
        
        for file in files:
            if file.endswith(extension):
                filepath = os.path.join(root, file)
                if update_file_content(filepath):
                    print(f"  更新: {filepath}")
                    count += 1
    print(f"  共更新 {count} 个文件")

def rename_folders():
    """重命名文件夹"""
    print("\n步骤7: 重命名文件夹...")
    count = 0
    for old_name, new_name in FOLDER_MAPPINGS.items():
        if os.path.exists(old_name):
            shutil.move(old_name, new_name)
            print(f"  重命名: {old_name} -> {new_name}")
            count += 1
    print(f"  共重命名 {count} 个文件夹")

def rename_files():
    """重命名文件"""
    print("\n步骤8: 重命名文件...")
    count = 0
    for old_name, new_name in FILE_MAPPINGS.items():
        if os.path.exists(old_name):
            shutil.move(old_name, new_name)
            print(f"  重命名: {old_name} -> {new_name}")
            count += 1
    print(f"  共重命名 {count} 个文件")

def rename_csproj_files():
    """重命名 .csproj 文件"""
    print("\n步骤9: 重命名 .csproj 文件...")
    count = 0
    for old_path, new_path in CSPROJ_MAPPINGS.items():
        if os.path.exists(old_path):
            shutil.move(old_path, new_path)
            print(f"  重命名: {old_path} -> {new_path}")
            count += 1
    print(f"  共重命名 {count} 个文件")

def main():
    print("=" * 60)
    print("Sun Eye Vision 重命名脚本")
    print("=" * 60)
    print("开始重命名项目为 Sun Eye Vision...\n")
    
    # 步骤1-6: 更新文件内容
    update_files_by_extension('.cs', '步骤1: 更新 .cs 文件')
    update_files_by_extension('.xaml', '步骤2: 更新 .xaml 文件')
    update_files_by_extension('.csproj', '步骤3: 更新 .csproj 文件')
    update_files_by_extension('.sln', '步骤4: 更新解决方案文件')
    update_files_by_extension('.md', '步骤5: 更新文档文件')
    update_files_by_extension('.py', '步骤6: 更新 Python 文件')
    
    # 步骤7-9: 重命名文件夹和文件
    rename_folders()
    rename_files()
    rename_csproj_files()
    
    print("\n" + "=" * 60)
    print("✅ 重命名完成!")
    print("=" * 60)
    print("请清理并重新构建项目。")

if __name__ == '__main__':
    main()
