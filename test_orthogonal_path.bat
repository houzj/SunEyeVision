@echo off
REM 测试 OrthogonalPathCalculator 的批处理脚本

echo ========================================
echo 测试 OrthogonalPathCalculator
echo ========================================
echo.

echo 1. 构建 Debug 版本...
cd /d "%~dp0"
dotnet build SunEyeVision.sln --configuration Debug

if %errorlevel% neq 0 (
    echo.
    echo [错误] 构建失败！
    pause
    exit /b 1
)

echo.
echo [成功] 构建完成！
echo.
echo 2. 启动应用程序进行测试...
echo.
echo 测试步骤：
echo 1. 观察连接线是否正确显示
echo 2. 尝试拖拽节点，观察连接线是否跟随
echo 3. 尝试拖拽端口创建新连接
echo.

cd SunEyeVision.UI\bin\Debug\net9.0-windows
start SunEyeVision.UI.exe

echo.
echo 应用程序已启动，请观察连接线渲染情况。
echo 按 Ctrl+C 查看日志输出。
pause
