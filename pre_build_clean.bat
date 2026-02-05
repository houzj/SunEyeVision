@echo off
REM Visual Studio 预构建事件脚本
REM 在每次构建前清理另一个配置的输出，避免程序集冲突

setlocal enabledelayedexpansion

echo [PreBuild] Cleaning conflicting output directories...

cd /d "%~dp0"

REM 清理 Release 目录
if "%1"=="Debug" (
    echo Cleaning Release directories...
    for /d /r %%d in (bin\Release) do (
        if exist "%%d" (
            echo   Removing: %%d
            rd /s /q "%%d" 2>nul
        )
    )
)

REM 清理 Debug 目录
if "%1"=="Release" (
    echo Cleaning Debug directories...
    for /d /r %%d in (bin\Debug) do (
        if exist "%%d" (
            echo   Removing: %%d
            rd /s /q "%%d" 2>nul
        )
    )
)

echo [PreBuild] Cleanup completed
exit /b 0
