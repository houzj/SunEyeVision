#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
修复 ViewModel 文件的命名空间和 using 语句
"""

import os
import sys
import re
from pathlib import Path

# 设置控制台输出编码为UTF-8
if sys.platform == "win32":
    import io
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')
    sys.stderr = io.TextIOWrapper(sys.stderr.buffer, encoding='utf-8')

# 基础路径
BASE_PATH = Path(r"d:\MyWork\SunEyeVision\SunEyeVision\SunEyeVision.PluginSystem")

def fix_viewmodel_file(file_path):
    """修复 ViewModel 文件"""
    try:
        content = file_path.read_text(encoding='utf-8')
        original_content = content
        
        # 获取工具名称
        tool_name = file_path.parent.name
        old_namespace = f"SunEyeVision.PluginSystem.Tools.{tool_name}.ViewModels"
        new_namespace = f"SunEyeVision.PluginSystem.Tools.{tool_name}"
        
        # 替换命名空间
        content = content.replace(old_namespace, new_namespace)
        
        # 移除 DTOs 引用
        content = re.sub(r"using SunEyeVision\.PluginSystem\.Tools\.\w+\.DTOs;", "", content)
        
        # 移除 SunEyeVision.UI.MVVM 引用
        content = content.replace("using SunEyeVision.UI.MVVM;", "")
        
        # 添加必要的 using 语句（如果不存在）
        required_usings = [
            "using SunEyeVision.PluginSystem.Core.Interfaces;",
            "using SunEyeVision.PluginSystem.Core.Models;",
        ]
        
        lines = content.split('\n')
        new_lines = []
        using_lines = set()
        other_lines = []
        
        # 提取现有的 using 语句
        for line in lines:
            if line.strip().startswith("using "):
                using_lines.add(line.strip())
            else:
                other_lines.append(line)
        
        # 添加必要的 using 语句
        for required_using in required_usings:
            if required_using not in using_lines:
                new_lines.append(required_using)
        
        # 保留其他 using 语句
        for using_line in sorted(using_lines):
            if using_line not in required_usings:
                new_lines.append(using_line)
        
        # 添加其他内容
        new_lines.extend(other_lines)
        
        content = '\n'.join(new_lines)
        
        # 只在有变化时写入文件
        if content != original_content:
            file_path.write_text(content, encoding='utf-8')
            return True
        
        return False
    except Exception as e:
        print(f"✗ 错误: {file_path} - {e}")
        return False

def main():
    """主函数"""
    print("\n" + "="*60)
    print("修复 ViewModel 文件的命名空间和 using 语句")
    print("="*60)
    
    updated_count = 0
    processed_count = 0
    
    # 遍历所有 ViewModel 文件
    for vm_file in BASE_PATH.glob("Tools/*/ViewModels/*.cs"):
        # 跳过 obj 目录
        if "obj" in str(vm_file):
            continue
        
        processed_count += 1
        if fix_viewmodel_file(vm_file):
            updated_count += 1
            print(f"+ 更新: {vm_file.relative_to(BASE_PATH)}")
    
    # 同时检查工具目录下的 ViewModel 文件（已经移动的）
    for vm_file in BASE_PATH.glob("Tools/*/*ViewModel.cs"):
        # 跳过 obj 目录
        if "obj" in str(vm_file):
            continue
        
        processed_count += 1
        if fix_viewmodel_file(vm_file):
            updated_count += 1
            print(f"+ 更新: {vm_file.relative_to(BASE_PATH)}")
    
    print("\n" + "="*60)
    print(f"处理了 {processed_count} 个文件，更新了 {updated_count} 个文件")
    print("="*60)

if __name__ == "__main__":
    main()
