@echo off
cd /d d:\MyWork\SunEyeVision\SunEyeVision\SunEyeVision.Core
echo Building SunEyeVision.Core...
dotnet build SunEyeVision.Core.csproj > ..\build_core.txt 2>&1
type ..\build_core.txt
