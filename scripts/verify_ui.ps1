# UI Constants Verification Script
Write-Host "=== UI Constants Verification ===" -ForegroundColor Cyan

$hasErrors = $false

# Check constants files
Write-Host "`nChecking constants files:" -ForegroundColor Yellow
if (Test-Path "src\Plugin.SDK\UI\Constants\ColorConstants.cs") {
    Write-Host "  [OK] ColorConstants.cs" -ForegroundColor Green
} else {
    Write-Host "  [ERROR] ColorConstants.cs not found" -ForegroundColor Red
    $hasErrors = $true
}

if (Test-Path "src\Plugin.SDK\UI\Constants\FontConstants.cs") {
    Write-Host "  [OK] FontConstants.cs" -ForegroundColor Green
} else {
    Write-Host "  [ERROR] FontConstants.cs not found" -ForegroundColor Red
    $hasErrors = $true
}

if (Test-Path "src\Plugin.SDK\UI\Constants\LayoutConstants.cs") {
    Write-Host "  [OK] LayoutConstants.cs" -ForegroundColor Green
} else {
    Write-Host "  [ERROR] LayoutConstants.cs not found" -ForegroundColor Red
    $hasErrors = $true
}

# Check resource files
Write-Host "`nChecking resource files:" -ForegroundColor Yellow
if (Test-Path "src\Plugin.SDK\UI\Themes\Colors.xaml") {
    Write-Host "  [OK] Colors.xaml" -ForegroundColor Green
} else {
    Write-Host "  [ERROR] Colors.xaml not found" -ForegroundColor Red
    $hasErrors = $true
}

if (Test-Path "src\Plugin.SDK\UI\Themes\Fonts.xaml") {
    Write-Host "  [OK] Fonts.xaml" -ForegroundColor Green
} else {
    Write-Host "  [ERROR] Fonts.xaml not found" -ForegroundColor Red
    $hasErrors = $true
}

if (Test-Path "src\Plugin.SDK\UI\Themes\Layout.xaml") {
    Write-Host "  [OK] Layout.xaml" -ForegroundColor Green
} else {
    Write-Host "  [ERROR] Layout.xaml not found" -ForegroundColor Red
    $hasErrors = $true
}

if (Test-Path "src\Plugin.SDK\UI\Themes\Styles.xaml") {
    Write-Host "  [OK] Styles.xaml" -ForegroundColor Green
} else {
    Write-Host "  [ERROR] Styles.xaml not found" -ForegroundColor Red
    $hasErrors = $true
}

# Check old file cleanup
Write-Host "`nChecking old file cleanup:" -ForegroundColor Yellow
if (-not (Test-Path "src\Plugin.SDK\UI\Themes\ThemeColorPalette.cs")) {
    Write-Host "  [OK] ThemeColorPalette.cs deleted" -ForegroundColor Green
} else {
    Write-Host "  [ERROR] ThemeColorPalette.cs still exists" -ForegroundColor Red
    $hasErrors = $true
}

# Check Generic.xaml references
Write-Host "`nChecking Generic.xaml:" -ForegroundColor Yellow
$genericContent = Get-Content "src\Plugin.SDK\UI\Themes\Generic.xaml" -Raw
if ($genericContent -match "Colors.xaml") {
    Write-Host "  [OK] References Colors.xaml" -ForegroundColor Green
} else {
    Write-Host "  [ERROR] Does not reference Colors.xaml" -ForegroundColor Red
    $hasErrors = $true
}

if ($genericContent -match "Fonts.xaml") {
    Write-Host "  [OK] References Fonts.xaml" -ForegroundColor Green
} else {
    Write-Host "  [ERROR] Does not reference Fonts.xaml" -ForegroundColor Red
    $hasErrors = $true
}

if ($genericContent -match "Layout.xaml") {
    Write-Host "  [OK] References Layout.xaml" -ForegroundColor Green
} else {
    Write-Host "  [ERROR] Does not reference Layout.xaml" -ForegroundColor Red
    $hasErrors = $true
}

# Summary
Write-Host "`n=== Verification Result ===" -ForegroundColor Cyan
if ($hasErrors) {
    Write-Host "[FAILED] Errors found" -ForegroundColor Red
    exit 1
} else {
    Write-Host "[SUCCESS] Verification passed" -ForegroundColor Green
    exit 0
}
