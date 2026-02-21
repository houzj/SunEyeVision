@echo off
echo ========================================
echo SunEyeVision 构建脚本
echo ========================================
echo.

echo [1/3] 正在编译工具插件解决方案...
dotnet build tools\SunEyeVision.Tools.sln --configuration Release
if %errorlevel% neq 0 (
    echo.
    echo ========================================
    echo 工具插件编译失败！请检查错误信息。
    echo ========================================
    pause
    exit /b 1
)
echo [1/3] 工具插件编译成功！

echo.
echo [2/3] 正在编译主解决方案...
dotnet build SunEyeVision.sln --configuration Release
if %errorlevel% neq 0 (
    echo.
    echo ========================================
    echo 主解决方案编译失败！请检查错误信息。
    echo ========================================
    pause
    exit /b 1
)
echo [2/3] 主解决方案编译成功！

echo.
echo ========================================
echo 构建成功！
echo ========================================
echo.
echo 插件目录: output\plugins\
echo 运行应用程序请使用: run_app.bat
pause
