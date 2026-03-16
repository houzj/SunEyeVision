@echo off
cd /d "d:\MyWork\SunEyeVision\SunEyeVision"
echo Building SunEyeVision.UI...
dotnet build src/UI/SunEyeVision.UI.csproj -c Release -v minimal > build_icon_log.txt 2>&1
type build_icon_log.txt
echo.
echo Build completed.
