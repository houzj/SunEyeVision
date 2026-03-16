@echo off
echo ========================================
echo   重新注册文件关联（使用新图标）
echo ========================================
echo.

set EXE_PATH=d:\MyWork\SunEyeVision\SunEyeVision\src\UI\bin\Release\net9.0-windows\SunEyeVision.UI.exe
set ICON_PATH=d:\MyWork\SunEyeVision\SunEyeVision\src\UI\bin\Release\net9.0-windows\Icons\solution.ico

echo 检查图标文件...
if not exist "%ICON_PATH%" (
    echo ❌ 错误: 图标文件不存在!
    echo    路径: %ICON_PATH%
    pause
    exit /b 1
)
echo ✅ 图标文件存在
dir "%ICON_PATH%" | find "solution.ico"
echo.

echo 正在注册文件关联...
echo.

echo [1/6] 注册文件扩展名...
reg add "HKCU\Software\Classes\.solution" /ve /t REG_SZ /d "SunEyeVision.SolutionFile" /f >nul
if errorlevel 1 (
    echo ❌ 失败
) else (
    echo ✅ 成功
)

echo [2/6] 设置内容类型...
reg add "HKCU\Software\Classes\.solution" /v "Content Type" /t REG_SZ /d "application/vnd.suneyvision.solution" /f >nul
if errorlevel 1 (
    echo ❌ 失败
) else (
    echo ✅ 成功
)

echo [3/6] 注册 ProgID...
reg add "HKCU\Software\Classes\SunEyeVision.SolutionFile" /ve /t REG_SZ /d "SunEyeVision 解决方案文件" /f >nul
if errorlevel 1 (
    echo ❌ 失败
) else (
    echo ✅ 成功
)

echo [4/6] 注册自定义图标...
reg add "HKCU\Software\Classes\SunEyeVision.SolutionFile\DefaultIcon" /ve /t REG_SZ /d "\"%ICON_PATH%\",0" /f >nul
if errorlevel 1 (
    echo ❌ 失败
) else (
    echo ✅ 成功
    echo    图标路径: "%ICON_PATH%"
)

echo [5/6] 注册打开命令...
reg add "HKCU\Software\Classes\SunEyeVision.SolutionFile\shell\open\command" /ve /t REG_SZ /d "\"%EXE_PATH%\" \"%%1\"" /f >nul
if errorlevel 1 (
    echo ❌ 失败
) else (
    echo ✅ 成功
)

echo [6/6] 注册编辑命令...
reg add "HKCU\Software\Classes\SunEyeVision.SolutionFile\shell\edit\command" /ve /t REG_SZ /d "\"%EXE_PATH%\" \"%%1\"" /f >nul
if errorlevel 1 (
    echo ❌ 失败
) else (
    echo ✅ 成功
)

echo.
echo ========================================
echo   注册完成！
echo ========================================
echo.
echo 正在清除图标缓存...
taskkill /f /im explorer.exe >nul 2>&1
cd /d "%userprofile%\AppData\Local\Microsoft\Windows\Explorer"
del /f /q iconcache_*.db >nul 2>&1
start explorer.exe

echo.
echo ✅ 图标缓存已清除
echo.
echo 请按照以下步骤查看图标：
echo 1. 等待桌面重新加载（几秒钟）
echo 2. 按 F5 刷新文件资源管理器
echo 3. 查看项目目录中的 .solution 文件
echo.
pause
