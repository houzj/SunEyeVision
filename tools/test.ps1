# SunEyeVision ??????????
# ???? CHM ???????

param(
    [string]$Configuration = "Debug",
    [string]$TargetFramework = "net9.0",
    [switch]$SkipBuild = $false
)

$ErrorActionPreference = "Stop"

# ????????
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$SolutionDir = Split-Path -Parent $ScriptDir
$HelpSourceDir = Join-Path $SolutionDir "Help\Source\zh-CN"
$HelpOutputDir = Join-Path $SolutionDir "Help\Output"
$ChmFile = Join-Path $HelpOutputDir "SunEyeVision.chm"
$ProjectFile = Join-Path $ScriptDir "ApiDocGenerator\ApiDocGenerator.csproj"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "SunEyeVision ????????" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# ?? 1: ??????
Write-Host "[?? 1/5] ??????..." -ForegroundColor Yellow
if (Test-Path $HelpOutputDir) {
    Remove-Item $HelpOutputDir -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $HelpOutputDir | Out-Null
Write-Host "??" -ForegroundColor Green
Write-Host ""

# ?? 2: ?????? XML ??
if (-not $SkipBuild) {
    Write-Host "[?? 2/5] ??????? XML ??..." -ForegroundColor Yellow
    $ProjectsToBuild = @(
        "SunEyeVision.Core",
        "SunEyeVision.Algorithms",
        "SunEyeVision.Workflow",
        "SunEyeVision.PluginSystem",
        "SunEyeVision.UI"
    )

    foreach ($project in $ProjectsToBuild) {
        $projectPath = Join-Path $SolutionDir "$project\$project.csproj"
        Write-Host "  ??: $project"
        & dotnet build $projectPath --configuration $Configuration --no-restore | Out-Null
        if ($LASTEXITCODE -ne 0) {
            Write-Host "  ??: ????" -ForegroundColor Red
            exit 1
        }
    }
    Write-Host "??" -ForegroundColor Green
    Write-Host ""
}

# ?? 3: ?? API ?????
Write-Host "[?? 3/5] ?? API ??..." -ForegroundColor Yellow
$apiGenOutput = Join-Path $ScriptDir "ApiDocGenerator\bin\$Configuration\$TargetFramework"
$apiGenExe = Join-Path $apiGenOutput "ApiDocGenerator.exe"

if (-not (Test-Path $apiGenExe)) {
    Write-Host "  ?? API ?????..."
    & dotnet build $ProjectFile --configuration $Configuration | Out-Null
    if ($LASTEXITCODE -ne 0) {
        Write-Host "  ??: ????" -ForegroundColor Red
        exit 1
    }
}

& $apiGenExe
if ($LASTEXITCODE -ne 0) {
    Write-Host "  ??: API ??????" -ForegroundColor Red
    exit 1
}
Write-Host "??" -ForegroundColor Green
Write-Host ""

# ?? 4: ??????
Write-Host "[?? 4/5] ????????..." -ForegroundColor Yellow
Copy-Item -Path "$HelpSourceDir\*" -Destination $HelpOutputDir -Recurse -Force
Write-Host "??" -ForegroundColor Green
Write-Host ""

# ?? 5: ?? CHM ??
Write-Host "[?? 5/5] ?? CHM ????..." -ForegroundColor Yellow

# ??????? HTML Help Workshop
$hhcPath = Get-ChildItem -Path "C:\Program Files", "C:\Program Files (x86)" `
    -Filter "hhc.exe" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1 -ExpandProperty FullName

if ($hhcPath) {
    Write-Host "  ?? HTML Help Workshop: $hhcPath"
    Write-Host ""
    Write-Host "  ??: ??? CHM ????,???? HHP ?????" -ForegroundColor Cyan
    Write-Host "  CHM ????????? HTML Help Workshop?" -ForegroundColor Cyan
    Write-Host "  ????? HTML ?????????" -ForegroundColor Green
} else {
    Write-Host "  ??? HTML Help Workshop (hhc.exe)" -ForegroundColor Yellow
    Write-Host "  CHM ?????? HTML Help Workshop" -ForegroundColor Cyan
    Write-Host "  ????: https://www.microsoft.com/en-us/download/details.aspx?id=21138" -ForegroundColor Cyan
}

# ?? HTML Help Workshop ??????
$hhpContent = @"
[OPTIONS]
Title=SunEyeVision ????
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

Write-Host "  ??? HHP ????: $hhpPath" -ForegroundColor Green
Write-Host ""

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "????????!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "HTML ????: $HelpOutputDir" -ForegroundColor White
Write-Host "CHM ????: $hhpPath" -ForegroundColor White
Write-Host ""

# ?????? HHC ???
if ($hhcPath -and (Test-Path $hhcPath)) {
    $compile = Read-Host "?????? CHM ??? (Y/N)"
    if ($compile -eq "Y" -or $compile -eq "y") {
        Write-Host "???? CHM ??..."
        Start-Process -FilePath $hhcPath -ArgumentList $hhpPath -Wait
        if (Test-Path $ChmFile) {
            Write-Host "CHM ??????: $ChmFile" -ForegroundColor Green
        } else {
            Write-Host "CHM ??????" -ForegroundColor Red
        }
    }
} else {
    Write-Host "??: HTML ?????????????" -ForegroundColor Cyan
    Write-Host "?? CHM ??,??? HTML Help Workshop ????? HHP ??" -ForegroundColor Cyan
}

Write-Host ""

