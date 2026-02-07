#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
修复SunEyeVision.Tools项目中缺失的using语句
"""

import re
from pathlib import Path

# 项目根目录
ROOT_DIR = Path("d:/MyWork/SunEyeVision/SunEyeVision")

# 需要替换的命名空间映射
NAMESPACE_REPLACEMENTS = {
    "SunEyeVision.PluginSystem.Tools": "SunEyeVision.Tools",
    "SunEyeVision.PluginSystem.Core": "SunEyeVision.PluginSystem.Base",
    "SunEyeVision.PluginSystem.Core.Interfaces": "SunEyeVision.PluginSystem.Base.Interfaces",
    "SunEyeVision.PluginSystem.Core.Models": "SunEyeVision.PluginSystem.Base.Models",
    "SunEyeVision.PluginSystem.Core.Services": "SunEyeVision.PluginSystem.Base.Services",
    "SunEyeVision.PluginSystem.Parameters": "SunEyeVision.PluginSystem.Base.Base",
}

# 需要添加的using语句
REQUIRED_USINGS = [
    "using SunEyeVision.PluginSystem.Base.Interfaces;",
    "using SunEyeVision.PluginSystem.Base.Models;",
    "using SunEyeVision.PluginSystem.Base.Base;",
]

def fix_file(file_path):
    """修复单个文件"""
    try:
        # 读取文件，尝试多种编码
        content = None
        for encoding in ['utf-8', 'gbk', 'latin-1']:
            try:
                content = file_path.read_text(encoding=encoding)
                break
            except (UnicodeDecodeError, UnicodeError):
                continue

        if content is None:
            return False

        original = content

        # 替换命名空间
        for old_ns, new_ns in NAMESPACE_REPLACEMENTS.items():
            # 替换 namespace 声明
            content = re.sub(
                r'namespace ' + re.escape(old_ns),
                f'namespace {new_ns}',
                content
            )
            # 替换 using 语句
            content = re.sub(
                r'using ' + re.escape(old_ns) + r';',
                f'using {new_ns};',
                content
            )

        # 移除对WPF相关类的引用（如果不需要UI）
        content = re.sub(
            r'using SunEyeVision\.PluginSystem\.Infrastructure.*?;',
            '',
            content,
            flags=re.DOTALL
        )

        # 添加必要的using语句
        lines = content.split('\n')
        insert_idx = -1

        # 找到最后一个using语句的位置
        for i, line in enumerate(lines):
            if line.strip().startswith('using '):
                insert_idx = i + 1
            elif 'namespace' in line:
                break

        if insert_idx > 0:
            # 检查哪些using缺失
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

def fix_tools_project():
    """修复Tools项目中所有.cs文件"""
    print("[FIX] 开始修复Tools项目的using语句...")

    tools_dir = ROOT_DIR / "SunEyeVision.Tools"
    fixed_count = 0
    checked_count = 0

    for cs_file in tools_dir.rglob("*.cs"):
        checked_count += 1
        if fix_file(cs_file):
            fixed_count += 1
            print(f"  [FIXED] {cs_file.relative_to(ROOT_DIR)}")

    print(f"\n[DONE] 检查了 {checked_count} 个文件，修复了 {fixed_count} 个文件")

if __name__ == "__main__":
    fix_tools_project()
