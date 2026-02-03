@echo off
cd /d "%~dp0"
echo ========== 开始运行程序 ==========
dotnet run --project SunEyeVision.UI\SunEyeVision.UI.csproj --configuration Debug
echo ========== 程序已结束 ==========
pause
