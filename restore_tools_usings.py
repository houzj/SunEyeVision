#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
恢复Tools项目中对SunEyeVision.PluginSystem的using语句
"""

import re
from pathlib import Path

# 项目根目录
ROOT_DIR = Path("d:/MyWork/SunEyeVision/SunEyeVision")

# 需要恢复的using语句
REQUIRED_USINGS = [
    "using SunEyeVision.PluginSystem.Infrastructure.Base;",
    "using SunEyeVision.PluginSystem.Infrastructure.UI.Windows;",
]

def restore_usings_in_file(file_path):
    """恢复文件中的using语句"""
    try:
        # 读取文件
        content = file_path.read_text(encoding='utf-8')
        original = content

        # 检查是否需要添加using
        needs_fix = False
        for using_stmt in REQUIRED_USINGS:
            if "ViewModel" in str(file_path) or "Window" in str(file_path):
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
            for using_stmt in REQUIRED_USINGS:
                if using_stmt not in content:
                    lines.insert(insert_idx, using_stmt)
                    insert_idx += 1

            content = '\n'.join(lines)

            if content != original:
                file_path.write_text(content, encoding='utf-8')
                return True

        return False
    except Exception as e:
        print(f"  [ERROR] {file_path}: {e}")
        return False

def restore_tools_usings():
    """恢复Tools项目中所有.cs文件"""
    print("[RESTORE] 开始恢复Tools项目的using语句...")

    tools_dir = ROOT_DIR / "SunEyeVision.Tools"
    restored_count = 0

    for cs_file in tools_dir.rglob("*.cs"):
        if restore_usings_in_file(cs_file):
            restored_count += 1
            print(f"  [RESTORED] {cs_file.relative_to(ROOT_DIR)}")

    print(f"\n[DONE] 共恢复 {restored_count} 个文件")

if __name__ == "__main__":
    restore_tools_usings()
