@echo off
setlocal enabledelayedexpansion

cd /d "%~dp0"

echo ========================================
echo   构建规则检查器 (Rule Checker)
echo ========================================
echo.

:: 设置临时日志文件
set TEMP_LOG=%TEMP%\rule_checker_build_%RANDOM%.txt

:: 清理函数
:cleanup
if exist "%TEMP_LOG%" del "%TEMP_LOG%" 2>nul
exit /b %errorlevel%

:: 编译规则检查器
echo 正在编译规则检查器...
dotnet build utilities/RuleChecker/RuleChecker.csproj --configuration Release > "%TEMP_LOG%" 2>&1

if %errorlevel% neq 0 (
    echo.
    echo ❌ 编译失败！
    echo.
    echo 错误信息：
    type "%TEMP_LOG%"
    call :cleanup
    pause
    exit /b 1
)

echo ✅ 编译成功！
echo.

:: 显示使用说明
echo ========================================
echo   使用说明
echo ========================================
echo.
echo 运行规则检查器：
echo   utilities\RuleChecker\bin\Release\net8.0\SunEyeVision.RuleChecker.exe
echo.
echo 检查指定目录：
echo   utilities\RuleChecker\bin\Release\net8.0\SunEyeVision.RuleChecker.exe "路径"
echo.
echo 在 VS Code 中作为任务运行：
echo   .vscode\tasks.json
echo.
echo 在 Git pre-commit hook 中运行：
echo   .git\hooks\pre-commit
echo.

call :cleanup
pause
exit /b 0
