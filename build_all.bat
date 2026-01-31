@echo off
cd /d d:\MyWork\SunEyeVision\SunEyeVision
echo ========================================
echo Building all projects...
echo ========================================

echo.
echo [1/6] Building SunEyeVision.Core...
cd SunEyeVision.Core
dotnet build SunEyeVision.Core.csproj > ..\build_all_output.txt 2>&1
if %errorlevel% neq 0 echo [FAILED] SunEyeVision.Core
if %errorlevel% equ 0 echo [OK] SunEyeVision.Core
cd ..

echo.
echo [2/6] Building SunEyeVision.Algorithms...
cd SunEyeVision.Algorithms
dotnet build SunEyeVision.Algorithms.csproj >> ..\build_all_output.txt 2>&1
if %errorlevel% neq 0 echo [FAILED] SunEyeVision.Algorithms
if %errorlevel% equ 0 echo [OK] SunEyeVision.Algorithms
cd ..

echo.
echo [3/6] Building SunEyeVision.Workflow...
cd SunEyeVision.Workflow
dotnet build SunEyeVision.Workflow.csproj >> ..\build_all_output.txt 2>&1
if %errorlevel% neq 0 echo [FAILED] SunEyeVision.Workflow
if %errorlevel% equ 0 echo [OK] SunEyeVision.Workflow
cd ..

echo.
echo [4/6] Building SunEyeVision.DeviceDriver...
cd SunEyeVision.DeviceDriver
dotnet build SunEyeVision.DeviceDriver.csproj >> ..\build_all_output.txt 2>&1
if %errorlevel% neq 0 echo [FAILED] SunEyeVision.DeviceDriver
if %errorlevel% equ 0 echo [OK] SunEyeVision.DeviceDriver
cd ..

echo.
echo [5/6] Building SunEyeVision.PluginSystem...
cd SunEyeVision.PluginSystem
dotnet build SunEyeVision.PluginSystem.csproj >> ..\build_all_output.txt 2>&1
if %errorlevel% neq 0 echo [FAILED] SunEyeVision.PluginSystem
if %errorlevel% equ 0 echo [OK] SunEyeVision.PluginSystem
cd ..

echo.
echo [6/6] Building SunEyeVision.UI...
cd SunEyeVision.UI
dotnet build SunEyeVision.UI.csproj >> ..\build_all_output.txt 2>&1
if %errorlevel% neq 0 echo [FAILED] SunEyeVision.UI
if %errorlevel% equ 0 echo [OK] SunEyeVision.UI
cd ..

echo.
echo ========================================
echo Build complete. See build_all_output.txt for details
echo ========================================
type build_all_output.txt
pause
