@echo off
setlocal enabledelayedexpansion

cd /d "%~dp0"

echo ========================================
echo   运行规则检查器 (Rule Checker)
echo ========================================
echo.

:: 检查规则检查器是否存在
if not exist "Release\SunEyeVision_v1.0.0\SunEyeVision\SunEyeVision.RuleChecker.dll" (
    echo ❌ 规则检查器未找到，请先编译
    echo 💡 运行: build_rule_checker.bat
    pause
    exit /b 1
)

:: 运行规则检查器
echo 正在运行规则检查器...
echo.

dotnet Release\SunEyeVision_v1.0.0\SunEyeVision\SunEyeVision.RuleChecker.dll "d:\MyWork\SunEyeVision_Dev\src"

echo.
echo ========================================
pause
