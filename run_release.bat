@echo off
echo ========================================
echo SunEyeVision Release 运行脚本
echo ========================================
echo.

echo [1/3] 清理所有 Debug 配置...
for /d /r %%d in (bin\Debug) do @if exist "%%d" (
    echo 删除 %%d...
    rd /s /q "%%d"
)

echo.
echo [2/3] 清理所有 obj 目录...
for /d /r %%d in (obj) do @if exist "%%d" (
    rd /s /q "%%d"
)

echo.
echo [3/3] 启动应用程序...
start "" "SunEyeVision.UI\bin\Release\net9.0-windows\SunEyeVision.UI.exe"

echo.
echo ========================================
echo 应用程序已启动！
echo ========================================
pause
