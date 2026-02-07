import os
import re

tools_dir = r"d:\MyWork\SunEyeVision\SunEyeVision\SunEyeVision.Tools\Tools"

# 需要修改的.cs文件列表
files_to_fix = [
    r"ColorConvertTool\Views\ColorConvertToolDebugWindow.xaml.cs",
    r"EdgeDetectionTool\Views\EdgeDetectionToolDebugWindow.xaml.cs",
    r"GaussianBlurTool\Views\GaussianBlurToolDebugWindow.xaml.cs",
    r"GaussianBlurTool\Views\GaussianBlurToolEnhancedDebugWindow.xaml.cs",
    r"ImageCaptureTool\Views\ImageCaptureToolDebugWindow.xaml.cs",
    r"ImageSaveTool\Views\ImageSaveToolDebugWindow.xaml.cs",
    r"OCRTool\Views\OCRToolDebugWindow.xaml.cs",
    r"ROICropTool\Views\ROICropToolDebugWindow.xaml.cs",
    r"TemplateMatchingTool\Views\TemplateMatchingToolDebugWindow.xaml.cs",
    r"ThresholdTool\Views\ThresholdToolDebugWindow.xaml.cs"
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
    
    # 移除: Window基类声明
    original_content = content
    content = re.sub(r': Window\s*{\s*', ': {', content)
    
    # 如果有修改，写回文件
    if content != original_content:
        with open(fullpath, 'w', encoding='utf-8') as f:
            f.write(content)
        print(f"  Updated: {os.path.basename(filepath)}")
    else:
        print(f"  No change needed")

print("Done!")
