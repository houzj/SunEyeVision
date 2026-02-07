#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
修复SunEyeVision.PluginSystem项目中缺失的using语句
"""

import os
import re
from pathlib import Path

# 项目根目录
ROOT_DIR = Path("d:/MyWork/SunEyeVision/SunEyeVision")

# 需要添加using的规则
USING_RULES = [
    {
        "pattern": r"(using SunEyeVision\.Interfaces;|using SunEyeVision\.Models;)",
        "usings": [
            "using SunEyeVision.PluginSystem.Base.Interfaces;",
            "using SunEyeVision.PluginSystem.Base.Models;",
            "using SunEyeVision.PluginSystem.Base.Base;"
        ]
    },
    {
        "pattern": r"namespace SunEyeVision\.PluginSystem",
        "usings": [
            "using SunEyeVision.PluginSystem.Base.Interfaces;",
            "using SunEyeVision.PluginSystem.Base.Models;",
            "using SunEyeVision.PluginSystem.Base.Base;"
        ]
    }
]

def fix_file_usings(file_path):
    """修复文件中的using语句"""
    try:
        with open(file_path, 'r', encoding='utf-8') as f:
            content = f.read()
        
        # 检查是否需要添加using
        needs_fix = False
        for rule in USING_RULES:
            if re.search(rule["pattern"], content):
                # 检查是否已经有这些using
                for using_stmt in rule["usings"]:
                    if using_stmt not in content:
                        needs_fix = True
                        break
        
        if not needs_fix:
            return False
        
        # 找到最后一个using语句的位置
        lines = content.split('\n')
        insert_idx = -1
        
        for i, line in enumerate(lines):
            if line.strip().startswith('using '):
                insert_idx = i + 1
            elif 'namespace' in line:
                break
        
        if insert_idx > 0:
            # 添加缺失的using语句
            for rule in USING_RULES:
                if re.search(rule["pattern"], content):
                    for using_stmt in rule["usings"]:
                        if using_stmt not in content:
                            lines.insert(insert_idx, using_stmt)
                            insert_idx += 1
            
            # 写回文件
            new_content = '\n'.join(lines)
            with open(file_path, 'w', encoding='utf-8') as f:
                f.write(new_content)
            
            print(f"  [FIX] {file_path.relative_to(ROOT_DIR)}")
            return True
        
        return False
    except Exception as e:
        print(f"  [ERROR] {file_path}: {e}")
        return False

def fix_all_files():
    """修复所有文件"""
    print("[FIX] 开始修复using语句...")
    
    plugin_system_dir = ROOT_DIR / "SunEyeVision.PluginSystem"
    fixed_count = 0
    
    for cs_file in plugin_system_dir.rglob("*.cs"):
        if fix_file_usings(cs_file):
            fixed_count += 1
    
    print(f"\n[DONE] 共修复 {fixed_count} 个文件")

if __name__ == "__main__":
    fix_all_files()
