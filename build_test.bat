@echo off
cd /d "d:\MyWork\SunEyeVision\SunEyeVision"
dotnet build tests\SunEyeVision.Core.Tests\SunEyeVision.Core.Tests.csproj --configuration Debug --no-restore 2>&1
pause
