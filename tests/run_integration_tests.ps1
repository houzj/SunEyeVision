# SunEyeVision 统一集成测试运行脚本 (PowerShell)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "SunEyeVision 统一集成测试运行器" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# 设置路径
$scriptDir = $PSScriptRoot
$projectRoot = Split-Path -Parent $scriptDir

Write-Host "[1/4] 清理旧的测试结果..." -ForegroundColor Yellow
$testResultsPath = Join-Path $projectRoot "TestResults"
if (Test-Path $testResultsPath) {
    Remove-Item -Path $testResultsPath -Recurse -Force
}

Write-Host "[2/4] 编译测试项目..." -ForegroundColor Yellow
$testProjectPath = Join-Path $projectRoot "tests\SunEyeVision.Core.Tests\SunEyeVision.Core.Tests.csproj"
$buildResult = dotnet build $testProjectPath --configuration Debug --no-restore 2>&1

if ($LASTEXITCODE -ne 0) {
    Write-Host "编译失败！" -ForegroundColor Red
    Write-Host $buildResult
    exit 1
}

Write-Host "[3/4] 运行单元测试和集成测试..." -ForegroundColor Yellow
$testResult = dotnet test $testProjectPath --configuration Debug --no-build --logger "trx;LogFileName=test_results.trx" --results-directory $testResultsPath 2>&1

Write-Host "[4/4] 生成测试报告..." -ForegroundColor Yellow
$reportPath = Join-Path $projectRoot "test_report.txt"
$timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"

$reportContent = @"
========================================
SunEyeVision 测试报告
时间: $timestamp
========================================

$testResult

测试已完成，结果保存在 TestResults 目录中。
"@

$reportContent | Out-File -FilePath $reportPath -Encoding UTF8

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "测试完成！" -ForegroundColor Green
Write-Host "结果目录: $testResultsPath" -ForegroundColor Green
Write-Host "测试报告: $reportPath" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green

# 解析测试结果
if ($testResult -match "Passed:\s+(\d+)") {
    Write-Host "通过测试数: $($Matches[1])" -ForegroundColor Green
}
if ($testResult -match "Failed:\s+(\d+)") {
    Write-Host "失败测试数: $($Matches[1])" -ForegroundColor Red
}
