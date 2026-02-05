@echo off
cd /d "%~dp0"
echo Building SunEyeVision.UI...
dotnet build SunEyeVision.UI\SunEyeVision.UI.csproj --verbosity minimal
if %ERRORLEVEL% EQU 0 (
    echo Build successful! Starting application...
    start "" "SunEyeVision.UI\bin\Debug\net9.0-windows\SunEyeVision.UI.exe"
) else (
    echo Build failed!
    pause
)
