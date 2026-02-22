# SunEyeVision 项目结构优化脚本

Write-Host "开始实施 SunEyeVision 项目结构优化..."

# 检查是否在正确目录
if (-not (Test-Path "SunEyeVision.sln")) {
    Write-Error "错误：请在项目根目录运行此脚本"
    exit 1
}

Write-Host "`n=== 步骤 1: 删除冗余目录和文件 ==="
# 删除空目录
if (Test-Path "SunEyeVision.Core") {
    Remove-Item -Path "SunEyeVision.Core" -Recurse -Force
    Write-Host "✓ 已删除 SunEyeVision.Core 空目录"
} else {
    Write-Host "✓ SunEyeVision.Core 目录已不存在"
}

# 删除重复目录
if (Test-Path "SunEyeVision.Plugin.Abstractions") {
    Remove-Item -Path "SunEyeVision.Plugin.Abstractions" -Recurse -Force
    Write-Host "✓ 已删除 SunEyeVision.Plugin.Abstractions 重复目录"
} else {
    Write-Host "✓ SunEyeVision.Plugin.Abstractions 目录已不存在"
}

# 删除临时文件
if (Test-Path "temp_old_version.cs") {
    Remove-Item -Path "temp_old_version.cs" -Force
    Write-Host "✓ 已删除 temp_old_version.cs 临时文件"
} else {
    Write-Host "✓ 临时文件已不存在"
}

Write-Host "`n=== 步骤 2: 重组目录结构 ==="
# 创建 utilities 目录
if (-not (Test-Path "utilities")) {
    New-Item -ItemType Directory -Path "utilities" | Out-Null
    Write-Host "✓ 已创建 utilities 目录"
} else {
    Write-Host "✓ utilities 目录已存在"
}

# 移动非工具项目
if (Test-Path "tools\ApiDocGenerator") {
    Move-Item -Path "tools\ApiDocGenerator" -Destination "utilities" -Force
    Write-Host "✓ 已移动 ApiDocGenerator 到 utilities 目录"
} else {
    Write-Host "✓ ApiDocGenerator 已不存在或已在目标位置"
}

if (Test-Path "tools\PythonService") {
    Move-Item -Path "tools\PythonService" -Destination "utilities" -Force
    Write-Host "✓ 已移动 PythonService 到 utilities 目录"
} else {
    Write-Host "✓ PythonService 已不存在或已在目标位置"
}

# 移动 adaptagrams 到 third_party
if (Test-Path "adaptagrams") {
    if (-not (Test-Path "third_party")) {
        New-Item -ItemType Directory -Path "third_party" | Out-Null
    }
    Move-Item -Path "adaptagrams" -Destination "third_party" -Force
    Write-Host "✓ 已移动 adaptagrams 到 third_party 目录"
} else {
    Write-Host "✓ adaptagrams 已不存在或已在目标位置"
}

Write-Host "`n=== 步骤 3: 整理文档 ==="
if (-not (Test-Path "docs\reports")) {
    New-Item -ItemType Directory -Path "docs\reports" | Out-Null
    Write-Host "✓ 已创建 docs/reports 目录"
} else {
    Write-Host "✓ docs/reports 目录已存在"
}

# 移动根目录的 MD 文件到 docs/reports
$movedCount = 0
$rootFiles = Get-ChildItem -Path "." -Filter "*.md"
foreach ($file in $rootFiles) {
    if ($file.Name -ne "SunEyeVision.sln") {
        Move-Item -Path $file.FullName -Destination "docs\reports" -Force
        $movedCount++
    }
}

Write-Host "✓ 已移动 $movedCount 个 MD 文件到 docs/reports 目录"

Write-Host "`n=== 优化完成 ==="
Write-Host "以下是执行的操作："
Write-Host "1. 删除了冗余目录：SunEyeVision.Core、SunEyeVision.Plugin.Abstractions"
Write-Host "2. 删除了临时文件：temp_old_version.cs"
Write-Host "3. 创建了 utilities 目录并移动了非工具项目"
Write-Host "4. 移动了 adaptagrams 到 third_party 目录"
Write-Host "5. 整理了文档文件到 docs/reports 目录"

Write-Host "`n请检查项目是否正常编译，如有需要请运行 build.bat 重新构建项目。"