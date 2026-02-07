#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
批量更新命名空间引用
"""

import os
import sys
from pathlib import Path

# 设置控制台输出编码为UTF-8
if sys.platform == "win32":
    import io
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')
    sys.stderr = io.TextIOWrapper(sys.stderr.buffer, encoding='utf-8')

# 基础路径
BASE_PATH = Path(r"d:\MyWork\SunEyeVision\SunEyeVision\SunEyeVision.PluginSystem")

# 命名空间映射
NAMESPACE_REPLACEMENTS = {
    "using SunEyeVision.PluginSystem.Base;": "using SunEyeVision.PluginSystem.Infrastructure.Base;",
    "using SunEyeVision.PluginSystem.Commands;": "using SunEyeVision.PluginSystem.Infrastructure.Commands;",
    "using SunEyeVision.PluginSystem.Decorators;": "using SunEyeVision.PluginSystem.Infrastructure.Decorators;",
}

# 类型映射
TYPE_REPLACEMENTS = {
    "SunEyeVision.PluginSystem.IVisionPlugin": "SunEyeVision.PluginSystem.Core.Interfaces.IVisionPlugin",
    "SunEyeVision.PluginSystem.IToolPlugin": "SunEyeVision.PluginSystem.Core.Interfaces.IToolPlugin",
    "SunEyeVision.PluginSystem.IPluginManager": "SunEyeVision.PluginSystem.Core.Interfaces.IPluginManager",
    "SunEyeVision.PluginSystem.ToolMetadata": "SunEyeVision.PluginSystem.Core.Models.ToolMetadata",
    "SunEyeVision.PluginSystem.ParameterMetadata": "SunEyeVision.PluginSystem.Core.Models.ParameterMetadata",
    "SunEyeVision.PluginSystem.ParameterType": "SunEyeVision.PluginSystem.Core.Models.ParameterType",
    "SunEyeVision.PluginSystem.ValidationResult": "SunEyeVision.PluginSystem.Core.Models.ValidationResult",
    "SunEyeVision.PluginSystem.ToolRegistry": "SunEyeVision.PluginSystem.Core.Services.ToolRegistry",
    "SunEyeVision.PluginSystem.PluginLoader": "SunEyeVision.PluginSystem.Core.Services.PluginLoader",
    "SunEyeVision.PluginSystem.ToolInitialization": "SunEyeVision.PluginSystem.Core.Services.ToolInitialization",
    "SunEyeVision.PluginSystem.PluginManager": "SunEyeVision.PluginSystem.Core.Services.PluginManager",
    "SunEyeVision.PluginSystem.ObservableObject": "SunEyeVision.PluginSystem.Infrastructure.Base.ObservableObject",
    "SunEyeVision.PluginSystem.ToolDebugViewModelBase": "SunEyeVision.PluginSystem.Infrastructure.Base.ToolDebugViewModelBase",
    "SunEyeVision.PluginSystem.AutoToolDebugViewModelBase": "SunEyeVision.PluginSystem.Infrastructure.Base.AutoToolDebugViewModelBase",
    "SunEyeVision.PluginSystem.ToolUIHelpers": "SunEyeVision.PluginSystem.Infrastructure.Base.ToolUIHelpers",
}

# 需要添加的新 using 语句
ADDITIONAL_USINGS = [
    "using SunEyeVision.PluginSystem.Core.Interfaces;",
    "using SunEyeVision.PluginSystem.Core.Models;",
    "using SunEyeVision.PluginSystem.Core.Services;",
    "using SunEyeVision.PluginSystem.Infrastructure.Base;",
    "using SunEyeVision.PluginSystem.Infrastructure.Commands;",
    "using SunEyeVision.PluginSystem.Infrastructure.Decorators;",
    "using SunEyeVision.PluginSystem.Infrastructure.UI.Converters;",
    "using SunEyeVision.PluginSystem.Infrastructure.UI.Windows;",
]

def update_file(file_path):
    """更新单个文件的命名空间引用"""
    try:
        content = file_path.read_text(encoding='utf-8')
        original_content = content
        
        # 替换命名空间
        for old_ns, new_ns in NAMESPACE_REPLACEMENTS.items():
            content = content.replace(old_ns, new_ns)
        
        # 替换类型引用
        for old_type, new_type in TYPE_REPLACEMENTS.items():
            content = content.replace(old_type, new_type)
        
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
    print("批量更新命名空间引用")
    print("="*60)
    
    updated_count = 0
    processed_count = 0
    
    # 遍历所有 .cs 文件
    for cs_file in BASE_PATH.rglob("*.cs"):
        # 跳过 obj 目录
        if "obj" in str(cs_file):
            continue
        
        processed_count += 1
        if update_file(cs_file):
            updated_count += 1
            print(f"+ 更新: {cs_file.relative_to(BASE_PATH)}")
    
    # 遍历所有 .xaml 文件
    for xaml_file in BASE_PATH.rglob("*.xaml"):
        # 跳过 obj 目录
        if "obj" in str(xaml_file):
            continue
        
        processed_count += 1
        if update_file(xaml_file):
            updated_count += 1
            print(f"+ 更新: {xaml_file.relative_to(BASE_PATH)}")
    
    print("\n" + "="*60)
    print(f"处理了 {processed_count} 个文件，更新了 {updated_count} 个文件")
    print("="*60)

if __name__ == "__main__":
    main()
