#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""
递进式编码修复 - 第一轮：问题较少的文件
"""

import os
import re

# 问题较少的文件列表（<20处）
FILES_TO_FIX = [
    'src/Core/Interfaces/Plugins/IAlgorithmPlugin.cs',
    'src/Core/Interfaces/Plugins/INodePlugin.cs',
    'src/Core/Interfaces/Plugins/IPlugin.cs',
    'src/Core/Interfaces/Plugins/IPluginUIProvider.cs',
    'src/Core/IO/FileAccessScope.cs',
    'src/Core/Services/LogManager.cs',
    'src/Core/Services/OptimizedLogger.cs',
    'src/Core/Services/PluginManager.cs',
    'src/Plugin.Infrastructure/Infrastructure/IPluginManager.cs',
    'src/UI/Adapters/AIAnalysisNodeDisplayAdapter.cs',
    'src/UI/Adapters/DiagramAdapter.cs',
    'src/UI/Adapters/ImageSourceNodeDisplayAdapter.cs',
    'src/UI/Adapters/LibraryValidator.cs',
    'src/UI/Adapters/ProcessingNodeDisplayAdapter.cs',
    'src/UI/Adapters/VideoSourceNodeDisplayAdapter.cs',
    'src/UI/Controls/Rendering/DirectXGpuThumbnailLoader.cs',
    'src/UI/Controls/Rendering/ExifThumbnailExtractor.cs',
    'src/UI/App.xaml.cs',
]

def fix_file(filepath):
    """尝试修复单个文件"""
    try:
        with open(filepath, 'rb') as f:
            content_bytes = f.read()
        
        # 尝试UTF-8解码
        try:
            content = content_bytes.decode('utf-8')
        except UnicodeDecodeError:
            content = content_bytes.decode('utf-8', errors='replace')
        
        # 检查是否有替换字符
        if '\ufffd' not in content:
            return 0, "No issues found"
        
        # 找出所有损坏的行
        lines = content.split('\n')
        fixed_lines = []
        fixes_made = 0
        
        for line in lines:
            if '\ufffd' in line:
                # 尝试根据上下文修复
                fixed_line = fix_line(line)
                if fixed_line != line:
                    fixes_made += 1
                fixed_lines.append(fixed_line)
            else:
                fixed_lines.append(line)
        
        new_content = '\n'.join(fixed_lines)
        
        # 写回文件
        with open(filepath, 'w', encoding='utf-8') as f:
            f.write(new_content)
        
        return fixes_made, "Fixed"
    
    except Exception as e:
        return 0, f"Error: {str(e)}"

def fix_line(line):
    """尝试根据上下文修复单行"""
    # 常见修复模式
    
    # 修复 /// <summary> 中的乱码
    if '/// <summary>' in line and '\ufffd' in line:
        # 提取可能的代码元素名
        pass
    
    # 简单替换：删除纯乱码注释
    # 如果行中只有乱码字符，尝试删除
    if line.strip().startswith('//') and '\ufffd' in line:
        # 计算乱码占比
        garbled_count = line.count('\ufffd')
        chinese_count = len(re.findall(r'[\u4e00-\u9fff]', line))
        
        if garbled_count > chinese_count * 2:
            # 乱码太多，删除该行或替换为空注释
            indent = len(line) - len(line.lstrip())
            return ' ' * indent + '// [注释已损坏]'
    
    return line

def main():
    print("=" * 60)
    print("递进式编码修复 - 第一轮：问题较少的文件")
    print("=" * 60)
    
    total_fixed = 0
    
    for filepath in FILES_TO_FIX:
        if not os.path.exists(filepath):
            print(f"\n跳过: {filepath} (文件不存在)")
            continue
        
        fixes, status = fix_file(filepath)
        total_fixed += fixes
        print(f"\n{filepath}")
        print(f"  修复: {fixes} 处, 状态: {status}")
    
    print("\n" + "=" * 60)
    print(f"总计修复: {total_fixed} 处")
    print("=" * 60)

if __name__ == '__main__':
    main()
