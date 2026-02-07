import os
import re

tools_dir = r"d:\MyWork\SunEyeVision\SunEyeVision\SunEyeVision.Tools\Tools"

# 需要修改的.cs文件列表及其目标ViewModel
files_and_viewmodels = [
    (r"EdgeDetectionTool\Views\EdgeDetectionToolDebugWindow.xaml.cs", "EdgeDetectionToolViewModel"),
    (r"GaussianBlurTool\Views\GaussianBlurToolDebugWindow.xaml.cs", "GaussianBlurToolViewModel"),
    (r"GaussianBlurTool\Views\GaussianBlurToolEnhancedDebugWindow.xaml.cs", "GaussianBlurToolViewModel"),
    (r"ImageCaptureTool\Views\ImageCaptureToolDebugWindow.xaml.cs", "ImageCaptureToolViewModel"),
    (r"ImageSaveTool\Views\ImageSaveToolDebugWindow.xaml.cs", "ImageSaveToolViewModel"),
    (r"OCRTool\Views\OCRToolDebugWindow.xaml.cs", "OCRToolViewModel"),
    (r"ROICropTool\Views\ROICropToolDebugWindow.xaml.cs", "ROICropToolViewModel"),
    (r"TemplateMatchingTool\Views\TemplateMatchingToolDebugWindow.xaml.cs", "TemplateMatchingToolViewModel"),
    (r"ThresholdTool\Views\ThresholdToolDebugWindow.xaml.cs", "ThresholdToolViewModel")
]

for filepath, viewmodel_name in files_and_viewmodels:
    fullpath = os.path.join(tools_dir, filepath)
    if not os.path.exists(fullpath):
        print(f"File not found: {fullpath}")
        continue
    
    print(f"Processing: {filepath}")
    
    # 读取文件
    with open(fullpath, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # 移除所有基类声明
    original_content = content
    content = re.sub(r':\s*(BaseToolDebugWindow|Window|EnhancedToolDebugWindow)\s*\{', '', content)
    
    # 生成新的类内容（移除CreateViewModel方法，修改构造函数）
    # 移除基类构造函数调用和CreateViewModel方法
    content = re.sub(
        r':\s*base\([^)]*\)\s*\{',
        '',
        content
    )
    
    # 移除CreateViewModel方法
    content = re.sub(
        r'\s*protected\s+override\s+ToolDebugViewModelBase\s+CreateViewModel\(\).*?\{[^}]*\}',
        '',
        content,
        flags=re.DOTALL
    )
    
    # 如果有修改，写回文件
    if content != original_content:
        with open(fullpath, 'w', encoding='utf-8') as f:
            f.write(content)
        print(f"  Updated: {os.path.basename(filepath)}")
    else:
        print(f"  No change needed")

print("Done!")
