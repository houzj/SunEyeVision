# 批量更新命名空间
# 使用方法: 在项目根目录运行此脚本

Write-Host "开始批量更新命名空间..." -ForegroundColor Cyan

# 定义要替换的内容
$replacements = @{
    "namespace VisionMaster.Algorithms" = "namespace SunEyeVision.Algorithms"
    "namespace VisionMaster.Workflow" = "namespace SunEyeVision.Workflow"
    "namespace VisionMaster.DeviceDriver" = "namespace SunEyeVision.DeviceDriver"
    "namespace VisionMaster.PluginSystem" = "namespace SunEyeVision.PluginSystem"
    "namespace VisionMaster.UI" = "namespace SunEyeVision.UI"
    "namespace VisionMaster.UI.Models" = "namespace SunEyeVision.UI.Models"
    "namespace VisionMaster.UI.ViewModels" = "namespace SunEyeVision.UI.ViewModels"
    "namespace VisionMaster.UI.Views" = "namespace SunEyeVision.UI.Views"
    "namespace VisionMaster.UI.Converters" = "namespace SunEyeVision.UI.Converters"
    "namespace VisionMaster.Demo" = "namespace SunEyeVision.Demo"
    "namespace VisionMaster.Test" = "namespace SunEyeVision.Test"
    "using VisionMaster.Models;" = "using SunEyeVision.Models;"
    "using VisionMaster.Interfaces;" = "using SunEyeVision.Interfaces;"
    "using VisionMaster.Services;" = "using SunEyeVision.Services;"
    "using VisionMaster.Algorithms;" = "using SunEyeVision.Algorithms;"
    "using VisionMaster.Workflow;" = "using SunEyeVision.Workflow;"
    "using VisionMaster.DeviceDriver;" = "using SunEyeVision.DeviceDriver;"
    "using VisionMaster.PluginSystem;" = "using SunEyeVision.PluginSystem;"
    ": VisionMaster.Interfaces." = ": SunEyeVision.Interfaces."
    ": VisionMaster.Models." = ": SunEyeVision.Models."
    ": VisionMaster.Services." = ": SunEyeVision.Services."
}

# 查找所有 .cs 文件（排除 obj 和 bin 目录）
$csFiles = Get-ChildItem -Path . -Filter *.cs -Recurse | 
    Where-Object { $_.FullName -notmatch "\\(obj|bin|\.vs)\\" }

Write-Host "`n找到 $($csFiles.Count) 个 .cs 文件`n" -ForegroundColor Yellow

# 对每个文件执行替换
$count = 0
foreach ($file in $csFiles) {
    $content = Get-Content $file.FullName -Raw -Encoding UTF8
    $modified = $false
    
    foreach ($key in $replacements.Keys) {
        if ($content -match [regex]::Escape($key)) {
            $content = $content -replace [regex]::Escape($key), $replacements[$key]
            $modified = $true
        }
    }
    
    if ($modified) {
        Set-Content $file.FullName $content -Encoding UTF8 -NoNewline
        Write-Host "  更新: $($file.FullName.Substring($pwd.Path.Length + 1))" -ForegroundColor Green
        $count++
    }
}

Write-Host "`n共更新 $count 个文件" -ForegroundColor Cyan

# 更新 XAML 文件
Write-Host "`n`n更新 XAML 文件..." -ForegroundColor Yellow

$xamlReplacements = @{
    "xmlns:local=`"clr-namespace:VisionMaster.UI`"" = "xmlns:local=`"clr-namespace:SunEyeVision.UI`""
    "xmlns:vm=`"clr-namespace:VisionMaster.UI.ViewModels`"" = "xmlns:vm=`"clr-namespace:SunEyeVision.UI.ViewModels`""
    "xmlns:converters=`"clr-namespace:VisionMaster.UI.Converters`"" = "xmlns:converters=`"clr-namespace:SunEyeVision.UI.Converters`""
    'x:Class="VisionMaster.UI.App"' = 'x:Class="SunEyeVision.UI.App"'
    'x:Class="VisionMaster.UI.Views.MainWindow"' = 'x:Class="SunEyeVision.UI.Views.MainWindow"'
    'x:Class="VisionMaster.UI.Views.MainWindow_Simple"' = 'x:Class="SunEyeVision.UI.Views.MainWindow_Simple"'
    'x:Class="VisionMaster.UI.Views.DocumentationWindow"' = 'x:Class="SunEyeVision.UI.Views.DocumentationWindow"'
}

$xamlFiles = Get-ChildItem -Path . -Filter *.xaml -Recurse | 
    Where-Object { $_.FullName -notmatch "\\(obj|bin|\.vs)\\" }

$xamlCount = 0
foreach ($file in $xamlFiles) {
    $content = Get-Content $file.FullName -Raw -Encoding UTF8
    $modified = $false
    
    foreach ($key in $xamlReplacements.Keys) {
        if ($content -match [regex]::Escape($key)) {
            $content = $content -replace [regex]::Escape($key), $xamlReplacements[$key]
            $modified = $true
        }
    }
    
    if ($modified) {
        Set-Content $file.FullName $content -Encoding UTF8 -NoNewline
        Write-Host "  更新: $($file.FullName.Substring($pwd.Path.Length + 1))" -ForegroundColor Green
        $xamlCount++
    }
}

Write-Host "`n共更新 $xamlCount 个 XAML 文件" -ForegroundColor Cyan

Write-Host "`n✅ 命名空间更新完成!" -ForegroundColor Green
Write-Host "`n下一步: 手动重命名文件夹和项目文件,然后重新构建" -ForegroundColor Yellow
