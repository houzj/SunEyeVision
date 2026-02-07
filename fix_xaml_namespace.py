import os
import re

tools_dir = r"d:\MyWork\SunEyeVision\SunEyeVision\SunEyeVision.Tools\Tools"

# 递归查找所有XAML文件
for root, dirs, files in os.walk(tools_dir):
    for file in files:
        if file.endswith('.xaml'):
            filepath = os.path.join(root, file)
            print(f"Processing: {filepath}")

            # 读取文件
            with open(filepath, 'r', encoding='utf-8') as f:
                content = f.read()

            # 替换命名空间
            original_content = content
            content = re.sub(
                r'x:Class="SunEyeVision\.PluginSystem\.Tools\.',
                r'x:Class="SunEyeVision.Tools.',
                content
            )

            # 如果有修改，写回文件
            if content != original_content:
                with open(filepath, 'w', encoding='utf-8') as f:
                    f.write(content)
                print(f"  Updated: {file}")
            else:
                print(f"  No change needed")

print("Done!")
