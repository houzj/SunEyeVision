# 编译 UI 项目验证修复
chcp 65001 | Out-Null
$ErrorActionPreference = "Continue"

Write-Host "开始编译 UI 项目..." -ForegroundColor Cyan

# 编译 UI 项目
$result = & dotnet build "src/UI/SunEyeVision.UI.csproj" 2>&1
$result | ForEach-Object { Write-Host $_ }

# 检查编译结果
if ($LASTEXITCODE -eq 0) {
    Write-Host "`n✓ 编译成功！" -ForegroundColor Green
    Write-Host "已移除 WinForms 依赖，统一使用 WPF" -ForegroundColor Green
} else {
    Write-Host "`n✗ 编译失败！" -ForegroundColor Red
    Write-Host "错误代码: $LASTEXITCODE" -ForegroundColor Red
}

Write-Host "`n完成。" -ForegroundColor Cyan
