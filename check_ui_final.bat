@echo off
echo =====================================================
echo 编译检查 - UI项目
echo =====================================================
cd /d "D:\MyWork\SunEyeVision\SunEyeVision"
dotnet build src/UI/SunEyeVision.UI.csproj > build_ui_check_final.txt 2>&1

echo.
echo =====================================================
echo 查找编译错误
echo =====================================================
findstr /C:"error CS" build_ui_check_final.txt

echo.
echo =====================================================
echo 编译完成
echo =====================================================
pause
