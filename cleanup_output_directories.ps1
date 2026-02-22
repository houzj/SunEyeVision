# 清理冗余输出目录脚本
# 删除工具项目下的 output 目录和根目录的 output 目录

Write-Host "开始清理冗余输出目录..." -ForegroundColor Yellow

# 删除根目录的 output 目录
$rootOutputPath = "D:\MyWork\SunEyeVision\SunEyeVision\output"
if (Test-Path $rootOutputPath) {
    Write-Host "删除根目录的 output: $rootOutputPath"
    Remove-Item -Recurse -Force $rootOutputPath
} else {
    Write-Host "根目录 output 已不存在: $rootOutputPath"
}

# 删除每个工具项目下的 output 目录
$toolsPath = "D:\MyWork\SunEyeVision\SunEyeVision\tools"
if (Test-Path $toolsPath) {
    $toolProjects = Get-ChildItem -Path $toolsPath -Directory -Filter "SunEyeVision.Tool.*"
    
    foreach ($project in $toolProjects) {
        $outputPath = Join-Path $project.FullName "output"
        if (Test-Path $outputPath) {
            Write-Host "删除工具项目输出: $outputPath"
            Remove-Item -Recurse -Force $outputPath
        } else {
            Write-Host "工具项目 output 已不存在: $outputPath"
        }
    }
} else {
    Write-Host "工具目录不存在: $toolsPath"
}

Write-Host "清理完成！" -ForegroundColor Green
Write-Host "清理的目录包括：" -ForegroundColor Cyan
Write-Host "1. 根目录: output/" -ForegroundColor Cyan
Write-Host "2. 每个工具项目下的: output/" -ForegroundColor Cyan
Write-Host "3. 共计: 1 + 9 = 10个目录" -ForegroundColor Cyan