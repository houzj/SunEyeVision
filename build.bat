@echo off
echo ========================================
echo SunEyeVision 构建脚本
echo ========================================
echo.

echo 正在构建解决方案...
dotnet build SunEyeVision.sln

if %errorlevel% equ 0 (
    echo.
    echo ========================================
    echo 构建成功！
    echo ========================================
    echo.
    echo 运行应用程序请使用: run_app.bat
) else (
    echo.
    echo ========================================
    echo 构建失败！请检查错误信息。
    echo ========================================
)

pause
