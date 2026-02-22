@echo off
cd /d "%~dp0"
echo Building SunEyeVision (Debug)...
dotnet build SunEyeVision.sln --configuration Debug
if %errorlevel% neq 0 (
    echo Build failed!
    pause
    exit /b 1
)
echo.
echo Running SunEyeVision.UI (Debug)...
dotnet run --project src\UI\SunEyeVision.UI.csproj --configuration Debug
pause
