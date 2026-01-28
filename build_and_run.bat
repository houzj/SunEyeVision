@echo off
chcp 65001 >nul
echo ========================================
echo   SunEyeVision 构建和运行脚本
echo ========================================
echo.

cd /d "%~dp0"

REM 检查 .NET SDK 是否安装
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo [错误] 未检测到 .NET SDK
    echo.
    echo 请先安装 .NET 9.0 SDK:
    echo https://dotnet.microsoft.com/download/dotnet/9.0
    echo.
    pause
    exit /b 1
)

echo [信息] .NET SDK 已安装
echo.

REM 构建整个解决方案
echo ========================================
echo   第一步: 恢复 NuGet 包
echo ========================================
dotnet restore SunEyeVision.sln --verbosity minimal
if errorlevel 1 (
    echo [错误] NuGet 包恢复失败!
    pause
    exit /b 1
)
echo.
echo [成功] NuGet 包恢复完成
echo.

echo ========================================
echo   第二步: 构建解决方案
echo ========================================
dotnet build SunEyeVision.sln --configuration Release --no-restore --verbosity minimal
if errorlevel 1 (
    echo [错误] 构建失败!
    echo.
    echo 尝试查看详细错误信息:
    dotnet build SunEyeVision.sln --configuration Release --verbosity detailed
    pause
    exit /b 1
)
echo.
echo [成功] 构建完成
echo.

echo ========================================
echo   第三步: 启动应用程序
echo ========================================
echo 正在启动 SunEyeVision.UI...
echo.

REM 启动应用程序
start "" "SunEyeVision.UI\bin\Release\net9.0-windows\SunEyeVision.UI.exe"

if errorlevel 1 (
    echo [警告] 直接启动失败，尝试使用 dotnet run...
    dotnet run --project SunEyeVision.UI --configuration Release --no-build
) else (
    echo [成功] 应用程序已启动!
    echo.
    echo 应用程序窗口应该已经打开。
    echo 如果没有看到窗口，请检查是否有错误提示。
)

echo.
echo ========================================
echo   操作完成
echo ========================================
echo.
pause
