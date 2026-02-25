# -*- coding: utf-8 -*-
"""扫描所有文件中的乱码中文"""
import os
import re
import sys

sys.stdout.reconfigure(encoding='utf-8')

# 常见乱码模式（UTF-8中文被错误解码后的特征）
garbled_patterns = [
    r'[àáâãäåæçèéêëìíîïðñòóôõöøùúûüýþÿ]+',  # 拉丁扩展字符
    r'[\u0080-\u00ff]{3,}',  # 连续的扩展ASCII字符
]

roots = [
    'd:/MyWork/SunEyeVision/SunEyeVision/src',
    'd:/MyWork/SunEyeVision/SunEyeVision/tools',
]

garbled_files = []

for path in roots:
    if not os.path.exists(path):
        continue
    for r, d, fs in os.walk(path):
        if 'obj' in r or 'bin' in r or 'node_modules' in r:
            continue
        for f in fs:
            if not f.endswith('.cs'):
                continue
            fp = os.path.join(r, f)
            try:
                with open(fp, 'r', encoding='utf-8') as file:
                    content = file.read()
                
                # 检查是否有乱码模式
                for pattern in garbled_patterns:
                    matches = re.findall(pattern, content)
                    if matches:
                        # 排除合法的法语/西班牙语单词
                        for m in matches:
                            if len(m) > 5:  # 超过5个字符的连续扩展ASCII
                                garbled_files.append(fp)
                                break
                        break
            except:
                pass

print(f'发现 {len(garbled_files)} 个可能包含乱码中文的文件:\n')
for fp in garbled_files[:30]:
    rel = fp.replace('d:/MyWork/SunEyeVision/SunEyeVision/', '')
    print(rel)
