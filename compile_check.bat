@echo off
cd /d "d:\MyWork\SunEyeVision_Dev-tool"
dotnet build --no-incremental 2>&1 | findstr /C:"error CS" /C:"warning CS" /C:"Build succeeded" /C:"Build FAILED"
