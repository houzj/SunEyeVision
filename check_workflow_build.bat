@echo off
cd /d d:\MyWork\SunEyeVision\SunEyeVision
dotnet build src\SunEyeVision.Workflow\SunEyeVision.Workflow.csproj > build_workflow_check.txt 2>&1
echo.
echo 结果已保存到 build_workflow_check.txt
type build_workflow_check.txt
