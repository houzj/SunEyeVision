@echo off
chcp 65001 >nul
echo ========================================
echo    GPU解码性能测试
echo ========================================
echo.

echo 正在测试GPU解码性能...
echo.

cd /d "%~dp0"

echo 清理旧的输出文件...
if exist "test_gpu_output.txt" del "test_gpu_output.txt"

echo 编译项目...
dotnet build SunEyeVision.sln --configuration Release --no-restore > build_gpu_test.txt 2>&1

if %ERRORLEVEL% neq 0 (
    echo ✗ 编译失败，请查看错误信息
    type build_gpu_test.txt
    pause
    exit /b 1
)

echo ✓ 编译成功
echo.

echo 启动应用程序进行GPU测试...
echo.
echo 提示：
echo 1. 等待应用启动
echo 2. 加载一些图片
echo 3. 查看控制台输出的GPU解码日志
echo 4. 按任意键关闭应用
echo.

start /wait SunEyeVision.UI\bin\Release\net9.0-windows\SunEyeVision.UI.exe

echo.
echo ========================================
echo    测试完成
echo ========================================
echo.

echo 查看测试日志：
type test_gpu_output.txt 2>nul

echo.
pause
