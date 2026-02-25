#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""
编码问题模式分析工具
识别文件中不同类型的编码错误模式
"""

import os
import re
import sys
from pathlib import Path
from collections import defaultdict

# 修复控制台输出编码
sys.stdout.reconfigure(encoding='utf-8', errors='replace')

def analyze_file(filepath):
    """分析单个文件的编码问题模式"""
    patterns = {
        'replacement_char': [],      # \ufffd 替换字符
        'question_mark_chinese': [], # 中文注释中的问号（可能是截断）
        'garbled_sequence': [],      # 乱码序列
        'truncated_chinese': [],     # 截断的中文字符
    }
    
    try:
        with open(filepath, 'rb') as f:
            content_bytes = f.read()
        
        # 尝试UTF-8解码
        try:
            content = content_bytes.decode('utf-8')
        except UnicodeDecodeError:
            content = content_bytes.decode('utf-8', errors='replace')
        
        lines = content.split('\n')
        
        for i, line in enumerate(lines, 1):
            # 模式1: 替换字符
            if '\ufffd' in line:
                patterns['replacement_char'].append((i, line.strip()[:80]))
            
            # 模式2: 中文注释中的问号 (非标准问号用法)
            # 匹配类似 "参数?、" 这样的模式
            question_patterns = re.findall(r'[\u4e00-\u9fff][?]([。，、；：]|[\u4e00-\u9fff])', line)
            if question_patterns:
                patterns['question_mark_chinese'].append((i, line.strip()[:80]))
            
            # 模式3: 截断的中文 (单个问号在中文环境中)
            truncated = re.findall(r'[^\x00-\x7F]*[?][^\x00-\x7F]*', line)
            if truncated and '\ufffd' not in line:
                for t in truncated:
                    if re.search(r'[\u4e00-\u9fff]', t):
                        patterns['truncated_chinese'].append((i, line.strip()[:80]))
                        break
        
        return patterns
        
    except Exception as e:
        return {'error': str(e)}

def main():
    src_path = Path('src')
    
    print("=" * 80)
    print("编码问题模式分析")
    print("=" * 80)
    
    all_patterns = defaultdict(lambda: defaultdict(list))
    file_stats = []
    
    # 扫描所有CS文件
    cs_files = list(src_path.rglob('*.cs'))
    cs_files = [f for f in cs_files if 'obj' not in str(f) and 'bin' not in str(f)]
    
    for filepath in cs_files:
        patterns = analyze_file(filepath)
        
        if 'error' in patterns:
            continue
        
        total_issues = sum(len(v) for v in patterns.values())
        if total_issues > 0:
            file_stats.append({
                'path': str(filepath),
                'total': total_issues,
                'patterns': patterns
            })
            
            # 收集所有模式
            for pattern_type, matches in patterns.items():
                for match in matches:
                    all_patterns[pattern_type][filepath.name].append(match)
    
    # 输出模式统计
    print("\n### 模式统计 ###\n")
    for pattern_type, files in all_patterns.items():
        total = sum(len(matches) for matches in files.values())
        file_count = len(files)
        print(f"{pattern_type}: {file_count} 个文件, {total} 处问题")
    
    # 按问题数量排序文件
    file_stats.sort(key=lambda x: x['total'], reverse=True)
    
    print(f"\n### 问题文件 TOP 20 ###\n")
    print(f"{'文件':<60} {'问题数':>8}")
    print("-" * 70)
    for stat in file_stats[:20]:
        rel_path = stat['path'].replace(str(Path.cwd()) + '\\', '')
        print(f"{rel_path:<60} {stat['total']:>8}")
    
    # 详细分析前5个文件
    print("\n### 详细分析 (TOP 5) ###\n")
    for stat in file_stats[:5]:
        print(f"\n文件: {stat['path']}")
        print("-" * 60)
        for pattern_type, matches in stat['patterns'].items():
            if matches:
                print(f"\n  {pattern_type}: {len(matches)} 处")
                for line_num, snippet in matches[:5]:
                    print(f"    行 {line_num}: {snippet}")
                if len(matches) > 5:
                    print(f"    ... 还有 {len(matches) - 5} 处")
    
    # 生成建议
    print("\n" + "=" * 80)
    print("修复建议")
    print("=" * 80)
    
    if all_patterns['replacement_char']:
        print("\n1. replacement_char 模式:")
        print("   - 原因: UTF-8解码失败产生的替换字符")
        print("   - 建议: 需要根据上下文手动修复或从备份恢复")
    
    if all_patterns['question_mark_chinese']:
        print("\n2. question_mark_chinese 模式:")
        print("   - 原因: 中文字符被错误替换为问号")
        print("   - 建议: 可尝试根据上下文推断原文")
    
    if all_patterns['truncated_chinese']:
        print("\n3. truncated_chinese 模式:")
        print("   - 原因: UTF-8多字节序列被截断")
        print("   - 建议: 检查文件是否经过不当编码转换")

if __name__ == '__main__':
    main()
