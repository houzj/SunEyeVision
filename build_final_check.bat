@echo off
cd /d d:\MyWork\SunEyeVision\SunEyeVision\SunEyeVision.UI
echo Building...
dotnet build SunEyeVision.UI.csproj --configuration Debug --no-incremental > build_final_check.txt 2>&1
echo.
echo ========== Build Results ==========
type build_final_check.txt | findstr /i "成功\|error"
pause
