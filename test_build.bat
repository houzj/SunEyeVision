@echo off
cd /d d:\MyWork\SunEyeVision\SunEyeVision
echo Building Plugin.SDK...
dotnet build src/Plugin.SDK/Plugin.SDK.csproj
if %ERRORLEVEL% NEQ 0 (
    echo Build failed!
    pause
    exit /b 1
)
echo Build succeeded!
pause
