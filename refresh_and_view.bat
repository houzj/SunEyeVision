@echo off
echo ========================================
echo   刷新文件资源管理器并查看图标
echo ========================================
echo.

echo 步骤 1: 停止 Windows 资源管理器...
taskkill /f /im explorer.exe >nul 2>&1
if errorlevel 1 (
    echo ✅ 资源管理器已停止
) else (
    echo ✅ 资源管理器已停止
)
echo.

echo 步骤 2: 清除图标缓存...
cd /d "%userprofile%\AppData\Local"
del /f /q IconCache.db >nul 2>&1
del /f /q IconCache_{*}.db >nul 2>&1
cd /d "%userprofile%\AppData\Local\Microsoft\Windows\Explorer"
del /f /q iconcache_*.db >nul 2>&1
echo ✅ 图标缓存已清除
echo.

echo 步骤 3: 重新启动 Windows 资源管理器...
start explorer.exe
echo ✅ 资源管理器已重新启动
echo.

echo 步骤 4: 打开项目目录...
cd /d "d:\MyWork\SunEyeVision\SunEyeVision"
start explorer.exe .
echo ✅ 项目目录已打开
echo.

echo ========================================
echo   完成！
echo ========================================
echo.
echo 现在您应该能看到 .solution 文件的自定义图标了！
echo.
echo 如果图标仍未显示，请：
echo 1. 等待几秒钟（Windows 可能需要时间加载图标）
echo 2. 在文件资源管理器中按 F5 刷新
echo 3. 右键 .solution 文件，选择"属性" > "更改图标"
echo.
pause
