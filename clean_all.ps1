# 完全清理并重新编译 SunEyeVision 解决方案
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "开始完全清理 SunEyeVision 解决方案" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$ErrorActionPreference = "Stop"

# 停止所有可能的进程
Write-Host "[1/6] 停止可能运行的进程..." -ForegroundColor Yellow
$processes = @("SunEyeVision.UI", "devenv", "MSBuild")
foreach ($process in $processes) {
    try {
        Get-Process -Name $process -ErrorAction SilentlyContinue | Stop-Process -Force
        Write-Host "  ✓ 已停止 $process" -ForegroundColor Green
    } catch {
        Write-Host "  - $process 未运行" -ForegroundColor Gray
    }
}
Start-Sleep -Seconds 2
Write-Host ""

# 清理 Plugin.SDK 项目
Write-Host "[2/6] 清理 Plugin.SDK 项目..." -ForegroundColor Yellow
if (Test-Path "src\Plugin.SDK\bin") {
    Remove-Item -Path "src\Plugin.SDK\bin" -Recurse -Force
    Write-Host "  ✓ 已删除 bin 目录" -ForegroundColor Green
}
if (Test-Path "src\Plugin.SDK\obj") {
    Remove-Item -Path "src\Plugin.SDK\obj" -Recurse -Force
    Write-Host "  ✓ 已删除 obj 目录" -ForegroundColor Green
}
Write-Host ""

# 清理 UI 项目
Write-Host "[3/6] 清理 UI 项目..." -ForegroundColor Yellow
if (Test-Path "src\UI\bin") {
    Remove-Item -Path "src\UI\bin" -Recurse -Force
    Write-Host "  ✓ 已删除 bin 目录" -ForegroundColor Green
}
if (Test-Path "src\UI\obj") {
    Remove-Item -Path "src\UI\obj" -Recurse -Force
    Write-Host "  ✓ 已删除 obj 目录" -ForegroundColor Green
}
Write-Host ""

# 清理 Core 项目
Write-Host "[4/6] 清理 Core 项目..." -ForegroundColor Yellow
if (Test-Path "src\Core\bin") {
    Remove-Item -Path "src\Core\bin" -Recurse -Force
    Write-Host "  ✓ 已删除 bin 目录" -ForegroundColor Green
}
if (Test-Path "src\Core\obj") {
    Remove-Item -Path "src\Core\obj" -Recurse -Force
    Write-Host "  ✓ 已删除 obj 目录" -ForegroundColor Green
}
Write-Host ""

# 使用 dotnet restore 恢复依赖
Write-Host "[5/6] 恢复 NuGet 包依赖..." -ForegroundColor Yellow
& dotnet restore SunEyeVision.sln
if ($LASTEXITCODE -ne 0) {
    Write-Host "  ✗ NuGet 包恢复失败！" -ForegroundColor Red
    exit 1
}
Write-Host "  ✓ NuGet 包恢复成功" -ForegroundColor Green
Write-Host ""

# 编译 Plugin.SDK 项目
Write-Host "[6/7] 编译 Plugin.SDK 项目 (Debug)..." -ForegroundColor Yellow
& dotnet build "src\Plugin.SDK\SunEyeVision.Plugin.SDK.csproj" --configuration Debug --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "  ✗ Plugin.SDK 编译失败！" -ForegroundColor Red
    Write-Host ""
    Write-Host "尝试显示详细错误信息..." -ForegroundColor Yellow
    & dotnet build "src\Plugin.SDK\SunEyeVision.Plugin.SDK.csproj" --configuration Debug --no-restore --verbosity diagnostic 2>&1 | Select-String -Pattern "error CS" -Context 0,1
    exit 1
}
Write-Host "  ✓ Plugin.SDK 编译成功" -ForegroundColor Green
Write-Host ""

# 编译 Core 项目
Write-Host "[7/7] 编译 Core 项目 (Debug)..." -ForegroundColor Yellow
& dotnet build "src\Core\SunEyeVision.Core.csproj" --configuration Debug --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "  ✗ Core 编译失败！" -ForegroundColor Red
    exit 1
}
Write-Host "  ✓ Core 编译成功" -ForegroundColor Green
Write-Host ""

Write-Host "========================================" -ForegroundColor Green
Write-Host "核心依赖编译成功！" -ForegroundColor Green
Write-Host ""
Write-Host "下一步：" -ForegroundColor Yellow
Write-Host "1. 在 Visual Studio 中打开 SunEyeVision.sln" -ForegroundColor Cyan
Write-Host "2. 右键点击 SunEyeVision.UI 项目" -ForegroundColor Cyan
Write-Host "3. 选择 '重新生成'" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Green
