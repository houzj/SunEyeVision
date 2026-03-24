@echo off
cd /d "%~dp0"
echo ========================================
echo 快速修复编译错误
echo ========================================
echo.

echo [1/3] 清理旧的 Release 目录...
if exist "Release\SunEyeVision_v1.0.0" (
    echo 删除旧的发布目录...
    rmdir /s /q "Release\SunEyeVision_v1.0.0"
)
echo 清理完成！
echo.

echo [2/3] 清理解决方案...
dotnet clean SunEyeVision.sln --configuration Debug
echo 清理完成！
echo.

echo [3/3] 重新编译解决方案...
dotnet build SunEyeVision.sln --configuration Debug
if %errorlevel% neq 0 (
    echo.
    echo ========================================
    echo 编译失败！请检查错误信息。
    echo ========================================
    pause
    exit /b 1
)

echo.
echo ========================================
echo 编译成功！
echo ========================================
echo.
pause
