@echo off
REM SunEyeVision 快速调试启动脚本
REM 使用方式: debug.bat [config]
REM   config: debug (默认) 或 release

setlocal enabledelayedexpansion

set "CONFIG=Debug"
if /I "%~1"=="release" set "CONFIG=Release"
if /I "%~1"=="Release" set "CONFIG=Release"

set "PROJECT_DIR=%~dp0"
set "EXE_PATH=%PROJECT_DIR%src\UI\bin\%CONFIG%\net9.0-windows\SunEyeVision.UI.exe"

echo ========================================
echo  SunEyeVision 调试启动器
echo  配置: %CONFIG%
echo  路径: %EXE_PATH%
echo ========================================

if not exist "%EXE_PATH%" (
    echo [构建] 可执行文件不存在，正在构建...
    dotnet build "%PROJECT_DIR%src\UI\SunEyeVision.UI.csproj" -c %CONFIG%
    if errorlevel 1 (
        echo [错误] 构建失败！
        pause
        exit /b 1
    )
)

echo [启动] 正在启动应用...
start "" "%EXE_PATH%"

echo [完成] 应用已启动
endlocal
