@echo off
cd /d "d:\MyWork\SunEyeVision\SunEyeVision\src\Plugin.Abstractions"
dotnet build --verbosity minimal > build_output.txt 2>&1
type build_output.txt
