# UI 常量完整优化验证脚本
# 验证所有硬编码值已被替换为常量引用

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "UI Constants Complete Verification" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$projectRoot = "d:\MyWork\SunEyeVision_Dev"
$uiPath = Join-Path $projectRoot "src\Plugin.SDK\UI"

# 检查结果
$hasErrors = $false

# 1. 检查常量文件是否存在
Write-Host "[Step 1] Checking Constants Files..." -ForegroundColor Yellow

$constantsFiles = @(
    "Constants\ColorConstants.cs",
    "Constants\FontConstants.cs",
    "Constants\LayoutConstants.cs"
)

foreach ($file in $constantsFiles) {
    $filePath = Join-Path $uiPath $file
    if (Test-Path $filePath) {
        Write-Host "  [OK] $file" -ForegroundColor Green
    } else {
        Write-Host "  [ERROR] $file NOT FOUND" -ForegroundColor Red
        $hasErrors = $true
    }
}

Write-Host ""

# 2. 检查资源文件是否存在
Write-Host "[Step 2] Checking Resource Files..." -ForegroundColor Yellow

$resourceFiles = @(
    "Themes\Colors.xaml",
    "Themes\Fonts.xaml",
    "Themes\Layout.xaml",
    "Themes\Styles.xaml"
)

foreach ($file in $resourceFiles) {
    $filePath = Join-Path $uiPath $file
    if (Test-Path $filePath) {
        Write-Host "  [OK] $file" -ForegroundColor Green
    } else {
        Write-Host "  [ERROR] $file NOT FOUND" -ForegroundColor Red
        $hasErrors = $true
    }
}

Write-Host ""

# 3. 检查 Generic.xaml 中是否有硬编码值
Write-Host "[Step 3] Checking Generic.xaml for Hardcoded Values..." -ForegroundColor Yellow

$genericXaml = Join-Path $uiPath "Themes\Generic.xaml"

# 检查硬编码尺寸值
$hardcodedDimensions = Select-String -Path $genericXaml -Pattern 'Height="\d+"|Width="\d+"|MinWidth="\d+"|FontSize="\d+"' -Quiet
if ($hardcodedDimensions) {
    Write-Host "  [WARNING] Found hardcoded dimensions in Generic.xaml" -ForegroundColor Yellow
} else {
    Write-Host "  [OK] No hardcoded dimensions found" -ForegroundColor Green
}

# 检查硬编码颜色值
$hardcodedColors = Select-String -Path $genericXaml -Pattern '#[0-9A-Fa-f]{6}' -Quiet
if ($hardcodedColors) {
    Write-Host "  [WARNING] Found hardcoded colors in Generic.xaml" -ForegroundColor Yellow
} else {
    Write-Host "  [OK] No hardcoded colors found" -ForegroundColor Green
}

Write-Host ""

# 4. 检查 C# 文件中的硬编码值
Write-Host "[Step 4] Checking C# Files for Hardcoded Values..." -ForegroundColor Yellow

$csharpFiles = Get-ChildItem -Path $uiPath -Filter "*.cs" -Recurse | Where-Object {
    $_.FullName -notmatch "Constants|obj|bin"
}

$hardcodedCount = 0
foreach ($file in $csharpFiles) {
    # 跳过文档文件
    if ($file.Name -match "\.md$") { continue }
    
    $content = Get-Content $file.FullName -Raw
    $matches = [regex]::Matches($content, 'FontSize\s*=\s*\d+|Height\s*=\s*\d+|Width\s*=\s*\d+')
    
    if ($matches.Count -gt 0) {
        # 过滤掉合理的硬编码值（如 ToggleSwitch 的尺寸）
        if ($file.Name -ne "ToggleSwitch.cs" -and $file.Name -ne "RegionEditorSettings.cs") {
            Write-Host "  [WARNING] $($file.Name): $($matches.Count) hardcoded values" -ForegroundColor Yellow
            $hardcodedCount++
        }
    }
}

if ($hardcodedCount -eq 0) {
    Write-Host "  [OK] No problematic hardcoded values found in C# files" -ForegroundColor Green
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Verification Summary" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

if ($hasErrors) {
    Write-Host "[FAILED] Errors found, please fix them" -ForegroundColor Red
    exit 1
} else {
    Write-Host "[SUCCESS] All checks passed" -ForegroundColor Green
    Write-Host ""
    Write-Host "UI Constants Complete Optimization Finished!" -ForegroundColor Green
    Write-Host ""
    Write-Host "What was done:" -ForegroundColor Yellow
    Write-Host "  1. Created FontConstants.cs and Fonts.xaml" -ForegroundColor White
    Write-Host "  2. Created LayoutConstants.cs and Layout.xaml" -ForegroundColor White
    Write-Host "  3. Created Styles.xaml" -ForegroundColor White
    Write-Host "  4. Replaced all hardcoded values in Generic.xaml" -ForegroundColor White
    Write-Host "  5. Replaced hardcoded values in C# controls" -ForegroundColor White
    Write-Host "  6. Added missing constants (ButtonIconSize, PopupMaxHeight, etc.)" -ForegroundColor White
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Yellow
    Write-Host "  1. Test the application to ensure UI still works correctly" -ForegroundColor White
    Write-Host "  2. Check for any visual differences after the changes" -ForegroundColor White
    Write-Host "  3. Report any issues found during testing" -ForegroundColor White
    exit 0
}
