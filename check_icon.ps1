# 检查图标和文件关联状态
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  SunEyeVision 图标状态检查" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# 1. 检查源图标文件
Write-Host "[1/5] 检查源图标文件..." -ForegroundColor Yellow
$sourceIcon = "d:\MyWork\SunEyeVision\SunEyeVision\src\UI\Icons\solution.ico"
if (Test-Path $sourceIcon) {
    $size = (Get-Item $sourceIcon).Length
    Write-Host "  ✅ 源图标存在: $sourceIcon" -ForegroundColor Green
    Write-Host "     文件大小: $size 字节 ($([math]::Round($size/1KB, 2)) KB)" -ForegroundColor Gray
} else {
    Write-Host "  ❌ 源图标不存在: $sourceIcon" -ForegroundColor Red
}
Write-Host ""

# 2. 检查部署的图标文件
Write-Host "[2/5] 检查部署的图标文件..." -ForegroundColor Yellow
$deployIcon = "d:\MyWork\SunEyeVision\SunEyeVision\src\UI\bin\Release\net9.0-windows\Icons\solution.ico"
if (Test-Path $deployIcon) {
    $size = (Get-Item $deployIcon).Length
    Write-Host "  ✅ 部署图标存在: $deployIcon" -ForegroundColor Green
    Write-Host "     文件大小: $size 字节 ($([math]::Round($size/1KB, 2)) KB)" -ForegroundColor Gray
} else {
    Write-Host "  ❌ 部署图标不存在: $deployIcon" -ForegroundColor Red
}
Write-Host ""

# 3. 检查文件扩展名注册
Write-Host "[3/5] 检查文件扩展名注册..." -ForegroundColor Yellow
$extKey = "HKCU:\Software\Classes\.solution"
if (Test-Path $extKey) {
    $value = (Get-ItemProperty $extKey).""
    Write-Host "  ✅ 文件扩展名已注册" -ForegroundColor Green
    Write-Host "     注册项: $extKey" -ForegroundColor Gray
    Write-Host "     默认值: $value" -ForegroundColor Gray
} else {
    Write-Host "  ❌ 文件扩展名未注册" -ForegroundColor Red
    Write-Host "     请运行程序以注册文件关联" -ForegroundColor Yellow
}
Write-Host ""

# 4. 检查 ProgID 注册
Write-Host "[4/5] 检查 ProgID 注册..." -ForegroundColor Yellow
$progId = "HKCU:\Software\Classes\SunEyeVision.SolutionFile"
if (Test-Path $progId) {
    $value = (Get-ItemProperty $progId).""
    Write-Host "  ✅ ProgID 已注册" -ForegroundColor Green
    Write-Host "     注册项: $progId" -ForegroundColor Gray
    Write-Host "     默认值: $value" -ForegroundColor Gray
} else {
    Write-Host "  ❌ ProgID 未注册" -ForegroundColor Red
}
Write-Host ""

# 5. 检查图标注册
Write-Host "[5/5] 检查图标注册..." -ForegroundColor Yellow
$iconKey = "HKCU:\Software\Classes\SunEyeVision.SolutionFile\DefaultIcon"
if (Test-Path $iconKey) {
    $value = (Get-ItemProperty $iconKey).""
    Write-Host "  ✅ 图标已注册" -ForegroundColor Green
    Write-Host "     注册项: $iconKey" -ForegroundColor Gray
    Write-Host "     图标路径: $value" -ForegroundColor Gray
} else {
    Write-Host "  ❌ 图标未注册" -ForegroundColor Red
}
Write-Host ""

# 总结
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  检查完成" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# 提供操作建议
Write-Host "建议操作：" -ForegroundColor Yellow
Write-Host "1. 如果所有项目都显示 ✅，刷新文件资源管理器（F5）" -ForegroundColor Gray
Write-Host "2. 如果有 ❌ 项目，运行 setup_icon_display.bat" -ForegroundColor Gray
Write-Host "3. 如果图标仍未更新，运行 clear_icon_cache.bat" -ForegroundColor Gray
Write-Host ""
