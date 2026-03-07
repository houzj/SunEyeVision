@echo off
echo ========================================
echo SunEyeVision 统一集成测试运行器
echo ========================================
echo.

REM 设置环境
set TEST_DIR=%~dp0
set PROJECT_ROOT=%TEST_DIR%..\

REM 进入项目根目录
cd /d "%PROJECT_ROOT%"

echo [1/4] 清理旧的测试结果...
if exist TestResults rmdir /s /q TestResults
if exist test_report.txt del /f test_report.txt

echo [2/4] 编译测试项目...
dotnet build tests\SunEyeVision.Core.Tests\SunEyeVision.Core.Tests.csproj --configuration Debug --no-restore
if %ERRORLEVEL% neq 0 (
    echo 编译失败！
    exit /b 1
)

echo [3/4] 运行单元测试和集成测试...
dotnet test tests\SunEyeVision.Core.Tests\SunEyeVision.Core.Tests.csproj --configuration Debug --no-build --logger "trx;LogFileName=test_results.trx" --results-directory TestResults

echo [4/4] 生成测试报告...
echo ======================================== >> test_report.txt
echo SunEyeVision 测试报告 >> test_report.txt
echo 时间: %DATE% %TIME% >> test_report.txt
echo ======================================== >> test_report.txt
echo. >> test_report.txt
echo 测试已完成，结果保存在 TestResults 目录中。 >> test_report.txt
echo. >> test_report.txt

echo.
echo ========================================
echo 测试完成！
echo 结果目录: %PROJECT_ROOT%TestResults
echo ========================================

pause
