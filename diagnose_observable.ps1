# 诊断 ObservableObject 使用情况
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "诊断 ObservableObject 引用问题" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# 检查 Plugin.SDK 中的所有 ObservableObject 子类
Write-Host "[1] 检查 Plugin.SDK 中使用 ObservableObject 的文件..." -ForegroundColor Yellow
Write-Host ""

$regionFiles = Get-ChildItem -Path "src\Plugin.SDK\UI\Controls\Region" -Recurse -Filter "*.cs"
$observableFiles = @()

foreach ($file in $regionFiles) {
    $content = Get-Content $file.FullName -Raw
    if ($content -match ':\s*ObservableObject') {
        $hasUsing = $content -match 'using\s+SunEyeVision\.Plugin\.SDK\.Models'
        $observableFiles += [PSCustomObject]@{
            File = $file.Name
            RelativePath = $file.FullName.Replace((Get-Location).Path + "\", "")
            HasUsing = $hasUsing
        }
    }
}

Write-Host "找到 $($observableFiles.Count) 个使用 ObservableObject 的文件：" -ForegroundColor Cyan
Write-Host ""

foreach ($item in $observableFiles | Sort-Object RelativePath) {
    $status = if ($item.HasUsing) { "✓" } else { "✗" }
    $color = if ($item.HasUsing) { "Green" } else { "Red" }
    Write-Host "  [$status] $($item.RelativePath)" -ForegroundColor $color
    if (-not $item.HasUsing) {
        Write-Host "      ⚠️  缺少 using SunEyeVision.Plugin.SDK.Models;" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "[2] 检查 ObservableObject.cs 文件是否存在..." -ForegroundColor Yellow
Write-Host ""

$observableObjectPath = "src\Plugin.SDK\Models\ObservableObject.cs"
if (Test-Path $observableObjectPath) {
    Write-Host "  ✓ ObservableObject.cs 文件存在" -ForegroundColor Green
    Write-Host "    路径: $observableObjectPath" -ForegroundColor Gray

    $content = Get-Content $observableObjectPath -Raw
    if ($content -match 'namespace\s+SunEyeVision\.Plugin\.SDK\.Models') {
        Write-Host "  ✓ 命名空间正确: SunEyeVision.Plugin.SDK.Models" -ForegroundColor Green
    } else {
        Write-Host "  ✗ 命名空间不正确！" -ForegroundColor Red
    }
} else {
    Write-Host "  ✗ ObservableObject.cs 文件不存在！" -ForegroundColor Red
}

Write-Host ""
Write-Host "[3] 检查 Plugin.SDK 项目文件..." -ForegroundColor Yellow
Write-Host ""

$csprojPath = "src\Plugin.SDK\SunEyeVision.Plugin.SDK.csproj"
if (Test-Path $csprojPath) {
    $csproj = [xml](Get-Content $csprojPath)
    $targetFramework = $csproj.Project.PropertyGroup.TargetFramework
    Write-Host "  目标框架: $targetFramework" -ForegroundColor Cyan
    Write-Host "  项目文件: ✓" -ForegroundColor Green
} else {
    Write-Host "  项目文件: ✗ 不存在" -ForegroundColor Red
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "诊断完成" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
