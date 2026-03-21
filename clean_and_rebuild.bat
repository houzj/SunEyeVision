@echo off
echo ========================================
echo 清理并重新编译解决方案
echo ========================================
echo.

echo [1/4] 清理解决方案...
dotnet clean SunEyeVision.New.sln
if errorlevel 1 (
    echo 清理失败！
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
dotnet build src/Workflow/SunEyeVision.Workflow.csproj --configuration Release
if errorlevel 1 (
    echo Workflow 编译失败！
    pause
    exit /b 1
)
echo Workflow 编译成功！
echo.

echo [4/4] 重新编译 UI 项目...
dotnet build src/UI/SunEyeVision.UI.csproj --configuration Release
if errorlevel 1 (
    echo UI 编译失败！
    pause
    exit /b 1
)
echo UI 编译成功！
echo.

echo ========================================
echo 全部编译成功！
echo ========================================
pause
