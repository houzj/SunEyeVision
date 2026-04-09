@echo off
cd /d "%~dp0\src\Workflow"
dotnet build --configuration Debug
if %errorlevel% neq 0 (
    echo 编译失败！
    pause
    exit /b 1
)
echo 编译成功！
pause
