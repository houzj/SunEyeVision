import os

tools_dir = r"d:\MyWork\SunEyeVision\SunEyeVision\SunEyeVision.Tools\Tools"

# 需要创建的.cs文件列表
files_to_create = [
    (r"EdgeDetectionTool\Views\EdgeDetectionToolDebugWindow.xaml.cs", "EdgeDetectionTool", "EdgeDetectionToolViewModel"),
    (r"GaussianBlurTool\Views\GaussianBlurToolDebugWindow.xaml.cs", "GaussianBlurTool", "GaussianBlurToolViewModel"),
    (r"ImageCaptureTool\Views\ImageCaptureToolDebugWindow.xaml.cs", "ImageCaptureTool", "ImageCaptureToolViewModel"),
    (r"ImageSaveTool\Views\ImageSaveToolDebugWindow.xaml.cs", "ImageSaveTool", "ImageSaveToolViewModel"),
    (r"OCRTool\Views\OCRToolDebugWindow.xaml.cs", "OCRTool", "OCRToolViewModel"),
    (r"ROICropTool\Views\ROICropToolDebugWindow.xaml.cs", "ROICropTool", "ROICropToolViewModel"),
    (r"TemplateMatchingTool\Views\TemplateMatchingToolDebugWindow.xaml.cs", "TemplateMatchingTool", "TemplateMatchingToolViewModel"),
    (r"ThresholdTool\Views\ThresholdToolDebugWindow.xaml.cs", "ThresholdTool", "ThresholdToolViewModel"),
]

# 获取命名空间
for filepath, tool_name, viewmodel_name in files_to_create:
    fullpath = os.path.join(tools_dir, filepath)
    namespace_name = filepath.replace("\\", ".").replace(".xaml.cs", "")
    
    # 生成命名空间（例如：SunEyeVision.Tools.EdgeDetectionTool.Views）
    namespace_parts = os.path.dirname(filepath).replace(os.sep, ".")
    full_namespace = f"SunEyeVision.Tools.{'.'.join(namespace_parts.split(os.sep))}"
    
    # 生成代码内容
    content = f"""using SunEyeVision.PluginSystem.Base.Interfaces;
using SunEyeVision.PluginSystem.Base.Models;

using SunEyeVision.PluginSystem;
using System.Windows;
using SunEyeVision.PluginSystem.Base.Base;
using SunEyeVision.PluginSystem.Infrastructure.Base;
using SunEyeVision.PluginSystem.Infrastructure.UI.Windows;


namespace {full_namespace}
{{
    public partial class {namespace_name.split('.')[-1]}
    {{
        public {namespace_name.split('.')[-1]}(string toolId, IToolPlugin? toolPlugin, ToolMetadata toolMetadata)
        {{
            InitializeComponent();

            // 创建并初始化ViewModel
            var viewModel = new {viewmodel_name}();
            viewModel.Initialize(toolId, toolPlugin, toolMetadata);
            DataContext = viewModel;
        }}
    }}
}}
"""
    
    # 写入文件
    with open(fullpath, 'w', encoding='utf-8') as f:
        f.write(content)
    print(f"Created: {os.path.basename(filepath)}")

print("Done!")
