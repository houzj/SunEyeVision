@echo off
cd /d "%~dp0"
echo ========================================
echo Starting SunEyeVision with NativeDiagram...
echo ========================================
echo.
echo [INFO] Starting application...
dotnet run --project SunEyeVision.UI\SunEyeVision.UI.csproj --configuration Release
echo.
echo [INFO] Application closed.
pause
