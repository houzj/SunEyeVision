@echo off
echo Building Workflow project...
cd "d:\MyWork\SunEyeVision\SunEyeVision"
dotnet build src\Workflow\SunEyeVision.Workflow.csproj --no-incremental
echo Build completed.
pause
