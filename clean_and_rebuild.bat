@echo off
cd /d "%~dp0"
echo ========================================
echo 清理并重新编译解决方案
echo ========================================
echo.

:: 设置临时日志文件（使用系统临时目录）
set TEMP_LOG=%TEMP%\suneyevision_clean_%RANDOM%.txt

echo [1/4] 清理解决方案...
dotnet clean SunEyeVision.New.sln > "%TEMP_LOG%" 2>&1
if errorlevel 1 (
    echo 清理失败！
    type "%TEMP_LOG%"
    del "%TEMP_LOG%" 2>nul
    pause
    exit /b 1
)
echo 清理完成！
echo.

echo [2/4] 清理 bin 和 obj 目录...
for /d /r . %%d in (bin obj) do @if exist "%%d" rd /s /q "%%d"
echo 清理完成！
echo.

echo [3/4] 重新编译 Workflow 项目...
dotnet build src/Workflow/SunEyeVision.Workflow.csproj --configuration Release >> "%TEMP_LOG%" 2>&1
if errorlevel 1 (
    echo Workflow 编译失败！
    type "%TEMP_LOG%"
    del "%TEMP_LOG%" 2>nul
    pause
    exit /b 1
)
echo Workflow 编译成功！
echo.

echo [4/4] 重新编译 UI 项目...
dotnet build src/UI/SunEyeVision.UI.csproj --configuration Release >> "%TEMP_LOG%" 2>&1
if errorlevel 1 (
    echo UI 编译失败！
    type "%TEMP_LOG%"
    del "%TEMP_LOG%" 2>nul
    pause
    exit /b 1
)
echo UI 编译成功！
echo.

:: 全部成功，删除临时日志
del "%TEMP_LOG%" 2>nul

echo ========================================
echo 全部编译成功！
echo ========================================
pause
