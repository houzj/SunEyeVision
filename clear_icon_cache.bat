@echo off
echo ========================================
echo   清除 Windows 图标缓存
echo ========================================
echo.

echo [1/4] 停止 Windows 资源管理器...
taskkill /f /im explorer.exe >nul 2>&1

echo [2/4] 删除图标缓存数据库...
cd /d "%userprofile%\AppData\Local"
del /f /q IconCache.db >nul 2>&1
del /f /q IconCache_{*}.db >nul 2>&1

cd /d "%userprofile%\AppData\Local\Microsoft\Windows\Explorer"
del /f /q iconcache_*.db >nul 2>&1

echo [3/4] 清除缩略图缓存...
cd /d "%userprofile%\AppData\Local\Microsoft\Windows\Explorer"
del /f /q thumbcache_*.db >nul 2>&1

echo [4/4] 重新启动 Windows 资源管理器...
start explorer.exe

echo.
echo ========================================
echo   图标缓存已清除！
echo ========================================
echo.
echo 请按照以下步骤查看图标效果：
echo 1. 等待桌面重新加载
echo 2. 按 F5 刷新文件资源管理器
echo 3. 查看项目目录中的 test_solution.solution 文件
echo.
pause
