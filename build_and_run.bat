@echo off
cd /d "%~dp0"
echo Building SunEyeVision...
dotnet build SunEyeVision.sln --configuration Release
if %errorlevel% neq 0 (
    echo Build failed!
    pause
    exit /b 1
)
echo.
echo Running SunEyeVision.UI...
dotnet run --project src\UI\SunEyeVision.UI.csproj --configuration Release
pause
