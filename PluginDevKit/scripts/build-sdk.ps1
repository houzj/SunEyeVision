# 从主项目构建 SDK 并复制到 PluginDevKit
# 使用方法: .\build-sdk.ps1 [-Configuration Release]

param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

# 路径配置
$RootDir = Split-Path -Parent $PSScriptRoot
$MainProjectDir = Split-Path -Parent $RootDir
$SdkOutputDir = Join-Path $RootDir "sdk\net9.0"

Write-Host "=== 构建 SunEyeVision Plugin SDK ===" -ForegroundColor Cyan
Write-Host "配置: $Configuration"
Write-Host "主项目目录: $MainProjectDir"
Write-Host "SDK 输出目录: $SdkOutputDir"

# 创建 SDK 输出目录
if (-not (Test-Path $SdkOutputDir)) {
    New-Item -ItemType Directory -Path $SdkOutputDir -Force | Out-Null
    Write-Host "创建 SDK 目录: $SdkOutputDir" -ForegroundColor Green
}

# 构建 SDK 项目（正确路径）
$SdkProject = Join-Path $MainProjectDir "src\Plugin.SDK\SunEyeVision.Plugin.SDK.csproj"

if (-not (Test-Path $SdkProject)) {
    Write-Host "SDK 项目不存在: $SdkProject" -ForegroundColor Red
    throw "找不到 SDK 项目文件"
}

Write-Host "构建 SDK 项目..." -ForegroundColor Yellow
dotnet build $SdkProject -c $Configuration --no-incremental

if ($LASTEXITCODE -ne 0) {
    throw "SDK 项目构建失败"
}

# 复制 SDK 文件
$SdkBinDir = Join-Path $MainProjectDir "src\Plugin.SDK\bin\$Configuration\net9.0"
$SdkFiles = @(
    "SunEyeVision.Plugin.SDK.dll",
    "SunEyeVision.Plugin.SDK.pdb",
    "SunEyeVision.Plugin.SDK.xml",
    "OpenCvSharp.dll",
    "OpenCvSharp.Extensions.dll"
)

foreach ($file in $SdkFiles) {
    $srcPath = Join-Path $SdkBinDir $file
    if (Test-Path $srcPath) {
        Copy-Item $srcPath $SdkOutputDir -Force
        Write-Host "复制: $file" -ForegroundColor Green
    }
}

# 复制运行时依赖
$RuntimeDir = Join-Path $SdkBinDir "runtimes"
if (Test-Path $RuntimeDir) {
    $DestRuntimeDir = Join-Path $SdkOutputDir "runtimes"
    Copy-Item $RuntimeDir $DestRuntimeDir -Recurse -Force
    Write-Host "复制: runtimes/" -ForegroundColor Green
}

Write-Host "`n=== SDK 构建完成 ===" -ForegroundColor Cyan
Write-Host "SDK 文件位于: $SdkOutputDir"

# 列出 SDK 文件
Get-ChildItem $SdkOutputDir | Format-Table Name, Length, LastWriteTime
