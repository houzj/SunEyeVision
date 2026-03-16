@echo off
echo =====================================================
echo 编译检查 Workflow 项目
echo =====================================================
cd /d "D:\MyWork\SunEyeVision\SunEyeVision"
dotnet build src/Workflow/SunEyeVision.Workflow.csproj > build_workflow_result.txt 2>&1
echo Workflow 编译完成
type build_workflow_result.txt | findstr /C:"error"
echo.
echo =====================================================
echo 编译检查 UI 项目
echo =====================================================
dotnet build src/UI/SunEyeVision.UI.csproj > build_ui_result.txt 2>&1
echo UI 编译完成
type build_ui_result.txt | findstr /C:"error"
echo.
echo =====================================================
echo 完整编译
echo =====================================================
dotnet build SunEyeVision.sln > build_full_result.txt 2>&1
echo 完整编译完成
type build_full_result.txt | findstr /C:"error"
echo.
echo =====================================================
echo 编译错误总结
echo =====================================================
echo Workflow 错误:
type build_workflow_result.txt | findstr /C:"error CS" | find /C /V ""
echo.
echo UI 错误:
type build_ui_result.txt | findstr /C:"error CS" | find /C /V ""
echo.
echo 完整解决方案错误:
type build_full_result.txt | findstr /C:"error CS" | find /C /V ""
echo.
echo 检查完成！
pause
