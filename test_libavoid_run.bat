@echo off
chcp 65001 > nul
echo ╔══════════════════════════════════════════════════════════════╗
echo ║          SunEyeVision UI 测试运行脚本                       ║
echo ╚══════════════════════════════════════════════════════════════╝
echo.

cd /d "%~dp0SunEyeVision.UI\bin\Debug\net9.0-windows"

echo [脚本] 检查文件...
if exist "SunEyeVision.UI.exe" (
    echo [脚本] ✅ SunEyeVision.UI.exe 存在
) else (
    echo [脚本] ❌ SunEyeVision.UI.exe 不存在
    goto :end
)

if exist "SunEyeVision.LibavoidWrapper.dll" (
    echo [脚本] ✅ SunEyeVision.LibavoidWrapper.dll 存在
    for %%A in ("SunEyeVision.LibavoidWrapper.dll") do echo [脚本]    文件大小: %%~zA 字节
) else (
    echo [脚本] ❌ SunEyeVision.LibavoidWrapper.dll 不存在
    goto :end
)

echo.
echo [脚本] 启动应用程序...
echo [脚本] 注意: 如果程序崩溃，请查看下方的调试输出
echo.
echo ╔══════════════════════════════════════════════════════════════╗
echo ║                      应用程序输出                             ║
echo ╚══════════════════════════════════════════════════════════════╝
echo.

SunEyeVision.UI.exe

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo ╔══════════════════════════════════════════════════════════════╗
    echo ║                    程序异常退出                             ║
    echo ╚══════════════════════════════════════════════════════════════╝
    echo [脚本] 退出代码: %ERRORLEVEL%
    echo [脚本] 如果程序闪退，可能是以下原因之一:
    echo [脚本] 1. DLL 架构不匹配（x86 vs x64）
    echo [脚本] 2. 缺少依赖项
    echo [脚本] 3. 运行时异常
    echo.
    echo [脚本] 请使用 DebugView (Sysinternals) 捕获调试输出:
    echo [脚本] 下载: https://learn.microsoft.com/en-us/sysinternals/downloads/debugview
) else (
    echo.
    echo ╔══════════════════════════════════════════════════════════════╗
    echo ║                    程序正常退出                             ║
    echo ╚══════════════════════════════════════════════════════════════╝
)

:end
echo.
pause
