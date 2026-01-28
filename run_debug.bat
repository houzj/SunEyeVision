@echo off
cd /d "%~dp0"
echo Building SunEyeVision.UI...
dotnet build SunEyeVision.UI --configuration Release --verbosity quiet
echo.
echo Starting SunEyeVision.UI...
SunEyeVision.UI\bin\Release\net9.0-windows\SunEyeVision.UI.exe
pause
