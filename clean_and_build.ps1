# 清理并重新编译 Plugin.SDK 项目
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "开始清理并重新编译 Plugin.SDK 项目" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# 清理 Plugin.SDK 项目
Write-Host "[1/4] 清理 Plugin.SDK 项目..." -ForegroundColor Yellow
& dotnet clean "src\Plugin.SDK\SunEyeVision.Plugin.SDK.csproj" --configuration Debug
if ($LASTEXITCODE -ne 0) {
    Write-Host "清理失败！" -ForegroundColor Red
    exit 1
}
Write-Host "✓ 清理完成" -ForegroundColor Green
Write-Host ""

# 清理 UI 项目
Write-Host "[2/4] 清理 UI 项目..." -ForegroundColor Yellow
& dotnet clean "src\UI\SunEyeVision.UI.csproj" --configuration Debug
if ($LASTEXITCODE -ne 0) {
    Write-Host "清理失败！" -ForegroundColor Red
    exit 1
}
Write-Host "✓ 清理完成" -ForegroundColor Green
Write-Host ""

# 编译 Plugin.SDK 项目
Write-Host "[3/4] 编译 Plugin.SDK 项目..." -ForegroundColor Yellow
& dotnet build "src\Plugin.SDK\SunEyeVision.Plugin.SDK.csproj" --configuration Debug --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "Plugin.SDK 编译失败！" -ForegroundColor Red
    Write-Host ""
    Write-Host "尝试显示详细错误信息..." -ForegroundColor Yellow
    & dotnet build "src\Plugin.SDK\SunEyeVision.Plugin.SDK.csproj" --configuration Debug --no-restore --verbosity detailed | Select-String -Pattern "error" -Context 0,2
    exit 1
}
Write-Host "✓ Plugin.SDK 编译成功" -ForegroundColor Green
Write-Host ""

# 编译 UI 项目
Write-Host "[4/4] 编译 UI 项目..." -ForegroundColor Yellow
& dotnet build "src\UI\SunEyeVision.UI.csproj" --configuration Debug --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "UI 编译失败！" -ForegroundColor Red
    Write-Host ""
    Write-Host "尝试显示详细错误信息..." -ForegroundColor Yellow
    & dotnet build "src\UI\SunEyeVision.UI.csproj" --configuration Debug --no-restore --verbosity detailed | Select-String -Pattern "error" -Context 0,2
    exit 1
}
Write-Host "✓ UI 编译成功" -ForegroundColor Green
Write-Host ""

Write-Host "========================================" -ForegroundColor Green
Write-Host "所有项目编译成功！" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
