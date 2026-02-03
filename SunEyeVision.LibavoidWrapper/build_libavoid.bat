@echo off
cd /d "%~dp0"
echo ========================================================
echo 编译 SunEyeVision.LibavoidWrapper
echo ========================================================

REM 尝试常见的MSBuild路径
set MSBUILD_PATH=

REM 检查 VS2022
if exist "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" (
    set MSBUILD_PATH=C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe
)

REM 检查 VS2019
if "%MSBUILD_PATH%"=="" (
    if exist "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe" (
        set MSBUILD_PATH=C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe
    )
)

REM 检查 VS2017
if "%MSBUILD_PATH%"=="" (
    if exist "C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\MSBuild.exe" (
        set MSBUILD_PATH=C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\MSBuild.exe
    )
)

if "%MSBUILD_PATH%"=="" (
    echo ERROR: 找不到 MSBuild.exe
    echo 请确保已安装 Visual Studio
    pause
    exit /b 1
)

echo 找到 MSBuild: %MSBUILD_PATH%
echo.
echo 开始编译...
echo ========================================================

"%MSBUILD_PATH%" SunEyeVision.LibavoidWrapper.vcxproj /t:Rebuild /p:Configuration=Debug /p:Platform=x64 /v:minimal

echo ========================================================
echo 编译完成！
echo ========================================================

pause
