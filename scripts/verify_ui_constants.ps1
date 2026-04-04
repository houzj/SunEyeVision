# UI 常量验证脚本
# 检查 Plugin.SDK/UI 中是否还有硬编码的颜色、字体、布局值

Write-Host "=== UI 常量验证脚本 ===" -ForegroundColor Cyan
Write-Host ""

$uiPath = "src\Plugin.SDK\UI"
$hasErrors = $false

# 1. 检查 XAML 文件中的硬编码颜色值
Write-Host "检查 XAML 文件中的硬编码颜色值..." -ForegroundColor Yellow
$xamlFiles = Get-ChildItem -Path $uiPath -Filter "*.xaml" -Recurse | Where-Object { $_.FullName -notmatch "\\obj\\|\\bin\\" }

foreach ($file in $xamlFiles) {
    $content = Get-Content $file.FullName -Raw
    $matches = [regex]::Matches($content, 'Color="#[0-9A-Fa-f]{6}"|Background="#[0-9A-Fa-f]{6}"|Foreground="#[0-9A-Fa-f]{6}"|BorderBrush="#[0-9A-Fa-f]{6}"')
    
    if ($matches.Count -gt 0) {
        Write-Host "  ❌ $($file.Name) - 发现 $($matches.Count) 处硬编码颜色" -ForegroundColor Red
        foreach ($match in $matches | Select-Object -First 3) {
            Write-Host "     $($match.Value)" -ForegroundColor Gray
        }
        $hasErrors = $true
    }
}

Write-Host ""

# 2. 检查 XAML 文件中的硬编码字体大小
Write-Host "检查 XAML 文件中的硬编码字体大小..." -ForegroundColor Yellow
$fontSizes = @("FontSize=\""8\""", "FontSize=\""10\""", "FontSize=\""11\""", "FontSize=\""12\""", "FontSize=\""13\""", "FontSize=\""14\""", "FontSize=\""16\""")

foreach ($file in $xamlFiles) {
    $content = Get-Content $file.FullName -Raw
    $foundMatches = @()
    
    foreach ($fontSize in $fontSizes) {
        $matches = [regex]::Matches($content, [regex]::Escape($fontSize))
        if ($matches.Count -gt 0) {
            $foundMatches += $matches
        }
    }
    
    if ($foundMatches.Count -gt 0) {
        Write-Host "  ⚠️  $($file.Name) - 发现 $($foundMatches.Count) 处硬编码字体大小" -ForegroundColor Yellow
        foreach ($match in $foundMatches | Select-Object -First 3) {
            Write-Host "     $($match.Value)" -ForegroundColor Gray
        }
    }
}

Write-Host ""

# 3. 检查 C# 文件中的硬编码字体和布局值
Write-Host "检查 C# 文件中的硬编码字体和布局值..." -ForegroundColor Yellow
$csFiles = Get-ChildItem -Path $uiPath -Filter "*.cs" -Recurse | Where-Object { $_.FullName -notmatch "\\obj\\|\\bin\\" }

foreach ($file in $csFiles) {
    $content = Get-Content $file.FullName -Raw
    
    # 检查硬编码字体大小
    $fontSizeMatches = [regex]::Matches($content, 'FontSize\s*=\s*\d+')
    
    # 检查硬编码控件尺寸
    $sizeMatches = [regex]::Matches($content, '(Width|Height)\s*=\s*\d+')
    
    if ($fontSizeMatches.Count -gt 0) {
        Write-Host "  ⚠️  $($file.Name) - 发现 $($fontSizeMatches.Count) 处硬编码字体大小" -ForegroundColor Yellow
        foreach ($match in $fontSizeMatches | Select-Object -First 3) {
            Write-Host "     $($match.Value)" -ForegroundColor Gray
        }
    }
    
    if ($sizeMatches.Count -gt 5) {
        Write-Host "  ⚠️  $($file.Name) - 发现 $($sizeMatches.Count) 处硬编码尺寸值" -ForegroundColor Yellow
        foreach ($match in $sizeMatches | Select-Object -First 3) {
            Write-Host "     $($match.Value)" -ForegroundColor Gray
        }
    }
}

Write-Host ""

# 4. 检查是否引用了常量类
Write-Host "检查常量类引用..." -ForegroundColor Yellow

$colorConstantsUsed = Get-ChildItem -Path $uiPath -Recurse -Include "*.cs","*.xaml" | Where-Object { $_.FullName -notmatch "\\obj\\|\\bin\\" } | Select-String -Pattern "ColorConstants" -Quiet
$fontConstantsUsed = Get-ChildItem -Path $uiPath -Recurse -Include "*.cs","*.xaml" | Where-Object { $_.FullName -notmatch "\\obj\\|\\bin\\" } | Select-String -Pattern "FontConstants" -Quiet
$layoutConstantsUsed = Get-ChildItem -Path $uiPath -Recurse -Include "*.cs","*.xaml" | Where-Object { $_.FullName -notmatch "\\obj\\|\\bin\\" } | Select-String -Pattern "LayoutConstants" -Quiet

if ($colorConstantsUsed) {
    Write-Host "  ✅ ColorConstants 已被引用" -ForegroundColor Green
} else {
    Write-Host "  ❌ ColorConstants 未被引用" -ForegroundColor Red
    $hasErrors = $true
}

if ($fontConstantsUsed) {
    Write-Host "  ✅ FontConstants 已被引用" -ForegroundColor Green
} else {
    Write-Host "  ❌ FontConstants 未被引用" -ForegroundColor Red
    $hasErrors = $true
}

if ($layoutConstantsUsed) {
    Write-Host "  ✅ LayoutConstants 已被引用" -ForegroundColor Green
} else {
    Write-Host "  ❌ LayoutConstants 未被引用" -ForegroundColor Red
    $hasErrors = $true
}

Write-Host ""

# 5. 检查常量文件是否存在
Write-Host "检查常量文件..." -ForegroundColor Yellow
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

Write-Host ""

# 6. 检查资源文件是否存在
Write-Host "检查资源文件..." -ForegroundColor Yellow
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

Write-Host ""

Write-Host ""

# 总结
Write-Host "=== 验证总结 ===" -ForegroundColor Cyan
if ($hasErrors) {
    Write-Host "❌ 发现错误，需要修复" -ForegroundColor Red
    exit 1
} else {
    Write-Host "✅ 验证通过，所有常量文件已创建并被正确引用" -ForegroundColor Green
    Write-Host ""
    Write-Host "注意：部分 XAML 文件中可能仍有硬编码值，建议逐步替换" -ForegroundColor Yellow
    exit 0
}

