# SunEyeVision 帮助文档自动生成脚本
# 用于生成 CHM 格式的帮助文档

param(
    [string]$Configuration = "Debug",
    [string]$TargetFramework = "net9.0",
    [switch]$SkipBuild = $false
)

$ErrorActionPreference = "Stop"

# 获取脚本所在目录
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$SolutionDir = Split-Path -Parent $ScriptDir
$HelpSourceDir = Join-Path $SolutionDir "Help\Source\zh-CN"
$HelpOutputDir = Join-Path $SolutionDir "Help\Output"
$ChmFile = Join-Path $HelpOutputDir "SunEyeVision.chm"
$ProjectFile = Join-Path $ScriptDir "ApiDocGenerator\ApiDocGenerator.csproj"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "SunEyeVision 帮助文档生成工具" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# 步骤 1: 清理输出目录
Write-Host "[步骤 1/5] 清理输出目录..." -ForegroundColor Yellow
if (Test-Path $HelpOutputDir) {
    Remove-Item $HelpOutputDir -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $HelpOutputDir | Out-Null
Write-Host "完成" -ForegroundColor Green
Write-Host ""

# 步骤 2: 构建项目生成 XML 文档
if (-not $SkipBuild) {
    Write-Host "[步骤 2/5] 构建项目并生成 XML 文档..." -ForegroundColor Yellow
    $ProjectsToBuild = @(
        "SunEyeVision.Core",
        "SunEyeVision.Algorithms",
        "SunEyeVision.Workflow",
        "SunEyeVision.PluginSystem",
        "SunEyeVision.UI"
    )

    foreach ($project in $ProjectsToBuild) {
        $projectPath = Join-Path $SolutionDir "$project\$project.csproj"
        Write-Host "  构建: $project"
        & dotnet build $projectPath --configuration $Configuration --no-restore | Out-Null
        if ($LASTEXITCODE -ne 0) {
            Write-Host "  错误: 构建失败" -ForegroundColor Red
            exit 1
        }
    }
    Write-Host "完成" -ForegroundColor Green
    Write-Host ""
}

# 步骤 3: 运行 API 文档生成器
Write-Host "[步骤 3/5] 生成 API 文档..." -ForegroundColor Yellow
$apiGenOutput = Join-Path $ScriptDir "ApiDocGenerator\bin\$Configuration\$TargetFramework"
$apiGenExe = Join-Path $apiGenOutput "ApiDocGenerator.exe"

if (-not (Test-Path $apiGenExe)) {
    Write-Host "  编译 API 文档生成器..."
    & dotnet build $ProjectFile --configuration $Configuration | Out-Null
    if ($LASTEXITCODE -ne 0) {
        Write-Host "  错误: 编译失败" -ForegroundColor Red
        exit 1
    }
}

& $apiGenExe
if ($LASTEXITCODE -ne 0) {
    Write-Host "  错误: API 文档生成失败" -ForegroundColor Red
    exit 1
}
Write-Host "完成" -ForegroundColor Green
Write-Host ""

# 步骤 4: 复制静态文件
Write-Host "[步骤 4/5] 复制帮助文档文件..." -ForegroundColor Yellow
Copy-Item -Path "$HelpSourceDir\*" -Destination $HelpOutputDir -Recurse -Force
Write-Host "完成" -ForegroundColor Green
Write-Host ""

# 步骤 5: 生成 CHM 文件
Write-Host "[步骤 5/5] 生成 CHM 帮助文件..." -ForegroundColor Yellow

# 检查是否安装了 HTML Help Workshop
$hhcPath = Get-ChildItem -Path "C:\Program Files", "C:\Program Files (x86)" `
    -Filter "hhc.exe" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1 -ExpandProperty FullName

if ($hhcPath) {
    Write-Host "  找到 HTML Help Workshop: $hhcPath"
    Write-Host ""
    Write-Host "  提示: 要使用 CHM 编译功能,需要创建 HHP 项目文件。" -ForegroundColor Cyan
    Write-Host "  CHM 编集成需要手动配置 HTML Help Workshop。" -ForegroundColor Cyan
    Write-Host "  当前生成的 HTML 文档可以直接使用。" -ForegroundColor Green
} else {
    Write-Host "  未找到 HTML Help Workshop (hhc.exe)" -ForegroundColor Yellow
    Write-Host "  CHM 编译需要安装 HTML Help Workshop" -ForegroundColor Cyan
    Write-Host "  下载地址: https://www.microsoft.com/en-us/download/details.aspx?id=21138" -ForegroundColor Cyan
}

# 创建 HTML Help Workshop 项目文件模板
$hhpContent = @"
[OPTIONS]
Title=SunEyeVision 帮助文档
Contents file=SunEyeVision.hhc
Compiled file=SunEyeVision.chm
Default Topic=index.html
Display compile progress=Yes
Error log file=error.log
[FILES]
index.html
usermanual/index.html
architecture/index.html
architecture/filestructure.html
roadmap/index.html
progress/index.html
api/index.html
api/core.html
api/algorithms.html
api/workflow.html
api/plugins.html
styles.css
"@

$hhpPath = Join-Path -Path $HelpOutputDir -ChildPath "SunEyeVision.hhp"
Set-Content -Path $hhpPath -Value $hhpContent -Encoding UTF8

Write-Host "  已创建 HHP 项目文件: $hhpPath" -ForegroundColor Green
Write-Host ""

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "帮助文档生成完成!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "HTML 文档位置: $HelpOutputDir" -ForegroundColor White
Write-Host "CHM 项目文件: $hhpPath" -ForegroundColor White
Write-Host ""

# 检查是否存在 HHC 编译器
if ($hhcPath -and (Test-Path $hhcPath)) {
    $compile = Read-Host "是否立即编译 CHM 文件? (Y/N)"
    if ($compile -eq "Y" -or $compile -eq "y") {
        Write-Host "正在编译 CHM 文件..."
        Start-Process -FilePath $hhcPath -ArgumentList $hhpPath -Wait
        if (Test-Path $ChmFile) {
            Write-Host "CHM 文件生成成功: $ChmFile" -ForegroundColor Green
        } else {
            Write-Host "CHM 文件生成失败" -ForegroundColor Red
        }
    }
} else {
    Write-Host "提示: HTML 文档可以直接在浏览器中查看" -ForegroundColor Cyan
    Write-Host "如需 CHM 格式,请安装 HTML Help Workshop 并手动编译 HHP 文件" -ForegroundColor Cyan
}

Write-Host ""

