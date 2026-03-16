@echo off
cd /d "d:\MyWork\SunEyeVision\SunEyeVision"
echo Rebuilding with new icons...
dotnet build src/UI/SunEyeVision.UI.csproj -c Release -v minimal
if errorlevel 1 (
    echo Build failed!
) else (
    echo Build succeeded!
)
echo.
echo Icon files:
dir "src\UI\Icons\solution*.ico" /B
echo.
echo Deployed icons:
dir "src\UI\bin\Release\net9.0-windows\Icons\solution*.ico" /B 2>nul || echo Not found
echo.
pause
