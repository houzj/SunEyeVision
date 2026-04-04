# UI 常量验证脚本
Write-Host "=== UI 常量验证 ===" -ForegroundColor Cyan

$hasErrors = $false

# 检查常量文件
Write-Host "`n检查常量文件:" -ForegroundColor Yellow
if (Test-Path "src\Plugin.SDK\UI\Constants\ColorConstants.cs") {
    Write-Host "  ✅ ColorConstants.cs" -ForegroundColor Green
} else {
    Write-Host "  ❌ ColorConstants.cs 不存在" -ForegroundColor Red
    $hasErrors = $true
}

if (Test-Path "src\Plugin.SDK\UI\Constants\FontConstants.cs") {
    Write-Host "  ✅ FontConstants.cs" -ForegroundColor Green
} else {
    Write-Host "  ❌ FontConstants.cs 不存在" -ForegroundColor Red
    $hasErrors = $true
}

if (Test-Path "src\Plugin.SDK\UI\Constants\LayoutConstants.cs") {
    Write-Host "  ✅ LayoutConstants.cs" -ForegroundColor Green
} else {
    Write-Host "  ❌ LayoutConstants.cs 不存在" -ForegroundColor Red
    $hasErrors = $true
}

# 检查资源文件
Write-Host "`n检查资源文件:" -ForegroundColor Yellow
if (Test-Path "src\Plugin.SDK\UI\Themes\Colors.xaml") {
    Write-Host "  ✅ Colors.xaml" -ForegroundColor Green
} else {
    Write-Host "  ❌ Colors.xaml 不存在" -ForegroundColor Red
    $hasErrors = $true
}

if (Test-Path "src\Plugin.SDK\UI\Themes\Fonts.xaml") {
    Write-Host "  ✅ Fonts.xaml" -ForegroundColor Green
} else {
    Write-Host "  ❌ Fonts.xaml 不存在" -ForegroundColor Red
    $hasErrors = $true
}

if (Test-Path "src\Plugin.SDK\UI\Themes\Layout.xaml") {
    Write-Host "  ✅ Layout.xaml" -ForegroundColor Green
} else {
    Write-Host "  ❌ Layout.xaml 不存在" -ForegroundColor Red
    $hasErrors = $true
}

if (Test-Path "src\Plugin.SDK\UI\Themes\Styles.xaml") {
    Write-Host "  ✅ Styles.xaml" -ForegroundColor Green
} else {
    Write-Host "  ❌ Styles.xaml 不存在" -ForegroundColor Red
    $hasErrors = $true
}

# 检查旧文件清理
Write-Host "`n检查旧文件清理:" -ForegroundColor Yellow
if (-not (Test-Path "src\Plugin.SDK\UI\Themes\ThemeColorPalette.cs")) {
    Write-Host "  ✅ ThemeColorPalette.cs 已删除" -ForegroundColor Green
} else {
    Write-Host "  ❌ ThemeColorPalette.cs 仍存在" -ForegroundColor Red
    $hasErrors = $true
}

# 检查 Generic.xaml 引用
Write-Host "`n检查 Generic.xaml:" -ForegroundColor Yellow
$genericContent = Get-Content "src\Plugin.SDK\UI\Themes\Generic.xaml" -Raw
if ($genericContent -match "Colors.xaml") {
    Write-Host "  ✅ 引用 Colors.xaml" -ForegroundColor Green
} else {
    Write-Host "  ❌ 未引用 Colors.xaml" -ForegroundColor Red
    $hasErrors = $true
}

if ($genericContent -match "Fonts.xaml") {
    Write-Host "  ✅ 引用 Fonts.xaml" -ForegroundColor Green
} else {
    Write-Host "  ❌ 未引用 Fonts.xaml" -ForegroundColor Red
    $hasErrors = $true
}

if ($genericContent -match "Layout.xaml") {
    Write-Host "  ✅ 引用 Layout.xaml" -ForegroundColor Green
} else {
    Write-Host "  ❌ 未引用 Layout.xaml" -ForegroundColor Red
    $hasErrors = $true
}

# 总结
Write-Host "`n=== 验证结果 ===" -ForegroundColor Cyan
if ($hasErrors) {
    Write-Host "❌ 发现错误" -ForegroundColor Red
    exit 1
} else {
    Write-Host "✅ 验证通过" -ForegroundColor Green
    exit 0
}
