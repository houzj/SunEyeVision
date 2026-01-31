@echo off
cd /d "%~dp0"
echo Building SunEyeVision...
dotnet build --no-incremental 2>&1 > build_debug_output.txt
echo Build complete. Output saved to build_debug_output.txt
type build_debug_output.txt
pause
