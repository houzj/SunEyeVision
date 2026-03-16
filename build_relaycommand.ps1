# 编译 RelayCommand 重构验证
cd "d:/MyWork/SunEyeVision/SunEyeVision"
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "编译 UI 项目..." -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
& "C:/Program Files/Microsoft Visual Studio/2022/Community/MSBuild/Current/Bin/MSBuild.exe" "src/UI/SunEyeVision.UI.csproj" /p:Configuration=Debug /m /v:m /nologo
if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ UI 项目编译成功" -ForegroundColor Green
} else {
    Write-Host "✗ UI 项目编译失败" -ForegroundColor Red
    exit 1
}
