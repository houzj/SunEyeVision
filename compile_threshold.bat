@echo off
cd /d "d:\MyWork\SunEyeVision_Dev-tool"
dotnet build "Tools\SunEyeVision.Tool.Threshold\SunEyeVision.Tool.Threshold.csproj" --no-incremental
echo.
echo ==================== 编译结果 ====================
echo.
findstr /C:"Build succeeded" /C:"Build FAILED" /C:"error MC" /C:"error CS"
echo.
pause