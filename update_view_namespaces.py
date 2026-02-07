#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
批量更新 View 文件的命名空间引用
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

def update_view_file(file_path):
    """更新 View 文件的命名空间引用"""
    try:
        content = file_path.read_text(encoding='utf-8')
        original_content = content
        
        # 替换 using 语句
        content = content.replace(
            "using SunEyeVision.PluginSystem.Base.Windows;",
            "using SunEyeVision.PluginSystem.Infrastructure.UI.Windows;"
        )
        
        # 替换 using ViewModels (不再存在)
        content = re.sub(
            r"using SunEyeVision\.PluginSystem\.Tools\.(\w+)\.ViewModels;",
            "",
            content
        )
        
        # 在文件开头添加必要的 using 语句（如果不存在）
        required_usings = [
            "using SunEyeVision.PluginSystem.Core.Interfaces;",
            "using SunEyeVision.PluginSystem.Core.Models;",
            "using SunEyeVision.PluginSystem.Infrastructure.UI.Windows;",
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
    print("批量更新 View 文件的命名空间引用")
    print("="*60)
    
    updated_count = 0
    processed_count = 0
    
    # 遍历所有 Views 目录下的 .xaml.cs 文件
    for view_file in BASE_PATH.rglob("Views/*Window.xaml.cs"):
        # 跳过 obj 目录
        if "obj" in str(view_file):
            continue
        
        processed_count += 1
        if update_view_file(view_file):
            updated_count += 1
            print(f"+ 更新: {view_file.relative_to(BASE_PATH)}")
    
    print("\n" + "="*60)
    print(f"处理了 {processed_count} 个文件，更新了 {updated_count} 个文件")
    print("="*60)

if __name__ == "__main__":
    main()
