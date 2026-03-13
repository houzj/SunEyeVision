@echo off
cd /d %~dp0
dotnet build src/UI/SunEyeVision.UI.csproj 2>&1
if %ERRORLEVEL% EQU 0 (
    echo === 编译成功 ===
) else (
    echo === 编译失败，错误代码: %ERRORLEVEL% ===
)
