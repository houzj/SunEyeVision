#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""
生成编码修复建议清单
针对每个问题文件，提取损坏行并生成修复建议
"""

import os
import re
import sys
from pathlib import Path
from collections import defaultdict

sys.stdout.reconfigure(encoding='utf-8', errors='replace')

# 常见的代码元素和对应注释映射（可根据项目扩展）
COMMON_COMMENTS = {
    # 通用
    'NodeId': '节点ID',
    'SubroutineId': '子程序ID',
    'CallDepth': '调用深度',
    'CallTime': '调用时间',
    'SubroutineCallInfo': '子程序调用信息',
    'WorkflowContext': '工作流执行上下文',
    'ExecutionPath': '执行路径',
    'NodeStates': '节点状态',
    'CallStack': '调用栈',
    'Variables': '变量',
    'Logs': '日志',
    'CancellationToken': '取消令牌',
    'IsDebugMode': '调试模式',
    'ProgressReporter': '进度报告器',
    'CurrentIteration': '当前迭代次数',
    'MaxIterations': '最大迭代次数',
    'LoopCondition': '循环条件',
    'ExecutionCount': '执行次数',
    'TotalExecutionTime': '总执行时间',
    'StartTime': '开始时间',
    'EndTime': '结束时间',
    'Duration': '持续时间',
    'Outputs': '输出',
    'Errors': '错误',
    'Success': '是否成功',
    'IsStopped': '是否停止',
    
    # 缩略词
    'Id': 'ID',
    'ID': 'ID',
}

def detect_encoding_issues(content):
    """检测内容中的编码问题"""
    issues = []
    lines = content.split('\n')
    
    for i, line in enumerate(lines, 1):
        # 检测锟斤拷模式
        if '锟' in line or '斤' in line or '拷' in line:
            issues.append(('garbled', i, line.strip()))
        # 检测替换字符
        elif '\ufffd' in line:
            issues.append(('replacement', i, line.strip()))
        # 检测中文环境中的问号
        elif re.search(r'[\u4e00-\u9fff]\?[\u4e00-\u9fff，。、；：]', line):
            issues.append(('question', i, line.strip()))
    
    return issues

def guess_comment(code_element):
    """根据代码元素猜测注释内容"""
    # 直接匹配
    if code_element in COMMON_COMMENTS:
        return COMMON_COMMENTS[code_element]
    
    # 后缀匹配
    for key, value in COMMON_COMMENTS.items():
        if code_element.endswith(key):
            prefix = code_element[:-len(key)]
            return prefix + value
    
    return None

def analyze_and_suggest(filepath):
    """分析文件并生成修复建议"""
    try:
        with open(filepath, 'rb') as f:
            content_bytes = f.read()
        content = content_bytes.decode('utf-8', errors='replace')
    except Exception as e:
        return None, str(e)
    
    issues = detect_encoding_issues(content)
    if not issues:
        return None, "No issues found"
    
    suggestions = []
    lines = content.split('\n')
    
    for issue_type, line_num, line_content in issues:
        suggestion = {
            'line': line_num,
            'type': issue_type,
            'original': line_content,
            'suggestion': None
        }
        
        # 尝试推断注释内容
        # 模式: /// <summary>锟斤拷...</summary>
        summary_match = re.search(r'///\s*<summary>(.+?)</summary>', line_content)
        if summary_match:
            # 查找下一行的代码元素
            if line_num < len(lines):
                next_lines = '\n'.join(lines[line_num:line_num+5])
                # 查找属性或类名
                prop_match = re.search(r'public\s+\w+\s+(\w+)\s*\{', next_lines)
                class_match = re.search(r'public\s+(?:class|record|struct|interface)\s+(\w+)', next_lines)
                method_match = re.search(r'public\s+\w+\s+(\w+)\s*\(', next_lines)
                
                element_name = None
                if prop_match:
                    element_name = prop_match.group(1)
                elif class_match:
                    element_name = class_match.group(1)
                elif method_match:
                    element_name = method_match.group(1)
                
                if element_name:
                    guessed = guess_comment(element_name)
                    if guessed:
                        suggestion['suggestion'] = f'/// <summary>{guessed}</summary>'
                        suggestion['element'] = element_name
        
        suggestions.append(suggestion)
    
    return suggestions, None

def main():
    print("=" * 80)
    print("编码修复建议生成")
    print("=" * 80)
    
    # 只处理问题最严重的几个文件
    priority_files = [
        'src/Workflow/WorkflowContext.cs',
        'src/Core/Enums/LogLevel.cs',
        'src/Core/Interfaces/IConfigManager.cs',
    ]
    
    for filepath in priority_files:
        if not Path(filepath).exists():
            continue
        
        print(f"\n{'='*80}")
        print(f"文件: {filepath}")
        print("=" * 80)
        
        suggestions, error = analyze_and_suggest(filepath)
        
        if error:
            print(f"错误: {error}")
            continue
        
        if not suggestions:
            print("无问题")
            continue
        
        print(f"\n发现 {len(suggestions)} 处问题:\n")
        
        for s in suggestions[:20]:  # 只显示前20个
            print(f"行 {s['line']} [{s['type']}]:")
            print(f"  原文: {s['original'][:70]}...")
            if s.get('suggestion'):
                print(f"  建议: {s['suggestion']}")
                if s.get('element'):
                    print(f"  元素: {s['element']}")
            print()

if __name__ == '__main__':
    main()
