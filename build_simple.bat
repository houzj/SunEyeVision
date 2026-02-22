@echo off
echo ========================================
echo SunEyeVision 简化构建脚本
echo ========================================
echo.

echo [1/6] 正在构建 Plugin.Abstractions...
dotnet build src\Plugin.Abstractions\SunEyeVision.Plugin.Abstractions.csproj --configuration Debug
if %errorlevel% neq 0 (
    echo.
    echo ========================================
    echo Plugin.Abstractions 编译失败！
    echo ========================================
    pause
    exit /b 1
)
echo [1/6] Plugin.Abstractions 编译成功！

echo.
echo [2/6] 正在构建 Core...
dotnet build src\Core\SunEyeVision.Core.csproj --configuration Debug
if %errorlevel% neq 0 (
    echo.
    echo ========================================
    echo Core 编译失败！
    echo ========================================
    pause
    exit /b 1
)
echo [2/6] Core 编译成功！

echo.
echo [3/6] 正在构建 Plugin.Infrastructure...
dotnet build src\Plugin.Infrastructure\SunEyeVision.Plugin.Infrastructure.csproj --configuration Debug
if %errorlevel% neq 0 (
    echo.
    echo ========================================
    echo Plugin.Infrastructure 编译失败！
    echo ========================================
    pause
    exit /b 1
)
echo [3/6] Plugin.Infrastructure 编译成功！

echo.
echo [4/6] 正在构建 Workflow...
dotnet build src\Workflow\SunEyeVision.Workflow.csproj --configuration Debug
if %errorlevel% neq 0 (
    echo.
    echo ========================================
    echo Workflow 编译失败！
    echo ========================================
    pause
    exit /b 1
)
echo [4/6] Workflow 编译成功！

echo.
echo [5/6] 正在构建 DeviceDriver...
dotnet build src\DeviceDriver\SunEyeVision.DeviceDriver.csproj --configuration Debug
if %errorlevel% neq 0 (
    echo.
    echo ========================================
    echo DeviceDriver 编译失败！
    echo ========================================
    pause
    exit /b 1
)
echo [5/6] DeviceDriver 编译成功！

echo.
echo [6/6] 正在构建 UI...
dotnet build src\UI\SunEyeVision.UI.csproj --configuration Debug
if %errorlevel% neq 0 (
    echo.
    echo ========================================
    echo UI 编译失败！
    echo ========================================
    pause
    exit /b 1
)
echo [6/6] UI 编译成功！

echo.
echo ========================================
echo 构建成功完成！
echo ========================================
echo.
echo 插件目录: src\UI\bin\Debug\net9.0-windows\plugins\
echo 运行应用程序请使用: run_app.bat
pause