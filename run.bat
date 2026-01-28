@echo off
cd /d "%~dp0"
echo Starting SunEyeVision.UI...
dotnet run --project SunEyeVision.UI --configuration Release
pause
