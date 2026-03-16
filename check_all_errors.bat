@echo off
echo ========================================
echo 编译整个解决方案
echo ========================================
cd /d d:\MyWork\SunEyeVision\SunEyeVision
dotnet build SunEyeVision.sln > build_all_errors_check.txt 2>&1
echo.
echo ========================================
echo 编译完成
echo ========================================
type build_all_errors_check.txt | findstr /C:"error" /C:"警告" /C:"warning"
