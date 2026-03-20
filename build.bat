@echo off
cd /d "%~dp0"
echo ========================================
echo SunEyeVision 构建脚本 (Debug模式)
echo ========================================
echo.

:: 设置临时日志文件（使用系统临时目录）
set TEMP_LOG=%TEMP%\suneyevision_build_%RANDOM%.txt

echo 正在编译主解决方案...
dotnet build SunEyeVision.sln --configuration Debug > "%TEMP_LOG%" 2>&1
if %errorlevel% neq 0 (
    echo.
    echo ========================================
    echo 编译失败！请检查错误信息。
    echo ========================================
    type "%TEMP_LOG%"
    del "%TEMP_LOG%" 2>nul
    pause
    exit /b 1
)

:: 编译成功，删除临时日志
del "%TEMP_LOG%" 2>nul

echo.
echo ========================================
echo 构建成功！
echo ========================================
echo.
echo 插件目录: src\UI\bin\Debug\net9.0-windows\plugins\
echo 运行应用程序请使用: run_app.bat
pause
