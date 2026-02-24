@echo off
chcp 65001 >nul
echo ========================================
echo SunEyeVision Plugin SDK NuGet 打包工具
echo ========================================
echo.

:: 设置变量
set SOLUTION_DIR=%~dp0
set NUPKG_DIR=%SOLUTION_DIR%nupkg
set PROJECT_PATH=%SOLUTION_DIR%src\Plugin.Abstractions\SunEyeVision.Plugin.Abstractions.csproj

:: 创建输出目录
if not exist "%NUPKG_DIR%" mkdir "%NUPKG_DIR%"

:: 清理旧包
echo 清理旧的NuGet包...
del /q "%NUPKG_DIR%\SunEyeVision.Plugin.SDK.*" 2>nul

:: 还原依赖
echo 还原NuGet依赖...
dotnet restore "%PROJECT_PATH%"
if %ERRORLEVEL% neq 0 (
    echo [错误] NuGet依赖还原失败
    pause
    exit /b 1
)

:: 编译项目
echo 编译SDK项目...
dotnet build "%PROJECT_PATH%" -c Release
if %ERRORLEVEL% neq 0 (
    echo [错误] 编译失败
    pause
    exit /b 1
)

:: 打包NuGet
echo 创建NuGet包...
dotnet pack "%PROJECT_PATH%" -c Release -o "%NUPKG_DIR%"
if %ERRORLEVEL% neq 0 (
    echo [错误] NuGet打包失败
    pause
    exit /b 1
)

echo.
echo ========================================
echo 打包完成！
echo ========================================
echo.
echo NuGet包位置: %NUPKG_DIR%
echo.
dir /b "%NUPKG_DIR%\*.nupkg" 2>nul
echo.
echo 使用方法:
echo   1. 添加本地NuGet源:
echo      dotnet nuget add source "%NUPKG_DIR%" -n SunEyeVisionLocal
echo.
echo   2. 在项目中安装:
echo      dotnet add package SunEyeVision.Plugin.SDK
echo.
pause
