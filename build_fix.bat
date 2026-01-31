@echo off
cd /d d:\MyWork\SunEyeVision\SunEyeVision\SunEyeVision.UI
echo Building SunEyeVision.UI...
dotnet build SunEyeVision.UI.csproj --configuration Debug --no-incremental > build_output.txt 2>&1
type build_output.txt
pause
