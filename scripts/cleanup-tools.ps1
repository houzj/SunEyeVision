# 清理 tools 目录中的空目录和遗留文件
# 使用方法: .\cleanup-tools.ps1 [-DryRun]

param(
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"

$ToolsDir = Join-Path $PSScriptRoot "..\tools"

Write-Host "=== 清理 tools 目录 ===" -ForegroundColor Cyan
Write-Host "Tools 目录: $ToolsDir"
Write-Host "模式: $(if ($DryRun) { '预览模式' } else { '执行模式' })"

# 要删除的顶级遗留目录
$TopLevelDirsToRemove = @(
    "output"      # 遗留输出目录
    "src"         # 空目录
)

# 每个插件项目中要删除的空目录
$PluginEmptyDirs = @(
    "src"
    "output" 
    "Views"
)

# 删除顶级遗留目录
Write-Host "`n[清理顶级遗留目录]" -ForegroundColor Yellow
foreach ($dir in $TopLevelDirsToRemove) {
    $path = Join-Path $ToolsDir $dir
    if (Test-Path $path) {
        $isEmpty = (Get-ChildItem $path -Recurse -Force | Measure-Object).Count -eq 0
        if ($isEmpty -or $dir -eq "output") {
            Write-Host "  删除: $dir" -ForegroundColor $(if ($DryRun) { 'Gray' } else { 'Green' })
            if (-not $DryRun) {
                Remove-Item $path -Recurse -Force
            }
        } else {
            Write-Host "  跳过（非空）: $dir" -ForegroundColor DarkGray
        }
    }
}

# 查找所有插件项目
$PluginProjects = Get-ChildItem $ToolsDir -Directory | Where-Object { 
    $_.Name -like "SunEyeVision.Tool.*" 
}

Write-Host "`n[清理插件项目空目录]" -ForegroundColor Yellow
foreach ($project in $PluginProjects) {
    Write-Host "  检查: $($project.Name)" -ForegroundColor DarkGray
    
    foreach ($dir in $PluginEmptyDirs) {
        $path = Join-Path $project.FullName $dir
        if (Test-Path $path) {
            $isEmpty = (Get-ChildItem $path -Recurse -Force -ErrorAction SilentlyContinue | Measure-Object).Count -eq 0
            if ($isEmpty) {
                Write-Host "    删除空目录: $dir" -ForegroundColor $(if ($DryRun) { 'Gray' } else { 'Green' })
                if (-not $DryRun) {
                    Remove-Item $path -Recurse -Force
                }
            }
        }
    }
}

# 清理 obj 目录中的缓存文件（保留必要的）
Write-Host "`n[清理 obj 缓存文件]" -ForegroundColor Yellow
$ObjDirs = Get-ChildItem $ToolsDir -Directory -Recurse -Filter "obj" -ErrorAction SilentlyContinue

foreach ($objDir in $ObjDirs) {
    $CacheFiles = Get-ChildItem $objDir.FullName -Filter "*.cache" -Recurse -ErrorAction SilentlyContinue
    if ($CacheFiles.Count -gt 0) {
        Write-Host "  清理: $($objDir.Parent.Name)\obj\*.cache ($($CacheFiles.Count) 个文件)" -ForegroundColor $(if ($DryRun) { 'Gray' } else { 'Green' })
        if (-not $DryRun) {
            $CacheFiles | Remove-Item -Force
        }
    }
}

# 清理 tools/output/plugins 遗留文件
$OutputPluginsDir = Join-Path $ToolsDir "output\plugins"
if (Test-Path $OutputPluginsDir) {
    Write-Host "`n[清理遗留插件输出]" -ForegroundColor Yellow
    Write-Host "  删除: output\plugins\" -ForegroundColor $(if ($DryRun) { 'Gray' } else { 'Green' })
    if (-not $DryRun) {
        Remove-Item $OutputPluginsDir -Recurse -Force -ErrorAction SilentlyContinue
    }
}

Write-Host "`n=== 清理完成 ===" -ForegroundColor Cyan

if ($DryRun) {
    Write-Host "这是预览模式，请移除 -DryRun 参数执行实际清理" -ForegroundColor Yellow
}
