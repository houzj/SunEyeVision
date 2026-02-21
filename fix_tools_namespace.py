#!/usr/bin/env python3
"""
批量修复Tools项目中的命名空间引用
"""

import os
import re

TOOLS_DIR = r"d:\MyWork\SunEyeVision\SunEyeVision\SunEyeVision.Tools\Tools"

# 要替换的模式
REPLACEMENTS = [
    # using语句替换
    (r'using SunEyeVision\.Models;', 'using SunEyeVision.Core.Models;'),
    (r'using SunEyeVision\.Interfaces;', 'using SunEyeVision.Core.Interfaces;'),
    
    # 完整命名空间替换
    (r'SunEyeVision\.Interfaces\.IImageProcessor', 'SunEyeVision.Core.Interfaces.IImageProcessor'),
    (r'SunEyeVision\.Models\.AlgorithmParameters', 'SunEyeVision.Core.Models.AlgorithmParameters'),
    (r'new SunEyeVision\.Models\.AlgorithmParameters', 'new SunEyeVision.Core.Models.AlgorithmParameters'),
]

def fix_file(filepath):
    """修复单个文件"""
    with open(filepath, 'r', encoding='utf-8') as f:
        content = f.read()
    
    original_content = content
    
    for pattern, replacement in REPLACEMENTS:
        content = re.sub(pattern, replacement, content)
    
    if content != original_content:
        with open(filepath, 'w', encoding='utf-8') as f:
            f.write(content)
        print(f"Fixed: {filepath}")
        return True
    return False

def main():
    """主函数"""
    fixed_count = 0
    
    for root, dirs, files in os.walk(TOOLS_DIR):
        for filename in files:
            if filename.endswith('.cs'):
                filepath = os.path.join(root, filename)
                if fix_file(filepath):
                    fixed_count += 1
    
    print(f"\nTotal files fixed: {fixed_count}")

if __name__ == "__main__":
    main()
