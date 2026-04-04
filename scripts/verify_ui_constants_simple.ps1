# UI 常量验证脚本（简化版）
# 检查常量文件是否创建并被引用

Write-Host "=== UI 常量验证脚本 ===" -ForegroundColor Cyan

$hasErrors = $false

# 1. 检查常量文件是否存在
Write-Host "`n检查常量文件..." -ForegroundColor Yellow
$constantsFiles = @(
    "src\Plugin.SDK\UI\Constants\ColorConstants.cs",
    "src\Plugin.SDK\UI\Constants\FontConstants.cs",
    "src\Plugin.SDK\UI\Constants\LayoutConstants.cs"
)

foreach ($file in $constantsFiles) {
    if (Test-Path $file) {
        Write-Host "  ✅ $file 存在" -ForegroundColor Green
    } else {
        Write-Host "  ❌ $file 不存在" -ForegroundColor Red
        $hasErrors = $true
    }
}

# 2. 检查资源文件是否存在
Write-Host "`n检查资源文件..." -ForegroundColor Yellow
$resourceFiles = @(
    "src\Plugin.SDK\UI\Themes\Colors.xaml",
    "src\Plugin.SDK\UI\Themes\Fonts.xaml",
    "src\Plugin.SDK\UI\Themes\Layout.xaml",
    "src\Plugin.SDK\UI\Themes\Styles.xaml"
)

foreach ($file in $resourceFiles) {
    if (Test-Path $file) {
        Write-Host "  ✅ $file 存在" -ForegroundColor Green
    } else {
        Write-Host "  ❌ $file 不存在" -ForegroundColor Red
        $hasErrors = $true
    }
}

# 3. 检查 Generic.xaml 是否引用了资源字典
Write-Host "`n检查 Generic.xaml 资源引用..." -ForegroundColor Yellow
$genericXaml = "src\Plugin.SDK\UI\Themes\Generic.xaml"
if (Test-Path $genericXaml) {
    $content = Get-Content $genericXaml -Raw
    if ($content -match "Colors.xaml" -and $content -match "Fonts.xaml" -and $content -match "Layout.xaml") {
        Write-Host "  ✅ Generic.xaml 已引用资源字典" -ForegroundColor Green
    } else {
        Write-Host "  ❌ Generic.xaml 未正确引用资源字典" -ForegroundColor Red
        $hasErrors = $true
    }
} else {
    Write-Host "  ❌ Generic.xaml 不存在" -ForegroundColor Red
    $hasErrors = $true
}

# 4. 检查 BaseToolDebugControl.cs 是否引用了常量
Write-Host "`n检查 BaseToolDebugControl.cs 常量引用..." -ForegroundColor Yellow
$baseControl = "src\Plugin.SDK\UI\BaseToolDebugControl.cs"
if (Test-Path $baseControl) {
    $content = Get-Content $baseControl -Raw
    if ($content -match "FontConstants" -and $content -match "LayoutConstants") {
        Write-Host "  ✅ BaseToolDebugControl.cs 已引用常量" -ForegroundColor Green
    } else {
        Write-Host "  ⚠️  BaseToolDebugControl.cs 未完全引用常量" -ForegroundColor Yellow
    }
} else {
    Write-Host "  ❌ BaseToolDebugControl.cs 不存在" -ForegroundColor Red
    $hasErrors = $true
}

# 5. 检查 ThemeColorPalette.cs 是否已删除
Write-Host "`n检查旧文件清理..." -ForegroundColor Yellow
$oldFile = "src\Plugin.SDK\UI\Themes\ThemeColorPalette.cs"
if (-not (Test-Path $oldFile)) {
    Write-Host "  ✅ ThemeColorPalette.cs 已删除" -ForegroundColor Green
} else {
    Write-Host "  ❌ ThemeColorPalette.cs 仍然存在" -ForegroundColor Red
    $hasErrors = $true
}

# 总结
Write-Host "`n=== 验证总结 ===" -ForegroundColor Cyan
if ($hasErrors) {
    Write-Host "❌ 发现错误，需要修复" -ForegroundColor Red
    exit 1
} else {
    Write-Host "✅ 验证通过，UI 基础设施优化完成" -ForegroundColor Green
    Write-Host "`n注意：部分 XAML 文件中可能仍有硬编码值，建议逐步替换" -ForegroundColor Yellow
    exit 0
}
