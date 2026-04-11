# 修复 XAML 绑定中的错误字符 }\}

$filePath = "d:/MyWork/SunEyeVision_Dev-tool/tools/SunEyeVision.Tool.Threshold/Views/ThresholdToolDebugControl.xaml"

# 读取文件
$content = Get-Content -Path $filePath -Raw -Encoding UTF8

# 修复：将 }\} 替换为 }
$content = $content -replace 'ToolDebugControlBase\}\}', 'ToolDebugControlBase\}'

# 写入文件
[System.IO.File]::WriteAllText($filePath, $content, [System.Text.UTF8Encoding]::new($false))

Write-Host "修复完成！" -ForegroundColor Green
