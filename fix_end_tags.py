import os
import re

tools_dir = r"d:\MyWork\SunEyeVision\SunEyeVision\SunEyeVision.Tools\Tools"

# 需要修复的XAML文件列表
files_to_fix = [
    r"EdgeDetectionTool\Views\EdgeDetectionToolDebugWindow.xaml",
    r"GaussianBlurTool\Views\GaussianBlurToolDebugWindow.xaml",
    r"ImageCaptureTool\Views\ImageCaptureToolDebugWindow.xaml",
    r"ImageSaveTool\Views\ImageSaveToolDebugWindow.xaml",
    r"OCRTool\Views\OCRToolDebugWindow.xaml",
    r"ROICropTool\Views\ROICropToolDebugWindow.xaml",
    r"TemplateMatchingTool\Views\TemplateMatchingToolDebugWindow.xaml",
    r"ThresholdTool\Views\ThresholdToolDebugWindow.xaml"
]

for filepath in files_to_fix:
    fullpath = os.path.join(tools_dir, filepath)
    if not os.path.exists(fullpath):
        print(f"File not found: {fullpath}")
        continue
    
    print(f"Processing: {filepath}")
    
    # 读取文件
    with open(fullpath, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # 移除</BaseToolDebugWindow.CustomContent>或</EnhancedToolDebugWindow.CustomContent>标签
    original_content = content
    content = re.sub(r'\s*</(BaseToolDebugWindow|EnhancedToolDebugWindow)\.CustomContent>\s*', '', content, flags=re.DOTALL)
    
    # 如果有修改，写回文件
    if content != original_content:
        with open(fullpath, 'w', encoding='utf-8') as f:
            f.write(content)
        print(f"  Updated: {os.path.basename(filepath)}")
    else:
        print(f"  No change needed")

print("Done!")
