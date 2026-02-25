# 一键准备 PluginDevKit（从主项目构建）
# 使用方法: .\prepare-devkit.ps1 [-Configuration Release]

param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$RootDir = Split-Path -Parent $PSScriptRoot
$DevKitDir = Join-Path $RootDir "PluginDevKit"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  准备 PluginDevKit 开发环境" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# 1. 构建 SDK
Write-Host "`n[1/3] 构建 SDK..." -ForegroundColor Yellow
& (Join-Path $DevKitDir "scripts\build-sdk.ps1") -Configuration $Configuration

# 2. 构建 Runtime
Write-Host "`n[2/3] 构建 Runtime..." -ForegroundColor Yellow
& (Join-Path $DevKitDir "scripts\build-runtime.ps1") -Configuration $Configuration

# 3. 构建示例插件
Write-Host "`n[3/3] 构建示例插件..." -ForegroundColor Yellow
$SamplePlugin = Join-Path $DevKitDir "samples\SamplePlugin\SamplePlugin.csproj"
if (Test-Path $SamplePlugin) {
    dotnet build $SamplePlugin -c $Configuration
}

Write-Host "`n========================================" -ForegroundColor Green
Write-Host "  PluginDevKit 准备完成！" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "接下来可以：" -ForegroundColor Yellow
Write-Host "  1. 打开 PluginDevKit\PluginDevKit.sln 开始开发" -ForegroundColor White
Write-Host "  2. 运行 PluginDevKit\scripts\launch-debug.ps1 启动调试" -ForegroundColor White
Write-Host "  3. 分发 PluginDevKit 目录给插件开发者" -ForegroundColor White
Write-Host ""

# 显示目录结构
Write-Host "PluginDevKit 目录结构:" -ForegroundColor Yellow
Get-ChildItem $DevKitDir -Directory | ForEach-Object {
    Write-Host "  $($_.Name)/"
}
